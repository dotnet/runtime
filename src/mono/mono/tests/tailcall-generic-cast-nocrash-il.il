/*
Test tailcall-generic-cast-il.il.

Author:
	Jay Krell (jaykrell@microsoft.com)

Copyright 2018 Microsoft
Licensed under the MIT license. See LICENSE file in the project root for full license information.

This derived from nearby tailcall-generic-cast-cs.cs.
*/

.assembly extern mscorlib { }

.assembly 'tailcall-generic-cast-nocrash-il' { }

.class public A { }

.class public B
{
	.method public instance void .ctor ( )
	{
		ldarg.0
		tail. call instance void [mscorlib]System.Object::.ctor ( )
		ret
	}
}

.class public C
{
	.field private static int32 i

	.method public static void
	check (int64 stack1, int64 stack2) noinlining
	{
/*
		ldsfld int32 C::i
		ldc.i4.1
		add
		stsfld int32 C::i
		//ldarg.0
		//brfalse.s IL_0010
		//ret

//IL_0010:
		ldarg.0
		ldarg.1
		beq.s IL_0037

		ldstr "{0} tailcall failure"
		ldsfld int32 C::i
		box [mscorlib]System.Int32
		call void [mscorlib]System.Console::WriteLine (string, object)
		ldc.i4.1
		tail. call void [mscorlib]System.Environment::Exit (int32)
IL_0037:
*/
		ret
	}

	.method public static !!T
	cast1<T> (object o, int32 counter, int64 stack) noinlining
	{
		.locals init (int32 V_0)

		ldarg.1
		ldc.i4.0
		ble.s IL_0012

		ldarg.0
		ldarg.1
		ldc.i4.1
		sub
		ldloca.s V_0
		conv.u
		conv.u8
		tail. call !!0 C::cast1<!!0> (object, int32, int64)
		ret

IL_0012:
		ldloca.s V_0
		conv.u
		conv.u8
		ldarg.2
		call void C::check (int64, int64)
		ldarg.0
		unbox.any !!T
		ret
	}

	.method public static class B
	cast2 (object o, int32 counter, int64 stack) noinlining
	{
		.locals init (int32 V_0)

		ldarg.1
		ldc.i4.0
		ble.s IL_0012

		ldarg.0
		ldarg.1
		ldc.i4.1
		sub
		ldloca.s V_0
		conv.u
		conv.u8
		tail. call class B C::cast2 (object, int32, int64)
		ret

IL_0012:
		ldloca.s V_0
		conv.u
		conv.u8
		ldarg.2
		call void C::check (int64, int64)
		ldarg.0
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		tail. call !!0 C::cast1<class B> (object, int32, int64)
		ret
	}

	.method public static !!T
	cast3<T> (object o, int32 counter, int64 stack) noinlining
	{
		.locals init (int32 V_0)

		ldarg.1
		ldc.i4.0
		ble.s IL_0012

		ldarg.0
		ldarg.1
		ldc.i4.1
		sub
		ldloca.s V_0
		conv.u
		conv.u8
		tail. call !!0 C::cast3<!!0> (object, int32, int64)
		ret

IL_0012:
		ldloca.s V_0
		conv.u
		conv.u8
		ldarg.2
		call void C::check (int64, int64)
		ldarg.0
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		tail. call !!0 C::cast1<!!0> (object, int32, int64)
		ret
	}

	.method public static class B[]
	cast4 (object o, int32 counter, int64 stack) noinlining
	{
		.locals init (int32 V_0)

		ldarg.1
		ldc.i4.0
		ble.s IL_0012

		ldarg.0
		ldarg.1
		ldc.i4.1
		sub
		ldloca.s V_0
		conv.u
		conv.u8
		tail. call class B[] C::cast4 (object, int32, int64)
		ret

IL_0012:
		ldloca.s V_0
		conv.u
		conv.u8
		ldarg.2
		call void C::check (int64, int64)
		ldarg.0
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		tail. call !!0 C::cast1<class B[]> (object, int32, int64)
		ret
	}

	.method public static !!T[]
	cast5<T> (object o, int32 counter, int64 stack) noinlining
	{
		.locals init (int32 V_0)

		ldarg.1
		ldc.i4.0
		ble.s IL_0012

		ldarg.0
		ldarg.1
		ldc.i4.1
		sub
		ldloca.s V_0
		conv.u
		conv.u8
		tail. call !!0[] C::cast5<!!0> (object, int32, int64)
		ret

IL_0012:
		ldloca.s V_0
		conv.u
		conv.u8
		ldarg.2
		call void C::check (int64, int64)
		ldarg.0
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		tail. call !!0 C::cast1<!!0[]> (object, int32, int64)
		ret
	}
}

.class public D`1<T1>
{
	.method public static void
	check (int64 stack1, int64 stack2) noinlining
	{
		ldarg.0
		ldarg.1
		tail. call void C::check (int64, int64)
		ret
	}

	.method public static !!T
	cast1<T> (object o, int32 counter, int64 stack) noinlining
	{
		.locals init (int32 V_0)

		ldarg.1
		ldc.i4.0
		ble.s IL_0012

		ldarg.0
		ldarg.1
		ldc.i4.1
		sub
		ldloca.s V_0
		conv.u
		conv.u8
		tail. call !!0 class D`1<!T1>::cast1<!!0> (object, int32, int64)
		ret

IL_0012:
		ldloca.s V_0
		conv.u
		conv.u8
		ldarg.2
		call void class D`1<!T1>::check (int64, int64)
		ldarg.0
		unbox.any !!T
		ret
	}

	.method public static class B
	cast2 (object o, int32 counter, int64 stack) noinlining
	{
		.locals init (int32 V_0)

		ldarg.1
		ldc.i4.0
		ble.s IL_0012

		ldarg.0
		ldarg.1
		ldc.i4.1
		sub
		ldloca.s V_0
		conv.u
		conv.u8
		tail. call class B class D`1<!T1>::cast2 (object, int32, int64)
		ret

IL_0012:
		ldloca.s V_0
		conv.u
		conv.u8
		ldarg.2
		call void class D`1<!T1>::check (int64, int64)
		ldarg.0
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		tail. call !!0 class D`1<!T1>::cast1<class B> (object, int32, int64)
		ret
	}

	.method public static !!T
	cast3<T> (object o, int32 counter, int64 stack) noinlining
	{
		.locals init (int32 V_0)

		ldarg.1
		ldc.i4.0
		ble.s IL_0012

		ldarg.0
		ldarg.1
		ldc.i4.1
		sub
		ldloca.s V_0
		conv.u
		conv.u8
		tail. call !!0 class D`1<!T1>::cast3<!!0> (object, int32, int64)
		ret

IL_0012:
		ldloca.s V_0
		conv.u
		conv.u8
		ldarg.2
		call void class D`1<!T1>::check (int64, int64)
		ldarg.0
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		tail. call !!0 class D`1<!T1>::cast1<!!0> (object, int32, int64)
		ret
	}

	.method public static class B[]
	cast4 (object o, int32 counter, int64 stack) noinlining
	{
		.locals init (int32 V_0)

		ldarg.1
		ldc.i4.0
		ble.s IL_0012

		ldarg.0
		ldarg.1
		ldc.i4.1
		sub
		ldloca.s V_0
		conv.u
		conv.u8
		tail. call class B[] class D`1<!T1>::cast4 (object, int32, int64)
		ret

IL_0012:
		ldloca.s V_0
		conv.u
		conv.u8
		ldarg.2
		call void class D`1<!T1>::check (int64, int64)
		ldarg.0
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		tail. call !!0 class D`1<!T1>::cast1<class B[]> (object, int32, int64)
		ret
	}

	.method public static !!T[]
	cast5<T> (object o, int32 counter, int64 stack) noinlining
	{
		.locals init (int32 V_0)

		ldarg.1
		ldc.i4.0
		ble.s IL_0012

		ldarg.0
		ldarg.1
		ldc.i4.1
		sub
		ldloca.s V_0
		conv.u
		conv.u8
		tail. call !!0[] class D`1<!T1>::cast5<!!0> (object, int32, int64)
		ret

IL_0012:
		ldloca.s V_0
		conv.u
		conv.u8
		ldarg.2
		call void class D`1<!T1>::check (int64, int64)
		ldarg.0
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		tail. call !!0 class D`1<!T1>::cast1<!!0[]> (object, int32, int64)
		ret
	}

	.method public static !T1
	cast6 (object o, int32 counter, int64 stack) noinlining
	{
		.locals init (int32 V_0)

		ldarg.1
		ldc.i4.0
		ble.s IL_0012

		ldarg.0
		ldarg.1
		ldc.i4.1
		sub
		ldloca.s V_0
		conv.u
		conv.u8
		tail. call !0 class D`1<!T1>::cast6 (object, int32, int64)
		ret

IL_0012:
		ldloca.s V_0
		conv.u
		conv.u8
		ldarg.2
		call void class D`1<!T1>::check (int64, int64)
		ldarg.0
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		tail. call !!0 class D`1<!T1>::cast1<!0> (object, int32, int64)
		ret
	}

	.method public static !T1
	cast7<T> (object o, int32 counter, int64 stack) noinlining
	{
		.locals init (int32 V_0)

		ldarg.1
		ldc.i4.0
		ble.s IL_0012

		ldarg.0
		ldarg.1
		ldc.i4.1
		sub
		ldloca.s V_0
		conv.u
		conv.u8
		tail. call !0 class D`1<!T1>::cast7<!!0> (object, int32, int64)
		ret

IL_0012:
		ldloca.s V_0
		conv.u
		conv.u8
		ldarg.2
		call void class D`1<!T1>::check (int64, int64)
		ldarg.0
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		tail. call !!0 class D`1<!T1>::cast1<!0> (object, int32, int64)
		ret
	}

	.method public static !T1[]
	cast8 (object o, int32 counter, int64 stack) noinlining
	{
		.locals init (int32 V_0)

		ldarg.1
		ldc.i4.0
		ble.s IL_0012

		ldarg.0
		ldarg.1
		ldc.i4.1
		sub
		ldloca.s V_0
		conv.u
		conv.u8
		tail. call !0[] class D`1<!T1>::cast8 (object, int32, int64)
		ret

IL_0012:
		ldloca.s V_0
		conv.u
		conv.u8
		ldarg.2
		call void class D`1<!T1>::check (int64, int64)
		ldarg.0
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		tail. call !!0 class D`1<!T1>::cast3<!0[]> (object, int32, int64)
		ret
	}

	.method public static !T1[]
	cast9<T> (object o, int32 counter, int64 stack) noinlining
	{
		.locals init (int32 V_0)

		ldarg.1
		ldc.i4.0
		ble.s IL_0012

		ldarg.0
		ldarg.1
		ldc.i4.1
		sub
		ldloca.s V_0
		conv.u
		conv.u8
		tail. call !0[] class D`1<!T1>::cast9<!!0> (object, int32, int64)
		ret

IL_0012:
		ldloca.s V_0
		conv.u
		conv.u8
		ldarg.2
		call void class D`1<!T1>::check (int64, int64)
		ldarg.0
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		tail. call !!0 class D`1<!T1>::cast3<!0[]> (object, int32, int64)
		ret
	}

	.method public
	instance void .ctor ( )
	{
		ldarg.0
		tail. call instance void [mscorlib]System.Object::.ctor ( )
		ret
	}
}

.class public E
{
	.field private static int32 i

	.method private static void
	print (object o) noinlining
	{
		ldsfld int32 E::i
		ldc.i4.1
		add
		stsfld int32 E::i
		ret
	}

	.method public static void
	Main (string[] args) noinlining
	{
		.entrypoint

		newobj instance void B::.ctor ( )
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		call class B C::cast2 (object, int32, int64)
		call void E::print (object)

		newobj instance void B::.ctor ( )
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		call !!0 C::cast3<class B> (object, int32, int64)
		call void E::print (object)

		ldc.i4.1
		newarr B
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		call !!0 C::cast3<class B[]> (object, int32, int64)
		call void E::print (object)

		ldc.i4.1
		newarr B
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		call class B[] C::cast4 (object, int32, int64)
		call void E::print (object)

		ldc.i4.1
		newarr B
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		call !!0[] C::cast5<class B> (object, int32, int64)
		call void E::print (object)

		newobj instance void B::.ctor ( )
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		call class B class D`1<class A>::cast2 (object, int32, int64)
		call void E::print (object)

		newobj instance void B::.ctor ( )
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		call !!0 class D`1<class A>::cast3<class B> (object, int32, int64)
		call void E::print (object)

		ldc.i4.1
		newarr B
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		call !!0 class D`1<class A>::cast3<class B[]> (object, int32, int64)
		call void E::print (object)

		ldc.i4.1
		newarr B
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		call class B[] class D`1<class A>::cast4 (object, int32, int64)
		call void E::print (object)

		ldc.i4.1
		newarr B
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		call !!0[] class D`1<class A>::cast5<class B> (object, int32, int64)
		call void E::print (object)

		newobj instance void B::.ctor ( )
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		call !0 class D`1<class B>::cast6 (object, int32, int64)
		call void E::print (object)

		newobj instance void B::.ctor ( )
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		call !0 class D`1<class B>::cast7<class A> (object, int32, int64)
		call void E::print (object)

		ldc.i4.1
		newarr B
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		call !0 class D`1<class B[]>::cast7<class A[]> (object, int32, int64)
		call void E::print (object)

		ldc.i4.1
		newarr B
		ldc.i4.s 100
		ldc.i4.0
		conv.i8
		call !0[] class D`1<class B>::cast8 (object, int32, int64)
		call void E::print (object)

		ldc.i4.1
		newarr B
		ldc.i4.s 100
		ldc.i4.0
		conv.i8

		call !0[] class D`1<class B>::cast9<class A> (object, int32, int64)
		tail. call void E::print (object)

		ret
	}
}
