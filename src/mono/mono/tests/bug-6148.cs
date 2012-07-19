using System;
using System.Runtime.InteropServices;


[StructLayout(LayoutKind.Explicit)]
struct DefaultPack
{
	[FieldOffset(0)]
		public int A;
	[FieldOffset(4)]
		public int A1;
	[FieldOffset(8)]
		public byte A2;
	
	[FieldOffset(9)]
		public int A3;
	[FieldOffset(13)]
		public int A4;
}
[StructLayout(LayoutKind.Explicit, Pack=2)]
struct ExplicitPack
{
	[FieldOffset(0)]
		public int A;
	[FieldOffset(4)]
		public int A1;
	[FieldOffset(8)]
		public byte A2;
	
	[FieldOffset(9)]
		public int A3;
	[FieldOffset(13)]
		public int A4;
}




public class Program {
	public static unsafe int Main(string[] args)
	{
		if (sizeof(DefaultPack) != 20)
			return 1;

		if (sizeof(ExplicitPack) != 18)
			return 2;
		return 0;
	}
}

