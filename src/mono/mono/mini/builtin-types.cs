// #define ARCH_32
#define NINT_JIT_OPTIMIZED

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


public class BuiltinTests {
	static int test_0_nint_ctor ()
	{
		var x = new nint (10);
		var y = new nint (x);
		var z = new nint (new nint (20));
		if ((int)x != 10)
			return 1;
		if ((int)y != 10)
			return 2;
		if ((int)z != 20)
			return 3;
		return 0;
	}

	static int test_0_nint_casts ()
	{
		var x = (nint)10;
		var y = (nint)20L;

		if ((int)x != 10)
			return 1;
		if ((long)x != 10L)
			return 2;
		if ((int)y != 20)
			return 3;
		if ((long)y != 20L)
			return 4;
		return 0;
	}

	static int test_0_nint_plus ()
	{
		var x = (nint)10;
		var z = +x;
		if ((int)z != 10)
			return 1;
		return 0;
	}

	static int test_0_nint_neg ()
	{
		var x = (nint)10;
		var z = -x;
		if ((int)z != -10)
			return 1;
		return 0;
	}

	static int test_0_nint_comp ()
	{
		var x = (nint)10;
		var z = ~x;
		if ((int)z != ~10)
			return 1;
		return 0;
	}

#if FALSE
	static int test_0_nint_inc ()
	{
		var x = (nint)10;
		++x;
		if ((int)x != 11)
			return 1;
		return 0;
	}

	static int test_0_nint_dec ()
	{
		var x = (nint)10;
		--x;
		if ((int)x != 9)
			return 1;
		return 0;
	}
#endif

	static int test_0_nint_add ()
	{
		var x = (nint)10;
		var y = (nint)20;
		var z = x + y;
		if ((int)z != 30)
			return 1;
		return 0;
	}

	static int test_0_nint_sub ()
	{
		var x = (nint)10;
		var y = (nint)20;
		var z = x - y;
		if ((int)z != -10)
			return 1;
		return 0;
	}

	static int test_0_nint_mul ()
	{
		var x = (nint)10;
		var y = (nint)20;
		var z = x * y;
		if ((int)z != 200)
			return 1;
		return 0;
	}

	static int test_0_nint_div ()
	{
		var x = (nint)30;
		var y = (nint)3;
		var z = x / y;
		if ((int)z != 10)
			return 1;
		return 0;
	}

	static int test_0_nint_rem ()
	{
		var x = (nint)22;
		var y = (nint)10;
		var z = x % y;
		if ((int)z != 2)
			return 1;
		return 0;
	}

	static int test_0_nint_and ()
	{
		var x = (nint)0x30;
		var y = (nint)0x11;
		var z = x & y;
		if ((int)z != 0x10)
			return 1;
		return 0;
	}

	static int test_0_nint_or ()
	{
		var x = (nint)0x0F;
		var y = (nint)0xF0;
		var z = x | y;
		if ((int)z != 0xFF)
			return 1;
		return 0;
	}

	static int test_0_nint_xor ()
	{
		var x = (nint)0xFF;
		var y = (nint)0xF0;
		var z = x ^ y;
		if ((int)z != 0x0F)
			return 1;
		return 0;
	}

	static int test_0_nint_shl ()
	{
		var x = (nint)10;
		var z = x << 2;
		if ((int)z != 40)
			return 1;
		return 0;
	}

	static int test_0_nint_shr ()
	{
		var x = (nint)10;
		var z = x >> 2;
		if ((int)z != 2)
			return 1;
		return 0;
	}

	static int test_0_nint_cmp_same_val ()
	{
		var x = (nint)10;
		var y = (nint)10;
		if (!(x == y))
			return 1;
		if (x != y)
			return 2;
		if (x < y)
			return 3;
		if (x > y)
			return 4;
		if (!(x <= y))
			return 5;
		if (!(x >= y))
			return 6;
		return 0;
	}

	static int test_0_nint_cmp_small_val ()
	{
		var x = (nint)5;
		var y = (nint)10;
		if (x == y)
			return 1;
		if (!(x != y))
			return 2;
		if (!(x < y))
			return 3;
		if (x > y)
			return 4;
		if (!(x <= y))
			return 5;
		if (x >= y)
			return 6;
		return 0;
	}

	static int test_0_nint_cmp_large_val ()
	{
		var x = (nint)20;
		var y = (nint)10;
		if (x == y)
			return 1;
		if (!(x != y))
			return 2;
		if (x < y)
			return 3;
		if (!(x > y))
			return 4;
		if (x <= y)
			return 1;
		if (!(x >= y))
			return 1;
		return 0;
	}

	// static int test_0_nint_call_boxed_equals ()
	// {
	// 	object x = new nint (10);
	// 	object y = new nint (10);
	// 	if (!x.Equals (y))
	// 		return 1;
	// 	return 0;
	// }

	static int test_0_nint_call_boxed_funs ()
	{
		object x = new nint (10);
		object y = new nint (10);
		if (x.GetHashCode () == 0)
			return 2;
		if (x.ToString () != "10")
			return 3;
		return 0;
	}

	public int test_0_nint_unboxed_member_calls ()
	{
		var x = (nint)10;
#if FALSE
		if (!x.Equals (x))
			return 1;
#endif
		if (x != nint.Parse ("10"))
			return 2;
		return 0;
	}

	static int test_0_nuint_ctor ()
	{
		var x = new nuint (10u);
		var y = new nuint (x);
		var z = new nuint (new nuint (20u));
		if ((uint)x != 10)
			return 1;
		if ((uint)y != 10)
			return 2;
		if ((uint)z != 20)
			return 3;
		return 0;
	}

	static int test_0_nuint_casts ()
	{
		var x = (nuint)10;
		var y = (nuint)20L;

		if ((uint)x != 10)
			return 1;
		if ((ulong)x != 10L)
			return 2;
		if ((uint)y != 20)
			return 3;
		if ((ulong)y != 20L)
			return 4;
		return 0;
	}

	static int test_0_nuint_plus ()
	{
		var x = (nuint)10;
		var z = +x;
		if ((uint)z != 10)
			return 1;
		return 0;
	}

	// static int test_0_nuint_neg ()
	// {
	// 	var x = (nuint)10;
	// 	var z = -x;
	// 	if ((uint)z != -10)
	// 		return 1;
	// 	return 0;
	// }

	static int test_0_nuint_comp ()
	{
		var x = (nuint)10;
		var z = ~x;
		if ((uint)z != ~10u)
			return 1;
		return 0;
	}

#if FALSE
	static int test_0_nuint_inc ()
	{
		var x = (nuint)10;
		++x;
		if ((uint)x != 11)
			return 1;
		return 0;
	}

	static int test_0_nuint_dec ()
	{
		var x = (nuint)10;
		--x;
		if ((uint)x != 9)
			return 1;
		return 0;
	}
#endif

	static int test_0_nuint_add ()
	{
		var x = (nuint)10;
		var y = (nuint)20;
		var z = x + y;
		if ((uint)z != 30)
			return 1;
		return 0;
	}

	static int test_0_nuint_sub ()
	{
		var x = (nuint)20;
		var y = (nuint)5;
		var z = x - y;
		if ((uint)z != 15)
			return 1;
		return 0;
	}

	static int test_0_nuint_mul ()
	{
		var x = (nuint)10;
		var y = (nuint)20;
		var z = x * y;
		if ((uint)z != 200)
			return 1;
		return 0;
	}

	static int test_0_nuint_div ()
	{
		var x = (nuint)30;
		var y = (nuint)3;
		var z = x / y;
		if ((uint)z != 10)
			return 1;
		return 0;
	}

	static int test_0_nuint_rem ()
	{
		var x = (nuint)22;
		var y = (nuint)10;
		var z = x % y;
		if ((uint)z != 2)
			return 1;
		return 0;
	}

	static int test_0_nuint_and ()
	{
		var x = (nuint)0x30;
		var y = (nuint)0x11;
		var z = x & y;
		if ((uint)z != 0x10)
			return 1;
		return 0;
	}

	static int test_0_nuint_or ()
	{
		var x = (nuint)0x0F;
		var y = (nuint)0xF0;
		var z = x | y;
		if ((uint)z != 0xFF)
			return 1;
		return 0;
	}

	static int test_0_nuint_xor ()
	{
		var x = (nuint)0xFF;
		var y = (nuint)0xF0;
		var z = x ^ y;
		if ((uint)z != 0x0F)
			return 1;
		return 0;
	}

	static int test_0_nuint_shl ()
	{
		var x = (nuint)10;
		var z = x << 2;
		if ((uint)z != 40)
			return 1;
		return 0;
	}

	static int test_0_nuint_shr ()
	{
		var x = (nuint)10;
		var z = x >> 2;
		if ((uint)z != 2)
			return 1;
		return 0;
	}

	static int test_0_nuint_cmp_same_val ()
	{
		var x = (nuint)10;
		var y = (nuint)10;
		if (!(x == y))
			return 1;
		if (x != y)
			return 2;
		if (x < y)
			return 3;
		if (x > y)
			return 4;
		if (!(x <= y))
			return 5;
		if (!(x >= y))
			return 6;
		return 0;
	}

	static int test_0_nuint_cmp_small_val ()
	{
		var x = (nuint)5;
		var y = (nuint)10;
		if (x == y)
			return 1;
		if (!(x != y))
			return 2;
		if (!(x < y))
			return 3;
		if (x > y)
			return 4;
		if (!(x <= y))
			return 5;
		if (x >= y)
			return 6;
		return 0;
	}

	static int test_0_nuint_cmp_large_val ()
	{
		var x = (nuint)20;
		var y = (nuint)10;
		if (x == y)
			return 1;
		if (!(x != y))
			return 2;
		if (x < y)
			return 3;
		if (!(x > y))
			return 4;
		if (x <= y)
			return 1;
		if (!(x >= y))
			return 1;
		return 0;
	}

	// static int test_0_nuint_call_boxed_equals ()
	// {
	// 	object x = new nuint (10);
	// 	object y = new nuint (10);
	// 	if (!x.Equals (y))
	// 		return 1;
	// 	return 0;
	// }

	static int test_0_nuint_call_boxed_funs ()
	{
		object x = new nuint (10u);
		object y = new nuint (10u);
		if (x.GetHashCode () == 0)
			return 2;
		if (x.ToString () != "10")
			return 3;
		return 0;
	}

	public int test_0_nuint_unboxed_member_calls ()
	{
		var x = (nuint)10;
#if FALSE
		if (!x.Equals (x))
			return 1;
#endif
		if (x != nuint.Parse ("10"))
			return 2;
		return 0;
	}

	static int test_0_nfloat_ctor ()
	{
		var x = new nfloat (10.0f);
		var y = new nfloat (x);
		var z = new nfloat (new nfloat (20f));
		if ((float)x != 10f)
			return 1;
		if ((float)y != 10f)
			return 2;
		if ((float)z != 20f)
			return 3;
		return 0;
	}

	static int test_0_nfloat_casts ()
	{
		var x = (nfloat)10f;

		if ((float)x != 10f)
			return 1;
		if ((double)x != 10)
			return 2;
#if FALSE
		var y = (nfloat)20;
		if ((float)y != 20f)
			return 3;
		if ((double)y != 20)
			return 4;
#endif
		return 0;
	}

	static int test_0_nfloat_plus ()
	{
		var x = (nfloat)10f;
		var z = +x;
		if ((float)z != 10f)
			return 1;
		return 0;
	}

	static int test_0_nfloat_neg ()
	{
		var x = (nfloat)10f;
		var z = -x;
		if ((float)z != -10f)
			return 1;
		return 0;
	}

#if FALSE
	static int test_0_nfloat_inc ()
	{
		var x = (nfloat)10f;
		++x;
		if ((float)x != 11f) {
			Console.WriteLine ((float)x);
			return 1;
		}
		return 0;
	}

	static int test_0_nfloat_dec ()
	{
		var x = (nfloat)10f;
		--x;
		if ((float)x != 9f) {
			Console.WriteLine ((float)x);
			return 1;
		}
		return 0;
	}
#endif

	static int test_0_nfloat_add ()
	{
		var x = (nfloat)10f;
		var y = (nfloat)20f;
		var z = x + y;
		if ((float)z != 30f)
			return 1;
		return 0;
	}

	static int test_0_nfloat_sub ()
	{
		var x = (nfloat)10f;
		var y = (nfloat)20f;
		var z = x - y;
		if ((float)z != -10f)
			return 1;
		return 0;
	}

	static int test_0_nfloat_mul ()
	{
		var x = (nfloat)10f;
		var y = (nfloat)20f;
		var z = x * y;
		if ((float)z != 200f)
			return 1;
		return 0;
	}

	static int test_0_nfloat_div ()
	{
		var x = (nfloat)30f;
		var y = (nfloat)3f;
		var z = x / y;
		if ((float)z != 10f)
			return 1;
		return 0;
	}

	static int test_0_nfloat_rem ()
	{
		var x = (nfloat)22f;
		var y = (nfloat)10f;
		var z = x % y;
		if ((float)z != 2f)
			return 1;
		return 0;
	}

	static int test_0_nfloat_cmp_same_val ()
	{
		var x = (nfloat)10f;
		var y = (nfloat)10f;
		if (!(x == y))
			return 1;
		if (x != y)
			return 2;
		if (x < y)
			return 3;
		if (x > y)
			return 4;
		if (!(x <= y))
			return 5;
		if (!(x >= y))
			return 6;
		return 0;
	}

	static int test_0_nfloat_cmp_small_val ()
	{
		var x = (nfloat)5f;
		var y = (nfloat)10f;
		if (x == y)
			return 1;
		if (!(x != y))
			return 2;
		if (!(x < y))
			return 3;
		if (x > y)
			return 4;
		if (!(x <= y))
			return 5;
		if (x >= y)
			return 6;
		return 0;
	}

	static int test_0_nfloat_cmp_large_val ()
	{
		var x = (nfloat)20f;
		var y = (nfloat)10f;
		if (x == y)
			return 1;
		if (!(x != y))
			return 2;
		if (x < y)
			return 3;
		if (!(x > y))
			return 4;
		if (x <= y)
			return 1;
		if (!(x >= y))
			return 1;
		return 0;
	}

	/* fails on arm64 */
#if FALSE
	static int test_0_nfloat_cmp_left_nan ()
	{
		var x = (nfloat)float.NaN;
		var y = (nfloat)10f;
		if (x == y)
			return 1;
		if (!(x != y))
			return 2;
		if (x < y)
			return 3;
		if (x > y)
			return 4;
		if (x <= y)
			return 1;
		if (x >= y)
			return 1;
		return 0;
	}


	static int test_0_nfloat_cmp_right_nan ()
	{
		var x = (nfloat)10f;
		var y = (nfloat)float.NaN;
		if (x == y)
			return 1;
		if (!(x != y))
			return 2;
		if (x < y)
			return 3;
		if (x > y)
			return 4;
		if (x <= y)
			return 1;
		if (x >= y)
			return 1;
		return 0;
	}
#endif

	// static int test_0_nfloat_call_boxed_equals ()
	// {
	// 	object x = new nfloat (10f);
	// 	object y = new nfloat (10f);
	// 	if (!x.Equals (y))
	// 		return 1;
	// 	return 0;
	// }

	static int test_0_nfloat_call_boxed_funs ()
	{
		object x = new nfloat (10f);
		object y = new nfloat (10f);
		if (x.GetHashCode () == 0)
			return 2;
		if (x.ToString () != "10")
			return 3;
		return 0;
	}

	public int test_0_nfloat_unboxed_member_calls ()
	{
		var x = (nfloat)10f;
#if FALSE
		if (!x.Equals (x))
			return 1;
#endif
		if (x != nfloat.Parse ("10"))
			return 2;
		return 0;
	}

#if !__MOBILE__
	public static int Main (String[] args) {
		return TestDriver.RunTests (typeof (BuiltinTests), args);
	}
#endif
}


// !!! WARNING - GENERATED CODE - DO NOT EDIT !!!
//
// Generated by NativeTypes.tt, a T4 template.
//
// NativeTypes.cs: basic types with 32 or 64 bit sizes:
//
//   - nint
//   - nuint
//   - nfloat
//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2013 Xamarin, Inc. All rights reserved.
//

namespace System
{
	[Serializable]
	[DebuggerDisplay ("{v,nq}")]
	public unsafe struct nint : IFormattable, IConvertible, IComparable, IComparable<nint>, IEquatable <nint>
	{
		internal nint (nint v) { this.v = v.v; }
		public nint (Int32 v) { this.v = v; }

#if ARCH_32
		public static readonly int Size = 4;

		public static readonly nint MaxValue = Int32.MaxValue;
		public static readonly nint MinValue = Int32.MinValue;

		[DebuggerBrowsable (DebuggerBrowsableState.Never)]
		internal Int32 v;

		public nint (Int64 v) { this.v = (Int32)v; }
#else
		public static readonly int Size = 8;

		public static readonly nint MaxValue = (nint) Int64.MaxValue; // 64-bit only codepath
		public static readonly nint MinValue = (nint) Int64.MinValue; // 64-bit only codepath

		[DebuggerBrowsable (DebuggerBrowsableState.Never)]
		internal Int64 v;

		public nint (Int64 v) { this.v = v; }
#endif

		public static explicit operator nint (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint ((int)v.v);
#else
			return new nint ((long)v.v);
#endif
		}

		public static explicit operator nuint (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint ((uint)v.v);
#else
			return new nuint ((ulong)v.v);
#endif
		}

		public static explicit operator nint (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint ((int)v.v);
#else
			return new nint ((long)v.v);
#endif
		}

		public static implicit operator nfloat (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat ((float)v.v);
#else
			return new nfloat ((double)v.v);
#endif
		}

		public static explicit operator nint (IntPtr v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint (*((int *)&v));
#else
			return new nint (*((long *)&v));
#endif
		}

		public static explicit operator IntPtr (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return *((IntPtr *)&v.v);
#else
			return *((IntPtr *)&v.v);
#endif
		}

		public static implicit operator nint (sbyte v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint ((int)v);
#else
			return new nint ((long)v);
#endif
		}

		public static explicit operator sbyte (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (sbyte)v.v;
#else
			return (sbyte)v.v;
#endif
		}

		public static implicit operator nint (byte v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint ((int)v);
#else
			return new nint ((long)v);
#endif
		}

		public static explicit operator byte (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (byte)v.v;
#else
			return (byte)v.v;
#endif
		}

		public static implicit operator nint (char v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint ((int)v);
#else
			return new nint ((long)v);
#endif
		}

		public static explicit operator char (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (char)v.v;
#else
			return (char)v.v;
#endif
		}

		public static implicit operator nint (short v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint ((int)v);
#else
			return new nint ((long)v);
#endif
		}

		public static explicit operator short (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (short)v.v;
#else
			return (short)v.v;
#endif
		}

		public static explicit operator nint (ushort v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint ((int)v);
#else
			return new nint ((long)v);
#endif
		}

		public static explicit operator ushort (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (ushort)v.v;
#else
			return (ushort)v.v;
#endif
		}

		public static implicit operator nint (int v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint ((int)v);
#else
			return new nint ((long)v);
#endif
		}

		public static explicit operator int (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (int)v.v;
#else
			return (int)v.v;
#endif
		}

		public static explicit operator nint (uint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint ((int)v);
#else
			return new nint ((long)v);
#endif
		}

		public static explicit operator uint (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (uint)v.v;
#else
			return (uint)v.v;
#endif
		}

		public static explicit operator nint (long v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint ((int)v);
#else
			return new nint ((long)v);
#endif
		}

		public static implicit operator long (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (long)v.v;
#else
			return (long)v.v;
#endif
		}

		public static explicit operator nint (ulong v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint ((int)v);
#else
			return new nint ((long)v);
#endif
		}

		public static explicit operator ulong (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (ulong)v.v;
#else
			return (ulong)v.v;
#endif
		}

		public static explicit operator nint (float v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint ((int)v);
#else
			return new nint ((long)v);
#endif
		}

		public static implicit operator float (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (float)v.v;
#else
			return (float)v.v;
#endif
		}

		public static explicit operator nint (double v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint ((int)v);
#else
			return new nint ((long)v);
#endif
		}

		public static implicit operator double (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (double)v.v;
#else
			return (double)v.v;
#endif
		}

		public static explicit operator nint (decimal v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nint ((int)v);
#else
			return new nint ((long)v);
#endif
		}

		public static implicit operator decimal (nint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (decimal)v.v;
#else
			return (decimal)v.v;
#endif
		}

#if NINT_JIT_OPTIMIZED
		public static nint operator + (nint v) { throw new NotImplementedException (); }
		public static nint operator - (nint v) { throw new NotImplementedException (); }
		public static nint operator ~ (nint v) { throw new NotImplementedException (); }
#else
		public static nint operator + (nint v) { return new nint (+v.v); }
		public static nint operator - (nint v) { return new nint (-v.v); }
		public static nint operator ~ (nint v) { return new nint (~v.v); }
#endif

#if NINT_JIT_OPTIMIZED
		public static nint operator ++ (nint v) { throw new NotImplementedException (); }
		public static nint operator -- (nint v) { throw new NotImplementedException (); }
#else
		public static nint operator ++ (nint v) { return new nint (v.v + 1); }
		public static nint operator -- (nint v) { return new nint (v.v - 1); }
#endif

#if NINT_JIT_OPTIMIZED
		public static nint operator + (nint l, nint r) { throw new NotImplementedException (); }
		public static nint operator - (nint l, nint r) { throw new NotImplementedException (); }
		public static nint operator * (nint l, nint r) { throw new NotImplementedException (); }
		public static nint operator / (nint l, nint r) { throw new NotImplementedException (); }
		public static nint operator % (nint l, nint r) { throw new NotImplementedException (); }
		public static nint operator & (nint l, nint r) { throw new NotImplementedException (); }
		public static nint operator | (nint l, nint r) { throw new NotImplementedException (); }
		public static nint operator ^ (nint l, nint r) { throw new NotImplementedException (); }

		public static nint operator << (nint l, int r) { throw new NotImplementedException (); }
		public static nint operator >> (nint l, int r) { throw new NotImplementedException (); }
#else
		public static nint operator + (nint l, nint r) { return new nint (l.v + r.v); }
		public static nint operator - (nint l, nint r) { return new nint (l.v - r.v); }
		public static nint operator * (nint l, nint r) { return new nint (l.v * r.v); }
		public static nint operator / (nint l, nint r) { return new nint (l.v / r.v); }
		public static nint operator % (nint l, nint r) { return new nint (l.v % r.v); }
		public static nint operator & (nint l, nint r) { return new nint (l.v & r.v); }
		public static nint operator | (nint l, nint r) { return new nint (l.v | r.v); }
		public static nint operator ^ (nint l, nint r) { return new nint (l.v ^ r.v); }

		public static nint operator << (nint l, int r) { return new nint (l.v << r); }
		public static nint operator >> (nint l, int r) { return new nint (l.v >> r); }
#endif

#if NINT_JIT_OPTIMIZED
		public static bool operator == (nint l, nint r) { throw new NotImplementedException (); }
		public static bool operator != (nint l, nint r) { throw new NotImplementedException (); }
		public static bool operator <  (nint l, nint r) { throw new NotImplementedException (); }
		public static bool operator >  (nint l, nint r) { throw new NotImplementedException (); }
		public static bool operator <= (nint l, nint r) { throw new NotImplementedException (); }
		public static bool operator >= (nint l, nint r) { throw new NotImplementedException (); }
#else
		public static bool operator == (nint l, nint r) { return l.v == r.v; }
		public static bool operator != (nint l, nint r) { return l.v != r.v; }
		public static bool operator <  (nint l, nint r) { return l.v < r.v; }
		public static bool operator >  (nint l, nint r) { return l.v > r.v; }
		public static bool operator <= (nint l, nint r) { return l.v <= r.v; }
		public static bool operator >= (nint l, nint r) { return l.v >= r.v; }
#endif

		public int CompareTo (nint value) { return v.CompareTo (value.v); }
		public int CompareTo (object value)
		{
			if (value is nint)
				return v.CompareTo (((nint) value).v);
			return v.CompareTo (value);
		}
		public bool Equals (nint obj) { return v.Equals (obj.v); }
		public override bool Equals (object obj)
		{
			if (obj is nint)
				return v.Equals (((nint) obj).v);
			return v.Equals (obj);
		}
		public override int GetHashCode () { return v.GetHashCode (); }

#if ARCH_32
		public static nint Parse (string s, IFormatProvider provider) { return (nint)Int32.Parse (s, provider); }
		public static nint Parse (string s, NumberStyles style) { return (nint)Int32.Parse (s, style); }
		public static nint Parse (string s) { return (nint)Int32.Parse (s); }
		public static nint Parse (string s, NumberStyles style, IFormatProvider provider) {
			return (nint)Int32.Parse (s, style, provider);
		}

		public static bool TryParse (string s, out nint result)
		{
			Int32 v;
			var r = Int32.TryParse (s, out v);
			result = (nint)v;
			return r;
		}

		public static bool TryParse (string s, NumberStyles style, IFormatProvider provider, out nint result)
		{
			Int32 v;
			var r = Int32.TryParse (s, style, provider, out v);
			result = (nint)v;
			return r;
		}
#else
		public static nint Parse (string s, IFormatProvider provider) { return (nint)Int64.Parse (s, provider); }
		public static nint Parse (string s, NumberStyles style) { return (nint)Int64.Parse (s, style); }
		public static nint Parse (string s) { return (nint)Int64.Parse (s); }
		public static nint Parse (string s, NumberStyles style, IFormatProvider provider) {
			return (nint)Int64.Parse (s, style, provider);
		}

		public static bool TryParse (string s, out nint result)
		{
			Int64 v;
			var r = Int64.TryParse (s, out v);
			result = (nint)v;
			return r;
		}

		public static bool TryParse (string s, NumberStyles style, IFormatProvider provider, out nint result)
		{
			Int64 v;
			var r = Int64.TryParse (s, style, provider, out v);
			result = (nint)v;
			return r;
		}
#endif

		public override string ToString () { return v.ToString (); }
		public string ToString (IFormatProvider provider) { return v.ToString (provider); }
		public string ToString (string format) { return v.ToString (format); }
		public string ToString (string format, IFormatProvider provider) { return v.ToString (format, provider); }

		public TypeCode GetTypeCode () { return v.GetTypeCode (); }

		bool     IConvertible.ToBoolean  (IFormatProvider provider) { return ((IConvertible)v).ToBoolean (provider); }
		byte     IConvertible.ToByte     (IFormatProvider provider) { return ((IConvertible)v).ToByte (provider); }
		char     IConvertible.ToChar     (IFormatProvider provider) { return ((IConvertible)v).ToChar (provider); }
		DateTime IConvertible.ToDateTime (IFormatProvider provider) { return ((IConvertible)v).ToDateTime (provider); }
		decimal  IConvertible.ToDecimal  (IFormatProvider provider) { return ((IConvertible)v).ToDecimal (provider); }
		double   IConvertible.ToDouble   (IFormatProvider provider) { return ((IConvertible)v).ToDouble (provider); }
		short    IConvertible.ToInt16    (IFormatProvider provider) { return ((IConvertible)v).ToInt16 (provider); }
		int      IConvertible.ToInt32    (IFormatProvider provider) { return ((IConvertible)v).ToInt32 (provider); }
		long     IConvertible.ToInt64    (IFormatProvider provider) { return ((IConvertible)v).ToInt64 (provider); }
		sbyte    IConvertible.ToSByte    (IFormatProvider provider) { return ((IConvertible)v).ToSByte (provider); }
		float    IConvertible.ToSingle   (IFormatProvider provider) { return ((IConvertible)v).ToSingle (provider); }
		ushort   IConvertible.ToUInt16   (IFormatProvider provider) { return ((IConvertible)v).ToUInt16 (provider); }
		uint     IConvertible.ToUInt32   (IFormatProvider provider) { return ((IConvertible)v).ToUInt32 (provider); }
		ulong    IConvertible.ToUInt64   (IFormatProvider provider) { return ((IConvertible)v).ToUInt64 (provider); }

		object IConvertible.ToType (Type targetType, IFormatProvider provider) {
			return ((IConvertible)v).ToType (targetType, provider);
		}

		public static void CopyArray (IntPtr source, nint [] destination, int startIndex, int length)
		{
			if (source == IntPtr.Zero)
				throw new ArgumentNullException ("source");
			if (destination == null)
				throw new ArgumentNullException ("destination");
			if (destination.Rank != 1)
				throw new ArgumentException ("destination", "array is multi-dimensional");
			if (startIndex < 0)
				throw new ArgumentException ("startIndex", "must be >= 0");
			if (length < 0)
				throw new ArgumentException ("length", "must be >= 0");
			if (startIndex + length > destination.Length)
				throw new ArgumentException ("length", "startIndex + length > destination.Length");

			for (int i = 0; i < length; i++)
				destination [i + startIndex] = (nint)Marshal.ReadIntPtr (source, i * nint.Size);
		}

		public static void CopyArray (nint [] source, int startIndex, IntPtr destination, int length)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			if (destination == IntPtr.Zero)
				throw new ArgumentNullException ("destination");
			if (source.Rank != 1)
				throw new ArgumentException ("source", "array is multi-dimensional");
			if (startIndex < 0)
				throw new ArgumentException ("startIndex", "must be >= 0");
			if (length < 0)
				throw new ArgumentException ("length", "must be >= 0");
			if (startIndex + length > source.Length)
				throw new ArgumentException ("length", "startIndex + length > source.Length");

			for (int i = 0; i < length; i++)
				Marshal.WriteIntPtr (destination, i * nint.Size, (IntPtr)source [i + startIndex]);
		}
	}
	[Serializable]
	[DebuggerDisplay ("{v,nq}")]
	public unsafe struct nuint : IFormattable, IConvertible, IComparable, IComparable<nuint>, IEquatable <nuint>
	{
		internal nuint (nuint v) { this.v = v.v; }
		public nuint (UInt32 v) { this.v = v; }

#if ARCH_32
		public static readonly int Size = 4;

		public static readonly nuint MaxValue = UInt32.MaxValue;
		public static readonly nuint MinValue = UInt32.MinValue;

		[DebuggerBrowsable (DebuggerBrowsableState.Never)]
		internal UInt32 v;

		public nuint (UInt64 v) { this.v = (UInt32)v; }
#else
		public static readonly int Size = 8;

		public static readonly nuint MaxValue = (nuint) UInt64.MaxValue; // 64-bit only codepath
		public static readonly nuint MinValue = (nuint) UInt64.MinValue; // 64-bit only codepath

		[DebuggerBrowsable (DebuggerBrowsableState.Never)]
		internal UInt64 v;

		public nuint (UInt64 v) { this.v = v; }
#endif

		public static explicit operator nuint (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint ((uint)v.v);
#else
			return new nuint ((ulong)v.v);
#endif
		}

		public static implicit operator nfloat (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat ((float)v.v);
#else
			return new nfloat ((double)v.v);
#endif
		}

		public static explicit operator nuint (IntPtr v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint (*((uint *)&v));
#else
			return new nuint (*((ulong *)&v));
#endif
		}

		public static explicit operator IntPtr (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return *((IntPtr *)&v.v);
#else
			return *((IntPtr *)&v.v);
#endif
		}

		public static explicit operator nuint (sbyte v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint ((uint)v);
#else
			return new nuint ((ulong)v);
#endif
		}

		public static explicit operator sbyte (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (sbyte)v.v;
#else
			return (sbyte)v.v;
#endif
		}

		public static implicit operator nuint (byte v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint ((uint)v);
#else
			return new nuint ((ulong)v);
#endif
		}

		public static explicit operator byte (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (byte)v.v;
#else
			return (byte)v.v;
#endif
		}

		public static implicit operator nuint (char v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint ((uint)v);
#else
			return new nuint ((ulong)v);
#endif
		}

		public static explicit operator char (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (char)v.v;
#else
			return (char)v.v;
#endif
		}

		public static explicit operator nuint (short v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint ((uint)v);
#else
			return new nuint ((ulong)v);
#endif
		}

		public static explicit operator short (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (short)v.v;
#else
			return (short)v.v;
#endif
		}

		public static implicit operator nuint (ushort v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint ((uint)v);
#else
			return new nuint ((ulong)v);
#endif
		}

		public static explicit operator ushort (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (ushort)v.v;
#else
			return (ushort)v.v;
#endif
		}

		public static explicit operator nuint (int v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint ((uint)v);
#else
			return new nuint ((ulong)v);
#endif
		}

		public static explicit operator int (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (int)v.v;
#else
			return (int)v.v;
#endif
		}

		public static implicit operator nuint (uint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint ((uint)v);
#else
			return new nuint ((ulong)v);
#endif
		}

		public static explicit operator uint (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (uint)v.v;
#else
			return (uint)v.v;
#endif
		}

		public static explicit operator nuint (long v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint ((uint)v);
#else
			return new nuint ((ulong)v);
#endif
		}

		public static explicit operator long (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (long)v.v;
#else
			return (long)v.v;
#endif
		}

		public static explicit operator nuint (ulong v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint ((uint)v);
#else
			return new nuint ((ulong)v);
#endif
		}

		public static implicit operator ulong (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (ulong)v.v;
#else
			return (ulong)v.v;
#endif
		}

		public static explicit operator nuint (float v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint ((uint)v);
#else
			return new nuint ((ulong)v);
#endif
		}

		public static implicit operator float (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (float)v.v;
#else
			return (float)v.v;
#endif
		}

		public static explicit operator nuint (double v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint ((uint)v);
#else
			return new nuint ((ulong)v);
#endif
		}

		public static implicit operator double (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (double)v.v;
#else
			return (double)v.v;
#endif
		}

		public static explicit operator nuint (decimal v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nuint ((uint)v);
#else
			return new nuint ((ulong)v);
#endif
		}

		public static implicit operator decimal (nuint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (decimal)v.v;
#else
			return (decimal)v.v;
#endif
		}

#if NINT_JIT_OPTIMIZED
		public static nuint operator + (nuint v) { throw new NotImplementedException (); }
		public static nuint operator ~ (nuint v) { throw new NotImplementedException (); }
#else
		public static nuint operator + (nuint v) { return new nuint (+v.v); }
		public static nuint operator ~ (nuint v) { return new nuint (~v.v); }
#endif

#if NINT_JIT_OPTIMIZED
		public static nuint operator ++ (nuint v) { throw new NotImplementedException (); }
		public static nuint operator -- (nuint v) { throw new NotImplementedException (); }
#else
		public static nuint operator ++ (nuint v) { return new nuint (v.v + 1); }
		public static nuint operator -- (nuint v) { return new nuint (v.v - 1); }
#endif

#if NINT_JIT_OPTIMIZED
		public static nuint operator + (nuint l, nuint r) { throw new NotImplementedException (); }
		public static nuint operator - (nuint l, nuint r) { throw new NotImplementedException (); }
		public static nuint operator * (nuint l, nuint r) { throw new NotImplementedException (); }
		public static nuint operator / (nuint l, nuint r) { throw new NotImplementedException (); }
		public static nuint operator % (nuint l, nuint r) { throw new NotImplementedException (); }
		public static nuint operator & (nuint l, nuint r) { throw new NotImplementedException (); }
		public static nuint operator | (nuint l, nuint r) { throw new NotImplementedException (); }
		public static nuint operator ^ (nuint l, nuint r) { throw new NotImplementedException (); }

		public static nuint operator << (nuint l, int r) { throw new NotImplementedException (); }
		public static nuint operator >> (nuint l, int r) { throw new NotImplementedException (); }
#else
		public static nuint operator + (nuint l, nuint r) { return new nuint (l.v + r.v); }
		public static nuint operator - (nuint l, nuint r) { return new nuint (l.v - r.v); }
		public static nuint operator * (nuint l, nuint r) { return new nuint (l.v * r.v); }
		public static nuint operator / (nuint l, nuint r) { return new nuint (l.v / r.v); }
		public static nuint operator % (nuint l, nuint r) { return new nuint (l.v % r.v); }
		public static nuint operator & (nuint l, nuint r) { return new nuint (l.v & r.v); }
		public static nuint operator | (nuint l, nuint r) { return new nuint (l.v | r.v); }
		public static nuint operator ^ (nuint l, nuint r) { return new nuint (l.v ^ r.v); }

		public static nuint operator << (nuint l, int r) { return new nuint (l.v << r); }
		public static nuint operator >> (nuint l, int r) { return new nuint (l.v >> r); }
#endif

#if NINT_JIT_OPTIMIZED
		public static bool operator == (nuint l, nuint r) { throw new NotImplementedException (); }
		public static bool operator != (nuint l, nuint r) { throw new NotImplementedException (); }
		public static bool operator <  (nuint l, nuint r) { throw new NotImplementedException (); }
		public static bool operator >  (nuint l, nuint r) { throw new NotImplementedException (); }
		public static bool operator <= (nuint l, nuint r) { throw new NotImplementedException (); }
		public static bool operator >= (nuint l, nuint r) { throw new NotImplementedException (); }
#else
		public static bool operator == (nuint l, nuint r) { return l.v == r.v; }
		public static bool operator != (nuint l, nuint r) { return l.v != r.v; }
		public static bool operator <  (nuint l, nuint r) { return l.v < r.v; }
		public static bool operator >  (nuint l, nuint r) { return l.v > r.v; }
		public static bool operator <= (nuint l, nuint r) { return l.v <= r.v; }
		public static bool operator >= (nuint l, nuint r) { return l.v >= r.v; }
#endif

		public int CompareTo (nuint value) { return v.CompareTo (value.v); }
		public int CompareTo (object value)
		{
			if (value is nuint)
				return v.CompareTo (((nuint) value).v);
			return v.CompareTo (value);
		}
		public bool Equals (nuint obj) { return v.Equals (obj.v); }
		public override bool Equals (object obj)
		{
			if (obj is nuint)
				return v.Equals (((nuint) obj).v);
			return v.Equals (obj);
		}
		public override int GetHashCode () { return v.GetHashCode (); }

#if ARCH_32
		public static nuint Parse (string s, IFormatProvider provider) { return (nuint)UInt32.Parse (s, provider); }
		public static nuint Parse (string s, NumberStyles style) { return (nuint)UInt32.Parse (s, style); }
		public static nuint Parse (string s) { return (nuint)UInt32.Parse (s); }
		public static nuint Parse (string s, NumberStyles style, IFormatProvider provider) {
			return (nuint)UInt32.Parse (s, style, provider);
		}

		public static bool TryParse (string s, out nuint result)
		{
			UInt32 v;
			var r = UInt32.TryParse (s, out v);
			result = (nuint)v;
			return r;
		}

		public static bool TryParse (string s, NumberStyles style, IFormatProvider provider, out nuint result)
		{
			UInt32 v;
			var r = UInt32.TryParse (s, style, provider, out v);
			result = (nuint)v;
			return r;
		}
#else
		public static nuint Parse (string s, IFormatProvider provider) { return (nuint)UInt64.Parse (s, provider); }
		public static nuint Parse (string s, NumberStyles style) { return (nuint)UInt64.Parse (s, style); }
		public static nuint Parse (string s) { return (nuint)UInt64.Parse (s); }
		public static nuint Parse (string s, NumberStyles style, IFormatProvider provider) {
			return (nuint)UInt64.Parse (s, style, provider);
		}

		public static bool TryParse (string s, out nuint result)
		{
			UInt64 v;
			var r = UInt64.TryParse (s, out v);
			result = (nuint)v;
			return r;
		}

		public static bool TryParse (string s, NumberStyles style, IFormatProvider provider, out nuint result)
		{
			UInt64 v;
			var r = UInt64.TryParse (s, style, provider, out v);
			result = (nuint)v;
			return r;
		}
#endif

		public override string ToString () { return v.ToString (); }
		public string ToString (IFormatProvider provider) { return v.ToString (provider); }
		public string ToString (string format) { return v.ToString (format); }
		public string ToString (string format, IFormatProvider provider) { return v.ToString (format, provider); }

		public TypeCode GetTypeCode () { return v.GetTypeCode (); }

		bool     IConvertible.ToBoolean  (IFormatProvider provider) { return ((IConvertible)v).ToBoolean (provider); }
		byte     IConvertible.ToByte     (IFormatProvider provider) { return ((IConvertible)v).ToByte (provider); }
		char     IConvertible.ToChar     (IFormatProvider provider) { return ((IConvertible)v).ToChar (provider); }
		DateTime IConvertible.ToDateTime (IFormatProvider provider) { return ((IConvertible)v).ToDateTime (provider); }
		decimal  IConvertible.ToDecimal  (IFormatProvider provider) { return ((IConvertible)v).ToDecimal (provider); }
		double   IConvertible.ToDouble   (IFormatProvider provider) { return ((IConvertible)v).ToDouble (provider); }
		short    IConvertible.ToInt16    (IFormatProvider provider) { return ((IConvertible)v).ToInt16 (provider); }
		int      IConvertible.ToInt32    (IFormatProvider provider) { return ((IConvertible)v).ToInt32 (provider); }
		long     IConvertible.ToInt64    (IFormatProvider provider) { return ((IConvertible)v).ToInt64 (provider); }
		sbyte    IConvertible.ToSByte    (IFormatProvider provider) { return ((IConvertible)v).ToSByte (provider); }
		float    IConvertible.ToSingle   (IFormatProvider provider) { return ((IConvertible)v).ToSingle (provider); }
		ushort   IConvertible.ToUInt16   (IFormatProvider provider) { return ((IConvertible)v).ToUInt16 (provider); }
		uint     IConvertible.ToUInt32   (IFormatProvider provider) { return ((IConvertible)v).ToUInt32 (provider); }
		ulong    IConvertible.ToUInt64   (IFormatProvider provider) { return ((IConvertible)v).ToUInt64 (provider); }

		object IConvertible.ToType (Type targetType, IFormatProvider provider) {
			return ((IConvertible)v).ToType (targetType, provider);
		}

		public static void CopyArray (IntPtr source, nuint [] destination, int startIndex, int length)
		{
			if (source == IntPtr.Zero)
				throw new ArgumentNullException ("source");
			if (destination == null)
				throw new ArgumentNullException ("destination");
			if (destination.Rank != 1)
				throw new ArgumentException ("destination", "array is multi-dimensional");
			if (startIndex < 0)
				throw new ArgumentException ("startIndex", "must be >= 0");
			if (length < 0)
				throw new ArgumentException ("length", "must be >= 0");
			if (startIndex + length > destination.Length)
				throw new ArgumentException ("length", "startIndex + length > destination.Length");

			for (int i = 0; i < length; i++)
				destination [i + startIndex] = (nuint)Marshal.ReadIntPtr (source, i * nuint.Size);
		}

		public static void CopyArray (nuint [] source, int startIndex, IntPtr destination, int length)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			if (destination == IntPtr.Zero)
				throw new ArgumentNullException ("destination");
			if (source.Rank != 1)
				throw new ArgumentException ("source", "array is multi-dimensional");
			if (startIndex < 0)
				throw new ArgumentException ("startIndex", "must be >= 0");
			if (length < 0)
				throw new ArgumentException ("length", "must be >= 0");
			if (startIndex + length > source.Length)
				throw new ArgumentException ("length", "startIndex + length > source.Length");

			for (int i = 0; i < length; i++)
				Marshal.WriteIntPtr (destination, i * nuint.Size, (IntPtr)source [i + startIndex]);
		}
	}
	[Serializable]
	[DebuggerDisplay ("{v,nq}")]
	public unsafe struct nfloat : IFormattable, IConvertible, IComparable, IComparable<nfloat>, IEquatable <nfloat>
	{
		internal nfloat (nfloat v) { this.v = v.v; }
		public nfloat (Single v) { this.v = v; }

#if ARCH_32
		public static readonly int Size = 4;

		public static readonly nfloat MaxValue = Single.MaxValue;
		public static readonly nfloat MinValue = Single.MinValue;
		public static readonly nfloat Epsilon = (nfloat)Single.Epsilon;
		public static readonly nfloat NaN = (nfloat)Single.NaN;
		public static readonly nfloat NegativeInfinity = (nfloat)Single.NegativeInfinity;
		public static readonly nfloat PositiveInfinity = (nfloat)Single.PositiveInfinity;

		[DebuggerBrowsable (DebuggerBrowsableState.Never)]
		internal Single v;

		public nfloat (Double v) { this.v = (Single)v; }
#else
		public static readonly int Size = 8;

		public static readonly nfloat MaxValue = (nfloat) Double.MaxValue; // 64-bit only codepath
		public static readonly nfloat MinValue = (nfloat) Double.MinValue; // 64-bit only codepath
		public static readonly nfloat Epsilon = (nfloat)Double.Epsilon;
		public static readonly nfloat NaN = (nfloat)Double.NaN;
		public static readonly nfloat NegativeInfinity = (nfloat)Double.NegativeInfinity;
		public static readonly nfloat PositiveInfinity = (nfloat)Double.PositiveInfinity;

		[DebuggerBrowsable (DebuggerBrowsableState.Never)]
		internal Double v;

		public nfloat (Double v) { this.v = v; }
#endif

		public static explicit operator nfloat (IntPtr v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat (*((float *)&v));
#else
			return new nfloat (*((double *)&v));
#endif
		}

		public static explicit operator IntPtr (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return *((IntPtr *)&v.v);
#else
			return *((IntPtr *)&v.v);
#endif
		}

		public static implicit operator nfloat (sbyte v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat ((float)v);
#else
			return new nfloat ((double)v);
#endif
		}

		public static explicit operator sbyte (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (sbyte)v.v;
#else
			return (sbyte)v.v;
#endif
		}

		public static implicit operator nfloat (byte v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat ((float)v);
#else
			return new nfloat ((double)v);
#endif
		}

		public static explicit operator byte (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (byte)v.v;
#else
			return (byte)v.v;
#endif
		}

		public static implicit operator nfloat (char v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat ((float)v);
#else
			return new nfloat ((double)v);
#endif
		}

		public static explicit operator char (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (char)v.v;
#else
			return (char)v.v;
#endif
		}

		public static implicit operator nfloat (short v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat ((float)v);
#else
			return new nfloat ((double)v);
#endif
		}

		public static explicit operator short (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (short)v.v;
#else
			return (short)v.v;
#endif
		}

		public static implicit operator nfloat (ushort v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat ((float)v);
#else
			return new nfloat ((double)v);
#endif
		}

		public static explicit operator ushort (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (ushort)v.v;
#else
			return (ushort)v.v;
#endif
		}

		public static implicit operator nfloat (int v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat ((float)v);
#else
			return new nfloat ((double)v);
#endif
		}

		public static explicit operator int (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (int)v.v;
#else
			return (int)v.v;
#endif
		}

		public static implicit operator nfloat (uint v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat ((float)v);
#else
			return new nfloat ((double)v);
#endif
		}

		public static explicit operator uint (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (uint)v.v;
#else
			return (uint)v.v;
#endif
		}

		public static implicit operator nfloat (long v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat ((float)v);
#else
			return new nfloat ((double)v);
#endif
		}

		public static explicit operator long (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (long)v.v;
#else
			return (long)v.v;
#endif
		}

		public static implicit operator nfloat (ulong v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat ((float)v);
#else
			return new nfloat ((double)v);
#endif
		}

		public static explicit operator ulong (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (ulong)v.v;
#else
			return (ulong)v.v;
#endif
		}

		public static implicit operator nfloat (float v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat ((float)v);
#else
			return new nfloat ((double)v);
#endif
		}

		public static explicit operator float (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (float)v.v;
#else
			return (float)v.v;
#endif
		}

		public static explicit operator nfloat (double v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat ((float)v);
#else
			return new nfloat ((double)v);
#endif
		}

		public static implicit operator double (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (double)v.v;
#else
			return (double)v.v;
#endif
		}

		public static explicit operator nfloat (decimal v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return new nfloat ((float)v);
#else
			return new nfloat ((double)v);
#endif
		}

		public static explicit operator decimal (nfloat v)
		{
#if NINT_JIT_OPTIMIZED
			throw new NotImplementedException ();
#elif ARCH_32
			return (decimal)v.v;
#else
			return (decimal)v.v;
#endif
		}

#if NINT_JIT_OPTIMIZED
		public static nfloat operator + (nfloat v) { throw new NotImplementedException (); }
		public static nfloat operator - (nfloat v) { throw new NotImplementedException (); }
#else
		public static nfloat operator + (nfloat v) { return new nfloat (+v.v); }
		public static nfloat operator - (nfloat v) { return new nfloat (-v.v); }
#endif

#if NINT_JIT_OPTIMIZED
		public static nfloat operator ++ (nfloat v) { throw new NotImplementedException (); }
		public static nfloat operator -- (nfloat v) { throw new NotImplementedException (); }
#else
		public static nfloat operator ++ (nfloat v) { return new nfloat (v.v + 1); }
		public static nfloat operator -- (nfloat v) { return new nfloat (v.v - 1); }
#endif

#if NINT_JIT_OPTIMIZED
		public static nfloat operator + (nfloat l, nfloat r) { throw new NotImplementedException (); }
		public static nfloat operator - (nfloat l, nfloat r) { throw new NotImplementedException (); }
		public static nfloat operator * (nfloat l, nfloat r) { throw new NotImplementedException (); }
		public static nfloat operator / (nfloat l, nfloat r) { throw new NotImplementedException (); }
		public static nfloat operator % (nfloat l, nfloat r) { throw new NotImplementedException (); }
#else
		public static nfloat operator + (nfloat l, nfloat r) { return new nfloat (l.v + r.v); }
		public static nfloat operator - (nfloat l, nfloat r) { return new nfloat (l.v - r.v); }
		public static nfloat operator * (nfloat l, nfloat r) { return new nfloat (l.v * r.v); }
		public static nfloat operator / (nfloat l, nfloat r) { return new nfloat (l.v / r.v); }
		public static nfloat operator % (nfloat l, nfloat r) { return new nfloat (l.v % r.v); }
#endif

#if NINT_JIT_OPTIMIZED
		public static bool operator == (nfloat l, nfloat r) { throw new NotImplementedException (); }
		public static bool operator != (nfloat l, nfloat r) { throw new NotImplementedException (); }
		public static bool operator <  (nfloat l, nfloat r) { throw new NotImplementedException (); }
		public static bool operator >  (nfloat l, nfloat r) { throw new NotImplementedException (); }
		public static bool operator <= (nfloat l, nfloat r) { throw new NotImplementedException (); }
		public static bool operator >= (nfloat l, nfloat r) { throw new NotImplementedException (); }
#else
		public static bool operator == (nfloat l, nfloat r) { return l.v == r.v; }
		public static bool operator != (nfloat l, nfloat r) { return l.v != r.v; }
		public static bool operator <  (nfloat l, nfloat r) { return l.v < r.v; }
		public static bool operator >  (nfloat l, nfloat r) { return l.v > r.v; }
		public static bool operator <= (nfloat l, nfloat r) { return l.v <= r.v; }
		public static bool operator >= (nfloat l, nfloat r) { return l.v >= r.v; }
#endif

		public int CompareTo (nfloat value) { return v.CompareTo (value.v); }
		public int CompareTo (object value)
		{
			if (value is nfloat)
				return v.CompareTo (((nfloat) value).v);
			return v.CompareTo (value);
		}
		public bool Equals (nfloat obj) { return v.Equals (obj.v); }
		public override bool Equals (object obj)
		{
			if (obj is nfloat)
				return v.Equals (((nfloat) obj).v);
			return v.Equals (obj);
		}
		public override int GetHashCode () { return v.GetHashCode (); }

#if ARCH_32
		public static bool IsNaN              (nfloat f) { return Single.IsNaN ((Single)f); }
		public static bool IsInfinity         (nfloat f) { return Single.IsInfinity ((Single)f); }
		public static bool IsPositiveInfinity (nfloat f) { return Single.IsPositiveInfinity ((Single)f); }
		public static bool IsNegativeInfinity (nfloat f) { return Single.IsNegativeInfinity ((Single)f); }

		public static nfloat Parse (string s, IFormatProvider provider) { return (nfloat)Single.Parse (s, provider); }
		public static nfloat Parse (string s, NumberStyles style) { return (nfloat)Single.Parse (s, style); }
		public static nfloat Parse (string s) { return (nfloat)Single.Parse (s); }
		public static nfloat Parse (string s, NumberStyles style, IFormatProvider provider) {
			return (nfloat)Single.Parse (s, style, provider);
		}

		public static bool TryParse (string s, out nfloat result)
		{
			Single v;
			var r = Single.TryParse (s, out v);
			result = (nfloat)v;
			return r;
		}

		public static bool TryParse (string s, NumberStyles style, IFormatProvider provider, out nfloat result)
		{
			Single v;
			var r = Single.TryParse (s, style, provider, out v);
			result = (nfloat)v;
			return r;
		}
#else
		public static bool IsNaN              (nfloat f) { return Double.IsNaN ((Double)f); }
		public static bool IsInfinity         (nfloat f) { return Double.IsInfinity ((Double)f); }
		public static bool IsPositiveInfinity (nfloat f) { return Double.IsPositiveInfinity ((Double)f); }
		public static bool IsNegativeInfinity (nfloat f) { return Double.IsNegativeInfinity ((Double)f); }

		public static nfloat Parse (string s, IFormatProvider provider) { return (nfloat)Double.Parse (s, provider); }
		public static nfloat Parse (string s, NumberStyles style) { return (nfloat)Double.Parse (s, style); }
		public static nfloat Parse (string s) { return (nfloat)Double.Parse (s); }
		public static nfloat Parse (string s, NumberStyles style, IFormatProvider provider) {
			return (nfloat)Double.Parse (s, style, provider);
		}

		public static bool TryParse (string s, out nfloat result)
		{
			Double v;
			var r = Double.TryParse (s, out v);
			result = (nfloat)v;
			return r;
		}

		public static bool TryParse (string s, NumberStyles style, IFormatProvider provider, out nfloat result)
		{
			Double v;
			var r = Double.TryParse (s, style, provider, out v);
			result = (nfloat)v;
			return r;
		}
#endif

		public override string ToString () { return v.ToString (); }
		public string ToString (IFormatProvider provider) { return v.ToString (provider); }
		public string ToString (string format) { return v.ToString (format); }
		public string ToString (string format, IFormatProvider provider) { return v.ToString (format, provider); }

		public TypeCode GetTypeCode () { return v.GetTypeCode (); }

		bool     IConvertible.ToBoolean  (IFormatProvider provider) { return ((IConvertible)v).ToBoolean (provider); }
		byte     IConvertible.ToByte     (IFormatProvider provider) { return ((IConvertible)v).ToByte (provider); }
		char     IConvertible.ToChar     (IFormatProvider provider) { return ((IConvertible)v).ToChar (provider); }
		DateTime IConvertible.ToDateTime (IFormatProvider provider) { return ((IConvertible)v).ToDateTime (provider); }
		decimal  IConvertible.ToDecimal  (IFormatProvider provider) { return ((IConvertible)v).ToDecimal (provider); }
		double   IConvertible.ToDouble   (IFormatProvider provider) { return ((IConvertible)v).ToDouble (provider); }
		short    IConvertible.ToInt16    (IFormatProvider provider) { return ((IConvertible)v).ToInt16 (provider); }
		int      IConvertible.ToInt32    (IFormatProvider provider) { return ((IConvertible)v).ToInt32 (provider); }
		long     IConvertible.ToInt64    (IFormatProvider provider) { return ((IConvertible)v).ToInt64 (provider); }
		sbyte    IConvertible.ToSByte    (IFormatProvider provider) { return ((IConvertible)v).ToSByte (provider); }
		float    IConvertible.ToSingle   (IFormatProvider provider) { return ((IConvertible)v).ToSingle (provider); }
		ushort   IConvertible.ToUInt16   (IFormatProvider provider) { return ((IConvertible)v).ToUInt16 (provider); }
		uint     IConvertible.ToUInt32   (IFormatProvider provider) { return ((IConvertible)v).ToUInt32 (provider); }
		ulong    IConvertible.ToUInt64   (IFormatProvider provider) { return ((IConvertible)v).ToUInt64 (provider); }

		object IConvertible.ToType (Type targetType, IFormatProvider provider) {
			return ((IConvertible)v).ToType (targetType, provider);
		}

		public static void CopyArray (IntPtr source, nfloat [] destination, int startIndex, int length)
		{
			if (source == IntPtr.Zero)
				throw new ArgumentNullException ("source");
			if (destination == null)
				throw new ArgumentNullException ("destination");
			if (destination.Rank != 1)
				throw new ArgumentException ("destination", "array is multi-dimensional");
			if (startIndex < 0)
				throw new ArgumentException ("startIndex", "must be >= 0");
			if (length < 0)
				throw new ArgumentException ("length", "must be >= 0");
			if (startIndex + length > destination.Length)
				throw new ArgumentException ("length", "startIndex + length > destination.Length");

			for (int i = 0; i < length; i++)
				destination [i + startIndex] = (nfloat)Marshal.ReadIntPtr (source, i * nfloat.Size);
		}

		public static void CopyArray (nfloat [] source, int startIndex, IntPtr destination, int length)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			if (destination == IntPtr.Zero)
				throw new ArgumentNullException ("destination");
			if (source.Rank != 1)
				throw new ArgumentException ("source", "array is multi-dimensional");
			if (startIndex < 0)
				throw new ArgumentException ("startIndex", "must be >= 0");
			if (length < 0)
				throw new ArgumentException ("length", "must be >= 0");
			if (startIndex + length > source.Length)
				throw new ArgumentException ("length", "startIndex + length > source.Length");

			for (int i = 0; i < length; i++)
				Marshal.WriteIntPtr (destination, i * nfloat.Size, (IntPtr)source [i + startIndex]);
		}
	}
}
