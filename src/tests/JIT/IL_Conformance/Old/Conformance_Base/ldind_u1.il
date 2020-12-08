// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public ldind_u1 {


.field public static int32 LOADME

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldsflda		int32 ldind_u1::LOADME
	ldc.i4		0x12789AEF
	stind.i4

	ldc.i4		0xEF
	ldsflda		int32 ldind_u1::LOADME
	ldind.u1
	ceq
	brfalse		FAIL

	ldc.i4		0x9A
	ldsflda		int32 ldind_u1::LOADME
	ldc.i4		0x1
	add
	ldind.u1
	ceq
	brfalse		FAIL

	ldc.i4		0x78
	ldsflda		int32 ldind_u1::LOADME
	ldc.i4		0x2
	add
	ldind.u1
	ceq
	brfalse		FAIL

	ldc.i4		0x12
	ldsflda		int32 ldind_u1::LOADME
	ldc.i4		0x3
	add
	ldind.u1
	ceq
	brfalse		FAIL


	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldind_u1.exe{}
