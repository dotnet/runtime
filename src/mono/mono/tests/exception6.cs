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
				return 1;

			try {
				long a = Int64.MaxValue;
				long t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 1;

			ocount = 0;
			try {
				ulong a = UInt64.MaxValue - 1;
				ulong t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 2;

			try {
				ulong a = UInt64.MaxValue;
				ulong t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 1;

			ocount = 0;
			try {
				long a = Int64.MinValue + 1;
				long t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 1;

			ocount = 0;
			try {
				long a = Int64.MinValue;
				long t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 1;

			ocount = 0;
			try {
				ulong a = UInt64.MinValue + 1;
				ulong t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 1;

			ocount = 0;
			try {
				ulong a = UInt64.MinValue;
				ulong t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 1;

			ocount = 0;
			try {
				int a = Int32.MinValue + 1;
				int t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 1;

			ocount = 0;
			try {
				int a = Int32.MinValue;
				int t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 1;

			ocount = 0;
			try {
				uint a = 1;
				uint t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 1;

			ocount = 0;
			try {
				uint a = 0;
				uint t = a--;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 1;

			ocount = 0;
			try {
				sbyte a = 126;
				sbyte t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 1;

			ocount = 0;
			try {
				sbyte a = 127;
				sbyte t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 1;

			ocount = 0;
			try {
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 1;

			ocount = 0;
			try {
				int a = 1 << 29;
				int t = a*2;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 1;

			ocount = 0;
			try {
				int a = 1 << 30;
				int t = a*2;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 1;

			/*
			ocount = 0;
			try {
				uint a = 0xffffffff;
				uint t = a*2;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 1;
			*/
		}
		
		return 0;
	}
	public static int Main () {
		return test();
	}
}


