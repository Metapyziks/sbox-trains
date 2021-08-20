using Sandbox;
using Ziks.Trains.RollingStock;
using Ziks.Trains.Track;
using Ziks.Trains.UI;

namespace Ziks.Trains
{
    public partial class Game : Sandbox.Game
    {
		public new static Game Current => (Game)Sandbox.Game.Current;

		[Net]
		public HexGrid HexGrid { get; set; }

		[Net]
		public TrackManager TrackManager { get; set; }

		[Net]
		public Hud Hud { get; set; }

		public Game()
		{
			if ( IsServer )
			{
				HexGrid = new HexGrid();
				TrackManager = new TrackManager();
				Hud = new Hud();

				var train = new Train { Throttle = 1f };

				train.SpawnCar( TrainCarModel.Engine );
				train.SpawnCar( TrainCarModel.Engine );
				train.SpawnCar( TrainCarModel.Engine );
				train.SpawnCar( TrainCarModel.Engine );
			}
		}

		public override void ClientJoined( Client cl )
	    {
		    base.ClientJoined( cl );

		    var player = new Player();

		    cl.Pawn = player;

			player.Respawn();
	    }
    }
}
