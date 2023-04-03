// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b25833' {}
.class public ILGEN_0x9f9bf0ac {

.field static	unsigned int32 field_0x2
.field static	int32 field_0x6
.field static	unsigned int8[] field_0xa
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	12

 .locals (unsigned int32[] local_0x2,float64 local_0x11,int32 ecode)
 
 ldc.i4.1
 stloc ecode
 .try {

ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x2
ldc.r8 float64(0x31103f2912c598e) 
stloc local_0x11

ldc.i4 0x52ed136e 
stsfld int32 ILGEN_0x9f9bf0ac::field_0x6
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0x9f9bf0ac::field_0xa

 BLOCK_1:
Start_Orphan_0:
 ldc.i4 55
 ldsfld int32 ILGEN_0x9f9bf0ac::field_0x6
 shl
 conv.ovf.u1
 ldc.r4 float32(0x72124921)
 conv.ovf.u1
 shr.un
 stsfld unsigned int32 ILGEN_0x9f9bf0ac::field_0x2
End_Orphan_0:
 ldloc  local_0x2
 ldloc local_0x2
 ldc.i4 0x75cd6cdc
 ldelem.u4
 ldelem.u4
 conv.ovf.i4
 
 pop
 leave xx
 } catch [mscorlib]System.OverflowException {
 pop
 ldc.i4.0
 stloc ecode
 leave xx
 }
 
xx:
 ldloc ecode
ldc.i4 100
add
 ret
}
}
