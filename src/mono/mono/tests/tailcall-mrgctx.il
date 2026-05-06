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

Test case 3.

using System;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;

unsafe class A
{
    [MethodImpl (NoInlining)]
    static int check (long stack1, long stack2)
    {
	return (stack1 == stack2) ? 0 : 1;
    }

    [MethodImpl (NoInlining)]
    public int f1<T> (int counter, long initial_stack, long current_stack)
    {
	int local;
	if (counter > 0)
	    return f2<T> (counter - 1, initial_stack, (long)&local);
	return check ((long)&local, current_stack);
    }

    [MethodImpl (NoInlining)]
    public int f2<T> (int counter, long initial_stack, long current_stack)
    {
	int local;
	if (counter > 0)
	    return f1<T> (counter - 1, initial_stack, (long)&local);
	return check ((long)&local, current_stack);
    }
}

unsafe class B
{
    [MethodImpl (NoInlining)]
    public static void Main()
    {
	int stack;
	Environment.Exit(new A().f1<object> (100, (long)&stack, 0));
    }
}
*/

.assembly extern mscorlib { }

.assembly 'tailcall-mrgctx' { }

.class A
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

.method instance int32 f1<T> (int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
	.locals init (int32 V_0)
	ldarg.1		// counter
	ldc.i4.0	// 0
	ble.s IL_0013	// if (counter <= 0) goto 13.

	ldarg.0     	// this
	ldarg.1		// counter
	ldc.i4.1	// 1
	sub		// counter - 1
	ldarg.2		// initial_stack
	ldloca.s V_0	// &local
	conv.u		// (long)&local
	conv.u8		// (long)&local
	tail. call instance int32 A::f2<!!0> (int32, int64, int64)
	ret

IL_0013:
	ldloca.s V_0	// &local
	conv.u		// (long)&local
	conv.u8		// (long)&local
	ldarg.3		// current_stack
	tail. call int32 A::check (int64, int64)
	ret
}

.method instance int32  f2<T> (int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
	.locals init (int32 V_0)
	ldarg.1
	ldc.i4.0
	ble.s IL_0013

	ldarg.0
	ldarg.1
	ldc.i4.1
	sub
	ldarg.2
	ldloca.s V_0
	conv.u
	conv.u8
	tail. call instance int32 A::f1<!!0> (int32, int64, int64)
	ret

IL_0013:
	ldloca.s V_0
	conv.u
	conv.u8
	ldarg.3
	tail. call int32 A::check (int64, int64)
	ret
}

.method instance void .ctor ()
{
	ldarg.0
	tail. call instance void [mscorlib]System.Object::.ctor ()
	ret
}

}

.class B
{
.method static int32 Main () noinlining
{
	.entrypoint
	.locals init (int32 V_0)
	newobj instance void A::.ctor ()
	ldc.i4.s 100
	ldloca.s V_0
	conv.u
	conv.u8
	ldc.i4.0
	conv.i8
	tail. call instance int32 A::f1<object> (int32, int64, int64)
	ret
}

.method instance void .ctor ()
{
	ldarg.0
	tail. call instance void [mscorlib]System.Object::.ctor ()
	ret
}
}
