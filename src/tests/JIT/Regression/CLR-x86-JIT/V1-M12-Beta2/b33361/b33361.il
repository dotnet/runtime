// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b33361' {}
.assembly extern xunit.core {}
.class ILGEN_0x8b849bfe {

.field static	unsigned int8 field_0x3

.method static int32 Method_0x83() {
.maxstack	9

 .locals (unsigned int64 local_0x0,unsigned int32 local_0x2,int8 local_0x4)

ldc.i4 0x249e758f 
stloc local_0x2
ldc.i4 0x69e95125 
stloc local_0x4

ldc.i4 0x53b22398 
stsfld unsigned int8 ILGEN_0x8b849bfe::field_0x3

Start_Orphan_0:
 ldsfld unsigned int8 ILGEN_0x8b849bfe::field_0x3
 conv.i8
 ldloc local_0x4
 conv.ovf.u8
 mul
 stloc local_0x0
End_Orphan_0:

 ldc.i4 55
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

call int32 ILGEN_0x8b849bfe::Method_0x83()
ldc.i4 55
sub
ldc.i4 100
add
ret
}

}
