using System;
using Mono.Simd;

public class SimdTests {
	static int test_0_vector8u_sub_sat () {
		Vector8u a = new Vector8u (0xF000,1,20,3,4,5,6,7);
		Vector8u b = new Vector8u (0xFF00,4,5,6,7,8,9,10);
		Vector8u c = Vector8u.SubWithSaturation (a, b);

		if (c.A != 0)
			return 1;
		if (c.B != 0)
			return 2;
		if (c.C != 15)
			return 3;
		if (c.D != 0)
			return 4;
		if (c.E != 0)
			return 5;
		if (c.F != 0)
			return 6;
		if (c.G != 0)
			return 7;
		if (c.H != 0)
			return 8;
		return 0;
	}

	static int test_0_vector8u_add_sat () {
		Vector8u a = new Vector8u (0xFF00,1,2,3,4,5,6,7);
		Vector8u b = new Vector8u (0xFF00,4,5,6,7,8,9,10);
		Vector8u c = Vector8u.AddWithSaturation (a, b);

		if (c.A != 0xFFFF)
			return 1;
		if (c.B != 5)
			return 2;
		if (c.C != 7)
			return 3;
		if (c.D != 9)
			return 4;
		if (c.E != 11)
			return 5;
		if (c.F != 13)
			return 6;
		if (c.G != 15)
			return 7;
		if (c.H != 17)
			return 8;
		return 0;
	}

	static int test_0_vector8u_unpack_low () {
		Vector8u a = new Vector8u (0,1,2,3,4,5,6,7);
		Vector8u b = new Vector8u (3,4,5,6,7,8,9,10);
		Vector8u c = Vector8u.UnpackLow (a, b);

		if (c.A != 0)
			return 1;
		if (c.B != 3)
			return 2;
		if (c.C != 1)
			return 3;
		if (c.D != 4)
			return 4;
		if (c.E != 2)
			return 5;
		if (c.F != 5)
			return 6;
		if (c.G != 3)
			return 7;
		if (c.H != 6)
			return 8;
		return 0;
	}


	static int test_0_vector8u_shift_left () {
		Vector8u a = new Vector8u (0xFF00,1,2,3,4,5,6,7);
		int amt = 2;
		Vector8u c = a << amt;
	
		if (c.A != 0xFC00)
			return 1;
		if (c.B != 4)
			return 2;
		if (c.H != 28)
			return 3;
		return 0;
	}
	
	static int test_0_vector8u_shift_right_arithmetic () {
		Vector8u a = new Vector8u (0xFF00,1,2,3,4,5,6,7);
		int amt = 2;
		Vector8u c = Vector8u.ShiftRightArithmetic (a, amt);
	
		if (c.A != 0x3FC0)
			return 1;
		if (c.B != 0)
			return 2;
		if (c.H != 1)
			return 3;
		return 0;
	}

	static int test_0_vector8u_shift_variable_offset () {
		int off = 2;
		Vector8u a = new Vector8u (0xF000,1,2,3,4,5,6,7);
		Vector8u b = a;
		Vector8u c = b >> off;
		a = b + b;

		if (c.A != 0x3C00)
			return 1;
		if (c.B != 0)
			return 2;
		if (c.H != 1)
			return 3;
		if (a.B != 2)
			return 4;
		if (a.H != 14)
			return 5;
		return 0;
	}
	
	
	static int test_0_vector8u_shift_operand_is_live_after_op () {
		Vector8u a = new Vector8u (0xF000,1,2,3,4,5,6,7);
		Vector8u b = a;
		Vector8u c = b >> 2;
		a = b + b;

		if (c.A != 0x3C00)
			return 1;
		if (c.B != 0)
			return 2;
		if (c.H != 1)
			return 3;
		if (a.B != 2)
			return 4;
		if (a.H != 14)
			return 5;
		return 0;
	}

	static int test_0_vector8u_shr_constant () {
		Vector8u a = new Vector8u (0xF000,1,2,3,4,5,6,7);
		Vector8u c = a >> 2;

		if (c.A != 0x3C00)
			return 1;
		if (c.B != 0)
			return 2;
		if (c.H != 1)
			return 3;
		return 0;
	}

	static int test_0_vector8u_mul () {
		Vector8u a = new Vector8u (0x0F00,4,5,6,7,8,9,10);
		Vector8u b = new Vector8u (0x0888,1,2,3,4,5,6,8);

		Vector8u c = a * b;
		if (c.A != 63488)
			return 1;
		if (c.B != 4)
			return 2;
		if (c.H != 80)
			return 3;
		return 0;
	}

	static int test_0_vector8u_add () {
		Vector8u a = new Vector8u (0xFF00,4,5,6,7,8,9,10);
		Vector8u b = new Vector8u (0x8888,1,2,3,4,5,6,8);

		Vector8u c = a + b;
		if (c.A != 34696)
			return 1;
		if (c.B != 5)
			return 2;
		if (c.H != 18)
			return 3;
		return 0;
	}


	static int test_0_vector8u_sub () {
		Vector8u a = new Vector8u (3,4,5,6,7,8,9,10);
		Vector8u b = new Vector8u (10,1,2,3,4,5,6,8);

		Vector8u c = a - b;

		if (c.A != 65529)
			return 1;
		if (c.B != 3)
			return 2;
		if (c.H != 2)
			return 3;
		return 0;
	}


	static int test_0_vector8u_accessors () {
		Vector8u a = new Vector8u (0,1,2,3,4,5,6,7);

		if (a.A != 0)
			return 1;
		if (a.B != 1)
			return 2;
		if (a.C != 2)
			return 3;
		if (a.D != 3)
			return 4;
		if (a.E != 4)
			return 5;
		if (a.F != 5)
			return 6;
		if (a.G != 6)
			return 7;
		if (a.H != 7)
			return 8;
		a.A = 10;
		a.B = 20;
		a.C = 30;
		a.D = 40;
		a.E = 50;
		a.F = 60;
		a.G = 70;
		a.H = 80;

		if (a.A != 10)
			return 17;
		if (a.B != 20)
			return 18;
		if (a.C != 30)
			return 19;
		if (a.D != 40)
			return 20;
		if (a.E != 50)
			return 21;
		if (a.F != 60)
			return 22;
		if (a.G != 70)
			return 23;
		if (a.H != 80)
			return 24;

		return 0;
	}


	static int test_0_vector16u_unpack_high () {
		Vector16u a = new Vector16u (0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);
		Vector16u b = new Vector16u (9,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16u c = Vector16u.UnpackHigh (a, b);

		if (c.A != 8)
			return 1;
		if (c.B != 1)
			return 2;
		if (c.C != 9)
			return 3;
		if (c.D != 2)
			return 4;
		if (c.E != 10)
			return 5;
		if (c.F != 3)
			return 6;
		if (c.O != 15)
			return 7;
		if (c.P != 8)
			return 8;
		return 0;
	}

	static int test_0_vector16u_unpack_low () {
		Vector16u a = new Vector16u (0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);
		Vector16u b = new Vector16u (9,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16u c = Vector16u.UnpackLow (a, b);

		if (c.A != 0)
			return 1;
		if (c.B != 9)
			return 2;
		if (c.C != 1)
			return 3;
		if (c.D != 10)
			return 4;
		if (c.E != 2)
			return 5;
		if (c.F != 11)
			return 6;
		if (c.O != 7)
			return 7;
		if (c.P != 0)
			return 8;
		return 0;
	}

	static int test_0_vector16u_sub_sat () {
		Vector16u a = new Vector16u (100,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16u b = new Vector16u (200,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);
		Vector16u c = Vector16u.SubWithSaturation (a, b);

		if (c.A != 0)
			return 1;
		if (c.B != 9)
			return 2;
		if (c.P != 0)
			return 3;
		return 0;
	}
	
	static int test_0_vector16u_add_sat () {
		Vector16u a = new Vector16u (200,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);
		Vector16u b = new Vector16u (200,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16u c = Vector16u.AddWithSaturation (a, b);

		if (c.A != 255)
			return 1;
		if (c.B != 11)
			return 2;
		if (c.P != 23)
			return 3;
		return 0;
	}

	static int test_0_vector16u_add_ovf () {
		Vector16u a = new Vector16u (200,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);
		Vector16u b = new Vector16u (200,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8);
		Vector16u c = a + b;

		if (c.A != 144)
			return 1;
		if (c.B != 11)
			return 2;
		if (c.P != 23)
			return 3;
		return 0;
	}

	static int test_0_vector16u_accessors () {
		Vector16u a = new Vector16u (0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);

		if (a.A != 0)
			return 1;
		if (a.B != 1)
			return 2;
		if (a.C != 2)
			return 3;
		if (a.D != 3)
			return 4;
		if (a.E != 4)
			return 5;
		if (a.F != 5)
			return 6;
		if (a.G != 6)
			return 7;
		if (a.H != 7)
			return 8;
		if (a.I != 8)
			return 9;
		if (a.J != 9)
			return 10;
		if (a.K != 10)
			return 11;
		if (a.L != 11)
			return 12;
		if (a.M != 12)
			return 13;
		if (a.N != 13)
			return 14;
		if (a.O != 14)
			return 15;
		if (a.P != 15)
			return 16;

		a.A = 10;
		a.B = 20;
		a.C = 30;
		a.D = 40;
		a.E = 50;
		a.F = 60;
		a.G = 70;
		a.H = 80;
		a.I = 90;
		a.J = 100;
		a.K = 110;
		a.L = 120;
		a.M = 130;
		a.N = 140;
		a.O = 150;
		a.P = 160;

		if (a.A != 10)
			return 17;
		if (a.B != 20)
			return 18;
		if (a.C != 30)
			return 19;
		if (a.D != 40)
			return 20;
		if (a.E != 50)
			return 21;
		if (a.F != 60)
			return 22;
		if (a.G != 70)
			return 23;
		if (a.H != 80)
			return 24;
		if (a.I != 90)
			return 25;
		if (a.J != 100)
			return 26;
		if (a.K != 110)
			return 27;
		if (a.L != 120)
			return 28;
		if (a.M != 130)
			return 29;
		if (a.N != 140)
			return 30;
		if (a.O != 150)
			return 31;
		if (a.P != 160)
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

