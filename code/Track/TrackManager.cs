using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;

namespace Ziks.Trains.Track
{
	public partial class TrackManager : Entity
	{
		public static void SpawnTrack( HexCoordEdge from, HexCoordEdge to )
		{
			SpawnTrack( from.Coord.X, from.Coord.Y, from.Edge, to.Coord.X, to.Coord.Y, to.Edge );
		}

		[ServerCmd]
		private static void SpawnTrack( int fromCoordX, int fromCoordY, HexEdge fromEdge,
			int toCoordX, int toCoordY, HexEdge toEdge )
		{
			var trackMan = Game.Current.TrackManager;
			var client = ConsoleSystem.Caller;

			trackMan.SpawnTrack( client,
				new HexCoordEdge( fromCoordX, fromCoordY, fromEdge ),
				new HexCoordEdge( toCoordX, toCoordY, toEdge ) );
		}

		public static void DeleteTrack( HexCoordEdge touching )
		{
			DeleteTrack( touching.Coord.X, touching.Coord.Y, touching.Edge );
		}

		[ServerCmd]
		private static void DeleteTrack( int touchingCoordX, int touchingCoordY, HexEdge touchingEdge )
		{
			var trackMan = Game.Current.TrackManager;
			var client = ConsoleSystem.Caller;

			trackMan.DeleteTrack( client, new HexCoordEdge( touchingCoordX, touchingCoordY, touchingEdge ) );
		}

		public readonly struct SpawnedRailPiece : IEquatable<SpawnedRailPiece>
		{
			public readonly HexCoord HexCoord;
			public readonly RailPiece RailPiece;

			public SpawnedRailPiece( HexCoord hexCoord, RailPiece railPiece )
			{
				HexCoord = hexCoord;
				RailPiece = railPiece;
			}

			public bool Equals(SpawnedRailPiece other)
			{
				return HexCoord.Equals(other.HexCoord) && RailPiece == other.RailPiece;
			}

			public override bool Equals(object obj)
			{
				return obj is SpawnedRailPiece other && Equals(other);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(HexCoord, (int) RailPiece);
			}
		}

		private readonly Dictionary<HexCoord, RailTile> _tiles = new ();
		private readonly Dictionary<SpawnedRailPiece, ModelEntity> _pieces = new ();

		protected HexGrid HexGrid => Game.Current.HexGrid;

		public TrackManager()
		{
			Transmit = TransmitType.Always;
		}

		private readonly List<HexCoordEdge> _tempPath = new ();
		private readonly List<RailPiece> _tempPieces = new ();

		public RailTile this[ HexCoord coord ]
		{
			get => _tiles.TryGetValue( coord, out var tile ) ? tile : RailTile.None;
			set => SetTile( coord, value );
		}

		public void SpawnTrack( Client client, HexCoordEdge start, HexCoordEdge end )
		{
			Host.AssertServer();

			_tempPath.Clear();

			if ( !HexGrid.GetShortestPath( _tempPath, start, end, false ) )
			{
				return;
			}

			var prev = _tempPath.First();

			PlaceTile( prev.Coord, prev.Edge.GetBufferRailPiece().ToTile() );

			foreach ( var next in _tempPath.Skip( 1 ) )
			{
				var from = prev.Edge.Opposite();
				var to = next.Edge;

				if ( RailExtensions.TryGetRailPiece( from, to, out var piece ) )
				{
					PlaceTile( next.Coord, piece.ToTile() );
				}

				prev = next;
			}

			PlaceTile( prev.Coord + prev.Edge, prev.Edge.Opposite().GetBufferRailPiece().ToTile() );
		}

		public void DeleteTrack( Client client, HexCoordEdge touching )
		{
			Host.AssertServer();

			if ( !_tiles.TryGetValue( touching.Coord, out var tile ) )
			{
				return;
			}

			RemoveTile( touching.Coord, tile.FilterByEdge( touching.Edge ) );
		}

		public void PlaceTile( HexCoord coord, RailTile toAdd )
		{
			Host.AssertServer();

			if ( _tiles.TryGetValue( coord, out var existing ) )
			{
				SetTile( coord, existing.Combine( toAdd ) );
			}
			else
			{
				SetTile( coord, toAdd );
			}
		}

		public void RemoveTile( HexCoord coord, RailTile toRemove )
		{
			Host.AssertServer();

			if ( _tiles.TryGetValue( coord, out var existing ) )
			{
				SetTile( coord, existing & ~toRemove );
			}
		}

		public void SetTile( HexCoord coord, RailTile tile )
		{
			Host.AssertServer();

			tile = tile.Normalized();

			var toAdd = tile;
			var toRemove = RailTile.None;

			if ( _tiles.TryGetValue( coord, out var existing ) )
			{
				toRemove = existing & ~tile;
				toAdd = tile & ~existing;

				_tiles[coord] = tile;
			}
			else
			{
				_tiles.Add( coord, tile );
			}
			
			_tempPieces.Clear();
			toRemove.GetPieces( _tempPieces );

			// Delete old pieces
			foreach ( var piece in _tempPieces )
			{
				var key = new SpawnedRailPiece( coord, piece );

				if ( !_pieces.TryGetValue( key, out var ent ) )
				{
					continue;
				}

				_pieces.Remove( key );
				ent.Delete();
			}

			_tempPieces.Clear();
			toAdd.GetPieces( _tempPieces );

			// Spawn new pieces
			foreach ( var piece in _tempPieces )
			{
				var key = new SpawnedRailPiece( coord, piece );

				if ( _pieces.ContainsKey( key) )
				{
					continue;
				}

				var ent = new ModelEntity( "models/track.vmdl" )
				{
					Position = HexGrid.GetWorldPosition( coord )
				};

				var rot = ((int)piece - 3) * -60f + 180f;
				ent.Rotation = Rotation.FromYaw( rot );

				ent.SetBodyGroup( 0, (piece.ToTile() & RailTile.Curved) != 0 ? 1 : (piece.ToTile() & RailTile.Buffer) != 0 ? 2 : 0 );

				_pieces.Add( key, ent );
			}
		}
	}
}
