using System;
using System.Runtime.InteropServices;


[StructLayout(LayoutKind.Sequential)]
public class VectorList
{
	public int a = 1;
	public int b = 2;
}


public class Test
{
	[DllImport("libtest")]
	private static extern VectorList TestVectorList (VectorList vl);

	public static int Main()
	{
		VectorList v1 = new VectorList ();

		VectorList v2 = TestVectorList (v1);

		if (v1.a != 1 || v1.b != 2)
			return 1;
		
		if (v2.a != 2 || v2.b != 3)
			return 2;
		
		return 0;
	}
}
