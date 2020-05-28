/*
Test tail.callvirt.

Author:
    Jay Krell (jaykrell@microsoft.com)

Copyright 2018 Microsoft
Licensed under the MIT license. See LICENSE file in the project root for full license information.

This IL will exit with 0 for success, 1 for failure.
It is based on nearby C# that is based on nearby F#.

This test covers tail.callvirt.
Incidentally it is a mutual recursion, not a self-recursion,
no generics, no pointers, small function signature, no value types, etc.

This is based on F#, that stack overflows or exit with 1 or 2.
If testing the F#, be sure to compile with --optimize- else
the tailcall might be optimized away:
	type A() =
	    member this.f2 (a) = if a > 0 then this.f1(a - 1)
				 else 1
	    member this.f1 (a) = if a > 0 then this.f2(a - 1)
				 else 2

	let a = 	A()
	printfn "%d" (a.f1(1000 * 1000 * 10))

and more closely on C#, csc -optimize to keep
ret next call, ildasm and add tail.:
	using System;
	using System.Runtime.CompilerServices;
	using static System.Runtime.CompilerServices.MethodImplOptions;

	unsafe class A
	{
	    static int check (long stack1, long stack2)
	    {
		return (stack1 == stack2) ? 0 : 1;
	    }

	    [MethodImpl (NoInlining)]
	    public virtual int f1(int counter, long initial_stack, long current_stack)
	    {
		int local;
		if (counter > 0)
		    return f2(counter - 1, initial_stack, (long)&local);
		return check((long)&local, current_stack);
	    }

	    [MethodImpl (NoInlining)]
	    public virtual int f2(int counter, long initial_stack, long current_stack)
	    {
		int local;
		if (counter > 0)
		    return f1(counter - 1, initial_stack, (long)&local);
		return check((long)&local, current_stack);
	    }

	    [MethodImpl (NoInlining)]
	    public static void Main()
	    {
		int stack;
		Environment.Exit(new A().f1(100, (long)&stack, 0));
	    }
	}
*/

.assembly extern mscorlib { }

.assembly 'tailcall-virt' { }

.class A
{
.method static int32 check(int64 stack1, int64 stack2) 
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

.method newslot virtual instance int32 f1(int32 counter, int64 initial_stack, int64 current_stack) noinlining
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
	tail. callvirt instance int32 A::f2(int32, int64, int64)
	ret

IL_0013:
	ldloca.s V_0
	conv.u
	conv.u8
	ldarg.3
	tail. call int32 A::check(int64, int64)
	ret
}

.method newslot virtual instance int32 f2(int32 counter, int64 initial_stack, int64 current_stack) noinlining
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
	tail. callvirt instance int32 A::f1(int32, int64, int64)
	ret

IL_0013:
	ldloca.s V_0
	conv.u
	conv.u8
	ldarg.3
	tail. call int32 A::check(int64, int64)
	ret
}

.method static void Main() noinlining
{
	.entrypoint
	.locals init (int32 V_0)
	newobj instance void A::.ctor()
	ldc.i4 0x100
	ldloca.s V_0
	conv.u
	conv.u8
	ldc.i4.0
	conv.i8
	callvirt instance int32 A::f1(int32, int64, int64)
	tail. call void [mscorlib]System.Environment::Exit(int32)
	ret
}

.method instance void .ctor() 
{
	ldarg.0
	tail. call instance void [mscorlib]System.Object::.ctor()
	ret
}

}
