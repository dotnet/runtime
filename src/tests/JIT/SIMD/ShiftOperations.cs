using System;
using System.Runtime.CompilerServices;
using System.Numerics;

public class Test
{	
	 
	[MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Shl(uint x, int y) => x<< y;
	
	[MethodImpl(MethodImplOptions.NoInlining)]
    private static int Sar(int x, int y) => x >> y;
 
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Shr(uint x, int y) => x >> y;
	
	[MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Ror(uint x) => BitOperations.RotateRight(x, 2);

  	[MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Shlx(ulong x, int y) => x << y;

	[MethodImpl(MethodImplOptions.NoInlining)]
    private static long Sarx(long x, int y) => x >> y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Shrx(ulong x, int y) => x >> y;

	[MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe ulong ShrxRef(ulong *x, int y) => *x >> y;
		
 	[MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Rorx(ulong x) => BitOperations.RotateRight(x, 2);
	
public static unsafe int Main()
{
	try
	{
		uint  valUInt = 0xFFFFFFFE;
		int   valInt = 8;		
		ulong valULong = 8;
		long  valLong = 8;
		int   shiftBy = 1;		
		uint  resUInt = 0;
		int   resInt = 0;
		ulong resULong = 0;
		long  resLong = 0;
		uint  expectedUInt = 0;
		int   expectedInt = 0;
		ulong expectedULong = 0;
		long  expectedLong = 0;
		int   MOD32 = 32;
		int   MOD64 = 64;
		
		//
		// shl tests
		//
		valUInt = 0;
		shiftBy = 1;
		resUInt = Shl(valUInt, shiftBy);
		expectedUInt = (uint) (valUInt * Math.Pow(2, (shiftBy % MOD32)));
		Console.Write("UnitTest Shl({0},{1}): {2}", valUInt, shiftBy, resUInt);
		if (resUInt != expectedUInt)
		{
			Console.Write(" != {0} Failed.\n", expectedUInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedUInt);
		
		valUInt = 8;
		shiftBy = 1;
		resUInt = Shl(valUInt, shiftBy);
		expectedUInt = (uint) (valUInt * Math.Pow(2, (shiftBy % MOD32)));
		Console.Write("UnitTest Shl({0},{1}): {2}", valUInt, shiftBy, resUInt);
		if (resUInt != expectedUInt)
		{
			Console.Write(" != {0} Failed.\n", expectedUInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedUInt);		
		
		valUInt = 1;
		shiftBy = 31;
		resUInt = Shl(valUInt, shiftBy);
		expectedUInt = (uint) (valUInt * Math.Pow(2, (shiftBy % MOD32)));
		Console.Write("UnitTest Shl({0},{1}): {2}", valUInt, shiftBy, resUInt);
		if (resUInt != expectedUInt)
		{
			Console.Write(" != {0} Failed.\n", expectedUInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedUInt);			

		valUInt = 1;
		shiftBy = 33;
		resUInt = Shl(valUInt, shiftBy);
		expectedUInt = (uint) (valUInt * Math.Pow(2, (shiftBy % MOD32)));
		Console.Write("UnitTest Shl({0},{1}): {2}", valUInt, shiftBy, resUInt);
		if (resUInt != expectedUInt)
		{
			Console.Write(" != {0} Failed.\n", expectedUInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedUInt);	
		
		valUInt = 0xFFFFFFFF;
		shiftBy = 1;
		resUInt = Shl(valUInt, shiftBy);
		expectedUInt = (uint) (valUInt * Math.Pow(2, shiftBy));
		Console.Write("UnitTest Shl({0},{1}): {2}", valUInt, shiftBy, resUInt);
		if (resUInt != expectedUInt)
		{
			Console.Write(" != {0} Failed.\n", expectedUInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedUInt);

		// 
		// sar tests
		//
		valInt = 0;
		shiftBy = 1;
		resInt = Sar(valInt, shiftBy);
		expectedInt = (int) (valInt / Math.Pow(2, (shiftBy % MOD32)));
		Console.Write("UnitTest Sar({0},{1}): {2}", valInt, shiftBy, resInt);
		if (resInt != expectedInt)
		{
			Console.Write(" != {0} Failed.\n", expectedInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedInt);
		
		valInt = -8;
		shiftBy = 1;
		resInt = Sar(valInt, shiftBy);
		expectedInt = (int) (valInt / Math.Pow(2, (shiftBy % MOD32)));
		Console.Write("UnitTest Sar({0},{1}): {2}", valInt, shiftBy, resInt);
		if (resInt != expectedInt)
		{
			Console.Write(" != {0} Failed.\n", expectedInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedInt);		
		
		valInt = 1;
		shiftBy = 33;
		resInt = Sar(valInt, shiftBy);
		expectedInt = (int) (valInt / Math.Pow(2, (shiftBy % MOD32)));
		Console.Write("UnitTest Sar({0},{1}): {2}", valInt, shiftBy, resInt);
		if (resInt != expectedInt)
		{
			Console.Write(" != {0} Failed.\n", expectedInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedInt);			

		valInt = 0x7FFFFFFF;
		shiftBy = 33;
		resInt = Sar(valInt, shiftBy);
		expectedInt = (int) (valInt / Math.Pow(2, (shiftBy % MOD32)));
		Console.Write("UnitTest Sar({0},{1}): {2}", valInt, shiftBy, resInt);
		if (resInt != expectedInt)
		{
			Console.Write(" != {0} Failed.\n", expectedInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedInt);	
		
		valInt = 0x7FFFFFFF;
		shiftBy = 30;
		resInt = Sar(valInt, shiftBy);
		expectedInt = (int) (valInt / Math.Pow(2, shiftBy));
		Console.Write("UnitTest Sar({0},{1}): {2}", valInt, shiftBy, resInt);
		if (resInt != expectedInt)
		{
			Console.Write(" != {0} Failed.\n", expectedInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedInt);		

		//
		// shr tests
		//
		valUInt = 1;
		shiftBy = 1;
		resUInt = Shr(valUInt, shiftBy);
		expectedUInt = (uint) (valUInt / Math.Pow(2, (shiftBy % MOD32)));
		Console.Write("UnitTest Shr({0},{1}): {2}", valUInt, shiftBy, resUInt);
		if (resUInt != expectedUInt)
		{
			Console.Write(" != {0} Failed.\n", expectedUInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedUInt);
		
		valUInt = 8;
		shiftBy = 2;
		resUInt = Shr(valUInt, shiftBy);
		expectedUInt = (uint) (valUInt / Math.Pow(2, (shiftBy % MOD32)));
		Console.Write("UnitTest Shr({0},{1}): {2}", valUInt, shiftBy, resUInt);
		if (resUInt != expectedUInt)
		{
			Console.Write(" != {0} Failed.\n", expectedUInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedUInt);
		
		valUInt = 1;
		shiftBy = 33;
		resUInt = Shr(valUInt, shiftBy);
		expectedUInt = (uint) (valUInt / Math.Pow(2, shiftBy));
		Console.Write("UnitTest Shr({0},{1}): {2}", valUInt, shiftBy, resUInt);
		if (resUInt != expectedUInt)
		{
			Console.Write(" != {0} Failed.\n", expectedUInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedUInt);
		
		valUInt = 0xFFFFFFFF;
		shiftBy = 31;
		resUInt = Shr(valUInt, shiftBy);
		expectedUInt = (uint) (valUInt / Math.Pow(2, (shiftBy % MOD32)));
		Console.Write("UnitTest Shr({0},{1}): {2}", valUInt, shiftBy, resUInt);
		if (resUInt != expectedUInt)
		{
			Console.Write(" != {0} Failed.\n", expectedUInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedUInt);			

		valUInt = 0xFFFFFFFF;
		shiftBy = 33;
		resUInt = Shr(valUInt, shiftBy);
		expectedUInt = (uint) (valUInt / Math.Pow(2, (shiftBy % MOD32)));
		Console.Write("UnitTest Shr({0},{1}): {2}", valUInt, shiftBy, resUInt);
		if (resUInt != expectedUInt)
		{
			Console.Write(" != {0} Failed.\n", expectedUInt);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedUInt);	
		
		//
		// Ror tests
		//
		valUInt = 0xFF;
		shiftBy = 2;
		resUInt = Ror(valUInt);
		Console.Write("UnitTest Ror({0},{1}): {2}", valUInt, shiftBy, resUInt);
		if (resUInt != 0xC000003F)
		{
			Console.Write(" Failed.\n");
			return 101;
		}
		Console.Write(" Passed.\n");			
		
		//
		// Shlx tests
		//
		valULong = 0;
		shiftBy = 1;
		resULong = Shlx(valULong, shiftBy);
		expectedULong = (ulong) (valULong * Math.Pow(2, (shiftBy % MOD64)));
		Console.Write("UnitTest Shlx({0},{1}): {2}", valULong, shiftBy, resULong);
		if (resULong != expectedULong)
		{
			Console.Write(" != {0} Failed.\n", expectedULong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedULong);
		
		valULong = 8;
		shiftBy = 1;
		resULong = Shlx(valULong, shiftBy);
		expectedULong = (ulong) (valULong * Math.Pow(2, (shiftBy % MOD64)));
		Console.Write("UnitTest Shlx({0},{1}): {2}", valULong, shiftBy, resULong);
		if (resULong != expectedULong)
		{
			Console.Write(" != {0} Failed.\n", expectedULong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedULong);		
		
		valULong = 1;
		shiftBy = 31;
		resULong = Shlx(valULong, shiftBy);
		expectedULong = (ulong) (valULong * Math.Pow(2, (shiftBy % MOD64)));
		Console.Write("UnitTest Shlx({0},{1}): {2}", valULong, shiftBy, resULong);
		if (resULong != expectedULong)
		{
			Console.Write(" != {0} Failed.\n", expectedULong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedULong);			

		valULong = 1;
		shiftBy = 33;
		resULong = Shlx(valULong, shiftBy);
		expectedULong = (ulong) (valULong * Math.Pow(2, (shiftBy % MOD64)));
		Console.Write("UnitTest Shlx({0},{1}): {2}", valULong, shiftBy, resULong);
		if (resULong != expectedULong)
		{
			Console.Write(" != {0} Failed.\n", expectedULong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedULong);	
		
		valULong = 0xFFFFFFFF;
		shiftBy = 1;
		resULong = Shlx(valULong, shiftBy);
		expectedULong = (ulong) (valULong * Math.Pow(2, shiftBy));
		Console.Write("UnitTest Shlx({0},{1}): {2}", valULong, shiftBy, resULong);
		if (resULong != expectedULong)
		{
			Console.Write(" != {0} Failed.\n", expectedULong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedULong);

		// 
		// Sarx tests
		//
		valLong = 1;
		shiftBy = 1;
		resLong = Sarx(valLong, shiftBy);
		expectedLong = (long) (valLong / Math.Pow(2, (shiftBy % MOD64)));
		Console.Write("UnitTest Sarx({0},{1}): {2}", valLong, shiftBy, resLong);
		if (resLong != expectedLong)
		{
			Console.Write(" != {0} Failed.\n", expectedLong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedLong);
		
		valLong = -8;
		shiftBy = 1;
		resLong = Sarx(valLong, shiftBy);
		expectedLong = (long) (valLong / Math.Pow(2, (shiftBy % MOD64)));
		Console.Write("UnitTest Sarx({0},{1}): {2}", valLong, shiftBy, resLong);
		if (resLong != expectedLong)
		{
			Console.Write(" != {0} Failed.\n", expectedLong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedLong);		
		
		valLong = -8;
		shiftBy = 65;
		resLong = Sarx(valLong, shiftBy);
		expectedLong = (long) (valLong / Math.Pow(2, (shiftBy % MOD64)));
		Console.Write("UnitTest Sarx({0},{1}): {2}", valLong, shiftBy, resLong);
		if (resLong != expectedLong)
		{
			Console.Write(" != {0} Failed.\n", expectedLong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedLong);			

		valLong = 0x7FFFFFFFFFFFFFFF;
		shiftBy = 63;
		resLong = Sarx(valLong, shiftBy);
		expectedLong = 0;
		Console.Write("UnitTest Sarx({0},{1}): {2}", valLong, shiftBy, resLong);
		if (resLong != 0)
		{
			Console.Write(" != {0} Failed.\n", expectedLong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedLong);
		
		valLong = 0x7FFFFFFFFFFFFFFF;
		shiftBy = 65;
		resLong = Sarx(valLong, shiftBy);
		expectedLong = 0x3FFFFFFFFFFFFFFF;
		Console.Write("UnitTest Sarx({0},{1}): {2}", valLong, shiftBy, resLong);
		if (resLong != expectedLong)
		{
			Console.Write(" != {0} Failed.\n", expectedLong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedLong);

		//
		// Shrx tests
		//
		valULong = 1;
		shiftBy = 1;
		resULong = Shrx(valULong, shiftBy);
		expectedULong = (ulong) (valULong / Math.Pow(2, (shiftBy % MOD64)));
		Console.Write("UnitTest Shrx({0},{1}): {2}", valULong, shiftBy, resULong);
		if (resULong != expectedULong)
		{
			Console.Write(" != {0} Failed.\n", expectedULong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedULong);
		
		valULong = 8;
		shiftBy = 2;
		resULong = Shrx(valULong, shiftBy);
		expectedULong = (ulong) (valULong / Math.Pow(2, (shiftBy % MOD64)));
		Console.Write("UnitTest Shrx({0},{1}): {2}", valULong, shiftBy, resULong);
		if (resULong != expectedULong)
		{
			Console.Write(" != {0} Failed.\n", expectedULong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedULong);
		
		valULong = 0xFFFFFFFFFFFFFFFF;
		shiftBy = 63;
		resULong = Shrx(valULong, shiftBy);
		expectedULong = 1;
		Console.Write("UnitTest Shrx({0},{1}): {2}", valULong, shiftBy, resULong);
		if (resULong != expectedULong)
		{
			Console.Write(" != {0} Failed.\n", expectedULong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedULong);			

		valULong = 0x7FFFFFFFFFFFFFFF;
		shiftBy = 65;
		resULong = Shrx(valULong, shiftBy);
		expectedULong = 0x3FFFFFFFFFFFFFFF;
		Console.Write("UnitTest Shrx({0},{1}): {2}", valULong, shiftBy, resULong);
		if (resULong != expectedULong)
		{
			Console.Write(" != {0} Failed.\n", expectedULong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedULong);	

		valULong = 8;
		shiftBy = 65;
		resULong = Shrx(valULong, shiftBy);
		expectedULong = 4;
		Console.Write("UnitTest Shrx({0},{1}): {2}", valULong, shiftBy, resULong);
		if (resULong != expectedULong)
		{
			Console.Write(" != {0} Failed.\n", expectedULong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedULong);	
		
		//
		// ShrxRef
		//
		valULong = 8;
		shiftBy = 1;
		resULong = ShrxRef(&valULong, shiftBy);		
		expectedULong = (ulong) (valULong / Math.Pow(2, (shiftBy % MOD64)));		
		Console.Write("UnitTest ShrxRef({0},{1}): {2}", valULong, shiftBy, resULong);
		if (resULong != expectedULong)
		{
			Console.Write(" != {0} Failed.\n", expectedULong);
			return 101;
		}
		Console.Write(" == {0} Passed.\n", expectedULong);
		
		//
		// Rorx tests
		//
		valULong = 0xFF;
		shiftBy = 2;
		resULong = Rorx(valULong);
		Console.Write("UnitTest Rorx({0},{1}): {2}", valULong, shiftBy, resULong);
		if (resULong != 0xC00000000000003F)
		{
			Console.Write(" Failed.\n");
			return 101;
		}
		Console.Write(" Passed.\n");	
		
	}
	catch (Exception e)
	{
		Console.WriteLine(e.Message);
		return 101;
	}
	Console.WriteLine("PASSED");
	return 100;		
}
}