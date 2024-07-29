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
.namespace JitTest_array_boxunbox_il {
.class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
{
.method public hidebysig static int32 Main() cil managed
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack  32
	.locals (unsigned int8[] ARR_u1,
		float64[] ARR_r8,
		unsigned int16[] ARR_u2,
		unsigned int32[] ARR_u4,
		unsigned int64[] ARR_u8,
		int8[] ARR_i1,
		int16[] ARR_i2,
		int32[] ARR_i4,
		float32[] ARR_r4,
		int64[] ARR_i8,
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
	ldc.i4 10
	newarr unsigned int8
	stloc.s ARR_u1	ldc.i4 10
	newarr float64
	stloc.s ARR_r8	ldc.i4 10
	newarr unsigned int16
	stloc.s ARR_u2	ldc.i4 10
	newarr unsigned int32
	stloc.s ARR_u4	ldc.i4 10
	newarr unsigned int64
	stloc.s ARR_u8	ldc.i4 10
	newarr int8
	stloc.s ARR_i1	ldc.i4 10
	newarr int16
	stloc.s ARR_i2	ldc.i4 10
	newarr int32
	stloc.s ARR_i4	ldc.i4 10
	newarr float32
	stloc.s ARR_r4	ldc.i4 10
	newarr int64
	stloc.s ARR_i8	ldloc.s ARR_i4
	ldc.i4.0
	ldelem.i4
	ldloc.s ARR_i2
	ldc.i4.0
	ldelem.i2
	dup
	ldloc.s ARR_r8
	ldc.i4.0
	ldelem.r8
	box float64
	dup
	unbox float64
	ldind.r8
	box float32
	unbox float32
	ldloc.s ARR_i2
	ldc.i4.0
	ldelem.i2
	conv.r8
	dup
	conv.r8
	ldloc.s ARR_r8
	ldc.i4.0
	ldelem.r8
	ldloc.s ARR_r8
	ldc.i4.0
	ldelem.r8
	dup
	conv.u8
	box int64
	ldloc.s ARR_r8
	ldc.i4.0
	ldelem.r8
	box float64
	dup
	unbox float64
	ldloc.s ARR_i1
	ldc.i4.0
	ldelem.i1
	dup
	box unsigned int32
	unbox unsigned int32
	ldloc.s ARR_u1
	ldc.i4.0
	ldelem.u1
	conv.r8
	ldloc.s ARR_u8
	ldc.i4.0
	ldelem.u8
	dup
	dup
	conv.i1
	ldloc.s ARR_r8
	ldc.i4.0
	ldelem.r8
	conv.r8
	ldloc.s ARR_i4
	ldc.i4.0
	ldelem.i4
	conv.u8
	ldloc.s ARR_i1
	ldc.i4.0
	ldelem.i1
	ldloc.s ARR_u8
	ldc.i4.0
	ldelem.u8
	box int64
	ldloc.s ARR_u8
	ldc.i4.0
	ldelem.u8
	box int64
	stloc.s BOX_i8
	unbox int64
	ldloc.s ARR_r4
	ldc.i4.0
	ldelem.r4
	conv.u8
	box unsigned int64
	ldloc.s ARR_i4
	ldc.i4.0
	ldelem.i4
	ldloc.s ARR_u2
	ldc.i4.0
	ldelem.u2
	box int8
	dup
	dup
	dup
	ldloc.s ARR_i8
	ldc.i4.0
	ldelem.i8
	conv.u1
	ldloc.s ARR_u2
	ldc.i4.0
	ldelem.u2
	conv.u2
	ldloc.s ARR_r4
	ldc.i4.0
	ldelem.r4
	conv.u1
	box int16
	stloc.s BOX_i2
	conv.i8
	ldloc.s ARR_u4
	ldc.i4.0
	ldelem.u4
	dup
	conv.i1
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.r4
	box float32
	stloc.s BOX_r4
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int8
	unbox int8
	ldind.i1
	box int8
	stloc.s BOX_i1
	box unsigned int64
	stloc.s BOX_u8
	box unsigned int16
	stloc.s BOX_u2
	stloc.s BOX_i1
	unbox int8
	ldind.i1
	box int32
	stloc.s BOX_i4
	unbox int8
	ldind.i1
	box int16
	unbox int16
	ldind.i2
	box int16
	stloc.s BOX_i2
	stloc.s BOX_i1
	conv.i2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int8
	unbox int8
	ldind.i1
	conv.r8
	box float32
	stloc.s BOX_r4
	stloc.s BOX_u8
	ldind.i8
	conv.i4
	box int32
	stloc.s BOX_i4
	conv.i8
	box int64
	stloc.s BOX_i8
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	box int8
	unbox int8
	ldind.i1
	conv.i4
	box int8
	unbox int8
	ldind.i1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int16
	stloc.s BOX_i2
	conv.u2
	box int16
	unbox int16
	ldind.i2
	box unsigned int32
	stloc.s BOX_u4
	conv.i1
	box int32
	unbox int32
	ldind.i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box int8
	unbox int8
	ldind.i1
	conv.u4
	box unsigned int32
	stloc.s BOX_u4
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	box int64
	unbox int64
	pop
	conv.u2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int32
	stloc.s BOX_i4
	conv.i2
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int16
	unbox int16
	ldind.i2
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.i4
	box int32
	stloc.s BOX_i4
	ldind.u4
	box int16
	unbox int16
	ldind.i2
	conv.i8
	box int64
	stloc.s BOX_i8
	box unsigned int16
	stloc.s BOX_u2
	ldind.r8
	conv.r8
	box float64
	unbox float64
	ldind.r8
	conv.i4
	box int8
	stloc.s BOX_i1
	unbox float64
	ldind.r8
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float64
	unbox float64
	ldind.r8
	conv.i8
	box unsigned int64
	stloc.s BOX_u8
	unbox int64
	ldind.i8
	conv.i1
	box unsigned int32
	stloc.s BOX_u4
	conv.i4
	box int16
	stloc.s BOX_i2
	box float32
	unbox float32
	ldind.r4
	conv.i2
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.u1
	box int32
	unbox int32
	ldind.i4
	conv.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	conv.r4
	box float64
	unbox float64
	ldind.r8
	conv.i1
	box unsigned int32
	unbox unsigned int32
	ldind.u4
	conv.u4
	box unsigned int8
	unbox unsigned int8
	ldind.u1
	box int32
	stloc.s BOX_i4
	box float32
	stloc.s BOX_r4
	conv.i4
	box unsigned int16
	stloc.s BOX_u2
	ldind.r4
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
	box unsigned int64
	unbox unsigned int64
	ldind.u8
	conv.u1
	box int8
	unbox int8
	ldind.i1
	conv.u1
	box int32
	unbox int32
	ldind.i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	box unsigned int16
	unbox unsigned int16
	pop
	unbox float64
	ldind.r8
	conv.i1
	box int8
	unbox int8
	ldind.i1
	box int8
	unbox int8
	ldind.i1
	box unsigned int16
	stloc.s BOX_u2
	conv.r4
	box float64
	unbox float64
	ldind.r8
	box float32
	stloc.s BOX_r4
	conv.i1
	box int32
	stloc.s BOX_i4
	box unsigned int16
	unbox unsigned int16
	ldind.u2
	conv.u4
	box int8
	stloc.s BOX_i1
	ldloc.s BOX_i1
	unbox int8
	ldind.i1
	ldc.i4 100
	or
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
