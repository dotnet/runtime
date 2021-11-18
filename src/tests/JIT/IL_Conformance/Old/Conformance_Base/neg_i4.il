// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public		_neg {



.method public static		int32 i4(int32,int32) {
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
	
	ldc.i4		0x80000000
	ldc.i4		0x80000000
	call	int32 _neg::i4(int32,int32)
	brfalse		FAIL
	ldc.i4		0x80000001
	ldc.i4		0x7FFFFFFF
	call	int32 _neg::i4(int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	call	int32 _neg::i4(int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 _neg::i4(int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	call	int32 _neg::i4(int32,int32)
	brfalse		FAIL
	ldc.i4		0x7FFFFFFE
	ldc.i4		0x80000002
	call	int32 _neg::i4(int32,int32)
	brfalse		FAIL
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x80000001
	call	int32 _neg::i4(int32,int32)
	brfalse		FAIL
	ldc.i4		0x55555555
	ldc.i4		0xAAAAAAAB
	call	int32 _neg::i4(int32,int32)
	brfalse		FAIL
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x55555556
	call	int32 _neg::i4(int32,int32)
	brfalse		FAIL




PASS:
	ldc.i4 100
	br		END

FAIL:
	ldc.i4	0x00000000

END:
	ret
}
}



.assembly neg_i4{}
