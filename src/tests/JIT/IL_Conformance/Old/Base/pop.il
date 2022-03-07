// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.assembly 'pop'{}

.class public _pop {


.field public static int32 PTR

.method public static int32 return(int32) {
.maxstack	2
	ldarg	0
	ret
}
.method public static int64 return(int64) {
.maxstack	2
	ldarg	0
	ret
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
        ldc.i4  0x0000000A
        call    int32 _pop::return(int32)
        ldc.i4  0xFFFFFFFF
        call    int32 _pop::return(int32)
        pop
        ldc.i4  0x0000000A
        call    int32 _pop::return(int32)
        ceq
        brfalse FAIL
        ldc.i4  0x0000000A
        call    int32 _pop::return(int32)
        ldc.i8  0xFFFFFFFFFFFFFFFF
        call    int64 _pop::return(int64)
        pop
        ldc.i4  0x0000000A
        call    int32 _pop::return(int32)
        ceq
        brfalse FAIL
        ldc.i4  0x0000000A
        call    int32 _pop::return(int32)
        ldsflda	int32 _pop::PTR
        pop
        ldc.i4  0x0000000A
        call    int32 _pop::return(int32)
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
