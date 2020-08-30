// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _starg {

.method public static	int32 args(float32,float32) {
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
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x023A05FC)
	call	int32 _starg::args(float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x023A05FC)
	call	int32 _starg::args(float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x023A05FC)
	call	int32 _starg::args(float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x023A05FC)
	call	int32 _starg::args(float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x023A05FC)
	call	int32 _starg::args(float32,float32)	
	brfalse		FAIL

	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x023A05FC)
	call	int32 _starg::args(float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x023A05FC)
	call	int32 _starg::args(float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x023A05FC)
	call	int32 _starg::args(float32,float32)
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly starg_r4.exe{}
