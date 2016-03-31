// pinvoke_ppcf.cs - Test cases for passing structures to and and returning
//                   structures from functions.  This particular test is for
//                   structures consisting wholy of 4 byte fields.
//
//                   The Power ABI version 2 allows for special parameter
//                   passing and returning optimizations for certain
//                   structures of homogenous composition (like all ints).
//                   This set of tests checks all the possible combinations
//                   that use the special parm/return rules and one beyond.
//
// Bill Seurer (seurer@linux.vnet.ibm.com)
//
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;


public class Test_float {

	[DllImport ("libtest", EntryPoint="mono_return_float1")]
	public static extern float1 mono_return_float1 (float1 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float1 {
		public float f1;
	}
	[DllImport ("libtest", EntryPoint="mono_return_float2")]
	public static extern float2 mono_return_float2 (float2 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float2 {
		public float f1,f2;
	}
	[DllImport ("libtest", EntryPoint="mono_return_float3")]
	public static extern float3 mono_return_float3 (float3 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float3 {
		public float f1,f2,f3;
	}
	[DllImport ("libtest", EntryPoint="mono_return_float4")]
	public static extern float4 mono_return_float4 (float4 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float4 {
		public float f1,f2,f3,f4;
	}
	// This structure is 1 element too large to use the special return
	//  rules.
	[DllImport ("libtest", EntryPoint="mono_return_float5")]
	public static extern float5 mono_return_float5 (float5 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float5 {
		public float f1,f2,f3,f4,f5;
	}
	[DllImport ("libtest", EntryPoint="mono_return_float6")]
	public static extern float6 mono_return_float6 (float6 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float6 {
		public float f1,f2,f3,f4,f5,f6;
	}
	[DllImport ("libtest", EntryPoint="mono_return_float7")]
	public static extern float7 mono_return_float7 (float7 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float7 {
		public float f1,f2,f3,f4,f5,f6,f7;
	}
	[DllImport ("libtest", EntryPoint="mono_return_float8")]
	public static extern float8 mono_return_float8 (float8 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float8 {
		public float f1,f2,f3,f4,f5,f6,f7,f8;
	}
	// This structure is 1 element too large to use the special parameter
	//  passing rules.
	[DllImport ("libtest", EntryPoint="mono_return_float9")]
	public static extern float9 mono_return_float9 (float9 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float9 {
		public float f1,f2,f3,f4,f5,f6,f7,f8,f9;
	}

	// This structure has nested structures within it but they are
	//  homogenous and thus should still use the special rules.
	public struct float4_nested1 {
		public float f1;
	};
	public struct float4_nested2 {
		public float f4;
	};
	[DllImport ("libtest", EntryPoint="mono_return_float4_nested")]
	public static extern float4_nested mono_return_float4_nested (float4_nested s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float4_nested {
		public float4_nested1 nested1;
		public float f2,f3;
		public float4_nested2 nested2;
	}

	public static int Main (string[] args) {

		float1 s1;
		s1.f1 = 1;
		s1 = mono_return_float1(s1, 906);
		if (s1.f1 != 1+906) {
			Console.WriteLine("   float1 s1.f1: got {0} but expected {1}", s1.f1, 1+906);
			return 1;
		}

		float2 s2;
		s2.f1 = 1;
		s2.f2 = 2;
		s2 = mono_return_float2(s2, 906);
		if (s2.f1 != 1+906) {
			Console.WriteLine("   float2 s2.f1: got {0} but expected {1}", s2.f1, 1+906);
			return 1;
		}
		if (s2.f2 != 2+906) {
			Console.WriteLine("   float2 s2.f2: got {0} but expected {1}", s2.f2, 2+906);
			return 2;
		}

		float3 s3;
		s3.f1 = 1;
		s3.f2 = 2;
		s3.f3 = 3;
		s3 = mono_return_float3(s3, 906);
		if (s3.f1 != 1+906) {
			Console.WriteLine("   float3 s3.f1: got {0} but expected {1}", s3.f1, 1+906);
			return 1;
		}
		if (s3.f2 != 2+906) {
			Console.WriteLine("   float3 s3.f2: got {0} but expected {1}", s3.f2, 2+906);
			return 2;
		}
		if (s3.f3 != 3+906) {
			Console.WriteLine("   float3 s3.f3: got {0} but expected {1}", s3.f3, 3+906);
			return 3;
		}

		float4 s4;
		s4.f1 = 1;
		s4.f2 = 2;
		s4.f3 = 3;
		s4.f4 = 4;
		s4 = mono_return_float4(s4, 906);
		if (s4.f1 != 1+906) {
			Console.WriteLine("   float4 s4.f1: got {0} but expected {1}", s4.f1, 1+906);
			return 1;
		}
		if (s4.f2 != 2+906) {
			Console.WriteLine("   float4 s4.f2: got {0} but expected {1}", s4.f2, 2+906);
			return 2;
		}
		if (s4.f3 != 3+906) {
			Console.WriteLine("   float4 s4.f3: got {0} but expected {1}", s4.f3, 3+906);
			return 3;
		}
		if (s4.f4 != 4+906) {
			Console.WriteLine("   float4 s4.f4: got {0} but expected {1}", s4.f4, 4+906);
			return 4;
		}

		float5 s5;
		s5.f1 = 1;
		s5.f2 = 2;
		s5.f3 = 3;
		s5.f4 = 4;
		s5.f5 = 5;
		s5 = mono_return_float5(s5, 906);
		if (s5.f1 != 1+906) {
			Console.WriteLine("   float5 s5.f1: got {0} but expected {1}", s5.f1, 1+906);
			return 1;
		}
		if (s5.f2 != 2+906) {
			Console.WriteLine("   float5 s5.f2: got {0} but expected {1}", s5.f2, 2+906);
			return 2;
		}
		if (s5.f3 != 3+906) {
			Console.WriteLine("   float5 s5.f3: got {0} but expected {1}", s5.f3, 3+906);
			return 3;
		}
		if (s5.f4 != 4+906) {
			Console.WriteLine("   float5 s5.f4: got {0} but expected {1}", s5.f4, 4+906);
			return 4;
		}
		if (s5.f5 != 5+906) {
			Console.WriteLine("   float5 s5.f5: got {0} but expected {1}", s5.f5, 5+906);
			return 5;
		}

		float6 s6;
		s6.f1 = 1;
		s6.f2 = 2;
		s6.f3 = 3;
		s6.f4 = 4;
		s6.f5 = 5;
		s6.f6 = 6;
		s6 = mono_return_float6(s6, 906);
		if (s6.f1 != 1+906) {
			Console.WriteLine("   float6 s6.f1: got {0} but expected {1}", s6.f1, 1+906);
			return 1;
		}
		if (s6.f2 != 2+906) {
			Console.WriteLine("   float6 s6.f2: got {0} but expected {1}", s6.f2, 2+906);
			return 2;
		}
		if (s6.f3 != 3+906) {
			Console.WriteLine("   float6 s6.f3: got {0} but expected {1}", s6.f3, 3+906);
			return 3;
		}
		if (s6.f4 != 4+906) {
			Console.WriteLine("   float6 s6.f4: got {0} but expected {1}", s6.f4, 4+906);
			return 4;
		}
		if (s6.f5 != 5+906) {
			Console.WriteLine("   float6 s6.f5: got {0} but expected {1}", s6.f5, 5+906);
			return 5;
		}
		if (s6.f6 != 6+906) {
			Console.WriteLine("   float6 s6.f6: got {0} but expected {1}", s6.f6, 6+906);
			return 6;
		}

		float7 s7;
		s7.f1 = 1;
		s7.f2 = 2;
		s7.f3 = 3;
		s7.f4 = 4;
		s7.f5 = 5;
		s7.f6 = 6;
		s7.f7 = 7;
		s7 = mono_return_float7(s7, 906);
		if (s7.f1 != 1+906) {
			Console.WriteLine("   float7 s7.f1: got {0} but expected {1}", s7.f1, 1+906);
			return 1;
		}
		if (s7.f2 != 2+906) {
			Console.WriteLine("   float7 s7.f2: got {0} but expected {1}", s7.f2, 2+906);
			return 2;
		}
		if (s7.f3 != 3+906) {
			Console.WriteLine("   float7 s7.f3: got {0} but expected {1}", s7.f3, 3+906);
			return 3;
		}
		if (s7.f4 != 4+906) {
			Console.WriteLine("   float7 s7.f4: got {0} but expected {1}", s7.f4, 4+906);
			return 4;
		}
		if (s7.f5 != 5+906) {
			Console.WriteLine("   float7 s7.f5: got {0} but expected {1}", s7.f5, 5+906);
			return 5;
		}
		if (s7.f6 != 6+906) {
			Console.WriteLine("   float7 s7.f6: got {0} but expected {1}", s7.f6, 6+906);
			return 6;
		}
		if (s7.f7 != 7+906) {
			Console.WriteLine("   float7 s7.f7: got {0} but expected {1}", s7.f7, 7+906);
			return 7;
		}

		float8 s8;
		s8.f1 = 1;
		s8.f2 = 2;
		s8.f3 = 3;
		s8.f4 = 4;
		s8.f5 = 5;
		s8.f6 = 6;
		s8.f7 = 7;
		s8.f8 = 8;
		s8 = mono_return_float8(s8, 906);
		if (s8.f1 != 1+906) {
			Console.WriteLine("   float8 s8.f1: got {0} but expected {1}", s8.f1, 1+906);
			return 1;
		}
		if (s8.f2 != 2+906) {
			Console.WriteLine("   float8 s8.f2: got {0} but expected {1}", s8.f2, 2+906);
			return 2;
		}
		if (s8.f3 != 3+906) {
			Console.WriteLine("   float8 s8.f3: got {0} but expected {1}", s8.f3, 3+906);
			return 3;
		}
		if (s8.f4 != 4+906) {
			Console.WriteLine("   float8 s8.f4: got {0} but expected {1}", s8.f4, 4+906);
			return 4;
		}
		if (s8.f5 != 5+906) {
			Console.WriteLine("   float8 s8.f5: got {0} but expected {1}", s8.f5, 5+906);
			return 5;
		}
		if (s8.f6 != 6+906) {
			Console.WriteLine("   float8 s8.f6: got {0} but expected {1}", s8.f6, 6+906);
			return 6;
		}
		if (s8.f7 != 7+906) {
			Console.WriteLine("   float8 s8.f7: got {0} but expected {1}", s8.f7, 7+906);
			return 7;
		}
		if (s8.f8 != 8+906) {
			Console.WriteLine("   float8 s8.f8: got {0} but expected {1}", s8.f8, 8+906);
			return 8;
		}

		float9 s9;
		s9.f1 = 1;
		s9.f2 = 2;
		s9.f3 = 3;
		s9.f4 = 4;
		s9.f5 = 5;
		s9.f6 = 6;
		s9.f7 = 7;
		s9.f8 = 8;
		s9.f9 = 9;
		s9 = mono_return_float9(s9, 906);
		if (s9.f1 != 1+906) {
			Console.WriteLine("   float9 s9.f1: got {0} but expected {1}", s9.f1, 1+906);
			return 1;
		}
		if (s9.f2 != 2+906) {
			Console.WriteLine("   float9 s9.f2: got {0} but expected {1}", s9.f2, 2+906);
			return 2;
		}
		if (s9.f3 != 3+906) {
			Console.WriteLine("   float9 s9.f3: got {0} but expected {1}", s9.f3, 3+906);
			return 3;
		}
		if (s9.f4 != 4+906) {
			Console.WriteLine("   float9 s9.f4: got {0} but expected {1}", s9.f4, 4+906);
			return 4;
		}
		if (s9.f5 != 5+906) {
			Console.WriteLine("   float9 s9.f5: got {0} but expected {1}", s9.f5, 5+906);
			return 5;
		}
		if (s9.f6 != 6+906) {
			Console.WriteLine("   float9 s9.f6: got {0} but expected {1}", s9.f6, 6+906);
			return 6;
		}
		if (s9.f7 != 7+906) {
			Console.WriteLine("   float9 s9.f7: got {0} but expected {1}", s9.f7, 7+906);
			return 7;
		}
		if (s9.f8 != 8+906) {
			Console.WriteLine("   float9 s9.f8: got {0} but expected {1}", s9.f8, 8+906);
			return 8;
		}
		if (s9.f9 != 9+906) {
			Console.WriteLine("   float9 s9.f9: got {0} but expected {1}", s9.f9, 9+906);
			return 9;
		}


		float4_nested sn4;
		sn4.nested1.f1 = 1;
		sn4.f2 = 2;
		sn4.f3 = 3;
		sn4.nested2.f4 = 4;
		sn4 = mono_return_float4_nested(sn4, 906);
		if (sn4.nested1.f1 != 1+906) {
			Console.WriteLine("   float4_nested sn4.nested1.f1: got {0} but expected {1}", sn4.nested1.f1, 1+906);
			return 1;
		}
		if (sn4.f2 != 2+906) {
			Console.WriteLine("   float4_nested sn4.f2: got {0} but expected {1}", sn4.f2, 2+906);
			return 2;
		}
		if (sn4.f3 != 3+906) {
			Console.WriteLine("   float4_nested sn4.f3: got {0} but expected {1}", sn4.f3, 3+906);
			return 3;
		}
		if (sn4.nested2.f4 != 4+906) {
			Console.WriteLine("   float4_nested sn4.nested2.f4: got {0} but expected {1}", sn4.nested2.f4, 4+906);
			return 4;
		}

		return 0;
	} // end Main
} // end class Test_float

