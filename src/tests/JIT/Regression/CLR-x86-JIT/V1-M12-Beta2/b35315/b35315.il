// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b35315' {}
.assembly extern xunit.core {}
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
