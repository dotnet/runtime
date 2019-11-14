// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.assembly mul.exe{}

.class public _mul {

.method public static  int32 I4(int32,int32,int32) {
.maxstack       3
        ldarg   0
        ldarg   1
        mul
        ldarg   2
        ceq
        ret
}
.method public static  int32 I8(int64,int64,int64) {
.maxstack       3
        ldarg   0
        ldarg   1
        mul
        ldarg   2
        ceq
        ret
}
.method public static  int32 R4(float32,float32,float32) {
.maxstack       3
        ldarg   0
        ldarg   1
        mul
        ldarg   2
        ceq
        ret
}
.method public static  int32 R8(float64,float64,float64) {
.maxstack       3
        ldarg   0
        ldarg   1
        mul
        ldarg   2
        ceq
        ret
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
        ldc.i4  0x55555555
        ldc.i4  0x00000002
        ldc.i4  0xAAAAAAAA
        call    int32 _mul::I4(int32,int32,int32)
        brfalse FAIL
        ldc.i8  0x5555555555555555
        ldc.i8  0x0000000000000002
        ldc.i8  0xAAAAAAAAAAAAAAAA
        call    int32 _mul::I8(int64,int64,int64)
        brfalse FAIL
	ldc.r4	float32(0xFF7FFFFF)
	ldc.r4	float32(0xBF800000)
	ldc.r4	float32(0x7F7FFFFF)
	call	int32 _mul::R4(float32,float32,float32)
	brfalse	FAIL	
        ldc.r8  float64(0xFFEFFFFFFFFFFFFF)
        ldc.r8  float64(0xBFF0000000000000)
        ldc.r8  float64(0x7FEFFFFFFFFFFFFF)
        call    int32 _mul::R8(float64,float64,float64)
        brfalse FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
