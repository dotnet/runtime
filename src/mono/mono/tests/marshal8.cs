using System;
using System.Runtime.InteropServices;


[StructLayout(LayoutKind.Sequential, Size=1024)]
public class Dummy {
	[MarshalAs(UnmanagedType.ByValArray, SizeConst=16)]
	public byte[]	a;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst=16)]
	public float[]	b;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst=16)]
	public long[]	c;
}

public class X {
	public static unsafe int Main () {

		///
		///	Structure to pointer
		///

		Dummy dummy = new Dummy ();
		dummy.a = new byte[16];
		dummy.b = new float[16];
		dummy.c = new long[16];

		for(int i=0; i<16; i++)
			dummy.a[i] = (byte)(dummy.b[i] = dummy.c[i] = i+1);

		IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Dummy)));
		Marshal.StructureToPtr(dummy, p, false);

		int offset = (int)Marshal.OffsetOf(typeof(Dummy), "a");
		byte *data1 = (byte*)p.ToPointer() + offset;
		for(int i=0; i<16; i++) {
			if(data1[i] != i+1)
				return 1;
		}
		
		offset = (int)Marshal.OffsetOf(typeof(Dummy), "b");
		float *data2 = (float*)((byte*)p.ToPointer() + offset);
		for(int i=0; i<16; i++)
			if(data2[i] != i+1)
				return 2;

		offset = (int)Marshal.OffsetOf(typeof(Dummy), "c");
		long *data3 = (long*)((byte*)p.ToPointer() + offset);
		for(int i=0; i<16; i++)
			if(data3[i] != i+1)
				return 3;

		///
		///	Pointer to structure
		///
		Dummy dummy2 = new Dummy ();
		Marshal.PtrToStructure(p, dummy2);

		if(dummy2.a.Length != dummy.a.Length) return 4;
		if(dummy2.b.Length != dummy.b.Length) return 5;
		if(dummy2.c.Length != dummy.c.Length) return 6;

		for(int i=0; i<16; i++)
		{
			if(dummy2.a[i] != i+1) return 7;
			if(dummy2.b[i] != i+1) return 8;
			if(dummy2.c[i] != i+1) return 9;
		}

		Marshal.FreeHGlobal(p);

		return 0;
	}
}
