// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly 'volatileLocal2' {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}

.class public ILGEN_0x537f7b0 {

//BEGIN STATIC FIELDS
.field static	unsigned int16[] field_0x0
.field static	int8[] field_0x1
.field static native int[] field_0x2
.field static	unsigned int8 field_0x3
.field static	unsigned int64[] field_0x4
.field static	int32 field_0x5
.field static	unsigned int32[] field_0x6
//END STATIC FIELDS
.method static float32  Method_0x323f83b5(float64 Arg_0x0, int8 Arg_0x1, unsigned int32 Arg_0x2, int16 Arg_0x3, unsigned int8 Arg_0x4, float64 Arg_0x5, int16 Arg_0x6, unsigned int16 Arg_0x7, float64 Arg_0x8, native int Arg_0x9, int32 Arg_0xa, native int Arg_0xb, unsigned int16 Arg_0xc, native int Arg_0xd, unsigned int32 Arg_0xe) {
.zeroinit 
.maxstack	19

 .locals (int8 LOCAL_0x0,unsigned int32 LOCAL_0x1,float64 LOCAL_0x2,int32[] LOCAL_0x3,int64 LOCAL_0x4,unsigned int8[] LOCAL_0x5,int32 LOCAL_0x6,unsigned int8 LOCAL_0x7,unsigned int8 LOCAL_0x8,object LOCAL_0x9,unsigned int8[] LOCAL_0xa,float64[] LOCAL_0xb,float64 LOCAL_0xc,native int[] LOCAL_0xd,float32 LOCAL_0xe)
//BEGIN INIT LOCALS
ldc.i4 0x4a733af0 
stloc LOCAL_0x0
ldc.i4 0x7efeeba2 
stloc LOCAL_0x1
ldc.r8 float64(0xa6d5160a595ae171) 
stloc LOCAL_0x2
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc LOCAL_0x3
ldc.i8 0x937d00542e25cd43 
stloc LOCAL_0x4
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc LOCAL_0x5
ldc.i4 0xc4e8fc32 
stloc LOCAL_0x6
ldc.i4 0x4011d5af 
stloc LOCAL_0x7
ldc.i4 0x82068af5 
stloc LOCAL_0x8
newobj     instance void [mscorlib]System.Object::.ctor()
stloc LOCAL_0x9
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc LOCAL_0xa
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc LOCAL_0xb
ldc.r8 float64(0xea288fb7134ecd94) 
stloc LOCAL_0xc
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stloc LOCAL_0xd
ldc.r4 float32(0x17d95789) 
stloc LOCAL_0xe

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x537f7b0::field_0x0
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0x537f7b0::field_0x1
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0x537f7b0::field_0x2
ldc.i4 0x14f1c39a 
stsfld unsigned int8 ILGEN_0x537f7b0::field_0x3
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x537f7b0::field_0x4
ldc.i4 0x1d29603c 
stsfld int32 ILGEN_0x537f7b0::field_0x5
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x537f7b0::field_0x6

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_a_0:
Start_Orphan_a_1:
Start_Orphan_a_2:
Start_Orphan_a_3:
     nop
End_Orphan_a_3:
Start_Orphan_b_4:
     nop
End_Orphan_b_4:
     ldarga Arg_0x6
    initobj [mscorlib]System.Int16
End_Orphan_a_2:
Start_Orphan_a_5:
      ldloca LOCAL_0xe
      ldloc LOCAL_0xe
     stobj [mscorlib]System.Single
End_Orphan_a_5:
Start_Orphan_a_6:
      nop
End_Orphan_a_6:
       ldc.i8 0x3a37ce68e54eed3c
       ldc.i8 0xaa37114c730e4c61
Start_Orphan_b_7:
      nop
End_Orphan_b_7:
      add
     conv.i4
    conv.u1
 newarr [mscorlib]System.SByte
   pop
End_Orphan_a_1:
Start_Orphan_b_8:
    ldsfld unsigned int64[] ILGEN_0x537f7b0::field_0x4
   stsfld unsigned int64[] ILGEN_0x537f7b0::field_0x4
End_Orphan_b_8:
   ldloc LOCAL_0xd
Start_Orphan_a_9:
 ldc.i4 7 
 Loop_0x2: //WHAT
Start_Orphan_b_a:
      nop
End_Orphan_b_a:
      ldsfld int8[] ILGEN_0x537f7b0::field_0x1
       sizeof [mscorlib]System.IntPtr
      not
Start_Orphan_b_b:
      ldloca LOCAL_0x4
      ldloc LOCAL_0x4
     stobj [mscorlib]System.Int64
End_Orphan_b_b:
ldc.i4 1
clt
     ldelem.i1
Start_Orphan_a_c:
      ldsfld native int[] ILGEN_0x537f7b0::field_0x2
      ldloc LOCAL_0x8
ldc.i4 1
clt
      ldarg Arg_0xd
     stelem.i
End_Orphan_a_c:
Start_Orphan_a_d:
      nop
End_Orphan_a_d:
Start_Orphan_b_e:
      nop
End_Orphan_b_e:
      ldarga Arg_0x3
Start_Orphan_b_f:
      ldc.r8 float64(0x886f20721a1f1a5a)
     pop
End_Orphan_b_f:
     unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Int16
Start_Orphan_b_10:
      ldarg Arg_0x3
Start_Orphan_b_11:
     nop
End_Orphan_b_11:
     conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.Byte
    stloc LOCAL_0xa
End_Orphan_b_10:
    shr.un
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x2
 pop
 Loop_0x3: // End of WHAT
End_Orphan_a_9:
Start_Orphan_b_12:
Start_Orphan_a_13:
      ldloc LOCAL_0x3
      ldarg Arg_0x1
ldc.i4 1
clt
      ldc.i4.m1
     stelem.i4
End_Orphan_a_13:
Start_Orphan_a_14:
      nop
End_Orphan_a_14:
       sizeof [mscorlib]System.Byte
      conv.u1
       ldloc LOCAL_0x0
       ldarg Arg_0xb
Start_Orphan_b_15:
      nop
End_Orphan_b_15:
      mul
     mul
Start_Orphan_a_16:
      ldloca LOCAL_0x6
      ldsflda int32 ILGEN_0x537f7b0::field_0x5
     cpobj [mscorlib]System.Int32
End_Orphan_a_16:
Start_Orphan_a_17:
      nop
End_Orphan_a_17:
       ldloc LOCAL_0x4
Start_Orphan_b_18:
      nop
End_Orphan_b_18:
      conv.u
     conv.i4
    add
   starg Arg_0xd
End_Orphan_b_12:
   ldarg Arg_0x4
Start_Orphan_b_19:
Start_Orphan_a_1a:
Start_Orphan_a_1b:
     nop
End_Orphan_a_1b:
      sizeof [mscorlib]System.UInt16
Start_Orphan_b_1c:
     nop
End_Orphan_b_1c:
     conv.u1
 newarr [mscorlib]System.Double
    pop
End_Orphan_a_1a:
Start_Orphan_a_1d:
      ldarg Arg_0x6
     starg Arg_0x1
End_Orphan_a_1d:
Start_Orphan_a_1e:
      nop
End_Orphan_a_1e:
       ldarg Arg_0xc
       ldarg Arg_0x9
Start_Orphan_b_1f:
      nop
End_Orphan_b_1f:
      sub
Start_Orphan_a_20:
      nop
End_Orphan_a_20:
       ldarga Arg_0xb
Start_Orphan_b_21:
      nop
End_Orphan_b_21:
      ldind.i
Start_Orphan_b_22:
      ldarga Arg_0x2
      sizeof [mscorlib]System.UInt16
     stind.i4
End_Orphan_b_22:
     mul
Start_Orphan_a_23:
      nop
End_Orphan_a_23:
       ldc.i4.6
       ldarg Arg_0xb
      add
       ldarg Arg_0xd
       ldarg Arg_0x9
Start_Orphan_b_24:
      nop
End_Orphan_b_24:
      add
     add
    mul
Start_Orphan_a_25:
      sizeof [mscorlib]System.Int16
     conv.u1
 newarr [mscorlib]System.UInt16
    pop
End_Orphan_a_25:
Start_Orphan_a_26:
      ldsfld unsigned int16[] ILGEN_0x537f7b0::field_0x0
      ldc.i4.8
ldc.i4 1
clt
      ldc.i4.8
     stelem.i2
End_Orphan_a_26:
Start_Orphan_a_27:
      nop
End_Orphan_a_27:
       ldarga Arg_0xb
Start_Orphan_b_28:
      nop
End_Orphan_b_28:
      ldobj [mscorlib]System.IntPtr
Start_Orphan_a_29:
      nop
End_Orphan_a_29:
       ldsfld unsigned int64[] ILGEN_0x537f7b0::field_0x4
      ldlen
Start_Orphan_b_2a:
      ldarg Arg_0x4
     pop
End_Orphan_b_2a:
     sub
       ldc.i4 0x4db64cc1
      conv.i
Start_Orphan_a_2b:
      nop
End_Orphan_a_2b:
       ldarg Arg_0x9
Start_Orphan_b_2c:
      nop
End_Orphan_b_2c:
      conv.u4
Start_Orphan_b_2d:
      ldloca LOCAL_0x8
      ldc.i4.5
     stobj [mscorlib]System.Byte
End_Orphan_b_2d:
     cgt
Start_Orphan_b_2e:
Start_Orphan_a_2f:
     nop
End_Orphan_a_2f:
      ldsfld unsigned int64[] ILGEN_0x537f7b0::field_0x4
      ldc.i4.m1
Start_Orphan_b_30:
     nop
End_Orphan_b_30:
ldc.i4 1
clt
     ldelema [mscorlib]System.UInt64
Start_Orphan_a_31:
     nop
End_Orphan_a_31:
      ldloca LOCAL_0x4
Start_Orphan_b_32:
     nop
End_Orphan_b_32:
     ldobj [mscorlib]System.Int64
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt64
End_Orphan_b_2e:
    sub
Start_Orphan_b_33:
Start_Orphan_a_34:
     ldsfld int8[] ILGEN_0x537f7b0::field_0x1
    stsfld int8[] ILGEN_0x537f7b0::field_0x1
End_Orphan_a_34:
    ldarga Arg_0x2
   initobj [mscorlib]System.UInt32
End_Orphan_b_33:
   clt.un
  stloc LOCAL_0x1
End_Orphan_b_19:
ldc.i4 1
clt
  ldelema [mscorlib]System.IntPtr
 initobj [mscorlib]System.IntPtr
End_Orphan_a_0:
Start_Orphan_a_35:
Start_Orphan_a_36:
Start_Orphan_a_37:
      ldsfld native int[] ILGEN_0x537f7b0::field_0x2
      ldsfld int32 ILGEN_0x537f7b0::field_0x5
ldc.i4 1
clt
     ldelema [mscorlib]System.IntPtr
      ldloc LOCAL_0x2
Start_Orphan_b_38:
     nop
End_Orphan_b_38:
     conv.i
    stobj [mscorlib]System.IntPtr
End_Orphan_a_37:
    ldloc LOCAL_0x5
   stloc LOCAL_0xa
End_Orphan_a_36:
   ldarga Arg_0xe
Start_Orphan_a_39:
Start_Orphan_a_3a:
Start_Orphan_a_3b:
     nop
End_Orphan_a_3b:
      ldc.r8 float64(0x10b81f1322f0a34d)
Start_Orphan_b_3c:
     nop
End_Orphan_b_3c:
     conv.u1
    starg Arg_0xc
End_Orphan_a_3a:
Start_Orphan_b_3d:
Start_Orphan_a_3e:
     nop
End_Orphan_a_3e:
     ldnull
    pop
End_Orphan_b_3d:
    ldsfld native int[] ILGEN_0x537f7b0::field_0x2
   stsfld native int[] ILGEN_0x537f7b0::field_0x2
End_Orphan_a_39:
Start_Orphan_a_3f:
Start_Orphan_b_40:
      ldloc LOCAL_0x3
     stloc LOCAL_0x3
End_Orphan_b_40:
     ldloca LOCAL_0xe
     ldloca LOCAL_0xe
    cpobj [mscorlib]System.Single
End_Orphan_a_3f:
Start_Orphan_a_41:
Start_Orphan_a_42:
      nop
End_Orphan_a_42:
Start_Orphan_b_43:
      nop
End_Orphan_b_43:
      ldloca LOCAL_0x1
     initobj [mscorlib]System.UInt32
End_Orphan_a_41:
Start_Orphan_b_44:
Start_Orphan_a_45:
      nop
End_Orphan_a_45:
Start_Orphan_b_46:
      nop
End_Orphan_b_46:
      ldsfld native int[] ILGEN_0x537f7b0::field_0x2
     stloc LOCAL_0xd
End_Orphan_b_44:
     ldloc LOCAL_0x3
        ldloc LOCAL_0xa
        sizeof [mscorlib]System.UInt32
ldc.i4 1
clt
       ldelema [mscorlib]System.Byte
      ldobj [mscorlib]System.Byte
Start_Orphan_b_47:
Start_Orphan_a_48:
      nop
End_Orphan_a_48:
      ldloca LOCAL_0xe
Start_Orphan_a_49:
      nop
End_Orphan_a_49:
       ldc.r4 float32(0x784bd9a)
Start_Orphan_b_4a:
      nop
End_Orphan_b_4a:
      call float64 [mscorlib]System.Math::Sin(float64)
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Single
End_Orphan_b_47:
     conv.i1
Start_Orphan_b_4b:
Start_Orphan_a_4c:
      ldsfld unsigned int32[] ILGEN_0x537f7b0::field_0x6
     pop
End_Orphan_a_4c:
Start_Orphan_a_4d:
      nop
End_Orphan_a_4d:
       sizeof [mscorlib]System.Int64
      conv.u1
 newarr [mscorlib]System.SByte
Start_Orphan_a_4e:
      nop
End_Orphan_a_4e:
      ldloc LOCAL_0x1
ldc.i4 1
clt
     ldelema [mscorlib]System.SByte
    pop
End_Orphan_b_4b:
ldc.i4 1
clt
    ldelema [mscorlib]System.Int32
Start_Orphan_b_4f:
Start_Orphan_b_50:
     ldarga Arg_0x6
      sizeof [mscorlib]System.Double
      ldarg Arg_0x3
Start_Orphan_b_51:
     nop
End_Orphan_b_51:
     sub
    stobj [mscorlib]System.Int16
End_Orphan_b_50:
    ldloca LOCAL_0x2
     ldloc LOCAL_0xe
       ldarg Arg_0x9
Start_Orphan_b_52:
      nop
End_Orphan_b_52:
      conv.u8
Start_Orphan_b_53:
      ldloc LOCAL_0xe
     stloc LOCAL_0xc
End_Orphan_b_53:
     conv.r8
    add 
   stobj [mscorlib]System.Double
End_Orphan_b_4f:
   unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Int32
  unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt32
End_Orphan_a_35:
   ldc.r4 float32(0x88040df4)
  conv.u4
 conv.i8
Start_Orphan_b_54:
Start_Orphan_a_55:
   ldloc LOCAL_0x3
  stloc LOCAL_0x3
End_Orphan_a_55:
Start_Orphan_a_56:
Start_Orphan_b_57:
     ldarga Arg_0x1
Start_Orphan_a_58:
     nop
End_Orphan_a_58:
      ldsfld int8[] ILGEN_0x537f7b0::field_0x1
      ldsfld unsigned int8 ILGEN_0x537f7b0::field_0x3
ldc.i4 1
clt
     ldelema [mscorlib]System.SByte
    cpobj [mscorlib]System.SByte
End_Orphan_b_57:
    ldloc LOCAL_0x3
   pop
End_Orphan_a_56:
Start_Orphan_a_59:
Start_Orphan_b_5a:
      ldarga Arg_0xd
      ldarga Arg_0xd
     cpobj [mscorlib]System.IntPtr
End_Orphan_b_5a:
     ldarga Arg_0x7
Start_Orphan_a_5b:
       ldsfld unsigned int64[] ILGEN_0x537f7b0::field_0x4
       ldarg Arg_0x1
ldc.i4 1
clt
      ldelema [mscorlib]System.UInt64
     pop
End_Orphan_a_5b:
Start_Orphan_a_5c:
      nop
End_Orphan_a_5c:
       ldc.i8 0xab24823b7ec51a21
       ldloc LOCAL_0x4
      add
     conv.i2
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt16
End_Orphan_a_59:
Start_Orphan_b_5d:
       ldarga Arg_0x1
      initobj [mscorlib]System.SByte
End_Orphan_b_5d:
      ldc.i4.3
Start_Orphan_b_5e:
Start_Orphan_b_5f:
      nop
End_Orphan_b_5f:
      ldarga Arg_0x6
Start_Orphan_a_60:
      nop
End_Orphan_a_60:
       ldloc LOCAL_0xa
Start_Orphan_b_61:
      nop
End_Orphan_b_61:
      ldlen
     stobj [mscorlib]System.Int16
End_Orphan_b_5e:
     conv.u8
Start_Orphan_a_62:
Start_Orphan_b_63:
      nop
End_Orphan_b_63:
      ldsflda int32 ILGEN_0x537f7b0::field_0x5
Start_Orphan_a_64:
      nop
End_Orphan_a_64:
      ldarga Arg_0xa
     cpobj [mscorlib]System.Int32
End_Orphan_a_62:
Start_Orphan_a_65:
       ldarga Arg_0x2
      initobj [mscorlib]System.UInt32
End_Orphan_a_65:
       ldloca LOCAL_0x4
Start_Orphan_b_66:
       ldarg Arg_0xc
      starg Arg_0x4
End_Orphan_b_66:
      unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Int64
     neg
    or
   conv.ovf.u4
  conv.r4
Start_Orphan_b_67:
Start_Orphan_a_68:
   ldloc LOCAL_0x5
  pop
End_Orphan_a_68:
Start_Orphan_a_69:
      ldsfld int32 ILGEN_0x537f7b0::field_0x5
     starg Arg_0xc
End_Orphan_a_69:
       sizeof [mscorlib]System.UInt32
Start_Orphan_b_6a:
      nop
End_Orphan_b_6a:
      conv.u1
 newarr [mscorlib]System.Int32
Start_Orphan_b_6b:
       ldsfld unsigned int64[] ILGEN_0x537f7b0::field_0x4
       ldarg Arg_0xa
ldc.i4 1
clt
      ldelema [mscorlib]System.UInt64
     initobj [mscorlib]System.UInt64
End_Orphan_b_6b:
     ldlen
Start_Orphan_a_6c:
      nop
End_Orphan_a_6c:
Start_Orphan_b_6d:
      nop
End_Orphan_b_6d:
      ldarg Arg_0x0
      ldarg Arg_0x0
 bgt.un Branch_0x4

Start_Orphan_a_6e:
      nop
End_Orphan_a_6e:
       ldarg Arg_0x9
Start_Orphan_b_6f:
      nop
End_Orphan_b_6f:
      not
 br Branch_0x5

 Branch_0x4:

       ldarg Arg_0xb
       ldarg Arg_0xd
 bgt.un Branch_0x6

       ldarg Arg_0xd
 br Branch_0x7

 Branch_0x6:

       ldarg Arg_0x9
 Branch_0x7:

 Branch_0x5:

    sub
       ldarg Arg_0xd
       ldarg Arg_0x9
      ceq
       ldsfld unsigned int8 ILGEN_0x537f7b0::field_0x3
       ldarg Arg_0x9
 blt.un Branch_0xa

       ldarg Arg_0xd
 br Branch_0xb

 Branch_0xa:

       ldarg Arg_0xb
 Branch_0xb:

     clt.un
Start_Orphan_a_70:
      ldloc LOCAL_0xa
     pop
End_Orphan_a_70:
       ldarg Arg_0xd
       ldarg Arg_0xb
 bgt.un Branch_0xc

       ldarg Arg_0x3
 br Branch_0xd

 Branch_0xc:

       ldarg Arg_0x4
 Branch_0xd:

     conv.i4
 bgt.un Branch_0x8

Start_Orphan_a_71:
      ldarga Arg_0x3
      ldarg Arg_0x4
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int16
End_Orphan_a_71:
       ldarg Arg_0xb
       ldarg Arg_0x6
Start_Orphan_b_72:
      nop
End_Orphan_b_72:
      sub
Start_Orphan_a_73:
      nop
End_Orphan_a_73:
       sizeof [mscorlib]System.UInt32
Start_Orphan_b_74:
      nop
End_Orphan_b_74:
      conv.u
     sub
 br Branch_0x9

 Branch_0x8:

       ldarg Arg_0x9
Start_Orphan_b_75:
      nop
End_Orphan_b_75:
      conv.u
       ldloc LOCAL_0xd
Start_Orphan_b_76:
      nop
End_Orphan_b_76:
      ldlen
     add
 Branch_0x9:

   cgt.un
Start_Orphan_a_77:
      ldsfld unsigned int32[] ILGEN_0x537f7b0::field_0x6
      ldc.i4.1
     ldelema [mscorlib]System.UInt32
    pop
End_Orphan_a_77:
Start_Orphan_a_78:
      nop
End_Orphan_a_78:
Start_Orphan_b_79:
      nop
End_Orphan_b_79:
      ldloc LOCAL_0xb
Start_Orphan_a_7a:
      nop
End_Orphan_a_7a:
      ldarg Arg_0x1
ldc.i4 1
clt
     ldelem.r8
Start_Orphan_b_7b:
      ldsfld native int[] ILGEN_0x537f7b0::field_0x2
      sizeof [mscorlib]System.Int64
Start_Orphan_b_7c:
     nop
End_Orphan_b_7c:
ldc.i4 1
clt
     ldelema [mscorlib]System.IntPtr
Start_Orphan_a_7d:
     nop
End_Orphan_a_7d:
      ldloc LOCAL_0x1
      ldarg Arg_0xd
     add
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.IntPtr
End_Orphan_b_7b:
    conv.i
Start_Orphan_a_7e:
      ldarg Arg_0xd
      ldloc LOCAL_0x8
Start_Orphan_b_7f:
     nop
End_Orphan_b_7f:
     cgt.un
    stloc LOCAL_0x1
End_Orphan_a_7e:
Start_Orphan_a_80:
      ldarga Arg_0x8
      ldloc LOCAL_0xe
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Double
End_Orphan_a_80:
Start_Orphan_a_81:
      nop
End_Orphan_a_81:
       ldc.r4 float32(0xd1cde548)
       ldarg Arg_0x5
Start_Orphan_b_82:
      nop
End_Orphan_b_82:
      add 
     call float64 [mscorlib]System.Math::Sin(float64)
Start_Orphan_b_83:
Start_Orphan_b_84:
     nop
End_Orphan_b_84:
     ldarg Arg_0xe
    stsfld int32 ILGEN_0x537f7b0::field_0x5
End_Orphan_b_83:
    conv.i
   add
 bge Branch_0x2

Start_Orphan_a_85:
     ldloca LOCAL_0x6
    pop
End_Orphan_a_85:
Start_Orphan_b_86:
      nop
End_Orphan_b_86:
      ldsfld unsigned int64[] ILGEN_0x537f7b0::field_0x4
       ldarg Arg_0xb
       ldc.i4.4
      cgt
     ldelem.u8
Start_Orphan_b_87:
Start_Orphan_a_88:
     nop
End_Orphan_a_88:
Start_Orphan_b_89:
     nop
End_Orphan_b_89:
     ldloca LOCAL_0x4
    pop
End_Orphan_b_87:
    conv.u4
   conv.u1
 br Branch_0x3

 Branch_0x2:

Start_Orphan_b_8a:
Start_Orphan_a_8b:
     ldarga Arg_0xe
     sizeof [mscorlib]System.UInt64
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt32
End_Orphan_a_8b:
Start_Orphan_a_8c:
     nop
End_Orphan_a_8c:
     ldloc LOCAL_0x5
Start_Orphan_a_8d:
     nop
End_Orphan_a_8d:
      ldarg Arg_0xb
      ldloc LOCAL_0x6
     cgt.un
    ldelema [mscorlib]System.Byte
   pop
End_Orphan_b_8a:
   ldarg Arg_0x1
 Branch_0x3:

Start_Orphan_a_8e:
Start_Orphan_a_8f:
Start_Orphan_a_90:
    nop
End_Orphan_a_90:
     ldloc LOCAL_0x6
Start_Orphan_b_91:
    nop
End_Orphan_b_91:
    conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.IntPtr
     ldloc LOCAL_0xd
    ldlen
Start_Orphan_a_92:
    nop
End_Orphan_a_92:
     ldc.i8 0x5b635be32164797a
    conv.i
ldc.i4 1
clt
   stelem.i
End_Orphan_a_8f:
Start_Orphan_a_93:
     ldarga Arg_0x1
     ldc.i4.m1
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.SByte
End_Orphan_a_93:
Start_Orphan_a_94:
     nop
End_Orphan_a_94:
      ldarg Arg_0xb
Start_Orphan_b_95:
     nop
End_Orphan_b_95:
     conv.i4
    conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.SByte
    sizeof [mscorlib]System.Byte
Start_Orphan_b_96:
     ldsfld unsigned int16[] ILGEN_0x537f7b0::field_0x0
     ldc.i4.8
Start_Orphan_b_97:
    nop
End_Orphan_b_97:
    ldelema [mscorlib]System.UInt16
Start_Orphan_a_98:
    nop
End_Orphan_a_98:
     ldsfld int8[] ILGEN_0x537f7b0::field_0x1
    ldlen
   stobj [mscorlib]System.UInt16
End_Orphan_b_96:
ldc.i4 1
clt
   ldelema [mscorlib]System.SByte
Start_Orphan_a_99:
    ldarga Arg_0x2
Start_Orphan_a_9a:
    nop
End_Orphan_a_9a:
     ldsfld unsigned int32[] ILGEN_0x537f7b0::field_0x6
     sizeof [mscorlib]System.Byte
ldc.i4 1
clt
    ldelema [mscorlib]System.UInt32
   cpobj [mscorlib]System.UInt32
End_Orphan_a_99:
Start_Orphan_a_9b:
     ldloc LOCAL_0xb
    pop
End_Orphan_a_9b:
     ldc.i4.1
    conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.SByte
Start_Orphan_a_9c:
     ldloca LOCAL_0x2
     ldloca LOCAL_0x2
    cpobj [mscorlib]System.Double
End_Orphan_a_9c:
Start_Orphan_a_9d:
     nop
End_Orphan_a_9d:
      sizeof [mscorlib]System.SByte
Start_Orphan_b_9e:
     nop
End_Orphan_b_9e:
     conv.r4
      ldloca LOCAL_0xe
     ldobj [mscorlib]System.Single
Start_Orphan_b_9f:
     ldsfld native int[] ILGEN_0x537f7b0::field_0x2
     sizeof [mscorlib]System.Int32
ldc.i4 1
clt
     ldarg Arg_0xb
    stelem.i
End_Orphan_b_9f:
    cgt.un
Start_Orphan_b_a0:
Start_Orphan_a_a1:
    nop
End_Orphan_a_a1:
     ldc.i4.3
Start_Orphan_b_a2:
    nop
End_Orphan_b_a2:
    conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.IntPtr
   pop
End_Orphan_b_a0:
ldc.i4 1
clt
   ldelema [mscorlib]System.SByte
  cpobj [mscorlib]System.SByte
End_Orphan_a_8e:
Start_Orphan_a_a3:
Start_Orphan_a_a4:
     ldloca LOCAL_0x4
     ldloc LOCAL_0x4
    stind.i8
End_Orphan_a_a4:
Start_Orphan_b_a5:
     nop
End_Orphan_b_a5:
     sizeof [mscorlib]System.Byte
    conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.UInt64
   stsfld unsigned int64[] ILGEN_0x537f7b0::field_0x4
End_Orphan_a_a3:
Start_Orphan_a_a6:
      nop
End_Orphan_a_a6:
       ldarga Arg_0xd
      ldobj [mscorlib]System.IntPtr
Start_Orphan_a_a7:
      nop
End_Orphan_a_a7:
       ldarg Arg_0xb
       sizeof [mscorlib]System.Int64
      sub
Start_Orphan_b_a8:
      ldarg Arg_0x5
     pop
End_Orphan_b_a8:
     sub
     ldloc LOCAL_0x7
    mul
Start_Orphan_a_a9:
      nop
End_Orphan_a_a9:
       ldc.i4.4
      conv.i
       ldsfld unsigned int64[] ILGEN_0x537f7b0::field_0x4
Start_Orphan_b_aa:
      nop
End_Orphan_b_aa:
      ldlen
     sub
    conv.i4
Start_Orphan_b_ab:
Start_Orphan_a_ac:
     ldarga Arg_0x8
    pop
End_Orphan_a_ac:
Start_Orphan_a_ad:
     nop
End_Orphan_a_ad:
Start_Orphan_b_ae:
     nop
End_Orphan_b_ae:
     ldloc LOCAL_0x3
Start_Orphan_a_af:
     nop
End_Orphan_a_af:
      ldloc LOCAL_0xa
Start_Orphan_b_b0:
     nop
End_Orphan_b_b0:
     ldlen
Start_Orphan_b_b1:
     ldc.r8 float64(0x2e3fb6b8b15423a8)
    stloc LOCAL_0x2
End_Orphan_b_b1:
ldc.i4 1
clt
    ldelema [mscorlib]System.Int32
      ldarg Arg_0xd
Start_Orphan_b_b2:
     nop
End_Orphan_b_b2:
     conv.i2
      ldloc LOCAL_0x2
     conv.u
    cgt
   stobj [mscorlib]System.Int32
End_Orphan_b_ab:
   add
Start_Orphan_a_b3:
      ldloc LOCAL_0xa
Start_Orphan_b_b4:
     nop
End_Orphan_b_b4:
     ldlen
    starg Arg_0x4
End_Orphan_a_b3:
Start_Orphan_a_b5:
      ldloc LOCAL_0xb
     stloc LOCAL_0xb
End_Orphan_a_b5:
Start_Orphan_a_b6:
      nop
End_Orphan_a_b6:
      ldc.i4.0
Start_Orphan_a_b7:
      nop
End_Orphan_a_b7:
       ldc.r4 float32(0x6a37284d)
      conv.i4
     add
Start_Orphan_a_b8:
      nop
End_Orphan_a_b8:
       ldc.i4.6
      conv.i1
       ldc.i4.7
Start_Orphan_b_b9:
      nop
End_Orphan_b_b9:
      conv.u2
     add
Start_Orphan_b_ba:
Start_Orphan_a_bb:
     nop
End_Orphan_a_bb:
     ldloc LOCAL_0xd
    pop
End_Orphan_b_ba:
    add
       sizeof [mscorlib]System.IntPtr
       ldarg Arg_0xd
      mul
Start_Orphan_b_bc:
      ldarga Arg_0x0
     initobj [mscorlib]System.Double
End_Orphan_b_bc:
     conv.i8
Start_Orphan_a_bd:
      ldsfld unsigned int16[] ILGEN_0x537f7b0::field_0x0
      sizeof [mscorlib]System.Int64
ldc.i4 1
clt
      sizeof [mscorlib]System.Byte
     stelem.i2
End_Orphan_a_bd:
Start_Orphan_a_be:
      nop
End_Orphan_a_be:
       ldarg Arg_0x9
      conv.u
     conv.u8
    cgt.un
   mul
  cgt.un
 blt.un Branch_0x0

Start_Orphan_b_bf:
Start_Orphan_a_c0:
 ldc.i4 1 
 Loop_0x0: //WHAT
     ldc.i4.4
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x0
 pop
 Loop_0x1: // End of WHAT
End_Orphan_a_c0:
    ldarga Arg_0x1
Start_Orphan_a_c1:
     ldsfld unsigned int32[] ILGEN_0x537f7b0::field_0x6
     sizeof [mscorlib]System.UInt16
ldc.i4 1
clt
     ldarg Arg_0x3
    stelem.i4
End_Orphan_a_c1:
Start_Orphan_a_c2:
     nop
End_Orphan_a_c2:
      ldloc LOCAL_0x0
Start_Orphan_b_c3:
     nop
End_Orphan_b_c3:
     conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.SByte
Start_Orphan_a_c4:
     nop
End_Orphan_a_c4:
      ldsfld int32 ILGEN_0x537f7b0::field_0x5
      ldarg Arg_0xe
Start_Orphan_b_c5:
     nop
End_Orphan_b_c5:
     or
Start_Orphan_b_c6:
     ldloc LOCAL_0x3
    stloc LOCAL_0x3
End_Orphan_b_c6:
ldc.i4 1
clt
    ldelema [mscorlib]System.SByte
   cpobj [mscorlib]System.SByte
End_Orphan_b_bf:
   ldloca LOCAL_0xc
Start_Orphan_b_c7:
   nop
End_Orphan_b_c7:
   ldarga Arg_0x0
  cpobj [mscorlib]System.Double
 br Branch_0x1

 Branch_0x0:

   sizeof [mscorlib]System.Single
  starg Arg_0x4
 Branch_0x1:

End_Orphan_b_67:
 conv.i4
pop
End_Orphan_b_54:
conv.r.un
 ret
}
.method public static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	31
.try {
ldc.r8 float64(0xb36fac34506b7d6) 
ldc.i4 0xb509985a 
ldc.i4 0xa894cffd 
ldc.i4 0x371b3df6 
ldc.i4 0x609205b2 
ldc.r8 float64(0x8b2b84255d5d7d22) 
ldc.i4 0xbdf1ad8a 
ldc.i4 0xafc4893 
ldc.r8 float64(0x53d35c16d05e0dcf) 
ldc.i4 0x6aad52ae conv.i
ldc.i4 0xa63a40c4 
ldc.i4 0x5185843a conv.i
ldc.i4 0xa18022b5 
ldc.i4 0x3ed3bef2 conv.i
ldc.i4 0x4007c6bd 
 call float32  ILGEN_0x537f7b0::Method_0x323f83b5(float64 Arg_0x0, int8 Arg_0x1, unsigned int32 Arg_0x2, int16 Arg_0x3, unsigned int8 Arg_0x4, float64 Arg_0x5, int16 Arg_0x6, unsigned int16 Arg_0x7, float64 Arg_0x8, native int Arg_0x9, int32 Arg_0xa, native int Arg_0xb, unsigned int16 Arg_0xc, native int Arg_0xd, unsigned int32 Arg_0xe)
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
