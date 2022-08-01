// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly 'negIndexRngChkElim' {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}

.class public ILGEN_0x1d013582 {

//BEGIN STATIC FIELDS
.field static	int8 field_0x0
//END STATIC FIELDS
.method static int64  Method_0x7ef63454() {
.zeroinit 
.maxstack	17

 .locals (int16 LOCAL_0x0,float32 LOCAL_0x1,unsigned int32[] LOCAL_0x2,unsigned int32[] LOCAL_0x3,float64[] LOCAL_0x4,unsigned int8 LOCAL_0x5,unsigned int64[] LOCAL_0x6,int16[] LOCAL_0x7)
//BEGIN INIT LOCALS
ldc.i4 0xdf476328 
stloc LOCAL_0x0
ldc.r4 float32(0x53de624) 
stloc LOCAL_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc LOCAL_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc LOCAL_0x3
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc LOCAL_0x4
ldc.i4 0x9580619a 
stloc LOCAL_0x5
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc LOCAL_0x6
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc LOCAL_0x7

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0xcfff2ca9 
stsfld int8 ILGEN_0x1d013582::field_0x0

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_a_0:
Start_Orphan_a_1:
Start_Orphan_a_2:
      ldloc LOCAL_0x4
      sizeof [mscorlib]System.UInt32
     ldelema [mscorlib]System.Double
     ldc.r8 float64(0x8bfe6a93501e1722)
    stind.r8
End_Orphan_a_2:
      ldloca LOCAL_0x0
     unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Int16
    conv.ovf.i8.un
Start_Orphan_a_3:
     ldloc LOCAL_0x6
     ldsfld int8 ILGEN_0x1d013582::field_0x0
     ldc.i8 0xa5e082f262e2bc5b
    stelem.i8
End_Orphan_a_3:
Start_Orphan_a_4:
     nop
End_Orphan_a_4:
      ldc.i4.6
Start_Orphan_b_5:
     nop
End_Orphan_b_5:
     conv.i1
    conv.i8
 blt Branch_0x4

Start_Orphan_a_6:
     nop
End_Orphan_a_6:
Start_Orphan_b_7:
     nop
End_Orphan_b_7:
     ldsflda int8 ILGEN_0x1d013582::field_0x0
    pop
 br Branch_0x5

 Branch_0x4:

Start_Orphan_a_8:
     nop
End_Orphan_a_8:
Start_Orphan_b_9:
     nop
End_Orphan_b_9:
     ldloca LOCAL_0x1
    pop
 Branch_0x5:

End_Orphan_a_1:
Start_Orphan_a_a:
Start_Orphan_a_b:
     nop
End_Orphan_a_b:
     ldsflda int8 ILGEN_0x1d013582::field_0x0
    initobj [mscorlib]System.SByte
End_Orphan_a_a:
Start_Orphan_a_c:
       ldloc LOCAL_0x6
       sizeof [mscorlib]System.Int16
      ldelema [mscorlib]System.UInt64
      ldc.i8 0xe408a1c2857a7fba
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt64
End_Orphan_a_c:
Start_Orphan_b_d:
      nop
End_Orphan_b_d:
      ldc.r8 float64(0x9b46c404651d6d1b)
       ldc.r4 float32(0xb85da4ea)
      conv.r4
     div 
    conv.r8
   call float64 [mscorlib]System.Math::Sqrt(float64)
Start_Orphan_b_e:
Start_Orphan_a_f:
      ldloc LOCAL_0x4
      ldc.i4.5
     ldelema [mscorlib]System.Double
    pop
End_Orphan_a_f:
Start_Orphan_b_10:
     ldloc LOCAL_0x4
     sizeof [mscorlib]System.UInt64
     ldc.r4 float32(0xb9e7277a)
    stelem.r8
End_Orphan_b_10:
    ldloc LOCAL_0x6
Start_Orphan_a_11:
      ldloc LOCAL_0x3
      sizeof [mscorlib]System.IntPtr
     ldelema [mscorlib]System.UInt32
      ldloc LOCAL_0x3
      sizeof [mscorlib]System.Int16
     ldelema [mscorlib]System.UInt32
    cpobj [mscorlib]System.UInt32
End_Orphan_a_11:
       ldloc LOCAL_0x3
       sizeof [mscorlib]System.IntPtr
      ldelema [mscorlib]System.UInt32
Start_Orphan_b_12:
     nop
End_Orphan_b_12:
     unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.UInt32
Start_Orphan_a_13:
     nop
End_Orphan_a_13:
      ldc.i4.m1
Start_Orphan_b_14:
     nop
End_Orphan_b_14:
     conv.ovf.i1
    div
Start_Orphan_a_15:
     nop
End_Orphan_a_15:
      ldc.i4.s -126
Start_Orphan_b_16:
     nop
End_Orphan_b_16:
     conv.ovf.i8.un
    not
   stelem.i8
End_Orphan_b_e:
   ldloc LOCAL_0x1
 bge Branch_0x2

Start_Orphan_a_17:
      ldc.i4.4
Start_Orphan_b_18:
     nop
End_Orphan_b_18:
     box [mscorlib]System.UInt32
    pop
End_Orphan_a_17:
Start_Orphan_b_19:
Start_Orphan_a_1a:
     nop
End_Orphan_a_1a:
      ldloc LOCAL_0x3
      sizeof [mscorlib]System.UInt16
Start_Orphan_b_1b:
     nop
End_Orphan_b_1b:
     ldelema [mscorlib]System.UInt32
Start_Orphan_a_1c:
     nop
End_Orphan_a_1c:
      ldloc LOCAL_0x3
      ldloc LOCAL_0x5
     ldelema [mscorlib]System.UInt32
    cpobj [mscorlib]System.UInt32
End_Orphan_b_19:
    ldloca LOCAL_0x5
   initobj [mscorlib]System.Byte
 br Branch_0x3

 Branch_0x2:

Start_Orphan_b_1d:
      ldc.i4.5
     conv.u1
 newarr [mscorlib]System.UInt32
    stloc LOCAL_0x3
End_Orphan_b_1d:
    ldloc LOCAL_0x3
   stloc LOCAL_0x2
 Branch_0x3:

End_Orphan_a_0:
   ldc.r8 float64(0xbcdba393d9ff87be)
Start_Orphan_b_1e:
   ldloc LOCAL_0x7
  pop
End_Orphan_b_1e:
  conv.ovf.u8
 conv.ovf.i4
Start_Orphan_a_1f:
Start_Orphan_a_20:
Start_Orphan_b_21:
Start_Orphan_b_22:
    nop
End_Orphan_b_22:
    ldloca LOCAL_0x0
     ldc.i4.8
     ldc.i4.s -115
Start_Orphan_b_23:
    nop
End_Orphan_b_23:
    sub.ovf
   stobj [mscorlib]System.Int16
End_Orphan_b_21:
   ldloca LOCAL_0x5
Start_Orphan_b_24:
Start_Orphan_a_25:
    nop
End_Orphan_a_25:
     ldloc LOCAL_0x7
     ldc.i4.m1
Start_Orphan_b_26:
    nop
End_Orphan_b_26:
    ldelema [mscorlib]System.Int16
Start_Orphan_b_27:
    nop
End_Orphan_b_27:
    ldloca LOCAL_0x0
   cpobj [mscorlib]System.Int16
End_Orphan_b_24:
   ldloca LOCAL_0x5
  cpobj [mscorlib]System.Byte
End_Orphan_a_20:
Start_Orphan_a_28:
Start_Orphan_a_29:
     ldloc LOCAL_0x2
     ldc.i4 0x9989595c
     ldloc LOCAL_0x5
    stelem.i4
End_Orphan_a_29:
Start_Orphan_a_2a:
     nop
End_Orphan_a_2a:
      sizeof [mscorlib]System.UInt64
     conv.u1
 newarr [mscorlib]System.UInt32
Start_Orphan_a_2b:
     nop
End_Orphan_a_2b:
      ldloca LOCAL_0x0
     ldind.i2
    ldelema [mscorlib]System.UInt32
Start_Orphan_a_2c:
      ldloc LOCAL_0x6
      sizeof [mscorlib]System.UInt32
     ldelema [mscorlib]System.UInt64
     ldc.i8 0x53743a6854dd3bea
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt64
End_Orphan_a_2c:
Start_Orphan_b_2d:
     ldc.i4.6
    stsfld int8 ILGEN_0x1d013582::field_0x0
End_Orphan_b_2d:
    sizeof [mscorlib]System.Double
   stind.i4
End_Orphan_a_28:
   ldloc LOCAL_0x7
Start_Orphan_a_2e:
Start_Orphan_a_2f:
     ldloca LOCAL_0x5
     ldloc LOCAL_0x5
    stobj [mscorlib]System.Byte
End_Orphan_a_2f:
Start_Orphan_a_30:
     nop
End_Orphan_a_30:
      sizeof [mscorlib]System.UInt64
      ldc.i4.0
Start_Orphan_b_31:
     nop
End_Orphan_b_31:
     add
    conv.u1
 newarr [mscorlib]System.Int16
   pop
End_Orphan_a_2e:
Start_Orphan_a_32:
      nop
End_Orphan_a_32:
Start_Orphan_b_33:
      nop
End_Orphan_b_33:
      ldsflda int8 ILGEN_0x1d013582::field_0x0
     ldobj [mscorlib]System.SByte
Start_Orphan_b_34:
Start_Orphan_b_35:
     nop
End_Orphan_b_35:
     ldloca LOCAL_0x1
    initobj [mscorlib]System.Single
End_Orphan_b_34:
    conv.i
Start_Orphan_a_36:
       ldloc LOCAL_0x6
       ldc.i4.8
      ldelema [mscorlib]System.UInt64
Start_Orphan_b_37:
     nop
End_Orphan_b_37:
     ldind.u8
    pop
End_Orphan_a_36:
Start_Orphan_a_38:
       ldloc LOCAL_0x6
       ldc.i4.8
      ldelema [mscorlib]System.UInt64
     pop
End_Orphan_a_38:
       sizeof [mscorlib]System.Int32
Start_Orphan_b_39:
      nop
End_Orphan_b_39:
      conv.i8
Start_Orphan_a_3a:
      nop
End_Orphan_a_3a:
       ldc.i4.7
Start_Orphan_b_3b:
      nop
End_Orphan_b_3b:
      conv.ovf.u8.un
Start_Orphan_b_3c:
      ldc.i4 0x1c0b7ded
     stsfld int8 ILGEN_0x1d013582::field_0x0
End_Orphan_b_3c:
     cgt.un
    conv.ovf.i.un
Start_Orphan_b_3d:
Start_Orphan_a_3e:
     ldloc LOCAL_0x7
     sizeof [mscorlib]System.IntPtr
     sizeof [mscorlib]System.UInt16
    stelem.i2
End_Orphan_a_3e:
Start_Orphan_b_3f:
      ldloc LOCAL_0x6
      ldc.i4.m1
     ldelema [mscorlib]System.UInt64
     ldc.i8 0xdf59bdb15eecd3b2
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt64
End_Orphan_b_3f:
    ldsflda int8 ILGEN_0x1d013582::field_0x0
   initobj [mscorlib]System.SByte
End_Orphan_b_3d:
   cgt
  ldelema [mscorlib]System.Int16
Start_Orphan_a_40:
      ldloc LOCAL_0x0
Start_Orphan_b_41:
     nop
End_Orphan_b_41:
     conv.u1
 newarr [mscorlib]System.UInt64
Start_Orphan_a_42:
     nop
End_Orphan_a_42:
       ldloc LOCAL_0x2
       ldc.i4.4
      ldelema [mscorlib]System.UInt32
     unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.UInt32
    ldelema [mscorlib]System.UInt64
Start_Orphan_a_43:
     nop
End_Orphan_a_43:
      ldloc LOCAL_0x6
      sizeof [mscorlib]System.UInt64
Start_Orphan_b_44:
     nop
End_Orphan_b_44:
     ldelema [mscorlib]System.UInt64
    ldind.u8
   stobj [mscorlib]System.UInt64
End_Orphan_a_40:
Start_Orphan_a_45:
Start_Orphan_a_46:
     nop
End_Orphan_a_46:
      sizeof [mscorlib]System.Double
Start_Orphan_b_47:
     nop
End_Orphan_b_47:
     conv.u4
    pop
End_Orphan_a_45:
Start_Orphan_a_48:
      nop
End_Orphan_a_48:
       sizeof [mscorlib]System.UInt16
       ldc.i4.8
      rem
Start_Orphan_b_49:
       ldloc LOCAL_0x3
       sizeof [mscorlib]System.SByte
      ldelema [mscorlib]System.UInt32
       ldloc LOCAL_0x3
       ldc.i4.s -19
      ldelema [mscorlib]System.UInt32
     cpobj [mscorlib]System.UInt32
End_Orphan_b_49:
     conv.i
Start_Orphan_a_4a:
      ldloca LOCAL_0x0
      ldloca LOCAL_0x0
     cpobj [mscorlib]System.Int16
End_Orphan_a_4a:
       sizeof [mscorlib]System.Single
      conv.ovf.i.un
Start_Orphan_a_4b:
      nop
End_Orphan_a_4b:
       ldc.i4.s -104
      conv.u1
Start_Orphan_b_4c:
      sizeof [mscorlib]System.Int64
     stloc LOCAL_0x5
End_Orphan_b_4c:
     add.ovf
Start_Orphan_b_4d:
Start_Orphan_a_4e:
     nop
End_Orphan_a_4e:
      ldloc LOCAL_0x3
      sizeof [mscorlib]System.UInt64
Start_Orphan_b_4f:
     nop
End_Orphan_b_4f:
     ldelema [mscorlib]System.UInt32
Start_Orphan_a_50:
     nop
End_Orphan_a_50:
Start_Orphan_b_51:
     nop
End_Orphan_b_51:
     ldc.i4.2
    stobj [mscorlib]System.UInt32
End_Orphan_b_4d:
    sub
Start_Orphan_a_52:
Start_Orphan_a_53:
     nop
End_Orphan_a_53:
Start_Orphan_b_54:
     nop
End_Orphan_b_54:
     ldloca LOCAL_0x0
Start_Orphan_a_55:
     nop
End_Orphan_a_55:
     sizeof [mscorlib]System.Byte
    stind.i2
End_Orphan_a_52:
Start_Orphan_a_56:
      ldloc LOCAL_0x3
     stloc LOCAL_0x2
End_Orphan_a_56:
     ldloc LOCAL_0x7
Start_Orphan_a_57:
      nop
End_Orphan_a_57:
Start_Orphan_b_58:
      nop
End_Orphan_b_58:
      sizeof [mscorlib]System.Single
     conv.i4
Start_Orphan_b_59:
 ldc.i4 1 
 Loop_0x0: //WHAT
Start_Orphan_a_5a:
     nop
End_Orphan_a_5a:
      ldc.i4.1
      ldc.i4.m1
Start_Orphan_b_5b:
     nop
End_Orphan_b_5b:
     add
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x0
 pop
 Loop_0x1: // End of WHAT
End_Orphan_b_59:
    ldelem.i2
   sub
Start_Orphan_b_5c:
Start_Orphan_a_5d:
Start_Orphan_a_5e:
    nop
End_Orphan_a_5e:
Start_Orphan_b_5f:
    nop
End_Orphan_b_5f:
    ldloca LOCAL_0x0
     ldc.i4.7
Start_Orphan_b_60:
    nop
End_Orphan_b_60:
    conv.ovf.u2.un
   stobj [mscorlib]System.Int16
End_Orphan_a_5d:
Start_Orphan_a_61:
      ldloc LOCAL_0x3
      ldc.i4.7
     ldelema [mscorlib]System.UInt32
    initobj [mscorlib]System.UInt32
End_Orphan_a_61:
Start_Orphan_a_62:
     nop
End_Orphan_a_62:
      sizeof [mscorlib]System.Int32
     conv.u1
Start_Orphan_b_63:
     nop
End_Orphan_b_63:
     ldc.i4 0x5150a146
    ceq
Start_Orphan_b_64:
Start_Orphan_b_65:
    nop
End_Orphan_b_65:
    ldloca LOCAL_0x5
Start_Orphan_a_66:
    nop
End_Orphan_a_66:
     ldc.i8 0x8b7708a5bdf7ecd4
Start_Orphan_b_67:
    nop
End_Orphan_b_67:
    conv.ovf.u2
   stobj [mscorlib]System.Byte
End_Orphan_b_64:
   conv.u1
 newarr [mscorlib]System.UInt64
  pop
End_Orphan_b_5c:
  conv.ovf.i2
 stind.i2
End_Orphan_a_1f:
Start_Orphan_b_68:
Start_Orphan_a_69:
 ldc.i4 4 
 Loop_0x2: //WHAT
Start_Orphan_a_6a:
     ldsflda int8 ILGEN_0x1d013582::field_0x0
    initobj [mscorlib]System.SByte
End_Orphan_a_6a:
      ldsfld int8 ILGEN_0x1d013582::field_0x0
      ldloc LOCAL_0x5
Start_Orphan_b_6b:
     nop
End_Orphan_b_6b:
     cgt.un
Start_Orphan_a_6c:
     nop
End_Orphan_a_6c:
      sizeof [mscorlib]System.Single
      ldc.i4.1
     clt
Start_Orphan_b_6d:
     ldloca LOCAL_0x1
     ldloc LOCAL_0x1
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Single
End_Orphan_b_6d:
    sub.ovf
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x2
 pop
 Loop_0x3: // End of WHAT
End_Orphan_a_69:
   ldloc LOCAL_0x7
  pop
End_Orphan_b_68:
  ldc.i4.1
Start_Orphan_a_6e:
Start_Orphan_a_6f:
Start_Orphan_a_70:
     nop
End_Orphan_a_70:
      ldc.i4.7
     conv.u1
 newarr [mscorlib]System.UInt64
Start_Orphan_a_71:
     nop
End_Orphan_a_71:
     ldc.i4 0xd2018783
Start_Orphan_b_72:
      ldloc LOCAL_0x4
      ldsfld int8 ILGEN_0x1d013582::field_0x0
     ldelema [mscorlib]System.Double
      ldloc LOCAL_0x4
      sizeof [mscorlib]System.SByte
     ldelema [mscorlib]System.Double
    cpobj [mscorlib]System.Double
End_Orphan_b_72:
    ldelema [mscorlib]System.UInt64
Start_Orphan_a_73:
      ldloc LOCAL_0x4
      ldc.i4 0xdf41fb4a
     ldelema [mscorlib]System.Double
    pop
End_Orphan_a_73:
Start_Orphan_a_74:
     nop
End_Orphan_a_74:
      ldc.i8 0xd455bcb5f46a81f3
Start_Orphan_b_75:
     nop
End_Orphan_b_75:
     conv.ovf.u.un
    conv.ovf.i8
   unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt64
End_Orphan_a_6f:
   ldc.r4 float32(0x17c252ef)
  pop
End_Orphan_a_6e:
Start_Orphan_a_76:
Start_Orphan_b_77:
    nop
End_Orphan_b_77:
    ldloc LOCAL_0x4
Start_Orphan_a_78:
Start_Orphan_a_79:
     nop
End_Orphan_a_79:
Start_Orphan_b_7a:
     nop
End_Orphan_b_7a:
     ldloc LOCAL_0x4
    pop
End_Orphan_a_78:
Start_Orphan_a_7b:
      sizeof [mscorlib]System.Double
     stloc LOCAL_0x5
End_Orphan_a_7b:
Start_Orphan_a_7c:
      nop
End_Orphan_a_7c:
       sizeof [mscorlib]System.UInt32
       ldc.i4.1
      or
     conv.ovf.i8
    conv.ovf.u4
Start_Orphan_a_7d:
Start_Orphan_a_7e:
     nop
End_Orphan_a_7e:
      ldloc LOCAL_0x7
      ldc.i4.s 116
Start_Orphan_b_7f:
     nop
End_Orphan_b_7f:
     ldelema [mscorlib]System.Int16
Start_Orphan_a_80:
     nop
End_Orphan_a_80:
      ldc.i4.7
      sizeof [mscorlib]System.UInt32
Start_Orphan_b_81:
     nop
End_Orphan_b_81:
     ceq
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int16
End_Orphan_a_7d:
Start_Orphan_b_82:
      nop
End_Orphan_b_82:
      ldc.i4.3
Start_Orphan_a_83:
      nop
End_Orphan_a_83:
       ldc.i4.s 58
      conv.ovf.i4.un
     clt.un
    conv.r.un
   stelem.r8
End_Orphan_a_76:
   ldloc LOCAL_0x4
  ldlen
Start_Orphan_b_84:
Start_Orphan_a_85:
Start_Orphan_a_86:
     nop
End_Orphan_a_86:
      ldc.i4.8
     conv.ovf.u4.un
Start_Orphan_b_87:
     ldloca LOCAL_0x0
    pop
End_Orphan_b_87:
    conv.u1
 newarr [mscorlib]System.Double
      ldc.i8 0x772a67a2c8250f46
     conv.r.un
      ldloc LOCAL_0x1
Start_Orphan_b_88:
     nop
End_Orphan_b_88:
     call float32 [mscorlib]System.Math::Abs(float32)
Start_Orphan_b_89:
     sizeof [mscorlib]System.Byte
    stloc LOCAL_0x5
End_Orphan_b_89:
    clt
   ldelema [mscorlib]System.Double
  pop
End_Orphan_a_85:
  ldc.r4 float32(0x48c18680)
 stloc LOCAL_0x1
End_Orphan_b_84:
 ceq
 ble.un Branch_0x0

Start_Orphan_a_8a:
    ldloca LOCAL_0x5
Start_Orphan_a_8b:
       ldloc LOCAL_0x4
       ldc.i4.7
      ldelema [mscorlib]System.Double
      ldloc LOCAL_0x1
     stind.r8
End_Orphan_a_8b:
Start_Orphan_a_8c:
      nop
End_Orphan_a_8c:
       ldc.i4.6
       sizeof [mscorlib]System.Byte
      mul.ovf
Start_Orphan_a_8d:
      nop
End_Orphan_a_8d:
       ldc.i8 0x90e6efe58fa1d2ac
      conv.u1
     rem
Start_Orphan_a_8e:
      ldloca LOCAL_0x5
     initobj [mscorlib]System.Byte
End_Orphan_a_8e:
       ldc.i4.4
       ldc.i4.7
      add.ovf.un
     conv.ovf.i2.un
Start_Orphan_b_8f:
Start_Orphan_a_90:
     nop
End_Orphan_a_90:
     ldloca LOCAL_0x0
Start_Orphan_a_91:
     nop
End_Orphan_a_91:
Start_Orphan_b_92:
     nop
End_Orphan_b_92:
     ldloca LOCAL_0x0
    cpobj [mscorlib]System.Int16
End_Orphan_b_8f:
    clt
   stobj [mscorlib]System.Byte
End_Orphan_a_8a:
Start_Orphan_a_93:
Start_Orphan_b_94:
      ldloc LOCAL_0x7
     stloc LOCAL_0x7
End_Orphan_b_94:
     ldloca LOCAL_0x0
Start_Orphan_a_95:
      ldsflda int8 ILGEN_0x1d013582::field_0x0
      sizeof [mscorlib]System.Int32
     stind.i1
End_Orphan_a_95:
Start_Orphan_a_96:
      nop
End_Orphan_a_96:
       sizeof [mscorlib]System.Single
      conv.u1
 newarr [mscorlib]System.Int16
Start_Orphan_a_97:
      nop
End_Orphan_a_97:
       ldc.i4.1
      conv.u2
Start_Orphan_b_98:
      ldloca LOCAL_0x5
      sizeof [mscorlib]System.Single
     stobj [mscorlib]System.Byte
End_Orphan_b_98:
     ldelema [mscorlib]System.Int16
    cpobj [mscorlib]System.Int16
End_Orphan_a_93:
Start_Orphan_b_99:
Start_Orphan_a_9a:
      ldsflda int8 ILGEN_0x1d013582::field_0x0
      ldc.i4.3
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.SByte
End_Orphan_a_9a:
      ldloc LOCAL_0x6
       ldc.i8 0xb1dd9824a9eb97bc
       ldc.i8 0xdd127d9cee036af
      cgt
Start_Orphan_b_9b:
       ldloc LOCAL_0x2
       sizeof [mscorlib]System.Int32
      ldelema [mscorlib]System.UInt32
      ldc.i4.1
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt32
End_Orphan_b_9b:
     ldelema [mscorlib]System.UInt64
Start_Orphan_a_9c:
      sizeof [mscorlib]System.SByte
     pop
End_Orphan_a_9c:
       sizeof [mscorlib]System.IntPtr
      conv.ovf.u8
Start_Orphan_a_9d:
      nop
End_Orphan_a_9d:
       ldc.i8 0x79fb3c6adbc01490
      conv.ovf.u8.un
Start_Orphan_b_9e:
      ldloca LOCAL_0x0
      ldc.i4.7
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int16
End_Orphan_b_9e:
     or
    stobj [mscorlib]System.UInt64
End_Orphan_b_99:
    ldc.i4.1
Start_Orphan_b_9f:
Start_Orphan_a_a0:
Start_Orphan_a_a1:
     nop
End_Orphan_a_a1:
      ldloc LOCAL_0x2
      ldc.i4.2
     ldelema [mscorlib]System.UInt32
      sizeof [mscorlib]System.Byte
     conv.u2
    stind.i4
End_Orphan_a_a0:
Start_Orphan_b_a2:
      ldloc LOCAL_0x7
Start_Orphan_b_a3:
     nop
End_Orphan_b_a3:
     ldlen
    pop
End_Orphan_b_a2:
    ldloca LOCAL_0x1
   initobj [mscorlib]System.Single
End_Orphan_b_9f:
   conv.ovf.u8.un
Start_Orphan_a_a4:
Start_Orphan_a_a5:
Start_Orphan_a_a6:
     nop
End_Orphan_a_a6:
Start_Orphan_b_a7:
     nop
End_Orphan_b_a7:
     ldloca LOCAL_0x5
Start_Orphan_a_a8:
     nop
End_Orphan_a_a8:
      ldc.i4.5
     conv.ovf.u2.un
    stobj [mscorlib]System.Byte
End_Orphan_a_a5:
    ldloc LOCAL_0x4
   stloc LOCAL_0x4
End_Orphan_a_a4:
Start_Orphan_a_a9:
Start_Orphan_a_aa:
      ldsflda int8 ILGEN_0x1d013582::field_0x0
     initobj [mscorlib]System.SByte
End_Orphan_a_aa:
      ldloc LOCAL_0x3
        ldloc LOCAL_0x2
        ldsfld int8 ILGEN_0x1d013582::field_0x0
       ldelema [mscorlib]System.UInt32
Start_Orphan_b_ab:
      nop
End_Orphan_b_ab:
      ldind.u4
     ldelema [mscorlib]System.UInt32
Start_Orphan_a_ac:
       ldloc LOCAL_0x4
       ldc.i4.6
      ldelema [mscorlib]System.Double
      ldc.r8 float64(0x165a1f35b4fd24a4)
     stobj [mscorlib]System.Double
End_Orphan_a_ac:
       ldloc LOCAL_0x2
       ldc.i4.6
      ldelema [mscorlib]System.UInt32
     ldobj [mscorlib]System.UInt32
    stobj [mscorlib]System.UInt32
End_Orphan_a_a9:
Start_Orphan_b_ad:
       nop
End_Orphan_b_ad:
       sizeof [mscorlib]System.UInt64
      conv.u1
 newarr [mscorlib]System.UInt64
Start_Orphan_a_ae:
       nop
End_Orphan_a_ae:
        sizeof [mscorlib]System.Int32
Start_Orphan_b_af:
       nop
End_Orphan_b_af:
       conv.i1
Start_Orphan_a_b0:
       nop
End_Orphan_a_b0:
        ldc.i8 0x10c57523c584fb3c
        ldc.i8 0xa2199815b743747e
       cgt.un
      shr.un
     ldelema [mscorlib]System.UInt64
    unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.UInt64
Start_Orphan_a_b1:
       ldloca LOCAL_0x0
      pop
End_Orphan_a_b1:
        ldc.i4.m1
Start_Orphan_b_b2:
       nop
End_Orphan_b_b2:
       conv.i4
Start_Orphan_b_b3:
       ldloca LOCAL_0x5
       ldloca LOCAL_0x5
      cpobj [mscorlib]System.Byte
End_Orphan_b_b3:
      conv.ovf.i8
Start_Orphan_a_b4:
       nop
End_Orphan_a_b4:
        ldloc LOCAL_0x6
        sizeof [mscorlib]System.IntPtr
       ldelema [mscorlib]System.UInt64
Start_Orphan_b_b5:
       ldc.i8 0xaa7ec65b3d0a46a1
      pop
End_Orphan_b_b5:
      ldind.u8
     rem
Start_Orphan_a_b6:
      ldloc LOCAL_0x6
     pop
End_Orphan_a_b6:
Start_Orphan_a_b7:
        ldloc LOCAL_0x6
        ldc.i4.s 24
       ldelema [mscorlib]System.UInt64
       ldc.i8 0xd753198b6dbca98
      unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt64
End_Orphan_a_b7:
Start_Orphan_a_b8:
       nop
End_Orphan_a_b8:
        ldc.r4 float32(0xce650c39)
       conv.ovf.i1
      conv.u
Start_Orphan_b_b9:
Start_Orphan_a_ba:
      nop
End_Orphan_a_ba:
Start_Orphan_b_bb:
      nop
End_Orphan_b_bb:
      ldloc LOCAL_0x7
     stloc LOCAL_0x7
End_Orphan_b_b9:
     conv.ovf.u2.un
    shr
   mul.ovf.un
Start_Orphan_b_bc:
Start_Orphan_a_bd:
Start_Orphan_b_be:
      ldloc LOCAL_0x6
      sizeof [mscorlib]System.UInt32
     ldelema [mscorlib]System.UInt64
     ldc.i8 0xd793f6a3924e116c
    stind.i8
End_Orphan_b_be:
    ldloca LOCAL_0x1
Start_Orphan_b_bf:
     nop
End_Orphan_b_bf:
     ldloca LOCAL_0x1
Start_Orphan_b_c0:
     ldloc LOCAL_0x7
    stloc LOCAL_0x7
End_Orphan_b_c0:
    unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Single
   unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Single
End_Orphan_a_bd:
   ldloca LOCAL_0x1
Start_Orphan_a_c1:
      ldc.r4 float32(0x7e73d16b)
     neg
    pop
End_Orphan_a_c1:
Start_Orphan_a_c2:
      nop
End_Orphan_a_c2:
       ldc.r8 float64(0x63b13c8b1408f6df)
      call float64 [mscorlib]System.Math::Sin(float64)
       sizeof [mscorlib]System.Int16
Start_Orphan_b_c3:
      nop
End_Orphan_b_c3:
      conv.r4
Start_Orphan_b_c4:
      ldloc LOCAL_0x6
      sizeof [mscorlib]System.Single
      ldc.i8 0x5cc1f16fd3f2d1c9
     stelem.i8
End_Orphan_b_c4:
     sub  
    call float64 [mscorlib]System.Math::Sin(float64)
Start_Orphan_a_c5:
      nop
End_Orphan_a_c5:
       ldloc LOCAL_0x1
      call float64 [mscorlib]System.Math::Abs(float64)
     conv.u2
Start_Orphan_b_c6:
Start_Orphan_a_c7:
     nop
End_Orphan_a_c7:
      ldloc LOCAL_0x4
      ldc.i4.4
     ldelema [mscorlib]System.Double
    pop
End_Orphan_b_c6:
    conv.r8
   div 
  stobj [mscorlib]System.Single
End_Orphan_b_bc:
  rem.un
Start_Orphan_a_c8:
Start_Orphan_a_c9:
Start_Orphan_a_ca:
     ldloca LOCAL_0x1
     ldloc LOCAL_0x1
    stobj [mscorlib]System.Single
End_Orphan_a_ca:
Start_Orphan_a_cb:
     nop
End_Orphan_a_cb:
      ldc.i4.8
     conv.ovf.u2.un
    conv.u1
 newarr [mscorlib]System.UInt32
Start_Orphan_a_cc:
     ldloc LOCAL_0x5
    stloc LOCAL_0x0
End_Orphan_a_cc:
Start_Orphan_a_cd:
     nop
End_Orphan_a_cd:
      ldc.r4 float32(0x642d9649)
     call float64 [mscorlib]System.Math::Cos(float64)
    conv.ovf.u1
Start_Orphan_a_ce:
      ldloc LOCAL_0x4
      ldc.i4.8
     ldelema [mscorlib]System.Double
    pop
End_Orphan_a_ce:
Start_Orphan_a_cf:
     nop
End_Orphan_a_cf:
      sizeof [mscorlib]System.Int16
     conv.u8
      ldsfld int8 ILGEN_0x1d013582::field_0x0
Start_Orphan_b_d0:
     nop
End_Orphan_b_d0:
     conv.u8
Start_Orphan_b_d1:
      ldloc LOCAL_0x2
      sizeof [mscorlib]System.Int16
     ldelema [mscorlib]System.UInt32
    pop
End_Orphan_b_d1:
    cgt.un
   stelem.i4
End_Orphan_a_c9:
Start_Orphan_b_d2:
   nop
End_Orphan_b_d2:
   ldloca LOCAL_0x1
Start_Orphan_b_d3:
      ldc.i4.3
     conv.u1
 newarr [mscorlib]System.Double
     ldloc LOCAL_0x0
Start_Orphan_b_d4:
     ldloc LOCAL_0x2
    pop
End_Orphan_b_d4:
    ldelema [mscorlib]System.Double
       ldloc LOCAL_0x4
       sizeof [mscorlib]System.UInt64
      ldelema [mscorlib]System.Double
     ldind.r8
      ldloc LOCAL_0x4
      sizeof [mscorlib]System.Double
Start_Orphan_b_d5:
     nop
End_Orphan_b_d5:
     ldelem.r8
Start_Orphan_b_d6:
     ldloc LOCAL_0x7
    stloc LOCAL_0x7
End_Orphan_b_d6:
    add 
   unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Double
End_Orphan_b_d3:
   ldloc LOCAL_0x1
  stobj [mscorlib]System.Single
End_Orphan_a_c8:
Start_Orphan_a_d7:
Start_Orphan_a_d8:
 ldc.i4 1 
 Loop_0x4: //WHAT
      ldc.i4.6
      ldc.i4.7
Start_Orphan_b_d9:
     nop
End_Orphan_b_d9:
     cgt.un
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x4
 pop
 Loop_0x5: // End of WHAT
End_Orphan_a_d8:
Start_Orphan_a_da:
      ldloca LOCAL_0x1
      ldloca LOCAL_0x1
     cpobj [mscorlib]System.Single
End_Orphan_a_da:
Start_Orphan_a_db:
      nop
End_Orphan_a_db:
       sizeof [mscorlib]System.SByte
Start_Orphan_b_dc:
      nop
End_Orphan_b_dc:
      conv.ovf.u2.un
     conv.u1
 newarr [mscorlib]System.UInt32
Start_Orphan_a_dd:
      nop
End_Orphan_a_dd:
       ldc.r4 float32(0x9a97386)
      call float64 [mscorlib]System.Math::Sqrt(float64)
Start_Orphan_b_de:
      ldloc LOCAL_0x2
     pop
End_Orphan_b_de:
     conv.ovf.i2
Start_Orphan_b_df:
Start_Orphan_a_e0:
     nop
End_Orphan_a_e0:
      ldloc LOCAL_0x6
      ldc.i4.m1
     ldelema [mscorlib]System.UInt64
    initobj [mscorlib]System.UInt64
End_Orphan_b_df:
    ldelema [mscorlib]System.UInt32
Start_Orphan_a_e1:
Start_Orphan_a_e2:
     nop
End_Orphan_a_e2:
Start_Orphan_b_e3:
     nop
End_Orphan_b_e3:
     ldloca LOCAL_0x0
Start_Orphan_a_e4:
     nop
End_Orphan_a_e4:
      sizeof [mscorlib]System.Int16
Start_Orphan_b_e5:
     nop
End_Orphan_b_e5:
     conv.ovf.i1
    stobj [mscorlib]System.Int16
End_Orphan_a_e1:
Start_Orphan_a_e6:
      ldc.i4.5
     stloc LOCAL_0x5
End_Orphan_a_e6:
Start_Orphan_b_e7:
       ldloc LOCAL_0x3
       sizeof [mscorlib]System.Double
      ldelema [mscorlib]System.UInt32
      ldc.i4.8
     stind.i4
End_Orphan_b_e7:
     ldloc LOCAL_0x2
Start_Orphan_a_e8:
      ldsflda int8 ILGEN_0x1d013582::field_0x0
      ldsflda int8 ILGEN_0x1d013582::field_0x0
     cpobj [mscorlib]System.SByte
End_Orphan_a_e8:
Start_Orphan_a_e9:
      nop
End_Orphan_a_e9:
       sizeof [mscorlib]System.Single
      conv.ovf.i8
     conv.ovf.u4.un
    ldelema [mscorlib]System.UInt32
   cpobj [mscorlib]System.UInt32
End_Orphan_a_d7:
Start_Orphan_a_ea:
       nop
End_Orphan_a_ea:
Start_Orphan_b_eb:
       nop
End_Orphan_b_eb:
       ldloca LOCAL_0x1
Start_Orphan_b_ec:
        ldloc LOCAL_0x3
        ldloc LOCAL_0x5
       ldelema [mscorlib]System.UInt32
      pop
End_Orphan_b_ec:
      ldind.r4
     conv.ovf.i8
Start_Orphan_a_ed:
Start_Orphan_a_ee:
      nop
End_Orphan_a_ee:
       ldloc LOCAL_0x6
       ldc.i4.5
      ldelema [mscorlib]System.UInt64
     pop
End_Orphan_a_ed:
Start_Orphan_a_ef:
       ldloca LOCAL_0x5
      pop
End_Orphan_a_ef:
        ldloc LOCAL_0x4
        sizeof [mscorlib]System.Int32
       ldelema [mscorlib]System.Double
Start_Orphan_b_f0:
       ldloc LOCAL_0x6
       ldc.i4.s 53
       ldc.i8 0x65b21f0bc6e7e8aa
      stelem.i8
End_Orphan_b_f0:
      ldind.r8
     conv.u8
    sub.ovf
   conv.ovf.i.un
Start_Orphan_b_f1:
Start_Orphan_a_f2:
Start_Orphan_a_f3:
     nop
End_Orphan_a_f3:
Start_Orphan_b_f4:
     nop
End_Orphan_b_f4:
     ldloca LOCAL_0x1
     ldloca LOCAL_0x1
    cpobj [mscorlib]System.Single
End_Orphan_a_f2:
Start_Orphan_a_f5:
      ldloca LOCAL_0x0
      ldloc LOCAL_0x5
     stind.i2
End_Orphan_a_f5:
Start_Orphan_a_f6:
      nop
End_Orphan_a_f6:
       ldc.r8 float64(0xd03cefebdd48964d)
Start_Orphan_b_f7:
      nop
End_Orphan_b_f7:
      conv.ovf.i8
     conv.i8
Start_Orphan_b_f8:
    nop
End_Orphan_b_f8:
    conv.ovf.u.un
   conv.u8
  pop
End_Orphan_b_f1:
  conv.ovf.u8.un
 sub.ovf.un
 br Branch_0x1

 Branch_0x0:

Start_Orphan_a_f9:
        ldloc LOCAL_0x4
        ldc.i4 0x9d4bde3e
       ldelema [mscorlib]System.Double
      ldind.r8
     pop
End_Orphan_a_f9:
Start_Orphan_b_fa:
Start_Orphan_a_fb:
      nop
End_Orphan_a_fb:
       ldc.i4.5
       sizeof [mscorlib]System.Int64
      sub.ovf.un
     pop
End_Orphan_b_fa:
     ldloc LOCAL_0x3
Start_Orphan_b_fc:
Start_Orphan_a_fd:
      nop
End_Orphan_a_fd:
Start_Orphan_b_fe:
      nop
End_Orphan_b_fe:
      ldc.i4.1
     stloc LOCAL_0x0
End_Orphan_b_fc:
     sizeof [mscorlib]System.UInt32
Start_Orphan_b_ff:
Start_Orphan_a_100:
      ldloc LOCAL_0x6
     pop
End_Orphan_a_100:
Start_Orphan_a_101:
      nop
End_Orphan_a_101:
      ldloc LOCAL_0x7
       ldsflda int8 ILGEN_0x1d013582::field_0x0
Start_Orphan_b_102:
      nop
End_Orphan_b_102:
      ldind.i1
Start_Orphan_b_103:
      ldloca LOCAL_0x1
      ldc.r4 float32(0x314c3705)
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Single
End_Orphan_b_103:
     ldelema [mscorlib]System.Int16
Start_Orphan_a_104:
      nop
End_Orphan_a_104:
      ldc.i4 0x15852f69
       ldloc LOCAL_0x5
      conv.ovf.u
Start_Orphan_b_105:
      ldloc LOCAL_0x7
     stloc LOCAL_0x7
End_Orphan_b_105:
     clt
    stind.i2
End_Orphan_b_ff:
    ldelema [mscorlib]System.UInt32
   ldobj [mscorlib]System.UInt32
  conv.ovf.i8
Start_Orphan_b_106:
 nop
End_Orphan_b_106:
 conv.i8
 Branch_0x1:

 ret
}
.method public static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	1

.locals (int32 MAINLOCAL_0x0)

.try
{
 call int64  ILGEN_0x1d013582::Method_0x7ef63454()
conv.i4
stloc MAINLOCAL_0x0
leave DONE
}
catch [mscorlib]System.IndexOutOfRangeException
{
leave DONE_RIGHT
}
DONE:
ldc.i4 101
ret
DONE_RIGHT:
ldc.i4 100
ret
}

}
