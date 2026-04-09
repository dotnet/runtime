// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14197
{
}
.class public ILGEN_622380794 {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10

 .locals (int64 local_0x1)
 BLOCK_2:
 ldc.i8 0
 stloc local_0x1

 ldloca local_0x1
 ldind.i8
 
 conv.u
 ldc.i4 0x3c3a730f
 mul
 ldc.i4.0
 clt

ldc.i4 100
add
ret
}
}
