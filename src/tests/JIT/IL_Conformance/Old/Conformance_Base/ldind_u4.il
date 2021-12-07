// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public ldind_u4 {


.field public static int64 LOADME
.field public static int64 LOADME2

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20

	ldsflda		int64 ldind_u4::LOADME
	ldc.i8		0x0123456789ABCDEF
	stind.i8

	ldsflda		int64 ldind_u4::LOADME2
	ldc.i8		0x00000000FFFFFFFF
	stind.i8

	ldc.i4		0x89ABCDEF
	ldsflda		int64 ldind_u4::LOADME
	ldind.u4
	ceq
	brfalse		FAIL

	ldc.i4		0x01234567
	ldsflda		int64 ldind_u4::LOADME
	ldc.i4		0x4
	add
	ldind.u4
	ceq
	brfalse		FAIL

	ldc.i4		0xFFFFFFFF
	ldsflda		int64 ldind_u4::LOADME2
	ldind.u4
	ceq
	brfalse		FAIL

	ldc.i4		0x00000000
	ldsflda		int64 ldind_u4::LOADME2
	ldc.i4		0x4
	add
	ldind.u4
	ceq
	brfalse		FAIL


	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldind_u4{}
