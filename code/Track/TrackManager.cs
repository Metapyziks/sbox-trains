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
		public static void SpawnTrack( HexCoord fromCoord, HexEdge fromEdge, HexCoord toCoord, HexEdge toEdge )
		{
			SpawnTrack( fromCoord.X, fromCoord.Y, fromEdge, toCoord.X, toCoord.Y, toEdge );
		}

		[ServerCmd]
		private static void SpawnTrack( int fromCoordX, int fromCoordY, HexEdge fromEdge, int toCoordX, int toCoordY, HexEdge toEdge )
		{
			var trackMan = Game.Current.TrackManager;
			var client = ConsoleSystem.Caller;

			trackMan.SpawnTrack( client,
				new HexCoord(fromCoordX, fromCoordY), fromEdge,
				new HexCoord( toCoordX, toCoordY ), toEdge );
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

		private readonly List<(HexCoord coord, HexEdge edge)> _tempPath = new List<(HexCoord coord, HexEdge edge)>();
		private readonly List<RailPiece> _tempPieces = new List<RailPiece>();

		public void SpawnTrack( Client client, HexCoord fromCoord, HexEdge fromEdge, HexCoord toCoord, HexEdge toEdge )
		{
			Host.AssertServer();

			var hexGrid = Game.Current.HexGrid;

			_tempPath.Clear();

			if ( !HexGrid.GetShortestPath( _tempPath, fromCoord, fromEdge, toCoord, toEdge, false ) )
			{
				return;
			}

			var prev = _tempPath.First();

			PlaceTile( prev.coord, prev.edge.GetBufferRailPiece().ToTile() );

			foreach ( var next in _tempPath.Skip( 1 ) )
			{
				var from = prev.Item2.Opposite();
				var to = next.Item2;

				if ( RailExtensions.TryGetRailPiece( from, to, out var piece ) )
				{
					PlaceTile( next.coord, piece.ToTile() );
				}

				prev = next;
			}

			PlaceTile( prev.coord + prev.edge, prev.edge.Opposite().GetBufferRailPiece().ToTile() );
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
