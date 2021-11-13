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
.class public		_neg {

//---- GLOBAL DATA ----------


//---- METHODS --------------


.method public static		int32 i8_0(int64,int64) {
.maxstack	2
		ldc.i8		0x8000000000000000
	neg
		ldc.i8		0x8000000000000000
	ceq
	ret

}
.method public static		int32 i8_1(int64,int64) {
.maxstack	2
		ldc.i8		0x8000000000000001
	neg
		ldc.i8		0x7FFFFFFFFFFFFFFF
	ceq
	ret

}
.method public static		int32 i8_2(int64,int64) {
.maxstack	2
		ldc.i8		0xFFFFFFFFFFFFFFFF
	neg
		ldc.i8		0x0000000000000001
	ceq
	ret

}
.method public static		int32 i8_3(int64,int64) {
.maxstack	2
		ldc.i8		0x0000000000000000
	neg
		ldc.i8		0x0000000000000000
	ceq
	ret

}
.method public static		int32 i8_4(int64,int64) {
.maxstack	2
		ldc.i8		0x0000000000000001
	neg
		ldc.i8		0xFFFFFFFFFFFFFFFF
	ceq
	ret

}
.method public static		int32 i8_5(int64,int64) {
.maxstack	2
		ldc.i8		0x7FFFFFFFFFFFFFFE
	neg
		ldc.i8		0x8000000000000002
	ceq
	ret

}
.method public static		int32 i8_6(int64,int64) {
.maxstack	2
		ldc.i8		0x7FFFFFFFFFFFFFFF
	neg
		ldc.i8		0x8000000000000001
	ceq
	ret

}
.method public static		int32 i8_7(int64,int64) {
.maxstack	2
		ldc.i8		0x5555555555555555
	neg
		ldc.i8		0xAAAAAAAAAAAAAAAB
	ceq
	ret

}
.method public static		int32 i8_8(int64,int64) {
.maxstack	2
		ldc.i8		0xAAAAAAAAAAAAAAAA
	neg
		ldc.i8		0x5555555555555556
	ceq
	ret

}
//---- CONSTRUCTOR ----------
.method public		void _neg() {
.maxstack	0
	ret
}

//---- MAIN -----------------
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
//====== begin testing ======
	
// -- Min
	ldc.i8		0x8000000000000000
	ldc.i8		0x8000000000000000
	call	int32 _neg::i8_0(int64,int64)
	brfalse		FAIL
// -- (Min + 1)
	ldc.i8		0x8000000000000001
	ldc.i8		0x7FFFFFFFFFFFFFFF
	call	int32 _neg::i8_1(int64,int64)
	brfalse		FAIL
// -- -1
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000001
	call	int32 _neg::i8_2(int64,int64)
	brfalse		FAIL
// -- 0
	ldc.i8		0x0000000000000000
	ldc.i8		0x0000000000000000
	call	int32 _neg::i8_3(int64,int64)
	brfalse		FAIL
// -- 1
	ldc.i8		0x0000000000000001
	ldc.i8		0xFFFFFFFFFFFFFFFF
	call	int32 _neg::i8_4(int64,int64)
	brfalse		FAIL
// -- (Max - 1)
	ldc.i8		0x7FFFFFFFFFFFFFFE
	ldc.i8		0x8000000000000002
	call	int32 _neg::i8_5(int64,int64)
	brfalse		FAIL
// -- Max
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0x8000000000000001
	call	int32 _neg::i8_6(int64,int64)
	brfalse		FAIL
// -- Odd
	ldc.i8		0x5555555555555555
	ldc.i8		0xAAAAAAAAAAAAAAAB
	call	int32 _neg::i8_7(int64,int64)
	brfalse		FAIL
// -- Even
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x5555555555555556
	call	int32 _neg::i8_8(int64,int64)
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

.assembly ldc_neg_i8{}

