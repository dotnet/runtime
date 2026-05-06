// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b07458
{
}
.class public ILGEN_940507601 {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	6
 .locals ( int64, float64, int32, int64, float64, float32, int32, int32, float64, int32, float64, int32, int32, float32, float32, float32, int64, float32, int32, int32, int32, int32, int32, int64, float32, float64, float64, float64, float64, int32, int64, int64, float32, float64, int64, int32, float64, int64, int32, int64, int32)
  ldc.i8 0x275d2b381f63fa
  stloc 0
  ldc.r8 float64(0x533ecef57c95fdf)
  stloc 1
  ldc.i4 0x5a0059ba
  stloc 2
  ldc.i8 0x621746e0420c7e30
  stloc 3
  ldc.r8 float64(0x3e5621543d4d28d)
  stloc 4
  ldc.r4 float32(0x3243a40)
  stloc 5
  ldc.i4 0x89d123b
  stloc 6
  ldc.i4 0x3c1857b8
  stloc 7
  ldc.r8 float64(0x7e1763e96a4a2090)
  stloc 8
  ldc.i4 0x575d3819
  stloc 9
  ldc.r8 float64(0x4da1c9abb1ce7)
  stloc 10
  ldc.i4 0x66cc22b3
  stloc 11
  ldc.i4 0x1e6f1d48
  stloc 12

 ldc.i4.8
 ldloc 11
 and
 conv.i8
 ldloc 0
 ldloc 40
 conv.i8
 ldc.i4.1
 stloc 11
 add
 clt
 ldc.i4 1
 ldloc 13
 ldloc 1
 conv.r4
 cgt
 sub
 clt

ldc.i4 100
add
 ret
}
}
