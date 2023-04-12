// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b06730
{
}

.class public ILGEN_939524508 {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	7
 .locals ( float32, int32, int32, int32, float32, int32,
 float32, float32, int32, int64, int64, int64, float64)

//ldc.i8 5
//stloc 9

      ldloc 12
      ldloc 12
      ldloc 9
     stloc 10
    add
  conv.u1
      ldloc 2
      ldloc 2
     add
    conv.i8
      ldc.i8 0x5757a9976481e71
      ldloc 10
    and
   mul
      ldloc 9
     conv.i8
  cgt
 xor
conv.i4
pop
ldc.i4 100
ret
}
}
