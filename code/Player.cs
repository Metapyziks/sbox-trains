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

		private HexCoordEdge _dragStart;
		private HexCoordEdge _dragEnd;

		private readonly List<HexCoordEdge> _tempPath = new ();

		public override void FrameSimulate()
		{
			EyeRot = Rotation.FromYaw( 90f ) * Rotation.FromPitch( 75f );

			if ( !Host.IsClient ) return;

			var hexGrid = Game.Current.HexGrid;

			var cursorPos = hexGrid.Trace( Input.Cursor );

			if ( !cursorPos.HasValue ) return;

			const float height = 2f;
			const float width = 4f;

			var lastDragEnd = _dragEnd;
			_dragEnd = hexGrid.GetHexCoordEdge( cursorPos.Value );

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
					TrackManager.SpawnTrack( _dragStart, _dragEnd );
				}

				var norm = Rotation.FromYaw( 90f ) * hexGrid.GetWorldDirection( _dragEnd.Edge );

				DebugOverlay.Line(
					hexGrid.GetWorldPosition( _dragEnd ) - norm * width + Vector3.Up * height,
					hexGrid.GetWorldPosition( _dragEnd.Coord ) - norm * width + Vector3.Up * height, Color.White );

				DebugOverlay.Line(
					hexGrid.GetWorldPosition( _dragEnd ) + norm * width + Vector3.Up * height,
					hexGrid.GetWorldPosition( _dragEnd.Coord ) + norm * width + Vector3.Up * height, Color.White );

				_dragging = false;
				_validPath = false;
				return;
			}

			if ( !lastDragEnd.Equals( _dragEnd ) )
			{
				_tempPath.Clear();
				_validPath = HexGrid.GetShortestPath( _tempPath, _dragStart, _dragEnd, false ) && _tempPath.Count > 1;
			}

			if ( !_validPath )
			{
				DebugOverlay.Line(
					hexGrid.GetWorldPosition( _dragStart ) + Vector3.Up * height,
					hexGrid.GetWorldPosition( _dragEnd ) + Vector3.Up * height, Color.Red );
				return;
			}

			var prev = _tempPath.First();

			var prevWorldPos = hexGrid.GetWorldPosition( prev );
			var prevWorldNorm = Rotation.FromYaw( 90f ) * hexGrid.GetWorldDirection( prev.Edge );

			foreach ( var next in _tempPath )
			{
				var nextWorldPos = hexGrid.GetWorldPosition( next );
				var nextWorldNorm = Rotation.FromYaw( 90f ) * hexGrid.GetWorldDirection( next.Edge );

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
