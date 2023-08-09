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
.namespace JitTest_local_boxunbox_il {
.class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
{
.method public hidebysig static int32 Main() cil managed
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack  32
	.locals (unsigned int8 VAR_u1,
		float64 VAR_r8,
		unsigned int16 VAR_u2,
		unsigned int32 VAR_u4,
		unsigned int64 VAR_u8,
		int8 VAR_i1,
		int16 VAR_i2,
		int32 VAR_i4,
		float32 VAR_r4,
		int64 VAR_i8,
		object BOX_u1,
		object BOX_r8,
		object BOX_u2,
		object BOX_u4,
		object BOX_u8,
		object BOX_i1,
		object BOX_i2,
		object BOX_i4,
		object BOX_r4,
		object BOX_i8)
	.zeroinit
	ldloc.s VAR_i1
	ldloc.s VAR_i4
	ldloc.s VAR_i1
	conv.u4
	ldloc.s VAR_u8
	ldloc.s VAR_u1
	box unsigned int8
	dup
	unbox unsigned int8
	ldind.u1
	dup
	ldloc.s VAR_i1
	dup
	dup
	box int32
	stloc.s BOX_i4
	ldloc.s VAR_u4
	ldloc.s VAR_i1
	ldloc.s VAR_u2
	ldloc.s VAR_i1
	ldloc.s VAR_u1
	conv.i8
	ldloc.s VAR_i2
	box unsigned int16
	dup
	dup
	stloc.s BOX_u2
	dup
	ldloc.s VAR_i4
	dup
	conv.i1
	box unsigned int16
	dup
	stloc.s BOX_u2
	dup
	ldloc.s VAR_u4
	dup
	ldloc.s VAR_i8
	box unsigned int64
	stloc.s BOX_u8
	box unsigned int16
	unbox unsigned int16
	dup
	ldloc.s VAR_i1
	ldloc.s VAR_i1
	conv.u8
	ldloc.s VAR_i1
	ldloc.s VAR_r4
	conv.i1
	dup
	conv.r8
	dup
	box float64
	unbox float64
	dup
	dup
	ldind.r8
	ldloc.s VAR_i1
	box unsigned int8
	stloc.s BOX_u1
	ldloc.s VAR_i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int16
	stloc.s BOX_i2
	conv.i4
	box unsigned int16
	stloc.s BOX_u2
	ldind.r8
	box float64
	stloc.s BOX_r8
	ldind.r8
	conv.i8
	box unsigned int64
	stloc.s BOX_u8
	box float64
	stloc.s BOX_r8
	box int16
	unbox int16
	ldind.i2
	conv.u4
	box unsigned int32
	stloc.s BOX_u4
	conv.u1
	pop
	conv.u2
	box int16
	stloc.s BOX_i2
	conv.i2
	box unsigned int32
	stloc.s BOX_u4
	ldind.u2
	conv.u1
	box int8
	stloc.s BOX_i1
	ldind.u2
	box int16
	stloc.s BOX_i2
	box unsigned int16
	stloc.s BOX_u2
	unbox unsigned int16
	pop
	stloc.s BOX_u2
	conv.u8
	box unsigned int64
	stloc.s BOX_u8
	stloc.s BOX_u2
	unbox unsigned int16
	ldind.u2
	conv.u1
	box int16
	stloc.s BOX_i2
	unbox unsigned int16
	ldind.u2
	box int16
	unbox int16
	ldind.i2
	conv.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int32
	stloc.s BOX_i4
	box unsigned int64
	stloc.s BOX_u8
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int8
	stloc.s BOX_i1
	conv.i1
	box int16
	unbox int16
	ldind.i2
	conv.r8
	box float32
	stloc.s BOX_r4
	box int32
	stloc.s BOX_i4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int32
	unbox int32
	ldind.i4
	conv.i2
	box int8
	unbox int8
	ldind.i1
	conv.i4
	box int8
	unbox int8
	ldind.i1
	box unsigned int32
	stloc.s BOX_u4
	conv.i1
	box int16
	stloc.s BOX_i2
	conv.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.r8
	box float32
	unbox float32
	ldind.r4
	box float64
	stloc.s BOX_r8
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int32
	unbox int32
	ldind.i4
	conv.u1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.i1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int16
	unbox int16
	ldind.i2
	box unsigned int32
	stloc.s BOX_u4
	box int8
	stloc.s BOX_i1
	unbox unsigned int8
	ldind.u1
	conv.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.r8
	box float64
	unbox float64
	ldind.r8
	conv.u1
	box unsigned int32
	stloc.s BOX_u4
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box unsigned int64
	stloc.s BOX_u8
	box unsigned int8
	stloc.s BOX_u1
	box unsigned int16
	stloc.s BOX_u2
	conv.i4
	box int32
	unbox int32
	ldind.i4
	box int32
	stloc.s BOX_i4
	ldloc.s BOX_i4
	unbox int32
	ldind.i4
	not
	ldc.i4 100
	and
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
