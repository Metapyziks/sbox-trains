using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Ziks.Trains.Track;

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
		private bool _validPath;

		private (HexCoord hexCoord, HexEdge edge) _dragStart;
		private (HexCoord hexCoord, HexEdge edge) _dragEnd;

		private readonly List<(HexCoord coord, HexEdge edge)> _tempPath = new ();

		public override void FrameSimulate()
		{
			EyeRot = Rotation.FromYaw( 90f ) * Rotation.FromPitch( 75f );

			if ( !Host.IsClient ) return;

			var hexGrid = Game.Current.HexGrid;

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
				if ( _dragging && _validPath )
				{
					TrackManager.SpawnTrack( _dragStart.hexCoord, _dragStart.edge,
						_dragEnd.hexCoord, _dragEnd.edge );
				}

				_dragging = false;
				_validPath = false;
				return;
			}

			const float height = 2f;
			const float width = 4f;

			_tempPath.Clear();

			if ( !HexGrid.GetShortestPath( _tempPath,
				_dragStart.hexCoord, _dragStart.edge,
				_dragEnd.hexCoord, _dragEnd.edge, false ) )
			{
				_validPath = false;
				DebugOverlay.Line( hexGrid.GetWorldPosition( _dragStart.hexCoord, _dragStart.edge ) + Vector3.Up * height,
					hexGrid.GetWorldPosition( _dragEnd.hexCoord, _dragEnd.edge ) + Vector3.Up * height, Color.Red );
				return;
			}

			_validPath = _tempPath.Count > 1;

			if ( !_validPath ) return;

			var prev = _tempPath.First();

			var prevWorldPos = hexGrid.GetWorldPosition( prev.coord, prev.edge );
			var prevWorldNorm = Rotation.FromYaw( 90f ) * hexGrid.GetWorldDirection( prev.edge );

			foreach ( var next in _tempPath )
			{
				var nextWorldPos = hexGrid.GetWorldPosition( next.coord, next.edge );
				var nextWorldNorm = Rotation.FromYaw( 90f ) * hexGrid.GetWorldDirection( next.edge );

				DebugOverlay.Line( prevWorldPos - prevWorldNorm * width + Vector3.Up * height,
					nextWorldPos - nextWorldNorm * width + Vector3.Up * height, Color.White );
				DebugOverlay.Line( prevWorldPos + prevWorldNorm * width + Vector3.Up * height,
					nextWorldPos + nextWorldNorm * width + Vector3.Up * height, Color.White );

				prevWorldPos = nextWorldPos;
				prevWorldNorm = nextWorldNorm;
			}
		}

		public override void Simulate()
		{
			var vel = (Vector3.Left * Input.Forward) + (Vector3.Backward * Input.Left);

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
