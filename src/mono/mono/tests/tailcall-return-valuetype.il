/*
using System;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;

unsafe public struct ValueType
{
	public long a0, a1, a2, a3, a4, a5, a6, a7, a8, a9;

	public ValueType Init ()
	{
		a0 = 0;
		a1 = 1;
		a2 = 2;
		a3 = 3;
		a4 = 4;
		a5 = 5;
		a6 = 6;
		a7 = 7;
		a8 = 8;
		a9 = 9;
		return this;
	}

	public static ValueType New ()
	{
		return new ValueType ().Init ();
	}

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
	public static ValueType Method1 (long depth = 999999, long stack = 0)
	{
		long local;
		if (depth > 0)
			return Method2 (depth - 1, (long)&local);
		check ((long)&local, stack);
		return New ();
	}

	[MethodImpl (NoInlining)]
	static ValueType Method2 (long depth = 999999, long stack = 0)
	{
		long local;
		if (depth > 0)
			return Method1 (depth - 1, (long)&local);
		check ((long)&local, stack);
		return New ();
	}
}

class B
{
	[MethodImpl (NoInlining)]
	public static void Main (string[] args)
	{
		Console.WriteLine(ValueType.Method1 (2));
	}
}
*/

.assembly extern mscorlib { }

.assembly 'tailcall-return-valuetype' { }

.class public ValueType
extends [mscorlib]System.ValueType
{
.field public int64 a0
.field public int64 a1
.field public int64 a2
.field public int64 a3
.field public int64 a4
.field public int64 a5
.field public int64 a6
.field public int64 a7
.field public int64 a8
.field public int64 a9

.method static valuetype ValueType check (int64 stack1, int64 stack2) noinlining
{
	.locals init ( int64 V_0, valuetype ValueType V_1)

/* in the C# but deliberately removed here
	ldarg 0
	brfalse IL_0004
	ret

IL_0004:
*/
	ldarg 0
	ldarg 1
	beq success
	//br success // to see the resulting value

IL_0009:
	ldstr "tailcall failure {0} {1}"
	ldarg 0
	box [mscorlib]System.Int64
	ldarg 1
	box [mscorlib]System.Int64
	call void class [mscorlib]System.Console::WriteLine (string, object, object)

// exit early, or let Main check for the expected values
	//ldc.i4 1
	//tail.call void class [mscorlib]System.Environment::Exit (int32)

// fail with all zeros (.locals init)
	ldloca 1
	ldobj ValueType
	ret

success: // with slightly nontrivial values a0 == 0, a1 == 1, etc.
	ldloca 1
	ldc.i8 0
	stfld int64 ValueType::a0
	ldloca 1
	ldc.i8 1
	stfld int64 ValueType::a1
	ldloca 1
	ldc.i8 2
	stfld int64 ValueType::a2
	ldloca 1
	ldc.i8 3
	stfld int64 ValueType::a3
	ldloca 1
	ldc.i8 4
	stfld int64 ValueType::a4
	ldloca 1
	ldc.i8 5
	stfld int64 ValueType::a5
	ldloca 1
	ldc.i8 6
	stfld int64 ValueType::a6
	ldloca 1
	ldc.i8 7
	stfld int64 ValueType::a7
	ldloca 1
	ldc.i8 8
	stfld int64 ValueType::a8
	ldloca 1
	ldc.i8 9
	stfld int64 ValueType::a9

	ldloca 1
	ldobj ValueType

	ret
}

.method public static valuetype ValueType Method1 (int64 depth, int64 stack) noinlining
{
	.locals ( int64 V_0)

	ldarg 0
	ldc.i8 0
	ble IL_0013

	ldarg 0
	ldc.i8 1
	sub
	ldloca 0
	conv.u8
tail.
	call valuetype ValueType valuetype ValueType::Method2 (int64, int64)
	ret

IL_0013:
	ldloca 0
	conv.u8
	ldarg 1
tail.
	call valuetype ValueType valuetype ValueType::check (int64, int64)
	ret
}

.method static valuetype ValueType Method2 (int64 depth, int64 stack) noinlining
{
	.locals ( int64 V_0 )

	ldarg 0
	ldc.i8 0
	ble IL_0013

	ldarg 0
	ldc.i8 1
	sub
	ldloca 0
	conv.u8
tail.
	call valuetype ValueType valuetype ValueType::Method1 (int64, int64)
	ret

IL_0013:
	ldloca 0
	conv.u8
	ldarg 1
tail.
	call valuetype ValueType valuetype ValueType::check (int64, int64)
	ret
}
}

.class B
{

.method public static void Main (string[] args) noinlining
{
	.maxstack 99
	.entrypoint
	.locals init (valuetype ValueType V_1)

	ldc.i8 1
	ldc.i8 0
	call valuetype ValueType valuetype ValueType::Method1 (int64, int64)
	stloc 0

// compare elements to expected a0 == 0, a1 == a1, etc.

	ldloc 0
	ldfld int64 ValueType::a0
	ldc.i8 0
	bne.un failure

	ldloc 0
	ldfld int64 ValueType::a1
	ldc.i8 1
	bne.un failure

	ldloc 0
	ldfld int64 ValueType::a2
	ldc.i8 2
	bne.un failure

	ldloc 0
	ldfld int64 ValueType::a3
	ldc.i8 3
	bne.un failure

	ldloc 0
	ldfld int64 ValueType::a4
	ldc.i8 4
	bne.un failure

	ldloc 0
	ldfld int64 ValueType::a5
	ldc.i8 5
	bne.un failure

	ldloc 0
	ldfld int64 ValueType::a6
	ldc.i8 6
	bne.un failure

	ldloc 0
	ldfld int64 ValueType::a7
	ldc.i8 7
	bne.un failure

	ldloc 0
	ldfld int64 ValueType::a8
	ldc.i8 8
	bne.un failure

	ldloc 0
	ldfld int64 ValueType::a9
	ldc.i8 9
	bne.un failure

	ldc.i4 0
	tail. call void class [mscorlib]System.Environment::Exit (int32)
	ret

failure:

	ldstr "failed: {0}{1}{2}{3}{4}{5}{6}{7}{8}{9}"

	ldc.i4 10
	newarr [mscorlib]System.Object

	dup
	ldc.i4 0
	ldloc 0
	ldfld int64 ValueType::a0
	box [mscorlib]System.Int64
	stelem.ref

	dup
	ldc.i4 1
	ldloc 0
	ldfld int64 ValueType::a1
	box [mscorlib]System.Int64
	stelem.ref

	dup
	ldc.i4 2
	ldloc 0
	ldfld int64 ValueType::a2
	box [mscorlib]System.Int64
	stelem.ref

	dup
	ldc.i4 3
	ldloc 0
	ldfld int64 ValueType::a3
	box [mscorlib]System.Int64
	stelem.ref

	dup
	ldc.i4 4
	ldloc 0
	ldfld int64 ValueType::a4
	box [mscorlib]System.Int64
	stelem.ref

	dup
	ldc.i4 5
	ldloc 0
	ldfld int64 ValueType::a5
	box [mscorlib]System.Int64
	stelem.ref

	dup
	ldc.i4 6
	ldloc 0
	ldfld int64 ValueType::a6
	box [mscorlib]System.Int64
	stelem.ref

	dup
	ldc.i4 7
	ldloc 0
	ldfld int64 ValueType::a7
	box [mscorlib]System.Int64
	stelem.ref

	dup
	ldc.i4 8
	ldloc 0
	ldfld int64 ValueType::a8
	box [mscorlib]System.Int64
	stelem.ref

	dup
	ldc.i4 9
	ldloc 0
	ldfld int64 ValueType::a9
	box [mscorlib]System.Int64
	stelem.ref

	call void class [mscorlib]System.Console::WriteLine(string, object[])

	ldc.i4 1
	tail. call void class [mscorlib]System.Environment::Exit (int32)
	ret
}

}
