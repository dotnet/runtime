// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public		_neg {



.method public static		int32 i8(int64,int64) {
.maxstack	2
	ldarg	0
	neg
	ldarg	1
	ceq
	ret
}


.method public		void _neg() {
.maxstack	0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
	
	ldc.i8		0x8000000000000000
	ldc.i8		0x8000000000000000
	call	int32 _neg::i8(int64,int64)
	brfalse		FAIL
	ldc.i8		0x8000000000000001
	ldc.i8		0x7FFFFFFFFFFFFFFF
	call	int32 _neg::i8(int64,int64)
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000001
	call	int32 _neg::i8(int64,int64)
	brfalse		FAIL
	ldc.i8		0x0000000000000000
	ldc.i8		0x0000000000000000
	call	int32 _neg::i8(int64,int64)
	brfalse		FAIL
	ldc.i8		0x0000000000000001
	ldc.i8		0xFFFFFFFFFFFFFFFF
	call	int32 _neg::i8(int64,int64)
	brfalse		FAIL
	ldc.i8		0x7FFFFFFFFFFFFFFE
	ldc.i8		0x8000000000000002
	call	int32 _neg::i8(int64,int64)
	brfalse		FAIL
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0x8000000000000001
	call	int32 _neg::i8(int64,int64)
	brfalse		FAIL
	ldc.i8		0x5555555555555555
	ldc.i8		0xAAAAAAAAAAAAAAAB
	call	int32 _neg::i8(int64,int64)
	brfalse		FAIL
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x5555555555555556
	call	int32 _neg::i8(int64,int64)
	brfalse		FAIL




PASS:
	ldc.i4 100
	br		END

FAIL:
	ldc.i4	0x0000000000000000

END:
	ret
}
}

.assembly neg_i8{}
