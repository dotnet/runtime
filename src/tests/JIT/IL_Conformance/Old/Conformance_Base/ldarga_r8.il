// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _ldarga {

.method public static	int32 args(float64,float64) {
.maxstack	5
	ldarga 0
	ldind.r8
	ldarga 1
	ldind.r8
	ceq
	ret
}


.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _ldarga::args(float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	call	int32 _ldarga::args(float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _ldarga::args(float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x8000000000000000)
	call	int32 _ldarga::args(float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _ldarga::args(float64,float64)	
	brfalse		FAIL

	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _ldarga::args(float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	call	int32 _ldarga::args(float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _ldarga::args(float64,float64)
	brfalse		FAIL



	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldarga_r8{}
