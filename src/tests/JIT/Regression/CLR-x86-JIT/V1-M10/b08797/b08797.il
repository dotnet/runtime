// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b08797
{
}
.class public ILGEN_941279580 {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	11
.locals ( int64)

 ldc.i8 0x7
 stloc 0

 ldloc 0
 conv.r4
 conv.i4
 ldloc 0
 conv.u
 conv.u8
 ldc.i8 0x2
 and
 conv.u4
 and
 ldc.i4 0x2
 sub

ldc.i4 100
add
 ret
}
}
