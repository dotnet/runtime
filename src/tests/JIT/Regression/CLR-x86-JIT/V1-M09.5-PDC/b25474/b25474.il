// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b25474' {}
.class public ILGEN_0x5dbf32e8 {

.field static	unsigned int32 field_0x2
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	12

 .locals (int64 local_0xf,float64 local_0x11)
nop
nop

 ldc.i8 0x3c4140e32d746b83 
 stloc local_0xf
 ldc.r8 float64(0x786d79d642ad4114) 
 stloc local_0x11
 
 ldc.i4 0x51c86b1d 
 stsfld unsigned int32 ILGEN_0x5dbf32e8::field_0x2

 ldc.i8 44
 ldc.r4 float32(0x34f74983)
 conv.i8
 ldloc local_0xf
 ldsfld unsigned int32 ILGEN_0x5dbf32e8::field_0x2
 conv.ovf.u8
 sub
 sub
 sub
 conv.u2
 dup
 sub
ldc.i4 100
add
ret
}
}
