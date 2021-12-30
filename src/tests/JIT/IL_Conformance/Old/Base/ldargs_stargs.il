// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class ldargs_stargs {


.field public static int32 PTR

.method public void .ctor() {

.maxstack       10
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
}


.method public static  int32 stargi4(int32) {
.maxstack       5
        ldarg.s 0
        starg.s 0
        ldarg.s 0
        ret
}

.method public static  int64 stargi8(int64) {
.maxstack       5
        ldarg.s 0
        starg.s 0
        ldarg.s 0
        ret
}
.method public static  float32 stargr4(float32) {
.maxstack       5
        ldarg.s 0
        starg.s 0
        ldarg.s 0
        ret
}

.method public static  float64 stargr8(float64) {
.maxstack       5
        ldarg.s 0
        starg.s 0
        ldarg.s 0
        ret
}
.method public static  class ldargs_stargs stargref(class ldargs_stargs) {
.maxstack       5
        ldarg.s 0
        starg.s 0
        ldarg.s 0
        ret
}

.method public static  int32 stargi(int32) {
.maxstack       5
        ldarg.s 0
        starg.s 0
        ldarg.s 0
        conv.i4
        ret
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack       10
        ldc.i4          0x1
        call    int32 ldargs_stargs::stargi4(int32)
        ldc.i4          0x1
        ceq
        brfalse         FAIL
        ldc.i8          0x2
        call    int64 ldargs_stargs::stargi8(int64)
        ldc.i8          0x2
        ceq
        brfalse         FAIL
        ldc.r4          1.1
        call    float32 ldargs_stargs::stargr4(float32)
        ldc.r4          1.1
        ceq
        brfalse         FAIL

        ldc.r8          2.2
        call    float64 ldargs_stargs::stargr8(float64)
        ldc.r8          2.2
        ceq
        brfalse         FAIL
        newobj  instance        void ldargs_stargs::.ctor()
        dup
        call    class ldargs_stargs ldargs_stargs::stargref(class ldargs_stargs)
        ceq
        brfalse         FAIL

        ldsflda		int32 ldargs_stargs::PTR
        call    int32 ldargs_stargs::stargi(int32)
        ldsflda		int32 ldargs_stargs::PTR
        conv.i4
        ceq
        brfalse         FAIL

PASS:
        ldc.i4 100
        ret
FAIL:
        ldc.i4  0x0
        ret
}
}

.assembly ldargs_stargs {}
