using System;
using System.Runtime.InteropServices;

class Test
{
	[DllImport("libtest")]
	extern static int marshal_test_ref_bool
	(
		int i, 
		[MarshalAs(UnmanagedType.I1)] ref bool b1, 
		[MarshalAs(UnmanagedType.VariantBool)] ref bool b2, 
		ref bool b3
	);

	struct BoolStruct
	{
		public int i;
		[MarshalAs(UnmanagedType.I1)] public bool b1;
		[MarshalAs(UnmanagedType.VariantBool)] public bool b2;
		public bool b3;
	}

	[DllImport("libtest")]
	extern static int marshal_test_bool_struct(ref BoolStruct s);

	public static int Main()
	{
		for (int i = 0; i < 8; i++)
		{
			bool b1 = (i & 4) != 0;
			bool b2 = (i & 2) != 0;
			bool b3 = (i & 1) != 0;
			bool orig_b1 = b1, orig_b2 = b2, orig_b3 = b3;
			if (marshal_test_ref_bool(i, ref b1, ref b2, ref b3) != 0)
				return 4 * i + 1;
			if (b1 != !orig_b1)
				return 4 * i + 2;
			if (b2 != !orig_b2)
				return 4 * i + 3;
			if (b3 != !orig_b3)
				return 4 * i + 4;
		}

		for (int i = 0; i < 8; i++)
		{
			BoolStruct s = new BoolStruct();
			s.i = i;
			s.b1 = (i & 4) != 0;
			s.b2 = (i & 2) != 0;
			s.b3 = (i & 1) != 0;
			BoolStruct orig = s;
			if (marshal_test_bool_struct(ref s) != 0)
				return 4 * i + 33;
			if (s.b1 != !orig.b1)
				return 4 * i + 34;
			if (s.b2 != !orig.b2)
				return 4 * i + 35;
			if (s.b3 != !orig.b3)
				return 4 * i + 36;
		}

		Console.WriteLine("Success");
		return 0;
	}
}
