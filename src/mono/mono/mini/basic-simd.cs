using System;
using Mono.Simd;

public class SimdTests {
	static int ddd;
	static void InitByRef (out Vector4i v) {
		v = new Vector4i (99);
		if (ddd > 10)
			throw new Exception ("ddd");
	}

	static int test_0_vector4i_one_element_ctor_with_byref ()
	{
		Vector4i a;
		InitByRef (out a);
		if (a.X != 99)
			return 1;
		if (a.Y != 99)
			return 2;
		if (a.Z != 99)
			return 3;
		if (a.W != 99)
			return 4;
		return 0;
	}
	
	static int test_0_vector2d_one_element_ctor () {
		Vector2d a = new Vector2d (99);
		if (a.X != 99)
			return 1;
		if (a.Y != 99)
			return 2;
		return 0;
	}

	static int test_0_vector2ul_one_element_ctor () {
		Vector2ul a = new Vector2ul (99);

		if (a.X != 99)
			return 1;
		if (a.Y != 99)
			return 2;
		return 0;
	}

	static int test_0_vector2l_one_element_ctor () {
		Vector2l a = new Vector2l (99);

		if (a.X != 99)
			return 1;
		if (a.Y != 99)
			return 2;
		return 0;
	}

	static int test_0_vector4f_one_element_ctor () {
		Vector4f a = new Vector4f (99);

		if (a.X != 99)
			return 1;
		if (a.Y != 99)
			return 2;
		if (a.Z != 99)
			return 3;
		if (a.W != 99)
			return 4;
		return 0;
	}

	static int test_0_vector4ui_one_element_ctor () {
		Vector4ui a = new Vector4ui (99);

		if (a.X != 99)
			return 1;
		if (a.Y != 99)
			return 2;
		if (a.Z != 99)
			return 3;
		if (a.W != 99)
			return 4;
		return 0;
	}

	static int test_0_vector4i_one_element_ctor () {
		Vector4i a = new Vector4i (99);

		if (a.X != 99)
			return 1;
		if (a.Y != 99)
			return 2;
		if (a.Z != 99)
			return 3;
		if (a.W != 99)
			return 4;
		return 0;
	}

	static int test_0_vector8us_one_element_ctor () {
		Vector8us a = new Vector8us (99);

		if (a.V0 != 99)
			return 1;
		if (a.V1 != 99)
			return 2;
		if (a.V2 != 99)
			return 3;
		if (a.V3 != 99)
			return 4;
		if (a.V4 != 99)
			return 5;
		if (a.V5 != 99)
			return 6;
		if (a.V6 != 99)
			return 7;
		if (a.V7 != 99)
			return 8;
		return 0;
	}

	static int test_0_vector8s_one_element_ctor () {
		Vector8s a = new Vector8s (99);

		if (a.V0 != 99)
			return 1;
		if (a.V1 != 99)
			return 2;
		if (a.V2 != 99)
			return 3;
		if (a.V3 != 99)
			return 4;
		if (a.V4 != 99)
			return 5;
		if (a.V5 != 99)
			return 6;
		if (a.V6 != 99)
			return 7;
		if (a.V7 != 99)
			return 8;
		return 0;
	}

	static int test_0_vector16sb_one_element_ctor () {
		Vector16sb a = new Vector16sb (99);

		if (a.V0 != 99)
			return 1;
		if (a.V1 != 99)
			return 2;
		if (a.V2 != 99)
			return 3;
		if (a.V3 != 99)
			return 4;
		if (a.V4 != 99)
			return 5;
		if (a.V5 != 99)
			return 6;
		if (a.V6 != 99)
			return 7;
		if (a.V7 != 99)
			return 8;
		if (a.V8 != 99)
			return 9;
		if (a.V9 != 99)
			return 10;
		if (a.V10 != 99)
			return 11;
		if (a.V11 != 99)
			return 12;
		if (a.V12 != 99)
			return 13;
		if (a.V13 != 99)
			return 14;
		if (a.V14 != 99)
			return 15;
		if (a.V15 != 99)
			return 16;
		return 0;
	}

	static int test_0_vector16b_one_element_ctor () {
		Vector16b a = new Vector16b (99);

		if (a.V0 != 99)
			return 1;
		if (a.V1 != 99)
			return 2;
		if (a.V2 != 99)
			return 3;
		if (a.V3 != 99)
			return 4;
		if (a.V4 != 99)
			return 5;
		if (a.V5 != 99)
			return 6;
		if (a.V6 != 99)
			return 7;
		if (a.V7 != 99)
			return 8;
		if (a.V8 != 99)
			return 9;
		if (a.V9 != 99)
			return 10;
		if (a.V10 != 99)
			return 11;
		if (a.V11 != 99)
			return 12;
		if (a.V12 != 99)
			return 13;
		if (a.V13 != 99)
			return 14;
		if (a.V14 != 99)
			return 15;
		if (a.V15 != 99)
			return 16;
		return 0;
	}

	public static unsafe int test_0_sizeof_returns_16_2d ()
	{
		double[] array = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		fixed (double *ptr = &array [0]) {
			Vector2d *f = (Vector2d*)ptr;
			Vector2d a = *f++;
			Vector2d b = *f++;
			Vector2d c = *f++;
			Vector2d d = *f++;

			if (a.X != 1 || a.Y  != 2)
				return 1;
			if (b.X != 3 || b.Y  != 4)
				return 2;
			if (c.X != 5 || c.Y  != 6)
				return 3;
			if (d.X != 7 || d.Y  != 8)
				return 4;
		}
		return 0;
	}

	public static unsafe int test_0_sizeof_returns_16_4f ()
	{
		float[] array = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
		fixed (float *ptr = &array [0]) {
			Vector4f *f = (Vector4f*)ptr;
			Vector4f a = *f++;
			Vector4f b = *f++;
			Vector4f c = *f++;
			Vector4f d = *f++;

			if (a.X != 1 || a.W  != 4)
				return 1;
			if (b.X != 5 || b.W  != 8)
				return 2;
			if (c.X != 9 || c.W  != 12)
				return 3;
			if (d.X != 13 || d.W  != 16)
				return 4;
		}
		return 0;
	}

	public static unsafe int test_0_sizeof_returns_16_8d ()
	{
		short[] array = new short[40];
		for (int i = 0; i < 40; ++i)
			array [i] = (short) (i + 1);

		fixed (short *ptr = &array [0]) {
			Vector8s *f = (Vector8s*)ptr;
			Vector8s a = *f++;
			Vector8s b = *f++;
			Vector8s c = *f++;
			Vector8s d = *f++;

			if (a.V0 != 1 || a.V7  != 8)
				return 1;
			if (b.V0 != 9 || b.V7  != 16)
				return 2;
			if (c.V0 != 17 || c.V7  != 24)
				return 3;
			if (d.V0 != 25 || d.V7  != 32)
				return 4;
		}
		return 0;
	}

	public static unsafe int test_0_sizeof_returns_16_16b ()
	{
		byte[] array = new byte[80];
		for (int i = 0; i < 80; ++i)
			array [i] = (byte) (i + 1);

		fixed (byte *ptr = &array [0]) {
			Vector16b *f = (Vector16b*)ptr;
			Vector16b a = *f++;
			Vector16b b = *f++;
			Vector16b c = *f++;
			Vector16b d = *f++;

			if (a.V0 != 1 || a.V15  != 16)
				return 1;
			if (b.V0 != 17 || b.V15  != 32)
				return 2;
			if (c.V0 != 33 || c.V15  != 48)
				return 3;
			if (d.V0 != 49 || d.V15  != 64)
				return 4;
		}
		return 0;
	}	
	public static int test_0_bug_462457 ()
	{
		Vector4f sum = new Vector4f(0,0,0,0);
		Vector4f add = new Vector4f(1.0F,1.0F,1.0F,1.0F);

		for (int i = 0; i < 10; ++i)
				sum = sum + add;

		if (sum.X != 10f)
			return 1;
		return 0;
	}

	public static int test_0_vector16b_operator_neq () {
		Vector16b a = new Vector16b(1,2,3,5,5,6,7,8,1,2,3,5,5,6,7,8);
		Vector16b b = new Vector16b(1,2,3,5,5,6,7,8,1,2,3,5,5,6,7,8);
		if (a != b)
			return 1;
		b.V0 = 99;
		if (!(a != b))
			return 2;
		return 0;
	}

	public static int test_0_vector16b_operator_eq () {
		Vector16b a = new Vector16b(1,2,3,5,5,6,7,8,1,2,3,5,5,6,7,8);
		Vector16b b = new Vector16b(1,2,3,5,5,6,7,8,1,2,3,5,5,6,7,8);
		if (!(a == b))
			return 1;
		b.V0 = 99;
		if (a == b)
			return 2;
		return 0;
	}

	public static int test_0_vector8us_operator_neq () {
		Vector8us a = new Vector8us(1, 2, 3, 4, 5, 6, 7, 8);
		Vector8us b = new Vector8us(1, 2, 3, 4, 5, 6, 7, 8);
		if (a != b)
			return 1;
		b.V0 = 99;
		if (!(a != b))
			return 2;
		return 0;
	}

	public static int test_0_vector8us_operator_eq () {
		Vector8us a = new Vector8us(1, 2, 3, 4, 5, 6, 7, 8);
		Vector8us b = new Vector8us(1, 2, 3, 4, 5, 6, 7, 8);
		if (!(a == b))
			return 1;
		b.V0 = 99;
		if (a == b)
			return 2;
		return 0;
	}

	public static int test_0_set_vector4f_operator_neq () {
		Vector4f a = new Vector4f(1, 2, 3, 4);
		Vector4f b = new Vector4f(1, 2, 3, 4);
		if (a != b)
			return 1;

		a = new Vector4f(1, 2, float.NaN, 4);
		b = new Vector4f(1, 2, float.NaN, 4);
		if (!(a != b)) //NaN is always !=
			return 2;

		a = new Vector4f(1, 2, float.NaN, 4);
		b = new Vector4f(1, 2, 10, 4);
		if (!(a != b))
			return 3;

		a = new Vector4f(1, 2, float.PositiveInfinity, 4);
		b = new Vector4f(1, 2, float.PositiveInfinity, 4);
		if (a != b)
			return 4;

		a = new Vector4f(1, 2, 20, 4);
		b = new Vector4f(1, 2, 30, 4);
		if (!(a != b))
			return 5;

		return 0;
	}
	
	public static int test_0_set_vector4f_operator_eq () {
		Vector4f a = new Vector4f(1, 2, 3, 4);
		Vector4f b = new Vector4f(1, 2, 3, 4);
		if (!(a == b))
			return 1;

		a = new Vector4f(1, 2, float.NaN, 4);
		b = new Vector4f(1, 2, float.NaN, 4);
		if (a == b)
			return 2;

		a = new Vector4f(1, 2, 10, 4);
		b = new Vector4f(1, 2, float.NaN, 4);
		if (a == b)
			return 3;

		a = new Vector4f(1, 2, float.PositiveInfinity, 4);
		b = new Vector4f(1, 2, float.PositiveInfinity, 4);
		if (!(a == b))
			return 4;
		return 0;
	}

	public static int test_1_set_vector4ui_operator_neq () {
		Vector4ui a = new Vector4ui(1, 2, 3, 4);
		Vector4ui b = new Vector4ui(1, 2, 3, 4);
		if (a != b)
			return 0;
		return 1;
	}

	public static int test_0_set_vector4ui_operator_neq () {
		Vector4ui a = new Vector4ui(1, 2, 3, 4);
		Vector4ui b = new Vector4ui(111, 2, 3, 4);
		if (a != b)
			return 0;
		return 1;
	}

	public static int test_0_set_vector4ui_operator_eq () {
		Vector4ui a = new Vector4ui(1, 2, 3, 4);
		Vector4ui b = new Vector4ui(1, 2, 3, 4);
		if (a == b)
			return 0;
		return 1;
	}

	public static int test_1_set_vector4ui_operator_eq () {
		Vector4ui a = new Vector4ui(1, 2, 3, 4);
		Vector4ui b = new Vector4ui(111, 2, 3, 4);
		if (a == b)
			return 0;
		return 1;
	}

	public static int test_0_set_vector_small_array () {
		uint[] array = new uint[3];

		try {
			array.SetVector (new Vector4ui (), 0);
			return 1;
		} catch (IndexOutOfRangeException) {
		}
		return 0;
	}
	
	public static int test_0_set_vector_negative_index () {
		uint[] array = new uint[4];

		try {
			array.SetVector (new Vector4ui (), -1);
			return 1;
		} catch (IndexOutOfRangeException) {
		}
		return 0;
	}

	public static int test_0_set_vector_bounds_error () {
		uint[] array = new uint[4];

		try {
			array.SetVector (new Vector4ui (), 1);
			return 1;
		} catch (IndexOutOfRangeException) {
		}
		return 0;
	}

	public static int test_0_set_vector () {
		uint[] array = new uint[10];
		Vector4ui a = new Vector4ui (11, 22, 33, 44);

		array.SetVector (a, 1);

		if (array [1] != 11)
			return 1;
		if (array [2] != 22)
			return 2;
		if (array [3] != 33)
			return 3;
		if (array [4] != 44)
			return 4;
		return 0;
	}

	public static int test_0_get_vector_small_array () {
		uint[] array = new uint[3];

		try {
			Vector4ui res = array.GetVector (0);
			return 1;
		} catch (IndexOutOfRangeException) {
		}
		return 0;
	}
	
	public static int test_0_get_vector_negative_index () {
		uint[] array = new uint[4];

		try {
			Vector4ui res = array.GetVector (-1);
			return 1;
		} catch (IndexOutOfRangeException) {
		}
		return 0;
	}

	public static int test_0_get_vector_bounds_error () {
		uint[] array = new uint[4];

		try {
			Vector4ui res = array.GetVector (1);
			return 1;
		} catch (IndexOutOfRangeException) {
		}
		return 0;
	}
	
	public static int test_0_get_vector () {
		uint[] array = new uint[] { 11, 22, 33, 44, 55, 66, 77, 88, 99, 111 };

		Vector4ui res = array.GetVector (1);

		if (res.X != 22)
			return 1;
		if (res.Y != 33)
			return 2;
		if (res.Z != 44)
			return 3;
		if (res.W != 55)
			return 4;

		return 0;
	}
	
	public static int test_0_accessor_vecto2l () {
		Vector2l a = new Vector2l (3, 2);

		if (a.X != 3)
			return 1;
		if (a.Y != 2)
			return 2;

		a.X = 500000000000055L;
		a.Y = -12345678900L;

		if (a.X != 500000000000055L)
			return 3;
		if (a.Y != -12345678900L)
			return 4;
		return 0;
	}

	public static int test_0_accessor_vecto2d () {
		Vector2d a = new Vector2d (3, 2);

		if (a.X != 3)
			return 1;
		if (a.Y != 2)
			return 2;

		a.X = 5000000000000;
		a.Y = -0.5;

		if (a.X != 5000000000000)
			return 3;
		if (a.Y != -0.5)
			return 4;
		return 0;
	}

	public static int test_0_accessor_vecto4f () {
		Vector4f a = new Vector4f (1,2,3,4);

		if (a.X != 1)
			return 1;
		if (a.Y != 2)
			return 2;
		if (a.Z != 3)
			return 3;
		if (a.W != 4)
			return 4;

		a.X = 128f;
		a.Y = 256f;
		a.Z = -0.5f;
		a.W = 0.125f;

		if (a.X != 128)
			return 5;
		if (a.Y != 256)
			return 6;
		if (a.Z != -0.5)
			return 7;
		if (a.W != 0.125)
			return 8;
		return 0;
	}

	public static int test_0_accessor_vecto4i () {
		Vector4i a = new Vector4i (0x70000000, -1, 3, 4);

		if (a.X != 0x70000000)
			return 1;
		if (a.Y != -1)
			return 2;
		if (a.Z != 3)
			return 3;
		if (a.W != 4)
			return 4;

		a.X = 11;
		a.Y = 22;
		a.Z = 33333344;
		a.W = -44444444;
		
		if (a.X != 11)
			return 5;
		if (a.Y != 22)
			return 6;
		if (a.Z != 33333344)
			return 7;
		if (a.W != -44444444)
			return 8;
		return 0;
	}

	public static int test_0_accessor_vecto4ui () {
		Vector4ui a = new Vector4ui (0xF0000000, 0xF0000, 3, 4);

		if (a.X != 0xF0000000)
			return 1;
		if (a.Y != 0xF0000)
			return 2;
		if (a.Z != 3)
			return 3;
		if (a.W != 4)
			return 4;

		a.X = 11;
		a.Y = 22;
		a.Z = 33333344;
		a.W = 44444444;

		if (a.X != 11)
			return 5;
		if (a.Y != 22)
			return 6;
		if (a.Z != 33333344)
			return 7;
		if (a.W != 44444444)
			return 8;
		return 0;
	}
	
	static float use_getter_with_byref (ref Vector4f a) {
		return a.W;
	}
 
	public static int test_0_accessor_and_byref_var () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		if (use_getter_with_byref (ref a) != 4)
			return 1;
		return 0;
	}
	
	public static unsafe int test_0_vector2ul_slr () {
		Vector2ul a = new Vector2ul (1, 6);

		Vector2ul c = a >> 1;
		if (c.X != 0)
			return 1;
		if (c.Y != 3)
			return 2;	
		return 0;
	}

	public static unsafe int test_0_vector2l_cmp_gt () {
		Vector2l a = new Vector2l (10, 5);
		Vector2l b = new Vector2l (-1, 5);

		Vector2l c = a.CompareGreaterThan (b);
	
		if (c.X != -1)
			return 1;
		if (c.Y != 0)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2l_cmp_eq () {
		Vector2l a = new Vector2l (0xFF,          5);
		Vector2l b = new Vector2l (0xFF000000FFL, 5);

		Vector2l c = a.CompareEqual (b);
	
		if (c.X != 0)
			return 1;
		if (c.Y != -1)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2l_srl () {
		Vector2l a = new Vector2l (1, 6);

		Vector2l c = a.LogicalRightShift (1);
	
		if (c.X != 0)
			return 1;
		if (c.Y != 3)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2l_unpack_high () {
		Vector2l a = new Vector2l (1, 6);
		Vector2l b = new Vector2l (3, 4);

		Vector2l c = a.UnpackHigh (b);
	
		if (c.X != 6)
			return 1;
		if (c.Y != 4)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2l_unpack_low () {
		Vector2l a = new Vector2l (1, 6);
		Vector2l b = new Vector2l (3, 4);

		Vector2l c = a.UnpackLow (b);
	
		if (c.X != 1)
			return 1;
		if (c.Y != 3)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2l_xor () {
		Vector2l a = new Vector2l (1, 6);
		Vector2l b = new Vector2l (3, 4);

		Vector2l c = a ^ b;
	
		if (c.X != 2)
			return 1;
		if (c.Y != 2)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2l_or () {
		Vector2l a = new Vector2l (1, 6);
		Vector2l b = new Vector2l (3, 4);

		Vector2l c = a | b;
	
		if (c.X != 3)
			return 1;
		if (c.Y != 6)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2l_and () {
		Vector2l a = new Vector2l (1, 6);
		Vector2l b = new Vector2l (3, 4);

		Vector2l c = a & b;
	
		if (c.X != 1)
			return 1;
		if (c.Y != 4)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2l_shl() {
		Vector2l a = new Vector2l (1, 6);

		Vector2l c = a << 3;
	
		if (c.X != 8)
			return 1;
		if (c.Y != 48)
			return 2;
		return 0;
	}
	public static unsafe int test_0_vector2l_sub() {
		Vector2l a = new Vector2l (1, 6);
		Vector2l b = new Vector2l (3, 4);

		Vector2l c = a - b;
	
		if (c.X != -2)
			return 1;
		if (c.Y != 2)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2l_add () {
		Vector2l a = new Vector2l (1, 2);
		Vector2l b = new Vector2l (3, 4);

		Vector2l c = a + b;
	
		if (c.X != 4)
			return 1;
		if (c.Y != 6)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2d_dup () {
		Vector2d a = new Vector2d (3, 2);

		Vector2d c = a.Duplicate ();
	
		if (c.X != 3)
			return 1;
		if (c.Y != 3)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2d_cmp_eq () {
		Vector2d a = new Vector2d (3, 2);
		Vector2d b = new Vector2d (3, 4);

		Vector4ui c = (Vector4ui)a.CompareEqual (b);
	
		if (c.X != 0xFFFFFFFF)
			return 1;
		if (c.Y != 0xFFFFFFFF)
			return 2;
		if (c.Z != 0)
			return 3;
		if (c.W != 0)
			return 4;
		return 0;
	}

	public static unsafe int test_0_vector2d_unpack_low () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (4, 5);

		Vector2d c = a.InterleaveLow (b);
	
		if (c.X != 1)
			return 1;
		if (c.Y != 4)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2d_unpack_high () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (4, 5);

		Vector2d c = a.InterleaveHigh (b);
	
		if (c.X != 2)
			return 1;
		if (c.Y != 5)
			return 2;
		return 0;
	}
	public static unsafe int test_0_vector2d_addsub () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (4, 1);

		Vector2d c = a.AddSub (b);
	
		if (c.X != -3)
			return 1;
		if (c.Y != 3)
			return 2;
		return 0;
	}
	public static unsafe int test_0_vector2d_hsub () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (4, 1);

		Vector2d c = a.HorizontalSub (b);
	
		if (c.X != -1)
			return 1;
		if (c.Y != 3)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2d_hadd () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (4, 0);

		Vector2d c = a.HorizontalAdd (b);
	
		if (c.X != 3)
			return 1;
		if (c.Y != 4)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2d_min () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (4, 0);

		Vector2d c = a.Min (b);
	
		if (c.X != 1)
			return 1;
		if (c.Y != 0)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2d_max () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (4, 0);

		Vector2d c = a.Max (b);
	
		if (c.X != 4)
			return 1;
		if (c.Y != 2)
			return 2;
		return 0;
	}


	public static unsafe int test_0_vector2d_andnot () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (3, 4);

		Vector4ui c = (Vector4ui)a.AndNot (b);
		Vector4ui ta = (Vector4ui)a;
		Vector4ui tb = (Vector4ui)b;
	
		if (c.X != (~ta.X & tb.X))
			return 1;
		if (c.Y != (~ta.Y & tb.Y))
			return 2;
		if (c.Z != (~ta.Z & tb.Z))
			return 3;
		if (c.W != (~ta.W & tb.W))
			return 4;
		return 0;
	}

	public static unsafe int test_0_vector2d_div () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (4, 5);

		Vector2d c = a / b;
	
		if (c.X != 0.25)
			return 1;
		if (c.Y != 0.4)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2d_mul () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (3, 5);

		Vector2d c = a * b;
	
		if (c.X != 3)
			return 1;
		if (c.Y != 10)
			return 2;
		return 0;
	}
	public static unsafe int test_0_vector2d_sub () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (3, 5);

		Vector2d c = a - b;
	
		if (c.X != -2)
			return 1;
		if (c.Y != -3)
			return 2;
		return 0;
	}
	public static unsafe int test_0_vector2d_add () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (3, 4);

		Vector2d c = a + b;
	
		if (c.X != 4)
			return 1;
		if (c.Y != 6)
			return 2;
		return 0;
	}
	public static unsafe int test_0_vector2d_xor () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (3, 4);

		Vector4ui c = (Vector4ui)(a ^ b);
		Vector4ui ta = (Vector4ui)a;
		Vector4ui tb = (Vector4ui)b;

		if (c.X != (ta.X ^ tb.X))
			return 1;
		if (c.Y != (ta.Y ^ tb.Y))
			return 2;
		if (c.Z != (ta.Z ^ tb.Z))
			return 3;
		if (c.W != (ta.W ^ tb.W))
			return 4;
		return 0;
	}

	public static unsafe int test_0_vector2d_or () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (3, 4);

		Vector4ui c = (Vector4ui)(a | b);
		Vector4ui ta = (Vector4ui)a;
		Vector4ui tb = (Vector4ui)b;
	
		if (c.X != (ta.X | tb.X))
			return 1;
		if (c.Y != (ta.Y | tb.Y))
			return 2;
		if (c.Z != (ta.Z | tb.Z))
			return 3;
		if (c.W != (ta.W | tb.W))
			return 4;
		return 0;
	}

	public static unsafe int test_0_vector2d_and () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (3, 4);

		Vector4ui c = (Vector4ui)(a & b);
		Vector4ui ta = (Vector4ui)a;
		Vector4ui tb = (Vector4ui)b;

		if (c.X != (ta.X & tb.X))
			return 1;
		if (c.Y != (ta.Y & tb.Y))
			return 2;
		if (c.Z != (ta.Z & tb.Z))
			return 3;
		if (c.W != (ta.W & tb.W))
			return 4;
		return 0;
	}

	public static unsafe int test_0_vector8s_pack_signed_sat () {
		Vector8s a = new Vector8s (-200, 200, 3, 0, 5, 6, 5, 4);
		Vector8s b = new Vector8s (9, 2, 1, 2, 3, 6, 5, 6);

		Vector16sb c = a.PackWithSignedSaturation (b);

		if (c.V0 != -128)
			return 1;
		if (c.V1 != 127)
			return 2;

		return 0;
	}

	public static unsafe int test_0_vector16sb_sub_sat () {
		Vector16sb a = new Vector16sb (100,-100,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16sb b = new Vector16sb (-100, 100,11,12,4,5,6,7,8,9,10,11,12,13,14,15);

		Vector16sb c = a.SubtractWithSaturation (b);

		if (c.V0 != 127)
			return 1;
		if (c.V1 != -128)
			return 2;
		if (c.V2 != 0)
			return 3;
		if (c.V3 != 0)
			return 4;
		if (c.V4 != 9)
			return 5;
		if (c.V5 != 9)
			return 6;
		if (c.V6 != 9)
			return 7;
		if (c.V7 != -7)
			return 8;
		return 0;
	}

	public static unsafe int test_0_vector16sb_add_sat () {
		Vector16sb a = new Vector16sb (100,-100,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16sb b = new Vector16sb (100, -100,11,12,4,5,6,7,8,9,10,11,12,13,14,15);

		Vector16sb c = a.AddWithSaturation (b);

		if (c.V0 != 127)
			return 1;
		if (c.V1 != -128)
			return 2;
		if (c.V2 != 22)
			return 3;
		if (c.V3 != 24)
			return 4;
		if (c.V4 != 17)
			return 5;
		if (c.V5 != 19)
			return 6;
		if (c.V6 != 21)
			return 7;
		if (c.V7 != 7)
			return 8;
		return 0;
	}

	public static unsafe int test_0_vector16sb_cmp_gt () {
		Vector16sb a = new Vector16sb (100,-100,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16sb b = new Vector16sb (-100, 100,11,12,4,5,6,7,8,9,10,11,12,13,14,15);

		Vector16sb c = a.CompareGreaterThan (b);

		if (c.V0 != -1)
			return 1;
		if (c.V1 != 0)
			return 2;
		if (c.V2 != 0)
			return 3;
		if (c.V3 != 0)
			return 4;
		if (c.V4 != -1)
			return 5;
		if (c.V5 != -1)
			return 6;
		if (c.V6 != -1)
			return 7;
		return 0;
	}


	public static int test_0_vector4ui_pack_with_sat () {
		Vector4ui a = new Vector4ui (0xF0000000,0xF0000,3,4);
		Vector4ui b = new Vector4ui (5,6,7,8);

		Vector8us c = a.SignedPackWithUnsignedSaturation (b);

		if (c.V0 != 0)
			return 1;
		if (c.V1 != 0xFFFF)
			return 2;
		if (c.V2 != 3)
			return 3;
		if (c.V3 != 4)
			return 4;
		if (c.V4 != 5)
			return 5;
		if (c.V5 != 6)
			return 6;
		if (c.V6 != 7)
			return 7;
		if (c.V7 != 8)
			return 8;
		return 0;
	}

	public static int test_0_vector8us_pack_with_sat () {
		Vector8us a = new Vector8us (0xFF00,1,2,3,4,5,6,7);
		Vector8us b = new Vector8us (3,4,5,6,7,8,9,10);
		Vector16b c = a.SignedPackWithUnsignedSaturation (b);

		if (c.V0 != 0)
			return 1;
		if (c.V1 != 1)
			return 2;
		if (c.V2 != 2)
			return 3;
		if (c.V8 != 3)
			return 4;
		if (c.V15 != 10)
			return 5;
		return 0;
	}

	public static int test_0_vector8us_mul_high () {
		Vector8us a = new Vector8us (0xFF00, 2, 3, 0, 5, 6, 5, 4);
		Vector8us b = new Vector8us (0xFF00, 2, 1, 2, 3, 6, 5, 6);
		Vector8us c = a.MultiplyStoreHigh (b);

		if (c.V0 != 0xFE01)
			return 1;
		if (c.V1 != 0)
			return 2;
		if (c.V2 != 0)
			return 3;
		if (c.V3 != 0)
			return 4;
		if (c.V4 != 0)
			return 5;
		if (c.V5 != 0)
			return 6;
		if (c.V6 != 0)
			return 7;
		if (c.V7 != 0)
			return 8;
		return 0;
	}

	public static int test_0_vector8us_cmpeq () {
		Vector8us a = new Vector8us (1, 2, 3, 0, 5, 6, 5, 4);
		Vector8us b = new Vector8us (9, 2, 1, 2, 3, 6, 5, 6);
		Vector8us c = a.CompareEqual (b);

		if (c.V0 != 0)
			return 1;
		if (c.V1 != 0xFFFF)
			return 2;
		if (c.V2 != 0)
			return 3;
		if (c.V3 != 0)
			return 4;
		if (c.V4 != 0)
			return 5;
		if (c.V5 != 0xFFFF)
			return 6;
		if (c.V6 != 0xFFFF)
			return 7;
		if (c.V7 != 0)
			return 8;
		return 0;
	}


	public static int test_0_vector4ui_cmpeq () {
		Vector4ui a = new Vector4ui (6,1,6,3);
		Vector4ui b = new Vector4ui (3,4,6,7);
		Vector4ui c = a.CompareEqual (b);

		if (c.X != 0)
			return 1;
		if (c.Y != 0)
			return 2;
		if (c.Z != 0xFFFFFFFF)
			return 3;
		if (c.W != 0)
			return 4;
		return 0;
	}

	public static int test_0_vector4ui_shuffle () {
		Vector4ui a = new Vector4ui (1,2,3,4);
		Vector4ui c = a.Shuffle (ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);

		if (c.X != 2)
			return 1;
		if (c.Y != 4)
			return 2;
		if (c.Z != 1)
			return 3;
		if (c.W != 3)
			return 4;
		return 0;
	}

	public static int test_0_vector4ui_min () {
		Vector4ui a = new Vector4ui (6,1,6,3);
		Vector4ui b = new Vector4ui (3,4,6,7);
		Vector4ui c = a.Min (b);

		if (c.X != 3)
			return 1;
		if (c.Y != 1)
			return 2;
		if (c.Z != 6)
			return 3;
		if (c.W != 3)
			return 4;
		return 0;
	}

	public static int test_0_vector4ui_max () {
		Vector4ui a = new Vector4ui (6,1,6,3);
		Vector4ui b = new Vector4ui (3,4,6,7);
		Vector4ui c = a.Max (b);

		if (c.X != 6)
			return 1;
		if (c.Y != 4)
			return 2;
		if (c.Z != 6)
			return 3;
		if (c.W != 7)
			return 4;
		return 0;
	}

	public static int vector16b_cmpeq () {
		Vector16b a = new Vector16b (1,0,9,0,0,0,0,0,0,0,0,0,0,0,0,1);
		Vector16b b = new Vector16b (0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0);
		Vector16b c = a.CompareEqual (b);

		if (c.V0 != 0)
			return 1;
		if (c.V1 != 0)
			return 2;
		if (c.V2 != 0)
			return 3;
		if (c.V3 != 0xff)
			return 4;
		if (c.V4 != 0xff)
			return 5;
		if (c.V5 != 0xff)
			return 6;
		if (c.V6 != 0xff)
			return 7;
		if (c.V7 != 0xff)
			return 8;
		if (c.V8 != 0xff)
			return 9;
		if (c.V9 != 0xff)
			return 10;
		if (c.V10 != 0xff)
			return 11;
		if (c.V11 != 0xff)
			return 12;
		if (c.V12 != 0xff)
			return 13;
		if (c.V13 != 0xff)
			return 14;
		if (c.V14 != 0xff)
			return 15;
		if (c.V15 != 0)
			return 16;
		return 0;
	}


	public static int vector16b_sum_abs_diff () {
		Vector16b a = new Vector16b (100,20,20,20,0,0,0,0,0,0,0,0,0,0, 0, 0);
		Vector16sb b = new Vector16sb (0,  10,10,10,0,0,0,0,0,0,0,0,0,0,10,10);
		Vector8us c = a.SumOfAbsoluteDifferences (b);

		if (c.V0 != 130)
			return 1;
		if (c.V1 != 0)
			return 2;
		if (c.V2 != 0)
			return 3;
		if (c.V3 != 0)
			return 4;
		if (c.V4 != 20)
			return 5;
		if (c.V5 != 0)
			return 6;
		if (c.V6 != 0)
			return 7;
		if (c.V7 != 0)
			return 8;
		return 0;
	}


	public static int test_0_vector16b_extract_mask () {
		Vector16b a = new Vector16b (0xF0,0,0xF0,0,0,0,0xF0,0xAA,0x0F,0,0xFF,0,0,0,0,0);
		int c = a.ExtractByteMask ();

		if (c != 0x4C5)
			return 1;
		return 0;
	}

	public static int test_0_vector16b_min () {
		Vector16b a = new Vector16b (0,12,20,12,4,5,6,7,8,9,10,11,12,13,14,15);
		Vector16b b = new Vector16b (9,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16b c = a.Min (b);

		if (c.V0 != 0)
			return 1;
		if (c.V1 != 10)
			return 2;
		if (c.V2 != 11)
			return 3;
		if (c.V3 != 12)
			return 4;
		if (c.V4 != 4)
			return 5;
		if (c.V5 != 5)
			return 6;
		if (c.V6 != 6)
			return 7;
		if (c.V7 != 0)
			return 8;
		if (c.V8 != 1)
			return 9;
		if (c.V9 != 2)
			return 10;
		if (c.V10 != 3)
			return 11;
		if (c.V11 != 4)
			return 12;
		if (c.V12 != 5)
			return 13;
		if (c.V13 != 6)
			return 14;
		if (c.V14 != 7)
			return 15;
		if (c.V15 != 8)
			return 16;
		return 0;
	}

	public static int test_0_vector16b_max () {
		Vector16b a = new Vector16b (0,12,20,12,4,5,6,7,8,9,10,11,12,13,14,15);
		Vector16b b = new Vector16b (9,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16b c = a.Max (b);

		if (c.V0 != 9)
			return 1;
		if (c.V1 != 12)
			return 2;
		if (c.V2 != 20)
			return 3;
		if (c.V3 != 12)
			return 4;
		if (c.V4 != 13)
			return 5;
		if (c.V5 != 14)
			return 6;
		if (c.V6 != 15)
			return 7;
		if (c.V7 != 7)
			return 8;
		if (c.V8 != 8)
			return 9;
		if (c.V9 != 9)
			return 10;
		if (c.V10 != 10)
			return 11;
		if (c.V11 != 11)
			return 12;
		if (c.V12 != 12)
			return 13;
		if (c.V13 != 13)
			return 14;
		if (c.V14 != 14)
			return 15;
		if (c.V15 != 15)
			return 16;
		return 0;
	}
	public static int test_0_vector16b_avg () {
		Vector16b a = new Vector16b (0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,120);
		Vector16b b = new Vector16b (9,10,11,12,13,14,15,0,1,2,3,4,5,6,7,180);
		Vector16b c = a.Average (b);

		if (c.V0 != 5)
			return 1;
		if (c.V1 != 6)
			return 2;
		if (c.V2 != 7)
			return 3;
		if (c.V3 != 8)
			return 4;
		if (c.V4 != 9)
			return 5;
		if (c.V5 != 10)
			return 6;
		if (c.V6 != 11)
			return 7;
		if (c.V7 != 4)
			return 8;
		if (c.V8 != 5)
			return 9;
		if (c.V9 != 6)
			return 10;
		if (c.V10 != 7)
			return 11;
		if (c.V11 != 8)
			return 12;
		if (c.V12 != 9)
			return 13;
		if (c.V13 != 10)
			return 14;
		if (c.V14 != 11)
			return 15;
		if (c.V15 != 150)
			return 16;
		return 0;
	}


	static unsafe Vector8us bad_method_regression_2 (Vector16b va, Vector16b vb) {
		Vector8us res = new Vector8us ();
		byte *a = (byte*)&va;
		byte *b = (byte*)&vb;

		int tmp = 0;
		for (int i = 0; i < 8; ++i)
			tmp += System.Math.Abs ((int)*a++ - (int)*b++);
		res.V0 = (ushort)tmp;

		tmp = 0;
		for (int i = 0; i < 8; ++i)
			tmp += System.Math.Abs ((int)*a++ - (int)*b++);
		res.V4 = (ushort)tmp;
		return res;
	}

	/*This bug was caused the simplifier not taking notice of LDADDR on the remaining blocks.*/
	public static int test_2_local_simplifier_regression_other_blocks () {
		Vector16b a = new Vector16b (1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1);
		Vector16b b = new Vector16b (0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0);
		Vector8us res = bad_method_regression_2 (a,b);
		return (int)res.V0 + res.V4;
	}

	static unsafe Vector8us bad_method_regression (Vector16b va, Vector16b vb) {
		Vector8us res = new Vector8us ();
		byte *a = (byte*)&va;
		byte *b = (byte*)&vb;
		*((ushort*)&res) = 10;

		int tmp = 0;
		if (*b != 0)
			tmp++;

		Vector8us dd = res;
		dd = dd + dd - dd;
		return dd;
	}

	/*This bug was caused the simplifier not taking notice of LDADDR on the first block.*/
	public static int test_10_local_simplifier_regression_first_block () {
		Vector16b a = new Vector16b ();
		Vector16b b = new Vector16b ();
		Vector8us res = bad_method_regression (a,b);
		return (int)res.V0;
	}
	
	public static int test_0_vecto8us_shuffle_low () {
		Vector8us a = new Vector8us (1, 2, 3, 4, 5, 6, 7, 8);
		Vector8us c = a.ShuffleLow (ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);

		if (c.V0 != 2)
			return 1;
		if (c.V1 != 4)
			return 2;
		if (c.V2 != 1)
			return 3;
		if (c.V3 != 3)
			return 4;
		if (c.V4 != 5)
			return 5;
		if (c.V5 != 6)
			return 6;
		if (c.V6 != 7)
			return 7;
		if (c.V7 != 8)
			return 8;
		return 0;
	}

	public static int test_0_vecto8us_shuffle_high () {
		Vector8us a = new Vector8us (1, 2, 3, 4, 5, 6, 7, 8);
		Vector8us c = a.ShuffleHigh (ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);

		if (c.V0 != 1)
			return 1;
		if (c.V1 != 2)
			return 2;
		if (c.V2 != 3)
			return 3;
		if (c.V3 != 4)
			return 4;
		if (c.V4 != 6)
			return 5;
		if (c.V5 != 8)
			return 6;
		if (c.V6 != 5)
			return 7;
		if (c.V7 != 7)
			return 8;

		return 0;
	}

	public static int test_0_vecto8us_max () {
		Vector8us a = new Vector8us (1, 2, 3, 4, 5, 6, 7, 8);
		Vector8us b = new Vector8us (9, 1, 1, 2, 9, 6, 5, 1000);
		Vector8us c = a.Max (b);

		if (c.V0 != 9)
			return 1;
		if (c.V1 != 2)
			return 2;
		if (c.V2 != 3)
			return 3;
		if (c.V3 != 4)
			return 4;
		if (c.V4 != 9)
			return 5;
		if (c.V5 != 6)
			return 6;
		if (c.V6 != 7)
			return 7;
		if (c.V7 != 1000)
			return 0;

		return 0;
	}

	public static int test_0_vecto8us_min () {
		Vector8us a = new Vector8us (1, 2, 3, 0, 5, 6, 5, 4);
		Vector8us b = new Vector8us (9, 1, 1, 2, 3, 4, 5, 6);
		Vector8us c = a.Min (b);

		if (c.V0 != 1)
			return 1;
		if (c.V1 != 1)
			return 2;
		if (c.V2 != 1)
			return 3;
		if (c.V3 != 0)
			return 4;
		if (c.V4 != 3)
			return 5;
		if (c.V5 != 4)
			return 6;
		if (c.V6 != 5)
			return 7;
		if (c.V7 != 4)
			return 8;
		return 0;
	}

	public static int test_0_vecto8us_avg () {
		Vector8us a = new Vector8us (1, 2, 3, 4, 5, 6, 7, 30000);
		Vector8us b = new Vector8us (9, 1, 1, 2, 3, 4, 5, 40000);
		Vector8us c = a.Average (b);

		if (c.V0 != 5)
			return 1;
		if (c.V1 != 2)
			return 2;
		if (c.V2 != 2)
			return 3;
		if (c.V3 != 3)
			return 4;
		if (c.V4 != 4)
			return 5;
		if (c.V5 != 5)
			return 6;
		if (c.V6 != 6)
			return 7;
		if (c.V7 != 35000)
			return 8;
		return 0;
	}

	static void store_helper (ref Vector4f x) {
		Vector4f k;
		k = new Vector4f(9,9,9,9);
		x = k;
	}

	public static int test_0_vector4f_byref_store ()
	{
		Vector4f k;
		k = new Vector4f(1,2,3,4);
		store_helper (ref k);
		if (k.X != 9)
			return 1;
		return 0;
	}

	public static int test_0_vector4f_init_array_element ()
	{
		Vector4f[] v = new Vector4f[1];
		v[0] = new Vector4f(9,9,9,9);
		if (v [0].X != 9)
			return 1;
		return 0;
	}

	public static int test_0_vector4f_dup_high () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f c = a.DuplicateHigh();

		if (c.X != 2)
			return 1;
		if (c.Y != 2)
			return 2;
		if (c.Z != 4)
			return 3;
		if (c.W != 4)
			return 4;
		return 0;
	}

	public static int test_0_vector4f_dup_low () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f c = a.DuplicateLow ();

		if (c.X != 1)
			return 1;
		if (c.Y != 1)
			return 2;
		if (c.Z != 3)
			return 3;
		if (c.W != 3)
			return 4;
		return 0;
	}


	public static int test_0_vector4f_interleave_high () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = new Vector4f (5, 6, 7, 8);
		Vector4f c = a.InterleaveHigh (b);

		if (c.X != 3)
			return 1;
		if (c.Y != 7)
			return 2;
		if (c.Z != 4)
			return 3;
		if (c.W != 8)
			return 4;
		return 0;
	}

	public static int test_0_vector4f_interleave_low () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = new Vector4f (5, 6, 7, 8);
		Vector4f c = a.InterleaveLow (b);

		if (c.X != 1)
			return 1;
		if (c.Y != 5)
			return 2;
		if (c.Z != 2)
			return 3;
		if (c.W != 6)
			return 4;
		return 0;
	}

	public static int test_0_vector4f_rcp () {
		Vector4f a = new Vector4f (1, 2, 4, 8);
		Vector4f c = a.Reciprocal ();

		//Test with ranges due to the terrible precision.
		if (c.X < (1 - 0.01f) || c.X > (1 + 0.01f))
			return 1;
		if (c.Y < (0.5 - 0.01f) || c.Y > (0.5 + 0.01f))
			return 2;
		if (c.Z < (0.25 - 0.01f) || c.Z > (0.25 + 0.01f))
			return 3;
		if (c.W < (0.125 - 0.01f) || c.W > (0.125 + 0.01f))
			return 4;
		return 0;
	}

	public static int test_0_vector4f_xor () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = new Vector4f (1, 3, 3, 8);
		Vector4f c = a ^ b;

		if (((Vector4ui)c).X != 0)
			return 1;
		if (((Vector4ui)c).Y != 0x400000)
			return 2;
		if (((Vector4ui)c).Z != 0)
			return 3;
		if (((Vector4ui)c).W != 0x1800000)
			return 4;
		return 0;
	}

	public static int test_0_vector4f_or () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = new Vector4f (1, 3, 3, 8);
		Vector4f c = a | b;

		if (((Vector4ui)c).X != 0x3F800000)
			return 1;
		if (((Vector4ui)c).Y != 0x40400000)
			return 2;
		if (((Vector4ui)c).Z != 0x40400000)
			return 3;
		if (((Vector4ui)c).W != 0x41800000)
			return 4;
		return 0;
	}
	public static int test_0_vector4f_andn () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = new Vector4f (1, 3, 3, 8);
		Vector4f c = a.AndNot (b);

		if (((Vector4ui)c).X != 0)
			return 1;
		if (((Vector4ui)c).Y != 0x400000)
			return 2;
		if (((Vector4ui)c).Z != 0)
			return 3;
		if (((Vector4ui)c).W != 0x1000000)
			return 4;
		return 0;
	}

	public static int test_0_vector4f_and () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = new Vector4f (1, 3, 3, 8);
		Vector4f c = a & b;

		if (((Vector4ui)c).X != 0x3F800000)
			return 1;
		if (((Vector4ui)c).Y != 0x40000000)
			return 2;
		if (((Vector4ui)c).Z != 0x40400000)
			return 3;
		if (((Vector4ui)c).W != 0x40000000)
			return 4;
		return 0;
	}

	public static int test_0_vector4f_cmpord () {
		Vector4f a = new Vector4f (float.NaN, 2,         3, 4);
		Vector4f b = new Vector4f (1,         float.NaN, 3, 6);
		Vector4f c = a.CompareOrdered (b);

		if (((Vector4ui)c).X != 0)
			return 1;
		if (((Vector4ui)c).Y != 0)
			return 2;
		if (((Vector4ui)c).Z != 0xFFFFFFFF)
			return 3;
		if (((Vector4ui)c).W != 0xFFFFFFFF)
			return 4;
		return 0;
	}

	public static int test_0_vector4f_cmpnle () {
		Vector4f a = new Vector4f (float.NaN, 2,         3, 4);
		Vector4f b = new Vector4f (1,         float.NaN, 3, 6);
		Vector4f c = a.CompareNotLessEqual (b);

		if (((Vector4ui)c).X != 0xFFFFFFFF)
			return 1;
		if (((Vector4ui)c).Y != 0xFFFFFFFF)
			return 2;
		if (((Vector4ui)c).Z != 0)
			return 3;
		if (((Vector4ui)c).W != 0)
			return 4;
		return 0;
	}

	public static int test_0_vector4f_cmpnlt () {
		Vector4f a = new Vector4f (float.NaN, 2,         3, 4);
		Vector4f b = new Vector4f (1,         float.NaN, 3, 6);
		Vector4f c = a.CompareNotLessThan (b);

		if (((Vector4ui)c).X != 0xFFFFFFFF)
			return 1;
		if (((Vector4ui)c).Y != 0xFFFFFFFF)
			return 2;
		if (((Vector4ui)c).Z != 0xFFFFFFFF)
			return 3;
		if (((Vector4ui)c).W != 0)
			return 4;
		return 0;
	}

	public static int test_0_vector4f_cmpneq () {
		Vector4f a = new Vector4f (float.NaN, 2,         3, 4);
		Vector4f b = new Vector4f (1,         float.NaN, 3, 6);
		Vector4f c = a.CompareNotEqual (b);

		if (((Vector4ui)c).X != 0xFFFFFFFF)
			return 1;
		if (((Vector4ui)c).Y != 0xFFFFFFFF)
			return 2;
		if (((Vector4ui)c).Z != 0)
			return 3;
		if (((Vector4ui)c).W != 0xFFFFFFFF)
			return 4;
		return 0;
	}

	public static int test_0_vector4f_cmpunord () {
		Vector4f a = new Vector4f (float.NaN, 2,         3, 4);
		Vector4f b = new Vector4f (1,         float.NaN, 3, 6);
		Vector4f c = a.CompareUnordered (b);

		if (((Vector4ui)c).X != 0xFFFFFFFF)
			return 1;
		if (((Vector4ui)c).Y != 0xFFFFFFFF)
			return 2;
		if (((Vector4ui)c).Z != 0)
			return 3;
		if (((Vector4ui)c).W != 0)
			return 4;
		return 0;
	}

	public static int test_0_vector4f_cmple () {
		Vector4f a = new Vector4f (float.NaN, 2,         3, 4);
		Vector4f b = new Vector4f (1,         float.NaN, 3, 6);
		Vector4f c = a.CompareLessEqual (b);

		if (((Vector4ui)c).X != 0)
			return 1;
		if (((Vector4ui)c).Y != 0)
			return 2;
		if (((Vector4ui)c).Z != 0xFFFFFFFF)
			return 3;
		if (((Vector4ui)c).W != 0xFFFFFFFF)
			return 4;
		return 0;
	}

	public static int test_0_vector4f_cmplt () {
		Vector4f a = new Vector4f (float.NaN, 2,         3, 4);
		Vector4f b = new Vector4f (1,         float.NaN, 3, 6);
		Vector4f c = a.CompareLessThan (b);

		if (((Vector4ui)c).X != 0)
			return 1;
		if (((Vector4ui)c).Y != 0)
			return 2;
		if (((Vector4ui)c).Z != 0)
			return 3;
		if (((Vector4ui)c).W != 0xFFFFFFFF)
			return 4;
		return 0;
	}

	public static int test_0_vector4f_cmpeq () {
		Vector4f a = new Vector4f (float.NaN, 2,         3, 6);
		Vector4f b = new Vector4f (1,         float.NaN, 3, 4);
		Vector4f c = a.CompareEqual (b);

		if (((Vector4ui)c).X != 0)
			return 1;
		if (((Vector4ui)c).Y != 0)
			return 2;
		if (((Vector4ui)c).Z != 0xFFFFFFFF)
			return 3;
		if (((Vector4ui)c).W != 0)
			return 4;
		return 0;
	}

	public static int test_0_vector4ui_sar () {
		Vector4ui a = new Vector4ui (0xF0000000u,20,3,40);
		
		Vector4ui c = a.ArithmeticRightShift (2);
	
		if (c.X != 0xFC000000)
			return 1;
		if (c.Y != 5)
			return 2;
		if (c.Z != 0)
			return 3;
		if (c.W != 10)
			return 4;
		return 0;
	}

	public static int test_0_vector4ui_unpack_high () {
		Vector4ui a = new Vector4ui (1,2,3,4);
		Vector4ui b = new Vector4ui (5,6,7,8);
		
		Vector4ui c = a.UnpackHigh(b);
	
		if (c.X != 3)
			return 1;
		if (c.Y != 7)
			return 2;
		if (c.Z != 4)
			return 3;
		if (c.W != 8)
			return 4;
		return 0;
	}

	public  static int test_0_vector4ui_unpack_low () {
		Vector4ui a = new Vector4ui (1,2,3,4);
		Vector4ui b = new Vector4ui (5,6,7,8);
		
		Vector4ui c = a.UnpackLow (b);
	
		if (c.X != 1)
			return 1;
		if (c.Y != 5)
			return 2;
		if (c.Z != 2)
			return 3;
		if (c.W != 6)
			return 4;
		return 0;
	}

	public  static int test_0_vector4ui_xor () {
		Vector4ui a = new Vector4ui (1,2,3,4);
		Vector4ui b = new Vector4ui (7,5,3,1);
		
		Vector4ui c = a ^ b;
	
		if (c.X != 6)
			return 1;
		if (c.Y != 7)
			return 2;
		if (c.Z != 0)
			return 3;
		if (c.W != 5)
			return 4;
		return 0;
	}

	public  static int test_0_vector4ui_or () {
		Vector4ui a = new Vector4ui (1,2,3,4);
		Vector4ui b = new Vector4ui (7,5,3,1);
		
		Vector4ui c = a | b;
	
		if (c.X != 7)
			return 1;
		if (c.Y != 7)
			return 2;
		if (c.Z != 3)
			return 3;
		if (c.W != 5)
			return 4;
		return 0;
	}
	public  static int test_0_vector4ui_and () {
		Vector4ui a = new Vector4ui (1,2,3,4);
		Vector4ui b = new Vector4ui (7,5,3,1);
		
		Vector4ui c = a & b;
	
		if (c.X != 1)
			return 1;
		if (c.Y != 0)
			return 2;
		if (c.Z != 3)
			return 3;
		if (c.W != 0)
			return 4;
		return 0;
	}

	public  static int test_0_vector4ui_shr () {
		Vector4ui a = new Vector4ui (0xF0000000u,20,3,40);
		
		Vector4ui c = a >> 2;
	
		if (c.X != 0x3C000000)
			return 1;
		if (c.Y != 5)
			return 2;
		if (c.Z != 0)
			return 3;
		if (c.W != 10)
			return 4;
		return 0;
	}

	public  static int test_0_vector4ui_shl () {
		Vector4ui a = new Vector4ui (10,20,3,40);
		
		Vector4ui c = a << 2;
	
		if (c.X != 40)
			return 1;
		if (c.Y != 80)
			return 2;
		if (c.Z != 12)
			return 3;
		if (c.W != 160)
			return 4;
		return 0;
	}

	public  static int test_0_vector4ui_mul () {
		Vector4ui a = new Vector4ui (0x8888,20,3,40);
		Vector4ui b = new Vector4ui (0xFF00FF00u,2,3,4);
		
		Vector4ui c = a * b;
	
		if (c.X != 0xffff7800)
			return 1;
		if (c.Y != 40)
			return 2;
		if (c.Z != 9)
			return 3;
		if (c.W != 160)
			return 4;
		return 0;
	}
	public  static int test_0_vector4ui_sub () {
		Vector4ui a = new Vector4ui (1,20,3,40);
		Vector4ui b = new Vector4ui (0xFF00FF00u,2,3,4);
		
		Vector4ui c = a - b;
	
		if (c.X != 0xff0101)
			return 1;
		if (c.Y != 18)
			return 2;
		if (c.Z != 0)
			return 3;
		if (c.W != 36)
			return 4;
		return 0;
	}

	public  static int test_0_vector4ui_add () {
		Vector4ui a = new Vector4ui (0xFF00FF00u,2,3,4);
		Vector4ui b = new Vector4ui (0xFF00FF00u,2,3,4);
		
		Vector4ui c = a + b;
	
		if (c.X != 0xfe01fe00)
			return 1;
		if (c.Y != 4)
			return 2;
		if (c.Z != 6)
			return 3;
		if (c.W != 8)
			return 4;
		return 0;
	}


	static int test_0_vector4ui_accessors () {
		Vector4ui a = new Vector4ui (1,2,3,4);

		if (a.X != 1)
			return 1;
		if (a.Y != 2)
			return 2;
		if (a.Z != 3)
			return 3;
		if (a.W != 4)
			return 4;
		a.X = 10;
		a.Y = 20;
		a.Z = 30;
		a.W = 40;

		if (a.X != 10)
			return 5;
		if (a.Y != 20)
			return 6;
		if (a.Z != 30)
			return 7;
		if (a.W != 40)
			return 8;
		return 0;
	}

	static int test_0_vector8us_sub_sat () {
		Vector8us a = new Vector8us (0xF000,1,20,3,4,5,6,7);
		Vector8us b = new Vector8us (0xFF00,4,5,6,7,8,9,10);
		Vector8us c = a.SubtractWithSaturation (b);

		if (c.V0 != 0)
			return 1;
		if (c.V1 != 0)
			return 2;
		if (c.V2 != 15)
			return 3;
		if (c.V3 != 0)
			return 4;
		if (c.V4 != 0)
			return 5;
		if (c.V5 != 0)
			return 6;
		if (c.V6 != 0)
			return 7;
		if (c.V7 != 0)
			return 8;
		return 0;
	}

	static int test_0_vector8us_add_sat () {
		Vector8us a = new Vector8us (0xFF00,1,2,3,4,5,6,7);
		Vector8us b = new Vector8us (0xFF00,4,5,6,7,8,9,10);
		Vector8us c = a.AddWithSaturation (b);

		if (c.V0 != 0xFFFF)
			return 1;
		if (c.V1 != 5)
			return 2;
		if (c.V2 != 7)
			return 3;
		if (c.V3 != 9)
			return 4;
		if (c.V4 != 11)
			return 5;
		if (c.V5 != 13)
			return 6;
		if (c.V6 != 15)
			return 7;
		if (c.V7 != 17)
			return 8;
		return 0;
	}

	static int test_0_vector8us_unpack_low () {
		Vector8us a = new Vector8us (0,1,2,3,4,5,6,7);
		Vector8us b = new Vector8us (3,4,5,6,7,8,9,10);
		Vector8us c = a.UnpackLow (b);

		if (c.V0 != 0)
			return 1;
		if (c.V1 != 3)
			return 2;
		if (c.V2 != 1)
			return 3;
		if (c.V3 != 4)
			return 4;
		if (c.V4 != 2)
			return 5;
		if (c.V5 != 5)
			return 6;
		if (c.V6 != 3)
			return 7;
		if (c.V7 != 6)
			return 8;
		return 0;
	}


	static int test_0_vector8us_shift_left () {
		Vector8us a = new Vector8us (0xFF00,1,2,3,4,5,6,7);
		int amt = 2;
		Vector8us c = a << amt;
	
		if (c.V0 != 0xFC00)
			return 1;
		if (c.V1 != 4)
			return 2;
		if (c.V7 != 28)
			return 3;
		return 0;
	}
	
	static int test_0_vector8us_shift_right_arithmetic () {
		Vector8us a = new Vector8us (0xFF00,1,2,3,4,5,6,7);
		int amt = 2;
		Vector8us c = a.ArithmeticRightShift (amt);
	
		if (c.V0 != 0xFFC0)
			return 1;
		if (c.V1 != 0)
			return 2;
		if (c.V7 != 1)
			return 3;
		return 0;
	}

	static int test_0_vector8us_shift_variable_offset () {
		int off = 2;
		Vector8us a = new Vector8us (0xF000,1,2,3,4,5,6,7);
		Vector8us b = a;
		Vector8us c = b >> off;
		a = b + b;

		if (c.V0 != 0x3C00)
			return 1;
		if (c.V1 != 0)
			return 2;
		if (c.V7 != 1)
			return 3;
		if (a.V1 != 2)
			return 4;
		if (a.V7 != 14)
			return 5;
		return 0;
	}
	
	
	static int test_0_vector8us_shift_operand_is_live_after_op () {
		Vector8us a = new Vector8us (0xF000,1,2,3,4,5,6,7);
		Vector8us b = a;
		Vector8us c = b >> 2;
		a = b + b;

		if (c.V0 != 0x3C00)
			return 1;
		if (c.V1 != 0)
			return 2;
		if (c.V7 != 1)
			return 3;
		if (a.V1 != 2)
			return 4;
		if (a.V7 != 14)
			return 5;
		return 0;
	}

	static int test_0_vector8us_shr_constant () {
		Vector8us a = new Vector8us (0xF000,1,2,3,4,5,6,7);
		Vector8us c = a >> 2;

		if (c.V0 != 0x3C00)
			return 1;
		if (c.V1 != 0)
			return 2;
		if (c.V7 != 1)
			return 3;
		return 0;
	}

	static int test_0_vector8us_mul () {
		Vector8us a = new Vector8us (0x0F00,4,5,6,7,8,9,10);
		Vector8us b = new Vector8us (0x0888,1,2,3,4,5,6,8);

		Vector8us c = a * b;
		if (c.V0 != 63488)
			return 1;
		if (c.V1 != 4)
			return 2;
		if (c.V7 != 80)
			return 3;
		return 0;
	}

	static int test_0_vector8us_add () {
		Vector8us a = new Vector8us (0xFF00,4,5,6,7,8,9,10);
		Vector8us b = new Vector8us (0x8888,1,2,3,4,5,6,8);

		Vector8us c = a + b;
		if (c.V0 != 34696)
			return 1;
		if (c.V1 != 5)
			return 2;
		if (c.V7 != 18)
			return 3;
		return 0;
	}


	static int test_0_vector8us_sub () {
		Vector8us a = new Vector8us (3,4,5,6,7,8,9,10);
		Vector8us b = new Vector8us (10,1,2,3,4,5,6,8);

		Vector8us c = a - b;

		if (c.V0 != 65529)
			return 1;
		if (c.V1 != 3)
			return 2;
		if (c.V7 != 2)
			return 3;
		return 0;
	}


	static int test_0_vector8us_accessors () {
		Vector8us a = new Vector8us (0,1,2,3,4,5,6,7);

		if (a.V0 != 0)
			return 1;
		if (a.V1 != 1)
			return 2;
		if (a.V2 != 2)
			return 3;
		if (a.V3 != 3)
			return 4;
		if (a.V4 != 4)
			return 5;
		if (a.V5 != 5)
			return 6;
		if (a.V6 != 6)
			return 7;
		if (a.V7 != 7)
			return 8;
		a.V0 = 10;
		a.V1 = 20;
		a.V2 = 30;
		a.V3 = 40;
		a.V4 = 50;
		a.V5 = 60;
		a.V6 = 70;
		a.V7 = 80;

		if (a.V0 != 10)
			return 17;
		if (a.V1 != 20)
			return 18;
		if (a.V2 != 30)
			return 19;
		if (a.V3 != 40)
			return 20;
		if (a.V4 != 50)
			return 21;
		if (a.V5 != 60)
			return 22;
		if (a.V6 != 70)
			return 23;
		if (a.V7 != 80)
			return 24;

		return 0;
	}


	static int test_0_vector16b_unpack_high () {
		Vector16b a = new Vector16b (0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);
		Vector16b b = new Vector16b (9,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16b c = a.UnpackHigh (b);

		if (c.V0 != 8)
			return 1;
		if (c.V1 != 1)
			return 2;
		if (c.V2 != 9)
			return 3;
		if (c.V3 != 2)
			return 4;
		if (c.V4 != 10)
			return 5;
		if (c.V5 != 3)
			return 6;
		if (c.V14 != 15)
			return 7;
		if (c.V15 != 8)
			return 8;
		return 0;
	}

	static int test_0_vector16b_unpack_low () {
		Vector16b a = new Vector16b (0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);
		Vector16b b = new Vector16b (9,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16b c = a.UnpackLow (b);

		if (c.V0 != 0)
			return 1;
		if (c.V1 != 9)
			return 2;
		if (c.V2 != 1)
			return 3;
		if (c.V3 != 10)
			return 4;
		if (c.V4 != 2)
			return 5;
		if (c.V5 != 11)
			return 6;
		if (c.V14 != 7)
			return 7;
		if (c.V15 != 0)
			return 8;
		return 0;
	}

	static int test_0_vector16b_sub_sat () {
		Vector16b a = new Vector16b (100,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16b b = new Vector16b (200,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);
		Vector16b c = a.SubtractWithSaturation (b);

		if (c.V0 != 0)
			return 1;
		if (c.V1 != 9)
			return 2;
		if (c.V15 != 0)
			return 3;
		return 0;
	}
	
	static int test_0_vector16b_add_sat () {
		Vector16b a = new Vector16b (200,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);
		Vector16b b = new Vector16b (200,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16b c = a.AddWithSaturation (b);

		if (c.V0 != 255)
			return 1;
		if (c.V1 != 11)
			return 2;
		if (c.V15 != 23)
			return 3;
		return 0;
	}

	static int test_0_vector16b_add_ovf () {
		Vector16b a = new Vector16b (200,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);
		Vector16b b = new Vector16b (200,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16b c = a + b;

		if (c.V0 != 144)
			return 1;
		if (c.V1 != 11)
			return 2;
		if (c.V15 != 23)
			return 3;
		return 0;
	}

	static int test_0_vector16b_accessors () {
		Vector16b a = new Vector16b (0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);

		if (a.V0 != 0)
			return 1;
		if (a.V1 != 1)
			return 2;
		if (a.V2 != 2)
			return 3;
		if (a.V3 != 3)
			return 4;
		if (a.V4 != 4)
			return 5;
		if (a.V5 != 5)
			return 6;
		if (a.V6 != 6)
			return 7;
		if (a.V7 != 7)
			return 8;
		if (a.V8 != 8)
			return 9;
		if (a.V9 != 9)
			return 10;
		if (a.V10 != 10)
			return 11;
		if (a.V11 != 11)
			return 12;
		if (a.V12 != 12)
			return 13;
		if (a.V13 != 13)
			return 14;
		if (a.V14 != 14)
			return 15;
		if (a.V15 != 15)
			return 16;

		a.V0 = 10;
		a.V1 = 20;
		a.V2 = 30;
		a.V3 = 40;
		a.V4 = 50;
		a.V5 = 60;
		a.V6 = 70;
		a.V7 = 80;
		a.V8 = 90;
		a.V9 = 100;
		a.V10 = 110;
		a.V11 = 120;
		a.V12 = 130;
		a.V13 = 140;
		a.V14 = 150;
		a.V15 = 160;

		if (a.V0 != 10)
			return 17;
		if (a.V1 != 20)
			return 18;
		if (a.V2 != 30)
			return 19;
		if (a.V3 != 40)
			return 20;
		if (a.V4 != 50)
			return 21;
		if (a.V5 != 60)
			return 22;
		if (a.V6 != 70)
			return 23;
		if (a.V7 != 80)
			return 24;
		if (a.V8 != 90)
			return 25;
		if (a.V9 != 100)
			return 26;
		if (a.V10 != 110)
			return 27;
		if (a.V11 != 120)
			return 28;
		if (a.V12 != 130)
			return 29;
		if (a.V13 != 140)
			return 30;
		if (a.V14 != 150)
			return 31;
		if (a.V15 != 160)
			return 32;
		return 0;
	}

	public static int test_0_accessors () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		if (a.X != 1f)
			return 1;
		if (a.Y != 2f)
			return 2;
		if (a.Z != 3f)
			return 3;
		if (a.W != 4f)
			return 4;
		return 0;
	}

	public static int test_0_packed_add_with_stack_tmp () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = new Vector4f (5, 6, 7, 8);
		Vector4f c = new Vector4f (-1, -3, -4, -5);
		Vector4f d = a + b + c;
		if (d.X != 5f)
			return 1;
		if (d.Y != 5f)
			return 2;
		if (d.Z != 6f)
			return 3;
		if (d.W != 7f)
			return 4;
		return 0;
	}

	public static int test_0_simple_packed_add () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = new Vector4f (5, 6, 7, 8);
		Vector4f c;
		c = a + b;
		if (c.X != 6f)
			return 1;
		if (c.Y != 8f)
			return 2;
		if (c.Z != 10f)
			return 3;
		if (c.W != 12f)
			return 4;
		return 0;
	}

	public static int test_0_simple_packed_sub () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = new Vector4f (5, 6, 7, 8);
		Vector4f c = b - a;
		if (c.X != 4f)
			return 1;
		if (c.Y != 4f)
			return 2;
		if (c.Z != 4f)
			return 3;
		if (c.W != 4f)
			return 4;
		return 0;
	}

	public static int test_0_simple_packed_mul () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = new Vector4f (5, 6, 7, 8);
		Vector4f c = b * a;
		if (c.X != 5f)
			return 1;
		if (c.Y != 12f)
			return 2;
		if (c.Z != 21f)
			return 3;
		if (c.W != 32f)
			return 4;
		return 0;
	}

	public static int test_0_simple_packed_div () {
		Vector4f a = new Vector4f (2, 2, 3, 4);
		Vector4f b = new Vector4f (20, 10, 33, 12);
		Vector4f c = b / a;
		if (c.X != 10f)
			return 1;
		if (c.Y != 5f)
			return 2;
		if (c.Z != 11f)
			return 3;
		if (c.W != 3f)
			return 4;
		return 0;
	}

	public static int test_0_simple_packed_sqrt () {
		Vector4f a = new Vector4f (16, 4, 9, 25);
		a = a.Sqrt ();
		if (a.X != 4f)
			return 1;
		if (a.Y != 2f)
			return 2;
		if (a.Z != 3f)
			return 3;
		if (a.W != 5f)
			return 4;
		return 0;
	}

	public static int test_0_simple_packed_invsqrt () {
		Vector4f a = new Vector4f (16, 4, 100, 25);
		//this function has VERY low precision
		a = a.InvSqrt ();
		if (a.X < (1/4f - 0.01f) || a.X > (1/4f + 0.01f))
			return 1;
		if (a.Y < (1/2f - 0.01f) || a.Y > (1/2f + 0.01f))
			return 2;
		if (a.Z < (1/10f - 0.01f) || a.Z > (1/10f + 0.01f))
			return 3;
		if (a.W < (1/5f - 0.01f) || a.W > (1/5f + 0.01f))
			return 4;
		return 0;
	}

	public static int test_0_simple_packed_min () {
		Vector4f a = new Vector4f (16, -4, 9, 25);
		Vector4f b = new Vector4f (5, 3, 9, 0);
		Vector4f c = a.Min (b);
		if (c.X != 5f)
			return 1;
		if (c.Y != -4f)
			return 2;
		if (c.Z != 9f)
			return 3;
		if (c.W != 0f)
			return 4;
		return 0;
	}

	public static int test_0_simple_packed_max () {
		Vector4f a = new Vector4f (16, -4, 9, 25);
		Vector4f b = new Vector4f (5, 3, 9, 0);
		Vector4f c = a.Max (b);
		if (c.X != 16f)
			return 1;
		if (c.Y != 3f)
			return 2;
		if (c.Z != 9f)
			return 3;
		if (c.W != 25f)
			return 4;
		return 0;
	}

	public static int test_0_simple_packed_hadd () {
		Vector4f a = new Vector4f (5, 5, 6, 6);
		Vector4f b = new Vector4f (7, 7, 8, 8);
		Vector4f c = a.HorizontalAdd (b);
		if (c.X != 10f)
			return 1;
		if (c.Y != 12f)
			return 2;
		if (c.Z != 14f)
			return 3;
		if (c.W != 16f)
			return 4;
		return 0;
	}

	public static int test_0_simple_packed_hsub () {
		Vector4f a = new Vector4f (5, 2, 6, 1);
		Vector4f b = new Vector4f (7, 0, 8, 3);
		Vector4f c = a.HorizontalSub (b);
		if (c.X != 3f)
			return 1;
		if (c.Y != 5f)
			return 2;
		if (c.Z != 7f)
			return 3;
		if (c.W != 5f)
			return 4;
		return 0;
	}

	public static int test_0_simple_packed_addsub () {
		Vector4f a = new Vector4f (5, 2, 6, 1);
		Vector4f b = new Vector4f (7, 0, 8, 3);
		Vector4f c = a.AddSub (b);
		if (c.X != -2f)
			return 1;
		if (c.Y != 2f)
			return 2;
		if (c.Z != -2f)
			return 3;
		if (c.W != 4f)
			return 4;
		return 0;
	}

	public static int test_0_simple_packed_shuffle () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		a = a.Shuffle(ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);
		if (a.X != 2f)
			return 1;
		if (a.Y != 4f)
			return 2;
		if (a.Z != 1f)
			return 3;
		if (a.W != 3f)
			return 4;
		return 0;
	}

	public static int test_0_packed_shuffle_with_reg_pressure () {
		Vector4f v = new Vector4f (1, 2, 3, 4);
		Vector4f m0 = v + v, m1 = v - v, m2 = v * v, m3 = v + v + v;
		if (ff) v = v + v -v	;

		Vector4f r0 = v.Shuffle (ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);
		Vector4f r1 = v.Shuffle (ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);
		Vector4f x = v.Shuffle (ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);
		Vector4f r2 = v.Shuffle (ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);
		Vector4f r3 = v.Shuffle (ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);
		Vector4f a = x;

		r0 = r0 * m0 + x;
		r1 = r1 * m1 + x;
		x = x - v + v;
		r2 = r2 * m2 + x;
		r3 = r3 * m3 + x;
		Vector4f result = r0 + r1 + r2 + r3;

		if (a.X != 2f)
			return 1;
		if (a.Y != 4f)
			return 2;
		if (a.Z != 1f)
			return 3;
		if (a.W != 3f)
			return 4;
		if (result.Y != result.Y)
			return 0;
		return 0;
	}
	
	public static int test_0_double_packed_sqrt () {
		Vector2d a = new Vector2d (16, 4);
		a = a.Sqrt ();
		if (a.X != 4f)
			return 1;
		if (a.Y != 2f)
			return 2;
		return 0;
	}

	public static int test_24_regs_pressure_a () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = a + a;
		Vector4f c = b * a;
		Vector4f d = a - b;
		c = a + b + c + d;
		return (int)c.Z;
	}

	public static int test_54_regs_pressure_b () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = a + a;
		Vector4f c = b - a;
		Vector4f d = c - a;
		Vector4f e = a + b + c;
		Vector4f f = d - b + a - c;
		Vector4f g = a - d * f - c + b;
		Vector4f h = a * b - c + e;
		Vector4f i = h - g - f - e - d - c - b - a;
		Vector4f j = a + b + c + d + e + f + g + h + i;
		return (int)j.Z;
	}

	public static int test_8_regs_pressure_c () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = a + a;
		Vector4f c = b - a;
		Vector4f d = c - a;
		Vector4f e = a + b + c;
		Vector4f f = d - b + a - c;
		Vector4f g = a - d * f - c + b;
		Vector4f h = a * b - c + e;
		Vector4f i = h - g - f - e - d - c - b - a;
		Vector4f j = a + b + c + d + e + f + g + h + i;
		Vector4f k = j - i - h + e + d - a + b - f + g;
		Vector4f l = k * c - j * b - i * e + f - g; 
		Vector4f m = l - k + j - i + e + f;
		Vector4f n = m - j + g - i + e * b + a * d;
		Vector4f o = k + j + i * b;
		Vector4f p = m + j + i + e + l;
		Vector4f q = l * m + j + k;
		Vector4f r = p * a + o * b + j * c + m * d + l * e;
		Vector4f s = a - b - c - d - e - f - g - h - i - j - k - l - m - p - o - q - r;
		Vector4f t = a + b + c + d + e + f + g + h + i + j + k + l + m + p + o + q + r + s;
		return (int)t.W;
	}

	public static int test_0_regs_pressure_fp_and_simd_share_bank_1 () {
		Vector4f a = new Vector4f (4, 3, 2, 1);
		float aF = 10f;
		Vector4f b = a + a;
		float bF = aF + aF;
		Vector4f c = b - a;
		float cF = bF - aF;
		Vector4f d = c - a;
		float dF = cF - aF;
		Vector4f e = a + b + c;
		float eF = aF + bF + cF;
		Vector4f f = d - b + a - c;
		float fF = dF - bF + aF - cF;
		Vector4f g = a - d * f - c + b;
		float gF = aF - dF * fF - cF + bF;
		Vector4f h = a * b - c + e;
		float hF = aF * bF - cF + eF;
		Vector4f i = h - g - f - e - d - c - b - a;
		float iF = hF - gF - fF - eF - dF - cF - bF - aF;
		Vector4f j = a + b + c + d + e + f + g + h + i;
		float jF = aF + bF + cF + dF + eF + fF + gF + hF + iF;

		if (j.X != 88f)
			return 1;

		if(jF != 460f)
			return 2;

		return 0;
	}

#if FALSE
	// Fails with -O=float32
	public static int test_0_regs_pressure_fp_and_simd_share_bank_2 () {
		Vector4f a = new Vector4f (4, 3, 2, 1);
		float aF = 10f;
		Vector4f b = a + a;
		float bF = aF + aF;
		Vector4f c = b - a;
		float cF = bF - aF;
		Vector4f d = c - a;
		float dF = cF - aF;
		Vector4f e = a + b + c;
		float eF = aF + bF + cF;
		Vector4f f = d - b + a - c;
		float fF = dF - bF + aF - cF;
		Vector4f g = a - d * f - c + b;
		float gF = aF - dF * fF - cF + bF;
		Vector4f h = a * b - c + e;
		float hF = aF * bF - cF + eF;
		Vector4f i = h - g - f - e - d - c - b - a;
		float iF = hF - gF - fF - eF - dF - cF - bF - aF;
		Vector4f j = a + b + c + d + e + f + g + h + i;
		float jF = aF + bF + cF + dF + eF + fF + gF + hF + iF;
		Vector4f k = j - i - h + e + d - a + b - f + g;
		float kF = jF - iF - hF + eF + dF - aF + bF - fF + gF;
		Vector4f l = k * c - j * b - i * e + f - g; 
		float lF = kF * cF - jF * bF - iF * eF + fF - gF;
		Vector4f m = l - k + j - i + e + f;
		float mF = lF - kF + jF - iF + eF + fF;
		Vector4f n = m - j + g - i + e * b + a * d;
		float nF = mF - jF + gF - iF + eF * bF + aF * dF;
		Vector4f o = k + j + i * b;
		float oF = kF + jF + iF * bF;
		Vector4f p = m + j + i + e + l;
		float pF = mF + jF + iF + eF + lF;
		Vector4f q = l * m + j + k;
		float qF = lF * mF + jF + kF;
		Vector4f r = p * a + o * b + j * c + m * d + l * e;
		float rF = pF * aF + oF * bF + jF * cF + mF * dF + lF * eF;
		Vector4f s = a - b - c - d - e - f - g - h - i - j - k - l - m - p - o - q - r;
		float sF = aF - bF - cF - dF - eF - fF - gF - hF - iF - jF - kF - lF - mF - pF - oF - qF - rF;
		Vector4f t = a + b + c + d + e + f + g + h + i + j + k + l + m + p + o + q + r + s;
		float tF = aF + bF + cF + dF + eF + fF + gF + hF + iF + jF + kF + lF + mF + pF + oF + qF + rF + sF;

		if (t.X != 8f)
			return 1;

		if(tF != 14f)
			return 2;

		return 0;
	}
#endif

	public static void call_simd_fp () {
		Vector4f a = new Vector4f (20f, 22f, 23f, 24f);
		float b = 25f;
		Vector4f c = new Vector4f (26f, 27f, 28f, 29f);
		float d = 30f;

		b += d;
		a += c;
	}
	public static int test_0_call_fp_and_simd_share_bank () {

		float a = 1f;
		Vector4f b = new Vector4f (2f, 3f, 4f, 5f);
		float c = 6f;
		Vector4f d = new Vector4f (7f, 8f, 9f, 10f);

		a += c;

		b += d;
		
		call_simd_fp ();
		if (a != 7f)
			return 1;
		if (b.X != 9f)
			return 2;
		if (c != 6f)
			return 3;
		if (d.X != 7f)
			return 4;
		if (b.W != 15f)
			return 5;
		if (d.W != 10f)
			return 6;
		

		return 0;
	}


	static bool ff;
	public static int test_3_single_block_var_is_properly_promoted () {
		Vector4f a = new Vector4f (4, 5, 6, 7);
		if (ff)
			a = a - a;
		else {
			Vector4f b = new Vector4f (1, 2, 3, 4);
			Vector4f c = b;
			a = a - b;
			if (ff) {
				c = a;
				a = c;
			}
		}
		return (int)a.X;
	}

	static float float_val = 45f;

	public static int test_0_sse2_opt_and_simd_intrinsic_proper_regalloc () {
		Vector4f v = new Vector4f (1, 2, 3, 4);
		float f = float_val;
		int x = (int)f;
		if (v.X != 1f)
			return 1;
		if (x != 45f)
			return 2;
		return 0;
	}

	public static int test_0_sse41_vector8s_min () {
		Vector8s v = new Vector8s(2);
		Vector8s v2 = new Vector8s(1);
		v = v.Min(v2);
		if (v.V0 != 1 || v.V1 != 1 || v.V2 != 1 || v.V3 != 1 || v.V4 != 1 || v.V5 != 1 || v.V6 != 1 || v.V7 != 1)
			return 1;
		return 0;
	}

	public static int test_0_simd_const_indexer_simple () {
		Vector4f v = new Vector4f (1, 2, 3, 4);
		
		if (v[0] != 1) 
			return 1;
		if (v[1] != 2) 
			return 2;
		if (v[2] != 3) 
			return 3;
		if (v[3] != 4) 
			return 4;
		return 0;
	}

	public static int test_0_simd_var_indexer_simple () {
		Vector4f v = new Vector4f (1, 2, 3, 4);

		int index = 0;
		
		if (v[index++] != 1) 
			return 1;
		if (v[index++] != 2) 
			return 2;
		if (v[index++] != 3) 
			return 3;
		if (v[index] != 4) 
			return 4;
		return 0;
	}

	public static int test_0_simd_const_indexer_double () {
		Vector2d v = new Vector2d (1, 2);
		
		if (v[0] != 1) 
			return 1;
		if (v[1] != 2) 
			return 2;
		return 0;
	}

	public static int test_0_simd_var_indexer_double () {
		Vector2d v = new Vector2d (1, 2);

		int index = 0;
		
		if (v[index++] != 1) 
			return 1;
		if (v[index] != 2) 
			return 2;
		return 0;
	}


	public static int test_0_scala_vector4f_mul () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = 2 * a;
		Vector4f c = a * 3;

		if (b.X != 2f || b.Y != 4f || b.Z != 6f || b.W != 8f )
			return 1;
		if (c.X != 3f || c.Y != 6f || c.Z != 9f || c.W != 12f )
			return 1;

		return 0;
	}

	static void CallMethodThatClobbersRegs () {
		Vector4f a = new Vector4f (9,9,9,9);
		Vector4f b = new Vector4f (9,9,9,9);
		a = a + b;
	}

	public static int test_0_call_spills_regs_correctly () {
		Vector4f a = new Vector4f (1,2,3,4);
		Vector4f b = new Vector4f (5,6,7,8);

		CallMethodThatClobbersRegs ();

		bool b0 = a.X == 1f;
		bool b1 = b.X == 5f;
		if (!b0 || !b1)
			return 1;
		return 0;
	}

	public static int test_0_shuffle_with_two_args_pd () {
		Vector2d a = new Vector2d (1,2);
		Vector2d b = new Vector2d (5,6);

		Vector2d c = a.Shuffle (b, 0x2);
		if (c.X != 1)
			return 1;
		if (c.Y != 6)
			return 2;
		return 0;
	}

	public static int test_0_shuffle_with_two_args_ps () {
		Vector4f a = new Vector4f (1, 2, 3, 4);
		Vector4f b = new Vector4f (5, 6, 7, 8);

		Vector4f c = a.Shuffle (b, ShuffleSel.ExpandY);
		if (c.X != 2)
			return 1;
		if (c.Y != 2)
			return 2;
		if (c.Z != 6)
			return 3;
		if (c.W != 6)
			return 4;
		return 0;
	}

	public static int test_0_i_to_d () {
		var a = new Vector4i (1, 2, 3, 4);
		var b = a.ConvertToDouble ();
		if (b.X != 1)
			return 1;
		if (b.Y != 2)
			return 2;
		return 0;
	}

	public static int test_0_i_to_f () {
		var a = new Vector4i (1, 2, 3, 4);
		var b = a.ConvertToFloat ();
		if (b.X != 1)
			return 1;
		if (b.Y != 2)
			return 2;
		if (b.Z != 3)
			return 3;
		if (b.W != 4)
			return 4;
		return 0;
	}

	public static int test_0_d_to_i () {
		var a = new Vector2d (1.4, 2.6);
		var b = a.ConvertToInt ();
		if (b.X != 1)
			return 1;
		if (b.Y != 3)
			return 2;
		if (b.Z != 0)
			return 3;
		if (b.W != 0)
			return 4;
		return 0;
	}

	public static int test_0_d_to_f () {
		var a = new Vector2d (1, 2);
		var b = a.ConvertToFloat ();
		if (b.X != 1)
			return 1;
		if (b.Y != 2)
			return 2;
		if (b.Z != 0)
			return 3;
		if (b.W != 0)
			return 4;
		return 0;
	}

	public static int test_0_f_to_i () {
		var a = new Vector4f (1.1f, 2.2f, 3.5f, 4.6f);
		var b = a.ConvertToInt ();
		if (b.X != 1)
			return 1;
		if (b.Y != 2)
			return 2;
		if (b.Z != 4)
			return 3;
		if (b.W != 5)
			return 4;
		return 0;
	}

	public static int test_0_f_to_d () {
		var a = new Vector4f (1,2,3,4);
		var b = a.ConvertToDouble ();
		if (b.X != 1)
			return 1;
		if (b.Y != 2)
			return 2;
		return 0;
	}

	public static int test_0_d_to_i_trunc () {
		var a = new Vector2d (1.4, 2.6);
		var b = a.ConvertToIntTruncated ();
		if (b.X != 1)
			return 1;
		if (b.Y != 2)
			return 2;
		if (b.Z != 0)
			return 3;
		if (b.W != 0)
			return 4;
		return 0;
	}

	public static int test_0_f_to_i_trunc () {
		var a = new Vector4f (1.1f, 2.2f, 3.5f, 4.6f);
		var b = a.ConvertToIntTruncated ();
		if (b.X != 1)
			return 1;
		if (b.Y != 2)
			return 2;
		if (b.Z != 3)
			return 3;
		if (b.W != 4)
			return 4;
		return 0;
	}

	class BoxedVector2d
	{
	    public Vector2d v;
	}

	public static int test_0_vector2d_set_x () {
		var bv = new BoxedVector2d ();
		var xy = new Vector2d ();
		xy.X = bv.v.X;

		if (xy.X != 0)
			return 1;
		if (xy.Y != 0)
			return 2;
		return 0;
	}

	public static int Main (String[] args) {
		return TestDriver.RunTests (typeof (SimdTests), args);
	}
}

