// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xcd02950a {}
.assembly extern xunit.core
{
    .publickeytoken = (
        8d 05 b1 bb 7a 6f db 6c
     )
     .ver 2:4:2:0
}
.class ILGEN_0xcd02950a {
.method static int32 Main() {
 .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
     01 00 00 00
 )
 .entrypoint
 .maxstack	20
 .locals (unsigned int32 local_0x0)
 ldc.i4 0x2a256bb9 
 stloc local_0x0
 ldc.i4 44
 ldloc local_0x0
 conv.u8
 conv.u4
 cgt.un
ldc.i4 100
add
 ret
}
}
