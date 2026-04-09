using System;
using System.Reflection;
using System.Runtime.CompilerServices;

/*
 * Regression tests for the mono JIT.
 *
 * Each test needs to be of the form:
 *
 * public static int test_<result>_<name> ();
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

#if __MOBILE__
class ExceptionTests
#else
class Tests
#endif
{

#if !__MOBILE__
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}
#endif

	public static int test_0_catch () {
		Exception x = new Exception ();
		
		try {
			throw x;
		} catch (Exception e) {
			if (e == x)
				return 0;
		}
		return 1;
	}

	public static int test_0_finally_without_exc () {
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

	public static int test_0_finally () {
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

	public static int test_0_nested_finally () {
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

	public static int test_0_byte_cast () {
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
	
	public static int test_0_sbyte_cast () {
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
			return -20;

		try {
			ulong ul = 128;
			failed = true;
			checked {
				b = (sbyte)ul;
			}
		}
		catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 21;
		if (b != 127)
			return -21;

		return 0;
	}

	public static int test_0_ushort_cast () {
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
	
	public static int test_0_short_cast () {
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

		try {
			l = 0x00000000ffffffff;
			failed = true;
			checked {
				b = (short)l;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 17;

		try {
			ulong ul = 32768;
			failed = true;
			checked {
				b = (short)ul;
			}
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 18;

		return 0;
	}
	
	public static int test_0_int_cast () {
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

		try {
			ulong ul = (long)(System.Int32.MaxValue) + 1;
			failed = true;
			checked {
				a = (int)ul;
			}
		}
		catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 10;

		try {
			ulong ul = UInt64.MaxValue;
			failed = true;
			checked {
				a = (int)ul;
			}
		}
		catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 11;

		{
			int i; 
			float f = 1.1f;
			checked {
				i = (int) f;
			}
		}

		return 0;
	}

	public static int test_0_uint_cast () {
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
	
	public static int test_0_long_cast () {

		/*
		 * These tests depend on properties of x86 fp arithmetic so they won't work
		 * on other platforms.
		 */
		/*
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
		*/

		{
			long i; 
			float f = 1.1f;
			checked {
				i = (long) f;
			}
		}

		return 0;
	}

	/* Github issue 13284 */
	public static int test_0_ulong_ovf_spilling () {
		checked {
			ulong x = 2UL;
			ulong y = 1UL;
			ulong z = 3UL;
			ulong t = x - y;

			try {
				var a = x - y >= z;
				if (a)
					return 1;
				// Console.WriteLine ($"u64 ({x} - {y} >= {z}) => {a} [{(a == false ? "OK" : "NG")}]");
			} catch (OverflowException) {
				return 2;
				// Console.WriteLine ($"u64 ({x} - {y} >= {z}) => overflow [NG]");
			}

			try {
				var a = t >= z;
				if (a)
					return 3;
				// Console.WriteLine ($"u64 ({t} >= {z}) => {a} [{(a == false ? "OK" : "NG")}]");
			} catch (OverflowException) {
				return 4;
				// Console.WriteLine ($"u64 ({t} >= {z}) => overflow [NG]");
			}

			try {
				var a = x - y - z >= 0;
				if (a)
					return 5;
				else
					return 6;
				// Console.WriteLine ($"u64 ({x} - {y} - {z} >= 0) => {a} [NG]");
			} catch (OverflowException) {
				return 0;
				// Console.WriteLine ($"u64 ({x} - {y} - {z} >= 0) => overflow [OK]");
			}
		}
	}

	public static int test_0_ulong_cast () {
		ulong a;
		bool failed;

		/*
		 * These tests depend on properties of x86 fp arithmetic so they won't work
		 * on other platforms.
		 */

		/*
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
		*/	

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

	public static int test_0_simple_double_casts () {

		double d = 0xffffffff;

		if ((uint)d != 4294967295)
			return 1;

		/*
		 * These tests depend on properties of x86 fp arithmetic so they won't work
		 * on other platforms.
		 */
		/*
		d = 0xffffffffffffffff;

		if ((ulong)d != 0)
			return 2;

		if ((ushort)d != 0)
			return 3;
			
		if ((byte)d != 0)
			return 4;
		*/
			
		d = 0xffff;

		if ((ushort)d != 0xffff)
			return 5;
		
		if ((byte)d != 0xff)
			return 6;
			
		return 0;
	}
	
	public static int test_0_div_zero () {
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
		} catch (OverflowException) {
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
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 4;

		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void dummy () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static int div_zero_llvm_inner (int i) {
		try {
			// This call make use avoid the 'handler without invoke' restriction in the llvm backend
			dummy ();
			return 5 / i;
		} catch (Exception ex) {
			return 0;
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static long div_zero_llvm_inner_long (long l) {
		try {
			dummy ();
			return (long)5 / l;
		} catch (Exception ex) {
			return 0;
		}
	}

	public static int test_0_div_zero_llvm () {
	    long r = div_zero_llvm_inner (0);
		if (r != 0)
			return 1;
	    r = div_zero_llvm_inner_long (0);
		if (r != 0)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static int div_overflow_llvm_inner (int i) {
		try {
			dummy ();
			return Int32.MinValue / i;
		} catch (Exception ex) {
			return 0;
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static long div_overflow_llvm_inner_long (long l) {
		try {
			dummy ();
			return Int64.MinValue / l;
		} catch (Exception ex) {
			return 0;
		}
	}

	public static int test_0_div_overflow_llvm () {
		long r = div_overflow_llvm_inner (-1);
		if (r != 0)
			return 1;
		r = div_overflow_llvm_inner_long ((long)-1);
		if (r != 0)
			return 2;
		return 0;
	}

	public static int return_55 () {
		return 55;
	}

	public static int test_0_cfold_div_zero () {
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

	public static int test_0_udiv_zero () {
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

	public static int test_0_long_div_zero () {
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
		} catch (OverflowException) {
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
		} catch (OverflowException) {
			failed = false;
		}
		if (failed)
			return 4;

		return 0;
	}

	public static int test_0_ulong_div_zero () {
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

	public static int test_0_float_div_zero () {
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

	public static int test_0_invalid_unbox () {

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
	public static int test_0_invalid_unbox_arrays () {
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
	public static int finally_level;
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

	public static int test_2_multiple_finally_clauses () {
		finally_level = 0;
		do_something ();
		if (finally_level == 1)
			return 2;
		return 0;
	}

	public static int test_3_checked_cast_un () {
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
	
	public static int test_4_checked_cast () {
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

	public static int test_0_multi_dim_array_access () {
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

	public static int test_2_array_mismatch () {
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

	public static int test_0_ovf1 () {
		int exception = 0;
		
		checked {
			try {
				ulong a =  UInt64.MaxValue - 1;
				ulong t = a++;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_1_ovf2 () {
		int exception = 0;

		checked {
			try {
				ulong a =  UInt64.MaxValue;
				ulong t = a++;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_0_ovf3 () {
		int exception = 0;

		long a = Int64.MaxValue - 1;
		checked {
			try {
				long t = a++;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_1_ovf4 () {
		int exception = 0;

		long a = Int64.MaxValue;
		checked {
			try {
				long t = a++;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_0_ovf5 () {
		int exception = 0;

		ulong a = UInt64.MaxValue - 1;
		checked {
			try {
				ulong t = a++;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_1_ovf6 () {
		int exception = 0;

		ulong a = UInt64.MaxValue;
		checked {
			try {
				ulong t = a++;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_0_ovf7 () {
		int exception = 0;

		long a = Int64.MinValue + 1;
		checked {
			try {
				long t = a--;
			} catch {
				exception = 1;
			}
		}
		return 0;
	}

	public static int test_1_ovf8 () {
		int exception = 0;

		long a = Int64.MinValue;
		checked {
			try {
				long t = a--;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_0_ovf9 () {
		int exception = 0;

		ulong a = UInt64.MinValue + 1;
		checked {
			try {
				ulong t = a--;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_1_ovf10 () {
		int exception = 0;

		ulong a = UInt64.MinValue;
		checked {
			try {
				ulong t = a--;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_0_ovf11 () {
		int exception = 0;

		int a = Int32.MinValue + 1;
		checked {
			try {
				int t = a--;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_1_ovf12 () {
		int exception = 0;

		int a = Int32.MinValue;
		checked {
			try {
				int t = a--;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_0_ovf13 () {
		int exception = 0;

		uint a = 1;
		checked {
			try {
				uint t = a--;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_1_ovf14 () {
		int exception = 0;

		uint a = 0;
		checked {
			try {
				uint t = a--;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_0_ovf15 () {
		int exception = 0;

		sbyte a = 126;
		checked {
			try {
				sbyte t = a++;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_1_ovf16 () {
		int exception = 0;

		sbyte a = 127;
		checked {
			try {
				sbyte t = a++;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_0_ovf17 () {
		int exception = 0;

		checked {
			try {
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_0_ovf18 () {
		int exception = 0;

		int a = 1 << 29;
		checked {
			try {
				int t = a*2;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_1_ovf19 () {
		int exception = 0;

		int a = 1 << 30;
		checked {
			try {
				int t = a*2;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_0_ovf20 () {
		int exception = 0;

		checked {
			try {
				ulong a = 0xffffffffff;
				ulong t = a*0x0ffffff;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_1_ovf21 () {
		int exception = 0;

		ulong a = 0xffffffffff;
		checked {
			try {
				ulong t = a*0x0fffffff;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_1_ovf22 () {
		int exception = 0;

		long a = Int64.MinValue;
		long b = 10;
		checked {
			try {
				long v = a * b;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	public static int test_1_ovf23 () {
		int exception = 0;

		long a = 10;
		long b = Int64.MinValue;
		checked {
			try {
				long v = a * b;
			} catch {
				exception = 1;
			}
		}
		return exception;
	}

	class Broken {
		public static int i;

		static Broken () {
			throw new Exception ("Ugh!");
		}
	
		public static int DoSomething () {
			return i;
		}
	}

	public static int test_0_exception_in_cctor () {
		try {
			Broken.DoSomething ();
		}
		catch (TypeInitializationException) {
			// This will only happen once even if --regression is used
		}
		return 0;
	}

	public static int test_5_regalloc () {
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

	public static void rethrow () {
		try {
			throw new ApplicationException();
		} catch (ApplicationException) {
			try {
				throw new OverflowException();
			} catch (Exception) {
				throw;
			}
		}
	}

	// Test that a rethrow rethrows the correct exception
	public static int test_0_rethrow_nested () {
		try {
			rethrow ();
		} catch (OverflowException) {
			return 0;
		} catch (Exception) {
			return 1;
		}
		return 2;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void rethrow1 () {
		throw new Exception ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void rethrow2 () {
		rethrow1 ();
		/* This disables tailcall opts */
		Console.WriteLine ();
	}

	[Category ("!BITCODE")]
	public static int test_0_rethrow_stacktrace () {
		// Check that rethrowing an exception preserves the original stack trace
		try {
			try {
				rethrow2 ();
			}
			catch (Exception ex) {
				// Check that each catch clause has its own exception variable
				// If not, the throw below will overwrite the exception used
				// by the rethrow
				try {
					throw new DivideByZeroException ();
				}
				catch (Exception foo) {
				}

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
		
	public static int test_1_array_mismatch_2 () {
		try {
			object [] o = new Face [1];
			o [0] = 1;
			return 0;
		} catch (ArrayTypeMismatchException) {
			return 1;
		}
	}
	
	public static int test_1_array_mismatch_3 () {
		try {
			object [] o = new IFace [1];
			o [0] = 1;
			return 0;
		} catch (ArrayTypeMismatchException) {
			return 1;
		}
	}
	
	public static int test_1_array_mismatch_4 () {
		try {
			object [][] o = new Face [5] [];
			o [0] = new object [5];
			
			return 0;
		} catch (ArrayTypeMismatchException) {
			return 1;
		}
	}

	public static int test_0_array_size () {
		bool failed;

		try {
			failed = true;
			int[,] mem2 = new int [Int32.MaxValue, Int32.MaxValue];
		}
		catch (OutOfMemoryException e) {
			failed = false;
		}
		if (failed)
			return 2;

		return 0;
	}

	struct S {
		int i, j, k, l, m, n;
	}

	static IntPtr[] addr;

	static unsafe void throw_func (int i, S s) {
		addr [i] = new IntPtr (&i);
		throw new Exception ();
	}

	/* Test that arguments are correctly popped off the stack during unwinding */
	/* FIXME: Fails on x86 when llvm is enabled (#5432) */
	/*
	public static int test_0_stack_unwind () {
		addr = new IntPtr [1000];
		S s = new S ();
		for (int j = 0; j < 1000; j++) {
			try {
				throw_func (j, s);
			}
			catch (Exception) {
			}
		}
		return (addr [0].ToInt64 () - addr [100].ToInt64 () < 100) ? 0 : 1;
	}
	*/

	static unsafe void get_sp (int i) {
		addr [i] = new IntPtr (&i);
	}

	/* Test that the arguments to the throw trampoline are correctly popped off the stack */
	public static int test_0_throw_unwind () {
		addr = new IntPtr [1000];
		S s = new S ();
		for (int j = 0; j < 1000; j++) {
			try {
				get_sp (j);
				throw new Exception ();
			}
			catch (Exception) {
			}
		}
		return (addr [0].ToInt64 () - addr [100].ToInt64 () < 100) ? 0 : 1;
	}

	public static int test_0_regress_73242 () {
		int [] arr = new int [10];
		for (int i = 0; i < 10; ++i)
			arr [i] = 0;
		try {
			throw new Exception ();
		}
		catch {
		}
		return 0;
    }

	public static int test_0_nullref () {
		try {
			Array foo = null;
			foo.Clone();
		} catch (NullReferenceException e) {
			return 0;
		}
		return 1;
	}

	public int amethod () {
		return 1;
	}

	public static int test_0_nonvirt_nullref_at_clause_start () {
		ExceptionTests t = null;
		try {
			t.amethod ();
		} catch (NullReferenceException) {
			return 0;
		}

		return 1;
	}

	public static int throw_only () {
		throw new Exception ();
	}

	[MethodImpl(MethodImplOptions.NoInlining)] 
	public static int throw_only2 () {
		return throw_only ();
	}

	public static int test_0_inline_throw_only () {
		try {
			return throw_only2 ();
		}
		catch (Exception ex) {
			return 0;
		}
	}

	public static string GetText (string s) {
		return s;
	}

	public static int throw_only_gettext () {
		throw new Exception (GetText ("FOO"));
	}

	public static int test_0_inline_throw_only_gettext () {
		object o = null;
		try {
			o = throw_only_gettext ();
		}
		catch (Exception ex) {
			return 0;
		}

		return o != null ? 0 : 1;
	}

	// bug #78633
	public static int test_0_throw_to_branch_opt_outer_clause () {
		int i = 0;

		try {
			try {
				string [] files = new string[1];

				string s = files[2];
			} finally {
				i ++;
			}
		} catch {
		}

		return (i == 1) ? 0 : 1;
	}		

	// bug #485721
	public static int test_0_try_inside_finally_cmov_opt () {
		bool Reconect = false;

		object o = new object ();

		try {
		}
		catch (Exception ExCon) {
			if (o != null)
				Reconect = true;

			try {
			}
			catch (Exception Last) {
			}
		}
		finally {
			if (Reconect == true) {
				try {
				}
				catch (Exception ex) {
				}
			}
		}

		return 0;
	}

	public static int test_0_inline_throw () {
		try {
			inline_throw1 (5);
			return 1;
		} catch {
			return 0;
		}
	}

	// for llvm, the end bblock is unreachable
	public static int inline_throw1 (int i) {
		if (i == 0)
			throw new Exception ();
		else
			return inline_throw2 (i);
	}

	public static int inline_throw2 (int i) {
		throw new Exception ();
	}

	// bug #539550
	public static int test_0_lmf_filter () {
		try {
			// The invoke calls a runtime-invoke wrapper which has a filter clause
#if __MOBILE__
			typeof (ExceptionTests).GetMethod ("lmf_filter").Invoke (null, new object [] { });
#else
			typeof (Tests).GetMethod ("lmf_filter").Invoke (null, new object [] { });
#endif
		} catch (TargetInvocationException) {
		}
		return 0;
	}

    public static void lmf_filter () {
        try {
            Connect ();
        }
        catch {
            throw new NotImplementedException ();
        }
    }

    public static void Connect () {
        Stop ();
        throw new Exception();
    }

    public static void Stop () {
        try {
            lock (null) {}
        }
        catch {
        }
    }

	private static void do_raise () {
		throw new System.Exception ();
	}

	private static int int_func (int i) {
		return i;
	}

	// #559876
	public static int test_8_local_deadce_causes () {
      int myb = 4;
  
      try {
        myb = int_func (8);
        do_raise();
        myb = int_func (2);
      } catch (System.Exception) {
		  return myb;
	  }
	  return 0;
	}

	public static int test_0_except_opt_two_clauses () {
		int size;
		size = -1;
		uint ui = (uint)size;
		try {
			checked {
				uint v = ui * (uint)4;
			}
		} catch (OverflowException e) {
			return 0;
		} catch (Exception) {
			return 1;
		}

		return 2;
	}

    class Child
    {
        public virtual long Method()
        {
            throw new Exception();
        }
    }

	/* #612206 */
	public static int test_100_long_vars_in_clauses_initlocals_opt () {
		Child c = new Child();
		long value = 100; 
		try {
			value = c.Method();
		}
		catch {}
		return (int)value;
	}

	class A {
		public object AnObj;
	}

	public static void DoSomething (ref object o) {
	}

	public static int test_0_ldflda_null () {
		A a = null;

		try {
			DoSomething (ref a.AnObj);
		} catch (NullReferenceException) {
			return 0;
		}

		return 1;
	}

	unsafe struct Foo
	{
		public int i;

		public static Foo* pFoo;
	}

	/* MS.NET doesn't seem to throw in this case */
	public unsafe static int test_0_ldflda_null_pointer () {
		int* pi = &Foo.pFoo->i;

		return 0;
	}

	static int test_0_try_clause_in_finally_clause_regalloc () {
		// Fill up registers with values
		object a = new object ();
		object[] arr1 = new object [1];
		object[] arr2 = new object [1];
		object[] arr3 = new object [1];
		object[] arr4 = new object [1];
		object[] arr5 = new object [1];

		for (int i = 0; i < 10; ++i)
			arr1 [0] = a;
		for (int i = 0; i < 10; ++i)
			arr2 [0] = a;
		for (int i = 0; i < 10; ++i)
			arr3 [0] = a;
		for (int i = 0; i < 10; ++i)
			arr4 [0] = a;
		for (int i = 0; i < 10; ++i)
			arr5 [0] = a;

		int res = 1;
		try {
			try_clause_in_finally_clause_regalloc_inner (out res);
		} catch (Exception) {
		}
		return res;		
	}

	public static object Throw () {
		for (int i = 0; i < 10; ++i)
			;
		throw new Exception ();
	}

	static void try_clause_in_finally_clause_regalloc_inner (out int res) {
		object o = null;

		res = 1;
		try {
			o = Throw ();
		} catch (Exception) {
			/* Make sure this doesn't branch to the finally */
			throw new DivideByZeroException ();
		} finally {
			try {
				/* Make sure o is register allocated */
				if (o == null)
					res = 0;
				else
					res = 1;
				if (o == null)
					res = 0;
				else
					res = 1;
				if (o == null)
					res = 0;
				else
					res = 1;
			} catch (DivideByZeroException) {
			}
		}
	}

    public static bool t_1835_inner () {
        bool a = true;
        if (a) throw new Exception();
        return true;
    }

	[MethodImpl(MethodImplOptions.NoInlining)] 
    public static bool t_1835_inner_2 () {
		bool b = t_1835_inner ();
		return b;
	}

	public static int test_0_inline_retval_throw_in_branch_1835 () {
		try {
			t_1835_inner_2 ();
		} catch {
			return 0;
		}
		return 1;
	}

	static bool finally_called = false;

	static void regress_30472 (int a, int b) {
			checked {
				try {
					int sum = a + b;
				} finally {
					finally_called = true;
				}
            }
		}

	public static int test_0_regress_30472 () {
		finally_called = false;
		try {
		    regress_30472 (Int32.MaxValue - 1, 2);
		} catch (Exception ex) {
		}
		return finally_called ? 0 : 1;
	}

	static int array_len_1 = 1;

	public static int test_0_bounds_check_negative_constant () {
		try {
			byte[] arr = new byte [array_len_1];
			byte b = arr [-1];
			return 1;
		} catch {
		}
		try {
			byte[] arr = new byte [array_len_1];
			arr [-1] = 1;
			return 2;
		} catch {
		}
		return 0;
	}

	public static int test_0_string_bounds_check_negative_constant () {
		try {
			string s = "A";
			char c = s [-1];
			return 1;
		} catch {
		}
		return 0;
	}

	public class MyException : Exception {
		public int marker = 0;
		public string res = "";

		public MyException (String res) {
			this.res = res;
		}

		public bool FilterWithoutState () {
			return this.marker == 0x666;
		}

		public bool FilterWithState () {
			bool ret = this.marker == 0x566;
			this.marker += 0x100;
			return ret;
		}

		public bool FilterWithStringState () {
			bool ret = this.marker == 0x777;
			this.res = "fromFilter_" + this.res;
			return ret;
		}
	}

	[Category ("!BITCODE")]
	public static int test_1_basic_filter_catch () {
		try {
			MyException e = new MyException ("");
			e.marker = 0x1337;
			throw e;
		} catch (MyException ex) when (ex.marker == 0x1337) {
			return 1;
		}
		return 0;
	}

	[Category ("!BITCODE")]
	public static int test_1234_complicated_filter_catch () {
		string res = "init";
		try {
			MyException e = new MyException (res);
			e.marker = 0x566;
			try {
				try {
					throw e;
				} catch (MyException ex) when (ex.FilterWithoutState ()) {
					res = "WRONG_" + res;
				} finally {
					e.marker = 0x777;
					res = "innerFinally_" + res;
				}
			} catch (MyException ex) when (ex.FilterWithState ()) {
				res = "2ndcatch_" + res;
			}
			// "2ndcatch_innerFinally_init"
			// Console.WriteLine ("res1: " + res);
			e.res = res;
			throw e;
		} catch (MyException ex) when (ex.FilterWithStringState ()) {
			res = "fwos_" + ex.res;
		} finally {
			res = "outerFinally_" + res;
		}
		// Console.WriteLine ("res2: " + res);
		return "outerFinally_fwos_fromFilter_2ndcatch_innerFinally_init" == res ? 1234 : 0;
	}

    public struct FooStruct
    {
        public long Part1 { get; }
        public long Part2 { get; }

        public byte Part3 { get; }
    }

    [MethodImpl( MethodImplOptions.NoInlining )]
    private static bool ExceptionFilter( byte x, FooStruct item ) => true;

	[Category ("!BITCODE")]
	public static int test_0_filter_caller_area () {
        try {
            throw new Exception();
        }
        catch (Exception) when (ExceptionFilter (default(byte), default (FooStruct))) {
        }
		return 0;
	}

	public static int test_0_signed_ct_div () {
		int n = 2147483647;
		bool divide_by_zero = false;
		bool overflow = false;

		n = -n;
		n--; /* MinValue */
		try {
			int r = n / (-1);
		} catch (OverflowException) {
			overflow = true;
		}
		if (!overflow)
			return 7;

		try {
			int r = n / 0;
		} catch (DivideByZeroException) {
			divide_by_zero = true;
		}
		if (!divide_by_zero)
			return 8;

		if ((n / 35) != -61356675)
			return 9;
		if ((n / -35) != 61356675)
			return 10;
		n = -(n + 1);  /* MaxValue */
		if ((n / 35) != 61356675)
			return 11;
		if ((n / -35) != -61356675)
			return 12;

		return 0;
	}

	public static int test_0_unsigned_ct_div () {
		uint n = 4294967295;
		bool divide_by_zero = false;

		try {
			uint a = n / 0;
		} catch (DivideByZeroException) {
			divide_by_zero = true;
		}

		if (!divide_by_zero)
			return 5;

		if ((n / 35) != 122713351)
			return 9;

		return 0;
	}

    struct AStruct {
        public int i1, i2, i3, i4;
    }

    // Running finally clauses with the interpreter in llvmonly-interp mode
    public static int test_0_finally_deopt () {
        int arg_i = 2;
        var o = new ExceptionTests ();
        try {
            o.finally_deopt (1, 0, ref arg_i, new AStruct () { i1 = 1, i2 = 2, i3 = 3, i4 = 4 });
        } catch (Exception) {
        }
        return finally_deopt_res;
    }

    static int dummy_static = 5;

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
    static void throw_inner () {
        // Avoid warnings/errors
        if (dummy_static == 5)
            throw new Exception ();
        // Run with the interpreter
        try {
            throw new Exception ();
        } catch (Exception) {
        }
    }

    static int finally_deopt_res;

    void finally_deopt (int arg_i, int unused_arg_i, ref int ref_arg_i, AStruct s) {
        int i = 3;

        try {
            try {
                i = 5;
                throw_inner ();
            } finally {
                // Check that arguments/locals are copied correctly to the interpreter
                object o = this;
                if (!(o is ExceptionTests))
                    finally_deopt_res = 1;
                if (arg_i != 1)
                    finally_deopt_res = 2;
                arg_i ++;
                if (i != 5)
                    finally_deopt_res = 3;
                i ++;
                if (ref_arg_i != 2)
                    finally_deopt_res = 4;
                ref_arg_i ++;
                if (s.i1 != 1 || s.i2 != 2)
                    finally_deopt_res = 5;
                s.i1 ++;
                s.i2 ++;
            }
        } finally {
            // Check that arguments/locals were copied back after the first call to the interpreter
            if (arg_i != 2)
                finally_deopt_res = 10;
            if (ref_arg_i != 3)
                finally_deopt_res = 11;
            if (i != 6)
                finally_deopt_res = 12;
            if (s.i1 != 2 || s.i2 != 3)
                finally_deopt_res = 13;
        }
    }
}

#if !__MOBILE__
class ExceptionTests : Tests
{
}
#endif
