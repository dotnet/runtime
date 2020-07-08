// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public _starg {

.method public static	int32 args(float64,float64) {
.maxstack	10
.locals	(int32)
	ldarg	0
	ldarg	1
	ldarg	0
	ldarg	1
	starg	0
	starg	1
	ldarg	0
	ceq
	stloc	0
	ldarg	1
	ceq
	ldloc	0
	add
	ldc.i4		0x2
	ceq
	ret
}


.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	ldc.r8		float64(0x3FF7FFFFFFFFFFFF)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _starg::args(float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF7FFFFFFFFFFFF)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	call	int32 _starg::args(float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF7FFFFFFFFFFFF)
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _starg::args(float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF7FFFFFFFFFFFF)
	ldc.r8		float64(0x8000000000000000)
	call	int32 _starg::args(float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF7FFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _starg::args(float64,float64)	
	brfalse		FAIL

	ldc.r8		float64(0x3FF7FFFFFFFFFFFF)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _starg::args(float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF7FFFFFFFFFFFF)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	call	int32 _starg::args(float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF7FFFFFFFFFFFF)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _starg::args(float64,float64)
	brfalse		FAIL



	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly starg_r8.exe{}
