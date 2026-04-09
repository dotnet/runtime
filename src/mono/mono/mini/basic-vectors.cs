using System;
using System.Numerics;
using System.Runtime.CompilerServices;

/*
 * Tests for the SIMD intrinsics in the System.Numerics.Vectors assembly.
 */
public class VectorTests {

#if !MOBILE
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (VectorTests), args);
	}
#endif

	//
	// Vector2 tests
	//

	public static int test_0_vector2_ctor_1 () {
		var v = new Vector2 (1.0f);

		if (v.X != 1.0f)
			return 1;
		if (v.Y != 1.0f)
			return 2;
		return 0;
	}

	public static int test_0_vector2_ctor_2 () {
		var v = new Vector2 (1.0f, 2.0f);

		if (v.X != 1.0f)
			return 1;
		if (v.Y != 2.0f)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static bool vector2_equals (Vector2 v1, Vector2 v2) {
		// cmpeqps+pmovmskb
		return v1.Equals (v2);
	}

	public static int test_0_vector2_equals () {
		var v1 = new Vector2 (1.0f, 2.0f);
		var v2 = new Vector2 (2.0f, 2.0f);

		if (vector2_equals (v1, v2))
			return 1;
		if (!vector2_equals (v1, v1))
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static float vector2_dot (Vector2 v1, Vector2 v2) {
		return Vector2.Dot (v1, v2);
	}

	public static int test_0_vector2_dot () {
		var v1 = new Vector2 (1.0f, 1.0f);
		var v2 = new Vector2 (2.0f, 2.0f);

		float f = vector2_dot (v1, v2);
		if (f != 4.0f)
			return 1;
		f = vector2_dot (v1, v1);
		if (f != 2.0f)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector2 vector2_min (Vector2 v1, Vector2 v2) {
		return Vector2.Min (v1, v2);
	}

	public static int test_0_vector2_min () {
		var v1 = new Vector2 (1.0f, 1.0f);
		var v2 = new Vector2 (2.0f, 2.0f);

		var v3 = vector2_min (v1, v2);
		if (v3.X != 1.0f || v3.Y != 1.0f)
			return 1;
		v3 = vector2_min (v2, v2);
		if (v3.X != 2.0f || v3.Y != 2.0f)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector2 vector2_max (Vector2 v1, Vector2 v2) {
		return Vector2.Max (v1, v2);
	}

	public static int test_0_vector2_max () {
		var v1 = new Vector2 (1.0f, 1.0f);
		var v2 = new Vector2 (2.0f, 2.0f);

		var v3 = vector2_max (v1, v2);
		if (v3.X != 2.0f || v3.Y != 2.0f)
			return 1;
		v3 = vector2_min (v1, v1);
		if (v3.X != 1.0f || v3.Y != 1.0f)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector2 vector2_abs (Vector2 v1) {
		return Vector2.Abs (v1);
	}

	public static int test_0_vector2_abs () {
		var v1 = new Vector2 (-1.0f, -2.0f);
		var v2 = new Vector2 (1.0f, 2.0f);

		var v3 = vector2_abs (v1);
		if (v3.X != 1.0f || v3.Y != 2.0f)
			return 1;
		v3 = vector2_abs (v2);
		if (v3.X != 1.0f || v3.Y != 2.0f)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector2 vector2_sqrt (Vector2 v1) {
		return Vector2.SquareRoot (v1);
	}

	public static int test_0_vector2_sqrt () {
		var v1 = new Vector2 (1.0f, 0.0f);

		var v3 = vector2_sqrt (v1);
		if (v3.X != 1.0f || v3.Y != 0.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector2 vector2_add (Vector2 v1, Vector2 v2) {
		return v1 + v2;
	}

	public static int test_0_vector2_add () {
		var v1 = new Vector2 (1.0f, 2.0f);
		var v2 = new Vector2 (3.0f, 4.0f);

		var v3 = vector2_add (v1, v2);
		if (v3.X != 4.0f || v3.Y != 6.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector2 vector2_sub (Vector2 v1, Vector2 v2) {
		return v1 - v2;
	}

	public static int test_0_vector2_sub () {
		var v1 = new Vector2 (1.0f, 2.0f);
		var v2 = new Vector2 (3.0f, 5.0f);

		var v3 = vector2_sub (v2, v1);
		if (v3.X != 2.0f || v3.Y != 3.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector2 vector2_mul (Vector2 v1, Vector2 v2) {
		return v1 * v2;
	}

	public static int test_0_vector2_mul () {
		var v1 = new Vector2 (1.0f, 2.0f);
		var v2 = new Vector2 (3.0f, 5.0f);

		var v3 = vector2_mul (v2, v1);
		if (v3.X != 3.0f || v3.Y != 10.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector2 vector2_mul_left (float v1, Vector2 v2) {
		return v1 * v2;
	}

	public static int test_0_vector2_mul_left () {
		var v1 = new Vector2 (3.0f, 5.0f);

		var v3 = vector2_mul_left (2.0f, v1);
		if (v3.X != 6.0f || v3.Y != 10.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector2 vector2_mul_right (Vector2 v1, float v2) {
		return v1 * v2;
	}

	public static int test_0_vector2_mul_right () {
		var v1 = new Vector2 (3.0f, 5.0f);

		var v3 = vector2_mul_right (v1, 2.0f);
		if (v3.X != 6.0f || v3.Y != 10.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector2 vector2_div (Vector2 v1, Vector2 v2) {
		return v1 / v2;
	}

	public static int test_0_vector2_div () {
		var v1 = new Vector2 (9.0f, 10.0f);
		var v2 = new Vector2 (3.0f, 5.0f);

		var v3 = vector2_div (v1, v2);
		if (v3.X != 3.0f || v3.Y != 2.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector2 vector2_div_right (Vector2 v1, float v2) {
		return v1 / v2;
	}

	public static int test_0_vector2_div_right () {
		var v1 = new Vector2 (9.0f, 15.0f);

		var v3 = vector2_div_right (v1, 3.0f);
		if (v3.X != 3.0f || v3.Y != 5.0f)
			return 1;
		return 0;
	}

	//
	// Vector4 tests
	//

	public static int test_0_vector4_ctor_1 () {
		var v = new Vector4 (1.0f);

		if (v.X != 1.0f)
			return 1;
		if (v.Y != 1.0f)
			return 2;
		if (v.Z != 1.0f)
			return 3;
		if (v.W != 1.0f)
			return 4;
		return 0;
	}

	public static int test_0_vector4_ctor_2 () {
		var v = new Vector4 (1.0f, 2.0f, 3.0f, 4.0f);

		if (v.X != 1.0f)
			return 1;
		if (v.Y != 2.0f)
			return 2;
		if (v.Z != 3.0f)
			return 3;
		if (v.W != 4.0f)
			return 4;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static bool vector4_equals (Vector4 v1, Vector4 v2) {
		// cmpeqps+pmovmskb
		return v1.Equals (v2);
	}

	public static int test_0_vector4_equals () {
		var v1 = new Vector4 (1.0f, 2.0f, 3.0f, 4.0f);
		var v2 = new Vector4 (2.0f, 2.0f, 2.0f, 2.0f);

		if (vector4_equals (v1, v2))
			return 1;
		if (!vector4_equals (v1, v1))
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static float vector4_dot (Vector4 v1, Vector4 v2) {
		return Vector4.Dot (v1, v2);
	}

	public static int test_0_vector4_dot () {
		var v1 = new Vector4 (1.0f, 1.0f, 1.0f, 1.0f);
		var v2 = new Vector4 (2.0f, 2.0f, 2.0f, 2.0f);

		float f = vector4_dot (v1, v2);
		if (f != 8.0f)
			return 1;
		f = vector4_dot (v1, v1);
		if (f != 4.0f)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector4 vector4_min (Vector4 v1, Vector4 v2) {
		return Vector4.Min (v1, v2);
	}

	public static int test_0_vector4_min () {
		var v1 = new Vector4 (1.0f, 2.0f, 3.0f, 4.0f);
		var v2 = new Vector4 (5.0f, 6.0f, 7.0f, 8.0f);

		var v3 = vector4_min (v1, v2);
		if (v3.X != 1.0f || v3.Y != 2.0f || v3.Z != 3.0f || v3.W != 4.0f)
			return 1;
		v3 = vector4_min (v2, v2);
		if (v3.X != 5.0f || v3.Y != 6.0f || v3.Z != 7.0f || v3.W != 8.0f)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector4 vector4_max (Vector4 v1, Vector4 v2) {
		return Vector4.Max (v1, v2);
	}

	public static int test_0_vector4_max () {
		var v1 = new Vector4 (1.0f, 2.0f, 3.0f, 4.0f);
		var v2 = new Vector4 (5.0f, 6.0f, 7.0f, 8.0f);

		var v3 = vector4_max (v1, v2);
		if (v3.X != 5.0f || v3.Y != 6.0f || v3.Z != 7.0f || v3.W != 8.0f)
			return 1;
		v3 = vector4_max (v1, v1);
		if (v3.X != 1.0f || v3.Y != 2.0f || v3.Z != 3.0f || v3.W != 4.0f)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector4 vector4_abs (Vector4 v1) {
		return Vector4.Abs (v1);
	}

	public static int test_0_vector4_abs () {
		var v1 = new Vector4 (-1.0f, -2.0f, -3.0f, -4.0f);
		var v2 = new Vector4 (1.0f, 2.0f, 3.0f, 4.0f);

		var v3 = vector4_abs (v1);
		if (v3.X != 1.0f || v3.Y != 2.0f || v3.Z != 3.0f || v3.W != 4.0f)
			return 1;
		v3 = vector4_abs (v2);
		if (v3.X != 1.0f || v3.Y != 2.0f || v3.Z != 3.0f || v3.W != 4.0f)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector4 vector4_sqrt (Vector4 v1) {
		return Vector4.SquareRoot (v1);
	}

	public static int test_0_vector4_sqrt () {
		var v1 = new Vector4 (1.0f, 0.0f, 1.0f, 0.0f);

		var v3 = vector4_sqrt (v1);
		if (v3.X != 1.0f || v3.Y != 0.0f || v3.Z != 1.0f || v3.W != 0.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector4 vector4_add (Vector4 v1, Vector4 v2) {
		return v1 + v2;
	}

	public static int test_0_vector4_add () {
		var v1 = new Vector4 (1.0f, 2.0f, 3.0f, 4.0f);
		var v2 = new Vector4 (5.0f, 6.0f, 7.0f, 8.0f);

		var v3 = vector4_add (v1, v2);
		if (v3.X != 6.0f || v3.Y != 8.0f || v3.Z != 10.0f || v3.W != 12.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector4 vector4_sub (Vector4 v1, Vector4 v2) {
		return v1 - v2;
	}

	public static int test_0_vector4_sub () {
		var v1 = new Vector4 (1.0f, 2.0f, 3.0f, 4.0f);
		var v2 = new Vector4 (3.0f, 5.0f, 7.0f, 9.0f);

		var v3 = vector4_sub (v2, v1);
		if (v3.X != 2.0f || v3.Y != 3.0f || v3.Z != 4.0f || v3.W != 5.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector4 vector4_mul (Vector4 v1, Vector4 v2) {
		return v1 * v2;
	}

	public static int test_0_vector4_mul () {
		var v1 = new Vector4 (1.0f, 2.0f, 3.0f, 4.0f);
		var v2 = new Vector4 (3.0f, 5.0f, 6.0f, 7.0f);

		var v3 = vector4_mul (v2, v1);
		if (v3.X != 3.0f || v3.Y != 10.0f || v3.Z != 18.0f || v3.W != 28.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector4 vector4_mul_left (float v1, Vector4 v2) {
		return v1 * v2;
	}

	public static int test_0_vector4_mul_left () {
		var v1 = new Vector4 (3.0f, 5.0f, 6.0f, 7.0f);

		var v3 = vector4_mul_left (2.0f, v1);
		if (v3.X != 6.0f || v3.Y != 10.0f || v3.Z != 12.0f || v3.W != 14.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector4 vector4_mul_right (Vector4 v1, float v2) {
		return v1 * v2;
	}

	public static int test_0_vector4_mul_right () {
		var v1 = new Vector4 (3.0f, 5.0f, 6.0f, 7.0f);

		var v3 = vector4_mul_right (v1, 2.0f);
		if (v3.X != 6.0f || v3.Y != 10.0f || v3.Z != 12.0f || v3.W != 14.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector4 vector4_div (Vector4 v1, Vector4 v2) {
		return v1 / v2;
	}

	public static int test_0_vector4_div () {
		var v1 = new Vector4 (9.0f, 10.0f, 12.0f, 21.0f);
		var v2 = new Vector4 (3.0f, 5.0f, 6.0f, 7.0f);

		var v3 = vector4_div (v1, v2);
		if (v3.X != 3.0f || v3.Y != 2.0f || v3.Z != 2.0f || v3.W != 3.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector4 vector4_div_right (Vector4 v1, float v2) {
		return v1 / v2;
	}

	public static int test_0_vector4_div_right () {
		var v1 = new Vector4 (9.0f, 15.0f, 21.0f, 30.0f);

		var v3 = vector4_div_right (v1, 3.0f);
		if (v3.X != 3.0f || v3.Y != 5.0f || v3.Z != 7.0f || v3.W != 10.0f)
			return 1;
		return 0;
	}

	public static int test_0_vector4_length () {
		var v = new Vector4 (2.0f, 2.0f, 2.0f, 2.0f);
		return v.Length () == 4.0f ? 0 : 1;
	}

	//
	// Vector3 tests
	//

	public static int test_0_vector3_ctor_1 () {
		var v = new Vector3 (1.0f);

		if (v.X != 1.0f)
			return 1;
		if (v.Y != 1.0f)
			return 2;
		if (v.Z != 1.0f)
			return 3;
		return 0;
	}

	public static int test_0_vector3_ctor_2 () {
		var v = new Vector3 (1.0f, 2.0f, 3.0f);

		if (v.X != 1.0f)
			return 1;
		if (v.Y != 2.0f)
			return 2;
		if (v.Z != 3.0f)
			return 3;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static bool vector3_equals (Vector3 v1, Vector3 v2) {
		// cmpeqps+pmovmskb
		return v1.Equals (v2);
	}

	public static int test_0_vector3_equals () {
		var v1 = new Vector3 (1.0f, 2.0f, 3.0f);
		var v2 = new Vector3 (2.0f, 2.0f, 2.0f);

		if (vector3_equals (v1, v2))
			return 1;
		if (!vector3_equals (v1, v1))
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static float vector3_dot (Vector3 v1, Vector3 v2) {
		return Vector3.Dot (v1, v2);
	}

	public static int test_0_vector3_dot () {
		var v1 = new Vector3 (1.0f, 1.0f, 1.0f);
		var v2 = new Vector3 (2.0f, 2.0f, 2.0f);

		float f = vector3_dot (v1, v2);
		if (f != 6.0f)
			return 1;
		f = vector3_dot (v1, v1);
		if (f != 3.0f)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector3 vector3_min (Vector3 v1, Vector3 v2) {
		return Vector3.Min (v1, v2);
	}

	public static int test_0_vector3_min () {
		var v1 = new Vector3 (1.0f, 2.0f, 3.0f);
		var v2 = new Vector3 (5.0f, 6.0f, 7.0f);

		var v3 = vector3_min (v1, v2);
		if (v3.X != 1.0f || v3.Y != 2.0f || v3.Z != 3.0f)
			return 1;
		v3 = vector3_min (v2, v2);
		if (v3.X != 5.0f || v3.Y != 6.0f || v3.Z != 7.0f)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector3 vector3_max (Vector3 v1, Vector3 v2) {
		return Vector3.Max (v1, v2);
	}

	public static int test_0_vector3_max () {
		var v1 = new Vector3 (1.0f, 2.0f, 3.0f);
		var v2 = new Vector3 (5.0f, 6.0f, 7.0f);

		var v3 = vector3_max (v1, v2);
		if (v3.X != 5.0f || v3.Y != 6.0f || v3.Z != 7.0f)
			return 1;
		v3 = vector3_max (v1, v1);
		if (v3.X != 1.0f || v3.Y != 2.0f || v3.Z != 3.0f)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector3 vector3_abs (Vector3 v1) {
		return Vector3.Abs (v1);
	}

	public static int test_0_vector3_abs () {
		var v1 = new Vector3 (-1.0f, -2.0f, -3.0f);
		var v2 = new Vector3 (1.0f, 2.0f, 3.0f);

		var v3 = vector3_abs (v1);
		if (v3.X != 1.0f || v3.Y != 2.0f || v3.Z != 3.0f)
			return 1;
		v3 = vector3_abs (v2);
		if (v3.X != 1.0f || v3.Y != 2.0f || v3.Z != 3.0f)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector3 vector3_sqrt (Vector3 v1) {
		return Vector3.SquareRoot (v1);
	}

	public static int test_0_vector3_sqrt () {
		var v1 = new Vector3 (1.0f, 0.0f, 1.0f);

		var v3 = vector3_sqrt (v1);
		if (v3.X != 1.0f || v3.Y != 0.0f || v3.Z != 1.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector3 vector3_add (Vector3 v1, Vector3 v2) {
		return v1 + v2;
	}

	public static int test_0_vector3_add () {
		var v1 = new Vector3 (1.0f, 2.0f, 3.0f);
		var v2 = new Vector3 (5.0f, 6.0f, 7.0f);

		var v3 = vector3_add (v1, v2);
		if (v3.X != 6.0f || v3.Y != 8.0f || v3.Z != 10.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector3 vector3_sub (Vector3 v1, Vector3 v2) {
		return v1 - v2;
	}

	public static int test_0_vector3_sub () {
		var v1 = new Vector3 (1.0f, 2.0f, 3.0f);
		var v2 = new Vector3 (3.0f, 5.0f, 7.0f);

		var v3 = vector3_sub (v2, v1);
		if (v3.X != 2.0f || v3.Y != 3.0f || v3.Z != 4.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector3 vector3_mul (Vector3 v1, Vector3 v2) {
		return v1 * v2;
	}

	public static int test_0_vector3_mul () {
		var v1 = new Vector3 (1.0f, 2.0f, 3.0f);
		var v2 = new Vector3 (3.0f, 5.0f, 6.0f);

		var v3 = vector3_mul (v2, v1);
		if (v3.X != 3.0f || v3.Y != 10.0f || v3.Z != 18.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector3 vector3_mul_left (float v1, Vector3 v2) {
		return v1 * v2;
	}

	public static int test_0_vector3_mul_left () {
		var v1 = new Vector3 (3.0f, 5.0f, 6.0f);

		var v3 = vector3_mul_left (2.0f, v1);
		if (v3.X != 6.0f || v3.Y != 10.0f || v3.Z != 12.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector3 vector3_mul_right (Vector3 v1, float v2) {
		return v1 * v2;
	}

	public static int test_0_vector3_mul_right () {
		var v1 = new Vector3 (3.0f, 5.0f, 6.0f);

		var v3 = vector3_mul_right (v1, 2.0f);
		if (v3.X != 6.0f || v3.Y != 10.0f || v3.Z != 12.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector3 vector3_div (Vector3 v1, Vector3 v2) {
		return v1 / v2;
	}

	public static int test_0_vector3_div () {
		var v1 = new Vector3 (9.0f, 10.0f, 12.0f);
		var v2 = new Vector3 (3.0f, 5.0f, 6.0f);

		var v3 = vector3_div (v1, v2);
		if (v3.X != 3.0f || v3.Y != 2.0f || v3.Z != 2.0f)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector3 vector3_div_right (Vector3 v1, float v2) {
		return v1 / v2;
	}

	public static int test_0_vector3_div_right () {
		var v1 = new Vector3 (9.0f, 15.0f, 21.0f);

		var v3 = vector3_div_right (v1, 3.0f);
		if (v3.X != 3.0f || v3.Y != 5.0f || v3.Z != 7.0f)
			return 1;
		return 0;
	}

	//
	// Vector<T>
	//

	public static int test_0_vector_t_count () {
		// This assumes a 16 byte simd register size
		if (Vector<byte>.Count != 16)
			return 1;
		if (Vector<short>.Count != 8)
			return 2;
		if (Vector<int>.Count != 4)
			return 3;
		if (Vector<long>.Count != 2)
			return 4;
		return 0;
	}

	public static int test_0_vector_t_zero () {
		var v = Vector<byte>.Zero;
		for (int i = 0; i < Vector<byte>.Count; ++i)
			if (v [i] != 0)
				return 1;
		var v2 = Vector<double>.Zero;
		for (int i = 0; i < Vector<double>.Count; ++i)
			if (v2 [i] != 0.0)
				return 2;
		return 0;
	}

	public static int test_0_vector_t_i1_accessor () {
		var elems = new byte [] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
		var v = new Vector<byte> (elems, 0);
		for (int i = 0; i < Vector<byte>.Count; ++i)
			if (v [i] != i + 1)
				return 1;
		if (v [0] != 1)
			return 2;
		if (v [1] != 2)
			return 2;
		if (v [15] != 16)
			return 2;
		try {
			int r = v [-1];
			return 3;
		} catch (IndexOutOfRangeException) {
		}
		try {
			int r = v [16];
			return 4;
		} catch (IndexOutOfRangeException) {
		}
		return 0;
	}

	public static int test_0_vector_t_i4_accessor () {
		var elems = new int [] { 1, 2, 3, 4 };
		var v = new Vector<int> (elems, 0);
		for (int i = 0; i < Vector<int>.Count; ++i)
			if (v [i] != i + 1)
				return 1;
		if (v [0] != 1)
			return 2;
		if (v [1] != 2)
			return 2;
		if (v [3] != 4)
			return 2;
		try {
			int r = v [-1];
			return 3;
		} catch (IndexOutOfRangeException) {
		}
		try {
			int r = v [Vector<int>.Count];
			return 4;
		} catch (IndexOutOfRangeException) {
		}
		return 0;
	}

	public static int test_0_vector_t_i8_accessor () {
		var elems = new long [] { 1, 2 };
		var v = new Vector<long> (elems, 0);
		for (int i = 0; i < Vector<long>.Count; ++i)
			if (v [i] != i + 1)
				return 1;
		if (v [0] != 1)
			return 2;
		if (v [1] != 2)
			return 2;
		try {
			var r = v [-1];
			return 3;
		} catch (IndexOutOfRangeException) {
		}
		try {
			var r = v [Vector<long>.Count];
			return 4;
		} catch (IndexOutOfRangeException) {
		}
		return 0;
	}

	public static int test_0_vector_t_r8_accessor () {
		var elems = new double [] { 1.0, 2.0 };
		var v = new Vector<double> (elems, 0);
		for (int i = 0; i < Vector<double>.Count; ++i)
			if (v [i] != (double)i + 1.0)
				return 1;
		if (v [0] != 1.0)
			return 2;
		if (v [1] != 2.0)
			return 2;
		try {
			var r = v [-1];
			return 3;
		} catch (IndexOutOfRangeException) {
		}
		try {
			var r = v [Vector<double>.Count];
			return 4;
		} catch (IndexOutOfRangeException) {
		}
		return 0;
	}

	public static int test_0_vector_t_i1_ctor_3 () {
		var v = new Vector<byte> (5);
		for (int i = 0; i < 16; ++i)
			if (v [i] != 5)
				return 1;
		return 0;
	}

	public static int test_0_vector_t_i2_ctor_3 () {
		var v = new Vector<short> (5);
		for (int i = 0; i < 8; ++i)
			if (v [i] != 5)
				return 1;
		return 0;
	}

	public static int test_0_vector_t_i4_ctor_3 () {
		var v = new Vector<int> (0xffffeee);
		for (int i = 0; i < 4; ++i)
			if (v [i] != 0xffffeee)
				return 1;
		return 0;
	}

	public static int test_0_vector_t_i8_ctor_3 () {
		var v = new Vector<long> (0xffffeeeeabcdefL);
		for (int i = 0; i < 2; ++i)
			if (v [i] != 0xffffeeeeabcdefL)
				return 1;
		return 0;
	}

	public static int test_0_vector_t_r4_ctor_3 () {
		var v = new Vector<float> (0.5f);
		for (int i = 0; i < 4; ++i) {
			if (v [i] != 0.5f)
				return 1;
		}
		return 0;
	}

	public static int test_0_vector_t_r8_ctor_3 () {
		var v = new Vector<double> (0.5f);
		for (int i = 0; i < 2; ++i) {
			if (v [i] != 0.5f)
				return 1;
		}
		return 0;
	}

	public static int test_0_vector_t_i1_ctor () {
		var elems = new byte [] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
								  0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
		var v = new Vector<byte> (elems, 16);
		for (int i = 0; i < 16; ++i)
			if (v [i] != i)
				return 1;
		try {
			var v2 = new Vector<byte> (elems, 16 + 4);
			return 2;
		} catch (IndexOutOfRangeException) {
		}
		return 0;
	}

	public static int test_0_vector_t_i1_ctor_2 () {
		var elems = new byte [] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
		var v = new Vector<byte> (elems);
		for (int i = 0; i < 16; ++i)
			if (v [i] != i + 1)
				return 1;
		try {
			var elems2 = new byte [] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
			var v2 = new Vector<byte> (elems2);
			return 2;
		} catch (IndexOutOfRangeException) {
		}
		return 0;
	}

	public static int test_0_vector_t_r4_equal () {
		var elems1 = new float [4] { 1.0f, 1.0f, 1.0f, 1.0f };
		var v1 = new Vector<float> (elems1);
		var elems2 = new float [4] { 1.0f, 2.0f, 1.0f, 2.0f };
		var v2 = new Vector<float> (elems2);
		Vector<int> v = Vector.Equals (v1, v2);
		if (v [0] != -1 || v [1] != 0 || v [2] != -1 || v [3] != 0)
			return 1;
		return 0;
	}

	public static int test_0_vector_t_r8_equal () {
		var elems1 = new double [] { 1.0f, 1.0f };
		var v1 = new Vector<double> (elems1);
		var elems2 = new double [] { 1.0f, 2.0f };
		var v2 = new Vector<double> (elems2);
		Vector<long> v = Vector.Equals (v1, v2);
		if (v [0] != -1 || v [1] != 0)
			return 1;
		return 0;
	}

	public static int test_0_vector_t_i8_equal () {
		var elems1 = new long [] { 1, 1 };
		var v1 = new Vector<long> (elems1);
		var elems2 = new long [] { 1, 2 };
		var v2 = new Vector<long> (elems2);
		Vector<long> v = Vector.Equals (v1, v2);
		if (v [0] != -1 || v [1] != 0)
			return 1;
		return 0;
	}

	public static int test_0_vector_t_i4_equal () {
		var elems1 = new int [] { 1, 1, 1, 1 };
		var v1 = new Vector<int> (elems1);
		var elems2 = new int [] { 1, 2, 1, 2 };
		var v2 = new Vector<int> (elems2);
		Vector<int> v = Vector.Equals (v1, v2);
		if (v [0] != -1 || v [1] != 0 || v [2] != -1 || v[3] != 0)
			return 1;
		return 0;
	}

	/*
	public static int test_0_vector_t_u4_equal () {
		var elems1 = new uint [] { 1, 1, 1, 1 };
		var v1 = new Vector<uint> (elems1);
		var elems2 = new uint [] { 1, 2, 1, 2 };
		var v2 = new Vector<uint> (elems2);
		Vector<uint> v = Vector.Equals (v1, v2);
		if (v [0] != 0xffffffff || v [1] != 0 || v [2] != 0xffffffff || v[3] != 0)
			return 1;
		return 0;
	}
	*/

	public static int test_0_vector_t_i2_equal () {
		var elems1 = new short [] { 1, 1, 1, 1, 1, 1, 1, 1 };
		var v1 = new Vector<short> (elems1);
		var elems2 = new short [] { 1, 2, 1, 2, 1, 2, 1, 2 };
		var v2 = new Vector<short> (elems2);
		Vector<short> v = Vector.Equals (v1, v2);
		for (int i = 0; i < Vector<short>.Count; ++i) {
			if (i % 2 == 0 && v [i] != -1)
				return 1;
			if (i % 2 == 1 && v [i] != 0)
				return 1;
		}
		return 0;
	}

	public static int test_0_vector_t_u2_equal () {
		var elems1 = new ushort [] { 1, 1, 1, 1, 1, 1, 1, 1 };
		var v1 = new Vector<ushort> (elems1);
		var elems2 = new ushort [] { 1, 2, 1, 2, 1, 2, 1, 2 };
		var v2 = new Vector<ushort> (elems2);
		Vector<ushort> v = Vector.Equals (v1, v2);
		for (int i = 0; i < Vector<ushort>.Count; ++i) {
			if (i % 2 == 0 && v [i] != 0xffff)
				return 1;
			if (i % 2 == 1 && v [i] != 0)
				return 1;
		}
		return 0;
	}

	public static int test_0_vector_t_i1_equal () {
		var elems1 = new sbyte [] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
		var v1 = new Vector<sbyte> (elems1);
		var elems2 = new sbyte [] { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 };
		var v2 = new Vector<sbyte> (elems2);
		Vector<sbyte> v = Vector.Equals (v1, v2);
		for (int i = 0; i < Vector<sbyte>.Count; ++i) {
			if (i % 2 == 0 && v [i] != -1)
				return 1;
			if (i % 2 == 1 && v [i] != 0)
				return 1;
		}
		return 0;
	}

	public static int test_0_vector_t_u1_equal () {
		var elems1 = new byte [] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
		var v1 = new Vector<byte> (elems1);
		var elems2 = new byte [] { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 };
		var v2 = new Vector<byte> (elems2);
		Vector<byte> v = Vector.Equals (v1, v2);
		for (int i = 0; i < Vector<byte>.Count; ++i) {
			if (i % 2 == 0 && v [i] != 0xff)
				return 1;
			if (i % 2 == 1 && v [i] != 0)
				return 1;
		}
		return 0;
	}

	/* op_Explicit () -> Vector<int32> */

	public static int test_0_vector_t_cast_vector_int32 () {
		var v1 = new Vector<long> (new long [] { 0x123456789abcdef0L, 0x23456789abcdef01L });
		var v = (Vector<int>)v1;
		if (BitConverter.IsLittleEndian) {
			if ((uint)v [0] != 0x9abcdef0 || (uint)v [1] != 0x12345678)
				return 1;
			if ((uint)v [2] != 0xabcdef01 || (uint)v [3] != 0x23456789)
				return 2;
		} else {
			if ((uint)v [1] != 0x9abcdef0 || (uint)v [0] != 0x12345678)
				return 1;
			if ((uint)v [3] != 0xabcdef01 || (uint)v [2] != 0x23456789)
				return 2;
		}
		return 0;
	}

	/* Vector.GreaterThanOrEqual */

	public static int test_0_vector_t_i1_ge () {
		var elems1 = new sbyte [] { 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1 };
		var v1 = new Vector<sbyte> (elems1);
		var elems2 = new sbyte [] { 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2 };
		var v2 = new Vector<sbyte> (elems2);
		Vector<sbyte> v = Vector.GreaterThanOrEqual (v1, v2);
		for (int i = 0; i < Vector<sbyte>.Count; ++i) {
			if (i % 2 == 0 && v [i] != -1)
				return 1;
			if (i % 2 == 1 && v [i] != 0)
				return 1;
		}
		return 0;
	}

	public static int test_0_vector_t_i2_ge () {
		var elems1 = new short [] { 1, 1, 0, 1, 1, 1, 0, 1 };
		var v1 = new Vector<short> (elems1);
		var elems2 = new short [] { 0, 2, 0, 2, 0, 2, 0, 2 };
		var v2 = new Vector<short> (elems2);
		Vector<short> v = Vector.GreaterThanOrEqual (v1, v2);
		for (int i = 0; i < Vector<short>.Count; ++i) {
			if (i % 2 == 0 && v [i] != -1)
				return 1;
			if (i % 2 == 1 && v [i] != 0)
				return 1;
		}
		return 0;
	}

	public static int test_0_vector_t_i4_ge () {
		var elems1 = new int [] { 1, 1, 0, 1 };
		var v1 = new Vector<int> (elems1);
		var elems2 = new int [] { 0, 2, 0, 2 };
		var v2 = new Vector<int> (elems2);
		Vector<int> v = Vector.GreaterThanOrEqual (v1, v2);
		if (v [0] != -1 || v [1] != 0 || v [2] != -1 || v[3] != 0)
			return 1;
		return 0;
	}

	public static int test_0_vector_t_i8_ge () {
		var elems1 = new long [] { 1, 1 };
		var v1 = new Vector<long> (elems1);
		var elems2 = new long [] { 0, 1 };
		var v2 = new Vector<long> (elems2);
		Vector<long> v = Vector.GreaterThanOrEqual (v1, v2);
		if (v [0] != -1 || v [1] != -1)
			return 1;
		return 0;
	}

	/* Vector.LessThan */

	public static int test_0_vector_t_i1_lt () {
		var elems1 = new sbyte [] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
		var v1 = new Vector<sbyte> (elems1);
		var elems2 = new sbyte [] { 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2 };
		var v2 = new Vector<sbyte> (elems2);
		Vector<sbyte> v = Vector.LessThan (v2, v1);
		for (int i = 0; i < Vector<sbyte>.Count; ++i) {
			if (i % 2 == 0 && v [i] != -1)
				return 1;
			if (i % 2 == 1 && v [i] != 0)
				return 1;
		}
		return 0;
	}

	/* Vector.GreaterThan */

	public static int test_0_vector_t_i1_gt () {
		var elems1 = new sbyte [] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
		var v1 = new Vector<sbyte> (elems1);
		var elems2 = new sbyte [] { 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2 };
		var v2 = new Vector<sbyte> (elems2);
		Vector<sbyte> v = Vector.GreaterThan (v1, v2);
		for (int i = 0; i < Vector<sbyte>.Count; ++i) {
			if (i % 2 == 0 && v [i] != -1)
				return 1;
			if (i % 2 == 1 && v [i] != 0)
				return 1;
		}
		return 0;
	}

	/* Vector.LessThanOrEqual */
	public static int test_0_vector_t_i1_le () {
		var elems1 = new sbyte [] { 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2 };
		var v1 = new Vector<sbyte> (elems1);
		var elems2 = new sbyte [] { 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1 };
		var v2 = new Vector<sbyte> (elems2);
		Vector<sbyte> v = Vector.LessThanOrEqual (v1, v2);
		for (int i = 0; i < Vector<sbyte>.Count; ++i) {
			if (i % 2 == 0 && v [i] != -1)
				return 1;
			if (i % 2 == 1 && v [i] != 0)
				return 1;
		}
		return 0;
	}

	/* Vector.Abs */

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector<T> vector_t_abs<T> (Vector<T> v1) where T: struct {
		return Vector.Abs (v1);
	}

	public static int test_0_vector_t_u1_abs () {
		var elems1 = new byte [] { 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2 };
		var v1 = new Vector<byte> (elems1);

		if (vector_t_abs (v1) != v1)
			return 1;
		return 0;
	}

	public static int test_0_vector_t_u2_abs () {
		var elems1 = new ushort [] { 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2 };
		var v1 = new Vector<ushort> (elems1);

		if (vector_t_abs (v1) != v1)
			return 1;
		return 0;
	}

	public static int test_0_vector_t_u4_abs () {
		var elems1 = new uint [] { 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2 };
		var v1 = new Vector<uint> (elems1);

		if (vector_t_abs (v1) != v1)
			return 1;
		return 0;
	}

	public static int test_0_vector_t_u8_abs () {
		var elems1 = new ulong [] { 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2 };
		var v1 = new Vector<ulong> (elems1);

		if (vector_t_abs (v1) != v1)
			return 1;
		return 0;
	}

	public static int test_0_vector_t_i1_abs () {
		var elems1 = new sbyte [] { 1, -2, 1, -2, 1, -2, 1, -2, 1, -2, 1, -2, 1, -2, 1, -2 };
		var v1 = new Vector<sbyte> (elems1);
		var elems2 = new sbyte [] { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 };
		var v2 = new Vector<sbyte> (elems2);

		if (vector_t_abs (v1) != v2)
			return 1;
		return 0;
	}

	// Vector<T>.Add
	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector<T> vector_add<T> (Vector<T> v1, Vector<T> v2) where T: struct {
		return v1 + v2;
	}

	public static int test_0_vector_byte_add () {
		var v1 = new Vector<byte> (new byte[] { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 });
		var v2 = new Vector<byte> (new byte[] { 2, 4, 2, 4, 2, 4, 2, 4, 2, 4, 2, 4, 2, 4, 2, 4 });

		var res = vector_add (v1, v1);
		if (res != v2)
			return 1;
		return 0;
	}

	public static int test_0_vector_sbyte_add () {
		var v1 = new Vector<sbyte> (new sbyte[] { 1, -1, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1 });
		var v2 = new Vector<sbyte> (new sbyte[] { 2, -2, 2, -2, 2, -2, 2, -2, 2, -2, 2, -2, 2, -2, 2, -2 });

		var res = vector_add (v1, v1);
		if (res != v2)
			return 1;
		return 0;
	}

	public static int test_0_vector_ushort_add () {
		var v1 = new Vector<ushort> (new ushort[] { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 });
		var v2 = new Vector<ushort> (new ushort[] { 2, 4, 2, 4, 2, 4, 2, 4, 2, 4, 2, 4, 2, 4, 2, 4 });

		var res = vector_add (v1, v1);
		if (res != v2)
			return 1;
		return 0;
	}

	public static int test_0_vector_short_add () {
		var v1 = new Vector<short> (new short[] { 1, -1, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1 });
		var v2 = new Vector<short> (new short[] { 2, -2, 2, -2, 2, -2, 2, -2, 2, -2, 2, -2, 2, -2, 2, -2 });

		var res = vector_add (v1, v1);
		if (res != v2)
			return 1;
		return 0;
	}

	public static int test_0_vector_uint_add () {
		var v1 = new Vector<uint> (new uint[] { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 });
		var v2 = new Vector<uint> (new uint[] { 2, 4, 2, 4, 2, 4, 2, 4, 2, 4, 2, 4, 2, 4, 2, 4 });

		var res = vector_add (v1, v1);
		if (res != v2)
			return 1;
		return 0;
	}

	public static int test_0_vector_int_add () {
		var v1 = new Vector<int> (new int[] { 1, -1, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1 });
		var v2 = new Vector<int> (new int[] { 2, -2, 2, -2, 2, -2, 2, -2, 2, -2, 2, 2, 2, -2, 2, -2 });

		var res = vector_add (v1, v1);
		if (res != v2)
			return 1;
		return 0;
	}

	public static int test_0_vector_double_add () {
		var v1 = new Vector<double> (new double[] { 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0 });
		var v2 = new Vector<double> (new double[] { 2.0, -2.0, 2.0, -2.0, 2.0, -2.0, 2.0, -2.0, 2.0, -2.0, 2.0, 2.0, 2.0, -2.0, 2.0, -2.0 });

		var res = vector_add (v1, v1);
		if (res != v2)
			return 1;
		return 0;
	}

	public static int test_0_vector_float_add () {
		var v1 = new Vector<float> (new float[] { 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f });
		var v2 = new Vector<float> (new float[] { 2.0f, -2.0f, 2.0f, -2.0f, 2.0f, -2.0f, 2.0f, -2.0f, 2.0f, -2.0f, 2.0f, 2.0f, 2.0f, -2.0f, 2.0f, -2.0f });

		var res = vector_add (v1, v1);
		if (res != v2)
			return 1;
		return 0;
	}

	// Vector<T>.op_Subtraction

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector<T> vector_sub<T> (Vector<T> v1, Vector<T> v2) where T: struct {
		return v1 - v2;
	}

	public static int test_0_vector_byte_sub () {
		var v1 = new Vector<byte> (new byte[] { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 });

		var res = vector_sub (v1, v1);
		if (res != Vector<byte>.Zero)
			return 1;
		return 0;
	}

	public static int test_0_vector_double_sub () {
		var v1 = new Vector<double> (new double[] { 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0 });

		var res = vector_sub (v1, v1);
		if (res != Vector<double>.Zero)
			return 1;
		return 0;
	}

	public static int test_0_vector_float_sub () {
		var v1 = new Vector<float> (new float[] { 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f });

		var res = vector_sub (v1, v1);
		if (res != Vector<float>.Zero)
			return 1;
		return 0;
	}

	// Vector<T>.op_Multiply
	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector<T> vector_mul<T> (Vector<T> v1, Vector<T> v2) where T: struct {
		return v1 * v2;
	}

	public static int test_0_vector_int_mul () {
		var v1 = new Vector<int> (new int[] { 1, 2, -1, 2, 1, 2, -1, 2, 1, -2, 1, 2, 1, 2, -1, 2 });
		var v2 = new Vector<int> (new int[] { 1, 4, 1, 4, 1, 4, 1, 4, 1, 4, 1, 4, 1, 4, 1, 4 });

		var res = vector_mul (v1, v1);
		if (res != v2)
			return 1;
		return 0;
	}

	public static int test_0_vector_double_mul () {
		var v1 = new Vector<double> (new double[] { 2.0, -1.0, 2.0, -1.0 });
		var v2 = new Vector<double> (new double[] { 4.0, 1.0, 4.0, 1.0 });

		var res = vector_mul (v1, v1);
		if (res != v2)
			return 1;
		return 0;
	}

	public static int test_0_vector_float_mul () {
		var v1 = new Vector<float> (new float[] { 2.0f, -1.0f, 2.0f, -1.0f });
		var v2 = new Vector<float> (new float[] { 4.0f, 1.0f, 4.0f, 1.0f });

		var res = vector_mul (v1, v1);
		if (res != v2)
			return 1;
		return 0;
	}

	// Vector<T>.op_Division
	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static Vector<T> vector_div<T> (Vector<T> v1, Vector<T> v2) where T: struct {
		return v1 / v2;
	}

	public static int test_0_vector_double_div () {
		var v1 = new Vector<double> (new double[] { 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0 });
		var v2 = new Vector<double> (new double[] { 1.0, 1.0, 1.0, 1.0 });

		var res = vector_div (v1, v1);
		if (res != v2)
			return 1;
		return 0;
	}

	public static int test_0_vector_float_div () {
		var v1 = new Vector<float> (new float[] { 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f });
		var v2 = new Vector<float> (new float[] { 1.0f, 1.0f, 1.0f, 1.0f });

		var res = vector_div (v1, v1);
		if (res != v2)
			return 1;
		return 0;
	}

	// Vector<T>.CopyTo
	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void vector_copyto<T> (Vector<T> v1, T[] array, int index) where T: struct {
		v1.CopyTo (array, index);
	}

	public static int test_0_vector_byte_copyto () {
		var v1 = new Vector<byte> (new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });

		byte[] arr = new byte [256];
		vector_copyto (v1, arr, 1);
		for (int i = 0; i < 16; ++i)
			if (arr [i + 1] != (i + 1))
				return 1;
		vector_copyto (v1, arr, 240);
		try {
			vector_copyto (v1, arr, 241);
			return 1;
		} catch (ArgumentException) {
		}
		return 0;
	}
}
