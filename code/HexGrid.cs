using System;
using Sandbox;

namespace Ziks.Trains
{
	public enum HexEdge : byte
	{
		Top,
		TopRight,
		BottomRight,
		Bottom,
		BottomLeft,
		TopLeft
	}

	public readonly struct HexCoord
	{
		public static HexCoord Zero => new HexCoord();

		public static HexCoord operator +( HexCoord a, HexCoord b )
		{
			return new(a.Row + b.Row, a.Col + b.Col);
		}

		public static HexCoord operator -( HexCoord a, HexCoord b )
		{
			return new(a.Row - b.Row, a.Col - b.Col);
		}

		public static implicit operator HexCoord( HexEdge edge )
		{
			switch ( edge )
			{
				case HexEdge.Top:
					return new HexCoord( 0, 1 );
				case HexEdge.TopRight:
					return new HexCoord( 1, 0 );
				case HexEdge.BottomRight:
					return new HexCoord( 1, -1 );
				case HexEdge.Bottom:
					return new HexCoord( 0, -1 );
				case HexEdge.BottomLeft:
					return new HexCoord( -1, 0 );
				case HexEdge.TopLeft:
					return new HexCoord( -1, 1 );
				default:
					throw new ArgumentOutOfRangeException( nameof( edge ) );
			}
		}

		public readonly int Row;
		public readonly int Col;

		public HexCoord( int row, int col )
		{
			this.Row = row;
			this.Col = col;
		}

		public HexCoord Neighbor( HexEdge edge )
		{
			return this + edge;
		}

		public override string ToString()
		{
			return $"({Row}, {Col}, {Row - Col})";
		}
	}

	public partial class HexGrid : Entity
	{
		public const float PiOver6 = MathF.PI / 6f;
		public const float HalfRoot3 = 0.8660254037844386467637231707529f;
		public const float UnitScale = 128f / 3f;

		private static readonly Vector3 Axis0 = new Vector3( MathF.Cos( PiOver6 ), MathF.Sin( PiOver6 ) ) * UnitScale;
		private static readonly Vector3 Axis1 = new Vector3( 0f, 1f ) * UnitScale;

		private static readonly Vector3[] Directions;

		static HexGrid()
		{
			Directions = new Vector3[6];

			for ( var i = 0; i < 6; ++i )
			{
				var edge = (HexEdge)i;
				var dir = (HexCoord) edge;

				Directions[i] = (dir.Row * Axis0 + dir.Col * Axis1).Normal;
			}
		}

		public HexGrid()
		{
			EnableDrawing = false;
			Transmit = TransmitType.Always;
		}

		public HexCoord GetHexCoord( Vector3 worldPos )
		{
			Vector2 localPos = GetLocalPosition( worldPos );

			var col = (int)MathF.Floor( localPos.x / Axis0.x );
			var row = (int)MathF.Floor( localPos.y / Axis1.y - 0.5f * (localPos.x / Axis0.x) );

			// There's probably a smarter way to do this...
			// Let's find the 4 closest cells, and compare which is closest

			Vector2 a = GetLocalPosition( new HexCoord( col, row ) );
			Vector2 b = GetLocalPosition( new HexCoord( col + 1, row ) );
			Vector2 c = GetLocalPosition( new HexCoord( col, row + 1 ) );
			Vector2 d = GetLocalPosition( new HexCoord( col + 1, row + 1 ) );

			var distA = (localPos - a).LengthSquared;
			var distB = (localPos - b).LengthSquared;
			var distC = (localPos - c).LengthSquared;
			var distD = (localPos - d).LengthSquared;

			float leftDist, rightDist;
			int leftRow, rightRow;

			if ( distA < distC )
			{
				leftDist = distA;
				leftRow = row;
			}
			else
			{
				leftDist = distC;
				leftRow = row + 1;
			}

			if ( distB < distD )
			{
				rightDist = distB;
				rightRow = row;
			}
			else
			{
				rightDist = distD;
				rightRow = row + 1;
			}

			return leftDist < rightDist ? new HexCoord( col, leftRow ) : new HexCoord( col + 1, rightRow );
		}

		public (HexCoord hexCoord, HexEdge edge) GetEdge( Vector3 worldPos )
		{
			var hexCoord = GetHexCoord( worldPos );
			var localPos = GetLocalPosition( worldPos );
			var centerPos = GetLocalPosition( hexCoord );
			var diff = centerPos - localPos;

			var angle = -MathF.Atan2( diff.y, diff.x );
			var rawIndex = (int)MathF.Round( angle * 3f / MathF.PI + 4.5f );

			return (hexCoord, (HexEdge)(rawIndex % 6));
		}

		public Vector3? Trace( Ray ray, double maxDistance = double.PositiveInfinity )
		{
			var plane = new Plane( Position, Rotation * Vector3.Up );
			return plane.Trace( ray, true, maxDistance );
		}

		public Vector3 GetWorldPosition( HexCoord gridPos )
		{
			return Position + Rotation * GetLocalPosition( gridPos ) * Scale;
		}

		public Vector3 GetWorldPosition( HexCoord gridPos, HexEdge edge )
		{
			return Position + Rotation * GetLocalPosition( gridPos, edge ) * Scale;
		}

		public Vector3 GetWorldDirection( HexEdge edge )
		{
			return Rotation * GetLocalDirection( edge );
		}

		public Vector3 GetLocalPosition( HexCoord gridPos )
		{
			return gridPos.Row * Axis0 + gridPos.Col * Axis1;
		}

		public Vector3 GetLocalPosition( HexCoord gridPos, HexEdge edge )
		{
			var nextPos = gridPos + edge;

			var a = gridPos.Row * Axis0 + gridPos.Col * Axis1;
			var b = nextPos.Row * Axis0 + nextPos.Col * Axis1;

			return a * 0.5f + b * 0.5f;
		}

		public Vector3 GetLocalPosition( Vector3 worldPos )
		{
			return Rotation.Inverse * (worldPos - Position) / Scale;
		}

		public Vector3 GetLocalDirection( HexEdge edge )
		{
			return Directions[(int)edge];
		}
	}
}
