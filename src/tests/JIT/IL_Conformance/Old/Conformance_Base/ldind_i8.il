// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public ldind_i8 {



.field public static int64 LOADME
.field public static int64 LOADME2
.field public static int64 LOADME3
.field public static int64 LOADME4

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20

	ldsflda		int64 ldind_i8::LOADME
	ldc.i8		0x0000111122223333
	stind.i8
	ldsflda		int64 ldind_i8::LOADME
	ldc.i4		8
	add
	ldc.i8		0x4444555566667777
	stind.i8
	ldsflda		int64 ldind_i8::LOADME
	ldc.i4		16
	add
	ldc.i8		0x88889999AAAABBBB
	stind.i8
	ldsflda		int64 ldind_i8::LOADME
	ldc.i4		24
	add
	ldc.i8		0xCCCCDDDDEEEEFFFF
	stind.i8


	ldc.i8		0x0000111122223333
	ldsflda		int64 ldind_i8::LOADME
	ldind.i8
	ceq
	brfalse		FAIL

	ldc.i8		0x4444555566667777
	ldsflda		int64 ldind_i8::LOADME
	ldc.i4		8
	add
	ldind.i8
	ceq
	brfalse		FAIL

	ldc.i8		0x88889999AAAABBBB
	ldsflda		int64 ldind_i8::LOADME
	ldc.i4		16
	add
	ldind.i8
	ceq
	brfalse		FAIL

	ldc.i8		0xCCCCDDDDEEEEFFFF
	ldsflda		int64 ldind_i8::LOADME
	ldc.i4		24
	add
	ldind.i8
	ceq
	brfalse		FAIL


	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldind_i8{}
