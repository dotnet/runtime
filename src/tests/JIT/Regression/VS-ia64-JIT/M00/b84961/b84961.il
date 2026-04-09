// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b84961'{}

.class public _mul {
.method public static  int32 I4(int32,int32,int32) {
.maxstack       3
        ldarg   0
        ldarg   1
        mul
        ldarg   2
        ceq
        ret
}

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
        ldc.i4  0x55555555
        ldc.i4  0x00000002
        ldc.i4  0xAAAAAAAA
        call    int32 _mul::I4(int32,int32,int32)
        brfalse FAIL
	ldc.i4	100
	ret
FAIL:
	ldc.i4	5
	ret
}
}
