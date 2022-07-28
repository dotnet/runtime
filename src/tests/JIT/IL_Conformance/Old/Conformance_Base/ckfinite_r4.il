// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public		_ckfinite {



.method public static int32 r4(float32) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
	ldarg	0
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
	ldc.r4		float32(0xFF800000)
	call	int32 _ckfinite::r4(float32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.r4		float32(0xFF7FFFFF)
	call	int32 _ckfinite::r4(float32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.r4		float32(0xBF800000)
	call	int32 _ckfinite::r4(float32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.r4		float32(0x80000000)
	call	int32 _ckfinite::r4(float32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.r4		float32(0x00000000)
	call	int32 _ckfinite::r4(float32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.r4		float32(0x3F800000)
	call	int32 _ckfinite::r4(float32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.r4		float32(0x7F7FFFFF)
	call	int32 _ckfinite::r4(float32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.r4		float32(0x7F800000)
	call	int32 _ckfinite::r4(float32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.r4		float32(0x7FC00000)
	call	int32 _ckfinite::r4(float32)
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

.assembly ckfinite_r4{}
