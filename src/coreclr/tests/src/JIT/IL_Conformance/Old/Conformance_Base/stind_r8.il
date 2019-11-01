// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}



.class public Stind_r8 {



.field public static int64 LOADME
.field public static int64 LOADME2
.field public static int64 LOADME3
.field public static int64 LOADME4

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldsflda		int64 Stind_r8::LOADME
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	stind.r8

	ldsflda		int64 Stind_r8::LOADME
	ldc.i4		0x8
	add
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	stind.r8

	ldsflda		int64 Stind_r8::LOADME
	ldc.i4		0x10
	add
	ldc.r8		float64(0xBFF0000000000000)
	stind.r8

	ldsflda		int64 Stind_r8::LOADME
	ldc.i4		0x18
	add
	ldc.r8		float64(0x3FF0000000000000)
	stind.r8


	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldsflda		int64 Stind_r8::LOADME
	ldind.r8
	ceq
	brfalse		FAIL

	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldsflda		int64 Stind_r8::LOADME
	ldc.i4		0x8
	add
	ldind.r8
	ceq
	brfalse		FAIL

	ldc.r8		float64(0xBFF0000000000000)
	ldsflda		int64 Stind_r8::LOADME
	ldc.i4		0x10
	add
	ldind.r8
	ceq
	brfalse		FAIL

	ldc.r8		float64(0x3FF0000000000000)
	ldsflda		int64 Stind_r8::LOADME
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

.assembly stind_r8.exe{}
