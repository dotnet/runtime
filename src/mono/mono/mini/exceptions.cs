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

	static int test_0_nested_finally () {
		int a;

		try {
			a = 1;
		} finally {
			try {
				a = 2;
			} finally {
				a = 0;
			}
		}
		return a;
	}		

	static int test_0_byte_cast () {
		int a;
		long l;
		ulong ul;
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
		if (b != 255)
			return -7;

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
		if (b != 255)
			return -8;

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

		try {
			ul = 256;
			failed = true;
			checked {
				b = (byte)ul;
			}
		}
		catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 13;
		if (b != 0)
			return -13;

		return 0;
	}
	
	static int test_0_sbyte_cast () {
		int a;
		long l;
		sbyte b = 0;
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
		if (b != 0)
			return -1;

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
		if (b != 0)
			return -2;

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
		if (b != 0)
			return -3;

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
		if (b != 0)
			return -4;

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
		if (b != -1)
			return -5;

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
		if (b != -128)
			return -6;

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
		if (b != 127)
			return -7;

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
		if (b != 127)
			return -8;

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
		if (b != 127)
			return -9;

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
		if (b != -128)
			return -10;

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
		if (b != -128)
			return -11;

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
		if (b != -128)
			return -12;

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
		if (b != -128)
			return -13;

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
		if (b != 0)
			return -14;

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
		if (b != 0)
			return -15;

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
		if (b != 0)
			return -16;

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
		if (b != -1)
			return -17;

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
		if (b != -128)
			return -18;

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
		if (b != 127)
			return -19;

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
		if (b != 127)
			return -19;

		return 0;
	}

	static int test_0_ushort_cast () {
		int a;
		long l;
		ulong ul;
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

		try {
			ul = 0xfffff;
			failed = true;
			checked {
				b = (ushort)ul;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 13;

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

		try {
			uint ui = System.UInt32.MaxValue;
			failed = true;
			checked {
				a = (int)ui;
			}
		}
		catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 9;

		{
			int i; 
			float f = 1.1f;
			checked {
				i = (int) f;
			}
		}

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

		try {
			int i = -1;
			failed = true;
			checked {
				a = (uint)i;
			}
		}
		catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 9;

		{
			uint i; 
			float f = 1.1f;
			checked {
				i = (uint) f;
			}
		}
		
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

		{
			long i; 
			float f = 1.1f;
			checked {
				i = (long) f;
			}
		}

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

		{
			ulong i; 
			float f = 1.1f;
			checked {
				i = (ulong) f;
			}
		}

		try {
			int i = -1;
			failed = true;
			checked {
				a = (ulong)i;
			}
		}
		catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 5;

		try {
			int i = Int32.MinValue;
			failed = true;
			checked {
				a = (ulong)i;
			}
		}
		catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 6;

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

		try {
			failed = true;
			q = -1;
			d = Int32.MinValue;
			val = d / q;
		} catch (DivideByZeroException) {
			/* wrong exception */
		} catch (ArithmeticException) {
			failed = false;
		}
		if (failed)
			return 3;

		try {
			failed = true;
			q = -1;
			d = Int32.MinValue;
			val = d % q;
		} catch (DivideByZeroException) {
			/* wrong exception */
		} catch (ArithmeticException) {
			failed = false;
		}
		if (failed)
			return 4;

		return 0;
	}

	static int return_55 () {
		return 55;
	}

	static int test_0_cfold_div_zero () {
		// Test that constant folding doesn't cause division by zero exceptions
		if (return_55 () != return_55 ()) {
			int d = 1;
			int q = 0;
			int val;			

			val = d / q;
			val = d % q;

			q = -1;
			d = Int32.MinValue;
			val = d / q;

			q = -1;
			val = d % q;
		}

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

		try {
			failed = true;
			q = -1;
			d = Int64.MinValue;
			val = d / q;
		} catch (DivideByZeroException) {
			/* wrong exception */
		} catch (ArithmeticException) {
			failed = false;
		}
		if (failed)
			return 3;

		try {
			failed = true;
			q = -1;
			d = Int64.MinValue;
			val = d % q;
		} catch (DivideByZeroException) {
			/* wrong exception */
		} catch (ArithmeticException) {
			failed = false;
		}
		if (failed)
			return 4;

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

	static int test_0_invalid_unbox () {

		int i = 123;
		object o = "Some string";
		int res = 1;
		
		try {
			// Illegal conversion; o contains a string not an int
			i = (int) o;   
		} catch (Exception e) {
			if (i ==123)
				res = 0;
		}

		return res;
	}

	// Test that double[] can't be cast to double (bug #46027)
	static int test_0_invalid_unbox_arrays () {
		double[] d1 = { 1.0 };
		double[][] d2 = { d1 };
		Array a = d2;

		try {
			foreach (double d in a) {
			}
			return 1;
		}
		catch (InvalidCastException e) {
			return 0;
		}
	}

	/* bug# 42190, at least mcs generates a leave for the return that
	 * jumps out of multiple exception clauses: we used to execute just 
	 * one enclosing finally block.
	 */
	static int finally_level;
	static void do_something () {
		int a = 0;
		try {
			try {
				return;
			} finally {
				a = 1;
			}
		} finally {
			finally_level++;
		}
	}

	static int test_2_multiple_finally_clauses () {
		finally_level = 0;
		do_something ();
		if (finally_level == 1)
			return 2;
		return 0;
	}

	static int test_3_checked_cast_un () {
                ulong i = 0x8000000034000000;
                long j;

		try {
	                checked { j = (long)i; }
		} catch (OverflowException) {
			j = 2;
		}

		if (j != 2)
			return 0;
		return 3;
	}
	
	static int test_4_checked_cast () {
                long i;
                ulong j;

		unchecked { i = (long)0x8000000034000000;};
		try {
                	checked { j = (ulong)i; }
		} catch (OverflowException) {
			j = 3;
		}

		if (j != 3)
			return 0;
		return 4;
	}

	static readonly int[] mul_dim_results = new int[] {
		0, 0, 0, 1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 6, 0, 7, 0, 8,
		1, 0, 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1, 6, 1, 7, 1, 8,
		2, 0, 2, 1, 2, 8, 
		3, 0, 3, 1, 3, 8, 
		4, 0, 4, 1, 4, 8, 
		5, 0, 5, 1, 5, 2, 5, 3, 5, 4, 5, 5, 5, 6, 5, 7, 5, 8,
		6, 0, 6, 1, 6, 2, 6, 3, 6, 4, 6, 5, 6, 6, 6, 7, 6, 8,
		7, 0, 7, 1, 7, 2, 7, 3, 7, 4, 7, 5, 7, 6, 7, 7, 7, 8,
	};

	static int test_0_multi_dim_array_access () {
		int [,] a = System.Array.CreateInstance (typeof (int),
			new int [] {3,6}, new int [] {2,2 }) as int[,];
                int x, y;
		int result_idx = 0;
		for (x = 0; x < 8; ++x) {
			for (y = 0; y < 9; ++y) {
				bool got_ex = false;
				try {
					a [x, y] = 1;
				} catch {
					got_ex = true;
				}
				if (got_ex) {
					if (result_idx >= mul_dim_results.Length)
						return -1;
					if (mul_dim_results [result_idx] != x || mul_dim_results [result_idx + 1] != y) {
						return result_idx + 1;
					}
					result_idx += 2;
				}
			}
		}
		if (result_idx == mul_dim_results.Length)
			return 0;
		return 200;
	}

	static void helper_out_obj (out object o) {
		o = (object)"buddy";
	}

	static void helper_out_string (out string o) {
		o = "buddy";
	}

	static int test_2_array_mismatch () {
		string[] a = { "hello", "world" };
		object[] b = a;
		bool passed = false;

		try {
			helper_out_obj (out b [1]);
		} catch (ArrayTypeMismatchException) {
			passed = true;
		}
		if (!passed)
			return 0;
		helper_out_string (out a [1]);
		if (a [1] != "buddy")
			return 1;
		return 2;
	}

	static int test_0_ovf () {
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

			ocount = 0;
			try {
				long a = Int64.MinValue;
				long b = 10;
				long v = a * b;
			} catch {
				ocount ++;
			}
			if (ocount != 1)
				return 22;

			ocount = 0;
			try {
				long a = 10;
				long b = Int64.MinValue;
				long v = a * b;
			} catch {
				ocount ++;
			}
			if (ocount != 1)
				return 23;
		}
		
		return 0;
	}

	class Broken {
		static int i;

		static Broken () {
			throw new Exception ("Ugh!");
		}
	
		public static int DoSomething () {
			return i;
		}
	}

	static int test_0_exception_in_cctor () {
		try {
			Broken.DoSomething ();
		}
		catch (TypeInitializationException) {
			// This will only happen once even if --regression is used
		}
		return 0;
	}

	static int test_5_regalloc () {
		int i = 0;

		try {
			for (i = 0; i < 10; ++i) {
				if (i == 5)
					throw new Exception ();
			}
		}
		catch (Exception) {
			if (i != 5)
				return i;
		}

		// Check that variables written in catch clauses are volatile
		int j = 0;
		try {
			throw new Exception ();
		}
		catch (Exception) {
			j = 5;
		}
		if (j != 5)
			return 6;

		int k = 0;
		try {
			try {
				throw new Exception ();
			}
			finally {
				k = 5;
			}
		}
		catch (Exception) {
		}
		if (k != 5)
			return 7;

		return i;
	}

	/* MarshalByRefObject prevents the methods from being inlined */
	class ThrowClass : MarshalByRefObject {
		public static void rethrow1 () {
			throw new Exception ();
		}

		public static void rethrow2 () {
			rethrow1 ();
		}
	}

	static int test_0_rethrow_stacktrace () {
		// Check that rethrowing an exception preserves the original stack trace
		try {
			try {
				ThrowClass.rethrow2 ();
			}
			catch (Exception ex) {
				throw;
			}
		}
		catch (Exception ex) {
			if (ex.StackTrace.IndexOf ("rethrow2") != -1)
				return 0;
		}

		return 1;
	}
	
	interface IFace {}
	class Face : IFace {}
		
	static int test_1_array_mismatch_2 () {
		try {
			object [] o = new Face [1];
			o [0] = 1;
			return 0;
		} catch (ArrayTypeMismatchException) {
			return 1;
		}
	}
	
	static int test_1_array_mismatch_3 () {
		try {
			object [] o = new IFace [1];
			o [0] = 1;
			return 0;
		} catch (ArrayTypeMismatchException) {
			return 1;
		}
	}
	
	static int test_1_array_mismatch_4 () {
		try {
			object [][] o = new Face [5] [];
			o [0] = new object [5];
			
			return 0;
		} catch (ArrayTypeMismatchException) {
			return 1;
		}
	}
}

