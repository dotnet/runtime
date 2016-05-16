// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public		_neg {



.method public static		int32 r4(float32,float32) {
.maxstack	8
	ldarg	0
	neg
	dup
	call	int32 _neg::checkfinite(float32)
	ldc.i4		0x0
	ceq
	brtrue		COMPARE
	call	int32 _neg::checkfinite(float32)
	ldarg	1
	call	int32 _neg::checkfinite(float32)
	ceq
	br			END
COMPARE:
	ldarg	1
	ceq
END:
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

.method public		void _neg() {
.maxstack	0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
	
	ldc.r4	float32(0xFF800000)
	ldc.r4	float32(0x7F800000)
	call	int32 _neg::r4(float32,float32)
	brfalse	FAIL
	ldc.r4	float32(0xFF7FFFFF)
	ldc.r4	float32(0x7F7FFFFF)
	call	int32 _neg::r4(float32,float32)
	brfalse	FAIL
	ldc.r4	float32(0xBF800000)
	ldc.r4	float32(0x3F800000)
	call	int32 _neg::r4(float32,float32)
	brfalse	FAIL
	ldc.r4	float32(0x80000000)
	ldc.r4	float32(0x00000000)
	call	int32 _neg::r4(float32,float32)
	brfalse	FAIL
	ldc.r4	float32(0x00000000)
	ldc.r4	float32(0x80000000)
	call	int32 _neg::r4(float32,float32)
	brfalse	FAIL
	ldc.r4	float32(0x3F800000)
	ldc.r4	float32(0xBF800000)
	call	int32 _neg::r4(float32,float32)
	brfalse	FAIL
	ldc.r4	float32(0x7F7FFFFF)
	ldc.r4	float32(0xFF7FFFFF)
	call	int32 _neg::r4(float32,float32)
	brfalse	FAIL
	ldc.r4	float32(0x7F800000)
	ldc.r4	float32(0xFF800000)
	call	int32 _neg::r4(float32,float32)
	brfalse	FAIL
	ldc.r4	float32(0x7FC00000)
	ldc.r4	float32(0x7FC00000)
	call	int32 _neg::r4(float32,float32)
	brfalse	FAIL


PASS:
	ldc.i4 100
	br		END

FAIL:
	ldc.i4	0x00000000

END:
	ret
}
}



.assembly neg_r4{}
