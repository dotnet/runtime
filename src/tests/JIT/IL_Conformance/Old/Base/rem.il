// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.assembly rem.exe{}

.class public _rem {

.method public static  int32 I4(int32,int32,int32) {
.maxstack       3
        ldarg   0
        ldarg   1
        rem
        ldarg   2
        ceq
        ret
}
.method public static  int32 I8(int64,int64,int64) {
.maxstack       3
        ldarg   0
        ldarg   1
        rem
        ldarg   2
        ceq
        ret
}
.method public static  int32 U4(int32,int32,int32) {
.maxstack       3
        ldarg   0
        ldarg   1
        rem.un
        ldarg   2
        ceq
        ret
}
.method public static  int32 U8(int64,int64,int64) {
.maxstack       3
        ldarg   0
        ldarg   1
        rem.un
        ldarg   2
        ceq
        ret
}
.method public static  int32 R4(float32,float32,float32) {
.maxstack       3
        ldarg   0
        ldarg   1
        rem
        ldarg   2
        ceq
        ret
}
.method public static  int32 R8(float64,float64,float64) {
.maxstack       3
        ldarg   0
        ldarg   1
        rem
        ldarg   2
        ceq
        ret
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
	ldc.i4	10
	ldc.i4	4
	ldc.i4	2
	call	int32 _rem::I4(int32,int32,int32)
	brfalse	FAIL
        ldc.i4  10
        ldc.i4  4
        ldc.i4  2
        call    int32 _rem::U4(int32,int32,int32)
        brfalse FAIL
	ldc.i8	10
	ldc.i8	4
	ldc.i8	2
	call	int32 _rem::I8(int64,int64,int64)
	brfalse	FAIL
        ldc.i8  10
        ldc.i8  4
        ldc.i8  2
        call    int32 _rem::U8(int64,int64,int64)
        brfalse FAIL
	ldc.r4	3.5
	ldc.r4	1
	ldc.r4	0.5
	call	int32 _rem::R4(float32,float32,float32)
	brfalse	FAIL	
        ldc.r8  9.25
        ldc.r8  1.5
        ldc.r8  .25
        call    int32 _rem::R8(float64,float64,float64)
        brfalse FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
