// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public _mul {
.method public static	int32 _mul(float32,float32,float32) {
.maxstack		5
	ldarg	0
	ldarg	1
	mul
	dup
	call	int32 _mul::checkfinite(float32)
	ldc.i4		0x0
	ceq
	brtrue		COMPARE
	call	int32 _mul::checkfinite(float32)
	ldarg	2
	call	int32 _mul::checkfinite(float32)
	ceq
	br			FINISH
COMPARE:
	ldarg	2
	ceq
FINISH:
	ret
}
.method public static	int32 checkfinite(float32) {
.maxstack		3
.locals		(class [mscorlib]System.ArithmeticException)
try_start:
	ldarg	0
	ckfinite
	pop
	leave.s	try_end
try_end:
	ldc.i4		0x0
	br			FINISH
ae:
	isinst		[mscorlib]System.ArithmeticException
	stloc			0x0
	leave			HEnd
HEnd:
	ldloc			0x0
	brfalse		FAIL
	ldarg	0
	ldc.r4		float32(0xFF800000)
	ceq
	brtrue		NINF
	ldarg	0
	ldc.r4		float32(0x7F800000)
	ceq
	brtrue		PINF
	br			NaN
NINF:
	ldc.i4		0x00000002
	br			FINISH
PINF:
	ldc.i4		0x00000003
	br			FINISH
NaN:
	ldc.i4		0x00000001
	br			FINISH
FAIL:
	ldc.i4		0xFF
	br			FINISH
FINISH:
	ret
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler ae to HEnd
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x7F7FFFFF)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0xFF7FFFFF)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x7F7FFFFF)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x3F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0xBF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0xFF7FFFFF)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0xBFC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x0000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x0000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0xFF7FFFFF)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0xBF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x3F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7F7FFFFF)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x3FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0xFF7FFFFF)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x7F7FFFFF)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL

	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0xFF800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0xBFC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x00000000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x3FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7F800000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x40100000)
	call	int32 _mul::_mul(float32,float32,float32)
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly mul_r4.exe{}
