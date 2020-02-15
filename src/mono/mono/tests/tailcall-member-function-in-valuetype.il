/*
To fix this, use a range check in the call and dynamically
decide tailcall or not.

Apply that to not just the `this` parameter, but all ref valuetypes.

Note that a small valuetype is no easier because it still has
reference semantics and is always passed by address.

using System;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;

unsafe public
// change to class and it works
struct
ValueType
{
	public int a;

	[MethodImpl (NoInlining)]
	static void check (long stack1, long stack2)
	{
// NOTE: This is wierd in order to be later hand edited (removed) in the IL.
		if (stack1 != 0)
			return;
		if (stack1 == stack2)
			return;
		Console.WriteLine ("tailcall failure {0} {1}", stack1, stack2);
		Environment.Exit (1);
	}

	[MethodImpl (NoInlining)]
	public void Method1 (long depth = 100, long stack = 0)
	{
		int local;
		if (depth > 0) {
			Method2 (depth - 1, (long)&local);
			return;
		}
		check ((long)&local, stack);
	}

	[MethodImpl (NoInlining)]
	void Method2 (long depth = 100, long stack = 0)
	{
		int local;
		if (depth > 0) {
			Method1 (depth - 1, (long)&local);
			return;
		}
		check ((long)&local, stack);
	}
}

class B
{
	[MethodImpl (NoInlining)]
	public static void Main (string[] args)
	{
		new ValueType ().Method1 (999999);
	}
}
*/

.assembly extern mscorlib { }

.assembly 'tailcall-member-function-in-valuetype' { }

.class public ValueType
extends [mscorlib]System.ValueType
{
.field public int32 a

.method static void check (int64 stack1, int64 stack2) noinlining
{
/*
ldarg 0
brfalse IL_0004
ret

IL_0004:
*/
ldarg 0
ldarg 1
bne.un IL_0009
ret

IL_0009:
ldstr "tailcall failure {0} {1}"
ldarg 0
box [mscorlib]System.Int64
ldarg 1
box [mscorlib]System.Int64
call void class [mscorlib]System.Console::WriteLine(string, object, object)
ldc.i4 1
tail.
call void class [mscorlib]System.Environment::Exit(int32)
ret
}

.method public instance void Method1 (int64 depth, int64 stack) noinlining
{
.locals init ( int32 V_0)

ldarg 1
ldc.i8 0
ble IL_0014

ldarg 0
ldarg 1
ldc.i8 1
sub
ldloca 0
conv.u8
tail.
call instance void valuetype ValueType::Method2(int64, int64)
ret

IL_0014:
ldloca 0
conv.u8
ldarg 2
tail.
call void valuetype ValueType::check(int64, int64)
ret
}

.method instance void Method2 (int64 depth, int64 stack) noinlining
{
.locals init ( int32 V_0)

ldarg 1
ldc.i8 0
ble IL_0014

ldarg 0
ldarg 1
ldc.i8 1
sub
ldloca 0
conv.u8
tail.
call instance void valuetype ValueType::Method1(int64, int64)
ret

IL_0014:
ldloca 0
conv.u8
ldarg 2
tail.
call void valuetype ValueType::check(int64, int64)
ret
}
}
.class B
extends [mscorlib]System.Object
{

.method public static void Main (string[] args) noinlining
{
.entrypoint
.locals init ( valuetype ValueType V_0)

ldloca 0
initobj ValueType
ldloc 0
stloc 0
ldloca 0
ldc.i8 1
ldc.i8 0
tail.
call instance void valuetype ValueType::Method1(int64, int64)
ret
}

.method public instance default void '.ctor' ()
{
ldarg 0
tail.
call instance void object::'.ctor'()
ret
}
}
