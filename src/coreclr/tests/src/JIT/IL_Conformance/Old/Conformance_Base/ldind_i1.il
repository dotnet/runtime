// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public ldind_i1 {


.field public static int32 LOADME

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldsflda		int32 ldind_i1::LOADME
	ldc.i4		0x12789AEF
	stind.i4

	ldc.i4		0xFFFFFFEF
	ldsflda		int32 ldind_i1::LOADME
	ldind.i1
	ceq
	brfalse		FAIL

	ldc.i4		0xFFFFFF9A
	ldsflda		int32 ldind_i1::LOADME
	ldc.i4		0x1
	add
	ldind.i1
	ceq
	brfalse		FAIL

	ldc.i4		0x00000078
	ldsflda		int32 ldind_i1::LOADME
	ldc.i4		0x2
	add
	ldind.i1
	ceq
	brfalse		FAIL

	ldc.i4		0x00000012
	ldsflda		int32 ldind_i1::LOADME
	ldc.i4		0x3
	add
	ldind.i1
	ceq
	brfalse		FAIL


	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldind_i1.exe{}
