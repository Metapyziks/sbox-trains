using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;

namespace Ziks.Trains.Track
{
	public class TrackManager : Entity
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

		public TrackManager()
		{
			Transmit = TransmitType.Always;
		}

		private readonly List<(HexCoord coord, HexEdge edge)> _tempPath = new List<(HexCoord coord, HexEdge edge)>();

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

			foreach ( var next in _tempPath.Skip( 1 ) )
			{
				var from = prev.Item2.Opposite();
				var to = next.Item2;

				if ( RailExtensions.TryGetRailPiece( from, to, out var piece ) )
				{
					var model = new ModelEntity( "models/track.vmdl" )
					{
						Position = hexGrid.GetWorldPosition( next.Item1 )
					};

					if ( (piece.ToTile() & RailTile.Curved) == 0 )
					{
						model.SetBodyGroup( 0, 0 );
						model.Rotation = Rotation.FromYaw( 90f ) *
						                 Rotation.LookAt( hexGrid.GetWorldDirection( next.Item2 ) );
					}
					else
					{
						Log.Info( $"{from} -> {to}" );

						var rot = ((int)piece - 3) * -60f + 180f;

						model.SetBodyGroup( 0, 1 );
						model.Rotation = Rotation.FromYaw( rot );
					}
				}

				prev = next;
			}
		}
	}
}
