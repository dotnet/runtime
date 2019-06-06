using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

/*
 * Regression tests for the mono JIT.
 *
 * Each test needs to be of the form:
 *
 * static int test_<result>_<name> ();
 *
 * where <result> is an integer (the value that needs to be returned by
 * the method to make it pass.
 * <name> is a user-displayed name used to identify the test.
 *
 * The tests can be driven in two ways:
 * *) running the program directly: Main() uses reflection to find and invoke
 * 	the test methods (this is useful mostly to check that the tests are correct)
 * *) with the --regression switch of the jit (this is the preferred way since
 * 	all the tests will be run with optimizations on and off)
 *
 * The reflection logic could be moved to a .dll since we need at least another
 * regression test file written in IL code to have better control on how
 * the IL code looks.
 */

#if __MOBILE__
namespace ObjectTests
{
#endif

struct Simple {
	public int a;
	public byte b;
	public short c;
	public long d;
}

struct Small {
	public byte b1;
	public byte b2;
}

// Size=2, Align=1
struct Foo {
	bool b1;
	bool b2;
}

struct Large {
	int one;
	int two;
	long three;
	long four;
	int five;
	long six;
	int seven;
	long eight;
	long nine;
	long ten;

	public void populate ()
	{
		one = 1; two = 2;
		three = 3; four = 4;
		five = 5; six = 6;
		seven = 7; eight = 8;
		nine = 9; ten = 10;
	}
	public bool check ()
	{
		return one == 1  && two == 2  &&
			three == 3  && four == 4  &&
			five == 5  && six == 6  &&
			seven == 7  && eight == 8  &&
			nine == 9  && ten == 10;
	}
}

class Sample {
	public int a;
	public Sample (int v) {
		a = v;
	}
}

[StructLayout ( LayoutKind.Explicit )]
struct StructWithBigOffsets {
		[ FieldOffset(10000) ] public byte b;
		[ FieldOffset(10001) ] public sbyte sb;
		[ FieldOffset(11000) ] public short s;
		[ FieldOffset(11002) ] public ushort us;
		[ FieldOffset(12000) ] public uint i;
		[ FieldOffset(12004) ] public int si;
		[ FieldOffset(13000) ] public long l;
		[ FieldOffset(14000) ] public float f;
		[ FieldOffset(15000) ] public double d;
}

enum SampleEnum {
	A,
	B,
	C
}

struct Alpha {
	public long a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v;
}

struct Beta {
	public Alpha a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v;
}

struct Gamma {
	public Beta a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v;
}

class Tests {

#if !__MOBILE__
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}
#endif
	
	public static int test_0_return () {
		Simple s;
		s.a = 1;
		s.b = 2;
		s.c = (short)(s.a + s.b);
		s.d = 4;
		return s.a - 1;
	}

	public static int test_0_string_access () {
		string s = "Hello";
		if (s [1] != 'e')
			return 1;
		return 0;
	}

	public static int test_0_string_virtual_call () {
		string s = "Hello";
		string s2 = s.ToString ();
		if (s2 [1] != 'e')
			return 1;
		return 0;
	}

	public static int test_0_iface_call () {
		string s = "Hello";
		object o = ((ICloneable)s).Clone ();
		return 0;
	}

	public static int test_5_newobj () {
		Sample s = new Sample (5);
		return s.a;
	}

	public static int test_4_box () {
		object obj = 4;
		return (int)obj;
	}

	public static int test_0_enum_unbox () {
		SampleEnum x = SampleEnum.A;
		object o = x;
		
		int res = 1;

		res = (int)o;
		
		return res;
	}
	
	static Simple get_simple (int v) {
		Simple r = new Simple ();
		r.a = v;
		r.b = (byte)(v + 1);
		r.c = (short)(v + 2);
		r.d = v + 3;

		return r;
	}

	public static int test_3_return_struct () {
		Simple v = get_simple (1);

		if (v.a != 1)
			return 0;
		if (v.b != 2)
			return 0;
		if (v.c != 3)
			return 0;
		if (v.d != 4)
			return 0;
		return 3;
	}

	public virtual Simple v_get_simple (int v)
	{
		return get_simple (v);
	}
	
	public static int test_2_return_struct_virtual () {
		Tests t = new Tests ();
		Simple v = t.v_get_simple (2);

		if (v.a != 2)
			return 0;
		if (v.b != 3)
			return 0;
		if (v.c != 4)
			return 0;
		if (v.d != 5)
			return 0;
		return 2;
	}

	static int receive_simple (int a, Simple v, int b) {
		if (v.a != 1)
			return 1;
		if (v.b != 2)
			return 2;
		if (v.c != 3)
			return 3;
		if (v.d != 4)
			return 4;
		if (a != 7)
			return 5;
		if (b != 9)
			return 6;
		return 0;
	}
	
	public static int test_5_pass_struct () {
		Simple v = get_simple (1);
		if (receive_simple (7, v, 9) != 0)
			return 0;
		if (receive_simple (7, get_simple (1), 9) != 0)
			return 1;
		return 5;
	}

	static Simple s_v;
	public static int test_5_pass_static_struct () {
		s_v = get_simple (1);
		if (receive_simple (7, s_v, 9) != 0)
			return 0;
		return 5;
	}

	// Test alignment of small structs

	static Small get_small (byte v) {
		Small r = new Small ();
	
		r.b1 = v;
		r.b2 = (byte)(v + 1);

		return r;
	}

	static Small return_small (Small s) {
		return s;
	}

	static int receive_small (int a, Small v, int b) {
		if (v.b1 != 1)
			return 1;
		if (v.b2 != 2)
			return 2;
		return 0;
	}

	static int receive_small_sparc_many_args (int a, int a2, int a3, int a4, int a5, int a6, Small v, int b) {
		if (v.b1 != 1)
			return 1;
		if (v.b2 != 2)
			return 2;
		return 0;
	}

	public static int test_5_pass_small_struct () {
		Small v = get_small (1);
		if (receive_small (7, v, 9) != 0)
			return 0;
		if (receive_small (7, get_small (1), 9) != 0)
			return 1;
		if (receive_small_sparc_many_args (1, 2, 3, 4, 5, 6, v, 9) != 0)
			return 2;
		v = return_small (v);
		if (v.b1 != 1)
			return 3;
		if (v.b2 != 2)
			return 4;
		return 5;
	}

	// 64-bits, 32-bit aligned
	struct struct1 {
		public int	a;
		public int	b;
	};

	static int check_struct1(struct1 x) {
		if (x.a != 1)
			return 1;
		if (x.b != 2)
			return 2;
		return 0;
	}

	static int pass_struct1(int a, int b, struct1 x) {
		if (a != 3)
			return 3;
		if (b != 4)
			return 4;
		return check_struct1(x);
	}

	static int pass_struct1(int a, struct1 x) {
		if (a != 3)
			return 3;
		return check_struct1(x);
	}

	static int pass_struct1(struct1 x) {
		return check_struct1(x);
	}

	public static int test_0_struct1_args () {
		int r;
		struct1 x;

		x.a = 1;
		x.b = 2;
		if ((r = check_struct1(x)) != 0)
			return r;
		if ((r = pass_struct1(x)) != 0)
			return r + 10;
		if ((r = pass_struct1(3, x)) != 0)
			return r + 20;
		if ((r = pass_struct1(3, 4, x)) != 0)
			return r + 30;
		return 0;
	}

	// 64-bits, 64-bit aligned
	struct struct2 {
		public long	a;
	};

	static int check_struct2(struct2 x) {
		if (x.a != 1)
			return 1;
		return 0;
	}

	static int pass_struct2(int a, int b, int c, struct2 x) {
		if (a != 3)
			return 3;
		if (b != 4)
			return 4;
		if (c != 5)
			return 5;
		return check_struct2(x);
	}

	static int pass_struct2(int a, int b, struct2 x) {
		if (a != 3)
			return 3;
		if (b != 4)
			return 4;
		return check_struct2(x);
	}

	static int pass_struct2(int a, struct2 x) {
		if (a != 3)
			return 3;
		return check_struct2(x);
	}

	static int pass_struct2(struct2 x) {
		return check_struct2(x);
	}

	public static int test_0_struct2_args () {
		int r;
		struct2 x;

		x.a = 1;
		if ((r = check_struct2(x)) != 0)
			return r;
		if ((r = pass_struct2(x)) != 0)
			return r + 10;
		if ((r = pass_struct2(3, x)) != 0)
			return r + 20;
		if ((r = pass_struct2(3, 4, x)) != 0)
			return r + 30;
		if ((r = pass_struct2(3, 4, 5, x)) != 0)
			return r + 40;
		return 0;
	}

	// 128 bits
	struct Struct3 {
		public long i, j, k, l;
	}

	static int pass_struct3 (int i, int j, int k, int l, int m, int n, int o, int p, Struct3 s, int q) {
		if (s.i + s.j + s.k + s.l != 10)
			return 1;
		else
			return 0;
	}

	public static int test_0_struct3_args () {
		Struct3 s = new Struct3 ();
		s.i = 1;
		s.j = 2;
		s.k = 3;
		s.l = 4;

		return pass_struct3 (1, 2, 3, 4, 5, 6, 7, 8, s, 9);
	}

	// Struct with unaligned size on 64 bit machines
	struct Struct4 {
        public int i, j, k, l, m;
		public int i1, i2, i3, i4, i5, i6;
	}

	static int pass_struct4 (Struct4 s) {
		if (s.i + s.j + s.k + s.l + s.m != 15)
			return 1;
		else
			return 0;
	}

	public static int test_0_struct4_args () {
		Struct4 s = new Struct4 ();
		s.i = 1;
		s.j = 2;
		s.k = 3;
		s.l = 4;
		s.m = 5;

		return pass_struct4 (s);
	}



	struct AStruct {
		public int i;

		public AStruct (int i) {
			this.i = i;
		}

		public override int GetHashCode () {
			return i;
		}
	}

	// Test that vtypes are unboxed during a virtual call
	public static int test_44_unbox_trampoline () {
		AStruct s = new AStruct (44);
		object o = s;
		return o.GetHashCode ();
	}

	public static int test_0_unbox_trampoline2 () {
		int i = 12;
		object o = i;
			
		if (i.ToString () != "12")
			return 1;
		if (((Int32)o).ToString () != "12")
			return 2;
		if (o.ToString () != "12")
			return 3;
		return 0;
	}

	// Test fields with big offsets
	public static int test_0_fields_with_big_offsets () {
		StructWithBigOffsets s = new StructWithBigOffsets ();
		StructWithBigOffsets s2 = new StructWithBigOffsets ();

		s.b = 0xde;
		s.sb = 0xe;
		s.s = 0x12de;
		s.us = 0x12da;
		s.i = 0xdeadbeef;
		s.si = 0xcafe;
		s.l = 0xcafebabe;
		s.f = 3.14F;
		s.d = 3.14;

		s2.b = s.b;
		s2.sb = s.sb;
		s2.s = s.s;
		s2.us = s.us;
		s2.i = s.i;
		s2.si = s.si;
		s2.l = s.l;
		s2.f = s.f;
		s2.d = s.d;

		if (s2.b != 0xde)
			return 1;
		if (s2.s != 0x12de)
			return 2;
		if (s2.i != 0xdeadbeef)
			return 3;
		if (s2.l != 0xcafebabe)
			return 4;
		if (s2.f != 3.14F)
			return 5;
		if (s2.d != 3.14)
			return 6;
		if (s2.sb != 0xe)
			return 7;
		if (s2.us != 0x12da)
			return 9;
		if (s2.si != 0xcafe)
			return 10;

		return 0;
	}

	class TestRegA {

		long buf_start;
		int buf_length, buf_offset;

		public TestRegA () {
			buf_start = 0;
			buf_length = 0;
			buf_offset = 0;
		}
	
		public long Seek (long position) {
			long pos = position;
			/* interaction between the register allocator and
			 * allocating arguments to registers */
			if (pos >= buf_start && pos <= buf_start + buf_length) {
				buf_offset = (int) (pos - buf_start);
				return pos;
			}
			return buf_start;
		}

	}

	public static int test_0_seektest () {
		TestRegA t = new TestRegA ();
		return (int)t.Seek (0);
	}

	class Super : ICloneable {
		public virtual object Clone () {
			return null;
		}
	}
	class Duper: Super {
	}

	public static int test_0_null_cast () {
		object o = null;

		Super s = (Super)o;

		return 0;
	}
	
	public static int test_0_super_cast () {
		Duper d = new Duper ();
		Super sup = d;
		Object o = d;

		if (!(o is Super))
			return 1;
		try {
			d = (Duper)sup;
		} catch {
			return 2;
		}
		if (!(d is Object))
			return 3;
		try {
			d = (Duper)(object)sup;
		} catch {
			return 4;
		}
		return 0;
	}

	public static int test_0_super_cast_array () {
		Duper[] d = new Duper [0];
		Super[] sup = d;
		Object[] o = d;

		if (!(o is Super[]))
			return 1;
		try {
			d = (Duper[])sup;
		} catch {
			return 2;
		}
		if (!(d is Object[]))
			return 3;
		try {
			d = (Duper[])(object[])sup;
		} catch {
			return 4;
		}
		return 0;
	}

	public static int test_0_multi_array_cast () {
		Duper[,] d = new Duper [1, 1];
		object[,] o = d;

		try {
			o [0, 0] = new Super ();
			return 1;
		}
		catch (ArrayTypeMismatchException) {
		}

		return 0;
	}

	public static int test_0_vector_array_cast () {
		Array arr1 = Array.CreateInstance (typeof (int), new int[] {1}, new int[] {0});
		Array arr2 = Array.CreateInstance (typeof (int), new int[] {1}, new int[] {10});
		Array arr5 = Array.CreateInstance (typeof (string), new int[] {1}, new int[] {10});

		if (arr1.GetType () != typeof (int[]))
			return 1;

		if (arr2.GetType () == typeof (int[]))
			return 2;

		int[] b;

		b = (int[])arr1;

		try {
			b = (int[])arr2;
			return 3;
		}
		catch (InvalidCastException) {
		}

		if (arr2 is int[])
			return 4;
		var as_object_arr = arr5 as object [];
		if (as_object_arr != null)
			return 5;

		int [,] [] arr3 = new int [1, 1] [];
		object o = arr3;
		int [,] [] arr4 = (int [,] [])o;

		return 0;
	}

	public static int test_0_enum_array_cast () {
		TypeCode[] tc = new TypeCode [0];
		object[] oa;
		ValueType[] vta;
		int[] inta;
		Array a = tc;
		bool ok;

		if (a is object[])
			return 1;
		if (a is ValueType[])
			return 2;
		if (a is Enum[])
			return 3;
		try {
			ok = false;
			oa = (object[])a;
		} catch {
			ok = true;
		}
		if (!ok)
			return 4;
		try {
			ok = false;
			vta = (ValueType[])a;
		} catch {
			ok = true;
		}
		if (!ok)
			return 5;
		try {
			ok = true;
			inta = (int[])a;
		} catch {
			ok = false;
		}
		if (!ok)
			return 6;
		return 0;
	}

	public static int test_0_more_cast_corner_cases () {
		ValueType[] vta = new ValueType [0];
		Enum[] ea = new Enum [0];
		Array a = vta;
		object[] oa;
		bool ok;

		if (!(a is object[]))
			return 1;
		if (!(a is ValueType[]))
			return 2;
		if (a is Enum[])
			return 3;
		a = ea;
		if (!(a is object[]))
			return 4;
		if (!(a is ValueType[]))
			return 5;
		if (!(a is Enum[]))
			return 6;

		try {
			ok = true;
			oa = (object[])a;
		} catch {
			ok = false;
		}
		if (!ok)
			return 7;
	
		try {
			ok = true;
			oa = (Enum[])a;
		} catch {
			ok = false;
		}
		if (!ok)
			return 8;
	
		try {
			ok = true;
			oa = (ValueType[])a;
		} catch {
			ok = false;
		}
		if (!ok)
			return 9;

		a = vta;
		try {
			ok = true;
			oa = (object[])a;
		} catch {
			ok = false;
		}
		if (!ok)
			return 10;
	
		try {
			ok = true;
			oa = (ValueType[])a;
		} catch {
			ok = false;
		}
		if (!ok)
			return 11;
	
		try {
			ok = false;
			vta = (Enum[])a;
		} catch {
			ok = true;
		}
		if (!ok)
			return 12;

		object arr = new int [10];
		if (arr is IList<int?>)
			return 13;

		return 0;
	}

	public static int test_0_cast_iface_array () {
		object o = new ICloneable [0];
		object o2 = new Duper [0];
		object t;
		bool ok;

		if (!(o is object[]))
			return 1;
		if (!(o2 is ICloneable[]))
			return 2;

		try {
			ok = true;
			t = (object[])o;
		} catch {
			ok = false;
		}
		if (!ok)
			return 3;
	
		try {
			ok = true;
			t = (ICloneable[])o2;
		} catch {
			ok = false;
		}
		if (!ok)
			return 4;

		try {
			ok = true;
			t = (ICloneable[])o;
		} catch {
			ok = false;
		}
		if (!ok)
			return 5;

		if (!(o is ICloneable[]))
			return 6;

		/* add tests for interfaces that 'inherit' interfaces */
		return 0;
	}

	public static unsafe int test_0_pointer_array() {
		int*[] ipa = new int* [0];
		Array a = ipa;

		
		if (a is object[])
			return 1;
		if (!(a is int*[]))
			return 2;
		if (a is ValueType[])
			return 3;
		if (a is Enum[])
			return 4;
		if (a is char*[])
			return 5;

		return 0;
	}

	private static int[] daysmonthleap = { 0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

	private static int AbsoluteDays (int year, int month, int day)
	{
		int temp = 0, m = 1;
		int[] days = daysmonthleap;
		while (m < month)
			temp += days[m++];
		return ((day-1) + temp + (365* (year-1)) + ((year-1)/4) - ((year-1)/100) + ((year-1)/400));
	}

	public static int test_719162_complex_div () {
		int adays = AbsoluteDays (1970, 1, 1);
		return adays;
	}

	delegate int GetIntDel ();

	static int return4 () {
		return 4;
	}

	int return5 () {
		return 5;
	}

	public static int test_2_static_delegate () {
		GetIntDel del = new GetIntDel (return4);
		int v = del ();
		if (v != 4)
			return 0;
		return 2;
	}

	public static int test_2_instance_delegate () {
		Tests t = new Tests ();
		GetIntDel del = new GetIntDel (t.return5);
		int v = del ();
		if (v != 5)
			return 0;
		return 2;
	}

	class InstanceDelegateTest {
		public int a;

		public int return_field () {
			return a;
		}
	}

	public static int test_2_instance_delegate_with_field () {
		InstanceDelegateTest t = new InstanceDelegateTest () { a = 1337 };
		GetIntDel del = new GetIntDel (t.return_field);
		int v = del ();
		if (v != 1337)
			return 0;
		return 2;
	}

	interface IFaceVirtualDel {
		int return_field ();
	}

	struct VtypeVirtualDelStruct : IFaceVirtualDel {
		public int f;
		public int return_field_nonvirt () {
			return f;
		}
		public int return_field () {
			return f;
		}
	}

	public static int test_42_vtype_delegate () {
		var s = new VtypeVirtualDelStruct () { f = 42 };
		Func<int> f = s.return_field_nonvirt;
		return f ();
	}

	public static int test_42_vtype_virtual_delegate () {
		IFaceVirtualDel s = new VtypeVirtualDelStruct () { f = 42 };
		Func<int> f = s.return_field;
		return f ();
	}

	public static int test_1_store_decimal () {
		decimal[,] a = {{1}};

		if (a[0,0] != 1m)
			return 0;
		return 1;
	}

	public static int test_2_intptr_stobj () {
		System.IntPtr [] arr = { new System.IntPtr () };

		if (arr [0] != (System.IntPtr)0)
			return 1;
		return 2;
	}

	static int llmult (int a, int b, int c, int d) {
		return a + b + c + d;
	}

	/* 
	 * Test that evaluation of complex arguments does not overwrite the
	 * arguments already in outgoing registers.
	 */
	public static int test_155_regalloc () {
		int a = 10;
		int b = 10;

		int c = 0;
		int d = 0;
		int[] arr = new int [5];

		return llmult (arr [c + d], 150, 5, 0);
	}

	static bool large_struct_test (Large a, Large b, Large c, Large d)
	{
		if (!a.check ()) return false;
		if (!b.check ()) return false;
		if (!c.check ()) return false;
		if (!d.check ()) return false;
		return true;
	}

	public static int test_2_large_struct_pass ()
	{
		Large a, b, c, d;
		a = new Large ();
		b = new Large ();
		c = new Large ();
		d = new Large ();
		a.populate ();
		b.populate ();
		c.populate ();
		d.populate ();
		if (large_struct_test (a, b, c, d))
			return 2;
		return 0;
	}

	public static unsafe int test_0_pin_string () {
		string x = "xxx";
		fixed (char *c = x) {
			if (*c != 'x')
				return 1;
		}
		return 0;
	}
	
	public static int my_flags;
	public static int test_0_and_cmp_static ()
	{
		
		/* various forms of test [mem], imm */
		
		my_flags = 0x01020304;
		
		if ((my_flags & 0x01020304) == 0)
			return 1;
		
		if ((my_flags & 0x00000304) == 0)
			return 2;
		
		if ((my_flags & 0x00000004) == 0)
			return 3;
		
		if ((my_flags & 0x00000300) == 0)
			return 4;
		
		if ((my_flags & 0x00020000) == 0)
			return 5;
		
		if ((my_flags & 0x01000000) == 0)
			return 6;
		
		return 0;
	}
	
	static byte b;
	public static int test_0_byte_compares ()
	{
		b = 0xff;
		if (b == -1)
			return 1;
		b = 0;
		if (!(b < System.Byte.MaxValue))
			return 2;
		
		if (!(b <= System.Byte.MaxValue))
			return 3;
		
		return 0;
	}

	static Nullable<bool> s_nullb;
	static AStruct s_struct1;

	/* test if VES uses correct sizes for value type write to static field */
	public static int test_0_static_nullable_bool () {
		s_struct1 = new AStruct (0x1337dead);
		s_nullb = true;
		/* make sure that the write to s_nullb didn't smash the value after it */
		if (s_struct1.i != 0x1337dead)
			return 2;
		return 0;
	}

	public static int test_71_long_shift_right () {
		ulong value = 38654838087;
		int x = 0;
		byte [] buffer = new byte [1];
		buffer [x] = ((byte)(value >> x));
		return buffer [x];
	}
	
	static long x;
	public static int test_0_addsub_mem ()
	{
		x = 0;
		x += 5;
		
		if (x != 5)
			return 1;
		
		x -= 10;
		
		if (x != -5)
			return 2;
		
		return 0;
	}
	
	static ulong y;
	public static int test_0_sh32_mem ()
	{
		y = 0x0102130405060708;
		y >>= 32;
		
		if (y != 0x01021304)
			return 1;
		
		y = 0x0102130405060708;
		y <<= 32;
		
		if (y != 0x0506070800000000)
			return 2;
		
		x = 0x0102130405060708;
		x <<= 32;
		
		if (x != 0x0506070800000000)
			return 2;
		
		return 0;
	}


	static uint dum_de_dum = 1;
	public static int test_0_long_arg_opt ()
	{
		return Foo (0x1234567887654321, dum_de_dum);
	}
	
	static int Foo (ulong x, ulong y)
	{
		if (x != 0x1234567887654321)
			return 1;
		
		if (y != 1)
			return 2;
		
		return 0;
	}
	
	public static int test_0_long_ret_opt ()
	{
		ulong x = X ();
		if (x != 0x1234567887654321)
			return 1;
		ulong y = Y ();
		if (y != 1)
			return 2;
		
		return 0;
	}
	
	static ulong X ()
	{
		return 0x1234567887654321;
	}
	
	static ulong Y ()
	{
		return dum_de_dum;
	}

	/* from bug# 71515 */
	static int counter = 0;
	static bool WriteStuff () {
		counter = 10;
		return true;
	}
	public static int test_0_cond_branch_side_effects () {
		counter = 5;
		if (WriteStuff()) {
		}
		if (counter == 10)
			return 0;
		return 1;
	}

	// bug #74992
	public static int arg_only_written (string file_name, int[]
ncells ) {
		if (file_name == null)
			return 1;

		ncells = foo ();
		bar (ncells [0]);

		return 0;
	}

	public static int[] foo () {
		return new int [3];
	}

	public static void bar (int i) {
	}
	

	public static int test_0_arg_only_written ()
	{
		return arg_only_written ("md.in", null);
	}		

	static long position = 0;

	public static int test_4_static_inc_long () {

		int count = 4;

		position = 0;

		position += count;

		return (int)position;
	}

	struct FooStruct {

		public FooStruct (long l) {
		}
	}

	public static int test_0_calls_opcode_emulation () {
		// Test that emulated opcodes do not clobber arguments already in
		// out registers
		checked {
			long val = 10000;
			new FooStruct (val * 10000);
		}
		return 0;
	}

	public static int test_0_intrins_string_length () {
		string s = "ABC";

		return (s.Length == 3) ? 0 : 1;
	}

	public static int test_0_intrins_string_chars () {
		string s = "ABC";

		return (s [0] == 'A' && s [1] == 'B' && s [2] == 'C') ? 0 : 1;
	}

	public static int test_0_intrins_object_gettype () {
		object o = 1;

		return (o.GetType () == typeof (int)) ? 0 : 1;
	}

	public static int test_0_intrins_object_gethashcode () {
		object o = new Object ();

		return (o.GetHashCode () == o.GetHashCode ()) ? 0 : 1;
	}

	class FooClass {
	}

	public static int test_0_intrins_object_ctor () {
		object o = new FooClass ();

		return (o != null) ? 0 : 1;
	}

	public static int test_0_intrins_array_rank () {
		int[,] a = new int [10, 10];

		return (a.Rank == 2) ? 0 : 1;
	}

	public static int test_0_intrins_array_length () {
		int[,] a = new int [10, 10];
		Array a2 = a;

		return (a2.Length == 100) ? 0 : 1;
	}

	public static int test_0_intrins_runtimehelpers_offset_to_string_data () {
		int i = RuntimeHelpers.OffsetToStringData;
		
		return i - i;
	}

	public static int test_0_intrins_string_setchar () {
		StringBuilder sb = new StringBuilder ("ABC");

		sb [1] = 'D';

		return sb.ToString () == "ADC" ? 0 : 1;
	}

	enum FlagsEnum {
		None = 0,
		A = 1,
		B = 2,
		C = 4
	}

	public static int test_0_intrins_enum_hasflag () {
		var e = FlagsEnum.A | FlagsEnum.B;

		if (!e.HasFlag (FlagsEnum.A))
			return 1;
		if (!e.HasFlag (FlagsEnum.A | FlagsEnum.B))
			return 2;
		if (!e.HasFlag (FlagsEnum.None))
			return 3;
		if (e.HasFlag (FlagsEnum.C))
			return 4;
		return 0;
	}

	public class Bar {
		bool allowLocation = true;
        Foo f = new Foo ();	
	}

	public static int test_0_regress_78990_unaligned_structs () {
		new Bar ();

		return 0;
	}

	public static unsafe int test_97_negative_index () {
		char[] arr = new char[] {'a', 'b'};
		fixed (char *p = arr) {
			char *i = p + 2;
			char a = i[-2];
			return a;
		}
	}

	/* bug #82281 */
	public static int test_0_unsigned_right_shift_imm0 () {
		uint temp = 0;
		byte[] data = new byte[256];
		for (int i = 0; i < 1; i ++)
			temp = (uint)(data[temp >> 24] | data[temp >> 0]);
		return 0;
	}

	class Foo2 {
		public virtual int foo () {
			return 0;
		}
	}

	sealed class Bar2 : Foo2 {
		public override int foo () {
			return 0;
		}
	}

	public static int test_0_abcrem_check_this_removal () {
		Bar2 b = new Bar2 ();

		// The check_this generated here by the JIT should be removed
		b.foo ();

		return 0;
	}

	static int invoke_twice (Bar2 b) {
		b.foo ();
		// The check_this generated here by the JIT should be removed
		b.foo ();

		return 0;
	}

	public static int test_0_abcrem_check_this_removal2 () {
		Bar2 b = new Bar2 ();

		invoke_twice (b);

		return 0;
	}

	/* #346563 */
	public static int test_0_array_access_64_bit () {
		int[] arr2 = new int [10];
		for (int i = 0; i < 10; ++i)
			arr2 [i] = i;
		string s = "ABCDEFGH";

		byte[] arr = new byte [4];
		arr [0] = 252;
		arr [1] = 255;
		arr [2] = 255;
		arr [3] = 255;

		int len = arr [0] | (arr [1] << 8) | (arr [2] << 16) | (arr [3] << 24);
		int len2 = - (len + 2);

		// Test array and string access with a 32 bit value whose upper 32 bits are
		// undefined
		// len2 = 3
		if (arr2 [len2] != 2)
			return 1;
		if (s [len2] != 'C')
			return 2;
		return 0;
	}

	public static float return_float () {
		return 1.4e-45f;
	}

#if !NO_BITCODE
	[Category ("!BITCODE")] // bug #59953
	public static int test_0_float_return_spill () {
		// The return value of return_float () is spilled because of the
		// boxing call
		object o = return_float ();
		float f = return_float ();
		return (float)o == f ? 0 : 1;
	}
#endif

	class R4Holder {
		public static float pi = 3.14f;

		public float float_field;
	}

	public static int test_0_ldsfld_soft_float () {
		if (R4Holder.pi == 3.14f)
			return 0;
		else
			return 1;
	}

	public static int test_0_ldfld_stfld_soft_float () {
		R4Holder h = new R4Holder ();
		h.float_field = 3.14f;

		if (h.float_field == 3.14f)
			return 0;
		else
			return 1;
	}

	class R4HolderRemote : MarshalByRefObject {
		public static float pi = 3.14f;

		public float float_field;
	}

	public static int test_0_ldfld_stfld_soft_float_remote () {
		R4HolderRemote h = new R4HolderRemote ();
		h.float_field = 3.14f;

		if (h.float_field == 3.14f)
			return 0;
		else
			return 1;
	}

	public static int test_0_locals_soft_float () {
		float f = 0.0f;
		
		f = 3.14f;

		if (f == 3.14f)
			return 0;
		else
			return 1;
	}

	struct AStruct2 {
		public int i;
		public int j;
	}

	static float pass_vtype_return_float (AStruct2 s) {
		return s.i + s.j == 6 ? 1.0f : -1.0f;
	}

	public static int test_0_vtype_arg_soft_float () {
		return pass_vtype_return_float (new AStruct2 () { i = 2, j = 4 }) > 0.0 ? 0 : 1;
	}

	static int range_check_strlen (int i, string s) {
		if (i < 0 || i > s.Length)
			return 1;
		else
			return 0;
	}
		
	public static int test_0_range_check_opt () {
		if (range_check_strlen (0, "A") != 0)
			return 1;
		if (range_check_strlen (1, "A") != 0)
			return 2;
		if (range_check_strlen (2, "A") != 1)
			return 3;
		if (range_check_strlen (-100, "A") != 1)
			return 4;
		return 0;
	}

	static int test_0_array_get_set_soft_float () {
		float[,] arr = new float [2, 2];
		arr [0, 0] = 256f;
		return arr [0, 0] == 256f ? 0 : 1;
	}

	//repro for #506915
	struct Bug506915 { public int val; }
	static int test_2_ldobj_stobj_optization ()
	{
		int i = 99;
		var a = new Bug506915 ();
		var b = new Bug506915 ();
		if (i.GetHashCode () == 99)
			i = 44;
		var array = new Bug506915 [2];
		array [0].val = 2;
		array [1] = (i == 0) ? a : array [0];
		
		return array [1].val;
	}

	/* mcs can't compile this (#646744) */
#if FALSE
	static void InitMe (out Gamma noMercyWithTheStack) {
		noMercyWithTheStack = new Gamma ();
	}

	static int FunNoInline () {
		int x = 99;
		if (x > 344 && x < 22)
			return 333;
		return x;
	}

	static float DoNothingButDontInline (float a, int b) {
		if (b > 0)
			return a;
		else if (b < 0 && b > 10)
			return 444.0f;
		return a;
	}

	/*
	 * The local register allocator emits loadr8_membase and storer8_membase
	 * to do spilling. This code is generated after mono_arch_lowering_pass so
	 * mono_arch_output_basic_block must know how to deal with big offsets.
	 * This only happens because the call in middle forces the temp for "(float)obj"
	 * to be spilled.
	*/
	public static int test_0_float_load_and_store_with_big_offset ()
	{
		object obj = 1.0f;
		Gamma noMercyWithTheStack;
		float res;

		InitMe (out noMercyWithTheStack);

		res = DoNothingButDontInline ((float)obj, FunNoInline ());

		if (!(res == 1.0f))
			return 1;
		return 0;
	}
#endif

	struct VTypePhi {
		public int i;
	}

	static int vtype_phi (VTypePhi v1, VTypePhi v2, bool first) {
		VTypePhi v = first ? v1 : v2;

		return v.i;
	}

	static int test_0_vtype_phi ()
	{
		VTypePhi v1 = new VTypePhi () { i = 1 };
		VTypePhi v2 = new VTypePhi () { i = 2 };

		if (vtype_phi (v1, v2, true) != 1)
			return 1;
		if (vtype_phi (v1, v2, false) != 2)
			return 2;

		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void UseValue (int index)
	{
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static bool IsFalse ()
	{
		return false;
	}

	static int test_0_llvm_moving_faulting_loads ()
	{
		int[] indexes = null;

		if (IsFalse ()) {
			indexes = new int[0];
		}
			
		while (IsFalse ()) {
			UseValue (indexes[0]);
			UseValue (indexes[0]);
		}

		return 0;
	}

	public static bool flag;

	class B {

		internal static B[] d;

		static B () {
			flag = true;
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static int regress_679467_inner () {
		if (flag == true)
			return 1;
		var o = B.d;
		var o2 = B.d;
		return 0;
	}

	/*
	 * FIXME: This fails with AOT #703317.
	 */
	/*
	static int test_0_multiple_cctor_calls_regress_679467 () {
		flag = false;
		return regress_679467_inner ();
	}
	*/

	static int test_0_char_ctor () {
		string s = new String (new char[] { 'A', 'B' }, 0, 1);
		return 0;
	}

	static object mInstance = null;

	[MethodImpl(MethodImplOptions.Synchronized)]
	public static object getInstance() {
		if (mInstance == null)
			mInstance = new object();
		return mInstance;
	}

	static int test_0_synchronized () {
		getInstance ();
		return 0;
	}

	struct BStruct {
		public Type t;
	}

	class Del<T> {
		public static BStruct foo () {
			return new BStruct () { t = typeof (T) };
		}
	}

	delegate BStruct ADelegate ();

	static int test_0_regress_10601 () {
		var act = (ADelegate)(Del<string>.foo);
		BStruct b = act ();
		if (b.t != typeof (string))
			return 1;
		return 0;
	}

	static int test_0_regress_11058 () {
		int foo = -252674008;
		int foo2 = (int)(foo ^ 0xF0F0F0F0); // = 28888
		var arr = new byte[foo2].Length;
		return 0;
	}

	public static void do_throw () {
		throw new Exception ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void empty () {
	}

	// #11297
	public static int test_0_llvm_inline_throw () {
		try {
			empty ();
		} catch (Exception) {
			do_throw ();
		}

		return 0;
	}

	enum ByteEnum : byte {
        Zero = 0
    }

    struct BugStruct {
        public ByteEnum f1;
        public ByteEnum f2;
        public ByteEnum f3;
        public byte f4;
        public byte f5;
        public byte f6;
        public byte f7;
    }

	public static int test_0_14217 () {
		t_14217_inner (new BugStruct ());
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void t_14217_inner (BugStruct bug) {
    }

	[StructLayout(LayoutKind.Sequential)]
	public struct EmptyStruct {
	}

	class EmptyClass {
		public static EmptyStruct s;
	}

	// #20349
	static int test_0_empty_struct_as_static () {
		var s = EmptyClass.s;
		return 0;
	}

	// #25487
	static int test_0_int_to_r4 () {
		return int_to_r4_inner (255);
	}

	static int int_to_r4_inner (int value1) {
		int sub = -value1;
		float mult = sub * 1f;
		if (mult != -255.0f)
			return 1;
		else
			return 0;
	}

	struct HFA4D {
		public double a, b, c, d;
	}

	static double arm64_hfa_on_stack_inner (double d1, double d2, double d3, double d4, double d5, double d6, double d7, double d8, HFA4D s) {
		return s.a + s.b + s.c + s.d;
	}

	static int test_0_arm64_hfa_on_stack () {
		var s = new HFA4D () { a = 1.0, b = 2.0, c = 3.0, d = 4.0 };
		var res = arm64_hfa_on_stack_inner (1, 2, 3, 4, 5, 6, 7, 8, s);
		return res == 10.0 ? 0 : 1;
	}

	class MulOvfClass {
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public unsafe void EncodeIntoBuffer(char* value, int valueLength, char* buffer, int bufferLength) {
		}
	}

	static unsafe int test_0_mul_ovf_regress_36052 () {
		var p = new MulOvfClass ();

		string typeName = typeof(int).Name;
		int bufferSize = 45;

		fixed (char* value = typeName) {
			char* buffer = stackalloc char[bufferSize];
			p.EncodeIntoBuffer(value, typeName.Length, buffer, bufferSize);
		}
		return 0;
	}

	struct Struct16 {
		public int a, b, c, d;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static int pass_struct16 (object o0, object o2, object o3, object o4, object o5, object o6, object o7, Struct16 o8) {
		// This disables LLVM
		try {
		} catch {
		}
		return o8.a;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static int pass_struct16 (object o0, object o2, object o3, object o6, object o7, Struct16 o8) {
		return pass_struct16 (o0, o2, null, o3, null, o6, o7, o8);
	}

	public static int test_42_pass_16byte_struct_split () {
		return pass_struct16 (null, null, null, null, null, new Struct16 () { a = 42 });
	}

	public interface IComparer2
	{
		Type foo<T> ();
	}

	public class AClass : IComparer2 {
		public Type foo<T> () {
			return typeof(T);
		}
	}

	public static int test_0_delegate_to_virtual_generic_on_ifaces () {
		IComparer2 c = new AClass ();

		Func<Type> f = c.foo<string>;
		return f () == typeof(string) ? 0 : 1;
	}

	public enum ByteEnum2 : byte {
		High = 142
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static int enum_arg_zero_extend (ByteEnum2 b) {
		return (int)b;
	}

	public static int test_142_byte_enum_arg_zero_extend () {
		return enum_arg_zero_extend (ByteEnum2.High);
	}

	enum Mine { One, Two }

	public static int test_0_enum_gethashcode_opt () {
		int sum = 0;
        for (int i = 0; i < 1000000; ++i)
			sum += Mine.Two.GetHashCode();

        return 0;
    }

	public static int test_0_typedref () {
		int i = 5;
		System.TypedReference r = __makeref(i);
		System.Type t = __reftype(r);

		if (t != typeof (int))
			return 1;
		int j = __refvalue(r, int);
		if (j != 5)
			return 2;

		try {
			object o = __refvalue (r, object);
		} catch (InvalidCastException) {
		}

		return 0;
	}

	enum FooEnum { Bar }
	//https://github.com/mono/mono/issues/6666
	public static int test_0_bad_unbox_nullable_of_enum () {
		try {
			var enumValue = FooEnum.Bar;
			object value = (int)enumValue;
			var res = (FooEnum?)value; // Should throw
		} catch (InvalidCastException) {
			return 0;
		}
		return 1;
	}

	//https://github.com/mono/mono/issues/6666
	public static int test_0_unbox_nullable_of_enum () {
		try {
			var enumValue = FooEnum.Bar;
			object value = (object)enumValue;
			var res = (FooEnum?)value; // Should not throw
		} catch (InvalidCastException) {
			return 1;
		}
		return 0;
	}

	static void decode (out sbyte v) {
		byte tmp = 134;
		v = (sbyte)tmp;
	}

	// gh #6414
	public static int test_0_alias_analysis_sign_extend () {
	  sbyte t;
	  decode (out t);

	  return t == -122 ? 0 : 1;
	}

	public interface IFoo
	{
	  int MyInt { get; }
	}

	public class IFooImpl : IFoo
	{
	  public int MyInt => 0;
	}

	//gh 6266
    public static int test_0_store_to_magic_iface_array ()
    {
      ICollection<IFoo> arr1 = new IFooImpl[1] { new IFooImpl() };
      ICollection<IFoo> arr2 = new IFooImpl[1] { new IFooImpl() };

      ICollection<IFoo>[] a2d = new ICollection<IFoo>[2] {
        arr1,
        arr2,
      };

	  return 0;
    }

	static volatile bool abool;

	public static unsafe int test_0_stind_r4_float32_stack_merge () {
		Single* dataPtr = stackalloc Single[4];
		abool = true;
		dataPtr[0] = abool ? 1.0f : 2.0f;
		return dataPtr [0] == 1.0f ? 0 : 1;
	}

	class AClass1 {
	}

	class BClass1 : AClass1 {
	}

	class CClass1 {
	}

	public static int test_0_array_of_magic_iface () {
		// Need to make this an object otherwise csc removes the cast
		object d = new [] { new [] { new BClass1 () } };
		if (!(d is IList<AClass1> []))
			return 1;
		if (d is IList<CClass1> [])
			return 2;
		var e2 = (IList<AClass1> []) d;
		return 0;
	}

	class SimpleContainer {
		public Simple simple1;
		public Simple simple2;

		public static Simple constsimple;

		public int SetFields () {
			constsimple.a = 0x1337;
			simple1 = simple2 = constsimple;
			return simple1.a - simple2.a;
		}
	}

	public static int test_0_dup_vtype () {
		return new SimpleContainer ().SetFields ();
	}

	public struct Vec3 {
		public int X, Y, Z;

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
			public Vec3(int x, int y, int z) {
			X = x;
			Y = y;
			Z = z;
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static int gh_11378_inner_1 (Vec3 p1, Vec3 p2) {
		p1.X -= p2.X;
		p1.Y -= p2.Y;
		p1.Z -= p2.Z;

		return (int)p2.Y;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static int gh_11378_inner_2 (Vec3 c, Vec3 pos) {
		return gh_11378_inner_1 (pos, c);
	}

	static int gh_11378_inner_3 (Vec3 c) {
		var c2 = c;
		return gh_11378_inner_2 (c, c2);
	}

	public static int test_2_gh_11378 () {
		return gh_11378_inner_3 (new Vec3(0, 2, -20));
	}

	static int variable_with_constant_address;

	public static int test_0_cfold_with_non_constant_ternary_op () {
		variable_with_constant_address = 0;
		var old = System.Threading.Interlocked.CompareExchange(ref variable_with_constant_address, 1, 0);
		return old == 0 && variable_with_constant_address == 1 ? 0 : 1;
	}
}

#if __MOBILE__
}
#endif
