// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b16881a' {}
.class public ILGEN_0x38e55e0d {

//BEGIN STATIC FIELDS
.field static	unsigned int64 field_0x3
.field static	int8 field_0x4
.field static	float64 field_0x9
//END STATIC FIELDS
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	14

 .locals (unsigned int8[] local_0x0,unsigned int16[] local_0x1,unsigned int32[] local_0x2,unsigned int64[] local_0x3,int8[] local_0x4,int16[] local_0x5,int32[] local_0x6,int64[] local_0x7,float32[] local_0x8,float64[] local_0x9,unsigned int8 local_0xa,unsigned int16 local_0xb,int8 local_0xc,int16 local_0xd,int32 local_0xe,int64 local_0xf,float32 local_0x10,float64 local_0x11,int32 ecode)
 
 ldc.i4.1
 stloc ecode
 .try {
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x4
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x5
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x6
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x7
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x8
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x9
ldc.i4 0x6d4c79bb 
stloc local_0xa
ldc.i4 0x32de7b4e 
stloc local_0xb
ldc.i4 0x1090545a 
stloc local_0xc
ldc.i8 0x17327ac1370768a 
stloc local_0xf

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i8 0x3f01666b27f15b6 
stsfld unsigned int64 ILGEN_0x38e55e0d::field_0x3
ldc.i4 0x20595e9e 
stsfld int8 ILGEN_0x38e55e0d::field_0x4
ldc.r8 float64(0x2b0914f268085a4c) 
stsfld float64 ILGEN_0x38e55e0d::field_0x9

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
 ldloc local_0x0
 ldc.i8 0x439848d646060f
 ldloc local_0xf
 mul.ovf
 conv.ovf.i1
 ldsfld unsigned int64 ILGEN_0x38e55e0d::field_0x3
 ldc.i8 0x5f7e5b3e446d3c3a
 add.ovf.un
 conv.ovf.i1
 sub.ovf.un
 ldelema [mscorlib]System.Byte
Start_Orphan_4:
Start_Orphan_5:
 ldloc local_0x4
 ldloc local_0xc
 ldloc local_0xc
 stelem.i1
End_Orphan_5:
 ldsflda int8 ILGEN_0x38e55e0d::field_0x4
 ldloc local_0x2
 ldc.i4.m1
 ldelema [mscorlib]System.UInt32
 ldind.u4
 stind.i1
End_Orphan_4:
 ldsfld float64 ILGEN_0x38e55e0d::field_0x9
 conv.ovf.i2
 stind.i1
End_Orphan_0:
 ldc.r8 float64(0x326540374894619)
 conv.ovf.u2
 
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
