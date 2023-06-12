// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b06754
{
}
.class public ILGEN_939661025 {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	7
 .locals (float32, int64, float32)
//ldc.r4 33
//stloc 0
//ldc.i8 33
//stloc 1
//ldc.r4 33
//stloc 2

      ldloc 2
    conv.i8
   conv.i4
      ldc.r4 float32(0x38cc5085)
      ldc.r4 float32(0x532657f6)
     stloc 2
    conv.r8
      ldc.r4 float32(0x60f849ff)
      ldloc 0
    cgt
   conv.r4
  stloc 2
pop
pop
ldc.i4 100
ret
}
}

