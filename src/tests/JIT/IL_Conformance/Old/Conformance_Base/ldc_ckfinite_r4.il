// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

//
//======================================

//---- CLASS ----------------
.class public		_ckfinite {

//---- GLOBAL DATA ----------


//---- METHODS --------------



.method public static int32 r4_0(float32) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r4		float32(0xFF800000)
	ckfinite
	//- No exception - FAIL
		ldc.r4		float32(0xFF800000)
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse		FAIL
	ldc.i4	0x11111111
	ret
aHandler:
	//- Got an ArithmeticException
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
.method public static int32 r4_1(float32) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r4		float32(0xFF7FFFFF)
	ckfinite
	//- No exception - FAIL
		ldc.r4		float32(0xFF7FFFFF)
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse		FAIL
	ldc.i4	0x11111111
	ret
aHandler:
	//- Got an ArithmeticException
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
.method public static int32 r4_2(float32) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r4		float32(0xBF800000)
	ckfinite
	//- No exception - FAIL
		ldc.r4		float32(0xBF800000)
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse		FAIL
	ldc.i4	0x11111111
	ret
aHandler:
	//- Got an ArithmeticException
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
.method public static int32 r4_3(float32) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r4		float32(0x80000000)
	ckfinite
	//- No exception - FAIL
		ldc.r4		float32(0x80000000)
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse		FAIL
	ldc.i4	0x11111111
	ret
aHandler:
	//- Got an ArithmeticException
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
.method public static int32 r4_4(float32) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r4		float32(0x00000000)
	ckfinite
	//- No exception - FAIL
		ldc.r4		float32(0x00000000)
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse		FAIL
	ldc.i4	0x11111111
	ret
aHandler:
	//- Got an ArithmeticException
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
.method public static int32 r4_5(float32) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r4		float32(0x3F800000)
	ckfinite
	//- No exception - FAIL
		ldc.r4		float32(0x3F800000)
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse		FAIL
	ldc.i4	0x11111111
	ret
aHandler:
	//- Got an ArithmeticException
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
.method public static int32 r4_6(float32) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r4		float32(0x7F7FFFFF)
	ckfinite
	//- No exception - FAIL
		ldc.r4		float32(0x7F7FFFFF)
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse		FAIL
	ldc.i4	0x11111111
	ret
aHandler:
	//- Got an ArithmeticException
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
.method public static int32 r4_7(float32) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r4		float32(0x7F800000)
	ckfinite
	//- No exception - FAIL
		ldc.r4		float32(0x7F800000)
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse		FAIL
	ldc.i4	0x11111111
	ret
aHandler:
	//- Got an ArithmeticException
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
.method public static int32 r4_8(float32) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r4		float32(0x7FC00000)
	ckfinite
	//- No exception - FAIL
		ldc.r4		float32(0x7FC00000)
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse		FAIL
	ldc.i4	0x11111111
	ret
aHandler:
	//- Got an ArithmeticException
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
//---- CONSTRUCTOR ----------
.method public		void _ckfinite() {
.maxstack	0
	ret
}

//---- MAIN -----------------
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
//====== begin testing ======
	//- 32 bit -inf
	ldc.r4		float32(0xFF800000)
	call	int32 _ckfinite::r4_0(float32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	//- 32 bit -min
	ldc.r4		float32(0xFF7FFFFF)
	call	int32 _ckfinite::r4_1(float32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	//- 32 bit -1
	ldc.r4		float32(0xBF800000)
	call	int32 _ckfinite::r4_2(float32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	//- 32 bit -0
	ldc.r4		float32(0x80000000)
	call	int32 _ckfinite::r4_3(float32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	//- 32 bit +0
	ldc.r4		float32(0x00000000)
	call	int32 _ckfinite::r4_4(float32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	//- 32 bit +1
	ldc.r4		float32(0x3F800000)
	call	int32 _ckfinite::r4_5(float32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	//- 32 bit +max
	ldc.r4		float32(0x7F7FFFFF)
	call	int32 _ckfinite::r4_6(float32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	//- 32 bit +inf
	ldc.r4		float32(0x7F800000)
	call	int32 _ckfinite::r4_7(float32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	//- 32 bit NaN
	ldc.r4		float32(0x7FC00000)
	call	int32 _ckfinite::r4_8(float32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL

//====== end testing ========

//---- branch here on pass --
PASS:
	ldc.i4 100
	br		END

//---- branch here on fail --
FAIL:
	ldc.i4 101

//---- return the result ----
END:
	ret
//---- END OF METHOD --------
}
//---- EOF ------------------
}

.assembly ldc_ckfinite_r4.exe{}

