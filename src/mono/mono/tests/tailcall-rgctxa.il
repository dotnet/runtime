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

Test case 2a.

using System;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;

unsafe class A<T>
{
    [MethodImpl (NoInlining)]
    static int check (long stack1, long stack2)
    {
	return (stack1 == stack2) ? 0 : 1;
    }

    [MethodImpl (NoInlining)]
    public static int f1 (int counter, long initial_stack, long current_stack)
    {
	int local;
	if (counter > 0)
	    return f2 (counter - 1, initial_stack, (long)&local);
	return check ((long)&local, current_stack);
    }

    [MethodImpl (NoInlining)]
    public static int f2 (int counter, long initial_stack, long current_stack)
    {
	int local;
	if (counter > 0)
	    return f1 (counter - 1, initial_stack, (long)&local);
	return check ((long)&local, current_stack);
    }
}

unsafe class B
{
    [MethodImpl (NoInlining)]
    public static void Main()
    {
	int stack;
	Environment.Exit(A<object>.f1 (100, (long)&stack, 0));
    }
}
*/

.assembly extern mscorlib { }

.assembly 'tailcall-rgctxa' { }

.class A`1<T>
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

.method static int32 f1 (int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
	.locals init (int32 V_0)
	ldarg.0
	ldc.i4.0
	ble.s IL_0012
	ldarg.0
	ldc.i4.1
	sub
	ldarg.1
	ldloca.s V_0
	conv.u
	conv.u8
	tail. call int32 class A`1<!T>::f2 (int32, int64, int64)
	ret

IL_0012:
	ldloca.s V_0
	conv.u
	conv.u8
	ldarg.2
	tail. call int32 class A`1<!T>::check (int64, int64)
	ret
}

.method public hidebysig static int32 f2 (int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
	.locals init (int32 V_0)
	ldarg.0
	ldc.i4.0
	ble.s IL_0012
	ldarg.0
	ldc.i4.1
	sub
	ldarg.1
	ldloca.s V_0
	conv.u
	conv.u8
	tail. call int32 class A`1<!T>::f1 (int32, int64, int64)
	ret

IL_0012:
	ldloca.s V_0
	conv.u
	conv.u8
	ldarg.2
	tail. call int32 class A`1<!T>::check (int64, int64)
	ret
}

.method instance void .ctor()
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
	ldc.i4.s 100
	ldloca.s V_0
	conv.u
	conv.u8
	ldc.i4.0
	conv.i8
	tail. call int32 class A`1<object>::f1(int32, int64, int64)
	ret
}

.method instance void .ctor()
{
	ldarg.0
	tail. call instance void [mscorlib]System.Object::.ctor()
	ret
}
}
