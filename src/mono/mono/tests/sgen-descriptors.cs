using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public struct SmallMixed
{
	public int a;
	public object b;
	public int c;
	public object d;
}

public struct LargeMixed {
	public SmallMixed a,b,c,d,e;
}

public class SmallBitMap {
	public SmallMixed a;
}

public class LargeBitMap {
	public SmallMixed a;
	public long b,c,d,e,f,g,h;
	public SmallMixed k;
}

public class ComplexBitMap {
	public SmallMixed a,b,c,d,e,f,g,h,i,j,k,l;
}

public class PtrFree {
	public int a,b,c,d;
}

public struct LargeStruct {
	public long a0,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15;
}

public struct LargeStruct2 {
	public LargeStruct a0,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15;
}

public struct LargeStruct3 {
	public LargeStruct2 a0,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15;
}

public class HugePtrFree {
	public LargeStruct3 a,b;
	public LargeStruct2 c;
}

[StructLayout (LayoutKind.Sequential)]
public class Non32bitBitmap {
	public object o;
	public long i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12, i13, i14, i15, i16, i17, i18, i19, i20, i21, i22, i23, i24, i25, i26, i27, i28, i29, i30, i31, i32, i33, i34, i35;
	public object o2;
}

/*
This is a stress test for descriptors.
*/
class Driver {
	static char[] FOO = new char[] { 'f', 'o', 'b' };

	static void Fill (int cycles) {
		object[] root = new object [13];
		object[] current = root;
		for (int i = 0; i < cycles; ++i) {
			current [0] = new object [13];
			current [1] = new int [6];
			current [2] = new int [2,3];
			current [3] = new string (FOO);
			current [4] = new SmallBitMap ();
			current [5] = new LargeBitMap ();
			current [6] = new ComplexBitMap ();
			current [7] = new PtrFree ();
			current [8] = new SmallMixed [3];
			current [9] = new LargeMixed [3];

			if ((i % 50000) == 0)
				current [10] = new HugePtrFree ();
			if ((i %  10000) == 0)
				current [11] = new LargeStruct2 [1];

			/* Test for 64 bit bitmap descriptors (#14834) */
			current [12] = new Non32bitBitmap () { o = new object (), i32 = 1, i33 = 1, i34 = 1, i35 = 1, o2 = new object () };
	
			current = (object[])current [0];
		}
	}

	static unsafe void FillPtr (int cycles) {
		var l = new List<Byte*[]> ();
		for (int i = 0; i < cycles; ++i)
		{
			var a = new Byte* [128];
			for (int j = 0; j < a.Length; ++j)
				a [j] = (Byte*) new IntPtr (j).ToPointer ();
			if (i < 1000)
				l.Add (a);
			else
				l [i % 1000] = a;
		}
	}

	static void Main () {
		int loops = 3;
		int cycles = 200000;
		for (int i = 0; i < loops; ++i) {
			Fill (cycles);
			FillPtr (cycles);
		}
	}
}
