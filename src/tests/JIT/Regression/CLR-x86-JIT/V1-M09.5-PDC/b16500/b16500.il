// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b16500 { }
.class public ILGEN_622380794 {
.field static	int8 field_0x12
.field static	int8 field_0x26
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	14

 .locals (unsigned int8 local_0x0,unsigned int16 local_0x1,int16 local_0x3,int16 local_0x7)
ldc.i4 0x495c54a2 
stloc local_0x0
ldc.i4 0x6383581d 
stloc local_0x1
ldc.i4 0x4e2a3217 
stloc local_0x3
ldc.i4 0x37cc4ddd 
stloc local_0x7

ldc.i4 0x5672313a 
stsfld int8 ILGEN_622380794::field_0x12
ldc.i4 0x6bcd573 
stsfld int8 ILGEN_622380794::field_0x26

 ldc.i4.5
 ldsfld int8 ILGEN_622380794::field_0x12
 cgt.un
 conv.i8
 ldc.i4.7
 conv.i8
 sub
Start_Orphan_d:
Start_Orphan_e:
 ldsflda int8 ILGEN_622380794::field_0x26
 ldc.i4.1
 stind.i1
 ldc.i4.1
 conv.u8
 ldc.i8 0x1
 conv.r.un
 ldloc local_0x7
 conv.r4
 conv.r8
 cgt.un
 shl
 ldc.r4 float32(0x5b837e74)
 conv.u8
 conv.r.un
 conv.i8
 or
 xor
 conv.u4

dup
sub
ldc.i4 100
add
ret
}
}
