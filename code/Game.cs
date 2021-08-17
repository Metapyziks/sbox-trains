using Sandbox;
using Ziks.Trains.UI;

namespace Ziks.Trains
{
    public partial class Game : Sandbox.Game
    {
		public static Game Instance { get; private set; }

		[Net]
		public HexGrid HexGrid { get; set; }

		[Net]
		public Hud Hud { get; set; }

		public Game()
		{
			if ( IsServer )
			{
				HexGrid = new HexGrid();
				Hud = new Hud();
			}

			Instance = this;
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
