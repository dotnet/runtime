using System;

public class Ex {

	public static int test () {
		int ocount = 0;
		
		checked {

			ocount = 0;
			try {
				ulong a =  UInt64.MaxValue - 1;
				ulong t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 1;

			ocount = 0;
			try {
				ulong a =  UInt64.MaxValue;
				ulong t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 2;

			ocount = 0;
			try {
				long a = Int64.MaxValue - 1;
				long t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 3;

			try {
				long a = Int64.MaxValue;
				long t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 4;

			ocount = 0;
			try {
				ulong a = UInt64.MaxValue - 1;
				ulong t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 5;

			try {
				ulong a = UInt64.MaxValue;
				ulong t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 6;

			ocount = 0;
			try {
				long a = Int64.MinValue + 1;
				long t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 7;

			ocount = 0;
			try {
				long a = Int64.MinValue;
				long t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 8;

			ocount = 0;
			try {
				ulong a = UInt64.MinValue + 1;
				ulong t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 9;

			ocount = 0;
			try {
				ulong a = UInt64.MinValue;
				ulong t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 10;

			ocount = 0;
			try {
				int a = Int32.MinValue + 1;
				int t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 11;

			ocount = 0;
			try {
				int a = Int32.MinValue;
				int t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 12;

			ocount = 0;
			try {
				uint a = 1;
				uint t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 13;

			ocount = 0;
			try {
				uint a = 0;
				uint t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 14;

			ocount = 0;
			try {
				sbyte a = 126;
				sbyte t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 15;

			ocount = 0;
			try {
				sbyte a = 127;
				sbyte t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 16;

			ocount = 0;
			try {
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 17;

			ocount = 0;
			try {
				int a = 1 << 29;
				int t = a*2;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 18;

			ocount = 0;
			try {
				int a = 1 << 30;
				int t = a*2;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 19;

			ocount = 0;
			try {
				ulong a = 0xffffffffff;
				ulong t = a*0x0ffffff;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 20;

			ocount = 0;
			try {
				ulong a = 0xffffffffff;
				ulong t = a*0x0fffffff;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 21;
		}
		
		return 0;
	}
	public static int Main () {
		return test();
	}
}


