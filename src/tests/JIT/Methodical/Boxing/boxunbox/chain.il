// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace JitTest_chain_boxunbox_il {
.class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
{
.method public hidebysig static int32 Main() cil managed
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack  32
	ldc.i8 13
	box unsigned int64
	unbox unsigned int64
	ldc.r4 88
	conv.i8
	box unsigned int64
	ldc.r4 10
	ldc.i4 21
	box int32
	ldc.i8 72
	conv.r4
	box float64
	unbox float64
	ldc.i4 5
	box unsigned int32
	unbox unsigned int32
	ldc.r4 121
	ldc.i4 79
	ldc.i8 52
	conv.u2
	box int16
	unbox int16
	ldind.i2
	conv.i8
	ldc.i4 68
	conv.u8
	box int64
	ldc.r4 50
	conv.r8
	ldc.r4 9
	box float64
	unbox float64
	ldc.i4 118
	ldc.i8 9
	ldc.r4 89
	box float32
	dup
	unbox float32
	dup
	dup
	dup
	dup
	ldc.i4 110
	ldc.i4 10
	ldc.i8 16
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u2
	box int16
	unbox int16
	dup
	ldind.i2
	ldc.r4 23
	box float32
	unbox float32
	ldc.i4 88
	box int32
	ldc.i8 17
	box int64
	ldc.i8 127
	box int64
	ldc.i8 45
	conv.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int32
	unbox int32
	ldind.i4
	conv.u1
	box int32
	unbox int32
	ldind.i4
	box int16
	unbox int16
	ldind.i2
	conv.r4
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	conv.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.u2
	box unsigned int32
	unbox unsigned int32
	pop
	unbox int64
	ldind.i8
	conv.r8
	box float32
	unbox float32
	ldind.r4
	conv.i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int16
	unbox int16
	ldind.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int16
	unbox int16
	ldind.i2
	box int16
	unbox int16
	ldind.i2
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i4
	box int8
	unbox int8
	ldind.i1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.u8
	box int64
	unbox int64
	ldind.i8
	conv.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u2
	box int32
	unbox int32
	ldind.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i8
	box int64
	unbox int64
	ldind.i8
	conv.r8
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.i1
	box int32
	unbox int32
	ldind.i4
	box int8
	unbox int8
	ldind.i1
	conv.r4
	box float32
	unbox float32
	ldind.r4
	conv.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int32
	unbox int32
	ldind.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.u8
	box int64
	unbox int64
	ldind.i8
	conv.r8
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.u4
	box int32
	unbox int32
	ldind.i4
	box int16
	unbox int16
	ldind.i2
	conv.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int16
	unbox int16
	ldind.i2
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	conv.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.u2
	box int8
	pop
	unbox int64
	ldind.i8
	conv.r4
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	conv.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i1
	box int16
	unbox int16
	ldind.i2
	box int32
	unbox int32
	ldind.i4
	conv.r4
	box float64
	unbox float64
	ldind.r8
	conv.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int16
	unbox int16
	ldind.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int32
	unbox int32
	ldind.i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int16
	unbox int16
	ldind.i2
	box int16
	unbox int16
	ldind.i2
	box int8
	unbox int8
	ldind.i1
	box int32
	unbox int32
	ldind.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.r4
	box float32
	unbox float32
	ldind.r4
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u8
	box int64
	unbox int64
	ldind.i8
	conv.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int32
	unbox int32
	ldind.i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int32
	unbox int32
	ldind.i4
	conv.i1
	box int32
	unbox int32
	ldind.i4
	conv.u4
	box int32
	unbox int32
	ldind.i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i1
	box int8
	unbox int8
	ldind.i1
	conv.u8
	box int64
	unbox int64
	ldind.i8
	conv.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int32
	unbox int32
	ldind.i4
	box int32
	unbox int32
	ldind.i4
	box unsigned int8
	unbox unsigned int8
	pop
	unbox int32
	ldind.i4
	conv.r4
	box float32
	unbox float32
	ldind.r4
	conv.r8
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	conv.r8
	box float64
	unbox float64
	ldind.r8
	conv.i4
	box int16
	unbox int16
	ldind.i2
	conv.u1
	box int8
	unbox int8
	ldind.i1
	conv.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int8
	unbox int8
	ldind.i1
	conv.u1
	box int16
	unbox int16
	ldind.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int32
	unbox int32
	ldind.i4
	conv.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int32
	unbox int32
	ldind.i4
	conv.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int8
	unbox int8
	ldind.i1
	conv.r4
	box float32
	unbox float32
	ldind.r4
	conv.u8
	box int64
	unbox int64
	ldind.i8
	conv.u4
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int8
	unbox int8
	ldind.i1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.u2
	box int16
	unbox int16
	ldind.i2
	conv.i1
	box int32
	unbox int32
	ldind.i4
	box int32
	unbox int32
	ldind.i4
	box int32
	unbox int32
	ldind.i4
	conv.u8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	conv.i1
	pop
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	conv.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int32
	unbox int32
	ldind.i4
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.u1
	box int8
	unbox int8
	ldind.i1
	box int16
	unbox int16
	ldind.i2
	box int16
	unbox int16
	ldind.i2
	conv.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i8
	box int64
	unbox int64
	ldind.i8
	conv.u8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i1
	box int16
	unbox int16
	ldind.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.r4
	box float32
	unbox float32
	ldind.r4
	conv.r4
	box float64
	unbox float64
	ldind.r8
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.r4
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	conv.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int8
	unbox int8
	ldind.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int8
	unbox int8
	ldind.i1
	box int8
	unbox int8
	ldind.i1
	conv.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int8
	unbox int8
	ldind.i1
	box int32
	unbox int32
	ldind.i4
	conv.r8
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	conv.r4
	box float32
	unbox float32
	ldind.r4
	conv.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.u2
	box int16
	unbox int16
	ldind.i2
	conv.i1
	box int16
	pop
	box int32
	unbox int32
	ldind.i4
	conv.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.r8
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	conv.r8
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	pop
	ldind.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.r8
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	conv.i4
	box int16
	unbox int16
	ldind.i2
	box int16
	unbox int16
	ldind.i2
	box int32
	unbox int32
	ldind.i4
	conv.r4
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	conv.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.r8
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	pop
	box int8
	unbox int8
	ldind.i1
	conv.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u2
	box int32
	unbox int32
	ldind.i4
	conv.u1
	box int16
	unbox int16
	ldind.i2
	conv.i4
	box int32
	unbox int32
	ldind.i4
	conv.i8
	box int64
	unbox int64
	ldind.i8
	conv.r8
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	conv.i8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	conv.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.r8
	box float32
	unbox float32
	ldind.r4
	conv.u1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int32
	unbox int32
	ldind.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int16
	unbox int16
	ldind.i2
	conv.i1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int8
	unbox int8
	ldind.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.u2
	box int32
	unbox int32
	ldind.i4
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int16
	unbox int16
	ldind.i2
	box int16
	unbox int16
	ldind.i2
	conv.u2
	box int32
	unbox int32
	ldind.i4
	conv.i8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	conv.i1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	conv.i4
	box int16
	unbox int16
	ldind.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i1
	box int16
	unbox int16
	ldind.i2
	conv.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u2
	box int32
	unbox int32
	ldind.i4
	conv.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int8
	unbox int8
	ldind.i1
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i4
	box int8
	unbox int8
	ldind.i1
	box int8
	unbox int8
	ldind.i1
	conv.i2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u4
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int16
	unbox int16
	ldind.i2
	conv.r8
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	conv.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.r8
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	conv.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.u8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u2
	box int32
	unbox int32
	ldind.i4
	conv.u2
	box int16
	unbox int16
	ldind.i2
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i2
	box int16
	unbox int16
	ldind.i2
	conv.u1
	box int32
	unbox int32
	ldind.i4
	conv.u4
	box int8
	unbox int8
	ldind.i1
	box int8
	unbox int8
	ldind.i1
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i1
	box int16
	unbox int16
	ldind.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int16
	unbox int16
	ldind.i2
	conv.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i1
	box int8
	unbox int8
	ldind.i1
	conv.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int8
	unbox int8
	ldind.i1
	conv.u8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	conv.i4
	box int8
	unbox int8
	ldind.i1
	box int8
	unbox int8
	ldind.i1
	box int8
	unbox int8
	ldind.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int16
	unbox int16
	ldind.i2
	box int8
	unbox int8
	ldind.i1
	box int32
	unbox int32
	ldind.i4
	box int32
	unbox int32
	ldind.i4
	conv.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int16
	unbox int16
	ldind.i2
	conv.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.r4
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	conv.u1
	box int8
	unbox int8
	ldind.i1
	conv.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u2
	box int8
	unbox int8
	ldind.i1
	box int32
	unbox int32
	ldind.i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.u8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	conv.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int16
	unbox int16
	ldind.i2
	box int8
	unbox int8
	ldind.i1
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	conv.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int8
	unbox int8
	ldind.i1
	conv.u1
	box int8
	pop
	conv.r8
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.r8
	box float64
	unbox float64
	ldind.r8
	conv.i4
	box int32
	unbox int32
	ldind.i4
	conv.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int16
	unbox int16
	ldind.i2
	conv.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.r4
	box float32
	pop
	ldind.r4
	conv.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.r4
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	conv.i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u8
	box int64
	unbox int64
	ldind.i8
	conv.u4
	box int8
	unbox int8
	ldind.i1
	box int32
	unbox int32
	ldind.i4
	box int8
	unbox int8
	ldind.i1
	conv.i8
	box int64
	unbox int64
	ldind.i8
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	conv.i8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	conv.u1
	box int16
	unbox int16
	ldind.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int32
	unbox int32
	ldind.i4
	conv.r8
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	conv.i1
	box int32
	unbox int32
	ldind.i4
	conv.i8
	box int64
	unbox int64
	ldind.i8
	conv.i1
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int8
	unbox int8
	ldind.i1
	conv.i4
	box int16
	unbox int16
	ldind.i2
	box int32
	unbox int32
	ldind.i4
	box int8
	unbox int8
	pop
	ldind.r4
	box float32
	pop
	ldind.r4
	conv.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int32
	unbox int32
	ldind.i4
	conv.u1
	box int32
	unbox int32
	ldind.i4
	conv.u8
	box unsigned int64
	unbox unsigned int64
	pop
	ldind.r4
	conv.i2
	box int32
	unbox int32
	ldind.i4
	conv.r8
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	conv.i1
	box int32
	unbox int32
	ldind.i4
	conv.r4
	pop
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	conv.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int16
	unbox int16
	ldind.i2
	conv.r8
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	conv.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int8
	unbox int8
	ldind.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int32
	unbox int32
	ldind.i4
	box int8
	unbox int8
	ldind.i1
	conv.u4
	box int16
	unbox int16
	ldind.i2
	box int16
	unbox int16
	ldind.i2
	box int16
	unbox int16
	ldind.i2
	conv.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i4
	box int8
	unbox int8
	ldind.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i2
	box int32
	unbox int32
	ldind.i4
	conv.i2
	box int32
	unbox int32
	ldind.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int16
	unbox int16
	ldind.i2
	box int8
	unbox int8
	ldind.i1
	conv.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.u8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	conv.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int32
	unbox int32
	ldind.i4
	box int16
	unbox int16
	ldind.i2
	conv.i4
	box int16
	unbox int16
	ldind.i2
	conv.u4
	box int32
	unbox int32
	ldind.i4
	box int32
	unbox int32
	ldind.i4
	box int32
	unbox int32
	ldind.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i8
	box int64
	unbox int64
	ldind.i8
	conv.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i4
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int8
	unbox int8
	ldind.i1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i1
	box int32
	unbox int32
	ldind.i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.u2
	box int32
	unbox int32
	ldind.i4
	box int16
	unbox int16
	ldind.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int32
	unbox int32
	ldind.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int32
	unbox int32
	ldind.i4
	box int32
	unbox int32
	ldind.i4
	conv.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i8
	box unsigned int64
	pop
	pop
	box int64
	unbox int64
	ldind.i8
	conv.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i2
	box int32
	unbox int32
	ldind.i4
	box int16
	unbox int16
	ldind.i2
	conv.i2
	box int16
	unbox int16
	ldind.i2
	conv.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i4
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i1
	box int32
	unbox int32
	ldind.i4
	conv.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int32
	unbox int32
	ldind.i4
	box int32
	unbox int32
	ldind.i4
	conv.u1
	box int32
	unbox int32
	ldind.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.r8
	box float32
	unbox float32
	ldind.r4
	conv.i4
	box int16
	unbox int16
	ldind.i2
	conv.i8
	box int64
	unbox int64
	ldind.i8
	conv.i1
	box int8
	unbox int8
	ldind.i1
	conv.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u2
	box int32
	unbox int32
	ldind.i4
	box int32
	unbox int32
	ldind.i4
	conv.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i2
	box unsigned int32
	unbox unsigned int32
	pop
	conv.r8
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u2
	box int8
	unbox int8
	ldind.i1
	box int32
	unbox int32
	ldind.i4
	conv.i8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	conv.i8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.r4
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	conv.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.r8
	box float64
	unbox float64
	ldind.r8
	conv.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i8
	box int64
	unbox int64
	ldind.i8
	conv.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.r8
	box float64
	unbox float64
	ldind.r8
	conv.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.r4
	box float64
	unbox float64
	pop
	ldind.r8
	conv.i4
	box int16
	unbox int16
	ldind.i2
	conv.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int32
	pop
	box float64
	unbox float64
	ldind.r8
	conv.i8
	pop
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	box float64
	pop
	box int64
	unbox int64
	ldind.i8
	conv.i4
	box int16
	unbox int16
	ldind.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u2
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.r4
	box float64
	unbox float64
	ldind.r8
	conv.i8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int8
	unbox int8
	ldind.i1
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.r8
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	conv.r8
	box float32
	unbox float32
	ldind.r4
	conv.i2
	box int8
	unbox int8
	ldind.i1
	box int8
	unbox int8
	ldind.i1
	conv.u4
	box int8
	unbox int8
	ldind.i1
	conv.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.r8
	box float32
	unbox float32
	ldind.r4
	conv.u8
	box int64
	unbox int64
	ldind.i8
	conv.u1
	box unsigned int16
	unbox unsigned int16
	pop
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int8
	unbox int8
	ldind.i1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.r4
	box float64
	unbox float64
	ldind.r8
	conv.r8
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	box float64
	pop
	box float32
	unbox float32
	ldind.r4
	conv.r4
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	conv.u1
	box int16
	unbox int16
	ldind.i2
	conv.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i8
	box int64
	unbox int64
	ldind.i8
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i1
	box int32
	unbox int32
	ldind.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.r8
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	conv.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.r8
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	conv.i8
	box int64
	unbox int64
	ldind.i8
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i1
	box int32
	unbox int32
	ldind.i4
	conv.r4
	box float64
	unbox float64
	ldind.r8
	conv.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i4
	box int16
	unbox int16
	ldind.i2
	box int16
	unbox int16
	ldind.i2
	conv.u1
	box int32
	unbox int32
	ldind.i4
	conv.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.r4
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.r4
	box float32
	unbox float32
	ldind.r4
	conv.i2
	box int32
	unbox int32
	ldind.i4
	conv.i4
	box int8
	unbox int8
	ldind.i1
	conv.r8
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	conv.i1
	box int8
	unbox int8
	ldind.i1
	box int32
	unbox int32
	ldind.i4
	conv.i8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i8
	pop
	ldind.u4
	conv.u2
	box int8
	unbox int8
	ldind.i1
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.i1
	box int16
	unbox int16
	ldind.i2
	conv.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int8
	unbox int8
	ldind.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int32
	unbox int32
	ldind.i4
	box int8
	unbox int8
	ldind.i1
	conv.r8
	box float64
	unbox float64
	ldind.r8
	conv.u2
	box int8
	unbox int8
	ldind.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.u4
	box int32
	unbox int32
	ldind.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int16
	unbox int16
	ldind.i2
	conv.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i8
	box int64
	unbox int64
	ldind.i8
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	conv.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.u1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	conv.i2
	box int16
	unbox int16
	ldind.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int32
	unbox int32
	ldind.i4
	box int32
	unbox int32
	ldind.i4
	box int32
	unbox int32
	ldind.i4
	box int32
	unbox int32
	ldind.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int32
	unbox int32
	ldind.i4
	conv.u2
	box int16
	unbox int16
	ldind.i2
	conv.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.r4
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.r8
	box float64
	unbox float64
	ldind.r8
	conv.i1
	box int16
	unbox int16
	ldind.i2
	conv.u8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.r8
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	conv.i2
	box int16
	unbox int16
	ldind.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u4
	box int8
	unbox int8
	ldind.i1
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int8
	unbox int8
	ldind.i1
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.r4
	box float32
	unbox float32
	ldind.r4
	conv.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.r8
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i4
	box int16
	unbox int16
	ldind.i2
	conv.u2
	box int8
	unbox int8
	ldind.i1
	conv.u2
	box int16
	unbox int16
	ldind.i2
	box int16
	unbox int16
	ldind.i2
	conv.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u2
	pop
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	conv.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.r8
	box float64
	unbox float64
	ldind.r8
	conv.i2
	box unsigned int32
	unbox unsigned int32
	pop
	unbox int32
	ldind.i4
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int32
	unbox int32
	ldind.i4
	box int16
	unbox int16
	ldind.i2
	conv.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i8
	pop
	box float64
	unbox float64
	ldind.r8
	conv.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i8
	box int64
	unbox int64
	ldind.i8
	conv.r8
	box float64
	unbox float64
	ldind.r8
	conv.i1
	box int16
	unbox int16
	ldind.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.r4
	box float32
	unbox float32
	ldind.r4
	conv.i1
	box int8
	unbox int8
	ldind.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.r8
	box float32
	unbox float32
	ldind.r4
	conv.r8
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.u4
	box int8
	unbox int8
	ldind.i1
	box int32
	unbox int32
	ldind.i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.u1
	box int32
	unbox int32
	ldind.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u4
	box int32
	unbox int32
	ldind.i4
	box int8
	unbox int8
	ldind.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i1
	box int16
	unbox int16
	ldind.i2
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u8
	box int64
	unbox int64
	ldind.i8
	box int64
	unbox int64
	ldind.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	ldind.i8
	conv.i4
	box int16
	unbox int16
	ldind.i2
	conv.r4
	box float32
	unbox float32
	ldind.r4
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	conv.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u4
	box int8
	unbox int8
	ldind.i1
	conv.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i8
	box int64
	unbox int64
	ldind.i8
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldind.r4
	conv.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.i4
	box int16
	unbox int16
	ldind.i2
	conv.i1
	box int32
	unbox int32
	ldind.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.u4
	box int32
	unbox int32
	ldind.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u1
	box int16
	unbox int16
	ldind.i2
	conv.i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u1
	box int16
	unbox int16
	ldind.i2
	box int8
	unbox int8
	ldind.i1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i2
	box int8
	unbox int8
	ldind.i1
	box int8
	unbox int8
	ldind.i1
	box int16
	unbox int16
	ldind.i2
	box int16
	unbox int16
	ldind.i2
	box int32
	unbox int32
	ldind.i4
	conv.i4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int32
	unbox int32
	ldind.i4
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int8
	unbox int8
	ldind.i1
	conv.i1
	box int16
	unbox int16
	ldind.i2
	box int8
	unbox int8
	ldind.i1
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int32
	pop
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i4
	pop
	ldind.u8
	conv.u2
	box int16
	unbox int16
	ldind.i2
	box int32
	unbox int32
	ldind.i4
	conv.i4
	box int32
	unbox int32
	ldind.i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int16
	unbox int16
	ldind.i2
	box int8
	unbox int8
	ldind.i1
	ldc.i4 87
	add.ovf
	dup
	ldc.i4 100
	beq.s IL_OK
	ldstr      "=== FAILED ==="
	br.s IL_NEXT
IL_OK:
	ldstr      "=== PASSED ==="
IL_NEXT:
	call       void [System.Console]System.Console::WriteLine(string)
	ret
}
.method public hidebysig specialname rtspecialname 
instance void  .ctor() cil managed
{
	.maxstack  8
	ldarg.0
	call       instance void [mscorlib]System.Object::.ctor()
	ret
}
}
}
