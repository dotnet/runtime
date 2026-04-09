/*
Based on this F# and C#, but with genericness reduced.
This ends up using calli/call_reg.
A more faithful rendition does not yet work, probably due to gsharedvt wrapping.

genericness reduced: the parameter and return

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

.assembly '4' { }

.class A`1<T1>
{

.method static void check (int64 stack1, int64 stack2) noinlining
{
//ldarg.0
//brfalse.s IL_0004
//ret

IL_0004:
ldarg.0
ldarg.1
bne.un.s IL_0009
ret

IL_0009:
ldstr "tailcall failure"
call void class [mscorlib]System.Console::WriteLine(string)
ldc.i4.1
tail.
call void class [mscorlib]System.Environment::Exit(int32)
ret
}

.method public static void
Method1 (int64 depth, int64 stack) noinlining
{
.locals init ( int64 V_0)

ldarg.0
ldc.i8 0
ble.s IL_0013

ldarg.0
ldc.i8 1
sub
ldloca.s 0
conv.u
conv.u8
tail.
call void class A`1<!T1>::Method2 (int64, int64)
ret

IL_0013:
ldloca.s 0
conv.u
conv.u8
ldarg.1
tail.
call void class A`1<!T1>::check(int64, int64)
ret
}

.method public static void
Method2 (int64 depth, int64 stack) noinlining
{
.locals init ( int64 V_0)

ldarg.0
ldc.i8 0
ble.s IL_0013

ldarg.0
ldc.i8 1
sub
ldloca.s 0
conv.u
conv.u8
tail.
call void class A`1<!T1>::Method1 (int64, int64)
ret

IL_0013:
ldloca.s 0
conv.u
conv.u8
ldarg.1
tail.
call void class A`1<!T1>::check(int64, int64)
ret
}

}

.class B
{

.method public static void Main (string[] args) noinlining
{
.entrypoint
ldc.i8 9
ldc.i8 0
conv.i8
tail.
call void class A`1<object>::Method1(int64, int64)
ret
}

}
