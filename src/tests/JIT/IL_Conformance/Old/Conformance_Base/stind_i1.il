// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public Stind_i1 {


.field public static int32 FOO

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldsflda		int32 Stind_i1::FOO
	ldc.i4		0xEF
	stind.i1

	ldsflda		int32 Stind_i1::FOO
	ldc.i4		0x1
	add
	ldc.i4		0x9A
	stind.i1

	ldsflda		int32 Stind_i1::FOO
	ldc.i4		0x2
	add
	ldc.i4		0x78
	stind.i1

	ldsflda		int32 Stind_i1::FOO
	ldc.i4		0x3
	add
	ldc.i4		0x12
	stind.i1

	ldsflda		int32 Stind_i1::FOO
	ldind.i4
	ldc.i4		0x12789AEF
	ceq
	brfalse		FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly stind_i1.exe{}
