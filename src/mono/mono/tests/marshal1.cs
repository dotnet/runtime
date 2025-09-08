using System;
using System.Runtime.InteropServices;

public class Test {

	public static int Main () {
		byte [] bytesrc = new byte [20];
		byte [] bytedest = new byte [20];

		IntPtr dest = Marshal.AllocHGlobal (1024);
		
		bytesrc [2] = 2;
		bytesrc [11] = 11;		
		Marshal.Copy (bytesrc, 2, dest, 10);

		if (Marshal.ReadByte (dest, 0) != 2)
			return 1;
		if (Marshal.ReadByte (dest, 9) != 11)
			return 1;

		Marshal.Copy (dest, bytedest, 2, 10);

		if (bytedest [2] != 2)
			return 1;
		if (bytedest [11] != 11)
			return 1;		

		return 0;
	}
}

