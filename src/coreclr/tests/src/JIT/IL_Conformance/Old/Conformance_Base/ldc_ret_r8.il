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
.class public		_ret {

//---- GLOBAL DATA ----------


//---- METHODS --------------

.method public static		float64 ret_test_0(float64) {
.maxstack	1
		ldc.r8	float64(0x7FF8000000000000)
	ret

}
.method public static		float64 ret_test_1(float64) {
.maxstack	1
		ldc.r8	float64(0x7FF0000000000000)
	ret

}
.method public static		float64 ret_test_2(float64) {
.maxstack	1
		ldc.r8	float64(0x7FEFFFFFFFFFFFFF)
	ret

}
.method public static		float64 ret_test_3(float64) {
.maxstack	1
		ldc.r8	float64(0x3FF0000000000000)
	ret

}
.method public static		float64 ret_test_4(float64) {
.maxstack	1
		ldc.r8	float64(0x0000000000000000)
	ret

}
.method public static		float64 ret_test_5(float64) {
.maxstack	1
		ldc.r8	float64(0x8000000000000000)
	ret

}
.method public static		float64 ret_test_6(float64) {
.maxstack	1
		ldc.r8	float64(0xBFF0000000000000)
	ret

}
.method public static		float64 ret_test_7(float64) {
.maxstack	1
		ldc.r8	float64(0xFFEFFFFFFFFFFFFF)
	ret

}
.method public static		float64 ret_test_8(float64) {
.maxstack	1
		ldc.r8	float64(0xFFF0000000000000)
	ret

}
//---- CONSTRUCTOR ----------
.method public		void _ret() {
.maxstack	0
	ret
}

//---- MAIN -----------------
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		12
//====== begin testing ======

// -- NaN
	ldc.r8	float64(0x7FF8000000000000)
	call	float64 _ret::ret_test_0(float64)
	// -- NaN
	ldc.r8	float64(0x7FF8000000000000)
	cgt.un
	brfalse		FAIL

// -- inf
	ldc.r8	float64(0x7FF0000000000000)
	call	float64 _ret::ret_test_1(float64)
	// -- inf
	ldc.r8	float64(0x7FF0000000000000)
	ceq
	brfalse		FAIL

// -- max
	ldc.r8	float64(0x7FEFFFFFFFFFFFFF)
	call	float64 _ret::ret_test_2(float64)
	// -- max
	ldc.r8	float64(0x7FEFFFFFFFFFFFFF)
	ceq
	brfalse		FAIL

// -- 1
	ldc.r8	float64(0x3FF0000000000000)
	call	float64 _ret::ret_test_3(float64)
	// -- 1
	ldc.r8	float64(0x3FF0000000000000)
	ceq
	brfalse		FAIL

// -- 0
	ldc.r8	float64(0x0000000000000000)
	call	float64 _ret::ret_test_4(float64)
	// -- 0
	ldc.r8	float64(0x0000000000000000)
	ceq
	brfalse		FAIL

// -- -0
	ldc.r8	float64(0x8000000000000000)
	call	float64 _ret::ret_test_5(float64)
	// -- -0
	ldc.r8	float64(0x8000000000000000)
	ceq
	brfalse		FAIL

// -- -1
	ldc.r8	float64(0xBFF0000000000000)
	call	float64 _ret::ret_test_6(float64)
	// -- -1
	ldc.r8	float64(0xBFF0000000000000)
	ceq
	brfalse		FAIL

// -- min
	ldc.r8	float64(0xFFEFFFFFFFFFFFFF)
	call	float64 _ret::ret_test_7(float64)
	// -- min
	ldc.r8	float64(0xFFEFFFFFFFFFFFFF)
	ceq
	brfalse		FAIL

// -- -inf
	ldc.r8	float64(0xFFF0000000000000)
	call	float64 _ret::ret_test_8(float64)
	// -- -inf
	ldc.r8	float64(0xFFF0000000000000)
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

.assembly ldc_ret_r8.exe{}

