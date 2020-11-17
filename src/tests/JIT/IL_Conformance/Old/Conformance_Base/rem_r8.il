// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public _rem {
.method public static	int32 _rem(float64,float64,float64) {
.maxstack		3
.locals	(int32)
	ldarg	0
	ldarg	1
	rem
	dup
	call	int32 _rem::checkfinite(float64)
	ldc.i4		0x0
	ceq
	brtrue		COMPARE

	call	int32 _rem::checkfinite(float64)

	stloc	0	
	ldloc	0	
	ldc.i4		0xFF
	ceq
	brtrue		FAIL	// error while determining type

	ldarg	2
	call	int32 _rem::checkfinite(float64)

	ldloc	0
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
.maxstack		3
.locals	(class [mscorlib]System.ArithmeticException)


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
	ldc.r8		float64(0xFFF0000000000000)
	ceq
	brtrue		NINF

	ldarg	0
	ldc.r8		float64(0x7FF0000000000000)
	ceq
	brtrue		PINF

NaN:
	ldc.i4		0x00000001
	br			FINISH

NINF:
	ldc.i4		0x00000002
	br			FINISH

PINF:
	ldc.i4		0x00000003
	br			FINISH

FAIL:						// bogus exception occurred
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
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL1

	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL


	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL


	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xBFE0000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL


	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL


	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL


	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL


	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x8000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL


	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL


	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL


	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL


	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL


	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL


	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x3FE0000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL


	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x3FE0000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x3FE0000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL


	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _rem::_rem(float64,float64,float64)
	brfalse		FAIL

	ldc.i4 100
	ret

FAIL:
	ldc.i4	0x0
	ret

FAIL1:
	ldc.i4	0x1
	ret

}
}
.assembly rem_r8.exe{}
