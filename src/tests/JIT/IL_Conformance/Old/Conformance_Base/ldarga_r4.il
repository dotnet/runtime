// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.namespace ldarga_r4
{
.class public _ldarga {

.method public static	int32 args(float32,float32) {
.maxstack	5
	ldarga 0
	ldind.r4
	ldarga 1
	ldind.r4
	ceq
	ret
}


.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		10
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0xFF800000)
	call	int32 ldarga_r4._ldarga::args(float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0xFF7FFFFF)
	call	int32 ldarga_r4._ldarga::args(float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0xBF80000)
	ldc.r4		float32(0xBF80000)
	call	int32 ldarga_r4._ldarga::args(float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x80000000)
	call	int32 ldarga_r4._ldarga::args(float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x00000000)
	call	int32 ldarga_r4._ldarga::args(float32,float32)	
	brfalse		FAIL

	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x3F800000)
	call	int32 ldarga_r4._ldarga::args(float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7F7FFFFF)
	call	int32 ldarga_r4._ldarga::args(float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7F800000)
	call	int32 ldarga_r4._ldarga::args(float32,float32)
	brfalse		FAIL



	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldarga_r4{}
}
