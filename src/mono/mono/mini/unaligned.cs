using System;
using System.Runtime.CompilerServices;
using Mono;

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
namespace UnalignedTests
{
#endif


class Tests {

#if !__MOBILE__
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}
#endif


	public static unsafe int test_0_ldobj_r4 ()
	{
		byte *ptr = stackalloc byte [32];
		float f = (float)123.44f;
		*(float*)ptr = (float)f;

		int expected = *(int*)ptr;

		Intrinsics.UnalignedStobj<int> (ptr + 1, expected);
		/* we can loose some precision due to r4<->r8 conversions */
		if (Math.Abs (Intrinsics.UnalignedLdobj<float> (ptr + 1) - f) > 0.01f)
			return 1;

		return 0;
	}

	public static unsafe int test_0_ldobj_r8 ()
	{
		byte *ptr = stackalloc byte [32];
		double f = 34423.44f;
		*(double*)ptr = (double)f;

		long expected = *(long*)ptr;

		Intrinsics.UnalignedStobj<long> (ptr + 3, expected);
		if (Intrinsics.UnalignedLdobj<double> (ptr + 3) != f)
			return 1;

		return 0;
	}


	public static unsafe int test_0_ldobj ()
	{
		byte *ptr = stackalloc byte [20];
		for (int i = 0; i < 20; ++i)
			ptr [i] = (byte)i;

		if (BitConverter.IsLittleEndian) {

			if (Intrinsics.UnalignedLdobj<short> (ptr + 0) != 0x0100)
				return 1;

			if (Intrinsics.UnalignedLdobj<short> (ptr + 1) != 0x0201)
				return 2;

			if (Intrinsics.UnalignedLdobj<short> (ptr + 2) != 0x0302)
				return 3;

			if (Intrinsics.UnalignedLdobj<int> (ptr + 1) != 0x04030201)
				return 4;

			if (Intrinsics.UnalignedLdobj<int> (ptr + 2) != 0x05040302)
				return 5;

			if (Intrinsics.UnalignedLdobj<long> (ptr + 1) != 0x0807060504030201)
				return 6;

			if (Intrinsics.UnalignedLdobj<long> (ptr + 6) != 0xD0C0B0A09080706)
				return 7;
		} else {

			if (Intrinsics.UnalignedLdobj<short> (ptr + 0) != 0x0001)
				return 1;

			if (Intrinsics.UnalignedLdobj<short> (ptr + 1) != 0x0102)
				return 2;

			if (Intrinsics.UnalignedLdobj<short> (ptr + 2) != 0x0203)
				return 3;

			if (Intrinsics.UnalignedLdobj<int> (ptr + 1) != 0x01020304)
				return 4;

			if (Intrinsics.UnalignedLdobj<int> (ptr + 2) != 0x02030405)
				return 5;

			if (Intrinsics.UnalignedLdobj<long> (ptr + 1) != 0x0102030405060708)
				return 6;

			if (Intrinsics.UnalignedLdobj<long> (ptr + 6) != 0x60708090A0B0C0D)
				return 7;
		}

		return 0;
	}

	public static unsafe int test_0_ldind ()
	{
		byte *ptr = stackalloc byte [20];
		for (int i = 0; i < 20; ++i)
			ptr [i] = (byte)i;


		if (BitConverter.IsLittleEndian) {

			if (Intrinsics.UnalignedLdInd2 (ptr + 0) != 0x0100)
				return 1;

			if (Intrinsics.UnalignedLdInd2 (ptr + 1) != 0x0201)
				return 2;

			if (Intrinsics.UnalignedLdInd2 (ptr + 2) != 0x0302)
				return 3;

			if (Intrinsics.UnalignedLdInd4 (ptr + 1) != 0x04030201)
				return 4;

			if (Intrinsics.UnalignedLdInd4 (ptr + 2) != 0x05040302)
				return 5;

			if (Intrinsics.UnalignedLdInd8 (ptr + 1) != 0x0807060504030201)
				return 6;

			if (Intrinsics.UnalignedLdInd8 (ptr + 6) != 0xD0C0B0A09080706)
				return 7;
		} else {

			if (Intrinsics.UnalignedLdInd2 (ptr + 0) != 0x0001)
				return 1;

			if (Intrinsics.UnalignedLdInd2 (ptr + 1) != 0x0102)
				return 2;

			if (Intrinsics.UnalignedLdInd2 (ptr + 2) != 0x0203)
				return 3;

			if (Intrinsics.UnalignedLdInd4 (ptr + 1) != 0x01020304)
				return 4;

			if (Intrinsics.UnalignedLdInd4 (ptr + 2) != 0x02030405)
				return 5;

			if (Intrinsics.UnalignedLdInd8 (ptr + 1) != 0x0102030405060708)
				return 6;

			if (Intrinsics.UnalignedLdInd8 (ptr + 6) != 0x60708090A0B0C0D)
				return 7;
		}

		return 0;
	}
	public static unsafe int test_0_cpobj ()
	{
		byte *dest = stackalloc byte [20];
		byte *src = stackalloc byte [20];
		for (int i = 0; i < 20; ++i)
			src [i] = (byte)i;

		Intrinsics.UnalignedCpobj<short> (dest + 0, src + 0);
		if (dest [0] != src [0] || dest [1] != src [1])
			return 1;

		Intrinsics.UnalignedCpobj<short> (dest + 1, src + 0);
		if (dest [1] != src [0] || dest [2] != src [1])
			return 2;

		Intrinsics.UnalignedCpobj<short> (dest + 0, src + 1);
		if (dest [0] != src [1] || dest [1] != src [2])
			return 3;

		Intrinsics.UnalignedCpobj<short> (dest + 1, src + 1);
		if (dest [1] != src [1] || dest [2] != src [2])
			return 3;

		Intrinsics.UnalignedCpobj<int> (dest + 3, src);
		for (int i = 0; i < 4; ++i) {
			if (dest [i + 3] != src [i])
				return 4;
		}

		Intrinsics.UnalignedCpobj<int> (dest + 1, src + 2);
		for (int i = 0; i < 4; ++i) {
			if (dest [i + 1] != src [i + 2])
				return 5;
		}

		Intrinsics.UnalignedCpobj<long> (dest + 1, src + 2);
		for (int i = 0; i < 8; ++i) {
			if (dest [i + 1] != src [i + 2])
				return 6;
		}

		Intrinsics.UnalignedCpobj<long> (dest + 7, src + 2);
		for (int i = 0; i < 8; ++i) {
			if (dest [i + 7] != src [i + 2])
				return 7;
		}

		return 0;
	}

	public static unsafe int test_0_stobj ()
	{
		byte *ptr = stackalloc byte [20];

		if (BitConverter.IsLittleEndian) {
			Intrinsics.UnalignedStobj <short> (ptr + 0, 0x6688);
			if (ptr [0] != 0x88 || ptr [1] != 0x66)
				return 1;

			Intrinsics.UnalignedStobj <short> (ptr + 1, 0x6589);
			if (ptr [1] != 0x89 || ptr [2] != 0x65)
				return 2;

			Intrinsics.UnalignedStobj <int> (ptr + 1, 0x60708090);
			if (ptr [1] != 0x90 || ptr [2] != 0x80 || ptr [3] != 0x70 || ptr [4] != 0x60)
				return 3;

			Intrinsics.UnalignedStobj <long> (ptr + 1, 0x405060708090);
			if (ptr [1] != 0x90 || ptr [2] != 0x80 || ptr [3] != 0x70 || 
			    ptr [4] != 0x60 || ptr [5] != 0x50 || ptr [6] != 0x40)
				return 4;
		} else {
			Intrinsics.UnalignedStobj <short> (ptr + 0, 0x6688);
			if (ptr [0] != 0x66 || ptr [1] != 0x88)
				return 1;

			Intrinsics.UnalignedStobj <short> (ptr + 1, 0x6589);
			if (ptr [1] != 0x65 || ptr [2] != 0x89)
				return 2;

			Intrinsics.UnalignedStobj <int> (ptr + 1, 0x60708090);
			if (ptr [1] != 0x60 || ptr [2] != 0x70 || ptr [3] != 0x80 || ptr [4] != 0x90)
				return 3;

			Intrinsics.UnalignedStobj <long> (ptr + 1, 0x2030405060708090);
			if (ptr [1] != 0x20 || ptr [2] != 0x30 || ptr [3] != 0x40 || 
			    ptr [4] != 0x50 || ptr [5] != 0x60 || ptr [6] != 0x70)
				return 4;
		}

		return 0;
	}

	public static unsafe int test_0_ldobj_stobj ()
	{
		byte *dest = stackalloc byte [20];
		byte *src = stackalloc byte [20];

		for (int i = 0; i < 20; ++i)
			src [i] = (byte)i;

		Intrinsics.UnalignedLdobjStObjPair<short> (dest + 0, src + 0);
		if (dest [0] != src [0] || dest [1] != src [1])
			return 1;

		Intrinsics.UnalignedLdobjStObjPair<short> (dest + 1, src + 0);
		if (dest [1] != src [0] || dest [2] != src [1])
			return 2;

		Intrinsics.UnalignedLdobjStObjPair<short> (dest + 0, src + 1);
		if (dest [0] != src [1] || dest [1] != src [2])
			return 3;

		Intrinsics.UnalignedLdobjStObjPair<short> (dest + 1, src + 1);
		if (dest [1] != src [1] || dest [2] != src [2])
			return 3;

		Intrinsics.UnalignedLdobjStObjPair<int> (dest + 1, src + 1);
		if (dest [1] != src [1] || dest [2] != src [2])
			return 4;

		Intrinsics.UnalignedLdobjStObjPair<long> (dest + 1, src + 1);
		if (dest [1] != src [1] || dest [2] != src [2])
			return 5;


		return 0;
	}


	public static unsafe int test_0_cpblk ()
	{
		byte *dest = stackalloc byte [20];
		byte *src = stackalloc byte [20];
		for (int i = 0; i < 20; ++i)
			src [i] = (byte)i;


		Intrinsics.UnalignedCpblk (dest + 0, src + 0, 2);
		if (dest [0] != src [0] || dest [1] != src [1])
			return 1;

		Intrinsics.UnalignedCpblk (dest + 1, src + 0, 2);
		if (dest [1] != src [0] || dest [2] != src [1])
			return 2;

		Intrinsics.UnalignedCpblk (dest + 0, src + 1, 2);
		if (dest [0] != src [1] || dest [1] != src [2])
			return 3;

		Intrinsics.UnalignedCpblk (dest + 1, src + 1, 2);
		if (dest [1] != src [1] || dest [2] != src [2])
			return 3;

		Intrinsics.UnalignedCpblk (dest + 1, src + 1, 4);
		for (int i = 0; i < 4; ++i) {
			if (dest [i + 1] != src [i + 1])
				return 4;
		}

		Intrinsics.UnalignedCpblk (dest + 1, src + 1, 8);
		for (int i = 0; i < 8; ++i) {
			if (dest [i + 1] != src [i + 1])
				return 5;
		}

		return 0;
	}


	public static unsafe int test_0_initblk ()
	{
		byte *ptr = stackalloc byte [20];

		for (int i = 0; i < 20; ++i)
			ptr [i] = (byte)i;

		Intrinsics.UnalignedInit (ptr, 30, 2);
		if (ptr [0] != 30 || ptr [1] != 30)
			return 1;

		Intrinsics.UnalignedInit (ptr + 1, 31, 2);
		if (ptr[0] != 30 || ptr [1] != 31 || ptr [2] != 31)
			return 2;

		return 0;
	}
}

#if __MOBILE__
}
#endif



