// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public ldind_u2 {


.field public static int64 LOADME

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20

	ldsflda		int64 ldind_u2::LOADME
	ldc.i8		0x0123456789ABCDEF
	stind.i8

	ldc.i4		0xCDEF
	ldsflda		int64 ldind_u2::LOADME
	ldind.u2
	ceq
	brfalse		FAIL

	ldc.i4		0x89AB
	ldsflda		int64 ldind_u2::LOADME
	ldc.i4		0x2
	add
	ldind.u2
	ceq
	brfalse		FAIL

	ldc.i4		0x4567
	ldsflda		int64 ldind_u2::LOADME
	ldc.i4		0x4
	add
	ldind.u2
	ceq
	brfalse		FAIL

	ldc.i4		0x0123
	ldsflda		int64 ldind_u2::LOADME
	ldc.i4		0x6
	add
	ldind.u2
	ceq
	brfalse		FAIL


	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldind_u2{}
