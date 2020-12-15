// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b07458
{
}
.class ILGEN_940507601 {
.method static int32 main() {
.entrypoint
.maxstack	6
 .locals ( int64 l0, float64 l1, int32 l2, int64 l3, float64 l4, float32 l5, int32 l6, int32 l7, float64 l8, int32 l9, float64 l10, int32 l11, int32 l12, float32 l13, float32 l14, float32 l15, int64 l16, float32 l17, int32 l18, int32 l19, int32 l20, int32 l21, int32 l22, int64 l23, float32 l24, float64 l25, float64 l26, float64 l27, float64 l28, int32 l29, int64 l30, int64 l31, float32 l32, float64 l33, int64 l34, int32 l35, float64 l36, int64 l37, int32 l38, int64 l39, int32 l40)
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
