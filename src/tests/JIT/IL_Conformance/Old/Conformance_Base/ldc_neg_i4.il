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


.method public static		int32 i4_0(int32,int32) {
.maxstack	2
		ldc.i4		0x80000000
	neg
		ldc.i4		0x80000000
	ceq
	ret

}
.method public static		int32 i4_1(int32,int32) {
.maxstack	2
		ldc.i4		0x80000001
	neg
		ldc.i4		0x7FFFFFFF
	ceq
	ret

}
.method public static		int32 i4_2(int32,int32) {
.maxstack	2
		ldc.i4		0xFFFFFFFF
	neg
		ldc.i4		0x00000001
	ceq
	ret

}
.method public static		int32 i4_3(int32,int32) {
.maxstack	2
		ldc.i4		0x00000000
	neg
		ldc.i4		0x00000000
	ceq
	ret

}
.method public static		int32 i4_4(int32,int32) {
.maxstack	2
		ldc.i4		0x00000001
	neg
		ldc.i4		0xFFFFFFFF
	ceq
	ret

}
.method public static		int32 i4_5(int32,int32) {
.maxstack	2
		ldc.i4		0x7FFFFFFE
	neg
		ldc.i4		0x80000002
	ceq
	ret

}
.method public static		int32 i4_6(int32,int32) {
.maxstack	2
		ldc.i4		0x7FFFFFFF
	neg
		ldc.i4		0x80000001
	ceq
	ret

}
.method public static		int32 i4_7(int32,int32) {
.maxstack	2
		ldc.i4		0x55555555
	neg
		ldc.i4		0xAAAAAAAB
	ceq
	ret

}
.method public static		int32 i4_8(int32,int32) {
.maxstack	2
		ldc.i4		0xAAAAAAAA
	neg
		ldc.i4		0x55555556
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
	ldc.i4		0x80000000
	ldc.i4		0x80000000
	call	int32 _neg::i4_0(int32,int32)
	brfalse		FAIL
// -- (Min + 1)
	ldc.i4		0x80000001
	ldc.i4		0x7FFFFFFF
	call	int32 _neg::i4_1(int32,int32)
	brfalse		FAIL
// -- -1
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	call	int32 _neg::i4_2(int32,int32)
	brfalse		FAIL
// -- 0
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 _neg::i4_3(int32,int32)
	brfalse		FAIL
// -- 1
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	call	int32 _neg::i4_4(int32,int32)
	brfalse		FAIL
// -- (Max - 1)
	ldc.i4		0x7FFFFFFE
	ldc.i4		0x80000002
	call	int32 _neg::i4_5(int32,int32)
	brfalse		FAIL
// -- Max
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x80000001
	call	int32 _neg::i4_6(int32,int32)
	brfalse		FAIL
// -- Odd
	ldc.i4		0x55555555
	ldc.i4		0xAAAAAAAB
	call	int32 _neg::i4_7(int32,int32)
	brfalse		FAIL
// -- Even
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x55555556
	call	int32 _neg::i4_8(int32,int32)
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



.assembly ldc_neg_i4.exe{}

