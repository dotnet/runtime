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
.class public		_ret {

//---- GLOBAL DATA ----------


//---- METHODS --------------

.method public static		float32 ret_test_0(float32) {
.maxstack	1
		ldc.r4	float32(0x7FC00000)
	ret

}
.method public static		float32 ret_test_1(float32) {
.maxstack	1
		ldc.r4	float32(0x7F800000)
	ret

}
.method public static		float32 ret_test_2(float32) {
.maxstack	1
		ldc.r4	float32(0x7F7FFFFF)
	ret

}
.method public static		float32 ret_test_3(float32) {
.maxstack	1
		ldc.r4	float32(0x3F800000)
	ret

}
.method public static		float32 ret_test_4(float32) {
.maxstack	1
		ldc.r4	float32(0x00000000)
	ret

}
.method public static		float32 ret_test_5(float32) {
.maxstack	1
		ldc.r4	float32(0x80000000)
	ret

}
.method public static		float32 ret_test_6(float32) {
.maxstack	1
		ldc.r4	float32(0xBF800000)
	ret

}
.method public static		float32 ret_test_7(float32) {
.maxstack	1
		ldc.r4	float32(0xFF7FFFFF)
	ret

}
.method public static		float32 ret_test_8(float32) {
.maxstack	1
		ldc.r4	float32(0xFF800000)
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
	ldc.r4	float32(0x7FC00000)
	call	float32 _ret::ret_test_0(float32)
	// -- NaN
	ldc.r4	float32(0x7F800000)
	cgt.un
	brfalse		FAIL

// -- inf
	ldc.r4	float32(0x7F800000)
	call	float32 _ret::ret_test_1(float32)
	// -- inf
	ldc.r4	float32(0x7F800000)
	ceq
	brfalse		FAIL

// -- max
	ldc.r4	float32(0x7F7FFFFF)
	call	float32 _ret::ret_test_2(float32)
	// -- max
	ldc.r4	float32(0x7F7FFFFF)
	ceq
	brfalse		FAIL

// -- 1
	ldc.r4	float32(0x3F800000)
	call	float32 _ret::ret_test_3(float32)
	// -- 1
	ldc.r4	float32(0x3F800000)
	ceq
	brfalse		FAIL

// -- 0
	ldc.r4	float32(0x00000000)
	call	float32 _ret::ret_test_4(float32)
	// -- 0
	ldc.r4	float32(0x00000000)
	ceq
	brfalse		FAIL

// -- -0
	ldc.r4	float32(0x80000000)
	call	float32 _ret::ret_test_5(float32)
	// -- -0
	ldc.r4	float32(0x80000000)
	ceq
	brfalse		FAIL

// -- -1
	ldc.r4	float32(0xBF800000)
	call	float32 _ret::ret_test_6(float32)
	// -- -1
	ldc.r4	float32(0xBF800000)
	ceq
	brfalse		FAIL

// -- min
	ldc.r4	float32(0xFF7FFFFF)
	call	float32 _ret::ret_test_7(float32)
	// -- min
	ldc.r4	float32(0xFF7FFFFF)
	ceq
	brfalse		FAIL

// -- -inf
	ldc.r4	float32(0xFF800000)
	call	float32 _ret::ret_test_8(float32)
	// -- -inf
	ldc.r4	float32(0xFF800000)
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

.assembly ldc_ret_r4.exe{}

