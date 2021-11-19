// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x7fec790f {}
.assembly extern xunit.core {}
.class ILGEN_0x7fec790f {

.field static	unsigned int16 field_0x7
.method static float32 Method_0x4945() {
.maxstack	13

 .locals (int64 local_0x1)

 ldc.i8 0x5c5b6efa93b3355 
 stloc local_0x1
 ldc.i4 0x33fc10a9 
 stsfld unsigned int16 ILGEN_0x7fec790f::field_0x7

Start_Orphan_0:
 ldsfld unsigned int16 ILGEN_0x7fec790f::field_0x7
 ldc.r8 float64(0x65c4195b6df36085)
 ldloc local_0x1
 conv.r8
 clt
 conv.i4
 shr
 stsfld unsigned int16 ILGEN_0x7fec790f::field_0x7
End_Orphan_0:

 ldc.r4 55
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
 call float32 ILGEN_0x7fec790f::Method_0x4945()
 conv.i4
 ldc.i4 45
 add
 ret
}
}
