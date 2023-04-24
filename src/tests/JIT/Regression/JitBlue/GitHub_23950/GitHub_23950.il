// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly extern xunit.core {}

.assembly 'GitHub_23950' {}

// Test that jit can inline method with a pinned pointer

.class public sequential ansi sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	.method public hidebysig static 
		int32 Main () cil managed
	{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)

		.maxstack 1
		.entrypoint
		.locals init (
			[0] int32
		)
		nop
		ldc.i4.s 100
		stloc.0
		ldloca.s 0
		call int32 Program::GetValueUnsafe(int32&)
		ret
	}

	.method private hidebysig 
		static int32 GetValueUnsafe (
			int32& buffer
		) cil managed 
	{
		.maxstack 3
		.locals init (
			[0] int32* pinned
		)

		ldarg.0
		stloc.0
		ldloc.0
		ldind.i4
		ret
	}
}
