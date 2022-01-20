// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.assembly 'or'{}

.class public _or {

.method public static int32 _or(int32,int32) {
.maxstack	2
	ldarg	0
	ldarg	1
	or
	ret
}
.method public static int64 _or(int64,int64) {
.maxstack	2
	ldarg	0
	ldarg	1
	or
	ret
}
.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
	ldc.i4	0xCCCCCCCC
	ldc.i4	0xF0F0F0F0
	call	int32 _or::_or(int32,int32)
	ldc.i4	0xFCFCFCFC
	ceq
	brfalse	FAIL
	ldc.i8	0xCCCCCCCCCCCCCCCC
	ldc.i8	0xF0F0F0F0F0F0F0F0
	call	int64 _or::_or(int64,int64)
	ldc.i8	0xFCFCFCFCFCFCFCFC
	ceq
	brfalse	FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
