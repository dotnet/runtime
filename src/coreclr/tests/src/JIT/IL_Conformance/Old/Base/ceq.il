// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.assembly ceq.exe{}

.class public _ceq {

.field public static   int32 i4
.field public static   int64 i8
.field public static   float32 r4
.field public static   float64 r8
.field public static   int32 i
.field public static   class _ceq ref

.method public void .ctor() {
.maxstack	10
	ldarg.0
	call instance void [mscorlib]System.Object::.ctor()
	ret
}

.method public static void initialize() {
        ldc.i4  0x1234ABCD
        stsfld  int32 _ceq::i4
        ldc.i8  0x1234ABCD5678EF09
        stsfld  int64 _ceq::i8
        ldc.r4  float32(0xBF800000)
        stsfld  float32 _ceq::r4
        ldc.r8  float64(0xBFF0000000000000)
        stsfld  float64 _ceq::r8
        ldc.i4  0x000000FF
        stsfld  int32 _ceq::i
        newobj  instance void _ceq::.ctor()
        stsfld  class _ceq _ceq::ref
        ret
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
	call	void _ceq::initialize()
        ldsfld  int32 _ceq::i4
        ldsfld  int32 _ceq::i4
        ceq
        brfalse FAIL    
        ldsfld  int64 _ceq::i8
        ldsfld  int64 _ceq::i8
        ceq
        brfalse FAIL    
        ldsfld  float32 _ceq::r4
        ldsfld  float32 _ceq::r4
        ceq
        brfalse FAIL    

        ldsfld  float64 _ceq::r8
        ldsfld  float64 _ceq::r8
        ceq
        brfalse FAIL    
        ldsfld  int32 _ceq::i
        ldsfld  int32 _ceq::i
        ceq
        brfalse FAIL    


PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
