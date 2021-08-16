using Sandbox;
using Sandbox.UI;

namespace Ziks.Trains.UI
{
	public partial class Hud : HudEntity<RootPanel>
	{
		public Hud()
		{
			if ( !IsClient ) return;

			RootPanel.StyleSheet.Load( "/ui/Hud.scss" );
			RootPanel.AddChild<CursorController>();
		}
	}
}
