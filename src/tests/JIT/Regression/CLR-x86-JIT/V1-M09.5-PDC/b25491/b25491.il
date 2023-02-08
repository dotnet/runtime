// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b25491' {}
.class public ILGEN_0x8bf68a14 {

.field static	unsigned int64 field_0x3
.field static	int64[] field_0x11
.field static	float32[] field_0x12

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	14

 .locals (unsigned int16[] local_0x1,unsigned int8 local_0xa,unsigned int16 local_0xb,int32 ecode)
 ldc.i4.1
 stloc ecode
 .try {

 ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x1
 ldc.i4 0x513a303f 
 stloc local_0xa
 ldc.i4 0xc9f7752 
 stloc local_0xb

 ldc.i8 0x71d2227232151758 
 stsfld unsigned int64 ILGEN_0x8bf68a14::field_0x3
 ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x8bf68a14::field_0x11
 ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x8bf68a14::field_0x12

Start_Orphan_0:
 ldsfld int64[] ILGEN_0x8bf68a14::field_0x11
 ldc.i4.0
 ldc.i8 0x4572704acf71aad
 stelem.i8
End_Orphan_0:

 ldsfld float32[] ILGEN_0x8bf68a14::field_0x12
 ldloc local_0xb
 ldelem.r4
 conv.r8


Start_Orphan_b:
 ldsflda unsigned int64 ILGEN_0x8bf68a14::field_0x3
 ldc.r8 float64(0x31d41b4f3955b30)
 conv.u8
 conv.ovf.i8
 ldc.i8 0x6186a1d4e72141e

Start_Orphan_11:
 ldc.i4.s -40
 stloc local_0xa
End_Orphan_11:

 ldloc local_0x1
 ldc.i4 0x505f5532
 ldelem.u2
 shl
 add
 ldc.r4 float32(0x6bb36407)
 conv.ovf.u8
 conv.i8
 sub.ovf.un
 stind.i8
End_Orphan_b:

conv.u4

 pop
 leave xx
 } catch [mscorlib]System.IndexOutOfRangeException {
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
