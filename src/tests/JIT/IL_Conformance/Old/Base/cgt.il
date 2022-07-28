// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.assembly 'cgt'{}

.class public _cgt {

.field public static   int32 i4
.field public static   int32 u4
.field public static   int64 i8
.field public static   int64 u8
.field public static   float32 r4
.field public static   float32 unr4
.field public static   float64 r8
.field public static   float64 unr8
.field public static   int32 u

.method public void _cgt() {
	ret
}

.method public static void initialize() {
        ldc.i4  0x7FFFFFFF
        stsfld  int32 _cgt::i4
        ldc.i4  0xFFFFFFFF
        stsfld  int32 _cgt::u4
        ldc.i8  0x7FFFFFFFFFFFFFFF
        stsfld  int64 _cgt::i8
        ldc.i8  0xFFFFFFFFFFFFFFFF
        stsfld  int64 _cgt::u8
        ldc.r4  float32(0x7F7FFFFF)
        stsfld  float32 _cgt::r4
        ldc.r8  float64(0x7FEFFFFFFFFFFFFF)
        stsfld  float64 _cgt::r8
        ldc.i4  0x000000FF      
        stsfld  int32 _cgt::u
        ret
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
	call	void _cgt::initialize()
        ldsfld  int32 _cgt::i4
        ldc.i4  0x00000001
        cgt
        brfalse FAIL    

        ldsfld  int32 _cgt::u4
        ldc.i4  0x00000001
        cgt.un
        brfalse FAIL    
        ldsfld  int64 _cgt::i8
        ldc.i8  0x0000000000000001
        cgt
        brfalse FAIL    

        ldsfld  int64 _cgt::u8
        ldc.i8  0x0000000000000001
        cgt.un
        brfalse FAIL    
        ldsfld  float32 _cgt::r4
        ldc.r4  float32(0x3F800000)
        cgt
        brfalse FAIL    

        ldsfld  float32 _cgt::r4
        ldc.r4  float32(0x7FC00000)
        cgt.un
        brfalse FAIL    

        ldsfld  float64 _cgt::r8
        ldc.r8  float64(0x3FF0000000000000)
        cgt
        brfalse FAIL    

        ldsfld  float64 _cgt::r8
        ldc.r8  float64(0x7FF8000000000000)
        cgt.un
        brfalse FAIL    
        ldsfld  int32 _cgt::u
        ldc.i4  0x00000001
        cgt
        brfalse FAIL    


PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
