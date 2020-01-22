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

#if __MOBILE__
class ArrayTests
#else
class Tests
#endif
{

#if !__MOBILE__
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}
#endif
	
	public static int test_10_create () {
		int[] a = new int [10];
		return a.Length;
	}

	public static int test_0_unset_value () {
		int[] a = new int [10];
		return a [5];
	}

	public static int test_3_set_value () {
		int[] a = new int [10];
		a [5] = 3;
		return a [5];
	}

	public static int test_0_char_array_1 () {
		int value = -30;
		char[] tmp = new char [20];
		char[] digitLowerTable = new char[16];
		tmp[0] = digitLowerTable[-(value % 10)];
		return 0;
	}
	
	public static int test_0_char_array_2 () {
		int value = 5;
		char[] tmp = new char [20];
		char[] digitLowerTable = new char[16];
		tmp[0] = digitLowerTable[value % 10];
		return 0;
	}

	public static int test_0_char_array_3 () {
		int value = -1;
		char[] tmp = new char [20];
		char[] digitLowerTable = new char[16];
		tmp [0] = digitLowerTable[value & 15];		
		return 0;
	}

	public unsafe static int test_0_byte_array () {
		byte [] src = new byte [8];
		double ret;
		byte *dst = (byte *)&ret;
		int start = 0;

		dst[0] = src[4 + start];
		
		return 0;
	}
	
	public static int test_0_set_after_shift () {
		int [] n = new int [1];
		int b = 16;
                   
		n [0] = 100 + (1 << (16 - b));

		if (n [0] != 101)
			return 1;

		return 0;
	}

	/* Regression test for #30073 */
	public static int test_0_newarr_emulation () {
		double d = 500;
		checked {
			double [] arr = new double [(int)d];
		}
		return 0;
	}

	class BitClass {
		private Int32[] m_array = new int [10];

		public void setBit (int bitIndex, bool value) {
			int index = bitIndex/32;
			int shift = bitIndex%32;

			Int32 theBit = 1 << shift;
			if (value)
				m_array[index] |= theBit;
			else
				m_array[index] &= ~theBit;
		}
	
		public bool getBit (int bitIndex) {
			int index = bitIndex/32;
			int shift = bitIndex%32;

			Int32 theBit = m_array[index] & (1 << shift);
			return (theBit == 0) ? false : true;
		}
	}
	
	public static int test_1_bit_index () {
		var t = new BitClass ();
		t.setBit (0, true);
		t.setBit (3, true);
		if (t.getBit (1))
			return 4;
		if (!t.getBit (0))
			return 5;
		if (!t.getBit (3))
			return 6;
		return 1;
	}

	class helper1 {

		int [] ma = new int [56];
		const int MBIG = int.MaxValue;

		public helper1 () {
			for (int k = 1; k < 5; k++) {
				for (int i = 1; i < 56; i++) {
					ma [i] -= ma [1 + (i + 30) % 55];
					if (ma [i] < 0)
						ma [i] += MBIG;
				}
			}
		}
	}

	public static int test_2_regalloc () {
		helper1 h = new helper1 ();
		return 2;
	}
	
	public static int test_0_stelemref_1 () {
		object [] o = new object [1];
		o [0] = null;
		
		return 0;
	}
	
	public static int test_0_stelemref_2 () {
		object [] o = new object [1];
		o [0] = 1;
		
		return 0;
	}
	
	interface IFace {}
	class Face : IFace {}
	
	public static int test_0_stelemref_3 () {
		object [] o = new IFace [1];
		o [0] = new Face ();
		
		return 0;
	}
	
	public static int test_0_stelemref_4 () {
		object [][] o = new object [5] [];
		o [0] = new object [5];
		
		return 0;
	}

	struct FooStruct {
		public int i;

		public FooStruct (int i) {
			this.i = i;
		}
	}

	public static int test_0_arrays () {

		int sum;

		byte[] a1 = new byte [10];
		for (int i = 0; i < 10; ++i)
			a1 [i] = (byte)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += a1 [i];
		if (sum != 45)
			return 1;

		sbyte[] a2 = new sbyte [10];
		for (int i = 0; i < 10; ++i)
			a2 [i] = (sbyte)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += a2 [i];
		if (sum != 45)
			return 2;

		short[] a3 = new short [10];
		for (int i = 0; i < 10; ++i)
			a3 [i] = (short)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += a3 [i];
		if (sum != 45)
			return 3;

		ushort[] a4 = new ushort [10];
		for (int i = 0; i < 10; ++i)
			a4 [i] = (ushort)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += a4 [i];
		if (sum != 45)
			return 4;

		int[] a5 = new int [10];
		for (int i = 0; i < 10; ++i)
			a5 [i] = (int)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += a5 [i];
		if (sum != 45)
			return 5;

		uint[] a6 = new uint [10];
		for (int i = 0; i < 10; ++i)
			a6 [i] = (uint)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += (int)a6 [i];
		if (sum != 45)
			return 6;

		long[] a7 = new long [10];
		for (int i = 0; i < 10; ++i)
			a7 [i] = i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += (int)a7 [i];
		if (sum != 45)
			return 7;

		ulong[] a8 = new ulong [10];
		for (int i = 0; i < 10; ++i)
			a8 [i] = (ulong)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += (int)a8 [i];
		if (sum != 45)
			return 8;

		float[] a9 = new float [10];
		for (int i = 0; i < 10; ++i)
			a9 [i] = (float)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += (int)a9 [i];
		if (sum != 45)
			return 9;

		double[] a10 = new double [10];
		for (int i = 0; i < 10; ++i)
			a10 [i] = i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += (int)a10 [i];
		if (sum != 45)
			return 10;

		object[] a11 = new object [10];
		object o = new Object ();
		for (int i = 0; i < 10; ++i)
			a11 [i] = o;
		for (int i = 0; i < 10; ++i)
		   if (a11 [i] != o)
				 return 11;

		FooStruct[] a12 = new FooStruct [10];
		for (int i = 0; i < 10; ++i)
			a12 [i] = new FooStruct (i);
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += a12 [i].i;
		if (sum != 45)
			return 12;

		return 0;
	}

	public static int test_0_multi_dimension_arrays () {
		int sum;

		byte[,] a1 = new byte [10, 10];
		for (int i = 0; i < 10; ++i)
			a1 [i, i] = (byte)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += a1 [i, i];
		if (sum != 45)
			return 1;

		sbyte[,] a2 = new sbyte [10, 10];
		for (int i = 0; i < 10; ++i)
			a2 [i, i] = (sbyte)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += a2 [i, i];
		if (sum != 45)
			return 2;

		short[,] a3 = new short [10, 10];
		for (int i = 0; i < 10; ++i)
			a3 [i, i] = (short)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += a3 [i, i];
		if (sum != 45)
			return 3;

		ushort[,] a4 = new ushort [10, 10];
		for (int i = 0; i < 10; ++i)
			a4 [i, i] = (ushort)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += a4 [i, i];
		if (sum != 45)
			return 4;

		int[,] a5 = new int [10, 10];
		for (int i = 0; i < 10; ++i)
			a5 [i, i] = (int)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += a5 [i, i];
		if (sum != 45)
			return 5;

		uint[,] a6 = new uint [10, 10];
		for (int i = 0; i < 10; ++i)
			a6 [i, i] = (uint)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += (int)a6 [i, i];
		if (sum != 45)
			return 6;

		long[,] a7 = new long [10, 10];
		for (int i = 0; i < 10; ++i)
			a7 [i, i] = i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += (int)a7 [i, i];
		if (sum != 45)
			return 7;

		ulong[,] a8 = new ulong [10, 10];
		for (int i = 0; i < 10; ++i)
			a8 [i, i] = (ulong)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += (int)a8 [i, i];
		if (sum != 45)
			return 8;

		float[,] a9 = new float [10, 10];
		for (int i = 0; i < 10; ++i)
			a9 [i, i] = (float)i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += (int)a9 [i, i];
		if (sum != 45)
			return 9;

		double[,] a10 = new double [10, 10];
		for (int i = 0; i < 10; ++i)
			a10 [i, i] = i;
		sum = 0;
		for (int i = 0; i < 10; ++i)
			sum += (int)a10 [i, i];
		if (sum != 45)
			return 10;

		object[,] a11 = new object [10, 10];
		object o = new Object ();
		for (int i = 0; i < 10; ++i)
			a11 [i, i] = o;
		for (int i = 0; i < 10; ++i)
		   if (a11 [i, i] != o)
				 return 11;

		FooStruct[,] a12 = new FooStruct [10, 10];
		for (int i = 0; i < 10; ++i)
			for (int j = 0; j < 10; ++j) {
				/* This one calls Address */
				a12 [i, j] = new FooStruct (i + j);

				/* Test Set as well */
				FooStruct s = new FooStruct (i + j);
				a12 [i, j] = s;
			}
		sum = 0;
		for (int i = 0; i < 10; ++i)
			for (int j = 0; j < 10; ++j) {
				/* This one calls Address */
				sum += a12 [i, j].i;

				/* Test Get as well */
				FooStruct s = a12 [i, j];
				sum += s.i;
			}
		if (sum != 1800)
			return 12;

		/* Null check */
		object[,] a13 = null;
		try {
			a13 [0, 0] = new Object ();
			return 13;
		} catch (NullReferenceException) {
		}

		return 0;
	}

	public static int test_100_3_dimensional_arrays () {
        int[,,] test = new int[10, 10, 10];

		test [1, 1, 1] = 100;
		return test [1, 1, 1];
	}

	public static int test_100_4_dimensional_arrays () {
        int[,,,] test = new int[10, 10, 10, 10];

		test [1, 1, 1, 1] = 100;
		return test [1, 1, 1, 1];
	}

	public static int test_0_bug_71454 () {
		int[,] a = new int[4,4];
		int[,] b = new int[4,4];
		for(int i = 0; i < 4; ++i) {
			b[0,0] = a[0,i % 4];
		}
		return 0;
	}

	public static int test_0_interface_array_cast () {
		try {
			object [] a = new ICloneable [2];
			ICloneable [] b = (ICloneable [])a;
		} catch {
			return 1;
		}
		return 0;
	}

	class Foo {
		public static Foo[][] foo;
	}

	public static int test_0_regress_74549 () {
		new Foo ();
		return 0;
	}

	public static int test_0_regress_75832 () {
		int[] table = new int[] { 0, 0 };
		
		int x = 0;
		
		int temp = -1 ^ x;
		temp = 2 + temp;
		int y = table[temp];

		return y;
	}

	class RefClass {
	}

	public static int test_0_stelem_ref_null_opt () {
		object[] arr = new RefClass [1];

		arr [0] = new RefClass ();
		arr [0] = null;

		return arr [0] == null ? 0 : 1;
	}

	public static int test_0_invalid_new_array_size () {
		int size;
		object res = null;
		size = -1;
		try {
			res = new float [size];
		} catch (OverflowException) {

		} catch (Exception) {
			return 1;
		}
		if (res != null)
			return 2;

		size = -2147483648;
		try {
			res = new float [size];
		} catch (OverflowException) {

		} catch (Exception) {
			return 3;
		}

		if (res != null)
			return 4;

		return 0;
	}

	public static int test_0_multidym_array_with_negative_lower_bound () {
		int[,] x = (int[,]) Array.CreateInstance(typeof (int), new int[] { 2, 2 }, new int[] { -2, -3 });

		if(x.GetLowerBound (0) != -2)
			return 1;
		if (x.GetLowerBound (1) != -3)
			return 2;

		x.SetValue (10, new int [] { -2, -3 });
		x.SetValue (20, new int [] { -2, -2 });
		x.SetValue (30, new int [] { -1, -3 });
		x.SetValue (40, new int [] { -1, -2 });

		try {
			x.SetValue (10, new int [] { -3, -3 });
			return 3;
		} catch (IndexOutOfRangeException) { }

		try {
			x.SetValue (10, new int [] { -2, -4 });
			return 4;
		} catch (IndexOutOfRangeException) { }

		try {
			x.SetValue (10, new int [] { 0, -3 });
			return 5;
		} catch (IndexOutOfRangeException) { }

		try {
			x.SetValue (10, new int [] { -1, -1 });
			return 6;
		} catch (IndexOutOfRangeException) { }

		if ((int)x.GetValue (new int [] { -2, -3 }) != 10)
			return 7;
		if ((int)x.GetValue (new int [] { -2, -2 }) != 20)
			return 8;
		if ((int)x.GetValue (new int [] { -1, -3 }) != 30)
			return 9;
		if ((int)x.GetValue (new int [] { -1, -2 }) != 40)
			return 10;

		try {
			x.GetValue (new int [] { -3, -3 });
			return 11;
		} catch (IndexOutOfRangeException) { }

		try {
			x.GetValue ( new int [] { -2, -4 });
			return 12;
		} catch (IndexOutOfRangeException) { }

		try {
			x.GetValue (new int [] { 0, -3 });
			return 13;
		} catch (IndexOutOfRangeException) { }

		try {
			x.GetValue (new int [] { -1, -1 });
			return 14;
		} catch (IndexOutOfRangeException) { }
		return 0;
	}


	public static int test_0_invalid_new_multi_dym_array_size () {
		int dym_size = 1;
		int size;
		object res = null;
		size = -1;
		try {
			res = new float [dym_size, size];
		} catch (OverflowException) {

		} catch (Exception) {
			return 1;
		}
		if (res != null)
			return 2;

		size = -2147483648;
		try {
			res = new float [size, dym_size];
		} catch (OverflowException) {

		} catch (Exception) {
			return 3;
		}

		if (res != null)
			return 4;

		return 0;
	}

	public enum IntEnum {
		A,B,C
	}

	public enum UintEnum : uint {
		A,B,C
	}

	static bool TryCast<T> (object o) {
		return o is T[];
	}

	public static int test_0_primitive_array_cast () {
		object a = new int[1];
		object b = new uint[1];
		object c = new IntEnum[1];
		object d = new UintEnum[1];

		object[] arr = new object[] { a, b, c, d };
		int err = 1;

		foreach (var v in arr) {
			if (!TryCast<int> (v))
				return err;
			if (!TryCast<uint> (v))
				return err + 1;
			if (!TryCast<IntEnum> (v))
				return err + 2;
			if (!TryCast<UintEnum> (v))
				return err + 3;
			err += 4;
		}

		foreach (var v in arr) {
			if (!(v is int[]))
				return err;
			if (!(v is uint[]))
				return err;
			if (!(v is IntEnum[]))
				return err;
			if (!(v is UintEnum[]))
				return err;
			err += 4;
		}
		return 0;
	}

	public static int test_0_intptr_array_cast () {
		object[] a = new object[] { new int[1], new uint[1] };
		object[] b = new object[] { new long[1], new ulong[1] };
		object[] c = new object[] { new IntPtr[1], new UIntPtr[1] };

		int err = 1;
		if (IntPtr.Size == 4) {
			foreach (var v in a) {
				if (!(v is IntPtr[]))
					return err;
				if (!(v is IntPtr[]))
					return err;
				err += 2;
			}
			foreach (var v in b) {
				if (v is IntPtr[])
					return err;
				if (v is IntPtr[])
					return err;
				err += 2;
			}

			foreach (var v in c) {
				if (!(v is int[]))
					return err;
				if (!(v is uint[]))
					return err;
				err += 2;
			}
		} else {
			foreach (var v in a) {
				if (v is IntPtr[])
					return err;
				if (v is IntPtr[])
					return err;
				err += 2;
			}
			foreach (var v in b) {
				if (!(v is IntPtr[]))
					return err;
				if (!(v is IntPtr[]))
					return err;
				err += 2;
			}
			foreach (var v in c) {
				if (!(v is long[]))
					return err;
				if (!(v is ulong[]))
					return err;
				err += 2;
			}
		}
		return 0;
	}

	public static int test_0_long_indices () {
		int[] arr = new int [10];
		int[,] arr2 = new int [10, 10];
		long index = 1;
		arr [index] = 5;
		if (arr [index] != 5)
			return 1;
		arr2 [index, index] = 5;
		if (arr2 [index, index] != 5)
			return 2;
		return 0;
	}

	// #7438
	public static int test_0_ldelema_2_64bit () {
        bool[,] test = new bool[201,201];
        int x,y;
        for(x=-100;x<100;x++) for(y=-100;y<100;y++){
            test[x+100,y+100] = true;
        }
		return 0;
	}

	public struct TestStruct {
	}

	// #11603
	public static int test_0_ldelema () {
		try {
			TestStruct[] frames = null;
			_ = frames[0];
			return 1;
		} catch (NullReferenceException) {
			return 0;
		}
	}

	static bool alloc_long (long l) {
		try {
			var arr = new byte[l];
			return false;
		} catch (Exception) {
			return true;
		}
	}

	// #13544
	public static int test_0_newarr_ovf () {
		if (!alloc_long (5000000000))
			return 1;
		if (!alloc_long (4000000000))
			return 2;
		if (!alloc_long (-1))
			return 3;
		if (!alloc_long (-4000000000))
			return 4;
		if (!alloc_long (-6000000000))
			return 5;
		return 0;
	}

	static int llvm_ldlen_licm (int[] arr) {
		int sum = 0;
		// The ldlen should be moved out of the loop
		for (int i = 0; i < arr.Length; ++i)
			sum += arr [i];
		return sum;
	}

	public static int test_10_llvm_ldlen_licm () {
		int[] arr = new int [10];
		for (int i = 0; i < 10; ++i)
			arr [i] = 1;
		return llvm_ldlen_licm (arr);
	}

	private unsafe static void WritePtr (FooStruct *val, out FooStruct* ptr)
	{
		ptr = val;
	}

	public unsafe static int test_0_ldelema_ptr () {
		int i;
		int len = 10;
		FooStruct*[] ptr_array = new FooStruct* [len];
		FooStruct str = new FooStruct (3);

		for (i = 0; i < len; i++)
			WritePtr (&str, out ptr_array [i]);

		for (i = 0; i < len; i++) {
			if (ptr_array [i]->i != 3)
				return i;
		}

		return 0;
	}

	class JaggedClass {
		public int[][] a;

		public JaggedClass () {
			a = new int[][]{
				new int[]{1,2,3},
				new int[]{4,5,6},
				new int[]{7,8,9}
			};
		}
	}

	public static int test_4_ref_jagged_array () {
		var f = new JaggedClass ();

		ref int[] r = ref f.a[1];

		return r [0];
	}
}
