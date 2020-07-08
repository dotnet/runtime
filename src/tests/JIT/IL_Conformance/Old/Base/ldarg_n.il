// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class ldarg_n {

.method public static int32 I4_0(int32,int32,int32,int32) {
        ldarg.0
        ret
}

.method public static int32 I4_1(int32,int32,int32,int32) {
        ldarg.1
        ret
}

.method public static int32 I4_2(int32,int32,int32,int32) {
        ldarg.2
        ret
}

.method public static int32 I4_3(int32,int32,int32,int32) {
        ldarg.3
        ret
}

.method public static int16 I2_0(int16,int16,int16,int16) {
        ldarg.0
        ret
}

.method public static int16 I2_1(int16,int16,int16,int16) {
        ldarg.1
        ret
}

.method public static int16 I2_2(int16,int16,int16,int16) {
        ldarg.2
        ret
}

.method public static int16 I2_3(int16,int16,int16,int16) {
        ldarg.3
        ret
}

.method public static int8 I1_0(int8,int8,int8,int8) {
        ldarg.0
        ret
}

.method public static int8 I1_1(int8,int8,int8,int8) {
        ldarg.1
        ret
}

.method public static int8 I1_2(int8,int8,int8,int8) {
        ldarg.2
        ret
}

.method public static int8 I1_3(int8,int8,int8,int8) {
        ldarg.3
        ret
}

.method public static int64 I8_0(int64,int64,int64,int64) {
        ldarg.0
        ret
}

.method public static int64 I8_1(int64,int64,int64,int64) {
        ldarg.1
        ret
}

.method public static int64 I8_2(int64,int64,int64,int64) {
        ldarg.2
        ret
}

.method public static int64 I8_3(int64,int64,int64,int64) {
        ldarg.3
        ret
}

.method public static float32 R4_0(float32,float32,float32,float32) {
        ldarg.0
        ret
}

.method public static float32 R4_1(float32,float32,float32,float32) {
        ldarg.1
        ret
}

.method public static float32 R4_2(float32,float32,float32,float32) {
        ldarg.2
        ret
}

.method public static float32 R4_3(float32,float32,float32,float32) {
        ldarg.3
        ret
}

.method public static float64 R8_0(float64,float64,float64,float64) {
        ldarg.0
        ret
}

.method public static float64 R8_1(float64,float64,float64,float64) {
        ldarg.1
        ret
}

.method public static float64 R8_2(float64,float64,float64,float64) {
        ldarg.2
        ret
}

.method public static float64 R8_3(float64,float64,float64,float64) {
        ldarg.3
        ret
}


.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack       10

        ldc.i4.0
        ldc.i4.1
        ldc.i4.2
        ldc.i4.3
        call    int32 ldarg_n::I4_0(int32,int32,int32,int32)
        ldc.i4.0
        ceq
        brfalse FAIL

        ldc.i4.0
        ldc.i4.1
        ldc.i4.2
        ldc.i4.3
        call    int32 ldarg_n::I4_1(int32,int32,int32,int32)
        ldc.i4.1
        ceq
        brfalse FAIL

        ldc.i4.0
        ldc.i4.1
        ldc.i4.2
        ldc.i4.3
        call    int32 ldarg_n::I4_2(int32,int32,int32,int32)
        ldc.i4.2
        ceq
        brfalse FAIL

        ldc.i4.0
        ldc.i4.1
        ldc.i4.2
        ldc.i4.3
        call    int32 ldarg_n::I4_3(int32,int32,int32,int32)
        ldc.i4.3
        ceq
        brfalse FAIL

        ldc.i4.0
        conv.i2
        ldc.i4.1
        conv.i2
        ldc.i4.2
        conv.i2
        ldc.i4.3
        conv.i2
        call    int16 ldarg_n::I2_0(int16,int16,int16,int16)
        ldc.i4.0
        conv.i2
        ceq
        brfalse FAIL

        ldc.i4.0
        conv.i2
        ldc.i4.1
        conv.i2
        ldc.i4.2
        conv.i2
        ldc.i4.3
        conv.i2
        call    int16 ldarg_n::I2_1(int16,int16,int16,int16)
        ldc.i4.1
        conv.i2
        ceq
        brfalse FAIL

        ldc.i4.0
        conv.i2
        ldc.i4.1
        conv.i2
        ldc.i4.2
        conv.i2
        ldc.i4.3
        conv.i2
        call    int16 ldarg_n::I2_2(int16,int16,int16,int16)
        ldc.i4.2
        conv.i2
        ceq
        brfalse FAIL

        ldc.i4.0
        conv.i2
        ldc.i4.1
        conv.i2
        ldc.i4.2
        conv.i2
        ldc.i4.3
        conv.i2
        call    int16 ldarg_n::I2_3(int16,int16,int16,int16)
        ldc.i4.3
        conv.i2
        ceq
        brfalse FAIL
        
        ldc.i4.0
        conv.i1
        ldc.i4.1
        conv.i1
        ldc.i4.2
        conv.i1
        ldc.i4.3
        conv.i1
        call    int8 ldarg_n::I1_0(int8,int8,int8,int8)
        ldc.i4.0
        conv.i1
        ceq
        brfalse FAIL

        ldc.i4.0
        conv.i1
        ldc.i4.1
        conv.i1
        ldc.i4.2
        conv.i1
        ldc.i4.3
        conv.i1
        call    int8 ldarg_n::I1_1(int8,int8,int8,int8)
        ldc.i4.1
        conv.i1
        ceq
        brfalse FAIL

        ldc.i4.0
        conv.i1
        ldc.i4.1
        conv.i1
        ldc.i4.2
        conv.i1
        ldc.i4.3
        conv.i1
        call    int8 ldarg_n::I1_2(int8,int8,int8,int8)
        ldc.i4.2
        conv.i1
        ceq
        brfalse FAIL

        ldc.i4.0
        conv.i1
        ldc.i4.1
        conv.i1
        ldc.i4.2
        conv.i1
        ldc.i4.3
        conv.i1
        call    int8 ldarg_n::I1_3(int8,int8,int8,int8)
        ldc.i4.3
        conv.i1
        ceq
        brfalse FAIL
        
        ldc.i8  0
        ldc.i8  1
        ldc.i8  2
        ldc.i8  3
        call    int64 ldarg_n::I8_0(int64,int64,int64,int64)
        ldc.i8  0
        ceq
        brfalse FAIL

        ldc.i8  0
        ldc.i8  1
        ldc.i8  2
        ldc.i8  3
        call    int64 ldarg_n::I8_1(int64,int64,int64,int64)
        ldc.i8  1
        ceq
        brfalse FAIL

        ldc.i8  0
        ldc.i8  1
        ldc.i8  2
        ldc.i8  3
        call    int64 ldarg_n::I8_2(int64,int64,int64,int64)
        ldc.i8  2
        ceq
        brfalse FAIL

        ldc.i8  0
        ldc.i8  1
        ldc.i8  2
        ldc.i8  3
        call    int64 ldarg_n::I8_3(int64,int64,int64,int64)
        ldc.i8  3
        ceq
        brfalse FAIL

        ldc.r4  1.1
        ldc.r4  2.2
        ldc.r4  3.3
        ldc.r4  4.4
        call    float32 ldarg_n::R4_0(float32,float32,float32,float32)
        ldc.r4  1.1
        ceq
        brfalse FAIL

        ldc.r4  1.1
        ldc.r4  2.2
        ldc.r4  3.3
        ldc.r4  4.4
        call    float32 ldarg_n::R4_1(float32,float32,float32,float32)
        ldc.r4  2.2
        ceq
        brfalse FAIL

        ldc.r4  1.1
        ldc.r4  2.2
        ldc.r4  3.3
        ldc.r4  4.4
        call    float32 ldarg_n::R4_2(float32,float32,float32,float32)
        ldc.r4  3.3
        ceq
        brfalse FAIL

        ldc.r4  1.1
        ldc.r4  2.2
        ldc.r4  3.3
        ldc.r4  4.4
        call    float32 ldarg_n::R4_3(float32,float32,float32,float32)
        ldc.r4  4.4
        ceq
        brfalse FAIL
        
        ldc.r8  1.1
        ldc.r8  2.2
        ldc.r8  3.3
        ldc.r8  4.4
        call    float64 ldarg_n::R8_0(float64,float64,float64,float64)
        ldc.r8  1.1
        ceq
        brfalse FAIL

        ldc.r8  1.1
        ldc.r8  2.2
        ldc.r8  3.3
        ldc.r8  4.4
        call    float64 ldarg_n::R8_1(float64,float64,float64,float64)
        ldc.r8  2.2
        ceq
        brfalse FAIL

        ldc.r8  1.1
        ldc.r8  2.2
        ldc.r8  3.3
        ldc.r8  4.4
        call    float64 ldarg_n::R8_2(float64,float64,float64,float64)
        ldc.r8  3.3
        ceq
        brfalse FAIL

        ldc.r8  1.1
        ldc.r8  2.2
        ldc.r8  3.3
        ldc.r8  4.4
        call    float64 ldarg_n::R8_3(float64,float64,float64,float64)
        ldc.r8  4.4
        ceq
        brfalse FAIL

PASS:
        ldc.i4 100
        ret
FAIL:
        ldc.i4  0x0
        ret
}
}

.assembly ldarg_n.exe {}

