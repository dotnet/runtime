using System;
using System.Reflection;

/*
 * Regression tests for the mono JIT.
 *
 * Each test needs to be of the form:
 *
 * static int test_<result>_<name> ();
 *
 * where <result> is an integer (the value that needs to be returned by
 * the method to make it pass.
 * <name> is a user-displayed name used to identify the test.
 *
 * The tests can be driven in two ways:
 * *) running the program directly: Main() uses reflection to find and invoke
 * 	the test methods (this is useful mostly to check that the tests are correct)
 * *) with the --regression switch of the jit (this is the preferred way since
 * 	all the tests will be run with optimizations on and off)
 *
 * The reflection logic could be moved to a .dll since we need at least another
 * regression test file written in IL code to have better control on how
 * the IL code looks.
 */

class Tests {

	static int Main () {
		return TestDriver.RunTests (typeof (Tests));
	}

	static int test_0_catch () {
		Exception x = new Exception ();
		
		try {
			throw x;
		} catch (Exception e) {
			if (e == x)
				return 0;
		}
		return 1;
	}

	static int test_0_finally_without_exc () {
		int x;
		
		try {
			x = 1;
		} catch (Exception e) {
			x = 2;
		} finally {
			x = 0;
		}
		
		return x;
	}

	static int test_0_finally () {
		int x = 1;
		
		try {
			throw new Exception ();
		} catch (Exception e) {
			x = 2;
		} finally {
			x = 0;
		}
		return x;
	}

	static int test_0_byte_cast () {
		int a;
		long l;
		byte b = 0;
		bool failed;

		try {
			a = 255;
			failed = false;
			checked {
				b = (byte)a;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 1;
		if (b != 255)
			return -1;

		try {
			a = 0;
			failed = false;
			checked {
				b = (byte)a;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 2;
		if (b != 0)
			return -2;

		try {
			a = 256;
			failed = true;
			checked {
				b = (byte)a;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 3;
		if (b != 0)
			return -3;

		try {
			a = -1;
			failed = true;
			checked {
				b = (byte)a;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 4;
		if (b != 0)
			return -4;

		try {
			double d = 0;
			failed = false;
			checked {
				b = (byte)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 5;
		if (b != 0)
			return -5;
		
		try {
			double d = -1;
			failed = true;
			checked {
				b = (byte)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 6;
		if (b != 0)
			return -6;

		try {
			double d = 255;
			failed = false;
			checked {
				b = (byte)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 7;
		// FIXME: we have a bug here
		//if (b != 255)
		//	return -7;

		try {
			double d = 256;
			failed = true;
			checked {
				b = (byte)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 8;

		try {
			l = 255;
			failed = false;
			checked {
				b = (byte)l;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 9;
		//Console.WriteLine ((int)b);
		if (b != 255)
			return -9;

		try {
			l = 0;
			failed = false;
			checked {
				b = (byte)l;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 10;
		if (b != 0)
			return -10;

		try {
			l = 256;
			failed = true;
			checked {
				b = (byte)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 11;
		if (b != 0)
			return -11;

		try {
			l = -1;
			failed = true;
			checked {
				b = (byte)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 12;
		if (b != 0)
			return -12;
		
		return 0;
	}
	
	static int test_0_sbyte_cast () {
		int a;
		long l;
		sbyte b;
		bool failed;

		try {
			a = 255;
			failed = true;
			checked {
				b = (sbyte)a;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 1;

		try {
			a = 0;
			failed = false;
			checked {
				b = (sbyte)a;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 2;

		try {
			a = 256;
			failed = true;
			checked {
				b = (sbyte)a;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 3;

		try {
			a = -129;
			failed = true;
			checked {
				b = (sbyte)a;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 4;

		try {
			a = -1;
			failed = false;
			checked {
				b = (sbyte)a;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 5;

		try {
			a = -128;
			failed = false;
			checked {
				b = (sbyte)a;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 6;

		try {
			a = 127;
			failed = false;
			checked {
				b = (sbyte)a;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 7;

		try {
			a = 128;
			failed = true;
			checked {
				b = (sbyte)a;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 8;

		try {
			double d = 127;
			failed = false;
			checked {
				b = (sbyte)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 9;

		try {
			double d = -128;
			failed = false;
			checked {
				b = (sbyte)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 10;

		try {
			double d = 128;
			failed = true;
			checked {
				b = (sbyte)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 11;

		try {
			double d = -129;
			failed = true;
			checked {
				b = (sbyte)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 12;

		try {
			l = 255;
			failed = true;
			checked {
				b = (sbyte)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 13;

		try {
			l = 0;
			failed = false;
			checked {
				b = (sbyte)l;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 14;

		try {
			l = 256;
			failed = true;
			checked {
				b = (sbyte)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 15;

		try {
			l = -129;
			failed = true;
			checked {
				b = (sbyte)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 16;

		try {
			l = -1;
			failed = false;
			checked {
				b = (sbyte)l;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 17;

		try {
			l = -128;
			failed = false;
			checked {
				b = (sbyte)l;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 18;

		try {
			l = 127;
			failed = false;
			checked {
				b = (sbyte)l;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 19;

		try {
			l = 128;
			failed = true;
			checked {
				b = (sbyte)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 20;

		return 0;
	}

	static int test_0_ushort_cast () {
		int a;
		long l;
		ushort b;
		bool failed;

		try {
			a = System.UInt16.MaxValue;
			failed = false;
			checked {
				b = (ushort)a;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 1;

		try {
			a = 0;
			failed = false;
			checked {
				b = (ushort)a;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 2;

		try {
			a = System.UInt16.MaxValue + 1;
			failed = true;
			checked {
				b = (ushort)a;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 3;

		try {
			a = -1;
			failed = true;
			checked {
				b = (ushort)a;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 4;

		try {
			double d = 0;
			failed = false;
			checked {
				b = (ushort)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 5;

		try {
			double d = System.UInt16.MaxValue;
			failed = false;
			checked {
				b = (ushort)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 6;

		try {
			double d = -1;
			failed = true;
			checked {
				b = (ushort)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 7;

		try {
			double d = System.UInt16.MaxValue + 1.0;
			failed = true;
			checked {
				b = (ushort)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 8;

		try {
			l = System.UInt16.MaxValue;
			failed = false;
			checked {
				b = (ushort)l;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 9;

		try {
			l = 0;
			failed = false;
			checked {
				b = (ushort)l;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 10;

		try {
			l = System.UInt16.MaxValue + 1;
			failed = true;
			checked {
				b = (ushort)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 11;

		try {
			l = -1;
			failed = true;
			checked {
				b = (ushort)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 12;

		return 0;
	}
	
	static int test_0_short_cast () {
		int a;
		long l;
		short b;
		bool failed;

		try {
			a = System.UInt16.MaxValue;
			failed = true;
			checked {
				b = (short)a;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 1;

		try {
			a = 0;
			failed = false;
			checked {
				b = (short)a;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 2;

		try {
			a = System.Int16.MaxValue + 1;
			failed = true;
			checked {
				b = (short)a;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 3;

		try {
			a = System.Int16.MinValue - 1;
			failed = true;
			checked {
				b = (short)a;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 4;

		try {
			a = -1;
			failed = false;
			checked {
				b = (short)a;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 5;

		try {
			a = System.Int16.MinValue;
			failed = false;
			checked {
				b = (short)a;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 6;

		try {
			a = System.Int16.MaxValue;
			failed = false;
			checked {
				b = (short)a;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 7;

		try {
			a = System.Int16.MaxValue + 1;
			failed = true;
			checked {
				b = (short)a;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 8;

		try {
			double d = System.Int16.MaxValue;
			failed = false;
			checked {
				b = (short)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 9;
		
		try {
			double d = System.Int16.MinValue;
			failed = false;
			checked {
				b = (short)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 10;
		
		try {
			double d = System.Int16.MaxValue + 1.0;
			failed = true;
			checked {
				b = (short)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 11;

		try {
			double d = System.Int16.MinValue - 1.0;
			failed = true;
			checked {
				b = (short)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 12;

		try {
			l = System.Int16.MaxValue + 1;
			failed = true;
			checked {
				b = (short)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 13;

		try {
			l = System.Int16.MaxValue;
			failed = false;
			checked {
				b = (short)l;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 14;

		try {
			l = System.Int16.MinValue - 1;
			failed = true;
			checked {
				b = (short)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 15;

		
		try {
			l = System.Int16.MinValue;
			failed = false;
			checked {
				b = (short)l;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 16;

		return 0;
	}
	
	static int test_0_int_cast () {
		int a;
		long l;
		bool failed;

		try {
			double d = System.Int32.MaxValue + 1.0;
			failed = true;
			checked {
				a = (int)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 1;

		try {
			double d = System.Int32.MaxValue;
			failed = false;
			checked {
				a = (int)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 2;
		

		try {
			double d = System.Int32.MinValue;
			failed = false;			
			checked {
				a = (int)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 3;


		try {
			double d =  System.Int32.MinValue - 1.0;
			failed = true;
			checked {
				a = (int)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 4;

		try {
			l = System.Int32.MaxValue + (long)1;
			failed = true;
			checked {
				a = (int)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 5;

		try {
			l = System.Int32.MaxValue;
			failed = false;
			checked {
				a = (int)l;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 6;
		

		try {
			l = System.Int32.MinValue;
			failed = false;			
			checked {
				a = (int)l;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 7;


		try {
			l =  System.Int32.MinValue - (long)1;
			failed = true;
			checked {
				a = (int)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 8;

		return 0;
	}

	static int test_0_uint_cast () {
		uint a;
		long l;
		bool failed;

		try {
			double d =  System.UInt32.MaxValue;
			failed = false;
			checked {
				a = (uint)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 1;

		try {
			double d = System.UInt32.MaxValue + 1.0;
			failed = true;
			checked {
				a = (uint)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 2;

		try {
			double d = System.UInt32.MinValue;
			failed = false;
			checked {
				a = (uint)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 3;

		try {
			double d = System.UInt32.MinValue - 1.0;
			failed = true;
			checked {
				a = (uint)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 4;
		
		try {
			l =  System.UInt32.MaxValue;
			failed = false;
			checked {
				a = (uint)l;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 5;

		try {
			l = System.UInt32.MaxValue + (long)1;
			failed = true;
			checked {
				a = (uint)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 6;

		try {
			l = System.UInt32.MinValue;
			failed = false;
			checked {
				a = (uint)l;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 7;

		try {
			l = System.UInt32.MinValue - (long)1;
			failed = true;
			checked {
				a = (uint)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 8;
		
		return 0;
	}
	
	static int test_0_long_cast () {
		long a;
		bool failed;

		try {
			double d = System.Int64.MaxValue - 512.0;
			failed = true;
			checked {
				a = (long)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 1;

		try {
			double d = System.Int64.MaxValue - 513.0;
			failed = false;
			checked {
				a = (long)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 2;
		

		try {
			double d = System.Int64.MinValue - 1024.0;
			failed = false;			
			checked {
				a = (long)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 3;

		try {
			double d = System.Int64.MinValue - 1025.0;
			failed = true;
			checked {
				a = (long)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 4;

		return 0;
	}

	static int test_0_ulong_cast () {
		ulong a;
		bool failed;

		try {
			double d = System.UInt64.MaxValue - 1024.0;
			failed = true;
			checked {
				a = (ulong)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 1;

		try {
			double d = System.UInt64.MaxValue - 1025.0;
			failed = false;
			checked {
				a = (ulong)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 2;
		

		try {
			double d = 0;
			failed = false;			
			checked {
				a = (ulong)d;
			}
		} catch (OverflowException) {
			failed = true;
		}
		if (failed)
			return 3;

		try {
			double d = -1;
			failed = true;
			checked {
				a = (ulong)d;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 4;

		return 0;
	}

	static int test_0_simple_double_casts () {

		double d = 0xffffffff;

		if ((uint)d != 4294967295)
			return 1;

		d = 0xffffffffffffffff;

		if ((ulong)d != 0)
			return 2;

		if ((ushort)d != 0)
			return 3;
			
		if ((byte)d != 0)
			return 4;
			
		d = 0xffff;

		if ((ushort)d != 0xffff)
			return 5;
		
		if ((byte)d != 0xff)
			return 6;
			
		return 0;
	}
	
	static int test_0_div_zero () {
		int d = 1;
		int q = 0;
		int val;
		bool failed;

		try {
			failed = true;
			val = d / q;
		} catch (DivideByZeroException) {
			failed = false;
		}
		if (failed)
			return 1;

		try {
			failed = true;
			val = d % q;
		} catch (DivideByZeroException) {
			failed = false;
		}
		if (failed)
			return 2;

		return 0;
	}

	static int test_0_udiv_zero () {
		uint d = 1;
		uint q = 0;
		uint val;
		bool failed;

		try {
			failed = true;
			val = d / q;
		} catch (DivideByZeroException) {
			failed = false;
		}
		if (failed)
			return 1;

		try {
			failed = true;
			val = d % q;
		} catch (DivideByZeroException) {
			failed = false;
		}
		if (failed)
			return 2;

		return 0;
	}

	static int test_0_long_div_zero () {
		long d = 1;
		long q = 0;
		long val;
		bool failed;

		try {
			failed = true;
			val = d / q;
		} catch (DivideByZeroException) {
			failed = false;
		}
		if (failed)
			return 1;

		try {
			failed = true;
			val = d % q;
		} catch (DivideByZeroException) {
			failed = false;
		}
		if (failed)
			return 2;

		return 0;
	}

	static int test_0_ulong_div_zero () {
		ulong d = 1;
		ulong q = 0;
		ulong val;
		bool failed;

		try {
			failed = true;
			val = d / q;
		} catch (DivideByZeroException) {
			failed = false;
		}
		if (failed)
			return 1;

		try {
			failed = true;
			val = d % q;
		} catch (DivideByZeroException) {
			failed = false;
		}
		if (failed)
			return 2;

		return 0;
	}

	static int test_0_float_div_zero () {
		double d = 1;
		double q = 0;
		double val;
		bool failed;

		try {
			failed = false;
			val = d / q;
		} catch (DivideByZeroException) {
			failed = true;
		}
		if (failed)
			return 1;

		try {
			failed = false;
			val = d % q;
		} catch (DivideByZeroException) {
			failed = true;
		}
		if (failed)
			return 2;

		return 0;
	}

}
