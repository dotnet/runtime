// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly 'indexMinusOne' {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}

.class public ILGEN_0x52ea038a {

//BEGIN STATIC FIELDS
.field static native int[] field_0x0
.field static	unsigned int16[] field_0x1
.field static	float64[] field_0x2
.field static	int64[] field_0x3
.field static	int32 field_0x4
.field static	native int field_0x5
.field static	native int field_0x6
.field static	int16 field_0x7
.field static	int16 field_0x8
.field static	float32[] field_0x9
//END STATIC FIELDS
.method static unsigned int16  Method_0xc9efd6b0(unsigned int32 Arg_0x0, int32 Arg_0x1, float64 Arg_0x2, unsigned int16 Arg_0x3, unsigned int32 Arg_0x4, unsigned int8 Arg_0x5, float64 Arg_0x6, unsigned int8 Arg_0x7, unsigned int16 Arg_0x8, int8 Arg_0x9) {
.zeroinit 
.maxstack	19

 .locals (unsigned int64[] LOCAL_0x0,int32 LOCAL_0x1,int32[] LOCAL_0x2,unsigned int16 LOCAL_0x3,unsigned int16 LOCAL_0x4,unsigned int16[] LOCAL_0x5,int8[] LOCAL_0x6,int64[] LOCAL_0x7,unsigned int32[] LOCAL_0x8,int8 LOCAL_0x9,native int LOCAL_0xa,float32[] LOCAL_0xb,native int LOCAL_0xc,native int LOCAL_0xd,unsigned int8[] LOCAL_0xe)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc LOCAL_0x0
ldc.i4 0x6226043 
stloc LOCAL_0x1
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc LOCAL_0x2
ldc.i4 0xf93bee7e 
stloc LOCAL_0x3
ldc.i4 0x4fa0b735 
stloc LOCAL_0x4
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc LOCAL_0x5
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc LOCAL_0x6
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc LOCAL_0x7
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc LOCAL_0x8
ldc.i4 0x309e59de 
stloc LOCAL_0x9
ldc.i4 0xec75ccd8 
stloc LOCAL_0xa
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc LOCAL_0xb
ldc.i4 0xdbdd468b 
stloc LOCAL_0xc
ldc.i4 0x892560d4 
stloc LOCAL_0xd
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc LOCAL_0xe

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0x52ea038a::field_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x52ea038a::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x52ea038a::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x52ea038a::field_0x3
ldc.i4 0xa1a75836 
stsfld int32 ILGEN_0x52ea038a::field_0x4
ldc.i4 0x734a8f1e 
stsfld native int ILGEN_0x52ea038a::field_0x5
ldc.i4 0xe375d55 
stsfld native int ILGEN_0x52ea038a::field_0x6
ldc.i4 0x1e8c4acc 
stsfld int16 ILGEN_0x52ea038a::field_0x7
ldc.i4 0xf09db6c5 
stsfld int16 ILGEN_0x52ea038a::field_0x8
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x52ea038a::field_0x9

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_a_0:
Start_Orphan_a_1:
Start_Orphan_a_2:
Start_Orphan_a_3:
     nop
End_Orphan_a_3:
      sizeof [mscorlib]System.UInt64
      ldsfld native int ILGEN_0x52ea038a::field_0x6
Start_Orphan_b_4:
     nop
End_Orphan_b_4:
     mul.ovf
Start_Orphan_a_5:
     nop
End_Orphan_a_5:
      ldloc LOCAL_0xb
Start_Orphan_b_6:
     nop
End_Orphan_b_6:
     ldlen
    div.un
   stsfld native int ILGEN_0x52ea038a::field_0x6
End_Orphan_a_2:
Start_Orphan_a_7:
      ldc.i4.6
     conv.u1
 newarr [mscorlib]System.Byte
    stloc LOCAL_0xe
End_Orphan_a_7:
       ldc.r4 float32(0x73677099)
      ckfinite
Start_Orphan_b_8:
      ldsfld unsigned int16[] ILGEN_0x52ea038a::field_0x1
     stsfld unsigned int16[] ILGEN_0x52ea038a::field_0x1
End_Orphan_b_8:
     call float64 [mscorlib]System.Math::Sin(float64)
Start_Orphan_b_9:
Start_Orphan_a_a:
     nop
End_Orphan_a_a:
     ldsfld unsigned int16[] ILGEN_0x52ea038a::field_0x1
      ldc.r4 float32(0xfc6b5ad0)
Start_Orphan_b_b:
     nop
End_Orphan_b_b:
     conv.ovf.i4
      ldarga Arg_0x4
     ldind.u4
    stelem.i2
End_Orphan_b_9:
    call float64 [mscorlib]System.Math::Cos(float64)
Start_Orphan_b_c:
Start_Orphan_a_d:
     nop
End_Orphan_a_d:
      sizeof [mscorlib]System.SByte
      ldloc LOCAL_0xc
Start_Orphan_b_e:
     nop
End_Orphan_b_e:
     sub.ovf.un
Start_Orphan_b_f:
      ldsfld int64[] ILGEN_0x52ea038a::field_0x3
      sizeof [mscorlib]System.UInt16
     ldelema [mscorlib]System.Int64
    pop
End_Orphan_b_f:
    conv.u
Start_Orphan_a_10:
     nop
End_Orphan_a_10:
Start_Orphan_b_11:
     nop
End_Orphan_b_11:
     ldsflda int16 ILGEN_0x52ea038a::field_0x8
    ldind.i2
 blt Branch_0x2

Start_Orphan_a_12:
     nop
End_Orphan_a_12:
      ldsfld float64[] ILGEN_0x52ea038a::field_0x2
      ldc.i4.8
     ldelema [mscorlib]System.Double
Start_Orphan_a_13:
     nop
End_Orphan_a_13:
      ldarg Arg_0x2
Start_Orphan_b_14:
     nop
End_Orphan_b_14:
     call float64 [mscorlib]System.Math::Cos(float64)
    stind.r8
 br Branch_0x3

 Branch_0x2:

Start_Orphan_a_15:
     nop
End_Orphan_a_15:
      ldarg Arg_0x3
      sizeof [mscorlib]System.UInt64
Start_Orphan_b_16:
     nop
End_Orphan_b_16:
     shl
    stloc LOCAL_0x3
 Branch_0x3:

End_Orphan_b_c:
   conv.ovf.u4
  stsfld int16 ILGEN_0x52ea038a::field_0x8
End_Orphan_a_1:
Start_Orphan_a_17:
Start_Orphan_a_18:
     ldsfld float64[] ILGEN_0x52ea038a::field_0x2
    pop
End_Orphan_a_18:
     ldsfld float64[] ILGEN_0x52ea038a::field_0x2
       ldc.i8 0x946b799c90ed588
       ldc.i8 0xf702697f645198e3
Start_Orphan_b_19:
      nop
End_Orphan_b_19:
      rem
     conv.u2
    ldelema [mscorlib]System.Double
Start_Orphan_b_1a:
Start_Orphan_a_1b:
     nop
End_Orphan_a_1b:
Start_Orphan_b_1c:
     nop
End_Orphan_b_1c:
     ldloc LOCAL_0x2
    stloc LOCAL_0x2
End_Orphan_b_1a:
    ldarga Arg_0x2
   cpobj [mscorlib]System.Double
End_Orphan_a_17:
Start_Orphan_a_1d:
Start_Orphan_a_1e:
      ldarga Arg_0x4
      sizeof [mscorlib]System.Int64
     stobj [mscorlib]System.UInt32
End_Orphan_a_1e:
     ldsflda int16 ILGEN_0x52ea038a::field_0x8
    initobj [mscorlib]System.Int16
End_Orphan_a_1d:
Start_Orphan_a_1f:
Start_Orphan_a_20:
      nop
End_Orphan_a_20:
       ldc.i4.4
      conv.u1
 newarr [mscorlib]System.Int32
       ldarga Arg_0x0
Start_Orphan_b_21:
      nop
End_Orphan_b_21:
      ldind.u4
       ldc.i8 0x121ca753dbf805
       ldc.i8 0xa62077e516b131
Start_Orphan_b_22:
      nop
End_Orphan_b_22:
      cgt.un
     stelem.i4
End_Orphan_a_1f:
Start_Orphan_a_23:
       ldsfld native int[] ILGEN_0x52ea038a::field_0x0
      pop
End_Orphan_a_23:
Start_Orphan_a_24:
       nop
End_Orphan_a_24:
        ldc.i4.4
Start_Orphan_b_25:
       nop
End_Orphan_b_25:
       conv.u1
 newarr [mscorlib]System.UInt16
Start_Orphan_b_26:
       ldloca LOCAL_0x1
      pop
End_Orphan_b_26:
      ldlen
      ldloc LOCAL_0xc
     mul
Start_Orphan_a_27:
Start_Orphan_a_28:
      nop
End_Orphan_a_28:
      ldloc LOCAL_0x8
     stloc LOCAL_0x8
End_Orphan_a_27:
Start_Orphan_a_29:
       ldloca LOCAL_0xd
      pop
End_Orphan_a_29:
      ldc.i8 0xf48a104d13901e4f
     conv.ovf.u.un
Start_Orphan_b_2a:
Start_Orphan_a_2b:
      ldsflda int32 ILGEN_0x52ea038a::field_0x4
      ldarg Arg_0x4
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int32
End_Orphan_a_2b:
     ldsflda int16 ILGEN_0x52ea038a::field_0x8
Start_Orphan_a_2c:
      nop
End_Orphan_a_2c:
        ldloc LOCAL_0xb
        sizeof [mscorlib]System.Byte
       ldelema [mscorlib]System.Single
Start_Orphan_b_2d:
      nop
End_Orphan_b_2d:
      ldobj [mscorlib]System.Single
Start_Orphan_b_2e:
      ldarga Arg_0x8
      ldc.i4.0
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt16
End_Orphan_b_2e:
     conv.ovf.i1
    stobj [mscorlib]System.Int16
End_Orphan_b_2a:
    sub.ovf
Start_Orphan_a_2f:
    nop
End_Orphan_a_2f:
Start_Orphan_a_30:
       ldarga Arg_0x0
       ldc.i4.3
      stind.i4
End_Orphan_a_30:
Start_Orphan_a_31:
       nop
End_Orphan_a_31:
        ldloc LOCAL_0xd
       not
        ldsfld native int ILGEN_0x52ea038a::field_0x6
       conv.ovf.i4
Start_Orphan_b_32:
       ldloc LOCAL_0xe
      pop
End_Orphan_b_32:
      add.ovf.un
Start_Orphan_a_33:
       ldsflda native int ILGEN_0x52ea038a::field_0x6
       ldsfld native int ILGEN_0x52ea038a::field_0x6
      stobj [mscorlib]System.IntPtr
End_Orphan_a_33:
Start_Orphan_a_34:
       nop
End_Orphan_a_34:
         ldloc LOCAL_0x0
         ldloc LOCAL_0x4
        ldelema [mscorlib]System.UInt64
       ldobj [mscorlib]System.UInt64
      conv.ovf.i2.un
Start_Orphan_b_35:
Start_Orphan_a_36:
      nop
End_Orphan_a_36:
       ldloc LOCAL_0x6
       ldc.i4.7
Start_Orphan_b_37:
      nop
End_Orphan_b_37:
      ldelema [mscorlib]System.SByte
Start_Orphan_a_38:
      nop
End_Orphan_a_38:
       ldloc LOCAL_0x6
       sizeof [mscorlib]System.Int64
      ldelema [mscorlib]System.SByte
     cpobj [mscorlib]System.SByte
End_Orphan_b_35:
     and
Start_Orphan_a_39:
        ldloc LOCAL_0x7
        ldarg Arg_0x0
       ldelema [mscorlib]System.Int64
      pop
End_Orphan_a_39:
        ldloc LOCAL_0xd
        ldloc LOCAL_0xd
Start_Orphan_b_3a:
       nop
End_Orphan_b_3a:
       mul
        ldsfld native int ILGEN_0x52ea038a::field_0x5
Start_Orphan_b_3b:
       nop
End_Orphan_b_3b:
       conv.ovf.i1.un
Start_Orphan_b_3c:
       ldsflda native int ILGEN_0x52ea038a::field_0x6
       ldloca LOCAL_0xc
      cpobj [mscorlib]System.IntPtr
End_Orphan_b_3c:
      rem
Start_Orphan_a_3d:
       ldarg Arg_0x8
      stloc LOCAL_0x1
End_Orphan_a_3d:
Start_Orphan_a_3e:
       nop
End_Orphan_a_3e:
        ldarg Arg_0x1
       conv.u1
 newarr [mscorlib]System.UInt32
Start_Orphan_a_3f:
       nop
End_Orphan_a_3f:
        ldsfld native int ILGEN_0x52ea038a::field_0x5
       conv.ovf.i4
      ldelem.u4
     clt.un
    add.ovf
Start_Orphan_b_40:
    ldarga Arg_0x4
   initobj [mscorlib]System.UInt32
End_Orphan_b_40:
   rem.un
Start_Orphan_a_41:
    ldloca LOCAL_0x9
Start_Orphan_a_42:
      ldloc LOCAL_0x7
      ldc.i4.m1
      ldc.i8 0xd2d7c279562153b8
     stelem.i8
End_Orphan_a_42:
Start_Orphan_a_43:
      nop
End_Orphan_a_43:
Start_Orphan_b_44:
      nop
End_Orphan_b_44:
      ldloca LOCAL_0xa
Start_Orphan_b_45:
      ldarg Arg_0x6
     starg Arg_0x2
End_Orphan_b_45:
     unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.IntPtr
Start_Orphan_a_46:
      nop
End_Orphan_a_46:
       ldc.i4.8
      conv.ovf.i1
       ldsflda int32 ILGEN_0x52ea038a::field_0x4
Start_Orphan_b_47:
      nop
End_Orphan_b_47:
      ldobj [mscorlib]System.Int32
     sub.ovf.un
    clt.un
   stobj [mscorlib]System.SByte
End_Orphan_a_41:
    ldarg Arg_0x8
   conv.ovf.u2.un
  div
Start_Orphan_a_48:
    ldloc LOCAL_0x6
Start_Orphan_b_49:
      ldloc LOCAL_0x8
     pop
End_Orphan_b_49:
     sizeof [mscorlib]System.Int64
    conv.i2
Start_Orphan_b_4a:
Start_Orphan_a_4b:
      ldloc LOCAL_0x0
      sizeof [mscorlib]System.Double
     ldelema [mscorlib]System.UInt64
      ldloc LOCAL_0x0
      ldc.i4.s 76
     ldelema [mscorlib]System.UInt64
    cpobj [mscorlib]System.UInt64
End_Orphan_a_4b:
      ldsfld native int ILGEN_0x52ea038a::field_0x5
     conv.i2
      ldc.i4 0x1e0852d4
      ldloc LOCAL_0xd
Start_Orphan_b_4c:
     nop
End_Orphan_b_4c:
     add
Start_Orphan_b_4d:
     ldsflda int32 ILGEN_0x52ea038a::field_0x4
     ldc.i4.3
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int32
End_Orphan_b_4d:
    clt
   starg Arg_0x0
End_Orphan_b_4a:
   ldelema [mscorlib]System.SByte
  pop
End_Orphan_a_48:
Start_Orphan_a_4e:
Start_Orphan_a_4f:
      ldsfld int64[] ILGEN_0x52ea038a::field_0x3
      sizeof [mscorlib]System.Byte
     ldelema [mscorlib]System.Int64
    initobj [mscorlib]System.Int64
End_Orphan_a_4f:
Start_Orphan_b_50:
      ldloc LOCAL_0xb
      ldarg Arg_0x8
     ldelema [mscorlib]System.Single
    initobj [mscorlib]System.Single
End_Orphan_b_50:
    ldarga Arg_0x1
   initobj [mscorlib]System.Int32
End_Orphan_a_4e:
Start_Orphan_a_51:
Start_Orphan_a_52:
      nop
End_Orphan_a_52:
       ldsfld native int ILGEN_0x52ea038a::field_0x6
       ldc.i4.8
Start_Orphan_b_53:
      nop
End_Orphan_b_53:
      cgt
Start_Orphan_b_54:
      ldc.r8 float64(0x76932e6bad84e4bd)
     starg Arg_0x6
End_Orphan_b_54:
     conv.u1
 newarr [mscorlib]System.IntPtr
Start_Orphan_a_55:
      ldloc LOCAL_0xe
      ldarg Arg_0x5
      sizeof [mscorlib]System.Single
     stelem.i1
End_Orphan_a_55:
     ldarg Arg_0x9
       ldsfld native int ILGEN_0x52ea038a::field_0x6
       ldsfld native int ILGEN_0x52ea038a::field_0x5
Start_Orphan_b_56:
      nop
End_Orphan_b_56:
      add.ovf.un
     conv.i
    stelem.i
End_Orphan_a_51:
Start_Orphan_a_57:
       nop
End_Orphan_a_57:
        ldc.i8 0xbb4ac1a8ed5e7cde
        ldc.i8 0xfab25ea9c213f774
Start_Orphan_b_58:
       nop
End_Orphan_b_58:
       and
        ldc.r4 float32(0xa47363dc)
Start_Orphan_b_59:
       nop
End_Orphan_b_59:
       conv.ovf.i8
      sub.ovf
Start_Orphan_b_5a:
       ldloc LOCAL_0x0
       ldarg Arg_0x9
Start_Orphan_b_5b:
      nop
End_Orphan_b_5b:
      ldelema [mscorlib]System.UInt64
       ldc.r8 float64(0xea9b2fa0e5712f83)
Start_Orphan_b_5c:
      nop
End_Orphan_b_5c:
      conv.u8
     stobj [mscorlib]System.UInt64
End_Orphan_b_5a:
     conv.i4
Start_Orphan_b_5d:
Start_Orphan_a_5e:
      ldarga Arg_0x5
     pop
End_Orphan_a_5e:
     ldarga Arg_0x7
    pop
End_Orphan_b_5d:
    conv.r8
   conv.ovf.i
Start_Orphan_a_5f:
Start_Orphan_a_60:
Start_Orphan_a_61:
     nop
End_Orphan_a_61:
      ldc.i8 0x64918e24a4088e59
      ldc.i8 0x8af4a622e2cda513
Start_Orphan_b_62:
     nop
End_Orphan_b_62:
     ceq
    starg Arg_0x1
End_Orphan_a_60:
Start_Orphan_a_63:
       ldsfld int64[] ILGEN_0x52ea038a::field_0x3
       sizeof [mscorlib]System.Int32
      ldelema [mscorlib]System.Int64
      ldc.i8 0x81df38d02161f4e3
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int64
End_Orphan_a_63:
Start_Orphan_b_64:
       ldsfld float32[] ILGEN_0x52ea038a::field_0x9
       ldc.i4.7
      ldelema [mscorlib]System.Single
      ldc.r4 float32(0x37967f7c)
     stind.r4
End_Orphan_b_64:
     ldloc LOCAL_0x7
Start_Orphan_a_65:
      nop
End_Orphan_a_65:
       ldarga Arg_0x2
      ldind.r8
     conv.ovf.i4
Start_Orphan_b_66:
      ldsflda native int ILGEN_0x52ea038a::field_0x6
Start_Orphan_b_67:
     nop
End_Orphan_b_67:
     ldobj [mscorlib]System.IntPtr
    stloc LOCAL_0xd
End_Orphan_b_66:
    ldelema [mscorlib]System.Int64
   initobj [mscorlib]System.Int64
End_Orphan_a_5f:
Start_Orphan_a_68:
       ldloc LOCAL_0x6
       ldarg Arg_0x5
       ldarg Arg_0x1
      stelem.i1
End_Orphan_a_68:
Start_Orphan_a_69:
       nop
End_Orphan_a_69:
       ldsfld int64[] ILGEN_0x52ea038a::field_0x3
        ldloc LOCAL_0xd
        ldarg Arg_0x9
       clt.un
Start_Orphan_b_6a:
       ldloc LOCAL_0x6
      stloc LOCAL_0x6
End_Orphan_b_6a:
      ldelema [mscorlib]System.Int64
Start_Orphan_b_6b:
Start_Orphan_a_6c:
      nop
End_Orphan_a_6c:
       ldsfld float64[] ILGEN_0x52ea038a::field_0x2
      ldlen
     starg Arg_0x9
End_Orphan_b_6b:
     ldobj [mscorlib]System.Int64
Start_Orphan_a_6d:
       ldloc LOCAL_0xb
       sizeof [mscorlib]System.UInt32
Start_Orphan_b_6e:
      nop
End_Orphan_b_6e:
      ldelema [mscorlib]System.Single
Start_Orphan_a_6f:
      nop
End_Orphan_a_6f:
       ldsfld float32[] ILGEN_0x52ea038a::field_0x9
       ldarg Arg_0x8
      ldelema [mscorlib]System.Single
     cpobj [mscorlib]System.Single
End_Orphan_a_6d:
Start_Orphan_a_70:
        ldloc LOCAL_0x7
        ldarg Arg_0x7
       ldelema [mscorlib]System.Int64
      initobj [mscorlib]System.Int64
End_Orphan_a_70:
Start_Orphan_a_71:
       nop
End_Orphan_a_71:
        ldc.i8 0xfc5a1273891c871c
       conv.ovf.i8.un
Start_Orphan_a_72:
       nop
End_Orphan_a_72:
         ldloc LOCAL_0x0
         sizeof [mscorlib]System.IntPtr
        ldelema [mscorlib]System.UInt64
       unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.UInt64
      or
     conv.ovf.i8
Start_Orphan_b_73:
Start_Orphan_a_74:
      nop
End_Orphan_a_74:
Start_Orphan_b_75:
      nop
End_Orphan_b_75:
      ldloc LOCAL_0x0
       ldsflda int16 ILGEN_0x52ea038a::field_0x7
      unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Int16
Start_Orphan_b_76:
      ldloca LOCAL_0x9
      ldc.i4.0
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.SByte
End_Orphan_b_76:
     ldelema [mscorlib]System.UInt64
    pop
End_Orphan_b_73:
    sub
   conv.ovf.i1.un
  sub.ovf.un
 blt Branch_0x0

Start_Orphan_a_77:
       ldloc LOCAL_0xe
       sizeof [mscorlib]System.UInt16
Start_Orphan_b_78:
      nop
End_Orphan_b_78:
      ldelema [mscorlib]System.Byte
       ldloc LOCAL_0xc
Start_Orphan_b_79:
      nop
End_Orphan_b_79:
      conv.ovf.i2
     stobj [mscorlib]System.Byte
End_Orphan_a_77:
     ldarg Arg_0x5
Start_Orphan_b_7a:
Start_Orphan_b_7b:
      ldloc LOCAL_0xe
     stloc LOCAL_0xe
End_Orphan_b_7b:
     ldsflda int16 ILGEN_0x52ea038a::field_0x8
Start_Orphan_a_7c:
      ldsfld native int ILGEN_0x52ea038a::field_0x5
     stsfld native int ILGEN_0x52ea038a::field_0x5
End_Orphan_a_7c:
       ldc.i8 0x695d268d280362f6
Start_Orphan_b_7d:
      nop
End_Orphan_b_7d:
      not
        ldloc LOCAL_0x0
        ldsfld int16 ILGEN_0x52ea038a::field_0x8
       ldelema [mscorlib]System.UInt64
      unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.UInt64
Start_Orphan_b_7e:
      ldloc LOCAL_0x0
     pop
End_Orphan_b_7e:
     clt
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int16
End_Orphan_b_7a:
    conv.u1
 newarr [mscorlib]System.Byte
   ldlen
  stsfld int32 ILGEN_0x52ea038a::field_0x4
 br Branch_0x1

 Branch_0x0:

Start_Orphan_b_7f:
    ldarga Arg_0x7
       ldsfld float32[] ILGEN_0x52ea038a::field_0x9
Start_Orphan_b_80:
      nop
End_Orphan_b_80:
      ldlen
Start_Orphan_b_81:
      ldloc LOCAL_0xe
     pop
End_Orphan_b_81:
     conv.u1
 newarr [mscorlib]System.IntPtr
Start_Orphan_b_82:
Start_Orphan_a_83:
     nop
End_Orphan_a_83:
     ldarga Arg_0x5
Start_Orphan_a_84:
     nop
End_Orphan_a_84:
      sizeof [mscorlib]System.Int32
      sizeof [mscorlib]System.SByte
Start_Orphan_b_85:
     nop
End_Orphan_b_85:
     shl
    stobj [mscorlib]System.Byte
End_Orphan_b_82:
    ldlen
   stind.i1
End_Orphan_b_7f:
   ldloc LOCAL_0x7
  pop
 Branch_0x1:

End_Orphan_a_0:
Start_Orphan_a_86:
   ldsflda int16 ILGEN_0x52ea038a::field_0x8
Start_Orphan_b_87:
Start_Orphan_a_88:
Start_Orphan_b_89:
     nop
End_Orphan_b_89:
     ldloc LOCAL_0x6
    pop
End_Orphan_a_88:
    ldsfld int64[] ILGEN_0x52ea038a::field_0x3
   stloc LOCAL_0x7
End_Orphan_b_87:
   ldsflda int16 ILGEN_0x52ea038a::field_0x8
  cpobj [mscorlib]System.Int16
End_Orphan_a_86:
Start_Orphan_a_8a:
Start_Orphan_a_8b:
Start_Orphan_a_8c:
     nop
End_Orphan_a_8c:
Start_Orphan_b_8d:
       ldloc LOCAL_0x0
       ldc.i4.2
      ldelema [mscorlib]System.UInt64
      ldc.i8 0xd6d2e2555e823e6d
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt64
End_Orphan_b_8d:
     ldsfld native int[] ILGEN_0x52ea038a::field_0x0
    pop
End_Orphan_a_8b:
    ldloca LOCAL_0x1
Start_Orphan_b_8e:
       sizeof [mscorlib]System.Int16
Start_Orphan_b_8f:
      nop
End_Orphan_b_8f:
      conv.u1
 newarr [mscorlib]System.UInt32
     pop
End_Orphan_b_8e:
     ldarga Arg_0x3
    ldobj [mscorlib]System.UInt16
   stind.i4
End_Orphan_a_8a:
Start_Orphan_b_90:
Start_Orphan_a_91:
        ldsfld float32[] ILGEN_0x52ea038a::field_0x9
        ldarg Arg_0x3
       ldelema [mscorlib]System.Single
       ldarg Arg_0x2
      stind.r4
End_Orphan_a_91:
Start_Orphan_b_92:
        ldloc LOCAL_0x0
        ldarg Arg_0x7
       ldelema [mscorlib]System.UInt64
      pop
End_Orphan_b_92:
      ldsflda int16 ILGEN_0x52ea038a::field_0x8
Start_Orphan_a_93:
       nop
End_Orphan_a_93:
Start_Orphan_b_94:
       nop
End_Orphan_b_94:
       ldarg Arg_0x2
Start_Orphan_b_95:
       ldsfld unsigned int16[] ILGEN_0x52ea038a::field_0x1
       ldc.i4.0
       ldarg Arg_0x1
      stelem.i2
End_Orphan_b_95:
      conv.u2
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int16
End_Orphan_b_90:
     ldloc LOCAL_0x0
Start_Orphan_a_96:
        ldloc LOCAL_0x2
        sizeof [mscorlib]System.IntPtr
        sizeof [mscorlib]System.UInt32
       stelem.i4
End_Orphan_a_96:
         ldc.i8 0xd04934d5616cc9ea
         ldarg Arg_0x4
        shr
Start_Orphan_b_97:
        ldloca LOCAL_0x9
       pop
End_Orphan_b_97:
       conv.i
      conv.r4
         ldc.i4.4
         ldsfld int32 ILGEN_0x52ea038a::field_0x4
Start_Orphan_b_98:
        nop
End_Orphan_b_98:
        sub
       conv.u1
 newarr [mscorlib]System.Single
Start_Orphan_a_99:
        ldarga Arg_0x3
        sizeof [mscorlib]System.SByte
       stind.i2
End_Orphan_a_99:
Start_Orphan_b_9a:
        sizeof [mscorlib]System.Double
       stsfld int32 ILGEN_0x52ea038a::field_0x4
End_Orphan_b_9a:
       ldc.i4.0
      ldelem.r4
Start_Orphan_b_9b:
Start_Orphan_a_9c:
       ldloc LOCAL_0x8
      stloc LOCAL_0x8
End_Orphan_a_9c:
      ldloc LOCAL_0xb
Start_Orphan_a_9d:
       nop
End_Orphan_a_9d:
        ldarg Arg_0x5
        ldc.i4.s -37
Start_Orphan_b_9e:
       nop
End_Orphan_b_9e:
       ceq
Start_Orphan_b_9f:
       ldloc LOCAL_0x0
      pop
End_Orphan_b_9f:
      conv.ovf.u1.un
Start_Orphan_a_a0:
       ldloc LOCAL_0xb
      stsfld float32[] ILGEN_0x52ea038a::field_0x9
End_Orphan_a_a0:
         ldsfld float32[] ILGEN_0x52ea038a::field_0x9
         ldc.i4.m1
        ldelema [mscorlib]System.Single
       ldind.r4
Start_Orphan_a_a1:
       nop
End_Orphan_a_a1:
Start_Orphan_b_a2:
       nop
End_Orphan_b_a2:
       ldc.r8 float64(0x7fd973d85d85b284)
Start_Orphan_b_a3:
        ldloc LOCAL_0x7
        ldc.i4.m1
       ldelema [mscorlib]System.Int64
       ldc.i8 0x3ce53a98ebe5f8c9
      stobj [mscorlib]System.Int64
End_Orphan_b_a3:
      mul 
     stelem.r4
End_Orphan_b_9b:
     ceq
Start_Orphan_b_a4:
Start_Orphan_a_a5:
Start_Orphan_b_a6:
      nop
End_Orphan_b_a6:
      ldloca LOCAL_0x9
       ldloc LOCAL_0x6
       ldc.i4.m1
      ldelem.i1
     stind.i1
End_Orphan_a_a5:
Start_Orphan_b_a7:
Start_Orphan_b_a8:
      nop
End_Orphan_b_a8:
      ldarga Arg_0x6
Start_Orphan_a_a9:
      nop
End_Orphan_a_a9:
       ldc.r4 float32(0x7eebea62)
Start_Orphan_b_aa:
      nop
End_Orphan_b_aa:
      neg
     stind.r8
End_Orphan_b_a7:
     ldloca LOCAL_0x4
Start_Orphan_b_ab:
Start_Orphan_b_ac:
      nop
End_Orphan_b_ac:
      ldloc LOCAL_0x7
     stloc LOCAL_0x7
End_Orphan_b_ab:
     ldc.i4.0
    stind.i2
End_Orphan_b_a4:
    ldelema [mscorlib]System.UInt64
Start_Orphan_b_ad:
Start_Orphan_a_ae:
       nop
End_Orphan_a_ae:
        ldc.i4.4
Start_Orphan_b_af:
       nop
End_Orphan_b_af:
       conv.u1
 newarr [mscorlib]System.UInt64
Start_Orphan_b_b0:
       ldsfld native int[] ILGEN_0x52ea038a::field_0x0
      stsfld native int[] ILGEN_0x52ea038a::field_0x0
End_Orphan_b_b0:
      ldlen
Start_Orphan_b_b1:
       sizeof [mscorlib]System.SByte
      conv.u1
 newarr [mscorlib]System.Int32
     pop
End_Orphan_b_b1:
     conv.u1
 newarr [mscorlib]System.Double
Start_Orphan_a_b2:
Start_Orphan_a_b3:
      nop
End_Orphan_a_b3:
       sizeof [mscorlib]System.UInt64
      conv.u1
 newarr [mscorlib]System.UInt64
     pop
End_Orphan_a_b2:
Start_Orphan_b_b4:
       ldsfld unsigned int16[] ILGEN_0x52ea038a::field_0x1
      pop
End_Orphan_b_b4:
      ldarga Arg_0x0
     ldobj [mscorlib]System.UInt32
    ldelema [mscorlib]System.Double
Start_Orphan_a_b5:
       ldsfld native int[] ILGEN_0x52ea038a::field_0x0
      stsfld native int[] ILGEN_0x52ea038a::field_0x0
End_Orphan_a_b5:
Start_Orphan_a_b6:
       nop
End_Orphan_a_b6:
        ldc.i4.3
        sizeof [mscorlib]System.Byte
       mul.ovf.un
Start_Orphan_b_b7:
       ldloca LOCAL_0x1
       ldc.i4.m1
      stobj [mscorlib]System.Int32
End_Orphan_b_b7:
      conv.u1
 newarr [mscorlib]System.Single
Start_Orphan_a_b8:
       ldloca LOCAL_0x4
       ldarga Arg_0x3
      cpobj [mscorlib]System.UInt16
End_Orphan_a_b8:
Start_Orphan_b_b9:
       ldarga Arg_0x2
       ldc.r8 float64(0x7600c4cbb631417e)
      stobj [mscorlib]System.Double
End_Orphan_b_b9:
      ldc.i4.5
Start_Orphan_b_ba:
Start_Orphan_a_bb:
      nop
End_Orphan_a_bb:
       ldc.r4 float32(0x4a591d73)
      conv.u1
     starg Arg_0x5
End_Orphan_b_ba:
     ldelema [mscorlib]System.Single
    ldobj [mscorlib]System.Single
   unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Double
End_Orphan_b_ad:
   unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.UInt64
Start_Orphan_a_bc:
Start_Orphan_a_bd:
Start_Orphan_a_be:
      nop
End_Orphan_a_be:
Start_Orphan_b_bf:
      nop
End_Orphan_b_bf:
      ldloc LOCAL_0x0
       ldloc LOCAL_0xc
Start_Orphan_b_c0:
      nop
End_Orphan_b_c0:
      conv.ovf.u1
Start_Orphan_b_c1:
      ldarga Arg_0x6
      ldc.r8 float64(0xf44d7a0246739982)
     stind.r8
End_Orphan_b_c1:
     ldelema [mscorlib]System.UInt64
Start_Orphan_a_c2:
      nop
End_Orphan_a_c2:
       ldc.i8 0x3bdf4c3fb17cca67
       ldc.i8 0xd8688a384c4c008e
      or
Start_Orphan_a_c3:
      nop
End_Orphan_a_c3:
       ldc.r4 float32(0x54b8d224)
Start_Orphan_b_c4:
      nop
End_Orphan_b_c4:
      conv.ovf.i8
     rem
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt64
End_Orphan_a_bd:
     ldsfld int64[] ILGEN_0x52ea038a::field_0x3
Start_Orphan_a_c5:
       ldarg Arg_0x7
      starg Arg_0x0
End_Orphan_a_c5:
Start_Orphan_b_c6:
       nop
End_Orphan_b_c6:
       ldarg Arg_0x2
Start_Orphan_b_c7:
       ldsflda int32 ILGEN_0x52ea038a::field_0x4
       sizeof [mscorlib]System.IntPtr
      unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int32
End_Orphan_b_c7:
      conv.ovf.u1
Start_Orphan_a_c8:
       ldloca LOCAL_0x9
       ldarg Arg_0x9
      stobj [mscorlib]System.SByte
End_Orphan_a_c8:
      ldc.i4.5
Start_Orphan_b_c9:
       sizeof [mscorlib]System.Int32
      conv.ovf.u1
     starg Arg_0x9
End_Orphan_b_c9:
     div.un
Start_Orphan_b_ca:
Start_Orphan_a_cb:
      ldarga Arg_0x0
     initobj [mscorlib]System.UInt32
End_Orphan_a_cb:
Start_Orphan_a_cc:
      nop
End_Orphan_a_cc:
       ldsfld native int ILGEN_0x52ea038a::field_0x5
       ldarg Arg_0x1
Start_Orphan_b_cd:
      nop
End_Orphan_b_cd:
      ceq
Start_Orphan_b_ce:
      sizeof [mscorlib]System.Single
     stsfld int32 ILGEN_0x52ea038a::field_0x4
End_Orphan_b_ce:
     conv.u1
 newarr [mscorlib]System.Single
    stloc LOCAL_0xb
End_Orphan_b_ca:
    ldelema [mscorlib]System.Int64
   initobj [mscorlib]System.Int64
End_Orphan_a_bc:
Start_Orphan_a_cf:
     ldsflda int16 ILGEN_0x52ea038a::field_0x7
    pop
End_Orphan_a_cf:
Start_Orphan_a_d0:
Start_Orphan_b_d1:
       ldsfld unsigned int16[] ILGEN_0x52ea038a::field_0x1
      pop
End_Orphan_b_d1:
      ldloc LOCAL_0xe
     stloc LOCAL_0xe
End_Orphan_a_d0:
Start_Orphan_a_d2:
        ldarga Arg_0x0
        sizeof [mscorlib]System.Double
       unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt32
End_Orphan_a_d2:
Start_Orphan_a_d3:
        nop
End_Orphan_a_d3:
         ldloc LOCAL_0xe
         ldloc LOCAL_0x9
        ldelema [mscorlib]System.Byte
Start_Orphan_b_d4:
         ldsfld float32[] ILGEN_0x52ea038a::field_0x9
         ldc.i4.7
        ldelema [mscorlib]System.Single
        ldarg Arg_0x6
       stind.r4
End_Orphan_b_d4:
       ldobj [mscorlib]System.Byte
      conv.u1
 newarr [mscorlib]System.Single
Start_Orphan_a_d5:
Start_Orphan_a_d6:
       nop
End_Orphan_a_d6:
Start_Orphan_b_d7:
       nop
End_Orphan_b_d7:
       ldsflda int16 ILGEN_0x52ea038a::field_0x7
        ldc.i4.s -51
        sizeof [mscorlib]System.UInt16
       rem
      stobj [mscorlib]System.Int16
End_Orphan_a_d5:
Start_Orphan_a_d8:
        ldarga Arg_0x4
        sizeof [mscorlib]System.Double
       stobj [mscorlib]System.UInt32
End_Orphan_a_d8:
       ldloc LOCAL_0xe
Start_Orphan_a_d9:
        nop
End_Orphan_a_d9:
         ldc.i8 0x7009bb268ecad21
         ldc.i8 0xb8d53afb3be975e2
        mul
Start_Orphan_b_da:
        sizeof [mscorlib]System.Int64
       stloc LOCAL_0x1
End_Orphan_b_da:
       conv.ovf.i1
Start_Orphan_b_db:
Start_Orphan_a_dc:
       nop
End_Orphan_a_dc:
Start_Orphan_b_dd:
       nop
End_Orphan_b_dd:
       ldsfld float32[] ILGEN_0x52ea038a::field_0x9
        ldarg Arg_0x1
        ldsfld int32 ILGEN_0x52ea038a::field_0x4
       shr.un
        ldarg Arg_0x2
       call float64 [mscorlib]System.Math::Sqrt(float64)
      stelem.r4
End_Orphan_b_db:
      ldelem.u1
Start_Orphan_b_de:
      ldsfld float64[] ILGEN_0x52ea038a::field_0x2
Start_Orphan_a_df:
        ldloc LOCAL_0x7
        sizeof [mscorlib]System.IntPtr
       ldelema [mscorlib]System.Int64
      initobj [mscorlib]System.Int64
End_Orphan_a_df:
Start_Orphan_a_e0:
       nop
End_Orphan_a_e0:
        ldc.i8 0x456195023d16d07
        ldc.i8 0xb438e8e6b13a3b9
Start_Orphan_b_e1:
       nop
End_Orphan_b_e1:
       sub
        ldc.i8 0x1f19c9e0417d1802
        ldc.i8 0xb8550462e62347c7
       sub.ovf.un
      clt
Start_Orphan_a_e2:
       ldarga Arg_0x7
      initobj [mscorlib]System.Byte
End_Orphan_a_e2:
Start_Orphan_b_e3:
       sizeof [mscorlib]System.Int64
      starg Arg_0x5
End_Orphan_b_e3:
      ldc.r8 float64(0x50ace092ddaf3332)
     stelem.r8
End_Orphan_b_de:
     ldelem.r4
    conv.ovf.u8
   not
  cgt.un
Start_Orphan_b_e4:
  ldarga Arg_0x0
Start_Orphan_a_e5:
Start_Orphan_a_e6:
Start_Orphan_a_e7:
     nop
End_Orphan_a_e7:
      ldarg Arg_0x4
     conv.u1
 newarr [mscorlib]System.Double
    stsfld float64[] ILGEN_0x52ea038a::field_0x2
End_Orphan_a_e6:
Start_Orphan_a_e8:
       ldloc LOCAL_0x0
       ldc.i4.3
      ldelema [mscorlib]System.UInt64
      ldc.i8 0x8392ff4feaf5f80e
     stind.i8
End_Orphan_a_e8:
       ldc.r4 float32(0xd967f9d5)
      conv.ovf.u8
Start_Orphan_b_e9:
      ldloca LOCAL_0xc
      ldloca LOCAL_0xa
     cpobj [mscorlib]System.IntPtr
End_Orphan_b_e9:
     conv.u2
Start_Orphan_b_ea:
      ldsfld float64[] ILGEN_0x52ea038a::field_0x2
      ldarg Arg_0x1
Start_Orphan_b_eb:
     nop
End_Orphan_b_eb:
     ldelema [mscorlib]System.Double
Start_Orphan_a_ec:
     nop
End_Orphan_a_ec:
      ldarga Arg_0x6
     unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Double
    stind.r8
End_Orphan_b_ea:
    conv.u1
 newarr [mscorlib]System.Single
Start_Orphan_a_ed:
Start_Orphan_a_ee:
     nop
End_Orphan_a_ee:
      sizeof [mscorlib]System.IntPtr
     conv.u1
 newarr [mscorlib]System.UInt16
Start_Orphan_a_ef:
     nop
End_Orphan_a_ef:
      ldarg Arg_0x4
      ldloc LOCAL_0xa
Start_Orphan_b_f0:
     nop
End_Orphan_b_f0:
     clt.un
      ldarga Arg_0x5
Start_Orphan_b_f1:
     nop
End_Orphan_b_f1:
     unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Byte
    stelem.i2
End_Orphan_a_ed:
     ldloca LOCAL_0x1
Start_Orphan_b_f2:
Start_Orphan_a_f3:
     nop
End_Orphan_a_f3:
      ldsfld int16 ILGEN_0x52ea038a::field_0x8
Start_Orphan_b_f4:
     nop
End_Orphan_b_f4:
     conv.u1
 newarr [mscorlib]System.IntPtr
      ldarg Arg_0x9
      ldarg Arg_0x1
Start_Orphan_b_f5:
     nop
End_Orphan_b_f5:
     cgt
Start_Orphan_a_f6:
     nop
End_Orphan_a_f6:
      ldsfld native int ILGEN_0x52ea038a::field_0x6
      ldsfld native int ILGEN_0x52ea038a::field_0x5
Start_Orphan_b_f7:
     nop
End_Orphan_b_f7:
     xor
    stelem.i
End_Orphan_b_f2:
    unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Int32
Start_Orphan_b_f8:
Start_Orphan_a_f9:
     ldarga Arg_0x9
    pop
End_Orphan_a_f9:
    ldsflda int16 ILGEN_0x52ea038a::field_0x7
   pop
End_Orphan_b_f8:
   ldelema [mscorlib]System.Single
Start_Orphan_a_fa:
Start_Orphan_a_fb:
     ldarga Arg_0x9
     sizeof [mscorlib]System.UInt16
    stind.i1
End_Orphan_a_fb:
Start_Orphan_a_fc:
     nop
End_Orphan_a_fc:
Start_Orphan_b_fd:
     nop
End_Orphan_b_fd:
     ldloc LOCAL_0x8
    ldlen
   starg Arg_0x0
End_Orphan_a_fa:
Start_Orphan_a_fe:
Start_Orphan_a_ff:
     nop
End_Orphan_a_ff:
     sizeof [mscorlib]System.Single
    stsfld int16 ILGEN_0x52ea038a::field_0x7
End_Orphan_a_fe:
Start_Orphan_a_100:
      nop
End_Orphan_a_100:
      ldloc LOCAL_0xe
Start_Orphan_b_101:
      ldloc LOCAL_0xe
      ldsfld int32 ILGEN_0x52ea038a::field_0x4
      ldloc LOCAL_0x3
     stelem.i1
End_Orphan_b_101:
     ldlen
Start_Orphan_b_102:
      ldsfld float64[] ILGEN_0x52ea038a::field_0x2
      sizeof [mscorlib]System.UInt32
Start_Orphan_b_103:
     nop
End_Orphan_b_103:
     ldelema [mscorlib]System.Double
Start_Orphan_a_104:
     nop
End_Orphan_a_104:
Start_Orphan_b_105:
     nop
End_Orphan_b_105:
     ldarga Arg_0x6
    cpobj [mscorlib]System.Double
End_Orphan_b_102:
    conv.u1
 newarr [mscorlib]System.Single
Start_Orphan_a_106:
Start_Orphan_a_107:
     nop
End_Orphan_a_107:
     ldloca LOCAL_0x3
      ldloc LOCAL_0x8
      ldloc LOCAL_0x9
Start_Orphan_b_108:
     nop
End_Orphan_b_108:
     ldelem.u4
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt16
End_Orphan_a_106:
Start_Orphan_a_109:
      ldc.i4.6
     stloc LOCAL_0x1
End_Orphan_a_109:
Start_Orphan_a_10a:
      nop
End_Orphan_a_10a:
       ldc.i4.5
Start_Orphan_b_10b:
      nop
End_Orphan_b_10b:
      conv.u1
 newarr [mscorlib]System.IntPtr
Start_Orphan_a_10c:
      nop
End_Orphan_a_10c:
       ldloc LOCAL_0x6
Start_Orphan_b_10d:
      nop
End_Orphan_b_10d:
      ldlen
Start_Orphan_b_10e:
      ldarga Arg_0x9
      ldc.i4.8
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.SByte
End_Orphan_b_10e:
     ldelem.i
Start_Orphan_b_10f:
Start_Orphan_a_110:
     nop
End_Orphan_a_110:
     ldloca LOCAL_0x9
    pop
End_Orphan_b_10f:
    conv.ovf.u2.un
Start_Orphan_b_111:
Start_Orphan_a_112:
      ldsfld int64[] ILGEN_0x52ea038a::field_0x3
      ldc.i4.0
     ldelema [mscorlib]System.Int64
     ldc.i8 0x37ba1742a47afc5
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int64
End_Orphan_a_112:
     ldloc LOCAL_0x7
Start_Orphan_a_113:
     nop
End_Orphan_a_113:
      ldsfld native int ILGEN_0x52ea038a::field_0x6
      ldloc LOCAL_0x1
     clt.un
Start_Orphan_b_114:
      ldloc LOCAL_0x0
      sizeof [mscorlib]System.UInt16
     ldelema [mscorlib]System.UInt64
     ldc.i8 0xc3b80d8d54013e4e
    stobj [mscorlib]System.UInt64
End_Orphan_b_114:
    ldelema [mscorlib]System.Int64
Start_Orphan_a_115:
      ldsfld float32[] ILGEN_0x52ea038a::field_0x9
      sizeof [mscorlib]System.UInt32
     ldelema [mscorlib]System.Single
     ldarg Arg_0x2
    stobj [mscorlib]System.Single
End_Orphan_a_115:
Start_Orphan_a_116:
     nop
End_Orphan_a_116:
      ldc.i8 0xf3439cc0d19e0293
      ldc.i8 0x33d6db8cc2c41621
     div.un
Start_Orphan_b_117:
     ldloca LOCAL_0x1
     ldc.i4.m1
    stind.i4
End_Orphan_b_117:
    conv.i8
   stind.i8
End_Orphan_b_111:
   ldelema [mscorlib]System.Single
  cpobj [mscorlib]System.Single
End_Orphan_a_e5:
Start_Orphan_a_118:
Start_Orphan_a_119:
      ldarga Arg_0x8
      ldc.i4.1
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt16
End_Orphan_a_119:
Start_Orphan_b_11a:
      ldarga Arg_0x2
      ldarg Arg_0x2
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Double
End_Orphan_b_11a:
     ldsfld native int[] ILGEN_0x52ea038a::field_0x0
Start_Orphan_a_11b:
       ldsfld float32[] ILGEN_0x52ea038a::field_0x9
       sizeof [mscorlib]System.UInt64
      ldelema [mscorlib]System.Single
     pop
End_Orphan_a_11b:
Start_Orphan_a_11c:
      nop
End_Orphan_a_11c:
Start_Orphan_b_11d:
      nop
End_Orphan_b_11d:
      ldarga Arg_0x8
Start_Orphan_b_11e:
      ldarga Arg_0x7
      ldc.i4.3
     stind.i1
End_Orphan_b_11e:
     ldobj [mscorlib]System.UInt16
    ldelema [mscorlib]System.IntPtr
       ldsfld native int ILGEN_0x52ea038a::field_0x5
       ldc.i4.3
Start_Orphan_b_11f:
      nop
End_Orphan_b_11f:
      div.un
     conv.ovf.i4
Start_Orphan_b_120:
     ldsflda int16 ILGEN_0x52ea038a::field_0x8
    initobj [mscorlib]System.Int16
End_Orphan_b_120:
    conv.ovf.u
   unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.IntPtr
End_Orphan_a_118:
Start_Orphan_a_121:
     ldloc LOCAL_0x0
    pop
End_Orphan_a_121:
Start_Orphan_a_122:
Start_Orphan_a_123:
      nop
End_Orphan_a_123:
       ldloc LOCAL_0xb
       sizeof [mscorlib]System.Byte
      ldelema [mscorlib]System.Single
       ldc.i8 0x6eb62a954335011e
      conv.r4
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Single
End_Orphan_a_122:
     ldloc LOCAL_0x6
Start_Orphan_b_124:
Start_Orphan_b_125:
      nop
End_Orphan_b_125:
      ldloc LOCAL_0x6
Start_Orphan_a_126:
      nop
End_Orphan_a_126:
       ldarg Arg_0x1
       sizeof [mscorlib]System.Double
Start_Orphan_b_127:
      nop
End_Orphan_b_127:
      rem
     ldelema [mscorlib]System.SByte
    pop
End_Orphan_b_124:
    ldlen
Start_Orphan_a_128:
Start_Orphan_b_129:
      ldsflda int16 ILGEN_0x52ea038a::field_0x8
      ldsflda int16 ILGEN_0x52ea038a::field_0x8
     cpobj [mscorlib]System.Int16
End_Orphan_b_129:
     sizeof [mscorlib]System.SByte
    stloc LOCAL_0x1
End_Orphan_a_128:
Start_Orphan_a_12a:
       nop
End_Orphan_a_12a:
        ldarg Arg_0x2
        ldc.r8 float64(0xd1d002769abdf919)
Start_Orphan_b_12b:
       nop
End_Orphan_b_12b:
       div 
Start_Orphan_b_12c:
       ldsfld float32[] ILGEN_0x52ea038a::field_0x9
      stsfld float32[] ILGEN_0x52ea038a::field_0x9
End_Orphan_b_12c:
      call float64 [mscorlib]System.Math::Sqrt(float64)
Start_Orphan_a_12d:
       ldsflda int16 ILGEN_0x52ea038a::field_0x7
       ldsflda int16 ILGEN_0x52ea038a::field_0x7
      cpobj [mscorlib]System.Int16
End_Orphan_a_12d:
        ldc.i4.8
        sizeof [mscorlib]System.UInt32
       mul
      conv.r4
     cgt
Start_Orphan_b_12e:
Start_Orphan_a_12f:
      ldloc LOCAL_0x6
     pop
End_Orphan_a_12f:
Start_Orphan_b_130:
      nop
End_Orphan_b_130:
      sizeof [mscorlib]System.Single
     conv.u1
 newarr [mscorlib]System.Byte
Start_Orphan_a_131:
      ldloc LOCAL_0xe
      ldc.i4.6
      ldc.i4.8
     stelem.i1
End_Orphan_a_131:
Start_Orphan_a_132:
      nop
End_Orphan_a_132:
      ldsflda int16 ILGEN_0x52ea038a::field_0x8
Start_Orphan_b_133:
      ldarga Arg_0x2
     initobj [mscorlib]System.Double
End_Orphan_b_133:
     ldobj [mscorlib]System.Int16
Start_Orphan_a_134:
      ldloc LOCAL_0x5
     pop
End_Orphan_a_134:
Start_Orphan_a_135:
      nop
End_Orphan_a_135:
       ldloc LOCAL_0x5
       ldc.i4.m1
Start_Orphan_b_136:
      nop
End_Orphan_b_136:
      ldelem.u2
Start_Orphan_b_137:
      nop
End_Orphan_b_137:
      sizeof [mscorlib]System.Int32
     shr
    stelem.i1
End_Orphan_b_12e:
    neg
   add
         ldloc LOCAL_0x0
         ldc.i4 0x72ce70c9
        ldelema [mscorlib]System.UInt64
Start_Orphan_b_138:
       nop
End_Orphan_b_138:
       unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.UInt64
Start_Orphan_b_139:
       ldsflda int16 ILGEN_0x52ea038a::field_0x7
      pop
End_Orphan_b_139:
      conv.u
Start_Orphan_b_13a:
       ldloc LOCAL_0xd
      pop
End_Orphan_b_13a:
      ldarg Arg_0x9
Start_Orphan_b_13b:
      ldarga Arg_0x6
     pop
End_Orphan_b_13b:
     mul.ovf
Start_Orphan_a_13c:
       sizeof [mscorlib]System.Int64
      conv.u1
 newarr [mscorlib]System.Double
       ldarg Arg_0x2
Start_Orphan_b_13d:
      nop
End_Orphan_b_13d:
      conv.ovf.u1
       ldarg Arg_0x2
      call float64 [mscorlib]System.Math::Round(float64)
     stelem.r8
End_Orphan_a_13c:
Start_Orphan_a_13e:
       ldarga Arg_0x0
       ldarg Arg_0x5
      stind.i4
End_Orphan_a_13e:
        ldloc LOCAL_0xe
        ldc.i4.8
Start_Orphan_b_13f:
       nop
End_Orphan_b_13f:
       ldelem.u1
Start_Orphan_a_140:
       nop
End_Orphan_a_140:
        ldloc LOCAL_0xc
        ldsfld native int ILGEN_0x52ea038a::field_0x6
       rem.un
Start_Orphan_b_141:
        ldsfld int64[] ILGEN_0x52ea038a::field_0x3
        ldc.i4.m1
       ldelema [mscorlib]System.Int64
       ldc.i8 0x7684a09242e5fbd1
      stobj [mscorlib]System.Int64
End_Orphan_b_141:
      mul
Start_Orphan_b_142:
Start_Orphan_a_143:
      nop
End_Orphan_a_143:
Start_Orphan_b_144:
      nop
End_Orphan_b_144:
      ldsfld float64[] ILGEN_0x52ea038a::field_0x2
     pop
End_Orphan_b_142:
     conv.ovf.u4.un
    mul.ovf
Start_Orphan_a_145:
Start_Orphan_b_146:
      nop
End_Orphan_b_146:
      ldsfld float32[] ILGEN_0x52ea038a::field_0x9
Start_Orphan_a_147:
      nop
End_Orphan_a_147:
       ldarg Arg_0x6
       ldc.r4 float32(0xfc3797f4)
Start_Orphan_b_148:
      nop
End_Orphan_b_148:
      clt
     ldelema [mscorlib]System.Single
      ldarga Arg_0x6
     unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Double
    stind.r4
End_Orphan_a_145:
Start_Orphan_a_149:
Start_Orphan_a_14a:
      nop
End_Orphan_a_14a:
Start_Orphan_b_14b:
      nop
End_Orphan_b_14b:
      ldloc LOCAL_0xe
     pop
End_Orphan_a_149:
Start_Orphan_a_14c:
       ldarga Arg_0x6
      initobj [mscorlib]System.Double
End_Orphan_a_14c:
      ldsfld int32 ILGEN_0x52ea038a::field_0x4
        ldarg Arg_0x9
        ldsfld native int ILGEN_0x52ea038a::field_0x5
       cgt
Start_Orphan_b_14d:
       sizeof [mscorlib]System.Int64
      starg Arg_0x3
End_Orphan_b_14d:
      conv.ovf.i1.un
Start_Orphan_b_14e:
Start_Orphan_a_14f:
      nop
End_Orphan_a_14f:
       ldsfld float32[] ILGEN_0x52ea038a::field_0x9
       ldc.i4.2
      ldelema [mscorlib]System.Single
       ldarga Arg_0x6
      unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Double
     stind.r4
End_Orphan_b_14e:
     div
Start_Orphan_a_150:
Start_Orphan_a_151:
      nop
End_Orphan_a_151:
       ldsfld float32[] ILGEN_0x52ea038a::field_0x9
       ldarg Arg_0x8
      ldelema [mscorlib]System.Single
Start_Orphan_a_152:
      nop
End_Orphan_a_152:
       ldarg Arg_0x6
       ldc.r4 float32(0xad0e08ad)
      rem 
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Single
End_Orphan_a_150:
Start_Orphan_a_153:
       ldc.i4.4
      starg Arg_0x8
End_Orphan_a_153:
        ldsfld native int ILGEN_0x52ea038a::field_0x5
        ldsfld native int ILGEN_0x52ea038a::field_0x5
       and
Start_Orphan_a_154:
       nop
End_Orphan_a_154:
        ldloc LOCAL_0xc
Start_Orphan_b_155:
       nop
End_Orphan_b_155:
       conv.u
Start_Orphan_b_156:
       ldarga Arg_0x1
       ldsflda int32 ILGEN_0x52ea038a::field_0x4
      cpobj [mscorlib]System.Int32
End_Orphan_b_156:
      mul.ovf.un
     conv.ovf.i1.un
    sub
   clt
  cgt
 stobj [mscorlib]System.UInt32
End_Orphan_b_e4:
 conv.u1
 newarr [mscorlib]System.Int32
Start_Orphan_a_157:
Start_Orphan_a_158:
       nop
End_Orphan_a_158:
       ldloc LOCAL_0x2
      stloc LOCAL_0x2
End_Orphan_a_157:
        ldc.r4 float32(0x8356ced0)
       conv.ovf.u
         ldc.i8 0xf1356ddc611018b7
Start_Orphan_b_159:
        nop
End_Orphan_b_159:
        conv.u1
         ldc.i8 0x20fdbe6df319e1ef
Start_Orphan_b_15a:
        nop
End_Orphan_b_15a:
        conv.ovf.i4.un
Start_Orphan_b_15b:
        sizeof [mscorlib]System.SByte
       starg Arg_0x0
End_Orphan_b_15b:
       sub.ovf
      add.ovf.un
Start_Orphan_a_15c:
       ldsfld native int[] ILGEN_0x52ea038a::field_0x0
      stsfld native int[] ILGEN_0x52ea038a::field_0x0
End_Orphan_a_15c:
Start_Orphan_a_15d:
        ldsflda int16 ILGEN_0x52ea038a::field_0x7
       initobj [mscorlib]System.Int16
End_Orphan_a_15d:
Start_Orphan_b_15e:
        nop
End_Orphan_b_15e:
        ldarg Arg_0x6
       conv.ovf.i
      conv.ovf.i1.un
Start_Orphan_b_15f:
      ldloc LOCAL_0x6
     pop
End_Orphan_b_15f:
     add.ovf.un
Start_Orphan_b_160:
     ldloca LOCAL_0x4
    pop
End_Orphan_b_160:
    conv.ovf.u8.un
      ldarga Arg_0x6
Start_Orphan_b_161:
Start_Orphan_b_162:
       ldarga Arg_0x6
       ldarg Arg_0x6
      stobj [mscorlib]System.Double
End_Orphan_b_162:
      ldarga Arg_0x7
Start_Orphan_a_163:
       ldc.r8 float64(0xbaafe489c3ca0ba2)
      starg Arg_0x6
End_Orphan_a_163:
Start_Orphan_b_164:
        ldloc LOCAL_0x0
        sizeof [mscorlib]System.Double
       ldelema [mscorlib]System.UInt64
       ldc.i8 0xeeb3596965fa7ff2
      unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt64
End_Orphan_b_164:
      sizeof [mscorlib]System.UInt16
     stind.i1
End_Orphan_b_161:
     unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Double
    conv.i8
Start_Orphan_b_165:
Start_Orphan_a_166:
     ldloca LOCAL_0x1
Start_Orphan_a_167:
       ldloc LOCAL_0xb
       ldloc LOCAL_0x9
      ldelema [mscorlib]System.Single
     initobj [mscorlib]System.Single
End_Orphan_a_167:
Start_Orphan_b_168:
      nop
End_Orphan_b_168:
      ldarga Arg_0x8
     ldind.u2
    stind.i4
End_Orphan_a_166:
Start_Orphan_a_169:
       sizeof [mscorlib]System.Double
      starg Arg_0x9
End_Orphan_a_169:
        ldloc LOCAL_0xa
        ldsfld int16 ILGEN_0x52ea038a::field_0x7
Start_Orphan_b_16a:
       nop
End_Orphan_b_16a:
       add.ovf
        sizeof [mscorlib]System.UInt64
Start_Orphan_b_16b:
       nop
End_Orphan_b_16b:
       conv.i4
Start_Orphan_b_16c:
       ldsfld native int[] ILGEN_0x52ea038a::field_0x0
      pop
End_Orphan_b_16c:
      cgt.un
Start_Orphan_b_16d:
      ldsflda int16 ILGEN_0x52ea038a::field_0x7
       ldloc LOCAL_0x2
       ldc.i4.4
      ldelem.i4
     stobj [mscorlib]System.Int16
End_Orphan_b_16d:
     conv.u1
 newarr [mscorlib]System.Double
Start_Orphan_a_16e:
       ldloc LOCAL_0x7
       sizeof [mscorlib]System.UInt64
      ldelema [mscorlib]System.Int64
Start_Orphan_a_16f:
      nop
End_Orphan_a_16f:
       ldloc LOCAL_0x7
       ldc.i4.m1
      ldelema [mscorlib]System.Int64
     cpobj [mscorlib]System.Int64
End_Orphan_a_16e:
Start_Orphan_a_170:
       ldsflda native int ILGEN_0x52ea038a::field_0x5
      initobj [mscorlib]System.IntPtr
End_Orphan_a_170:
      ldloc LOCAL_0xe
Start_Orphan_a_171:
       ldloc LOCAL_0xb
      stloc LOCAL_0xb
End_Orphan_a_171:
       ldloc LOCAL_0x6
      ldlen
     ldelem.u1
    ldelem.r8
   starg Arg_0x6
End_Orphan_b_165:
   sub.ovf.un
Start_Orphan_b_172:
Start_Orphan_a_173:
Start_Orphan_b_174:
      ldarga Arg_0x2
      ldarga Arg_0x2
     cpobj [mscorlib]System.Double
End_Orphan_b_174:
     ldloca LOCAL_0x9
    initobj [mscorlib]System.SByte
End_Orphan_a_173:
     ldloc LOCAL_0x7
Start_Orphan_a_175:
Start_Orphan_a_176:
      nop
End_Orphan_a_176:
       ldc.i4.5
      conv.u1
 newarr [mscorlib]System.UInt32
     pop
End_Orphan_a_175:
Start_Orphan_a_177:
       ldloca LOCAL_0x9
       ldloca LOCAL_0x9
      cpobj [mscorlib]System.SByte
End_Orphan_a_177:
        ldc.i8 0x48861ba75290be74
Start_Orphan_b_178:
       nop
End_Orphan_b_178:
       conv.ovf.u
      conv.ovf.u.un
Start_Orphan_b_179:
Start_Orphan_a_17a:
      nop
End_Orphan_a_17a:
       ldsfld native int[] ILGEN_0x52ea038a::field_0x0
       sizeof [mscorlib]System.Int32
Start_Orphan_b_17b:
      nop
End_Orphan_b_17b:
      ldelema [mscorlib]System.IntPtr
Start_Orphan_a_17c:
      nop
End_Orphan_a_17c:
Start_Orphan_b_17d:
      nop
End_Orphan_b_17d:
      ldloca LOCAL_0xc
     cpobj [mscorlib]System.IntPtr
End_Orphan_b_179:
     conv.i1
    ldelema [mscorlib]System.Int64
Start_Orphan_a_17e:
Start_Orphan_a_17f:
      ldsflda int16 ILGEN_0x52ea038a::field_0x8
      ldsflda int16 ILGEN_0x52ea038a::field_0x8
     cpobj [mscorlib]System.Int16
End_Orphan_a_17f:
Start_Orphan_a_180:
      nop
End_Orphan_a_180:
       ldc.r4 float32(0x16eeb254)
      conv.ovf.i
Start_Orphan_b_181:
      ldarga Arg_0x0
     pop
End_Orphan_b_181:
     conv.i4
    stloc LOCAL_0x1
End_Orphan_a_17e:
Start_Orphan_a_182:
      ldsfld native int[] ILGEN_0x52ea038a::field_0x0
     pop
End_Orphan_a_182:
Start_Orphan_a_183:
       sizeof [mscorlib]System.Int64
      starg Arg_0x9
End_Orphan_a_183:
       sizeof [mscorlib]System.UInt64
Start_Orphan_b_184:
       ldarga Arg_0x0
       ldloc LOCAL_0x9
      unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt32
End_Orphan_b_184:
      conv.u8
Start_Orphan_b_185:
       nop
End_Orphan_b_185:
       ldc.i4.m1
        ldsfld int64[] ILGEN_0x52ea038a::field_0x3
       ldlen
      xor
Start_Orphan_b_186:
Start_Orphan_b_187:
      nop
End_Orphan_b_187:
      ldc.i4.3
     starg Arg_0x0
End_Orphan_b_186:
     shr.un
    conv.i8
   unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int64
End_Orphan_b_172:
   ldc.i4.m1
Start_Orphan_b_188:
Start_Orphan_a_189:
Start_Orphan_a_18a:
Start_Orphan_a_18b:
     nop
End_Orphan_a_18b:
      ldloc LOCAL_0x0
      sizeof [mscorlib]System.Double
Start_Orphan_b_18c:
     nop
End_Orphan_b_18c:
     ldelema [mscorlib]System.UInt64
      ldc.i8 0x42c246285fdfb2de
      ldc.i8 0x9a5883f3bced12ef
Start_Orphan_b_18d:
     nop
End_Orphan_b_18d:
     div
    stind.i8
End_Orphan_a_18a:
Start_Orphan_b_18e:
      ldarga Arg_0x4
      ldarga Arg_0x4
     cpobj [mscorlib]System.UInt32
End_Orphan_b_18e:
     ldloc LOCAL_0x0
Start_Orphan_a_18f:
      nop
End_Orphan_a_18f:
       ldsfld native int ILGEN_0x52ea038a::field_0x6
Start_Orphan_b_190:
      nop
End_Orphan_b_190:
      conv.ovf.i.un
Start_Orphan_a_191:
      nop
End_Orphan_a_191:
       ldarg Arg_0x2
Start_Orphan_b_192:
      nop
End_Orphan_b_192:
      conv.i4
     clt.un
    ldelema [mscorlib]System.UInt64
     ldloc LOCAL_0x0
Start_Orphan_a_193:
       ldloc LOCAL_0xb
       ldloc LOCAL_0x3
      ldelema [mscorlib]System.Single
     initobj [mscorlib]System.Single
End_Orphan_a_193:
     sizeof [mscorlib]System.SByte
Start_Orphan_b_194:
     ldloc LOCAL_0x5
Start_Orphan_a_195:
     nop
End_Orphan_a_195:
      ldloc LOCAL_0x2
Start_Orphan_b_196:
     nop
End_Orphan_b_196:
     ldlen
      ldloc LOCAL_0x9
Start_Orphan_b_197:
     nop
End_Orphan_b_197:
     conv.i1
    stelem.i2
End_Orphan_b_194:
    ldelema [mscorlib]System.UInt64
   cpobj [mscorlib]System.UInt64
End_Orphan_a_189:
Start_Orphan_b_198:
Start_Orphan_a_199:
      ldarga Arg_0x2
      ldarga Arg_0x2
     cpobj [mscorlib]System.Double
End_Orphan_a_199:
      ldloc LOCAL_0xe
Start_Orphan_a_19a:
      nop
End_Orphan_a_19a:
       ldloca LOCAL_0x1
Start_Orphan_b_19b:
      nop
End_Orphan_b_19b:
      unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Int32
Start_Orphan_b_19c:
      ldarga Arg_0x1
      ldarga Arg_0x1
     cpobj [mscorlib]System.Int32
End_Orphan_b_19c:
     ldelema [mscorlib]System.Byte
Start_Orphan_a_19d:
      ldc.r8 float64(0x402c0e26f5961f09)
     starg Arg_0x2
End_Orphan_a_19d:
Start_Orphan_a_19e:
      nop
End_Orphan_a_19e:
      ldloc LOCAL_0xe
Start_Orphan_a_19f:
      nop
End_Orphan_a_19f:
      ldarg Arg_0x1
     ldelema [mscorlib]System.Byte
    cpobj [mscorlib]System.Byte
End_Orphan_b_198:
    ldloc LOCAL_0xb
Start_Orphan_a_1a0:
     ldloc LOCAL_0x6
Start_Orphan_a_1a1:
      nop
End_Orphan_a_1a1:
       ldsfld int32 ILGEN_0x52ea038a::field_0x4
      conv.ovf.i1
       ldloc LOCAL_0xc
       sizeof [mscorlib]System.Single
      cgt.un
Start_Orphan_b_1a2:
      ldloc LOCAL_0x9
     stsfld int16 ILGEN_0x52ea038a::field_0x8
End_Orphan_b_1a2:
     cgt.un
       ldsfld float64[] ILGEN_0x52ea038a::field_0x2
      ldlen
Start_Orphan_b_1a3:
      nop
End_Orphan_b_1a3:
      ldarg Arg_0x9
     shl
    stelem.i1
End_Orphan_a_1a0:
Start_Orphan_b_1a4:
Start_Orphan_b_1a5:
      ldsfld float64[] ILGEN_0x52ea038a::field_0x2
     stsfld float64[] ILGEN_0x52ea038a::field_0x2
End_Orphan_b_1a5:
     ldloc LOCAL_0x5
    stloc LOCAL_0x5
End_Orphan_b_1a4:
    ldloc LOCAL_0x9
   ldelema [mscorlib]System.Single
Start_Orphan_a_1a6:
       ldc.i4.2
Start_Orphan_b_1a7:
      nop
End_Orphan_b_1a7:
      conv.u1
 newarr [mscorlib]System.Single
       ldloc LOCAL_0x6
      ldlen
     ldelema [mscorlib]System.Single
Start_Orphan_a_1a8:
       ldsfld int64[] ILGEN_0x52ea038a::field_0x3
       ldc.i4.4
      ldelema [mscorlib]System.Int64
     pop
End_Orphan_a_1a8:
Start_Orphan_a_1a9:
      nop
End_Orphan_a_1a9:
       ldc.r4 float32(0xb07283ca)
Start_Orphan_b_1aa:
      nop
End_Orphan_b_1aa:
      conv.r8
Start_Orphan_b_1ab:
      ldloc LOCAL_0x6
     stloc LOCAL_0x6
End_Orphan_b_1ab:
     call float32 [mscorlib]System.Math::Abs(float32)
    stobj [mscorlib]System.Single
End_Orphan_a_1a6:
Start_Orphan_b_1ac:
Start_Orphan_a_1ad:
      ldc.i4 0x6f0cf69d
     starg Arg_0x9
End_Orphan_a_1ad:
Start_Orphan_b_1ae:
      nop
End_Orphan_b_1ae:
      ldarg Arg_0x9
     conv.u1
 newarr [mscorlib]System.Int32
    pop
End_Orphan_b_1ac:
    ldloc LOCAL_0xb
Start_Orphan_a_1af:
Start_Orphan_a_1b0:
      ldarga Arg_0x6
      ldc.r4 float32(0x1bb17ad6)
     stind.r8
End_Orphan_a_1b0:
Start_Orphan_b_1b1:
       ldloc LOCAL_0x7
       sizeof [mscorlib]System.Int64
      ldelema [mscorlib]System.Int64
      ldc.i8 0xae4e28a1971c0016
     stobj [mscorlib]System.Int64
End_Orphan_b_1b1:
     ldsflda int16 ILGEN_0x52ea038a::field_0x7
    pop
End_Orphan_a_1af:
Start_Orphan_a_1b2:
       ldarga Arg_0x9
       ldloca LOCAL_0x9
      cpobj [mscorlib]System.SByte
End_Orphan_a_1b2:
Start_Orphan_a_1b3:
       nop
End_Orphan_a_1b3:
        ldloc LOCAL_0xc
        sizeof [mscorlib]System.Int32
       add.ovf.un
        ldc.i4.3
        ldsfld native int ILGEN_0x52ea038a::field_0x5
Start_Orphan_b_1b4:
       nop
End_Orphan_b_1b4:
       sub
Start_Orphan_b_1b5:
       ldc.i4.8
      stsfld int16 ILGEN_0x52ea038a::field_0x7
End_Orphan_b_1b5:
      rem
Start_Orphan_a_1b6:
       ldloc LOCAL_0xb
      stsfld float32[] ILGEN_0x52ea038a::field_0x9
End_Orphan_a_1b6:
Start_Orphan_a_1b7:
       nop
End_Orphan_a_1b7:
       ldarga Arg_0x5
Start_Orphan_b_1b8:
        ldsfld float32[] ILGEN_0x52ea038a::field_0x9
        ldc.i4.s 12
       ldelema [mscorlib]System.Single
       ldc.r8 float64(0x17d329e13ceffb9a)
      unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Single
End_Orphan_b_1b8:
      ldind.u1
     mul.ovf.un
Start_Orphan_a_1b9:
        ldloc LOCAL_0x0
        ldsfld int32 ILGEN_0x52ea038a::field_0x4
       ldelema [mscorlib]System.UInt64
       ldc.i8 0x3c85ca2fc79fe624
      stobj [mscorlib]System.UInt64
End_Orphan_a_1b9:
      ldloc LOCAL_0x8
Start_Orphan_b_1ba:
Start_Orphan_a_1bb:
      nop
End_Orphan_a_1bb:
      ldarga Arg_0x2
     pop
End_Orphan_b_1ba:
     ldlen
Start_Orphan_b_1bc:
Start_Orphan_a_1bd:
      ldsflda int16 ILGEN_0x52ea038a::field_0x8
     pop
End_Orphan_a_1bd:
Start_Orphan_b_1be:
      nop
End_Orphan_b_1be:
      ldloc LOCAL_0xe
Start_Orphan_a_1bf:
      nop
End_Orphan_a_1bf:
       ldarg Arg_0x6
       ldarg Arg_0x2
Start_Orphan_b_1c0:
      nop
End_Orphan_b_1c0:
      cgt.un
     ldelema [mscorlib]System.Byte
      sizeof [mscorlib]System.Int64
      ldloc LOCAL_0xa
     clt.un
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Byte
End_Orphan_b_1bc:
    cgt
Start_Orphan_b_1c1:
Start_Orphan_a_1c2:
Start_Orphan_a_1c3:
     nop
End_Orphan_a_1c3:
      ldarga Arg_0x8
Start_Orphan_b_1c4:
     nop
End_Orphan_b_1c4:
     ldobj [mscorlib]System.UInt16
    stloc LOCAL_0x4
End_Orphan_a_1c2:
Start_Orphan_a_1c5:
      ldsflda native int ILGEN_0x52ea038a::field_0x6
     initobj [mscorlib]System.IntPtr
End_Orphan_a_1c5:
Start_Orphan_b_1c6:
      ldsfld float32[] ILGEN_0x52ea038a::field_0x9
      sizeof [mscorlib]System.UInt64
      ldarg Arg_0x2
     stelem.r4
End_Orphan_b_1c6:
     ldloc LOCAL_0x0
Start_Orphan_a_1c7:
      nop
End_Orphan_a_1c7:
Start_Orphan_b_1c8:
      nop
End_Orphan_b_1c8:
      ldloca LOCAL_0x4
Start_Orphan_b_1c9:
      ldarg Arg_0x5
     stloc LOCAL_0x9
End_Orphan_b_1c9:
     ldobj [mscorlib]System.UInt16
    ldelema [mscorlib]System.UInt64
   initobj [mscorlib]System.UInt64
End_Orphan_b_1c1:
   ldelema [mscorlib]System.Single
  cpobj [mscorlib]System.Single
End_Orphan_b_188:
  shl
 conv.u4
ldelem.i4
 ret
}
.method public static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	21
.try
{
ldc.i4 0x7c7265ee 
ldc.i4 0x900ba7ff 
ldc.r8 float64(0xbf85c43bcf2a878c) 
ldc.i4 0xd11fee73 
ldc.i4 0xcbba3476 
ldc.i4 0xb750ce63 
ldc.r8 float64(0x74fb1ad4786fbf85) 
ldc.i4 0xe0ca43b7 
ldc.i4 0xc30b4350 
ldc.i4 0x14df67f1 

 call unsigned int16  ILGEN_0x52ea038a::Method_0xc9efd6b0(unsigned int32 Arg_0x0, int32 Arg_0x1, float64 Arg_0x2, unsigned int16 Arg_0x3, unsigned int32 Arg_0x4, unsigned int8 Arg_0x5, float64 Arg_0x6, unsigned int8 Arg_0x7, unsigned int16 Arg_0x8, int8 Arg_0x9)
conv.i4
leave RET
}
catch [mscorlib]System.OverflowException
{
leave CORRECT_RET
}
RET:
ldc.i4 101
ret

CORRECT_RET:
ldc.i4 100
 ret
}

}


