using System.Collections.Generic;
using Sandbox;

namespace Ziks.Trains
{
	public partial class Player : Sandbox.Player
	{
		public new PlayerController Controller
		{
			get => base.Controller as PlayerController;
			set => base.Controller = value;
		}

		public override void Respawn()
		{
			Controller = new PlayerController();
			Camera = new FirstPersonCamera();

			EnableAllCollisions = false;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = false;

			Position = Vector3.Up * 256f;

			base.Respawn();
		}
	}

	public partial class PlayerController : BasePlayerController
	{
		public float MinZ { get; set; } = 128f;
		public float MaxZ { get; set; } = 512f;

		private bool _dragging;

		private (HexCoord hexCoord, HexEdge edge) _dragStart;
		private (HexCoord hexCoord, HexEdge edge) _dragEnd;

		private readonly List<(HexCoord, HexEdge)> _tempPath = new ();

		public override void FrameSimulate()
		{
			EyeRot = Rotation.FromPitch( 75f );

			if ( !Host.IsClient ) return;

			var hexGrid = Game.Instance.HexGrid;

			var cursorPos = hexGrid.Trace( Input.Cursor );

			if ( !cursorPos.HasValue ) return;

			_dragEnd = hexGrid.GetEdge( cursorPos.Value );

			if ( Input.Pressed( InputButton.Attack1 ) )
			{
				_dragging = true;
				_dragStart = _dragEnd;
				return;
			}

			if ( !Input.Down( InputButton.Attack1 ) )
			{
				if ( _dragging )
				{
					// TODO: Draw track
				}

				_dragging = false;
				return;
			}

			var startWorldPos = hexGrid.GetWorldPosition( _dragStart.hexCoord, _dragStart.edge );

			_tempPath.Clear();

			if ( !HexGrid.GetShortestPath( _tempPath,
				_dragStart.hexCoord, _dragStart.edge,
				_dragEnd.hexCoord, _dragEnd.edge, false ) )
			{
				DebugOverlay.Line( startWorldPos, hexGrid.GetWorldPosition( _dragEnd.hexCoord, _dragEnd.edge ), Color.Red );
				return;
			}

			foreach ( var (hexCoord, hexEdge) in _tempPath )
			{
				var endWorldPos = hexGrid.GetWorldPosition( hexCoord, hexEdge );

				DebugOverlay.Line( startWorldPos, endWorldPos, Color.White );

				startWorldPos = endWorldPos;
			}
		}

		public override void Simulate()
		{
			var vel = (Vector3.Forward * Input.Forward) + (Vector3.Left * Input.Left);

			if ( Input.Down( InputButton.Jump ) )
			{
				vel += Vector3.Up * 1;
			}

			if ( Input.Down( InputButton.Duck ) )
			{
				vel -= Vector3.Up * 1;
			}

			vel = vel.Normal * 2000;

			vel.x *= Position.z / 256f;
			vel.y *= Position.z / 256f;

			if ( Input.Down( InputButton.Run ) )
				vel *= 4f;

			if ( Input.Down( InputButton.Walk ) )
				vel *= 0.25f;

			Velocity += vel * Time.Delta;

			if ( Velocity.LengthSquared > 0.01f )
			{
				Position += Velocity * Time.Delta;
			}

			Position = Position.WithZ( Position.z.Clamp( MinZ, MaxZ ) );

			Velocity = Velocity.Approach( 0, Velocity.Length * Time.Delta * 5.0f );

			WishVelocity = Velocity;
			GroundEntity = null;
			BaseVelocity = Vector3.Zero;

			SetTag( "noclip" );
		}
	}
}
