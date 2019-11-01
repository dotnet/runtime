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

.method public static		int32 ret_test_0(int32) {
.maxstack	1
		ldc.i4		0xAAAAAAAA
	ret

}
.method public static		int32 ret_test_1(int32) {
.maxstack	1
		ldc.i4		0x55555555
	ret

}
.method public static		int32 ret_test_2(int32) {
.maxstack	1
		ldc.i4		0x7FFFFFFF
	ret

}
.method public static		int32 ret_test_3(int32) {
.maxstack	1
		ldc.i4		0x00000001
	ret

}
.method public static		int32 ret_test_4(int32) {
.maxstack	1
		ldc.i4		0x00000000
	ret

}
.method public static		int32 ret_test_5(int32) {
.maxstack	1
		ldc.i4		0xFFFFFFFF
	ret

}
.method public static		int32 ret_test_6(int32) {
.maxstack	1
		ldc.i4		0x80000000
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
.maxstack		5
//====== begin testing ======
// -- Even
	ldc.i4		0xAAAAAAAA
	call	int32 _ret::ret_test_0(int32)
	// -- Even
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL

// -- Odd
	ldc.i4		0x55555555
	call	int32 _ret::ret_test_1(int32)
	// -- Odd
	ldc.i4		0x55555555
	ceq
	brfalse		FAIL

// -- Max
	ldc.i4		0x7FFFFFFF
	call	int32 _ret::ret_test_2(int32)
	// -- Max
	ldc.i4		0x7FFFFFFF
	ceq
	brfalse		FAIL

// -- 1
	ldc.i4		0x00000001
	call	int32 _ret::ret_test_3(int32)
	// -- 1
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL

// -- 0
	ldc.i4		0x00000000
	call	int32 _ret::ret_test_4(int32)
	// -- 0
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL

// -- -1
	ldc.i4		0xFFFFFFFF
	call	int32 _ret::ret_test_5(int32)
	// -- -1
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL

// -- Min
	ldc.i4		0x80000000
	call	int32 _ret::ret_test_6(int32)
	// -- Min
	ldc.i4		0x80000000
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



.assembly ldc_ret_i4.exe{}

