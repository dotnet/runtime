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

[StructLayout(LayoutKind.Sequential)]
class FormattedClass
{
	public int i;
		
	public FormattedClass(int i)
	{
		this.i = i;
	}
}

[StructLayout(LayoutKind.Sequential)]
struct Struct
{
	public int i;
		
	public Struct(int i)
	{
		this.i = i;
	}
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


		///
		/// Only allow 
		///
		FormattedClass fc = new FormattedClass(20);
		IntPtr fc_ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FormattedClass)));
		Marshal.StructureToPtr(fc, fc_ptr, false);
		Marshal.PtrToStructure(fc_ptr, fc);
		if (fc.i != 20)
			return 10;
		Marshal.FreeHGlobal(fc_ptr);
			
		bool exception = false;
		try
		{
			object str = new Struct(20);
			IntPtr str_ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Struct)));
			Marshal.StructureToPtr(str, str_ptr, false);
			Marshal.PtrToStructure(str_ptr, str);
			Marshal.FreeHGlobal(str_ptr);
		}
		catch (Exception ex)
		{
			exception = true;
		}
		if (!exception)
			return 11;

		return 0;
	}
}
