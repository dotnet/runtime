using System;
using Mono.Simd;

public class SimdTests {
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

		Vector2l c = Vector2l.CompareGreaterThan (a, b);
	
		if (c.X != -1)
			return 1;
		if (c.Y != 0)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2l_cmp_eq () {
		Vector2l a = new Vector2l (0xFF,          5);
		Vector2l b = new Vector2l (0xFF000000FFL, 5);

		Vector2l c = Vector2l.CompareEqual (a, b);
	
		if (c.X != 0)
			return 1;
		if (c.Y != -1)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2l_srl () {
		Vector2l a = new Vector2l (1, 6);

		Vector2l c = Vector2l.ShiftRightLogic (a, 1);
	
		if (c.X != 0)
			return 1;
		if (c.Y != 3)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2l_unpack_high () {
		Vector2l a = new Vector2l (1, 6);
		Vector2l b = new Vector2l (3, 4);

		Vector2l c = Vector2l.UnpackHigh (a, b);
	
		if (c.X != 6)
			return 1;
		if (c.Y != 4)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2l_unpack_low () {
		Vector2l a = new Vector2l (1, 6);
		Vector2l b = new Vector2l (3, 4);

		Vector2l c = Vector2l.UnpackLow (a, b);
	
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

		Vector2d c = Vector2d.Duplicate (a);
	
		if (c.X != 3)
			return 1;
		if (c.Y != 3)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2d_cmp_eq () {
		Vector2d a = new Vector2d (3, 2);
		Vector2d b = new Vector2d (3, 4);

		Vector4ui c = (Vector4ui)Vector2d.CompareEqual (a, b);
	
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

		Vector2d c = Vector2d.InterleaveLow (a, b);
	
		if (c.X != 1)
			return 1;
		if (c.Y != 4)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2d_unpack_high () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (4, 5);

		Vector2d c = Vector2d.InterleaveHigh (a, b);
	
		if (c.X != 2)
			return 1;
		if (c.Y != 5)
			return 2;
		return 0;
	}
	public static unsafe int test_0_vector2d_addsub () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (4, 1);

		Vector2d c = Vector2d.AddSub (a, b);
	
		if (c.X != -3)
			return 1;
		if (c.Y != 3)
			return 2;
		return 0;
	}
	public static unsafe int test_0_vector2d_hsub () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (4, 1);

		Vector2d c = Vector2d.HorizontalSub (a, b);
	
		if (c.X != -1)
			return 1;
		if (c.Y != 3)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2d_hadd () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (4, 0);

		Vector2d c = Vector2d.HorizontalAdd (a, b);
	
		if (c.X != 3)
			return 1;
		if (c.Y != 4)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2d_min () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (4, 0);

		Vector2d c = Vector2d.Min (a, b);
	
		if (c.X != 1)
			return 1;
		if (c.Y != 0)
			return 2;
		return 0;
	}

	public static unsafe int test_0_vector2d_max () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (4, 0);

		Vector2d c = Vector2d.Max (a, b);
	
		if (c.X != 4)
			return 1;
		if (c.Y != 2)
			return 2;
		return 0;
	}


	public static unsafe int test_0_vector2d_andnot () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (3, 4);

		Vector4ui c = (Vector4ui)Vector2d.AndNot (a, b);
	
		if (c.X != 0)
			return 1;
		if (c.Y != 1074266112)
			return 2;
		if (c.Z != 0)
			return 3;
		if (c.W != 1048576)
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
	
		if (c.X != 0)
			return 1;
		if (c.Y != 2146959360)
			return 2;
		if (c.Z != 0)
			return 3;
		if (c.W != 1048576)
			return 4;
		return 0;
	}

	public static unsafe int test_0_vector2d_or () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (3, 4);

		Vector4ui c = (Vector4ui)(a | b);
	
		if (c.X != 0)
			return 1;
		if (c.Y != 2146959360)
			return 2;
		if (c.Z != 0)
			return 3;
		if (c.W != 1074790400)
			return 4;
		return 0;
	}

	public static unsafe int test_0_vector2d_and () {
		Vector2d a = new Vector2d (1, 2);
		Vector2d b = new Vector2d (3, 4);

		Vector4ui c = (Vector4ui)(a & b);
	
		if (c.X != 0)
			return 1;
		if (c.Y != 0)
			return 2;
		if (c.Z != 0)
			return 3;
		if (c.W != 1073741824)
			return 3;
		return 0;
	}

	public static unsafe int test_vector8s_pack_signed_sat () {
		Vector8s a = new Vector8s (-200, 200, 3, 0, 5, 6, 5, 4);
		Vector8s b = new Vector8s (9, 2, 1, 2, 3, 6, 5, 6);

		Vector16sb c = Vector8s.PackWithSignedSaturation (a, b);

		if (c.V0 != -128)
			return 1;
		if (c.V1 != 127)
			return 2;

		return 0;
	}

	public static unsafe int test_vector16sb_sub_sat () {
		Vector16sb a = new Vector16sb (100,-100,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16sb b = new Vector16sb (-100, 100,11,12,4,5,6,7,8,9,10,11,12,13,14,15);

		Vector16sb c = Vector16sb.SubWithSaturation (a, b);

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

	public static unsafe int test_vector16sb_add_sat () {
		Vector16sb a = new Vector16sb (100,-100,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16sb b = new Vector16sb (100, -100,11,12,4,5,6,7,8,9,10,11,12,13,14,15);

		Vector16sb c = Vector16sb.AddWithSaturation (a, b);

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

	public static unsafe int test_vector16sb_cmp_gt () {
		Vector16sb a = new Vector16sb (100,-100,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16sb b = new Vector16sb (-100, 100,11,12,4,5,6,7,8,9,10,11,12,13,14,15);

		Vector16sb c = Vector16sb.CompareGreaterThan (a, b);

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

		Vector8us c = Vector4ui.SignedPackWithUnsignedSaturation (a, b);

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
		Vector16b c = Vector8us.SignedPackWithUnsignedSaturation (a, b);

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
		Vector8us c = Vector8us.MultiplyStoreHigh (a, b);

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
		Vector8us c = Vector8us.CompareEqual (a, b);

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
		Vector4ui c = Vector4ui.CompareEqual (a, b);

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
		Vector4ui c = Vector4ui.Shuffle (a, ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);

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

	public static int test_0_vector4ui_extract_mask () {
		Vector4ui a = new Vector4ui (0xFF00FF00,0x0F0FAA99,0,0);
		int c = Vector4ui.ExtractByteMask (a);

		if (c != 0x3A)
			return 1;
		return 0;
	}

	public static int test_0_vector4ui_min () {
		Vector4ui a = new Vector4ui (6,1,6,3);
		Vector4ui b = new Vector4ui (3,4,6,7);
		Vector4ui c = Vector4ui.Min (a, b);

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
		Vector4ui c = Vector4ui.Max (a, b);

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
		Vector16b c = Vector16b.CompareEqual (a, b);

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
		Vector8us c = Vector16b.SumOfAbsoluteDifferences (a, b);

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
		int c = Vector16b.ExtractByteMask (a);

		if (c != 0x4C5)
			return 1;
		return 0;
	}

	public static int test_0_vector16b_min () {
		Vector16b a = new Vector16b (0,12,20,12,4,5,6,7,8,9,10,11,12,13,14,15);
		Vector16b b = new Vector16b (9,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16b c = Vector16b.Min (a, b);

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
		Vector16b c = Vector16b.Max (a, b);

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
		Vector16b a = new Vector16b (0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);
		Vector16b b = new Vector16b (9,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16b c = Vector16b.Average (a, b);

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
		if (c.V15 != 12)
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
	

	public static int test_0_vecto8us_extract_mask () {
		Vector8us a = new Vector8us (0xF0F0, 0x700F, 0xAABB, 0x0000, 0x00F0, 0xF0F0, 0, 0);
		int c = Vector8us.ExtractByteMask (a);

		if (c != 0xD33)
			return 1;
		return 0;
	}

	public static int test_0_vecto8us_shuffle_low () {
		Vector8us a = new Vector8us (1, 2, 3, 4, 5, 6, 7, 8);
		Vector8us c = Vector8us.ShuffleLow (a, ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);

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
		Vector8us c = Vector8us.ShuffleHigh (a, ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);

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
		Vector8us c = Vector8us.Max (a, b);

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
		Vector8us c = Vector8us.Min (a, b);

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
		Vector8us a = new Vector8us (1, 2, 3, 4, 5, 6, 7, 8);
		Vector8us b = new Vector8us (9, 1, 1, 2, 3, 4, 5, 6);
		Vector8us c = Vector8us.Average (a, b);

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
		if (c.V7 != 7)
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
		Vector4f c = Vector4f.DuplicateHigh(a);

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
		Vector4f c = Vector4f.DuplicateLow (a);

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
		Vector4f c = Vector4f.InterleaveHigh (a, b);

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
		Vector4f c = Vector4f.InterleaveLow (a, b);

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
		Vector4f c = Vector4f.Reciprocal (a);

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
		Vector4f c = Vector4f.AndNot (a ,b);

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
		Vector4f c = Vector4f.CompareOrdered (a, b);

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
		Vector4f c = Vector4f.CompareNotLessEqual (a, b);

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
		Vector4f c = Vector4f.CompareNotLessThan (a, b);

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
		Vector4f c = Vector4f.CompareNotEqual (a, b);

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
		Vector4f c = Vector4f.CompareUnordered (a, b);

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
		Vector4f c = Vector4f.CompareLessEqual (a, b);

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
		Vector4f c = Vector4f.CompareLessThan (a, b);

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
		Vector4f c = Vector4f.CompareEqual (a, b);

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
		
		Vector4ui c = Vector4ui.ShiftRightArithmetic (a, 2);
	
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
		
		Vector4ui c = Vector4ui.UnpackHigh(a, b);
	
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
		
		Vector4ui c = Vector4ui.UnpackLow (a, b);
	
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
		Vector8us c = Vector8us.SubWithSaturation (a, b);

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
		Vector8us c = Vector8us.AddWithSaturation (a, b);

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
		Vector8us c = Vector8us.UnpackLow (a, b);

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
		Vector8us c = Vector8us.ShiftRightArithmetic (a, amt);
	
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
		Vector16b c = Vector16b.UnpackHigh (a, b);

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
		Vector16b c = Vector16b.UnpackLow (a, b);

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
		Vector16b c = Vector16b.SubWithSaturation (a, b);

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
		Vector16b c = Vector16b.AddWithSaturation (a, b);

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
		a = Vector4f.Sqrt (a);
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
		a = Vector4f.InvSqrt (a);
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
		Vector4f c = Vector4f.Min (a, b);
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
		Vector4f c = Vector4f.Max (a, b);
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
		Vector4f c = Vector4f.HorizontalAdd (a, b);
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
		Vector4f c = Vector4f.HorizontalSub (a, b);
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
		Vector4f c = Vector4f.AddSub (a, b);
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
		a = Vector4f.Shuffle(a, ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);
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

		Vector4f r0 = Vector4f.Shuffle (v, ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);
		Vector4f r1 = Vector4f.Shuffle (v, ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);
		Vector4f x = Vector4f.Shuffle (v, ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);
		Vector4f r2 = Vector4f.Shuffle (v, ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);
		Vector4f r3 = Vector4f.Shuffle (v, ShuffleSel.XFromY | ShuffleSel.YFromW | ShuffleSel.ZFromX | ShuffleSel.WFromZ);
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

	public static int Main () {
		return TestDriver.RunTests (typeof (SimdTests));
	}
}

