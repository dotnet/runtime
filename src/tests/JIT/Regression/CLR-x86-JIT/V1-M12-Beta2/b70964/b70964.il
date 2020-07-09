// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x977f9ed2 {}
.class ILGEN_0x977f9ed2 {

//BEGIN STATIC FIELDS
.field static	unsigned int64[] field_0x0
.field static	unsigned int16 field_0x1
.field static	unsigned int64 field_0x2
.field static	native int field_0x3
.field static	float64[] field_0x4
.field static	float64 field_0x5
.field static	unsigned int8 field_0x6
//END STATIC FIELDS
.method static float32  Method_0xf6b7353b() {
.maxstack	19

 .locals (int8[] LOCAL_0x0,int32[] LOCAL_0x1,unsigned int16 LOCAL_0x2,unsigned int16[] LOCAL_0x3,int16 LOCAL_0x4,int8[] LOCAL_0x5,int64[] LOCAL_0x6)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc LOCAL_0x0
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc LOCAL_0x1
ldc.i4 0x67bb75e6 
stloc LOCAL_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc LOCAL_0x3
ldc.i4 0xe6087e6f 
stloc LOCAL_0x4
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc LOCAL_0x5
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc LOCAL_0x6

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
ldc.i4 0x64bf2e62 
stsfld unsigned int16 ILGEN_0x977f9ed2::field_0x1
ldc.i8 0x36717ab3cb7ec08 
stsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
ldc.i4 0x616c5bdc 
stsfld native int ILGEN_0x977f9ed2::field_0x3
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x977f9ed2::field_0x4
ldc.r8 float64(0xbdf27c1c63767d6a) 
stsfld float64 ILGEN_0x977f9ed2::field_0x5
ldc.i4 0x5775bb6a 
stsfld unsigned int8 ILGEN_0x977f9ed2::field_0x6

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
  ldsfld float64[] ILGEN_0x977f9ed2::field_0x4
  ldc.i4.s 35
Start_Orphan_1:
Start_Orphan_2:
      ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
     pop
End_Orphan_2:
       ldc.i8 0x65ed72d2578bcfce
       ldc.i8 0x58cde708de08647a
      sub
       ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
      conv.u8
     mul.ovf
    conv.ovf.u1.un
   conv.u1
 newarr [mscorlib]System.Double
Start_Orphan_3:
Start_Orphan_4:
     nop
End_Orphan_4:
     ldsflda unsigned int8 ILGEN_0x977f9ed2::field_0x6
Start_Orphan_5:
     nop
End_Orphan_5:
      ldloc LOCAL_0x4
      ldc.i4.7
     or
    stind.i1
End_Orphan_3:
    ldsfld float64[] ILGEN_0x977f9ed2::field_0x4
   ldlen
Start_Orphan_6:
       ldloc LOCAL_0x1
       ldc.i4.6
      ldelema [mscorlib]System.Int32
     pop
End_Orphan_6:
      ldc.i8 0xf3a35259e00a2c23
       ldc.i4.8
      conv.u8
     add.ovf
Start_Orphan_7:
       ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
       ldc.i4.4
      ldelema [mscorlib]System.UInt64
     pop
End_Orphan_7:
Start_Orphan_8:
      nop
End_Orphan_8:
       ldsfld native int ILGEN_0x977f9ed2::field_0x3
      conv.u2
       ldloc LOCAL_0x2
      conv.ovf.i2
     shr.un
    shr
   conv.r4
  stelem.r8
End_Orphan_1:
   ldc.r4 float32(0xf564f025)
Start_Orphan_9:
     ldloc LOCAL_0x5
Start_Orphan_a:
      ldloc LOCAL_0x6
      ldc.i4.m1
      ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
     stelem.i8
End_Orphan_a:
Start_Orphan_b:
      nop
End_Orphan_b:
       ldsflda float64 ILGEN_0x977f9ed2::field_0x5
      ldind.r8
     conv.u1
    ldelema [mscorlib]System.SByte
Start_Orphan_c:
     ldsflda native int ILGEN_0x977f9ed2::field_0x3
      ldsfld native int ILGEN_0x977f9ed2::field_0x3
      ldc.i4.7
     mul.ovf.un
    stind.i
End_Orphan_c:
Start_Orphan_d:
      ldsfld float64[] ILGEN_0x977f9ed2::field_0x4
     stsfld float64[] ILGEN_0x977f9ed2::field_0x4
End_Orphan_d:
       ldc.i4.s 93
      conv.i8
        ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
        ldc.i4.6
       ldelema [mscorlib]System.UInt64
      ldind.u8
     add
    conv.ovf.i1
   stind.i1
End_Orphan_9:
Start_Orphan_e:
Start_Orphan_f:
      ldloc LOCAL_0x5
     pop
End_Orphan_f:
Start_Orphan_10:
      nop
End_Orphan_10:
       ldloc LOCAL_0x3
       ldc.i4.0
      ldelem.u2
     conv.u1
 newarr [mscorlib]System.Double
Start_Orphan_11:
      nop
End_Orphan_11:
       ldc.i4.0
      conv.ovf.i4.un
Start_Orphan_12:
      nop
End_Orphan_12:
       ldsfld float64 ILGEN_0x977f9ed2::field_0x5
      conv.ovf.u1
     ceq
       ldc.r4 float32(0x1c287e62)
      conv.r4
     call float64 [mscorlib]System.Math::Abs(float64)
    stelem.r8
End_Orphan_e:
Start_Orphan_13:
       ldc.i4.s -70
      conv.u1
 newarr [mscorlib]System.Double
Start_Orphan_14:
      nop
End_Orphan_14:
       ldc.r8 float64(0xa3277d318c31ac54)
      conv.ovf.i2
       ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
      conv.r8
     stelem.r8
End_Orphan_13:
Start_Orphan_15:
       nop
End_Orphan_15:
        ldc.i8 0x397d0386cdd31a3d
        ldloc LOCAL_0x2
       shr
        ldc.i8 0x5f0cb65dea8a265
        ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
       mul.ovf.un
      mul.ovf.un
     conv.ovf.u8.un
Start_Orphan_16:
      ldsflda native int ILGEN_0x977f9ed2::field_0x3
       ldloc LOCAL_0x4
      conv.ovf.i2.un
     stind.i
End_Orphan_16:
Start_Orphan_17:
       nop
End_Orphan_17:
        ldc.i4.1
       conv.ovf.i8
Start_Orphan_18:
       nop
End_Orphan_18:
        ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
       not
      mul.ovf
       ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
        ldloc LOCAL_0x5
        ldloc LOCAL_0x2
       ldelem.i1
      ldelem.u8
     mul.ovf.un
    add.ovf
   conv.r8
  add 
 stelem.r8
End_Orphan_0:
Start_Orphan_19:
Start_Orphan_1a:
Start_Orphan_1b:
      ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
      ldc.i4.6
      ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
     stelem.i8
End_Orphan_1b:
     ldsflda native int ILGEN_0x977f9ed2::field_0x3
    pop
End_Orphan_1a:
Start_Orphan_1c:
       nop
End_Orphan_1c:
        ldc.i4.3
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
       mul
        ldc.i4.1
        ldloc LOCAL_0x4
       mul.ovf.un
      add.ovf.un
     not
Start_Orphan_1d:
Start_Orphan_1e:
      nop
End_Orphan_1e:
      ldloc LOCAL_0x6
     stloc LOCAL_0x6
End_Orphan_1d:
      ldloca LOCAL_0x4
     ldind.i2
    div.un
Start_Orphan_1f:
     ldsflda native int ILGEN_0x977f9ed2::field_0x3
    pop
End_Orphan_1f:
Start_Orphan_20:
       nop
End_Orphan_20:
        ldc.i4.m1
       conv.i
      conv.ovf.u8
     conv.u8
Start_Orphan_21:
       ldloc LOCAL_0x3
       ldc.i4.4
       ldc.i4.1
      stelem.i2
End_Orphan_21:
        ldloc LOCAL_0x6
        ldc.i4.7
       ldelem.i8
      conv.ovf.u8.un
Start_Orphan_22:
       nop
End_Orphan_22:
        ldc.r8 float64(0x27e271fcf43c8f99)
        ldc.r8 float64(0xfe593fd65ca58622)
       add 
      conv.ovf.u2
     shl
    cgt
   clt.un
  stloc LOCAL_0x2
End_Orphan_19:
Start_Orphan_23:
Start_Orphan_24:
       nop
End_Orphan_24:
        ldsfld unsigned int8 ILGEN_0x977f9ed2::field_0x6
       conv.ovf.u4
      conv.ovf.u8.un
     conv.ovf.i8
    conv.r.un
   pop
End_Orphan_23:
Start_Orphan_25:
Start_Orphan_26:
       nop
End_Orphan_26:
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
       ceq
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
       sub.ovf.un
      clt.un
     conv.u1
 newarr [mscorlib]System.Double
    pop
End_Orphan_25:
Start_Orphan_27:
        ldloc LOCAL_0x6
       pop
End_Orphan_27:
         ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
         ldc.i8 0xc00aba8e87b22b12
        sub
       conv.ovf.i4.un
Start_Orphan_28:
        nop
End_Orphan_28:
         ldc.r8 float64(0x96d87f9f4b356049)
         ldc.r8 float64(0xce95b13773f0f25e)
        sub  
       conv.ovf.i
      rem
Start_Orphan_29:
      nop
End_Orphan_29:
          ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
          ldsfld unsigned int16 ILGEN_0x977f9ed2::field_0x1
         ldelema [mscorlib]System.UInt64
        ldind.u8
Start_Orphan_2a:
        nop
End_Orphan_2a:
         ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
        conv.ovf.i8.un
       ceq
Start_Orphan_2b:
        ldloc LOCAL_0x5
       stloc LOCAL_0x5
End_Orphan_2b:
         ldc.r4 float32(0x4ce71d24)
        conv.u2
       conv.i1
      mul.ovf
     ceq
    conv.u1
 newarr [mscorlib]System.Double
   ldlen
  conv.u1
 conv.i8
Start_Orphan_2c:
Start_Orphan_2d:
    ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
Start_Orphan_2e:
      ldloc LOCAL_0x1
     pop
End_Orphan_2e:
       ldc.i4.1
      conv.r8
       ldc.r8 float64(0x121d7b4aa02070f5)
       ldc.r4 float32(0xfec48dd)
      add 
     clt.un
    not
Start_Orphan_2f:
      nop
End_Orphan_2f:
       ldsfld unsigned int8 ILGEN_0x977f9ed2::field_0x6
      conv.u1
 newarr [mscorlib]System.Int64
       ldsfld native int ILGEN_0x977f9ed2::field_0x3
      conv.u4
     ldelem.i8
       ldsfld unsigned int8 ILGEN_0x977f9ed2::field_0x6
      conv.i8
       ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
      conv.ovf.u8.un
     mul.ovf
    div.un
   stelem.i8
End_Orphan_2d:
Start_Orphan_30:
      ldc.i4.3
     conv.u1
 newarr [mscorlib]System.Int64
    stloc LOCAL_0x6
End_Orphan_30:
Start_Orphan_31:
       ldloc LOCAL_0x5
       ldc.i4.7
      ldelema [mscorlib]System.SByte
     pop
End_Orphan_31:
     ldc.i4.5
Start_Orphan_32:
       ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
       ldc.i4.6
      ldelema [mscorlib]System.UInt64
       ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
       ldc.i8 0x7e5aa3d9ed4e9116
      and
     stind.i8
End_Orphan_32:
Start_Orphan_33:
       ldloc LOCAL_0x6
       ldc.i4.3
       ldc.i8 0x9a56e2daef2ebfa2
      stelem.i8
End_Orphan_33:
Start_Orphan_34:
       nop
End_Orphan_34:
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
        ldc.i4.3
       add
Start_Orphan_35:
       nop
End_Orphan_35:
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
       sub.ovf.un
      mul
      ldc.i4.1
     mul
    mul.ovf.un
   conv.ovf.i.un
  pop
End_Orphan_2c:
Start_Orphan_36:
Start_Orphan_37:
     ldloca LOCAL_0x2
Start_Orphan_38:
      ldsflda float64 ILGEN_0x977f9ed2::field_0x5
      ldc.r8 float64(0xce13bb5cfa6d6439)
     stind.r8
End_Orphan_38:
Start_Orphan_39:
      nop
End_Orphan_39:
       ldc.r4 float32(0x3bf16f0)
       ldc.r8 float64(0x60765528cc2ce13a)
      sub  
       ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
      conv.r8
     ceq
    stind.i2
End_Orphan_37:
    ldsflda native int ILGEN_0x977f9ed2::field_0x3
Start_Orphan_3a:
       ldloc LOCAL_0x1
       ldc.i4.8
      ldelema [mscorlib]System.Int32
     pop
End_Orphan_3a:
Start_Orphan_3b:
       nop
End_Orphan_3b:
        ldc.r8 float64(0xba643331e5d04d45)
       conv.ovf.i8
      conv.i8
     conv.ovf.i.un
    conv.ovf.i2.un
   stind.i
End_Orphan_36:
Start_Orphan_3c:
Start_Orphan_3d:
       nop
End_Orphan_3d:
       ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
Start_Orphan_3e:
       nop
End_Orphan_3e:
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
       conv.i4
      ldelema [mscorlib]System.UInt64
     pop
End_Orphan_3c:
Start_Orphan_3f:
        nop
End_Orphan_3f:
         ldloc LOCAL_0x4
        conv.ovf.u.un
       conv.ovf.u.un
Start_Orphan_40:
        ldsflda native int ILGEN_0x977f9ed2::field_0x3
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
       stind.i
End_Orphan_40:
         ldsfld native int ILGEN_0x977f9ed2::field_0x3
         ldsfld native int ILGEN_0x977f9ed2::field_0x3
        rem
         ldsfld native int ILGEN_0x977f9ed2::field_0x3
         ldsfld native int ILGEN_0x977f9ed2::field_0x3
        add.ovf.un
       add
      sub.ovf.un
Start_Orphan_41:
       ldsflda unsigned int64 ILGEN_0x977f9ed2::field_0x2
        ldc.i8 0xb34f4bef45fa55d0
        ldc.i8 0x776640c081be90d9
       or
      stind.i8
End_Orphan_41:
Start_Orphan_42:
        ldloc LOCAL_0x3
        ldc.i4.2
        ldc.i4.s 106
       stelem.i2
End_Orphan_42:
Start_Orphan_43:
        nop
End_Orphan_43:
        ldloc LOCAL_0x6
       ldlen
Start_Orphan_44:
        ldloc LOCAL_0x5
       stloc LOCAL_0x5
End_Orphan_44:
         ldsfld native int ILGEN_0x977f9ed2::field_0x3
        conv.ovf.u8.un
       conv.ovf.i1.un
      div
     cgt
Start_Orphan_45:
Start_Orphan_46:
       ldsflda unsigned int64 ILGEN_0x977f9ed2::field_0x2
      pop
End_Orphan_46:
      ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
     pop
End_Orphan_45:
Start_Orphan_47:
        nop
End_Orphan_47:
         ldc.r4 float32(0xcef80edc)
         ldc.r8 float64(0x34bd8c16ad63b8f9)
        clt
       conv.r.un
Start_Orphan_48:
         ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
         ldc.i4.6
        ldelema [mscorlib]System.UInt64
        ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
       stind.i8
End_Orphan_48:
Start_Orphan_49:
        nop
End_Orphan_49:
         ldc.i8 0x44913f25b4e34afa
        conv.r.un
Start_Orphan_4a:
        nop
End_Orphan_4a:
         ldc.r4 float32(0xcca727c5)
         ldc.r8 float64(0x7d58c5fdd8b96223)
        div 
       mul 
      div 
     conv.u2
    rem.un
   conv.u1
 newarr [mscorlib]System.Int64
Start_Orphan_4b:
Start_Orphan_4c:
Start_Orphan_4d:
      ldsflda float64 ILGEN_0x977f9ed2::field_0x5
     pop
End_Orphan_4d:
       ldc.i8 0xac9b3ae157cfd61
      conv.i4
     conv.u1
 newarr [mscorlib]System.Double
Start_Orphan_4e:
      ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
     stsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
End_Orphan_4e:
       ldc.i4.3
       ldsfld native int ILGEN_0x977f9ed2::field_0x3
      rem
     conv.ovf.u4
Start_Orphan_4f:
       ldloc LOCAL_0x1
       ldsfld unsigned int8 ILGEN_0x977f9ed2::field_0x6
      ldelema [mscorlib]System.Int32
      ldc.i4.m1
     stind.i4
End_Orphan_4f:
Start_Orphan_50:
      nop
End_Orphan_50:
       ldc.i4.0
      conv.r8
     call float64 [mscorlib]System.Math::Sqrt(float64)
    stelem.r8
End_Orphan_4c:
Start_Orphan_51:
       ldloc LOCAL_0x3
       ldc.i4.s -10
      ldelema [mscorlib]System.UInt16
     pop
End_Orphan_51:
Start_Orphan_52:
       ldloc LOCAL_0x0
      stloc LOCAL_0x0
End_Orphan_52:
Start_Orphan_53:
       nop
End_Orphan_53:
        ldc.r8 float64(0xab5d5f6680fa04cd)
       call float64 [mscorlib]System.Math::Sin(float64)
      conv.u
     conv.ovf.i4
    conv.u1
 newarr [mscorlib]System.Int32
   stloc LOCAL_0x1
End_Orphan_4b:
Start_Orphan_54:
        ldsfld float64[] ILGEN_0x977f9ed2::field_0x4
        ldc.i4.s -27
       ldelem.r8
      conv.u4
     conv.ovf.u1
    stloc LOCAL_0x4
End_Orphan_54:
         ldc.i8 0x2c8ff728e29ff918
        conv.u2
       conv.ovf.i8.un
      conv.ovf.i8
         ldloc LOCAL_0x2
         ldc.i4.4
        shl
       conv.ovf.i8
         ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
         ldc.i8 0x531fdd759ef9112d
        sub.ovf.un
Start_Orphan_55:
        nop
End_Orphan_55:
         ldloc LOCAL_0x1
         ldc.i4.7
        ldelem.i4
       shr.un
      and
     sub.ovf
Start_Orphan_56:
Start_Orphan_57:
       ldloc LOCAL_0x2
      stsfld unsigned int8 ILGEN_0x977f9ed2::field_0x6
End_Orphan_57:
      ldloc LOCAL_0x6
     pop
End_Orphan_56:
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
Start_Orphan_58:
        nop
End_Orphan_58:
         ldsfld native int ILGEN_0x977f9ed2::field_0x3
        conv.ovf.u1
       mul.ovf
Start_Orphan_59:
        nop
End_Orphan_59:
         ldloc LOCAL_0x2
         ldc.i4.0
        ceq
         ldsfld native int ILGEN_0x977f9ed2::field_0x3
         ldsfld native int ILGEN_0x977f9ed2::field_0x3
        mul.ovf
       mul
      div.un
     conv.ovf.u8.un
    or
Start_Orphan_5a:
      ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
        ldsfld float64 ILGEN_0x977f9ed2::field_0x5
       call float64 [mscorlib]System.Math::Sqrt(float64)
Start_Orphan_5b:
       nop
End_Orphan_5b:
        ldsfld float64 ILGEN_0x977f9ed2::field_0x5
       call float64 [mscorlib]System.Math::Cos(float64)
      cgt
     ldelema [mscorlib]System.UInt64
Start_Orphan_5c:
     nop
End_Orphan_5c:
Start_Orphan_5d:
       ldc.r4 float32(0x42dcdda6)
      stsfld float64 ILGEN_0x977f9ed2::field_0x5
End_Orphan_5d:
        ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
       conv.ovf.u8.un
      not
Start_Orphan_5e:
       nop
End_Orphan_5e:
        ldc.i8 0x5fe1cb2a1787a8
        ldc.i8 0x4e2fb303f70921fb
       rem
Start_Orphan_5f:
       nop
End_Orphan_5f:
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
       conv.i8
      rem
     div.un
    stind.i8
End_Orphan_5a:
Start_Orphan_60:
        ldsfld unsigned int8 ILGEN_0x977f9ed2::field_0x6
       conv.u1
 newarr [mscorlib]System.UInt16
      pop
End_Orphan_60:
Start_Orphan_61:
        ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
       pop
End_Orphan_61:
Start_Orphan_62:
        nop
End_Orphan_62:
         ldc.i8 0xc19216298643aa97
        conv.ovf.i8.un
Start_Orphan_63:
        nop
End_Orphan_63:
         ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
        conv.i8
       add.ovf
Start_Orphan_64:
        ldloc LOCAL_0x6
       pop
End_Orphan_64:
Start_Orphan_65:
        nop
End_Orphan_65:
         ldc.i8 0x1b2ae26153e55e9d
         ldc.i8 0x105193c0724cdb47
        mul
Start_Orphan_66:
        nop
End_Orphan_66:
         ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
         ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
        sub.ovf
       add
      rem.un
         ldc.i4.s 77
         ldsfld native int ILGEN_0x977f9ed2::field_0x3
        clt
       conv.ovf.i2.un
      conv.u8
     sub.ovf.un
Start_Orphan_67:
Start_Orphan_68:
       ldsfld float64[] ILGEN_0x977f9ed2::field_0x4
       ldc.i4.6
       ldc.r4 float32(0x1392a42f)
      stelem.r8
End_Orphan_68:
Start_Orphan_69:
       nop
End_Orphan_69:
        ldsfld unsigned int16 ILGEN_0x977f9ed2::field_0x1
        ldc.i4.7
       add.ovf.un
      conv.u1
 newarr [mscorlib]System.Double
Start_Orphan_6a:
       nop
End_Orphan_6a:
        ldc.i4.7
       conv.u1
 newarr [mscorlib]System.UInt64
      ldlen
Start_Orphan_6b:
       nop
End_Orphan_6b:
        ldc.r4 float32(0x4f458d6f)
       ckfinite
      conv.r4
     stelem.r8
End_Orphan_67:
Start_Orphan_6c:
Start_Orphan_6d:
       nop
End_Orphan_6d:
       ldloc LOCAL_0x0
        ldc.i4 0xc9267f97
       conv.ovf.i2
Start_Orphan_6e:
       nop
End_Orphan_6e:
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
       conv.u1
      stelem.i1
End_Orphan_6c:
Start_Orphan_6f:
        nop
End_Orphan_6f:
        ldc.i4.5
       conv.u1
 newarr [mscorlib]System.UInt64
         ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
         ldc.i8 0x6ba58d99320d8283
        sub
       conv.u1
      ldelem.u8
     not
    and
   cgt.un
  ldelema [mscorlib]System.Int64
 ldind.i8
 bge.un Branch_0x0

Start_Orphan_70:
Start_Orphan_71:
Start_Orphan_72:
     ldsflda unsigned int64 ILGEN_0x977f9ed2::field_0x2
      ldc.i8 0xf9d685f31f777c91
     conv.u8
    stind.i8
End_Orphan_72:
Start_Orphan_73:
      ldloc LOCAL_0x6
     stloc LOCAL_0x6
End_Orphan_73:
Start_Orphan_74:
      nop
End_Orphan_74:
       ldc.i8 0xf9f0243897f38f32
      conv.ovf.u8
     conv.i
Start_Orphan_75:
      ldsflda unsigned int16 ILGEN_0x977f9ed2::field_0x1
      ldc.i4.7
     stind.i2
End_Orphan_75:
Start_Orphan_76:
      nop
End_Orphan_76:
       ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
       ldloc LOCAL_0x2
      shr.un
     conv.ovf.i4.un
    sub
   stsfld native int ILGEN_0x977f9ed2::field_0x3
End_Orphan_71:
Start_Orphan_77:
Start_Orphan_78:
      nop
End_Orphan_78:
       ldc.i8 0xd62a516b15d596d1
       ldloc LOCAL_0x2
      shl
     pop
End_Orphan_77:
Start_Orphan_79:
       ldc.i4.7
      stloc LOCAL_0x4
End_Orphan_79:
        ldc.r8 float64(0xc40397183e739205)
       call float64 [mscorlib]System.Math::Sin(float64)
      conv.ovf.i
Start_Orphan_7a:
       ldloc LOCAL_0x0
      stloc LOCAL_0x5
End_Orphan_7a:
        ldsfld unsigned int16 ILGEN_0x977f9ed2::field_0x1
       conv.r4
      conv.u
     mul.ovf.un
    conv.ovf.i2.un
   conv.ovf.u8
Start_Orphan_7b:
Start_Orphan_7c:
      ldc.i4.8
     conv.u1
 newarr [mscorlib]System.Int32
Start_Orphan_7d:
     nop
End_Orphan_7d:
      ldloc LOCAL_0x1
      ldc.i4.5
     ldelem.i4
Start_Orphan_7e:
     nop
End_Orphan_7e:
      ldc.i8 0xf6f8e127da6b495d
     conv.u4
    stelem.i4
End_Orphan_7c:
     ldloc LOCAL_0x1
Start_Orphan_7f:
      ldsflda unsigned int64 ILGEN_0x977f9ed2::field_0x2
     pop
End_Orphan_7f:
Start_Orphan_80:
      nop
End_Orphan_80:
       ldc.i4.s 78
       ldsfld native int ILGEN_0x977f9ed2::field_0x3
      div
Start_Orphan_81:
      nop
End_Orphan_81:
       ldc.i4.4
      conv.i
     ceq
    ldelema [mscorlib]System.Int32
       ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
       ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
      mul
     conv.ovf.i.un
    conv.ovf.i4
   stind.i
End_Orphan_7b:
Start_Orphan_82:
      nop
End_Orphan_82:
Start_Orphan_83:
       nop
End_Orphan_83:
        ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
        ldc.i4.8
       shl
       ldc.i8 0x74dc8d261e8ac03a
      or
        ldc.i4.8
       conv.r8
      conv.u8
     sub
Start_Orphan_84:
Start_Orphan_85:
      nop
End_Orphan_85:
       ldloc LOCAL_0x1
       ldc.i4.5
      ldelema [mscorlib]System.Int32
       ldsfld float64 ILGEN_0x977f9ed2::field_0x5
      conv.ovf.i2
     stind.i
End_Orphan_84:
Start_Orphan_86:
       ldloc LOCAL_0x6
      stloc LOCAL_0x6
End_Orphan_86:
Start_Orphan_87:
       nop
End_Orphan_87:
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
       add
      conv.ovf.u8.un
Start_Orphan_88:
        ldloc LOCAL_0x1
        ldc.i4.8
       ldelema [mscorlib]System.Int32
      pop
End_Orphan_88:
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
       conv.ovf.u8.un
        ldc.i8 0xcfe57b0d86057c58
       conv.ovf.i8.un
      sub
     sub
    add.ovf.un
Start_Orphan_89:
Start_Orphan_8a:
      nop
End_Orphan_8a:
      ldloca LOCAL_0x4
       ldloc LOCAL_0x2
       ldc.i4.4
      ceq
     stind.i2
End_Orphan_89:
       ldsflda unsigned int64 ILGEN_0x977f9ed2::field_0x2
      ldind.i8
Start_Orphan_8b:
       nop
End_Orphan_8b:
        ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
        ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
       mul.ovf.un
      conv.ovf.i8.un
     sub
Start_Orphan_8c:
       ldc.i4.4
       ldsfld native int ILGEN_0x977f9ed2::field_0x3
      mul.ovf
     pop
End_Orphan_8c:
        ldc.i8 0xccb2a6854a631d1
       not
      conv.i2
     conv.ovf.i8
    add.ovf
   sub.ovf.un
  clt
 stloc LOCAL_0x4
End_Orphan_70:
Start_Orphan_8d:
Start_Orphan_8e:
    ldloc LOCAL_0x3
   pop
End_Orphan_8e:
Start_Orphan_8f:
       ldc.i4 0x1950fa57
       ldc.i4.1
      clt
     stloc LOCAL_0x4
End_Orphan_8f:
Start_Orphan_90:
       nop
End_Orphan_90:
        ldc.r8 float64(0xdb88e2a537f776f0)
       call float64 [mscorlib]System.Math::Round(float64)
      conv.ovf.i4
     conv.u1
 newarr [mscorlib]System.UInt64
    ldlen
   neg
  stsfld unsigned int8 ILGEN_0x977f9ed2::field_0x6
End_Orphan_8d:
   ldc.i4.m1
  conv.r4
 conv.r4
 br Branch_0x1

 Branch_0x0:

Start_Orphan_91:
Start_Orphan_92:
   ldloca LOCAL_0x4
Start_Orphan_93:
Start_Orphan_94:
     nop
End_Orphan_94:
      ldc.i4.1
     conv.u1
 newarr [mscorlib]System.UInt64
    stsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
End_Orphan_93:
     ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
    conv.ovf.u2
Start_Orphan_95:
      ldloc LOCAL_0x3
     pop
End_Orphan_95:
Start_Orphan_96:
      nop
End_Orphan_96:
       ldsfld native int ILGEN_0x977f9ed2::field_0x3
      neg
Start_Orphan_97:
      nop
End_Orphan_97:
       ldloc LOCAL_0x1
       ldc.i4.5
      ldelem.i4
     sub.ovf
       ldsfld native int ILGEN_0x977f9ed2::field_0x3
      conv.i
     conv.ovf.i4.un
    and
   cgt
  stind.i2
End_Orphan_92:
Start_Orphan_98:
Start_Orphan_99:
      ldsfld float64[] ILGEN_0x977f9ed2::field_0x4
      ldloc LOCAL_0x2
     ldelema [mscorlib]System.Double
    pop
End_Orphan_99:
      ldc.i4.6
Start_Orphan_9a:
      nop
End_Orphan_9a:
       ldc.r8 float64(0xf120f423d1039c14)
      conv.i4
     clt
    conv.u1
 newarr [mscorlib]System.SByte
       ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
       ldloc LOCAL_0x2
      ldelema [mscorlib]System.UInt64
     ldind.u8
       ldloc LOCAL_0x2
      conv.ovf.u8.un
Start_Orphan_9b:
      nop
End_Orphan_9b:
        ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
        ldc.i4.5
       ldelema [mscorlib]System.UInt64
      ldind.u8
     xor
    clt.un
Start_Orphan_9c:
Start_Orphan_9d:
     nop
End_Orphan_9d:
     ldloc LOCAL_0x3
    stloc LOCAL_0x3
End_Orphan_9c:
Start_Orphan_9e:
      ldloc LOCAL_0x1
     pop
End_Orphan_9e:
       ldsfld float64 ILGEN_0x977f9ed2::field_0x5
      conv.ovf.u8
Start_Orphan_9f:
      nop
End_Orphan_9f:
       ldsfld float64 ILGEN_0x977f9ed2::field_0x5
      conv.ovf.i8
     ceq
Start_Orphan_a0:
      ldc.i4 0xaed1a7e8
     stsfld unsigned int16 ILGEN_0x977f9ed2::field_0x1
End_Orphan_a0:
       ldc.i4 0x134aba89
      conv.u8
Start_Orphan_a1:
      nop
End_Orphan_a1:
       ldc.i8 0xc6363c99c12dd1cc
       ldc.i8 0x9442a63d8a6d4001
      rem
     cgt
    add.ovf.un
   stelem.i1
End_Orphan_98:
Start_Orphan_a2:
       ldc.i4.8
      stloc LOCAL_0x2
End_Orphan_a2:
      ldsfld float64[] ILGEN_0x977f9ed2::field_0x4
Start_Orphan_a3:
       ldloc LOCAL_0x1
       ldsfld unsigned int16 ILGEN_0x977f9ed2::field_0x1
       ldc.i4.2
      stelem.i4
End_Orphan_a3:
Start_Orphan_a4:
       nop
End_Orphan_a4:
        ldloc LOCAL_0x2
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
       add.ovf.un
      conv.ovf.i4
     ldelem.r8
    conv.ovf.i4
Start_Orphan_a5:
Start_Orphan_a6:
      ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
     pop
End_Orphan_a6:
     ldsfld float64[] ILGEN_0x977f9ed2::field_0x4
    pop
End_Orphan_a5:
Start_Orphan_a7:
       ldc.r4 float32(0xdae4adc2)
      call float64 [mscorlib]System.Math::Cos(float64)
     stsfld float64 ILGEN_0x977f9ed2::field_0x5
End_Orphan_a7:
Start_Orphan_a8:
       ldsfld float64 ILGEN_0x977f9ed2::field_0x5
      pop
End_Orphan_a8:
      ldsflda unsigned int16 ILGEN_0x977f9ed2::field_0x1
     ldind.u2
Start_Orphan_a9:
Start_Orphan_aa:
      nop
End_Orphan_aa:
       ldsfld unsigned int8 ILGEN_0x977f9ed2::field_0x6
      conv.u1
 newarr [mscorlib]System.UInt64
     stsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
End_Orphan_a9:
Start_Orphan_ab:
       nop
End_Orphan_ab:
        ldloc LOCAL_0x4
        ldc.i4.6
       cgt
       ldc.i4.3
      mul.ovf
Start_Orphan_ac:
       nop
End_Orphan_ac:
        ldsfld unsigned int8 ILGEN_0x977f9ed2::field_0x6
       conv.u1
 newarr [mscorlib]System.Int32
      ldlen
     rem.un
    clt
   div.un
  conv.u1
 newarr [mscorlib]System.Int32
 stloc LOCAL_0x1
End_Orphan_91:
Start_Orphan_ad:
   ldsfld float64[] ILGEN_0x977f9ed2::field_0x4
Start_Orphan_ae:
Start_Orphan_af:
      nop
End_Orphan_af:
      ldloc LOCAL_0x1
      ldloc LOCAL_0x4
     ldelema [mscorlib]System.Int32
    pop
End_Orphan_ae:
Start_Orphan_b0:
       ldc.i4 0xf84d0b47
      conv.u1
     stloc LOCAL_0x4
End_Orphan_b0:
Start_Orphan_b1:
       nop
End_Orphan_b1:
        ldloc LOCAL_0x1
       ldlen
      conv.u1
 newarr [mscorlib]System.UInt16
     ldlen
    conv.u1
Start_Orphan_b2:
Start_Orphan_b3:
      nop
End_Orphan_b3:
       ldc.i4.7
      conv.u1
 newarr [mscorlib]System.Double
     pop
End_Orphan_b2:
Start_Orphan_b4:
       ldloc LOCAL_0x3
      stloc LOCAL_0x3
End_Orphan_b4:
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
       conv.u
Start_Orphan_b5:
       nop
End_Orphan_b5:
        ldc.i4.6
       conv.ovf.i1
      sub
     not
Start_Orphan_b6:
       ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
      pop
End_Orphan_b6:
        ldc.i8 0x7c45bedeed4931a
       conv.i
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
        ldsfld unsigned int16 ILGEN_0x977f9ed2::field_0x1
       div
      mul.ovf
Start_Orphan_b7:
        ldloc LOCAL_0x1
        ldc.i4.6
       ldelema [mscorlib]System.Int32
      pop
End_Orphan_b7:
Start_Orphan_b8:
       nop
End_Orphan_b8:
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
       conv.ovf.i4.un
      conv.i4
     rem.un
    cgt.un
   or
Start_Orphan_b9:
Start_Orphan_ba:
      nop
End_Orphan_ba:
       ldsfld float64 ILGEN_0x977f9ed2::field_0x5
       ldsfld float64 ILGEN_0x977f9ed2::field_0x5
      ceq
     stsfld unsigned int8 ILGEN_0x977f9ed2::field_0x6
End_Orphan_b9:
       ldsflda native int ILGEN_0x977f9ed2::field_0x3
      ldind.i
Start_Orphan_bb:
       nop
End_Orphan_bb:
       ldsflda native int ILGEN_0x977f9ed2::field_0x3
      ldind.i
     sub
    conv.r8
   conv.r4
  stelem.r8
End_Orphan_ad:
Start_Orphan_bc:
Start_Orphan_bd:
       ldsflda native int ILGEN_0x977f9ed2::field_0x3
      pop
End_Orphan_bd:
Start_Orphan_be:
       nop
End_Orphan_be:
        ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
       conv.i4
      conv.u1
 newarr [mscorlib]System.Int64
     stloc LOCAL_0x6
End_Orphan_bc:
Start_Orphan_bf:
         ldloc LOCAL_0x1
         ldloc LOCAL_0x2
        ldelema [mscorlib]System.Int32
        ldsfld unsigned int16 ILGEN_0x977f9ed2::field_0x1
       stind.i4
End_Orphan_bf:
       ldsflda unsigned int8 ILGEN_0x977f9ed2::field_0x6
      ldind.i1
Start_Orphan_c0:
Start_Orphan_c1:
       nop
End_Orphan_c1:
        ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
        ldc.i4.m1
       ldelema [mscorlib]System.UInt64
Start_Orphan_c2:
       nop
End_Orphan_c2:
        ldc.i8 0x70d99bd2db24264f
        ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
       sub.ovf
      stind.i8
End_Orphan_c0:
Start_Orphan_c3:
        ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
       pop
End_Orphan_c3:
        ldloc LOCAL_0x5
         ldsfld native int ILGEN_0x977f9ed2::field_0x3
         ldsfld native int ILGEN_0x977f9ed2::field_0x3
        clt.un
       ldelema [mscorlib]System.SByte
      ldind.i1
     rem.un
    conv.u1
 newarr [mscorlib]System.Double
Start_Orphan_c4:
Start_Orphan_c5:
        ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
        ldc.i4.s -125
       ldelema [mscorlib]System.UInt64
      pop
End_Orphan_c5:
        ldsfld float64 ILGEN_0x977f9ed2::field_0x5
       call float64 [mscorlib]System.Math::Sin(float64)
        ldc.r4 float32(0x429593ba)
        ldc.r4 float32(0xe13c3aae)
       add 
      clt.un
     conv.ovf.i1
    stloc LOCAL_0x2
End_Orphan_c4:
Start_Orphan_c6:
Start_Orphan_c7:
       ldsflda native int ILGEN_0x977f9ed2::field_0x3
      pop
End_Orphan_c7:
Start_Orphan_c8:
       nop
End_Orphan_c8:
        ldc.r4 float32(0x313d2731)
       conv.ovf.i8
Start_Orphan_c9:
       nop
End_Orphan_c9:
        ldsfld unsigned int64[] ILGEN_0x977f9ed2::field_0x0
        ldc.i4.5
       ldelem.u8
      clt.un
     stloc LOCAL_0x2
End_Orphan_c6:
Start_Orphan_ca:
Start_Orphan_cb:
       nop
End_Orphan_cb:
       ldsflda float64 ILGEN_0x977f9ed2::field_0x5
      pop
End_Orphan_ca:
Start_Orphan_cc:
        ldc.r4 float32(0x381d789b)
       pop
End_Orphan_cc:
         ldsfld native int ILGEN_0x977f9ed2::field_0x3
         ldc.i4.1
        add
Start_Orphan_cd:
        nop
End_Orphan_cd:
         ldc.i4.0
        conv.ovf.i1.un
       ceq
      conv.u1
 newarr [mscorlib]System.Int32
Start_Orphan_ce:
        ldloc LOCAL_0x5
       pop
End_Orphan_ce:
         ldc.i8 0x5877cd05ad27fa5c
         ldsfld unsigned int64 ILGEN_0x977f9ed2::field_0x2
        mul
       conv.ovf.u
      conv.u1
     ldelem.i4
    conv.ovf.i4
   ldelem.r8
Start_Orphan_cf:
     ldloc LOCAL_0x3
    stloc LOCAL_0x3
End_Orphan_cf:
Start_Orphan_d0:
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
        ldsfld native int ILGEN_0x977f9ed2::field_0x3
       sub
        ldc.i4.5
       conv.ovf.u4.un
      sub.ovf.un
     stsfld native int ILGEN_0x977f9ed2::field_0x3
End_Orphan_d0:
      ldsfld float64 ILGEN_0x977f9ed2::field_0x5
     call float64 [mscorlib]System.Math::Abs(float64)
    neg
   conv.r8
  div 
 neg
 Branch_0x1:

 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	1
.try {

 call float32  ILGEN_0x977f9ed2::Method_0xf6b7353b()
conv.i4
 pop
leave END
} catch [mscorlib]System.OverflowException {
pop
leave END
}
END:
ldc.i4 100
ret

}

}
