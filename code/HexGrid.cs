using System;
using System.Collections.Generic;
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

	public static class HexEdgeExtensions
	{
		public static HexEdge Opposite( this HexEdge edge )
		{
			return (HexEdge)(((int)edge + 3) % 6);
		}
	}

	public readonly struct HexCoord : IEquatable<HexCoord>
	{
		public static HexCoord Zero => new HexCoord();

		public static HexCoord operator +( HexCoord a, HexCoord b )
		{
			return new(a.X + b.X, a.Y + b.Y);
		}

		public static HexCoord operator -( HexCoord a, HexCoord b )
		{
			return new(a.X - b.X, a.Y - b.Y);
		}

		public static bool operator ==( HexCoord a, HexCoord b )
		{
			return a.X == b.X && a.Y == b.Y;
		}

		public static bool operator !=( HexCoord a, HexCoord b )
		{
			return a.X != b.X || a.Y != b.Y;
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

		public readonly int X;
		public readonly int Y;
		public int Z => X - Y;

		public int Length => Math.Min(
			Math.Abs( X ) + Math.Abs( Y ),
			Math.Min(
				Math.Abs( X ) + Math.Abs( Z ),
				Math.Abs( Y ) + Math.Abs( Z ) ) );

		public HexCoord( int x, int y )
		{
			X = x;
			Y = y;
		}

		public HexCoord Neighbor( HexEdge edge )
		{
			return this + edge;
		}

		public override string ToString()
		{
			return $"({X}, {Y}, {Z})";
		}

		public bool Equals(HexCoord other)
		{
			return X == other.X && Y == other.Y;
		}

		public override bool Equals(object obj)
		{
			return obj is HexCoord other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y);
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

				Directions[i] = (dir.X * Axis0 + dir.Y * Axis1).Normal;
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

		public static Vector3 GetLocalPosition( HexCoord gridPos )
		{
			return gridPos.X * Axis0 + gridPos.Y * Axis1;
		}

		public static Vector3 GetLocalPosition( HexCoord gridPos, HexEdge edge )
		{
			var nextPos = gridPos + edge;

			var a = gridPos.X * Axis0 + gridPos.Y * Axis1;
			var b = nextPos.X * Axis0 + nextPos.Y * Axis1;

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

		private static float GetLocalDistanceSquared( HexCoord fromCoord, Vector2 localPos )
		{
			return ((Vector2)GetLocalPosition( fromCoord ) - localPos).LengthSquared;
		}

		private static float GetLocalDistanceSquared( HexCoord fromCoord, HexEdge fromEdge, Vector2 localPos )
		{
			return ((Vector2)GetLocalPosition( fromCoord, fromEdge ) - localPos).LengthSquared;
		}

		private static bool MoveNextShortestPath( ref HexCoord fromCoord, ref HexEdge fromEdge,
			HexCoord toCoord, HexEdge toEdge, Vector2 toLocalPos )
		{
			fromCoord += fromEdge;

			if ( fromCoord == toCoord && fromEdge == toEdge.Opposite() ) return true;

			var edgeA = fromEdge;
			var edgeB = (HexEdge)(((int)fromEdge + 1) % 6);
			var edgeC = (HexEdge)(((int)fromEdge + 5) % 6);

			var distA = GetLocalDistanceSquared( fromCoord, edgeA, toLocalPos );
			var distB = GetLocalDistanceSquared( fromCoord, edgeB, toLocalPos );
			var distC = GetLocalDistanceSquared( fromCoord, edgeC, toLocalPos );

			if ( distA <= distB && distA <= distC )
			{
				fromEdge = edgeA;
				return false;
			}

			if ( distB <= distA && distB <= distC )
			{
				fromEdge = edgeB;
				return false;
			}

			fromEdge = edgeC;
			return false;
		}
		
		public static bool GetShortestPath( List<(HexCoord, HexEdge)> outPath, HexCoord fromCoord, HexEdge fromEdge,
			HexCoord toCoord, HexEdge toEdge, bool allowSharpTurns )
		{
			if ( allowSharpTurns )
			{
				throw new NotImplementedException();
			}

			var fromLocalPos = GetLocalPosition( fromCoord, fromEdge );
			var toLocalPos = GetLocalPosition( toCoord, toEdge );

			if ( GetLocalDistanceSquared( fromCoord, toLocalPos ) < GetLocalDistanceSquared( fromCoord + fromEdge, toLocalPos ) )
			{
				fromCoord += fromEdge;
				fromEdge = fromEdge.Opposite();
			}

			if ( GetLocalDistanceSquared( toCoord, fromLocalPos ) < GetLocalDistanceSquared( toCoord + toEdge, fromLocalPos ) )
			{
				toCoord += toEdge;
				toEdge = toEdge.Opposite();
			}

			var maxTiles = (fromCoord - toCoord).Length + 2;
			var insertIndex = outPath.Count;

			do
			{
				outPath.Insert( insertIndex++, (fromCoord, fromEdge) );
				if ( MoveNextShortestPath( ref fromCoord, ref fromEdge, toCoord, toEdge, toLocalPos ) ) return true;
				fromLocalPos = GetLocalPosition( fromCoord, fromEdge );

				outPath.Insert( insertIndex, (toCoord + toEdge, toEdge.Opposite()) );
				if ( MoveNextShortestPath( ref toCoord, ref toEdge, fromCoord, fromEdge, fromLocalPos ) ) return true;
				toLocalPos = GetLocalPosition( toCoord, toEdge );
			} while ( maxTiles-- > 0 );

			return false;
		}
	}
}
