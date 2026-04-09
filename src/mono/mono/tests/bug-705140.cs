using System;


namespace BufferTest
{
	class Test
	{
		const byte TotalLength = 32;

		public static byte Expected (byte dest, byte src, int len, byte i)
		{
			if (i >= dest && i < dest + len)
				return (byte) (src + (i - dest));
			else
				return i;
		}

		public static bool TestMove (byte dest, byte src, int len)
		{
			byte[] array = new byte [TotalLength];
			for (byte i = 0; i < TotalLength; ++i)
				array [i] = i;
			Buffer.BlockCopy (array, src, array, dest, len);
			for (byte i = 0; i < TotalLength; ++i)
			{
				if (array [i] != Expected (dest, src, len, i)) {
					Console.WriteLine ("when copying " + len + " from " + src + " to " + dest + ": expected ");
					for (byte j = 0; j < TotalLength; ++j)
						Console.Write ("" + Expected (dest, src, len, j) + " ");
					Console.WriteLine ();
					Console.WriteLine ("got");
					for (byte j = 0; j < TotalLength; ++j)
						Console.Write ("" + array [j] + " ");
					Console.WriteLine ();
					return false;
				}
			}
			return true;
		}

		public static int Main (string[] args)
		{
			bool failed = false;
			for (byte i = 0; i < TotalLength; ++i) {
				for (byte j = 0; j < TotalLength; ++j) {
					byte max = (byte) (TotalLength - Math.Max (i, j));
					for (byte l = 0; l < max; ++l) {
						if (!TestMove (i, j, l))
							failed = true;
					}
				}
			}

			return failed ? 1 : 0;
		}
	}
}
