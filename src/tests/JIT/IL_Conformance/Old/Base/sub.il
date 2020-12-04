// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern  mscorlib {}
.assembly extern legacy library mscorlib {}
.assembly sub.exe{}

.class public _sub {


.field static native int PTR

.method static int32 i4(int32,int32) {
.maxstack	2
	ldarg	0
	ldarg	1
	sub
	ret
}
.method static int64 i8(int64,int64) {
.maxstack	2
	ldarg	0
	ldarg	1
	sub
	ret
}
.method static float32 r4(float32,float32) {
.maxstack	2
	ldarg	0
	ldarg	1
	sub
	ret
}
.method static float64 r8(float64,float64) {
.maxstack	2
	ldarg	0
	ldarg	1
	sub
	ret
}
.method static native int i(native int,native int) {
.maxstack	2
	ldarg	0
	ldarg	1
	sub
	ret
}

.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	10
	ldc.i4	0xFF
	ldc.i4	0xF0
	call	int32 _sub::i4(int32,int32)
	ldc.i4	0xF
	ceq
	brfalse	FAIL

	ldc.i8	0xFF
	ldc.i8	0xF0
	call	int64 _sub::i8(int64,int64)
	ldc.i8	0xF
	ceq
	brfalse	FAIL

	ldc.r4	5.5
	ldc.r4	1.25
	call	float32 _sub::r4(float32,float32)
	ldc.r4	4.25
	ceq
	brfalse	FAIL	
	
	ldc.r8	5.5
	ldc.r8	1.25
	call	float64 _sub::r8(float64,float64)
	ldc.r8	4.25
	ceq
	brfalse	FAIL

	ldc.i8	8
	conv.i
	stsfld	native int _sub::PTR
	ldsfld	native int _sub::PTR
	ldc.i4	0x00000001
	conv.i
	call	native int _sub::i(native int,native int)
	conv.i8
	ldc.i8	0x00000007
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
