// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class ldc_i4_n {
.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack       10

        ldc.i4.0
        ldc.i4  0
        ceq
        brfalse FAIL
        
        ldc.i4.1
        ldc.i4  1
        ceq
        brfalse FAIL
        
        ldc.i4.2
        ldc.i4  2
        ceq
        brfalse FAIL

        ldc.i4.3
        ldc.i4  3
        ceq
        brfalse FAIL

        ldc.i4.4
        ldc.i4  4
        ceq
        brfalse FAIL

        ldc.i4.5
        ldc.i4  5
        ceq
        brfalse FAIL
        
        ldc.i4.6
        ldc.i4  6
        ceq
        brfalse FAIL

        ldc.i4.7
        ldc.i4  7
        ceq
        brfalse FAIL

        ldc.i4.8
        ldc.i4  8
        ceq
        brfalse FAIL
        
        ldc.i4.0
        ldc.i4.1
        sub
        ldc.i4.m1
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

.assembly ldc_i4_n {}

