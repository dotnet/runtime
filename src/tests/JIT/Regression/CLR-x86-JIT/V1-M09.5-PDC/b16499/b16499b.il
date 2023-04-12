// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b16499b' {}
.class public ILGEN_0x38e69edd {

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
.maxstack	18

 .locals (unsigned int8[] local_0x0,unsigned int16[] local_0x1,unsigned int32[] local_0x2,unsigned int64[] local_0x3,int8[] local_0x4,int16[] local_0x5,int32[] local_0x6,int64[] local_0x7,float32[] local_0x8,float64[] local_0x9,unsigned int8 local_0xa,unsigned int16 local_0xb,int8 local_0xc,int16 local_0xd,int32 local_0xe,int64 local_0xf,float32 local_0x10,float64 local_0x11)

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

//BEGIN INIT LOCALS
.try {
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
ldc.i4 0xe651937 
stloc local_0xa
ldc.i4 0x53d14420 
stloc local_0xb
ldc.i4 0x5fc54 
stloc local_0xc
ldc.i4 0x53fa70e9 
stloc local_0xd
ldc.i4 0x703a303e 
stloc local_0xe
ldc.i8 0x40298ca246d425b 
stloc local_0xf
ldc.r4 float32(0x13ce201a) 
stloc local_0x10
ldc.r8 float64(0xe1f7c5537714d0c) 
stloc local_0x11

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0x1044314f 
stsfld unsigned int8 ILGEN_0x38e69edd::field_0x0
ldc.i4 0x7e35634b 
stsfld unsigned int16 ILGEN_0x38e69edd::field_0x1
ldc.i4 0x6435667 
stsfld unsigned int32 ILGEN_0x38e69edd::field_0x2
ldc.i8 0xf9148946368240e 
stsfld unsigned int64 ILGEN_0x38e69edd::field_0x3
ldc.i4 0x6f8f7687 
stsfld int8 ILGEN_0x38e69edd::field_0x4
ldc.i4 0x5ef315aa 
stsfld int16 ILGEN_0x38e69edd::field_0x5
ldc.i4 0x412c42bb 
stsfld int32 ILGEN_0x38e69edd::field_0x6
ldc.i8 0xde36d4c3b134e6f 
stsfld int64 ILGEN_0x38e69edd::field_0x7
ldc.r4 float32(0x46df6745) 
stsfld float32 ILGEN_0x38e69edd::field_0x8
ldc.r8 float64(0x23cd2c0951314d43) 
stsfld float64 ILGEN_0x38e69edd::field_0x9

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
 ldloc local_0x7
Start_Orphan_1:
Start_Orphan_2:
 ldloc local_0xe
 conv.ovf.u1
 pop
End_Orphan_2:
 ldloc local_0x5
 pop
End_Orphan_1:
 ldc.i4.7
Start_Orphan_3:
Start_Orphan_4:
 ldloc local_0x6
Start_Orphan_5:
 nop
End_Orphan_5:
 ldsfld unsigned int32 ILGEN_0x38e69edd::field_0x2
 ldc.i4.4
 clt.un
 ldc.i4.4
 conv.ovf.i1
 stelem.i4
End_Orphan_4:
Start_Orphan_6:
 ldloc local_0x4
 ldsfld unsigned int8 ILGEN_0x38e69edd::field_0x0
 ldc.i4.6
 stelem.i1
End_Orphan_6:
 ldc.i8 0x15a128c243ae5548
 conv.r.un
Start_Orphan_7:
 nop
End_Orphan_7:
 ldsflda float64 ILGEN_0x38e69edd::field_0x9
 ldind.r8
 div
 conv.i4
 stsfld unsigned int32 ILGEN_0x38e69edd::field_0x2
End_Orphan_3:
Start_Orphan_8:
Start_Orphan_9:
 nop
End_Orphan_9:
 ldsflda unsigned int64 ILGEN_0x38e69edd::field_0x3
 ldind.u8
 conv.r8
 stsfld float64 ILGEN_0x38e69edd::field_0x9
End_Orphan_8:
Start_Orphan_a:
 ldloc local_0x8
 ldloc local_0xd
 ldelema [mscorlib]System.Single
 ldsfld float64 ILGEN_0x38e69edd::field_0x9
 conv.r4
 stind.r4
End_Orphan_a:
Start_Orphan_b:
 ldc.i8 0x4673431d2baa3f25
 stsfld int64 ILGEN_0x38e69edd::field_0x7
End_Orphan_b:
Start_Orphan_c:
 nop
End_Orphan_c:
 ldc.i4.8
 conv.r8
 ldc.r8 float64(0x15855ece22707c37)
 ldsfld float64 ILGEN_0x38e69edd::field_0x9
 rem
 cgt
 conv.u1
 newarr [mscorlib]System.Int32
Start_Orphan_d:
 ldsflda float32 ILGEN_0x38e69edd::field_0x8
 pop
End_Orphan_d:
Start_Orphan_e:
 nop
End_Orphan_e:
 ldc.i4.4
 ldsfld int16 ILGEN_0x38e69edd::field_0x5
 add
 ldloc local_0xa
 conv.ovf.u4.un
 mul.ovf.un
 conv.ovf.i4
 ldelem.i4
 conv.ovf.u8.un
 stelem.i8
End_Orphan_0:
Start_Orphan_f:
Start_Orphan_10:
Start_Orphan_11:
 ldloc local_0x1
 pop
End_Orphan_11:
 ldsflda int64 ILGEN_0x38e69edd::field_0x7
 pop
End_Orphan_10:
Start_Orphan_12:
 ldsflda unsigned int8 ILGEN_0x38e69edd::field_0x0
 ldloc local_0xa
 stind.i1
End_Orphan_12:
 ldloc local_0x11
Start_Orphan_13:
 nop
End_Orphan_13:
 ldc.i4 0x2ac879ba
 conv.ovf.i8.un
 conv.r8
 div
Start_Orphan_14:
Start_Orphan_15:
 nop
End_Orphan_15:
 ldloc local_0x4
 pop
End_Orphan_14:
 ldloc local_0x11
 add
 stsfld float64 ILGEN_0x38e69edd::field_0x9
End_Orphan_f:
 ldloc local_0xe
 ldloc local_0xc
 add.ovf
Start_Orphan_16:
 nop
End_Orphan_16:
 ldc.i4.6
 conv.u4
 div
Start_Orphan_17:
 ldc.i4.7
 stsfld unsigned int16 ILGEN_0x38e69edd::field_0x1
End_Orphan_17:
 ldc.i8 0x36c2669a22988d1
 conv.ovf.u2.un
 ldc.r4 float32(0x2d94132)
 conv.ovf.i
 rem.un
 mul.ovf
Start_Orphan_18:
 ldloca local_0x11
 pop
End_Orphan_18:
 ldsfld unsigned int64 ILGEN_0x38e69edd::field_0x3
 conv.ovf.i.un
 not
Start_Orphan_19:
 ldc.i4.4
 stsfld unsigned int8 ILGEN_0x38e69edd::field_0x0
End_Orphan_19:
 ldc.i4.6
 conv.ovf.u.un
 conv.ovf.i4
 clt.un
 add
Start_Orphan_1a:
Start_Orphan_1b:
 ldsfld unsigned int32 ILGEN_0x38e69edd::field_0x2
 stloc local_0xa
End_Orphan_1b:
Start_Orphan_1c:
 nop
End_Orphan_1c:
 ldsfld int32 ILGEN_0x38e69edd::field_0x6
 conv.u1
 newarr [mscorlib]System.UInt64
Start_Orphan_1d:
 nop
End_Orphan_1d:
 ldc.i4.s 90
 ldc.i4.s -92
 rem.un
 ldelema [mscorlib]System.UInt64
 pop
End_Orphan_1a:
Start_Orphan_1e:
 ldc.i4.5
 stsfld unsigned int8 ILGEN_0x38e69edd::field_0x0
End_Orphan_1e:
Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldsfld unsigned int32 ILGEN_0x38e69edd::field_0x2
 conv.r8
 conv.ovf.u2
Start_Orphan_20:
 ldsflda int64 ILGEN_0x38e69edd::field_0x7
 pop
End_Orphan_20:
Start_Orphan_21:
 nop
End_Orphan_21:
 ldc.r4 float32(0xd2f1216)
 neg
 conv.u2
 sub
 conv.i
 sub.ovf.un
 conv.ovf.u2.un
Start_Orphan_22:
Start_Orphan_23:
 ldsfld float32 ILGEN_0x38e69edd::field_0x8
 conv.ovf.i1
 stsfld int8 ILGEN_0x38e69edd::field_0x4
End_Orphan_23:
Start_Orphan_24:
 ldsflda int32 ILGEN_0x38e69edd::field_0x6
 ldsfld unsigned int8 ILGEN_0x38e69edd::field_0x0
 stind.i4
End_Orphan_24:
 ldc.i4.0
 ldloc local_0xb
 mul.ovf.un
 conv.u1
 newarr [mscorlib]System.UInt32
 ldlen
 conv.u1
 newarr [mscorlib]System.Byte
 stloc local_0x0
End_Orphan_22:
Start_Orphan_25:
Start_Orphan_26:
Start_Orphan_27:
 nop
End_Orphan_27:
 ldsflda int8 ILGEN_0x38e69edd::field_0x4
 pop
End_Orphan_26:
Start_Orphan_28:
 ldloc local_0x4
 stloc local_0x4
End_Orphan_28:
Start_Orphan_29:
 nop
End_Orphan_29:
 ldc.i4.2
 ldloc local_0xa
 add.ovf.un
 conv.i1
 conv.u1
 newarr [mscorlib]System.Byte
 pop
End_Orphan_25:
Start_Orphan_2a:
 ldsfld int8 ILGEN_0x38e69edd::field_0x4
 conv.u1
 newarr [mscorlib]System.Single
Start_Orphan_2b:
 nop
End_Orphan_2b:
 ldloc local_0x11
 conv.ovf.i4
 ldsfld float32 ILGEN_0x38e69edd::field_0x8
 neg
 stelem.r4
End_Orphan_2a:
 ldc.r4 float32(0x2e1aed)
 conv.r8
 ldc.r8 float64(0x5bdf6e072e5023eb)
 rem
 conv.ovf.u
 sub.ovf
Start_Orphan_2c:
Start_Orphan_2d:
Start_Orphan_2e:
Start_Orphan_2f:
 nop
End_Orphan_2f:
 ldc.i4.8
 conv.u1
 newarr [mscorlib]System.Int64
 stloc local_0x7
End_Orphan_2e:
Start_Orphan_30:
 ldloc local_0x7
 pop
End_Orphan_30:
Start_Orphan_31:
 nop
End_Orphan_31:
 ldsfld unsigned int64 ILGEN_0x38e69edd::field_0x3
 ldsfld unsigned int64 ILGEN_0x38e69edd::field_0x3
 add
 ldsfld int16 ILGEN_0x38e69edd::field_0x5
 conv.i8
 add.ovf.un
 conv.i1
 stloc local_0xb
End_Orphan_2d:
 ldsflda unsigned int8 ILGEN_0x38e69edd::field_0x0
 pop
End_Orphan_2c:
Start_Orphan_32:
Start_Orphan_33:
Start_Orphan_34:
 ldsflda int64 ILGEN_0x38e69edd::field_0x7
 pop
End_Orphan_34:
Start_Orphan_35:
 nop
End_Orphan_35:
 ldloc local_0xf
 conv.r8
 conv.u2
 stsfld unsigned int8 ILGEN_0x38e69edd::field_0x0
End_Orphan_33:
 ldloca local_0xf
 pop
End_Orphan_32:
 ldsfld unsigned int32 ILGEN_0x38e69edd::field_0x2
 conv.ovf.i1.un
 cgt
Start_Orphan_36:
 ldc.i4.8
 stsfld unsigned int32 ILGEN_0x38e69edd::field_0x2
End_Orphan_36:
Start_Orphan_37:
 ldloc local_0x2
 pop
End_Orphan_37:
Start_Orphan_38:
Start_Orphan_39:
 nop
End_Orphan_39:
 ldsflda int64 ILGEN_0x38e69edd::field_0x7
 ldind.i8
 conv.u8
 pop
End_Orphan_38:
Start_Orphan_3a:
Start_Orphan_3b:
 nop
End_Orphan_3b:
 ldc.i4.1
 conv.u1
 newarr [mscorlib]System.Int64
Start_Orphan_3c:
 nop
End_Orphan_3c:
 ldsfld int32 ILGEN_0x38e69edd::field_0x6
 ldsfld unsigned int32 ILGEN_0x38e69edd::field_0x2
 clt.un
 ldloc local_0xe
 conv.u8
 stelem.i8
End_Orphan_3a:
 ldloc local_0x2
Start_Orphan_3d:
 ldc.i4.2
 conv.u1
 newarr [mscorlib]System.Int32
 ldc.i4.s -107
 ldc.i4.0
 cgt
Start_Orphan_3e:
 nop
End_Orphan_3e:
 ldloc local_0x11
 conv.u4
 stelem.i4
End_Orphan_3d:
Start_Orphan_3f:
 nop
End_Orphan_3f:
 ldc.i4.7
 conv.ovf.u2.un
 ldc.i8 0x20a421ce98961ed
 conv.i
 ceq
 conv.ovf.u1
 ldelema [mscorlib]System.UInt32
 ldind.u4
 conv.u1
 newarr [mscorlib]System.Int16
Start_Orphan_40:
 ldloc local_0x10
 conv.ovf.u8
 stloc local_0xf
End_Orphan_40:
Start_Orphan_41:
Start_Orphan_42:
 ldloc local_0x5
 stloc local_0x5
End_Orphan_42:
 ldc.i8 0x509653046e911561
 stloc local_0xf
End_Orphan_41:
 ldsfld unsigned int8 ILGEN_0x38e69edd::field_0x0
 conv.ovf.u.un
Start_Orphan_43:
 ldsfld unsigned int64 ILGEN_0x38e69edd::field_0x3
 conv.u4
 conv.u1
 newarr [mscorlib]System.UInt32
Start_Orphan_44:
 nop
End_Orphan_44:
 ldloc local_0x11
 conv.u8
 conv.ovf.i1
Start_Orphan_45:
 ldsflda int64 ILGEN_0x38e69edd::field_0x7
 pop
End_Orphan_45:
Start_Orphan_46:
 nop
End_Orphan_46:
 ldsfld int32 ILGEN_0x38e69edd::field_0x6
 conv.ovf.u
 ldc.i4.2
 ldloc local_0xd
 clt.un
 clt.un
 stelem.i4
End_Orphan_43:
 ldloc local_0x10
 conv.ovf.u1
 clt
 ldelem.i2
 conv.ovf.i4
 sub.ovf.un
 pop
 leave the_end
 } catch [mscorlib]System.OverflowException {
 pop
 leave the_end
 }
 the_end:
 ldc.i4 100
ret
}
}
