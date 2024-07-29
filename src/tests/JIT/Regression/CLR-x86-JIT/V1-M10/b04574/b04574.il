// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b04574
{
}
.class public _simple {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	100

.locals (int32 A)
 ldc.i4 1
 stloc.0

 ldc.r4 6
 conv.r4
 ldc.r4 64
 add
 conv.i4
 ldc.i4 1
 ldc.i4 2
 conv.i4
 add
 add
 stloc.0

 ldc.i4 100
 ret
}
}
