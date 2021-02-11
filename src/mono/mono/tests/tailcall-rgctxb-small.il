/*
Author:
    Jay Krell (jaykrell@microsoft.com)

Copyright 2018 Microsoft
Licensed under the MIT license. See LICENSE file in the project root for full license information.

http://www.mono-project.com/docs/advanced/runtime/docs/generic-sharing/
1. Non-generic non-static methods of reference types have access to the
   RGCTX via the “this” argument (this->vtable->rgctx).
2. a. Non-generic static methods of reference types and b. non-generic methods
   of value types need to be passed a pointer to the caller’s class’s VTable in the MONO_ARCH_RGCTX_REG register.
3. Generic methods need to be passed a pointer to the MRGCTX in the MONO_ARCH_RGCTX_REG register

Test case 2b, with a small type.

using System;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;

unsafe struct A<T>
{
    void f3 ()
    {
        a = 1;
    }
    public int a;

    [MethodImpl (NoInlining)]
    static int check (long stack1, long stack2)
    {
	return (stack1 == stack2) ? 0 : 1;
    }

    [MethodImpl (NoInlining)]
    public int Method1 (int counter, long initial_stack, long current_stack)
    {
	int local;
	object a = new T[101];
	if (counter > 0)
	    return Method2 (counter - 1, initial_stack, (long)&local);
	return check ((long)&local, current_stack);
    }

    [MethodImpl (NoInlining)]
    public int Method2 (int counter, long initial_stack, long current_stack)
    {
	int local;
	object a = new T[101];
	if (counter > 0)
	    return Method1 (counter - 1, initial_stack, (long)&local);
	return check ((long)&local, current_stack);
    }
}

unsafe class B
{
    [MethodImpl (NoInlining)]
    public static void Main()
    {
	int stack;
	Environment.Exit(new A<int>().Method1 (100, (long)&stack, 0));
    }
}
*/

.assembly extern mscorlib { }

.assembly 'tailcall-rgctxb' { }

.class A`1<T>

// Remove this change the test a lot and make it pass.
// It is ok in this case because the ValueType is small.
extends [mscorlib]System.ValueType

{
.method static int32 check (int64 stack1, int64 stack2) noinlining
{
	ldarg.0
	ldarg.1
	beq.s IL_0006

	ldc.i4.1
	ret

IL_0006:
	ldc.i4.0
	ret
}

.method instance int32 Method1 (int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
	.locals init (int32 V_0)

	ldarg.1
	ldc.i4.0
	ble.s IL_001b

	ldarg.0
	ldarg.1
	ldc.i4.1
	sub
	ldarg.2
	ldloca.s V_0
	conv.u
	conv.u8
	tail. call instance int32 valuetype A`1<!T>::Method2 (int32, int64, int64)
	ret

IL_001b:
	ldloca.s V_0
	conv.u
	conv.u8
	ldarg.3
	tail. call int32 valuetype A`1<!T>::check (int64, int64)
	ret
}

.method instance int32 Method2 (int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
	.locals init (int32 V_0)
	ldarg.1
	ldc.i4.0
	ble.s IL_001b

	ldarg.0
	ldarg.1
	ldc.i4.1
	sub
	ldarg.2
	ldloca.s V_0
	conv.u
	conv.u8
	tail. call instance int32 valuetype A`1<!T>::Method1 (int32, int64, int64)
	ret

IL_001b:
	ldloca.s V_0
	conv.u
	conv.u8
	ldarg.3
	tail. call int32 valuetype A`1<!T>::check (int64, int64)
	ret
}
}

.class B extends [mscorlib]System.Object
{

.method static int32 Main() noinlining
{
	.entrypoint
	.locals init (int32 V_0, valuetype A`1<int32> V_1)
	ldloca.s V_1
	initobj valuetype A`1<int32>
	ldloc.1
	stloc.1
	ldloca.s V_1
	ldc.i4.s 100
	ldloca.s V_0
	conv.u
	conv.u8
	ldc.i4.0
	conv.i8
	tail. call instance int32 valuetype A`1<int32>::Method1 (int32, int64, int64)
	ret
}

.method instance void .ctor()
{
	ldarg.0
	tail. call instance void [mscorlib]System.Object::.ctor ()
	ret
}
}
