using System;

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

/*
This is a stress test for descriptors.
*/
class Driver {
	static char[] FOO = new char[] { 'f', 'o', 'b' };

	static void Fill (int cycles) {
		object[] root = new object [12];
		object[] current = root;
		for (int i = 0; i < cycles; ++i) {
			current [0] = new object [12];
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
	
			current = (object[])current [0];
		}
	}

	static void Main () {
		int loops = 3;
		int cycles = 200000;
		for (int i = 0; i < loops; ++i) {
			Fill (cycles);
		}
	}
}
