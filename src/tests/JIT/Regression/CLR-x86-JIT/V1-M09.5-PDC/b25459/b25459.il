// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b25459' {}
.class public ILGEN_0x5034b2f4 {

//BEGIN STATIC FIELDS
.field static	unsigned int8 field_0x0
.field static	unsigned int16 field_0x1
.field static	unsigned int32 field_0x2
.field static	unsigned int64 field_0x3
.field static	int8 field_0x4
.field static	int16 field_0x5
.field static	int32 field_0x6
.field static	int64 field_0x7
.field static	float32 field_0x8
.field static	float64 field_0x9
.field static	unsigned int8[] field_0xa
.field static	unsigned int16[] field_0xb
.field static	unsigned int32[] field_0xc
.field static	unsigned int64[] field_0xd
.field static	int8[] field_0xe
.field static	int16[] field_0xf
.field static	int32[] field_0x10
.field static	int64[] field_0x11
.field static	float32[] field_0x12
.field static	float64[] field_0x13
//END STATIC FIELDS
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	16

 .locals (unsigned int8[] local_0x0,unsigned int16[] local_0x1,unsigned int32[] local_0x2,unsigned int64[] local_0x3,int8[] local_0x4,int16[] local_0x5,int32[] local_0x6,int64[] local_0x7,float32[] local_0x8,float64[] local_0x9,unsigned int8 local_0xa,unsigned int16 local_0xb,int8 local_0xc,int16 local_0xd,int32 local_0xe,int64 local_0xf,float32 local_0x10,float64 local_0x11,int32 ecode)
ldnull  stloc.0
ldnull  stloc.1
ldnull  stloc.2
ldnull  stloc.3
ldnull  stloc 4
ldnull  stloc 5
ldnull  stloc 6
ldnull  stloc 7
ldnull  stloc 8
ldnull  stloc 9
ldc.i4.0  stloc 10
ldc.i4.0  stloc 11 
ldc.i4.0  stloc 12 
ldc.i4.0  stloc 13
ldc.i4.0  stloc 14
ldc.i8 0  stloc 15
ldc.r4 0  stloc 16
ldc.r8 0  stloc 17 
ldc.i4.0  stloc 18

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
ldc.i4 0x1288347c 
stloc local_0xa
ldc.i4 0x23f58c 
stloc local_0xb
ldc.i4 0x30cf1f8a 
stloc local_0xc
ldc.i4 0x172d10a3 
stloc local_0xd
ldc.i4 0x4f3d2eae 
stloc local_0xe
ldc.i8 0x36e047e27263f0a 
stloc local_0xf
ldc.r4 float32(0x13fe5e26) 
stloc local_0x10
ldc.r8 float64(0x28da2a5c4c727aac) 
stloc local_0x11

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0x79db6edb 
stsfld unsigned int8 ILGEN_0x5034b2f4::field_0x0
ldc.i4 0x11881d82 
stsfld unsigned int16 ILGEN_0x5034b2f4::field_0x1
ldc.i4 0x50f651b1 
stsfld unsigned int32 ILGEN_0x5034b2f4::field_0x2
ldc.i8 0x4b982c2e33631694 
stsfld unsigned int64 ILGEN_0x5034b2f4::field_0x3
ldc.i4 0xd7729a9 
stsfld int8 ILGEN_0x5034b2f4::field_0x4
ldc.i4 0x4ccff3c 
stsfld int16 ILGEN_0x5034b2f4::field_0x5
ldc.i4 0x63e62c25 
stsfld int32 ILGEN_0x5034b2f4::field_0x6
ldc.i8 0x3a8b512f47672187 
stsfld int64 ILGEN_0x5034b2f4::field_0x7
ldc.r4 float32(0x2db33f8e) 
stsfld float32 ILGEN_0x5034b2f4::field_0x8
ldc.r8 float64(0xe8a61fb62dc6a66) 
stsfld float64 ILGEN_0x5034b2f4::field_0x9
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0x5034b2f4::field_0xa
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x5034b2f4::field_0xb
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x5034b2f4::field_0xc
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x5034b2f4::field_0xd
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0x5034b2f4::field_0xe
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x5034b2f4::field_0xf
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0x5034b2f4::field_0x10
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x5034b2f4::field_0x11
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x5034b2f4::field_0x12
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x5034b2f4::field_0x13

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
 ldsfld float32 ILGEN_0x5034b2f4::field_0x8
 ldsfld unsigned int16 ILGEN_0x5034b2f4::field_0x1
 conv.r4
Start_Orphan_5:
 ldloc local_0x9
 ldloc local_0xa
 ldsfld float64 ILGEN_0x5034b2f4::field_0x9
 stelem.r8
End_Orphan_5:
 ldsfld unsigned int32 ILGEN_0x5034b2f4::field_0x2
 conv.r4
 ldc.i4.m1
 conv.r4
 sub
Start_Orphan_6:
 ldsfld unsigned int32[] ILGEN_0x5034b2f4::field_0xc
 ldc.i4.1
 ldc.i4.6
 stelem.i4
End_Orphan_6:
 ldc.r4 float32(0xfcb7503)
 rem
 mul
 ldc.r8 float64(0x50f73c956d9929aa)
 ldloc local_0x11
 rem
 conv.r4
 ldloc local_0x10
 ldsfld float32 ILGEN_0x5034b2f4::field_0x8
 add
 conv.r4
 div
 ckfinite
 add
 clt
 stsfld int16 ILGEN_0x5034b2f4::field_0x5
End_Orphan_0:
Start_Orphan_8:
 ldloc local_0x7
Start_Orphan_9:
 ldsfld int64[] ILGEN_0x5034b2f4::field_0x11
 stloc local_0x7
End_Orphan_9:
 ldc.i4.6
Start_Orphan_a:
 ldloc local_0xe
 stsfld int16 ILGEN_0x5034b2f4::field_0x5
End_Orphan_a:
 ldc.i4.6
 conv.i1
 clt
 ldloc local_0xc
 add
Start_Orphan_11:
Start_Orphan_12:
 nop
End_Orphan_12:
 ldloca local_0xe
 ldind.i4
 conv.u1
 newarr [mscorlib]System.UInt16
 stloc local_0x1
End_Orphan_11:
 ldsfld float64 ILGEN_0x5034b2f4::field_0x9
 ldc.r8 float64(0x37e82bc429e420f1)
 add
 ldsfld float64 ILGEN_0x5034b2f4::field_0x9
 clt
 conv.ovf.u8
Start_Orphan_17:
 ldloc local_0x6
 stsfld int32[] ILGEN_0x5034b2f4::field_0x10
End_Orphan_17:
 ldsfld float32 ILGEN_0x5034b2f4::field_0x8
 ldloc local_0x10
 cgt
 ldloc local_0xd
 conv.ovf.u4.un
 sub
 conv.ovf.u8.un
 xor
Start_Orphan_19:
 ldsfld int16[] ILGEN_0x5034b2f4::field_0xf
 stloc local_0x5
End_Orphan_19:
 ldsfld int8 ILGEN_0x5034b2f4::field_0x4
 ldc.i4.5
 add.ovf.un
 neg
Start_Orphan_1c:
 ldsfld unsigned int32 ILGEN_0x5034b2f4::field_0x2
 stsfld int16 ILGEN_0x5034b2f4::field_0x5
End_Orphan_1c:
 ldc.i4.6
 ldc.i4.2
 cgt
 ldsfld int32 ILGEN_0x5034b2f4::field_0x6
 ldloc local_0xc
 add.ovf.un
 mul
 sub
 conv.ovf.i8
 sub.ovf
 stelem.i8
End_Orphan_8:
 ldloc local_0x8
 ldloc local_0x10
 conv.i8
 ldloc local_0x3
 ldc.i4.s -78
 ldelem.u8
 mul.ovf.un
Start_Orphan_24:
 ldloc local_0xd
 ldc.i4.7
 rem
 stsfld int16 ILGEN_0x5034b2f4::field_0x5
End_Orphan_24:
 ldc.i8 0x6b1f392a7e511e9
 ldloc local_0x11
 conv.ovf.i8
 ldloc local_0x7
 ldc.i4.s -103
 ldelem.i8
 add.ovf.un
 and
 xor
 conv.u4
 ldelem.r4
 conv.i4
 conv.i1
 
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
