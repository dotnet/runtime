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
// (C) {Copyright holder}
//

using System;
using System.Runtime.InteropServices;


public class Test_float {

	[DllImport ("libtest", EntryPoint="mono_return_float1")]
	public static extern float mono_return_float1 (float1 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float1 {
		public float f1;
	}
	[DllImport ("libtest", EntryPoint="mono_return_float2")]
	public static extern float mono_return_float2 (float2 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float2 {
		public float f1,f2;
	}
	[DllImport ("libtest", EntryPoint="mono_return_float3")]
	public static extern float mono_return_float3 (float3 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float3 {
		public float f1,f2,f3;
	}
	[DllImport ("libtest", EntryPoint="mono_return_float4")]
	public static extern float mono_return_float4 (float4 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float4 {
		public float f1,f2,f3,f4;
	}
	// This structure is 1 element too large to use the special return
	//  rules.
	[DllImport ("libtest", EntryPoint="mono_return_float5")]
	public static extern float mono_return_float5 (float5 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float5 {
		public float f1,f2,f3,f4,f5;
	}
	[DllImport ("libtest", EntryPoint="mono_return_float6")]
	public static extern float mono_return_float6 (float6 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float6 {
		public float f1,f2,f3,f4,f5,f6;
	}
	[DllImport ("libtest", EntryPoint="mono_return_float7")]
	public static extern float mono_return_float7 (float7 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float7 {
		public float f1,f2,f3,f4,f5,f6,f7;
	}
	[DllImport ("libtest", EntryPoint="mono_return_float8")]
	public static extern float mono_return_float8 (float8 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct float8 {
		public float f1,f2,f3,f4,f5,f6,f7,f8;
	}
	// This structure is 1 element too large to use the special parameter
	//  passing rules.
	[DllImport ("libtest", EntryPoint="mono_return_float9")]
	public static extern float mono_return_float9 (float9 s, int addend);
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
		float retval1 = mono_return_float1(s1, 906);
		if (retval1 != 2*906) {
			Console.WriteLine("   float1 retval1: got {0} but expected {1}", retval1, 2*906);
			return 1;
		}

		float2 s2;
		s2.f1 = 1;
		s2.f2 = 2;
		float retval2 = mono_return_float2(s2, 906);
		if (retval2 != 2*906) {
			Console.WriteLine("   float2 retval2: got {0} but expected {1}", retval2, 2*906);
			return 1;
		}

		float3 s3;
		s3.f1 = 1;
		s3.f2 = 2;
		s3.f3 = 3;
		float retval3 = mono_return_float3(s3, 906);
		if (retval3 != 2*906) {
			Console.WriteLine("   float3 retval3: got {0} but expected {1}", retval3, 2*906);
			return 1;
		}

		float4 s4;
		s4.f1 = 1;
		s4.f2 = 2;
		s4.f3 = 3;
		s4.f4 = 4;
		float retval4 = mono_return_float4(s4, 906);
		if (retval4 != 2*906) {
			Console.WriteLine("   float4 retval4: got {0} but expected {1}", retval4, 2*906);
			return 1;
		}

		float5 s5;
		s5.f1 = 1;
		s5.f2 = 2;
		s5.f3 = 3;
		s5.f4 = 4;
		s5.f5 = 5;
		float retval5 = mono_return_float5(s5, 906);
		if (retval5 != 2*906) {
			Console.WriteLine("   float5 retval5: got {0} but expected {1}", retval5, 2*906);
			return 1;
		}

		float6 s6;
		s6.f1 = 1;
		s6.f2 = 2;
		s6.f3 = 3;
		s6.f4 = 4;
		s6.f5 = 5;
		s6.f6 = 6;
		float retval6 = mono_return_float6(s6, 906);
		if (retval6 != 2*906) {
			Console.WriteLine("   float6 retval6: got {0} but expected {1}", retval6, 2*906);
			return 1;
		}

		float7 s7;
		s7.f1 = 1;
		s7.f2 = 2;
		s7.f3 = 3;
		s7.f4 = 4;
		s7.f5 = 5;
		s7.f6 = 6;
		s7.f7 = 7;
		float retval7 = mono_return_float7(s7, 906);
		if (retval7 != 2*906) {
			Console.WriteLine("   float7 retval7: got {0} but expected {1}", retval7, 2*906);
			return 1;
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
		float retval8 = mono_return_float8(s8, 906);
		if (retval8 != 2*906) {
			Console.WriteLine("   float8 retval8: got {0} but expected {1}", retval8, 2*906);
			return 1;
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
		float retval9 = mono_return_float9(s9, 906);
		if (retval9 != 2*906) {
			Console.WriteLine("   float9 retval9: got {0} but expected {1}", retval9, 2*906);
			return 1;
		}


		return 0;
	} // end Main
} // end class Test_float





