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

.method public static		int64 ret_test_0(int64) {
.maxstack	1
		ldc.i8		0xAAAAAAAAAAAAAAAA
	ret

}
.method public static		int64 ret_test_1(int64) {
.maxstack	1
		ldc.i8		0x5555555555555555
	ret

}
.method public static		int64 ret_test_2(int64) {
.maxstack	1
		ldc.i8		0x7FFFFFFFFFFFFFFF
	ret

}
.method public static		int64 ret_test_3(int64) {
.maxstack	1
		ldc.i8		0x0000000000000001
	ret

}
.method public static		int64 ret_test_4(int64) {
.maxstack	1
		ldc.i8		0x0000000000000000
	ret

}
.method public static		int64 ret_test_5(int64) {
.maxstack	1
		ldc.i8		0xFFFFFFFFFFFFFFFF
	ret

}
.method public static		int64 ret_test_6(int64) {
.maxstack	1
		ldc.i8		0x8000000000000000
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
.maxstack		9
//====== begin testing ======
// -- Even
	ldc.i8		0xAAAAAAAAAAAAAAAA
	call	int64 _ret::ret_test_0(int64)
	// -- Even
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ceq
	brfalse		FAIL

// -- Odd
	ldc.i8		0x5555555555555555
	call	int64 _ret::ret_test_1(int64)
	// -- Odd
	ldc.i8		0x5555555555555555
	ceq
	brfalse		FAIL

// -- Max
	ldc.i8		0x7FFFFFFFFFFFFFFF
	call	int64 _ret::ret_test_2(int64)
	// -- Max
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ceq
	brfalse		FAIL

// -- 1
	ldc.i8		0x0000000000000001
	call	int64 _ret::ret_test_3(int64)
	// -- 1
	ldc.i8		0x0000000000000001
	ceq
	brfalse		FAIL

// -- 0
	ldc.i8		0x0000000000000000
	call	int64 _ret::ret_test_4(int64)
	// -- 0
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL

// -- -1
	ldc.i8		0xFFFFFFFFFFFFFFFF
	call	int64 _ret::ret_test_5(int64)
	// -- -1
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ceq
	brfalse		FAIL

// -- Min
	ldc.i8		0x8000000000000000
	call	int64 _ret::ret_test_6(int64)
	// -- Min
	ldc.i8		0x8000000000000000
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

.assembly ldc_ret_i8{}

