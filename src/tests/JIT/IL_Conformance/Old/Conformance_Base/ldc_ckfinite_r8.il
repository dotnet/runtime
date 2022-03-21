// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


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



.method public static int32 r8_0(float64) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r8		float64(0xFFF0000000000000)
	//widen the r8 to whatever the float64 is supposed to be on the stak
	conv.r8
	ckfinite
	//- No exception - FAIL
		ldc.r8		float64(0xFFF0000000000000)
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
.method public static int32 r8_1(float64) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	//widen the r8 to whatever the float64 is supposed to be on the stak
	conv.r8
	ckfinite
	//- No exception - FAIL
		ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
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
.method public static int32 r8_2(float64) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r8		float64(0xBFF0000000000000)
	//widen the r8 to whatever the float64 is supposed to be on the stak
	conv.r8
	ckfinite
	//- No exception - FAIL
		ldc.r8		float64(0xBFF0000000000000)
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
.method public static int32 r8_3(float64) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r8		float64(0x8000000000000000)
	//widen the r8 to whatever the float64 is supposed to be on the stak
	conv.r8
	ckfinite
	//- No exception - FAIL
		ldc.r8		float64(0x8000000000000000)
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
.method public static int32 r8_4(float64) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r8		float64(0x0000000000000000)
	//widen the r8 to whatever the float64 is supposed to be on the stak
	conv.r8
	ckfinite
	//- No exception - FAIL
		ldc.r8		float64(0x0000000000000000)
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
.method public static int32 r8_5(float64) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r8		float64(0x3FF0000000000000)
	//widen the r8 to whatever the float64 is supposed to be on the stak
	conv.r8
	ckfinite
	//- No exception - FAIL
		ldc.r8		float64(0x3FF0000000000000)
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
.method public static int32 r8_6(float64) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	//widen the r8 to whatever the float64 is supposed to be on the stak
	conv.r8
	ckfinite
	//- No exception - FAIL
		ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
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
.method public static int32 r8_7(float64) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r8		float64(0x7FF0000000000000)
	//widen the r8 to whatever the float64 is supposed to be on the stak
	conv.r8
	ckfinite
	//- No exception - FAIL
		ldc.r8		float64(0x7FF0000000000000)
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
.method public static int32 r8_8(float64) {
.locals		(class [mscorlib]System.ArithmeticException,int32)
.maxstack	5
try_start:
		ldc.r8		float64(0x7FF8000000000000)
	//widen the r8 to whatever the float64 is supposed to be on the stak
	conv.r8
	ckfinite
	//- No exception - FAIL
		ldc.r8		float64(0x7FF8000000000000)
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
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _ckfinite::r8_0(float64)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	//- 32 bit -min
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	call	int32 _ckfinite::r8_1(float64)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	//- 32 bit -1
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _ckfinite::r8_2(float64)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	//- 32 bit -0
	ldc.r8		float64(0x8000000000000000)
	call	int32 _ckfinite::r8_3(float64)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	//- 32 bit +0
	ldc.r8		float64(0x0000000000000000)
	call	int32 _ckfinite::r8_4(float64)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	//- 32 bit +1
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _ckfinite::r8_5(float64)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	//- 32 bit +max
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	call	int32 _ckfinite::r8_6(float64)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	//- 32 bit +inf
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _ckfinite::r8_7(float64)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	//- 32 bit NaN
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _ckfinite::r8_8(float64)
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

.assembly ldc_ckfinite_r8{}

