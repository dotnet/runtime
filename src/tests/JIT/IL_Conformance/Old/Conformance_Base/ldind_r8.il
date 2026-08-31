// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.class public ldind_r8 {



.field public static int64 LOADME
.field public static int64 LOADME2
.field public static int64 LOADME3
.field public static int64 LOADME4

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		20
	ldsflda		int64 ldind_r8::LOADME
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	stind.r8

	ldsflda		int64 ldind_r8::LOADME
	ldc.i4		0x8
	add
	ldc.r8		float64(0xBFF0000000000000)
	stind.r8

	ldsflda		int64 ldind_r8::LOADME
	ldc.i4		0x10
	add
	ldc.r8		float64(0x3FF0000000000000)
	stind.r8

	ldsflda		int64 ldind_r8::LOADME
	ldc.i4		0x18
	add
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	stind.r8


	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldsflda		int64 ldind_r8::LOADME
	ldind.r8
	ceq
	brfalse		FAIL

	ldc.r8		float64(0xBFF0000000000000)
	ldsflda		int64 ldind_r8::LOADME
	ldc.i4		0x8
	add
	ldind.r8
	ceq
	brfalse		FAIL

	ldc.r8		float64(0x3FF0000000000000)
	ldsflda		int64 ldind_r8::LOADME
	ldc.i4		0x10
	add
	ldind.r8
	ceq
	brfalse		FAIL

	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldsflda		int64 ldind_r8::LOADME
	ldc.i4		0x18
	add
	ldind.r8
	ceq
	brfalse		FAIL


	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}

.assembly ldind_r8{}
