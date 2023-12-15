// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b05773
{
}
.class public _simple {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	100

      ldc.r4 float32(0x76c1)
      ldc.r4 float32(0x76c1)
   cgt.un
 conv.r8
       ldc.r8 float64(0x73dd472c6fd66c1)
       ldc.r8 float64(0x73dd472c6fd616c1)
       ldc.r8 float64(0x61f619384f62e0e)
       ldc.r8 float64(0x638961f533a745df)
      add
     mul
   cgt
 conv.r8
clt

ldc.i4 100
add
ret
}
}
