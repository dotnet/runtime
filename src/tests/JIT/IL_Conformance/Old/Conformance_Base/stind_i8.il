// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public Stind_i8 {



.field public static int64 FOO
.field public static int64 FOO2
.field public static int64 FOO3
.field public static int64 FOO4

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldsflda		int64 Stind_i8::FOO
	ldc.i8		0x0123456789ABCDEF
	stind.i8

	ldsflda		int64 Stind_i8::FOO
	ldc.i4		0x8
	add
	ldc.i8		0x0011223344556677
	stind.i8

	ldsflda		int64 Stind_i8::FOO
	ldc.i4		0x10
	add
	ldc.i8		0x8899AABBCCDDEEFF
	stind.i8

	ldsflda		int64 Stind_i8::FOO
	ldc.i4		0x18
	add
	ldc.i8		0xFFFFFFFF00000000
	stind.i8

	ldsflda		int64 Stind_i8::FOO
	ldind.i8
	ldc.i8		0x0123456789ABCDEF
	ceq
	brfalse		FAIL

	ldsflda		int64 Stind_i8::FOO
	ldc.i4		0x8
	add
	ldind.i8
	ldc.i8		0x0011223344556677
	ceq
	brfalse		FAIL	

	ldsflda		int64 Stind_i8::FOO
	ldc.i4		0x10
	add
	ldind.i8
	ldc.i8		0x8899AABBCCDDEEFF
	ceq
	brfalse		FAIL	

	ldsflda		int64 Stind_i8::FOO
	ldc.i4		0x18
	add
	ldind.i8
	ldc.i8		0xFFFFFFFF00000000
	ceq
	brfalse		FAIL	

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly stind_i8.exe{}
