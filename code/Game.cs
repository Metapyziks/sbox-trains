using Sandbox;
using Ziks.Trains.Facilities;
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
		public FacilityManager FacilityManager { get; set; }

		[Net]
		public Hud Hud { get; set; }

		public Game()
		{
			if ( IsServer )
			{
				HexGrid = new HexGrid();
				TrackManager = new TrackManager();
				FacilityManager = new FacilityManager();

				Hud = new Hud();
			}
		}

		[Event.Entity.PostSpawn]
		private void OnPostSpawn()
		{
			if ( !IsServer ) return;

			// Example train

			var train = new Train { Throttle = 1f };

			train.SpawnCar( TrainCarModel.Engine );
			train.SpawnCar( TrainCarModel.Engine );
			train.SpawnCar( TrainCarModel.Engine );
			train.SpawnCar( TrainCarModel.Engine );

			// Spawn some facilities

			var spawnBounds = ((HexCircle) HexCoord.Zero).Expand( 16 );
			var random = new System.Random();

			var coordCount = spawnBounds.CoordinateCount;

			for ( var i = 0; i < 1; ++i )
			{
				var coord = spawnBounds.GetCoordinate( random.Next( 0, coordCount ) );

				FacilityManager.SpawnFacility( coord, FacilityType.Mine );
			}

			for ( var i = 0; i < 1; ++i )
			{
				var coord = spawnBounds.GetCoordinate( random.Next( 0, coordCount ) );

				FacilityManager.SpawnFacility( coord, FacilityType.PowerPlant );
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
