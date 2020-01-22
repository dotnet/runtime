/*
using System;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;

struct ValueType
{
	// This should be large enough to never be passed by value in registers.
	public long a0;
	public long a1;
	public long a2;
	public long a3;
	public long a4;
	public long a5;
	public long a6;
	public long a7;
	public long a8;
	public long a9;

	[MethodImpl (NoInlining)]
	public ValueType init ()
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
}

class A
{
	[MethodImpl (NoInlining)]
	static bool f0 (ValueType value)
	{
		return value.Equals (new ValueType ().init ());
	}

	[MethodImpl (NoInlining)]
	static bool f1 (ValueType value)
	{
		return f0 (value); // tailcall
	}

	[MethodImpl (NoInlining)]
	static void Main (string [ ] args)
	{
		Environment.Exit (f1 (new ValueType ().init ()) ? 0 : 1);
	}
}
*/
.assembly 'tailcall-valuetype-parameter' { }

.class ValueType extends [mscorlib]System.ValueType
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

.method public valuetype ValueType 'init' () noinlining
{
	ldarg 0
	ldc.i8 0
	stfld int64 ValueType::a0

	ldarg.0
	ldc.i8 1
	stfld int64 ValueType::a1

	ldarg.0
	ldc.i8 2
	stfld int64 ValueType::a2

	ldarg.0
	ldc.i8 3
	stfld int64 ValueType::a3

	ldarg 0
	ldc.i8 4
	stfld int64 ValueType::a4

	ldarg 0
	ldc.i8 5
	stfld int64 ValueType::a5

	ldarg 0
	ldc.i8 6
	stfld int64 ValueType::a6

	ldarg 0
	ldc.i8 7
	stfld int64 ValueType::a7

	ldarg 0
	ldc.i8 8
	stfld int64 ValueType::a8

	ldarg 0
	ldc.i8 9
	stfld int64 ValueType::a9

	ldarg 0
	ldobj ValueType
	ret
}
}

.class A
{

.method static bool f0 (valuetype ValueType 'value') noinlining
{
	.locals init ( valuetype ValueType V_0)

	ldarga 0
	ldloca 0
	initobj ValueType

	ldloc 0
	stloc 0
	ldloca 0
	call instance valuetype ValueType valuetype ValueType::'init' ()
	box ValueType
	constrained. ValueType
	callvirt instance bool object::Equals (object)
	ret
}

.method static bool f1 (valuetype ValueType 'value') noinlining
{
	ldarg 0
	tail.
	call bool class A::f0 (valuetype ValueType)
	ret
}

.method static int32 Main ( ) noinlining
{
	.entrypoint
	.locals init ( valuetype ValueType V_0)

	ldloca 0
	initobj ValueType
	ldloca 0
	call instance valuetype ValueType valuetype ValueType::'init' ()
	call bool class A::f1 (valuetype ValueType)
	brtrue success
	ldc.i4 1
	ret

success:
	ldc.i4.0
	ret
}

}
