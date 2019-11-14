// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.assembly ldind_stind.exe{}

.class public ldind_stind {


	.field public static int8 I1
	.field public static int16 I2
	.field public static int32 I4
	.field public static int64 I8
	.field public static float32 R4
	.field public static float64 R8
	.field public static native int I


.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
        ldsflda		int8 ldind_stind::I1
        ldc.i4          0x00000011
        stind.i1

        ldsflda		int16 ldind_stind::I2
        ldc.i4          0x00002222
        stind.i2

        ldsflda		int32 ldind_stind::I4
        ldc.i4          0x44444444
        stind.i4
        ldsflda		int64 ldind_stind::I8
        ldc.i8          0x8888888888888888
        stind.i8
        ldsflda		float32 ldind_stind::R4
        ldc.r4          float32(0x3F800000)
        stind.r4

        ldsflda		float64 ldind_stind::R8
        ldc.r8          float64(0x3FF0000000000000)
        stind.r8
        ldsflda		native int ldind_stind::I
	ldsflda		native int ldind_stind::I
        stind.i


      	ldsflda		int8 ldind_stind::I1
        ldind.i1
        ldc.i4          0x00000011
        ceq
        brfalse         FAIL

        ldsflda		int16 ldind_stind::I2
        ldind.i2
        ldc.i4          0x00002222
        ceq
        brfalse         FAIL

        ldsflda		int32 ldind_stind::I4
        ldind.i4
        ldc.i4          0x44444444
        ceq
        brfalse         FAIL
        ldsflda		int64 ldind_stind::I8
        ldind.i8
        ldc.i8          0x8888888888888888
        ceq
        brfalse         FAIL
        ldsflda		float32 ldind_stind::R4
        ldind.r4
        ldc.r4          float32(0x3F800000)
        ceq
        brfalse         FAIL

        ldsflda		float64 ldind_stind::R8
        ldind.r8
        ldc.r8          float64(0x3FF0000000000000)
        ceq
        brfalse         FAIL
	ldsflda		native int ldind_stind::I
	ldind.i
	conv.i4
	ldsfld		native int ldind_stind::I
	conv.i4
	ceq
	brfalse		FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
