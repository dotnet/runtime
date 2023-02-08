// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b16503' {}
.class public ILGEN_622380794 {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	18

 .locals (int8 local_0xe,int8 local_0x1e,unsigned int16 local_0x25)
 ldc.i4.0 stloc.0
 ldc.i4.0 stloc.1
 ldc.i4.0 stloc.2

ldc.i4 0x50e84d54 
stloc local_0xe
ldc.i4 0x504ea30 
stloc local_0x1e

 ldc.i4.8
 ldloc local_0x25
 mul
 conv.u8
 ldc.i8 0x42a116576054f94
 ldloc local_0xe
 conv.u8
 sub
 and
 ldc.i8 0x4c5e26c24605968
 ldc.r4 float32(0x31f66628)
 conv.u8
 and
 conv.u4
 shr
 conv.u8
 ldc.i4.8
 ldc.i4.7
 ldloc local_0x1e
 cgt.un
 conv.i4
 clt.un
 conv.r.un
 conv.u8
 xor
 conv.i
 
 dup
 sub
ldc.i4 100
add
 ret
}
}
