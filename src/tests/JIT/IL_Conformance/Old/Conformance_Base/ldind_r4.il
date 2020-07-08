// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public ldind_r4 {



.field public static int64 LOADME
.field public static int64 LOADME2

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldsflda		int64 ldind_r4::LOADME
	ldc.r4		1.5
	stind.r4

	ldsflda		int64 ldind_r4::LOADME
	ldc.i4		0x4
	add
	ldc.r4		2.25
	stind.r4

	ldsflda		int64 ldind_r4::LOADME
	ldc.i4		0x8
	add
	ldc.r4		3.125
	stind.r4

	ldsflda		int64 ldind_r4::LOADME
	ldc.i4		0xC
	add
	ldc.r4		4.0625
	stind.r4


	ldc.r4		1.5
	ldsflda		int64 ldind_r4::LOADME
	ldind.r4
	ceq
	brfalse		FAIL

	ldc.r4		2.25
	ldsflda		int64 ldind_r4::LOADME
	ldc.i4		0x4
	add
	ldind.r4
	ceq
	brfalse		FAIL

	ldc.r4		3.125
	ldsflda		int64 ldind_r4::LOADME
	ldc.i4		0x8
	add
	ldind.r4
	ceq
	brfalse		FAIL

	ldc.r4		4.0625
	ldsflda		int64 ldind_r4::LOADME
	ldc.i4		0xC
	add
	ldind.r4
	ceq
	brfalse		FAIL


	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}

.assembly ldind_r4.exe{}
