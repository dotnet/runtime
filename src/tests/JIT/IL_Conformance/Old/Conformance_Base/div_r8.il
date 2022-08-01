// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _div {
.method public static	int32 _div(float64,float64,float64) {
.maxstack		3
	ldarg	0
	conv.r8
	ldarg	1
	conv.r8
	div
	dup
	call	int32 _div::checkfinite(float64)
	ldc.i4		0x0
	ceq
	brtrue		COMPARE
	call	int32 _div::checkfinite(float64)
	ldarg	2
	conv.r8
	call	int32 _div::checkfinite(float64)
	ceq
	brfalse		FAIL
	ldc.i4		0x1
	br			FINISH
COMPARE:
	ldarg	2
	ceq
	brfalse		FAIL
	ldc.i4		0x1
	br			FINISH
FAIL:
	ldc.i4		0x0
	br			FINISH
FINISH:
	ret
}
.method public static	int32 checkfinite(float64) {
.locals		(class [mscorlib]System.Exception)
.maxstack		3
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
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
	ldarg	0
	conv.r8
	ldc.r8		float64(0xFFF0000000000000)
	ceq
	brtrue		NINF
	ldarg	0
	conv.r8
	ldc.r8		float64(0x7FF0000000000000)
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
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xFFE5555555555555)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0004000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8004000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xBFE5555555555555)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8004000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0004000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x3FE5555555555555)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FE5555555555555)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8006000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xBFF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0006000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly div_r8{}
