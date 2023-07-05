// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b48554a'
{
}


.class public ILGEN_877332642 {
 //(Nov 12 1999)15:27:27
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	6
 .locals ( int32, float64, float64, int64, int32, float64, int64, int32, int64, float64, float32, float32, float32, int64, float64, int32)
  ldc.i4 0x33fb5f4a
  stloc 0
  ldc.r8 float64(0x2a045fc65d836ab8)
  stloc 1
  ldc.r8 float64(0x6e1d2e227d4b756a)
  stloc 2
  ldc.i8 0x296779c3221772d7
  stloc 3
  ldc.i4 0x61a11b2
  stloc 4
  ldc.r8 float64(0x26d3549e60046c0f)
  stloc 5
  ldc.i8 0x5488414146c37bd9
  stloc 6
  ldc.i4 0x1bff1520
  stloc 7
  ldc.i8 0x7c468fa593459a
  stloc 8
  ldc.r8 float64(0x473479cbe027795)
  stloc 9
  ldc.r4 float32(0x21a412dd)
  stloc 10
  ldc.r4 float32(0x272937ba)
  stloc 11
  ldc.r4 float32(0x43c818f2)
  stloc 12
  ldc.i8 0x6448377b121c4166
  stloc 13
  ldc.r8 float64(0x4dc668dd35461cd4)
  stloc 14
  ldc.i4 0x1047cad
  stloc 15

     ldc.r4 float32(0x493630c)
     ldloc 8
    pop
   conv.r4
  conv.i
     ldc.i8 0x483f1a9c28a2880
     ldc.i4.4
    stloc 0
   conv.i4
     ldc.i4.7
    conv.u
   conv.u4
  cgt.un
 sub
     ldloc 8
    conv.u
     ldloc 11
    conv.i
   xor
     ldc.r8 float64(0x741f55ce7dd462b0)
     ldc.r8 float64(0x400b4b00622e7f20)
    add
     ldc.i4 0x9516219
    conv.i4
   stloc 15
  stloc 9
 conv.u1
clt
ldc.i4.1
sub
ldc.i4 100
add
ret
}
}
