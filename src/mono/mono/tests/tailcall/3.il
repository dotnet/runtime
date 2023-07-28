/*

https://github.com/mono/mono/issues/8273

This test is really slow, so its iteration count is lowered.
It takes under a second for JIT and AOT, but 9 minutes for FullAOT, if you grow the iteration count.
Also string is slow but int32 is fast.
Search for "999".

Based on this F# and C#.

type TailCallLoopGenericClassAndMethod<'T1>(resultA: 'T1) =
    member this.Method1<'T2>(x:int, resultB: 'T2) =
        if x = 0 then (resultA, resultB) else this.Method2 (x - 1, resultB)
    member this.Method2<'T2>(x:int, resultB: 'T2) =
        if x = 0 then (resultA, resultB) else this.Method1 (x  - 1, resultB)

using System;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;

unsafe class A<T1>
{
	T1 resultA;

	[MethodImpl (NoInlining)]
	static void check (long stack1, long stack2)
	{
// NOTE: This is weird in order to feed into the edited IL.
		if (stack1 != 0)
			return;
		if (stack1 == stack2)
			return;
		Console.WriteLine ("tailcall failure {0} {1}", stack1, stack2);
		Environment.Exit (1);
	}

	[MethodImpl (NoInlining)]
	public A(T1 a)
	{
		resultA = a;
	}

	[MethodImpl (NoInlining)]
	public System.Tuple<T1, T2> Method1<T2> (T2 resultB, long depth = 100, long stack = 0)
	{
		int local;
		if (depth > 0)
			return Method2 (resultB, depth - 1, (long)&local);
		check ((long)&local, stack);
		return new System.Tuple<T1, T2> (resultA, resultB);
	}

	[MethodImpl (NoInlining)]
	System.Tuple<T1, T2> Method2<T2> (T2 resultB, long depth = 100, long stack = 0)
	{
		int local;
		if (depth > 0)
			return Method1 (resultB, depth - 1, (long)&local);
		check ((long)&local, stack);
		return new System.Tuple<T1, T2> (resultA, resultB);
	}
}

class B
{
	[MethodImpl (NoInlining)]
	public static void Main (string[] args)
	{
		new A<int> (1).Method1 (2);
	}
}
*/

.assembly extern mscorlib { }

.assembly '1' { }

.class A`1<T1>
{
.field assembly !T1 resultA

.method static void check (int64 stack1, int64 stack2) noinlining
{
//ldarg.0
//brfalse.s IL_0004
//ret
//IL_0004:

ldarg.0
ldarg.1
bne.un.s IL_0009
ret

IL_0009:
ldstr "tailcall failure {0:X}"
ldarg.0
ldarg.1
sub
box int64
call void class [mscorlib]System.Console::WriteLine(string, object)
ldc.i4.1
tail.
call void class [mscorlib]System.Environment::Exit(int32)
ret
}

.method instance void '.ctor' (!T1 a) noinlining
{
ldarg.0
call instance void object::'.ctor'()
ldarg.0
ldarg.1
stfld !0 class A`1<!0>::resultA
ret
}

.method public hidebysig instance class [mscorlib]System.Tuple`2<!T1, !!T2> Method1<T2> (!!T2 resultB, int64 depth, int64 stack) noinlining
{
.locals init ( int64 a, int64 b, int64 c, int64 d, int64 e)

ldarg.2
ldc.i4.0
conv.i8
ble.s IL_0015

ldarg.0
ldarg.1
ldarg.2
ldc.i4.1
conv.i8
sub
ldloca.s 0
conv.u
conv.u8
tail.
call instance class [mscorlib]System.Tuple`2<!0,!!0> class A`1<!T1>::Method2<!!0> (!!0, int64, int64)
ret

IL_0015:
ldloca.s 0
conv.u
conv.u8
ldarg.3
call void class A`1<!T1>::check(int64, int64)
ldarg.0
ldfld !0 class A`1<!0>::resultA
ldarg.1
newobj instance void class [mscorlib]System.Tuple`2<!T1, !!T2>::'.ctor'(!0, !1)
ret
}

.method public hidebysig instance class [mscorlib]System.Tuple`2<!T1, !!T2> Method2<T2> (!!T2 resultB, int64 depth, int64 stack) noinlining
{
.locals ( int64 a )

ldarg.2
ldc.i8 0
ble.s IL_0015

ldarg.0
ldarg.1
ldarg.2
ldc.i8 1
sub
ldloca.s 0
conv.u
conv.u8

tail.
call instance class [mscorlib]System.Tuple`2<!0,!!0> class A`1<!T1>::Method1<!!0> (!!0, int64, int64)
ret

IL_0015:
ldloca.s 0
conv.u
conv.u8
ldarg.3
call void class A`1<!T1>::check (int64, int64)

ldarg.0
ldfld !0 class A`1<!0>::resultA
ldarg.1
newobj instance void class [mscorlib]System.Tuple`2<!T1, !!T2>::'.ctor'(!0, !1)
ret
}
}

.class B
{

.method static void Main (string[] args) noinlining
{
.entrypoint

// Using string instead of int32 here makes this test really slow,'
// so use a low iteration count -- anything small is ok to test tailcall.

ldstr "a"
//ldc.i4 1
newobj instance void class A`1<string>::'.ctor'(!0)
ldstr "b"
//ldc.i4 2
//ldc.i8 99999999
ldc.i8 9
ldc.i8 0

call instance class [mscorlib]System.Tuple`2<!0,!!0> class A`1<string>::Method1<string> (!!0, int64, int64)
//call void class [mscorlib]System.Console::WriteLine (object)
pop
ret
}

.method void '.ctor' ()
{
ldarg.0
tail.
call instance void object::'.ctor'()
ret
}
}
