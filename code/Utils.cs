using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ziks.Trains
{
	public static class Utils
	{
		private static readonly byte[] MultiplyDeBruijnBitPosition = new byte[32]
		{
			0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
			8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
		};

		public static byte Log2( uint v )
		{
			v |= v >> 1; // first round down to one less than a power of 2 
			v |= v >> 2;
			v |= v >> 4;
			v |= v >> 8;
			v |= v >> 16;

			return MultiplyDeBruijnBitPosition[(v * 0x07C4ACDDU) >> 27];
		}

		public static float EaseTo( this float current, float goal, float factorPercent, float dt, float referenceFrameRate = 60f )
		{
			if ( float.IsPositiveInfinity( dt ) ) return goal;
			return current + (goal - current) * (1f - (float) Math.Pow( 1d - Math.Clamp( factorPercent, 0f, 1f ), dt * referenceFrameRate ));
		}
	}
}
