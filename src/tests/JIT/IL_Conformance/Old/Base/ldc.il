// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.assembly ldc.exe{}

.class public ldc {

.method public static int32 i4(int32) {
.maxstack	2
	ldarg	0
	ret
}
.method public static int64 i8(int64) {
.maxstack	2
	ldarg	0
	ret
}
.method public static float32 r4(float32) {
.maxstack	2
	ldarg	0
	ret
}
.method public static float64 r8(float64) {
.maxstack	2
	ldarg	0
	ret
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
        ldc.i4          0xAAAAAAAA
        call    int32 ldc::i4(int32)
        ldc.i4          0xAAAAAAAA
        ceq
        brfalse         FAIL
        ldc.i8          0x5555555555555555
        call    int64 ldc::i8(int64)
        ldc.i8          0x5555555555555555
        ceq
        brfalse         FAIL
        ldc.r4          float32(0x7F7FFFFF)
        call    float32 ldc::r4(float32)
        ldc.r4          float32(0x7F7FFFFF)
        ceq
        brfalse         FAIL

        ldc.r8          float64(0x7FEFFFFFFFFFFFFF)
        call    float64 ldc::r8(float64)
        ldc.r8          float64(0x7FEFFFFFFFFFFFFF)
        ceq
        brfalse         FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
