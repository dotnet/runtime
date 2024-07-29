// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b07411
{
}

.class public ILGEN_940455031 {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	11
.locals init ( int32, float64, int64)

ldc.i8 1
ldc.i8 2
conv.i8
ldloc 2
conv.u
conv.u8
sub
sub
ldloc 1
conv.i8
cgt
ldc.i4 1
sub

ldc.i4 101
add
ret
}
}
