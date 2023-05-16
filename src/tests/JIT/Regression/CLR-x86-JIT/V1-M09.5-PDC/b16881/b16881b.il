// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b16881b' {}
.class public ILGEN_0x38e6c1a4 {

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
//END STATIC FIELDS
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	14

 .locals (unsigned int8[] local_0x0,unsigned int16[] local_0x1,unsigned int32[] local_0x2,unsigned int64[] local_0x3,int8[] local_0x4,int16[] local_0x5,int32[] local_0x6,int64[] local_0x7,float32[] local_0x8,float64[] local_0x9,unsigned int8 local_0xa,unsigned int16 local_0xb,int8 local_0xc,int16 local_0xd,int32 local_0xe,int64 local_0xf,float32 local_0x10,float64 local_0x11, int32 ecode)
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
ldc.i4 0x568f3bf8 
stloc local_0xa
ldc.i4 0x74e91504 
stloc local_0xb
ldc.i4 0x1bde7199 
stloc local_0xc
ldc.i4 0x40e91bc3 
stloc local_0xd
ldc.i4 0x1ecf4027 
stloc local_0xe
ldc.i8 0x4ee07d684afc3461 
stloc local_0xf
ldc.r4 float32(0x2d317df4) 
stloc local_0x10
ldc.r8 float64(0x279e788f3a3a353d) 
stloc local_0x11

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0x6737267b 
stsfld unsigned int8 ILGEN_0x38e6c1a4::field_0x0
ldc.i4 0x66008b9 
stsfld unsigned int16 ILGEN_0x38e6c1a4::field_0x1
ldc.i4 0x67221f7 
stsfld unsigned int32 ILGEN_0x38e6c1a4::field_0x2
ldc.i8 0x63e1fe048a14f1e 
stsfld unsigned int64 ILGEN_0x38e6c1a4::field_0x3
ldc.i4 0x347f2885 
stsfld int8 ILGEN_0x38e6c1a4::field_0x4
ldc.i4 0x3d091f52 
stsfld int16 ILGEN_0x38e6c1a4::field_0x5
ldc.i4 0x2eaa6a02 
stsfld int32 ILGEN_0x38e6c1a4::field_0x6
ldc.i8 0x46ee2d6d46c12427 
stsfld int64 ILGEN_0x38e6c1a4::field_0x7
ldc.r4 float32(0x1282711) 
stsfld float32 ILGEN_0x38e6c1a4::field_0x8
ldc.r8 float64(0x7985be615b24212) 
stsfld float64 ILGEN_0x38e6c1a4::field_0x9

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
 ldsfld int8 ILGEN_0x38e6c1a4::field_0x4
 ldloc local_0xb
 mul
 stsfld unsigned int8 ILGEN_0x38e6c1a4::field_0x0
End_Orphan_2:
 ldloc local_0x0
 stloc local_0x0
End_Orphan_1:
 ldloc local_0x4
 pop
End_Orphan_0:
Start_Orphan_3:
Start_Orphan_4:
Start_Orphan_5:
 ldloc local_0xf
 pop
End_Orphan_5:
 ldsfld int16 ILGEN_0x38e6c1a4::field_0x5
 conv.u2
 conv.u1
 newarr [mscorlib]System.Single
Start_Orphan_6:
 ldc.r4 float32(0x7cfc681b)
 stloc local_0x10
End_Orphan_6:
Start_Orphan_7:
 nop
End_Orphan_7:
 ldc.i4.5
 conv.i8
 conv.ovf.u4
 ldc.i4.8
 conv.r8
 conv.r4
 stelem.r4
End_Orphan_4:
Start_Orphan_8:
Start_Orphan_9:
 nop
End_Orphan_9:
 ldc.i4.8
 conv.u1
 newarr [mscorlib]System.UInt32
 stloc local_0x2
End_Orphan_8:
 ldsfld unsigned int32 ILGEN_0x38e6c1a4::field_0x2
 conv.u1
 newarr [mscorlib]System.UInt32
 stloc local_0x2
End_Orphan_3:
Start_Orphan_a:
 ldloc local_0x7
 stloc local_0x7
End_Orphan_a:
 ldloc local_0x3
Start_Orphan_b:
Start_Orphan_c:
 ldc.i4.s -2
 stsfld int16 ILGEN_0x38e6c1a4::field_0x5
End_Orphan_c:
 ldc.i4.5
 ldloc local_0xb
 add
 conv.u1
 newarr [mscorlib]System.Int16
Start_Orphan_d:
 ldloca local_0x10
 pop
End_Orphan_d:
 ldsflda int8 ILGEN_0x38e6c1a4::field_0x4
 ldind.i1
 conv.i2
Start_Orphan_e:
 ldc.i4.2
 stloc local_0xe
End_Orphan_e:
 ldloc local_0xf
 ldsfld int64 ILGEN_0x38e6c1a4::field_0x7
 or
 conv.ovf.i2
 stelem.i2
End_Orphan_b:
 ldc.i4.1
 ldelem.u8
 conv.ovf.i4
Start_Orphan_f:
 ldloc local_0x5
 pop
End_Orphan_f:
Start_Orphan_10:
 ldloca local_0xf
Start_Orphan_11:
 ldsflda int32 ILGEN_0x38e6c1a4::field_0x6
 ldsfld unsigned int32 ILGEN_0x38e6c1a4::field_0x2
 stind.i4
End_Orphan_11:
 ldc.r4 float32(0x3fab23bb)
 conv.r8
 conv.ovf.u8
 stind.i8
End_Orphan_10:
Start_Orphan_12:
 ldloc local_0x1
 pop
End_Orphan_12:
Start_Orphan_13:
 nop
End_Orphan_13:
 ldc.i4.7
 conv.r.un
 conv.ovf.u2
 conv.u1
 newarr [mscorlib]System.Int32
Start_Orphan_14:
 ldloc local_0x6
 stloc local_0x6
End_Orphan_14:
Start_Orphan_15:
 ldloc local_0x5
 ldc.i4.s -18
 ldc.i4.5
 stelem.i2
End_Orphan_15:
Start_Orphan_16:
 nop
End_Orphan_16:
 ldc.i4.s -49
 ldloc local_0xe
 mul.ovf.un
 ldc.i4.8
 ldloc local_0xa
 add.ovf.un
 mul.ovf
 conv.ovf.u1
 ldelem.i4
Start_Orphan_17:
Start_Orphan_18:
 ldloc local_0x8
 ldc.i4.1
 ldc.r4 float32(0x43df1b9b)
 stelem.r4
End_Orphan_18:
 ldsflda unsigned int64 ILGEN_0x38e6c1a4::field_0x3
Start_Orphan_19:
 ldloc local_0x1
 pop
End_Orphan_19:
Start_Orphan_1a:
 nop
End_Orphan_1a:
 ldc.i8 0x2fdb4f9564a579
 ldsfld int64 ILGEN_0x38e6c1a4::field_0x7
 add.ovf.un
 ldloc local_0xf
 ldloc local_0xf
 mul.ovf.un
 sub.ovf.un
 stind.i8
End_Orphan_17:
Start_Orphan_1b:
 ldsflda int32 ILGEN_0x38e6c1a4::field_0x6
 pop
End_Orphan_1b:
 ldc.r4 float32(0x12766c25)
 ldc.r4 float32(0xca64e25)
 sub
 conv.ovf.i2
 ldloc local_0xf
 conv.ovf.u1.un
 conv.ovf.i
 add.ovf
 ldloc local_0xc
 ldloc local_0xb
 sub
 ldc.i4 0x14df1fcb
 conv.ovf.u1
 shl
 ldc.i4.7
 ldsfld unsigned int8 ILGEN_0x38e6c1a4::field_0x0
 mul
 conv.u
 add.ovf
 add.ovf.un
 clt
 conv.u4
 shl
 
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
