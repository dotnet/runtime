// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public Stind_i4 {



.field public static int64 FOO
.field public static int64 FOO2

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldsflda		int64 Stind_i4::FOO
	ldc.i4		0x89ABCDEF
	stind.i4

	ldsflda		int64 Stind_i4::FOO
	ldc.i4		0x4
	add
	ldc.i4		0x01234567
	stind.i4

	ldsflda		int64 Stind_i4::FOO
	ldc.i4		0x8
	add
	ldc.i4		0xCCDDEEFF
	stind.i4

	ldsflda		int64 Stind_i4::FOO
	ldc.i4		0xC
	add
	ldc.i4		0x00112233
	stind.i4

	ldsflda		int64 Stind_i4::FOO
	ldind.i8
	ldc.i8		0x0123456789ABCDEF
	ceq
	brfalse		FAIL

	ldsflda		int64 Stind_i4::FOO
	ldc.i4		0x8
	add
	ldind.i8
	ldc.i8		0x00112233CCDDEEFF
	ceq
	brfalse		FAIL	

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly stind_i4.exe{}
