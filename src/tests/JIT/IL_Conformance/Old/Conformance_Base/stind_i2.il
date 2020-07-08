// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public Stind_i2 {
    .field public static int64 FOO


.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
        ldsflda		int64 Stind_i2::FOO
        ldc.i4          0xCDEF
        stind.i2

        ldsflda		int64 Stind_i2::FOO

        ldc.i4          0x2
        add
        ldc.i4          0x89AB
        stind.i2

        ldsflda		int64 Stind_i2::FOO
        ldc.i4          0x4
        add
        ldc.i4          0x4567
        stind.i2

        ldsflda		int64 Stind_i2::FOO
        ldc.i4          0x6
        add
        ldc.i4          0x0123
        stind.i2

        ldsflda		int64 Stind_i2::FOO

        ldind.i8
        ldc.i8          0x0123456789ABCDEF
        ceq
        brfalse         FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly stind_i2.exe{}
