// DESTest.cs was failing with rev 38316...
// but the DES code was changed after the fix
// so the original code lives here forever ;-)

using System;

class Program {

	public const int keySizeByte = 8;

	internal static readonly ulong[] weakKeys = {
		0x0101010101010101, /* 0000000 0000000 */
		0xFEFEFEFEFEFEFEFE, /* FFFFFFF FFFFFFF */
		0x1F1F1F1F0E0E0E0E, /* 0000000 FFFFFFF */
		0xE0E0E0E0F1F1F1F1  /* FFFFFFF 0000000 */
	};

	internal static ulong PackKey (byte[] key) 
	{
		byte[] paritySetKey = new byte [keySizeByte];
		// adapted from bouncycastle - see bouncycastle.txt
		for (int i=0; i < key.Length; i++) {
			byte b = key [i];
			paritySetKey [i] = (byte)((b & 0xfe) |
				((((b >> 1) ^ (b >> 2) ^ (b >> 3) ^ (b >> 4) ^
				(b >> 5) ^ (b >> 6) ^ (b >> 7)) ^ 0x01) & 0x01));
		}

		ulong res = 0;
		for (int i = 0, sh = 64; (sh = sh - 8) >= 0; i++) {
			res |= (ulong) paritySetKey [i] << sh;
		}
		return res;
	}

	static int Main ()
	{
		byte[] wk2p = { 0x1F, 0x1F, 0x1F, 0x1F, 0x0E, 0x0E, 0x0E, 0x0E };
		ulong lk = PackKey (wk2p);
		foreach (ulong wk in weakKeys) {
			if (lk == wk)
				return 0;
		}
		return 1;
	}
}
