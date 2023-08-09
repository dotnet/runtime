// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b12487
{
}
.class public ILGEN_1806854430
{
.method public static int32 main()
 {
 .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
     01 00 00 00
 )
 .entrypoint
 .maxstack	8
 .locals ( float32, int64)
  ldc.r4 float32(0x63e756a3)
  stloc 0
  ldc.i8 0x176646e5325931d5
  stloc 1

  ldc.i4 0x25f24225
  ldloc 0
  ldc.i8 5
  conv.r.un
  conv.r4
  add
  conv.i
  ldc.i8 0x62f756c239065e5
  ldloc 1
  clt.un
  conv.i1
  conv.r4
  conv.i
  ldc.i4 0x45e23077
  ldloc 0
  conv.i
  cgt
  conv.u1
  mul
  clt
  clt.un
ldc.i4 100
add
  ret
 }
}
