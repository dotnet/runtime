// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.method public static int32 foo(int32) {
    ldarg    0
    ldc.i4      0x2
    mul
    ret 
}

.method public static int32 foo(int32,int32) {
    ldarg    0
    ldarg    1
    mul
    ret 
}

.class public Ldftn {
.method public static   int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack       20
    ldftn       int32 foo(int32)
    ldftn       int32 foo(int32)
    ceq
    brfalse     FAIL

    ldftn       int32 foo(int32,int32)
    ldftn       int32 foo(int32,int32)
    ceq
    brfalse     FAIL

    ldftn       int32 foo(int32)
    ldftn       int32 foo(int32,int32)
    ceq
    brtrue      FAIL


    ldc.i4 100
    ret
FAIL:
    ldc.i4      0x0
    ret
}
}
.assembly ldftn.exe{}
