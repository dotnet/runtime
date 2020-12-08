// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.assembly dup.exe{
}

.class public _dup {


.field public static int32 PTR

.method public static int32 _dup(int32*) {
.maxstack	2
	ldarg	0
	dup
	ceq
	ret
}
.method public static int32 _dup(int32) {
.maxstack	2
	ldarg	0
	dup
	ceq
	ret
}
.method public static int32 _dup(int64) {
.maxstack	2
	ldarg	0
	dup
	ceq
	ret	
}
.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
        ldc.i4          0xFAFAFAFA
        call    int32 _dup::_dup(int32)
        brfalse         FAIL
        ldc.i8          0xFAFAFAFA
        call    int32 _dup::_dup(int64)
        brfalse         FAIL
        ldsflda		int32 _dup::PTR
	call	int32 _dup::_dup(int32*)
	brfalse		FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
