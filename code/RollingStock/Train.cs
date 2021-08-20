using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sandbox;
using Ziks.Trains.Track;

namespace Ziks.Trains.RollingStock
{
	public partial class Train : Entity
	{
		public struct TraversedTile
		{
			public HexCoord GridPos;
			public HexEdge FromEdge;
			public HexEdge ToEdge;
			public Vector3 FromPos;
			public Vector3 ToPos;
			public Rotation FromAngle;
			public Rotation ToAngle;
			public float Length;

			public bool Curved;
			public Vector3 CurveCenter;
			public float CurveRadius;
		}

		private const float G = 9.81f;
		private const float CoeffFriction = 0.04f;
		private const float CurveFrictionFactor = 4f;

		private float _traversedTilesLength;
		private readonly Queue<TraversedTile> _traversed = new Queue<TraversedTile>();

		protected HexGrid HexGrid => Game.Current.HexGrid;
		protected TrackManager TrackManager => Game.Current.TrackManager;

		[Net]
		private List<TrainCar> Cars { get; set; } = new ();

		[Net]
		public bool OnTrack { get; private set; }

		[Net]
		public float Spacing { get; set; } = 0.25f;

		[Net]
		public float HeadTileTravel { get; private set; }

		[Net]
		public float TotalLength { get; private set; }

		[Net]
		public float TrackVelocity { get; set; }

		[Net]
		public float Throttle { get; set; }

		public Train()
		{
			Transmit = TransmitType.Always;
		}

		public TrainCar SpawnCar( TrainCarModel model )
		{
			return SpawnCar( model, ^0 );
		}

		public TrainCar SpawnCar( TrainCarModel model, Index index )
		{
			Host.AssertServer();

			var car = new TrainCar();

			car.SetParent( this );

			switch ( model )
			{
				case TrainCarModel.Engine:
					car.EngineForce = 1f;
					car.Mass = 1f;
					car.SetModel( "models/engine.vmdl" );
					break;

				default:
					throw new NotImplementedException();
			}

			Cars.Insert( index.GetOffset( Cars.Count ), car );

			return car;
		}

		public bool TryGetTraversedTile( float distanceFromHead, out TraversedTile tile, out float travelOffset )
		{
			var distance = HeadTileTravel - distanceFromHead;
			travelOffset = 0f;

			var headTile = true;

			// TODO: this probably allocates
			foreach ( var t in _traversed.Reverse() )
			{
				if ( headTile ) headTile = false;
				else travelOffset += t.Length;

				if ( distance > -travelOffset )
				{
					tile = t;
					return true;
				}
			}

			tile = default;
			return false;
		}

		private void ClearTraversedTiles()
		{
			_traversed.Clear();
			_traversedTilesLength = 0f;
		}

		[ClientRpc]
		private void ClientPushTraversedTile( int gridPosX, int gridPosY, HexEdge from, HexEdge to )
		{
			PushTraversedTile( new HexCoord( gridPosX, gridPosY ), from, to );
		}

		private void PushTraversedTile( HexCoord gridPos, HexEdge from, HexEdge to )
		{
			if ( IsServer )
			{
				ClientPushTraversedTile( gridPos.X, gridPos.Y, from, to );
			}

			var curved = HexGrid.GetWorldCurve( gridPos, from, to, out var radius, out var center );

			var tile = new TraversedTile
			{
				GridPos = gridPos,
				FromEdge = from,
				ToEdge = to,
				FromPos = HexGrid.GetWorldPosition( gridPos, from ),
				ToPos = HexGrid.GetWorldPosition( gridPos, to ),
				FromAngle = Rotation.LookAt( HexGrid.GetWorldDirection( from.Opposite() ) ),
				ToAngle = Rotation.LookAt( HexGrid.GetWorldDirection( to ) ),
				Length = curved ? MathF.PI / 3f : 1f,

				Curved = curved,
				CurveRadius = radius,
				CurveCenter = center
			};

			_traversed.Enqueue( tile );
			_traversedTilesLength += tile.Length;

			var endTileLengths = _traversed.Peek().Length + _traversed.Last().Length;

			while ( _traversed.Count > 1 && _traversedTilesLength > TotalLength + endTileLengths )
			{
				_traversedTilesLength -= _traversed.Dequeue().Length;
			}
		}

		private bool GetTransform( float distanceFromHead, out Vector3 pos, out Rotation rot )
		{
			if ( !TryGetTraversedTile( distanceFromHead, out var tile, out var travelOffset ) )
			{
				pos = default;
				rot = default;
				return false;
			}

			var progress = (HeadTileTravel - distanceFromHead + travelOffset) / tile.Length;

			rot = Rotation.Slerp( tile.FromAngle, tile.ToAngle, progress );
			pos = tile.Curved
				? tile.CurveCenter + rot * new Vector3( 0f, -1f, 0f ) * tile.CurveRadius
				: Vector3.Lerp( tile.FromPos, tile.ToPos, progress );

			return true;
		}

		private static readonly Random Random = new();

		private bool TryPutOnTrack()
		{
			var gridPos = HexGrid.GetHexCoord( Position );

			var railTile = TrackManager[gridPos] & ~RailTile.Buffer;
			if ( railTile == RailTile.None ) return false;

			HeadTileTravel = 0f;
			OnTrack = true;

			var piece = railTile.GetRandomPiece( Random );
			var forward = Cars.First( x => x != null ).Rotation.Forward;

			var bestEdge = default( HexEdge );
			var bestDot = float.NegativeInfinity;

			for ( var i = 0; i < 6; ++i )
			{
				var edge = (HexEdge)i;

				if ( !piece.Touches( edge ) ) continue;

				var dir = HexGrid.GetWorldDirection( edge );
				var dot = Vector3.Dot( dir, forward );

				if ( dot > bestDot )
				{
					bestEdge = edge;
					bestDot = dot;
				}
			}

			ClearTraversedTiles();
			PushTraversedTile( gridPos, piece.GetConnectedEdge( bestEdge ), bestEdge );

			return true;
		}

		[Event("server.tick")]

		private void OnServerTick()
		{
			if ( Cars.Count == 0 || !OnTrack && !TryPutOnTrack() ) return;
			
			HeadTileTravel += TrackVelocity * Time.Delta;

			var offset = 0f;
			var totalMass = 0f;
			var totalReactionForce = 0f;
			var totalEngineForce = 0f;

			foreach ( var car in Cars )
			{
				if ( car == null ) continue;

				offset += car.Length * 0.5f;
				car.DistanceFromHead = offset;
				offset += car.Length * 0.5f + Spacing;

				totalMass += car.Mass;
				totalEngineForce += car.EngineForce;

				if ( !TryGetTraversedTile( car.DistanceFromHead, out var tile, out _ ) )
				{
					// TODO: throw?
					continue;
				}

				var curveForce = tile.Curved ? CurveFrictionFactor * TrackVelocity * TrackVelocity : 0;

				totalReactionForce += car.Mass * (G + curveForce);
			}

			TotalLength = offset;
			
			TrackVelocity += Throttle * totalEngineForce / totalMass * Time.Delta;
			TrackVelocity -= Math.Min( totalReactionForce * CoeffFriction * Time.Delta / totalMass, Math.Abs( TrackVelocity ) ) * Math.Sign( TrackVelocity );

			TraversedTile headTile;

			while ( HeadTileTravel > (headTile = _traversed.Last()).Length )
			{
				// TODO: This will skip applying bonus friction for corners when going very, very fast

				HeadTileTravel -= headTile.Length;

				var nextFromEdge = headTile.ToEdge.Opposite();
				var nextGridPos = headTile.GridPos + headTile.ToEdge ;

				var nextTile = TrackManager[nextGridPos].FilterByEdge( nextFromEdge ) & ~RailTile.Buffer;

				if ( nextTile == RailTile.None )
				{
					Throttle = 0f;
					TrackVelocity = 0f;
					HeadTileTravel = headTile.Length;
					break;
				}

				var nextPiece = nextTile.GetRandomPiece( Random );
				var nextToEdge = nextPiece.GetConnectedEdge( nextFromEdge );

				PushTraversedTile( nextGridPos, nextFromEdge, nextToEdge );
			}

			UpdateTransform();
		}

		[Event.Frame]
		private void OnClientFrame()
		{
			// UpdateTransform();
		}

		private void UpdateTransform()
		{
			if ( Cars.Count == 0 || !OnTrack ) return;

			var isHead = true;

			foreach ( var car in Cars )
			{
				if ( car == null || !car.IsValid() ) continue;

				var targetTransform = isHead ? (Entity)this : car;

				if ( isHead )
				{
					car.LocalPosition = Vector3.Zero;
					car.LocalRotation = Rotation.Identity;
				}

				if ( !GetTransform( car.DistanceFromHead - car.Length * 0.375f, out var frontPos, out var frontRot ) ) continue;
				if ( !GetTransform( car.DistanceFromHead + car.Length * 0.375f, out var rearPos, out var rearRot ) ) continue;

				targetTransform.Rotation = Rotation.FromYaw( 90f ) * Rotation.Slerp( frontRot, rearRot, 0.5f );
				targetTransform.Position = (frontPos + rearPos) * 0.5f;

				isHead = false;
			}
		}
	}
}
