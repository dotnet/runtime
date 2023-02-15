// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.assembly 'clt'{
}

.namespace clt_
{
.class public _clt {

.field public static   int32 i4
.field public static   int32 u4
.field public static   int64 i8
.field public static   int64 u8
.field public static   float32 r4
.field public static   float64 r8
.field public static   int32 u

.method public static void initialize() {
        ldc.i4  0x7FFFFFFF
        stsfld  int32 clt_._clt::i4
        ldc.i4  0xFFFFFFFF
        stsfld  int32 clt_._clt::u4
        ldc.i8  0x7FFFFFFFFFFFFFFF
        stsfld  int64 clt_._clt::i8
        ldc.i8  0xFFFFFFFFFFFFFFFF
        stsfld  int64 clt_._clt::u8
        ldc.r4  float32(0x7F7FFFFF)
        stsfld  float32 clt_._clt::r4
        ldc.r8  float64(0x7FEFFFFFFFFFFFFF)
        stsfld  float64 clt_._clt::r8
        ldc.i4  0x000000FF              
        stsfld  int32 clt_._clt::u
        ret
}

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
	call	void clt_._clt::initialize()
        ldc.i4  0x00000001
        ldsfld  int32 clt_._clt::i4
        clt
        brfalse FAIL    

        ldc.i4  0x00000001
        ldsfld  int32 clt_._clt::u4
        clt.un
        brfalse FAIL    
        ldc.i8  0x0000000000000001
        ldsfld  int64 clt_._clt::i8
        clt
        brfalse FAIL    

        ldc.i8  0x0000000000000001
        ldsfld  int64 clt_._clt::u8
        clt.un
        brfalse FAIL    
        ldc.r4  float32(0x3F800000)
        ldsfld  float32 clt_._clt::r4
        clt
        brfalse FAIL    

        ldc.r4  float32(0x7FC00000)
        ldsfld  float32 clt_._clt::r4
        clt.un
        brfalse FAIL    

        ldc.r8  float64(0x3FF0000000000000)
        ldsfld  float64 clt_._clt::r8
        clt
        brfalse FAIL    

        ldc.r8  float64(0x7FF8000000000000)
        ldsfld  float64 clt_._clt::r8
        clt.un
        brfalse FAIL    
        ldc.i4  0x00000001
        ldsfld  int32 clt_._clt::u
        clt.un
        brfalse FAIL    
PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
}
