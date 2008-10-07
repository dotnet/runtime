using System;
using Mono.Simd;

public class SimdTests {

	public static int test_accessors () {
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

	public static int test_packed_add_with_stack_tmp () {
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

	public static int test_simple_packed_add () {
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

	public static int test_simple_packed_sub () {
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

	public static int test_simple_packed_mul () {
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

	public static int test_simple_packed_div () {
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

	public static int test_simple_packed_sqrt () {
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

	public static int test_simple_packed_invsqrt () {
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

	public static int test_simple_packed_min () {
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

	public static int test_simple_packed_max () {
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

	public static int test_simple_packed_hadd () {
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

	public static int test_simple_packed_hsub () {
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

	public static int test_simple_packed_addsub () {
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

	public static int test_simple_packed_shuffle () {
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

