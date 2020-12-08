// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public		_ckfinite {



.method public static int32 r8(float64) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
	ldarg	0
	conv.r8
	ckfinite
	ldarg	0
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse		FAIL
	ldc.i4	0x11111111
	ret
aHandler:
	isinst	[mscorlib]System.ArithmeticException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse	FAIL
	ldc.i4	0xEEEEEEEE
	ret
FAIL:
	ldc.i4	0x00000000
	ret
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd
}



.method public		void _ckfinite() {
.maxstack	0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _ckfinite::r8(float64)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	call	int32 _ckfinite::r8(float64)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _ckfinite::r8(float64)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.r8		float64(0x8000000000000000)
	call	int32 _ckfinite::r8(float64)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.r8		float64(0x0000000000000000)
	call	int32 _ckfinite::r8(float64)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _ckfinite::r8(float64)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	call	int32 _ckfinite::r8(float64)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _ckfinite::r8(float64)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _ckfinite::r8(float64)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL


PASS:
	ldc.i4 100
	br		END

FAIL:
	ldc.i4	0x00000000

END:
	ret
}
}

.assembly ckfinite_r8.exe{}
