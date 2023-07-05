// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b05784
{
}
.class public _simple {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	100

.locals (int32, int32, int32, int32, int64, int64, int64, int64, float32, float32, float32, float32, float64,
 float64, float64, float64)
 ldc.i4 1 ldc.i4 1 ldc.i4 1 ldc.i4 1 stloc.0 stloc.1 stloc.2 stloc.3 ldc.i8 1 ldc.i8 1 ldc.i8 1 ldc.i8 1 stloc 4
 stloc 5 stloc 6 stloc 7 ldc.r4 1 ldc.r4 1 ldc.r4 1 ldc.r4 1 stloc 8 stloc 9 stloc 10 stloc 11 ldc.r8 1 ldc.r8 1
 ldc.r8 1 ldc.r8 1 stloc 12 stloc 13 stloc 14 stloc 15

ldc.i4 -123
ldloc 6
conv.i8
stloc 4
ldc.i8 0xc235b5e79c46b1e
stloc 6
ldc.i8 4
ldloc 6
ldloc 11
stloc 8
ldloc 4
xor
sub
ldloc.1
conv.r8
conv.i8
and
conv.i4
sub
ldc.i8 4294967172
conv.u4 
sub

ldc.i4 100
add
ret
}
}
