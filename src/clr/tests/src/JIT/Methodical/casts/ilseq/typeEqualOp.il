// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly ILGEN_0x6d358988 {}
.assembly extern mscorlib{auto}

.class ILGEN_0x6d358988 {

//BEGIN STATIC FIELDS
.field static	unsigned int16 field_0x0
.field static	native int field_0x1
.field static	float64 field_0x2
.field static	int64 field_0x3
.field static	int8[] field_0x4
.field static	int32[] field_0x5
.field static	unsigned int8[] field_0x6
.field static	int64 field_0x7
.field static	float64 field_0x8
.field static	unsigned int64[] field_0x9
.field static	unsigned int16[] field_0xa
.field static	int16[] field_0xb
.field static	unsigned int16[] field_0xc
.field static	int16[] field_0xd
//END STATIC FIELDS
.method static unsigned int8  Method_0xee788a65(float32 Arg_0x0, int16 Arg_0x1, int16 Arg_0x2) {
.zeroinit 
.maxstack	19

 .locals (float64 LOCAL_0x0,native int LOCAL_0x1,int16 LOCAL_0x2,object LOCAL_0x3,unsigned int32[] LOCAL_0x4,int32[] LOCAL_0x5,int8[] LOCAL_0x6,unsigned int64 LOCAL_0x7,unsigned int32[] LOCAL_0x8,int32[] LOCAL_0x9,float32 LOCAL_0xa,object LOCAL_0xb,unsigned int16[] LOCAL_0xc,unsigned int16 LOCAL_0xd)
//BEGIN INIT LOCALS
ldc.r8 float64(0x40df61335c877dc6) 
stloc LOCAL_0x0
ldc.i4 0x357473f 
stloc LOCAL_0x1
ldc.i4 0xeada6cdf 
stloc LOCAL_0x2
newobj     instance void [mscorlib]System.Object::.ctor()
stloc LOCAL_0x3
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc LOCAL_0x4
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc LOCAL_0x5
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc LOCAL_0x6
ldc.i8 0x6b25199d751b104a 
stloc LOCAL_0x7
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc LOCAL_0x8
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc LOCAL_0x9
ldc.r4 float32(0xf957bd30) 
stloc LOCAL_0xa
newobj     instance void [mscorlib]System.Object::.ctor()
stloc LOCAL_0xb
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc LOCAL_0xc
ldc.i4 0xd3008148 
stloc LOCAL_0xd

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0xf176892e 
stsfld unsigned int16 ILGEN_0x6d358988::field_0x0
ldc.i4 0xb1796dcb 
stsfld native int ILGEN_0x6d358988::field_0x1
ldc.r8 float64(0x60f37a9c87981378) 
stsfld float64 ILGEN_0x6d358988::field_0x2
ldc.i8 0x5b83b5fd5ca51324 
stsfld int64 ILGEN_0x6d358988::field_0x3
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0x6d358988::field_0x4
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0x6d358988::field_0x5
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
ldc.i8 0x94f8c387a65ada3e 
stsfld int64 ILGEN_0x6d358988::field_0x7
ldc.r8 float64(0x807adde6e4a8a84) 
stsfld float64 ILGEN_0x6d358988::field_0x8
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x6d358988::field_0x9
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x6d358988::field_0xa
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x6d358988::field_0xb
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x6d358988::field_0xc
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x6d358988::field_0xd

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_a_0:
Start_Orphan_a_1:
        ldsfld int32[] ILGEN_0x6d358988::field_0x5
        sizeof [mscorlib]System.Int16
ldc.i4 1
clt
       ldelema [mscorlib]System.Int32
       sizeof [mscorlib]System.Int32
      stobj [mscorlib]System.Int32
End_Orphan_a_1:
      ldsflda int64 ILGEN_0x6d358988::field_0x7
      ldsflda int64 ILGEN_0x6d358988::field_0x7
     cpobj [mscorlib]System.Int64
End_Orphan_a_0:
Start_Orphan_b_2:
        ldc.i4.1
       starg Arg_0x2
End_Orphan_b_2:
       sizeof [mscorlib]System.UInt64
         ldloc LOCAL_0x7
Start_Orphan_b_3:
        nop
End_Orphan_b_3:
        conv.i
         ldsfld native int ILGEN_0x6d358988::field_0x1
         ldloc LOCAL_0x1
Start_Orphan_b_4:
        nop
End_Orphan_b_4:
        mul
Start_Orphan_b_5:
         ldloc LOCAL_0x6
         sizeof [mscorlib]System.Int32
ldc.i4 1
clt
        ldelema [mscorlib]System.SByte
        ldloc LOCAL_0xd
       stobj [mscorlib]System.SByte
End_Orphan_b_5:
       sub
Start_Orphan_b_6:
        ldsfld int32[] ILGEN_0x6d358988::field_0x5
        sizeof [mscorlib]System.SByte
ldc.i4 1
clt
       ldelema [mscorlib]System.Int32
Start_Orphan_a_7:
       nop
End_Orphan_a_7:
        ldloc LOCAL_0x5
        sizeof [mscorlib]System.Int32
Start_Orphan_b_8:
       nop
End_Orphan_b_8:
ldc.i4 1
clt
       ldelema [mscorlib]System.Int32
      cpobj [mscorlib]System.Int32
End_Orphan_b_6:
      sub
Start_Orphan_a_9:
Start_Orphan_b_a:
       nop
End_Orphan_b_a:
       ldsflda native int ILGEN_0x6d358988::field_0x1
      initobj [mscorlib]System.IntPtr
End_Orphan_a_9:
Start_Orphan_b_b:
Start_Orphan_a_c:
       nop
End_Orphan_a_c:
Start_Orphan_b_d:
       nop
End_Orphan_b_d:
       ldsflda float64 ILGEN_0x6d358988::field_0x8
      pop
End_Orphan_b_b:
      ldloc LOCAL_0xd
     add
Start_Orphan_a_e:
        nop
End_Orphan_a_e:
         ldarg Arg_0x2
         ldloc LOCAL_0x1
Start_Orphan_b_f:
        nop
End_Orphan_b_f:
        sub
         ldsfld int16[] ILGEN_0x6d358988::field_0xb
         sizeof [mscorlib]System.Double
Start_Orphan_b_10:
        nop
End_Orphan_b_10:
ldc.i4 1
clt
        ldelem.i2
       add
Start_Orphan_a_11:
        ldarga Arg_0x0
        ldloca LOCAL_0xa
       cpobj [mscorlib]System.Single
End_Orphan_a_11:
Start_Orphan_a_12:
        nop
End_Orphan_a_12:
         sizeof [mscorlib]System.UInt64
Start_Orphan_b_13:
        nop
End_Orphan_b_13:
        conv.u1
         ldloc LOCAL_0x1
         sizeof [mscorlib]System.Int64
Start_Orphan_b_14:
        nop
End_Orphan_b_14:
        mul
Start_Orphan_b_15:
        ldsfld int16[] ILGEN_0x6d358988::field_0xd
       stsfld int16[] ILGEN_0x6d358988::field_0xd
End_Orphan_b_15:
       add
Start_Orphan_b_16:
       ldsflda native int ILGEN_0x6d358988::field_0x1
        ldloc LOCAL_0x1
        ldc.i4.4
       mul
      unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.IntPtr
End_Orphan_b_16:
      cgt.un
Start_Orphan_a_17:
Start_Orphan_b_18:
       nop
End_Orphan_b_18:
       ldloc LOCAL_0x4
      pop
End_Orphan_a_17:
Start_Orphan_a_19:
        nop
End_Orphan_a_19:
         ldc.i4.8
        conv.i1
       conv.i
Start_Orphan_a_1a:
        sizeof [mscorlib]System.Int16
       starg Arg_0x2
End_Orphan_a_1a:
Start_Orphan_a_1b:
        nop
End_Orphan_a_1b:
         ldsfld int64 ILGEN_0x6d358988::field_0x7
        conv.u2
       conv.i4
Start_Orphan_b_1c:
        ldloca LOCAL_0xa
Start_Orphan_b_1d:
       nop
End_Orphan_b_1d:
       unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Single
      stsfld float64 ILGEN_0x6d358988::field_0x2
End_Orphan_b_1c:
      clt
Start_Orphan_b_1e:
      ldloc LOCAL_0x9
     pop
End_Orphan_b_1e:
     mul
    mul
Start_Orphan_a_1f:
Start_Orphan_b_20:
Start_Orphan_a_21:
      nop
End_Orphan_a_21:
       ldloc LOCAL_0x8
       sizeof [mscorlib]System.SByte
ldc.i4 1
clt
      ldelema [mscorlib]System.UInt32
       ldc.i8 0xe57736fd1f40cca
Start_Orphan_b_22:
      nop
End_Orphan_b_22:
      conv.u4
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt32
End_Orphan_b_20:
     ldarga Arg_0x0
    pop
End_Orphan_a_1f:
Start_Orphan_a_23:
        ldsfld int32[] ILGEN_0x6d358988::field_0x5
        sizeof [mscorlib]System.Double
ldc.i4 1
clt
        sizeof [mscorlib]System.Int64
       stelem.i4
End_Orphan_a_23:
         ldsfld int64 ILGEN_0x6d358988::field_0x7
        conv.i4
       not
      conv.u
Start_Orphan_a_24:
        ldloc LOCAL_0x6
       stsfld int8[] ILGEN_0x6d358988::field_0x4
End_Orphan_a_24:
         ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
         sizeof [mscorlib]System.Int64
ldc.i4 1
clt
        ldelema [mscorlib]System.Byte
       ldind.u1
Start_Orphan_a_25:
        sizeof [mscorlib]System.SByte
       pop
End_Orphan_a_25:
Start_Orphan_a_26:
        nop
End_Orphan_a_26:
         ldloc LOCAL_0x7
         ldsfld int64 ILGEN_0x6d358988::field_0x7
        add
Start_Orphan_b_27:
         ldloc LOCAL_0x4
         sizeof [mscorlib]System.UInt64
ldc.i4 1
clt
        ldelema [mscorlib]System.UInt32
        ldloc LOCAL_0xd
       stind.i4
End_Orphan_b_27:
       conv.u
Start_Orphan_b_28:
Start_Orphan_a_29:
       nop
End_Orphan_a_29:
        ldsfld unsigned int16 ILGEN_0x6d358988::field_0x0
        ldloc LOCAL_0x1
Start_Orphan_b_2a:
       nop
End_Orphan_b_2a:
       mul
      stloc LOCAL_0x1
End_Orphan_b_28:
      mul
Start_Orphan_b_2b:
Start_Orphan_b_2c:
       ldloca LOCAL_0x7
       ldsfld int64 ILGEN_0x6d358988::field_0x7
      stobj [mscorlib]System.UInt64
End_Orphan_b_2c:
      ldloca LOCAL_0x2
     initobj [mscorlib]System.Int16
End_Orphan_b_2b:
     sub
Start_Orphan_b_2d:
Start_Orphan_a_2e:
Start_Orphan_a_2f:
      nop
End_Orphan_a_2f:
      ldloc LOCAL_0x5
     stloc LOCAL_0x5
End_Orphan_a_2e:
Start_Orphan_a_30:
        ldloc LOCAL_0x6
        ldarg Arg_0x2
ldc.i4 1
clt
       ldelema [mscorlib]System.SByte
      pop
End_Orphan_a_30:
        ldc.i4.8
Start_Orphan_b_31:
       nop
End_Orphan_b_31:
       conv.i1
Start_Orphan_a_32:
       nop
End_Orphan_a_32:
        ldsfld int64 ILGEN_0x6d358988::field_0x7
       conv.i4
Start_Orphan_b_33:
       ldloca LOCAL_0x7
       ldsfld int64 ILGEN_0x6d358988::field_0x3
      stobj [mscorlib]System.UInt64
End_Orphan_b_33:
      add
Start_Orphan_b_34:
       sizeof [mscorlib]System.Int16
      conv.u1
 newarr [mscorlib]System.Int16
Start_Orphan_a_35:
      nop
End_Orphan_a_35:
       ldloc LOCAL_0x1
      conv.u2
ldc.i4 1
clt
Start_Orphan_a_36:
      nop
End_Orphan_a_36:
        ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
        ldc.i4.3
       ldelema [mscorlib]System.Byte
Start_Orphan_b_37:
      nop
End_Orphan_b_37:
      ldobj [mscorlib]System.Byte
     stelem.i2
End_Orphan_b_34:
     conv.i
    pop
End_Orphan_b_2d:
    conv.u4
   add
Start_Orphan_a_38:
Start_Orphan_a_39:
Start_Orphan_a_3a:
      ldloca LOCAL_0x7
      ldloc LOCAL_0x7
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt64
End_Orphan_a_3a:
Start_Orphan_b_3b:
      nop
End_Orphan_b_3b:
      ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
Start_Orphan_a_3c:
      nop
End_Orphan_a_3c:
Start_Orphan_b_3d:
      nop
End_Orphan_b_3d:
      ldsfld unsigned int16 ILGEN_0x6d358988::field_0x0
ldc.i4 1
clt
     ldelema [mscorlib]System.Byte
    pop
End_Orphan_a_39:
    ldarga Arg_0x1
Start_Orphan_a_3e:
Start_Orphan_a_3f:
      ldloc LOCAL_0x0
     stsfld float64 ILGEN_0x6d358988::field_0x2
End_Orphan_a_3f:
     sizeof [mscorlib]System.UInt64
    starg Arg_0x1
End_Orphan_a_3e:
Start_Orphan_a_40:
       ldsfld unsigned int64[] ILGEN_0x6d358988::field_0x9
      stsfld unsigned int64[] ILGEN_0x6d358988::field_0x9
End_Orphan_a_40:
        ldloc LOCAL_0x1
        ldc.i4.0
Start_Orphan_b_41:
       nop
End_Orphan_b_41:
       sub
        ldc.i8 0xd449a44cbccba757
Start_Orphan_b_42:
       nop
End_Orphan_b_42:
       conv.u1
      sub
Start_Orphan_a_43:
       ldsflda int64 ILGEN_0x6d358988::field_0x3
      initobj [mscorlib]System.Int64
End_Orphan_a_43:
        ldloc LOCAL_0x1
Start_Orphan_b_44:
       nop
End_Orphan_b_44:
       conv.u8
      conv.i1
Start_Orphan_b_45:
      ldloca LOCAL_0xa
Start_Orphan_a_46:
      nop
End_Orphan_a_46:
       ldloc LOCAL_0x7
Start_Orphan_b_47:
      nop
End_Orphan_b_47:
      conv.r8
     stind.r4
End_Orphan_b_45:
     add
    conv.i2
   stind.i2
End_Orphan_a_38:
Start_Orphan_a_48:
Start_Orphan_a_49:
        ldloc LOCAL_0x4
        ldloc LOCAL_0x2
ldc.i4 1
clt
       ldelema [mscorlib]System.UInt32
       sizeof [mscorlib]System.UInt64
      stind.i4
End_Orphan_a_49:
        ldarg Arg_0x1
Start_Orphan_b_4a:
       nop
End_Orphan_b_4a:
       conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.UInt64
        ldsfld int8[] ILGEN_0x6d358988::field_0x4
       ldlen
ldc.i4 1
clt
      ldelema [mscorlib]System.UInt64
Start_Orphan_a_4b:
        ldloc LOCAL_0x6
        ldc.i4.1
       ldelema [mscorlib]System.SByte
       ldc.i4.2
      stobj [mscorlib]System.SByte
End_Orphan_a_4b:
        ldc.i4.m1
       conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.UInt64
Start_Orphan_a_4c:
       nop
End_Orphan_a_4c:
        ldloc LOCAL_0x7
        ldsfld int64 ILGEN_0x6d358988::field_0x3
Start_Orphan_b_4d:
       nop
End_Orphan_b_4d:
       cgt.un
      ldelema [mscorlib]System.UInt64
     cpobj [mscorlib]System.UInt64
End_Orphan_a_48:
Start_Orphan_b_4e:
         ldloc LOCAL_0x4
         ldc.i4.5
        ldelema [mscorlib]System.UInt32
       initobj [mscorlib]System.UInt32
End_Orphan_b_4e:
       ldarga Arg_0x0
Start_Orphan_b_4f:
       ldarga Arg_0x1
        ldarga Arg_0x1
       ldind.i2
      stobj [mscorlib]System.Int16
End_Orphan_b_4f:
      ldind.r4
Start_Orphan_b_50:
      ldloca LOCAL_0x1
      ldsflda native int ILGEN_0x6d358988::field_0x1
     cpobj [mscorlib]System.IntPtr
End_Orphan_b_50:
     conv.u8
Start_Orphan_b_51:
Start_Orphan_a_52:
Start_Orphan_a_53:
      nop
End_Orphan_a_53:
       ldloc LOCAL_0xc
       ldc.i4.8
Start_Orphan_b_54:
      nop
End_Orphan_b_54:
      ldelema [mscorlib]System.UInt16
       ldarga Arg_0x1
      unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Int16
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt16
End_Orphan_a_52:
Start_Orphan_a_55:
        ldloc LOCAL_0x9
        ldc.i4.4
       ldelema [mscorlib]System.Int32
       ldsfld unsigned int16 ILGEN_0x6d358988::field_0x0
      unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int32
End_Orphan_a_55:
Start_Orphan_b_56:
       nop
End_Orphan_b_56:
       ldc.i4 0x9300114d
      conv.u1
 newarr [mscorlib]System.UInt64
Start_Orphan_a_57:
       ldarga Arg_0x0
       ldarga Arg_0x0
      cpobj [mscorlib]System.Single
End_Orphan_a_57:
        ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
        ldc.i4.3
       ldelema [mscorlib]System.Byte
      ldind.u1
ldc.i4 1
clt
     ldelema [mscorlib]System.UInt64
Start_Orphan_a_58:
Start_Orphan_a_59:
      nop
End_Orphan_a_59:
      ldsflda unsigned int16 ILGEN_0x6d358988::field_0x0
       ldsfld native int ILGEN_0x6d358988::field_0x1
      conv.u4
     stind.i2
End_Orphan_a_58:
Start_Orphan_b_5a:
Start_Orphan_b_5b:
      nop
End_Orphan_b_5b:
      ldc.i4.6
     stsfld unsigned int16 ILGEN_0x6d358988::field_0x0
End_Orphan_b_5a:
     ldloca LOCAL_0x7
    cpobj [mscorlib]System.UInt64
End_Orphan_b_51:
    conv.u
Start_Orphan_a_5c:
        ldloc LOCAL_0x6
        ldc.i4.3
Start_Orphan_b_5d:
       nop
End_Orphan_b_5d:
ldc.i4 1
clt
       ldelema [mscorlib]System.SByte
Start_Orphan_a_5e:
       nop
End_Orphan_a_5e:
        ldloc LOCAL_0xa
Start_Orphan_b_5f:
       nop
End_Orphan_b_5f:
       conv.i4
      stind.i1
End_Orphan_a_5c:
         ldc.i4.3
         ldsfld native int ILGEN_0x6d358988::field_0x1
Start_Orphan_b_60:
        nop
End_Orphan_b_60:
        mul
         ldc.i4.4
         sizeof [mscorlib]System.IntPtr
        shr.un
Start_Orphan_b_61:
        ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
        ldsfld unsigned int16 ILGEN_0x6d358988::field_0x0
ldc.i4 1
clt
        ldc.i4 0x5dfd466d
       stelem.i1
End_Orphan_b_61:
       mul
Start_Orphan_a_62:
         ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
         ldc.i4.8
        ldelema [mscorlib]System.Byte
       pop
End_Orphan_a_62:
         ldloca LOCAL_0x7
Start_Orphan_b_63:
        nop
End_Orphan_b_63:
        ldind.u8
       conv.u4
      mul
Start_Orphan_a_64:
        ldsfld int16[] ILGEN_0x6d358988::field_0xd
       pop
End_Orphan_a_64:
Start_Orphan_a_65:
        nop
End_Orphan_a_65:
         ldsfld float64 ILGEN_0x6d358988::field_0x8
         ldsfld float64 ILGEN_0x6d358988::field_0x2
        clt
         ldsfld native int ILGEN_0x6d358988::field_0x1
        conv.i2
       add
Start_Orphan_a_66:
        ldloca LOCAL_0xa
       pop
End_Orphan_a_66:
         ldloc LOCAL_0x6
         sizeof [mscorlib]System.UInt16
Start_Orphan_b_67:
        nop
End_Orphan_b_67:
ldc.i4 1
clt
        ldelem.i1
Start_Orphan_b_68:
         ldloc LOCAL_0x8
         ldc.i4.8
        ldelema [mscorlib]System.UInt32
        sizeof [mscorlib]System.UInt64
       unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt32
End_Orphan_b_68:
       conv.u2
      add
     mul
Start_Orphan_a_69:
         ldloc LOCAL_0x5
         ldc.i4 0x49a70598
ldc.i4 1
clt
        ldelema [mscorlib]System.Int32
        ldc.i4.1
       stobj [mscorlib]System.Int32
End_Orphan_a_69:
Start_Orphan_a_6a:
        nop
End_Orphan_a_6a:
Start_Orphan_b_6b:
        nop
End_Orphan_b_6b:
        ldc.i4.3
       conv.u2
Start_Orphan_a_6c:
        ldloca LOCAL_0x2
        sizeof [mscorlib]System.Single
       unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int16
End_Orphan_a_6c:
Start_Orphan_a_6d:
        nop
End_Orphan_a_6d:
         ldsfld native int ILGEN_0x6d358988::field_0x1
         sizeof [mscorlib]System.UInt32
Start_Orphan_b_6e:
        nop
End_Orphan_b_6e:
        mul
         ldsfld native int ILGEN_0x6d358988::field_0x1
        conv.u
Start_Orphan_b_6f:
        ldc.r4 float32(0x1c6048f1)
       stloc LOCAL_0x0
End_Orphan_b_6f:
       add
Start_Orphan_b_70:
        ldloca LOCAL_0x0
       ldind.r8
      stloc LOCAL_0x0
End_Orphan_b_70:
      add
Start_Orphan_a_71:
Start_Orphan_a_72:
       nop
End_Orphan_a_72:
       ldsflda int64 ILGEN_0x6d358988::field_0x3
      initobj [mscorlib]System.Int64
End_Orphan_a_71:
       ldarga Arg_0x2
      ldobj [mscorlib]System.Int16
     add
Start_Orphan_b_73:
Start_Orphan_a_74:
Start_Orphan_a_75:
      nop
End_Orphan_a_75:
       ldc.i8 0xc567db3c52053ef2
Start_Orphan_b_76:
      nop
End_Orphan_b_76:
      conv.i
     stsfld native int ILGEN_0x6d358988::field_0x1
End_Orphan_a_74:
     ldloca LOCAL_0xa
Start_Orphan_a_77:
Start_Orphan_a_78:
      nop
End_Orphan_a_78:
       sizeof [mscorlib]System.Byte
      conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.UInt32
       sizeof [mscorlib]System.Int16
      conv.i4
Start_Orphan_a_79:
      nop
End_Orphan_a_79:
ldc.i4 1
clt
       ldloc LOCAL_0x1
       ldsfld native int ILGEN_0x6d358988::field_0x1
      clt.un
     stelem.i4
End_Orphan_a_77:
      ldloca LOCAL_0xa
Start_Orphan_b_7a:
Start_Orphan_a_7b:
      nop
End_Orphan_a_7b:
       ldloc LOCAL_0x8
       ldc.i4 0xa546ba64
Start_Orphan_b_7c:
      nop
End_Orphan_b_7c:
ldc.i4 1
clt
      ldelema [mscorlib]System.UInt32
     initobj [mscorlib]System.UInt32
End_Orphan_b_7a:
     ldobj [mscorlib]System.Single
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Single
End_Orphan_b_73:
    add
Start_Orphan_b_7d:
Start_Orphan_a_7e:
      ldsfld int16[] ILGEN_0x6d358988::field_0xb
Start_Orphan_a_7f:
      nop
End_Orphan_a_7f:
       ldsfld int64 ILGEN_0x6d358988::field_0x7
      conv.u2
ldc.i4 1
clt
     ldelema [mscorlib]System.Int16
Start_Orphan_a_80:
      ldloca LOCAL_0x7
      ldloca LOCAL_0x7
     cpobj [mscorlib]System.UInt64
End_Orphan_a_80:
Start_Orphan_a_81:
      nop
End_Orphan_a_81:
      ldsfld int16[] ILGEN_0x6d358988::field_0xb
       ldc.i4.0
Start_Orphan_b_82:
      nop
End_Orphan_b_82:
      neg
Start_Orphan_b_83:
      ldsflda float64 ILGEN_0x6d358988::field_0x2
     pop
End_Orphan_b_83:
ldc.i4 1
clt
     ldelema [mscorlib]System.Int16
    cpobj [mscorlib]System.Int16
End_Orphan_a_7e:
    ldloc LOCAL_0x8
   stloc LOCAL_0x8
End_Orphan_b_7d:
   add
Start_Orphan_b_84:
Start_Orphan_a_85:
       ldloc LOCAL_0x4
       ldc.i4.1
Start_Orphan_b_86:
      nop
End_Orphan_b_86:
ldc.i4 1
clt
      ldelema [mscorlib]System.UInt32
     pop
End_Orphan_a_85:
Start_Orphan_a_87:
       ldc.i4.s 10
      pop
End_Orphan_a_87:
        sizeof [mscorlib]System.IntPtr
        sizeof [mscorlib]System.SByte
       add
      conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.UInt32
Start_Orphan_a_88:
       nop
End_Orphan_a_88:
       ldloc LOCAL_0xc
      ldlen
ldc.i4 1
clt
     ldelema [mscorlib]System.UInt32
Start_Orphan_b_89:
     ldloca LOCAL_0xd
Start_Orphan_a_8a:
      ldsflda int64 ILGEN_0x6d358988::field_0x7
      ldloc LOCAL_0x7
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int64
End_Orphan_a_8a:
Start_Orphan_a_8b:
      nop
End_Orphan_a_8b:
       ldloc LOCAL_0xc
       ldc.i4.5
      ldelema [mscorlib]System.UInt16
Start_Orphan_b_8c:
      ldloca LOCAL_0x2
      ldarg Arg_0x1
     stobj [mscorlib]System.Int16
End_Orphan_b_8c:
     ldobj [mscorlib]System.UInt16
    stobj [mscorlib]System.UInt16
End_Orphan_b_89:
    ldind.u4
Start_Orphan_b_8d:
Start_Orphan_a_8e:
     ldsfld unsigned int64[] ILGEN_0x6d358988::field_0x9
    stsfld unsigned int64[] ILGEN_0x6d358988::field_0x9
End_Orphan_a_8e:
    ldloca LOCAL_0xa
Start_Orphan_a_8f:
     ldsfld unsigned int16[] ILGEN_0x6d358988::field_0xa
    stloc LOCAL_0xc
End_Orphan_a_8f:
Start_Orphan_b_90:
Start_Orphan_a_91:
     nop
End_Orphan_a_91:
     ldsflda int64 ILGEN_0x6d358988::field_0x7
    initobj [mscorlib]System.Int64
End_Orphan_b_90:
    ldarga Arg_0x0
   cpobj [mscorlib]System.Single
End_Orphan_b_8d:
   conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.Int16
Start_Orphan_a_92:
Start_Orphan_b_93:
     ldsflda unsigned int16 ILGEN_0x6d358988::field_0x0
      ldc.i4 0x7130228c
      sizeof [mscorlib]System.IntPtr
Start_Orphan_b_94:
     nop
End_Orphan_b_94:
     cgt.un
    stind.i2
End_Orphan_b_93:
    ldarga Arg_0x0
Start_Orphan_a_95:
      ldloc LOCAL_0xc
     stloc LOCAL_0xc
End_Orphan_a_95:
Start_Orphan_a_96:
      nop
End_Orphan_a_96:
       ldc.r8 float64(0x105873f3aed2f69c)
      neg
Start_Orphan_b_97:
       ldsfld int32[] ILGEN_0x6d358988::field_0x5
       ldloc LOCAL_0xd
ldc.i4 1
clt
      ldelema [mscorlib]System.Int32
     pop
End_Orphan_b_97:
     call float32 [mscorlib]System.Math::Abs(float32)
    call float64 [mscorlib]System.Math::Sin(float64)
   stind.r4
End_Orphan_a_92:
Start_Orphan_a_98:
Start_Orphan_a_99:
      nop
End_Orphan_a_99:
      ldsflda int64 ILGEN_0x6d358988::field_0x7
Start_Orphan_a_9a:
      nop
End_Orphan_a_9a:
       ldsflda int64 ILGEN_0x6d358988::field_0x7
      ldind.i8
     stobj [mscorlib]System.Int64
End_Orphan_a_98:
Start_Orphan_a_9b:
        ldloc LOCAL_0x6
        ldc.i4.6
       ldelema [mscorlib]System.SByte
      pop
End_Orphan_a_9b:
Start_Orphan_a_9c:
       nop
End_Orphan_a_9c:
Start_Orphan_b_9d:
       nop
End_Orphan_b_9d:
       ldloca LOCAL_0x1
      ldind.i
Start_Orphan_b_9e:
       ldloc LOCAL_0x4
       ldc.i4.m1
ldc.i4 1
clt
      ldelema [mscorlib]System.UInt32
       ldc.i4.3
       ldsfld native int ILGEN_0x6d358988::field_0x1
      cgt
     stind.i4
End_Orphan_b_9e:
     conv.u2
    conv.i8
   conv.i2
Start_Orphan_a_9f:
Start_Orphan_a_a0:
      ldsflda unsigned int16 ILGEN_0x6d358988::field_0x0
      ldloca LOCAL_0xd
     cpobj [mscorlib]System.UInt16
End_Orphan_a_a0:
     ldloca LOCAL_0xa
Start_Orphan_a_a1:
      ldsflda native int ILGEN_0x6d358988::field_0x1
     pop
End_Orphan_a_a1:
Start_Orphan_a_a2:
      nop
End_Orphan_a_a2:
Start_Orphan_b_a3:
      nop
End_Orphan_b_a3:
      ldloca LOCAL_0xa
     ldobj [mscorlib]System.Single
    stobj [mscorlib]System.Single
End_Orphan_a_9f:
Start_Orphan_b_a4:
        ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
        sizeof [mscorlib]System.Double
ldc.i4 1
clt
       ldelema [mscorlib]System.Byte
      initobj [mscorlib]System.Byte
End_Orphan_b_a4:
      ldloc LOCAL_0x1
Start_Orphan_a_a5:
       ldsflda float64 ILGEN_0x6d358988::field_0x2
       ldsfld float64 ILGEN_0x6d358988::field_0x8
      unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Double
End_Orphan_a_a5:
Start_Orphan_a_a6:
       nop
End_Orphan_a_a6:
Start_Orphan_b_a7:
       nop
End_Orphan_b_a7:
       ldsfld unsigned int16[] ILGEN_0x6d358988::field_0xc
Start_Orphan_a_a8:
       nop
End_Orphan_a_a8:
        ldloc LOCAL_0x7
Start_Orphan_b_a9:
       nop
End_Orphan_b_a9:
       conv.u1
Start_Orphan_b_aa:
       ldloca LOCAL_0x3
       ldloc LOCAL_0x3
      stind.ref
End_Orphan_b_aa:
ldc.i4 1
clt
      ldelem.u2
Start_Orphan_b_ab:
     nop
End_Orphan_b_ab:
     sub
    conv.u4
Start_Orphan_a_ac:
Start_Orphan_a_ad:
       ldloc LOCAL_0x8
       sizeof [mscorlib]System.Double
ldc.i4 1
clt
      ldelema [mscorlib]System.UInt32
      sizeof [mscorlib]System.IntPtr
     stind.i4
End_Orphan_a_ad:
       ldsfld int16[] ILGEN_0x6d358988::field_0xd
      ldlen
Start_Orphan_b_ae:
       ldloc LOCAL_0x6
       ldc.i4.0
      ldelema [mscorlib]System.SByte
     pop
End_Orphan_b_ae:
     conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.SByte
       sizeof [mscorlib]System.Int16
Start_Orphan_b_af:
      nop
End_Orphan_b_af:
      conv.u2
       ldarg Arg_0x0
Start_Orphan_b_b0:
      nop
End_Orphan_b_b0:
      conv.u2
Start_Orphan_b_b1:
      ldsflda native int ILGEN_0x6d358988::field_0x1
     initobj [mscorlib]System.IntPtr
End_Orphan_b_b1:
     add
ldc.i4 1
clt
      ldloca LOCAL_0x2
Start_Orphan_b_b2:
      ldloca LOCAL_0x7
      ldc.i8 0x4b4de1824e7239f
     stobj [mscorlib]System.UInt64
End_Orphan_b_b2:
     ldind.i2
    stelem.i1
End_Orphan_a_ac:
Start_Orphan_a_b3:
      ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
       sizeof [mscorlib]System.Int64
       sizeof [mscorlib]System.Int16
      mul
ldc.i4 1
clt
      ldc.i4.3
     stelem.i1
End_Orphan_a_b3:
     ldc.r4 float32(0x1464d35c)
Start_Orphan_a_b4:
      ldloca LOCAL_0xb
     pop
End_Orphan_a_b4:
Start_Orphan_b_b5:
Start_Orphan_a_b6:
      nop
End_Orphan_a_b6:
      ldloc LOCAL_0x6
     pop
End_Orphan_b_b5:
     ldc.r8 float64(0xa6e0ef6f9e80b0a9)
Start_Orphan_b_b7:
       sizeof [mscorlib]System.Double
Start_Orphan_b_b8:
      nop
End_Orphan_b_b8:
      conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.UInt32
Start_Orphan_b_b9:
      nop
End_Orphan_b_b9:
      ldc.i4.8
     ldelema [mscorlib]System.UInt32
Start_Orphan_a_ba:
      nop
End_Orphan_a_ba:
Start_Orphan_b_bb:
      nop
End_Orphan_b_bb:
      ldloc LOCAL_0x4
       ldsfld native int ILGEN_0x6d358988::field_0x1
Start_Orphan_b_bc:
      nop
End_Orphan_b_bc:
      conv.u1
Start_Orphan_b_bd:
      ldsfld int16[] ILGEN_0x6d358988::field_0xd
     stsfld int16[] ILGEN_0x6d358988::field_0xb
End_Orphan_b_bd:
ldc.i4 1
clt
     ldelema [mscorlib]System.UInt32
    cpobj [mscorlib]System.UInt32
End_Orphan_b_b7:
    clt.un
Start_Orphan_b_be:
Start_Orphan_a_bf:
      nop
End_Orphan_a_bf:
       ldsflda int64 ILGEN_0x6d358988::field_0x3
      ldobj [mscorlib]System.Int64
     conv.r4
Start_Orphan_b_c0:
Start_Orphan_a_c1:
     nop
End_Orphan_a_c1:
      ldsfld unsigned int64[] ILGEN_0x6d358988::field_0x9
      sizeof [mscorlib]System.UInt32
Start_Orphan_b_c2:
     nop
End_Orphan_b_c2:
     ldelema [mscorlib]System.UInt64
      ldsfld int64 ILGEN_0x6d358988::field_0x3
      sizeof [mscorlib]System.UInt32
     shr
    stobj [mscorlib]System.UInt64
End_Orphan_b_c0:
    conv.i
   stloc LOCAL_0x1
End_Orphan_b_be:
   and
pop
ldc.i4 1
clt
ldelem.i2
pop
  //stelem.i2
End_Orphan_b_84:
  or
Start_Orphan_a_c3:
     ldloca LOCAL_0xa
Start_Orphan_a_c4:
       ldarga Arg_0x1
       ldloca LOCAL_0x2
      cpobj [mscorlib]System.Int16
End_Orphan_a_c4:
        ldc.i4.4
Start_Orphan_b_c5:
       nop
End_Orphan_b_c5:
       conv.i8
      conv.i8
Start_Orphan_b_c6:
       ldsfld int32[] ILGEN_0x6d358988::field_0x5
       sizeof [mscorlib]System.UInt16
Start_Orphan_b_c7:
      nop
End_Orphan_b_c7:
ldc.i4 1
clt
      ldelema [mscorlib]System.Int32
     pop
End_Orphan_b_c6:
     conv.r4
    stobj [mscorlib]System.Single
End_Orphan_a_c3:
Start_Orphan_a_c8:
Start_Orphan_a_c9:
       ldloc LOCAL_0x0
      stloc LOCAL_0x0
End_Orphan_a_c9:
      ldsflda int64 ILGEN_0x6d358988::field_0x3
Start_Orphan_b_ca:
       ldloca LOCAL_0xd
       ldarg Arg_0x2
      stind.i2
End_Orphan_b_ca:
      ldsflda int64 ILGEN_0x6d358988::field_0x7
     cpobj [mscorlib]System.Int64
End_Orphan_a_c8:
Start_Orphan_a_cb:
Start_Orphan_a_cc:
       nop
End_Orphan_a_cc:
Start_Orphan_b_cd:
       nop
End_Orphan_b_cd:
       ldloca LOCAL_0x2
Start_Orphan_a_ce:
       nop
End_Orphan_a_ce:
         ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
         sizeof [mscorlib]System.UInt32
ldc.i4 1
clt
        ldelema [mscorlib]System.Byte
       unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Byte
      unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int16
End_Orphan_a_cb:
Start_Orphan_b_cf:
Start_Orphan_a_d0:
       nop
End_Orphan_a_d0:
        ldsfld native int ILGEN_0x6d358988::field_0x1
        ldsfld native int ILGEN_0x6d358988::field_0x1
Start_Orphan_b_d1:
       nop
End_Orphan_b_d1:
       sub
      stsfld native int ILGEN_0x6d358988::field_0x1
End_Orphan_b_cf:
      ldsfld int64 ILGEN_0x6d358988::field_0x7
Start_Orphan_a_d2:
        ldsflda int64 ILGEN_0x6d358988::field_0x7
       initobj [mscorlib]System.Int64
End_Orphan_a_d2:
         ldc.i4 0xc11ea9c7
Start_Orphan_b_d3:
        nop
End_Orphan_b_d3:
        conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.UInt64
         ldsfld float64 ILGEN_0x6d358988::field_0x2
        conv.u4
ldc.i4 1
clt
       ldelema [mscorlib]System.UInt64
Start_Orphan_b_d4:
       ldsflda int64 ILGEN_0x6d358988::field_0x3
      initobj [mscorlib]System.Int64
End_Orphan_b_d4:
      unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.UInt64
     add
Start_Orphan_b_d5:
Start_Orphan_a_d6:
       nop
End_Orphan_a_d6:
        ldc.i4.2
        ldloc LOCAL_0xd
Start_Orphan_b_d7:
       nop
End_Orphan_b_d7:
       or
      box [mscorlib]System.Int16
Start_Orphan_b_d8:
      ldarga Arg_0x2
Start_Orphan_a_d9:
      nop
End_Orphan_a_d9:
       ldsfld int64 ILGEN_0x6d358988::field_0x3
      conv.u4
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int16
End_Orphan_b_d8:
     isinst [mscorlib]System.Int32
    stloc LOCAL_0xb
End_Orphan_b_d5:
    conv.i8
Start_Orphan_a_da:
Start_Orphan_a_db:
Start_Orphan_a_dc:
      nop
End_Orphan_a_dc:
      ldarga Arg_0x0
Start_Orphan_a_dd:
      nop
End_Orphan_a_dd:
       ldc.r4 float32(0x53c42325)
       ldc.r8 float64(0x2e3794b8c1d592fb)
Start_Orphan_b_de:
      nop
End_Orphan_b_de:
      mul 
     stobj [mscorlib]System.Single
End_Orphan_a_db:
     ldarga Arg_0x0
Start_Orphan_a_df:
       nop
End_Orphan_a_df:
        sizeof [mscorlib]System.SByte
       conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.SByte
Start_Orphan_b_e0:
       ldloca LOCAL_0x2
      initobj [mscorlib]System.Int16
End_Orphan_b_e0:
      ldlen
Start_Orphan_b_e1:
       ldloc LOCAL_0x4
       ldloc LOCAL_0x2
Start_Orphan_b_e2:
      nop
End_Orphan_b_e2:
ldc.i4 1
clt
      ldelema [mscorlib]System.UInt32
     pop
End_Orphan_b_e1:
     conv.r4
    stind.r4
End_Orphan_a_da:
Start_Orphan_a_e3:
Start_Orphan_b_e4:
       nop
End_Orphan_b_e4:
       ldloc LOCAL_0x8
      pop
End_Orphan_a_e3:
Start_Orphan_a_e5:
        ldsfld int16[] ILGEN_0x6d358988::field_0xb
        ldarg Arg_0x2
ldc.i4 1
clt
        ldarg Arg_0x2
       stelem.i2
End_Orphan_a_e5:
Start_Orphan_a_e6:
        nop
End_Orphan_a_e6:
         ldsfld int64 ILGEN_0x6d358988::field_0x7
         sizeof [mscorlib]System.UInt32
        shl
Start_Orphan_a_e7:
        nop
End_Orphan_a_e7:
         sizeof [mscorlib]System.Int32
Start_Orphan_b_e8:
        nop
End_Orphan_b_e8:
        conv.u8
Start_Orphan_b_e9:
        ldloca LOCAL_0x7
       pop
End_Orphan_b_e9:
       and
Start_Orphan_a_ea:
        ldloca LOCAL_0x7
        ldsfld int64 ILGEN_0x6d358988::field_0x3
       unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt64
End_Orphan_a_ea:
         ldsfld int64 ILGEN_0x6d358988::field_0x3
Start_Orphan_b_eb:
        nop
End_Orphan_b_eb:
        conv.i8
Start_Orphan_a_ec:
        nop
End_Orphan_a_ec:
         ldc.r8 float64(0x85dee312c167afdd)
        conv.i8
       sub
Start_Orphan_b_ed:
        sizeof [mscorlib]System.UInt64
       conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.UInt64
      pop
End_Orphan_b_ed:
      add
Start_Orphan_a_ee:
Start_Orphan_a_ef:
       nop
End_Orphan_a_ef:
Start_Orphan_b_f0:
       nop
End_Orphan_b_f0:
       ldsflda int64 ILGEN_0x6d358988::field_0x7
Start_Orphan_a_f1:
       nop
End_Orphan_a_f1:
        ldloc LOCAL_0x7
        ldloc LOCAL_0x7
       mul
      stind.i8
End_Orphan_a_ee:
Start_Orphan_a_f2:
        ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
        ldsfld unsigned int16 ILGEN_0x6d358988::field_0x0
ldc.i4 1
clt
        ldloc LOCAL_0xd
       stelem.i1
End_Orphan_a_f2:
         ldc.i4 0xab8311b9
        conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.UInt64
Start_Orphan_b_f3:
        nop
End_Orphan_b_f3:
        sizeof [mscorlib]System.Byte
ldc.i4 1
clt
       ldelema [mscorlib]System.UInt64
      ldind.u8
Start_Orphan_b_f4:
Start_Orphan_a_f5:
        ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
        sizeof [mscorlib]System.Single
ldc.i4 1
clt
       ldelema [mscorlib]System.Byte
       ldarg Arg_0x2
      unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Byte
End_Orphan_a_f5:
Start_Orphan_b_f6:
       ldsflda native int ILGEN_0x6d358988::field_0x1
      pop
End_Orphan_b_f6:
      ldloca LOCAL_0xd
Start_Orphan_a_f7:
       ldc.i4.0
      stloc LOCAL_0xd
End_Orphan_a_f7:
      ldsflda unsigned int16 ILGEN_0x6d358988::field_0x0
     cpobj [mscorlib]System.UInt16
End_Orphan_b_f4:
     clt.un
    conv.i8
   add
Start_Orphan_b_f8:
Start_Orphan_a_f9:
Start_Orphan_a_fa:
Start_Orphan_b_fb:
     nop
End_Orphan_b_fb:
     ldarga Arg_0x2
Start_Orphan_a_fc:
     nop
End_Orphan_a_fc:
      ldsfld native int ILGEN_0x6d358988::field_0x1
      ldsfld native int ILGEN_0x6d358988::field_0x1
Start_Orphan_b_fd:
     nop
End_Orphan_b_fd:
     ceq
    unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int16
End_Orphan_a_fa:
Start_Orphan_b_fe:
      nop
End_Orphan_b_fe:
      ldloc LOCAL_0x4
Start_Orphan_a_ff:
      nop
End_Orphan_a_ff:
      ldc.i4.6
     ldelema [mscorlib]System.UInt32
Start_Orphan_b_100:
     ldloc LOCAL_0x3
    stloc LOCAL_0xb
End_Orphan_b_100:
    ldobj [mscorlib]System.UInt32
   stsfld unsigned int16 ILGEN_0x6d358988::field_0x0
End_Orphan_a_f9:
Start_Orphan_a_101:
      ldloca LOCAL_0x0
     initobj [mscorlib]System.Double
End_Orphan_a_101:
Start_Orphan_a_102:
       ldarga Arg_0x1
       ldloca LOCAL_0x2
      cpobj [mscorlib]System.Int16
End_Orphan_a_102:
Start_Orphan_a_103:
       nop
End_Orphan_a_103:
        ldsfld int32[] ILGEN_0x6d358988::field_0x5
        ldc.i4.7
Start_Orphan_b_104:
       nop
End_Orphan_b_104:
ldc.i4 1
clt
       ldelema [mscorlib]System.Int32
      unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Int32
Start_Orphan_a_105:
        ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
        sizeof [mscorlib]System.Int16
ldc.i4 1
clt
       ldelema [mscorlib]System.Byte
      initobj [mscorlib]System.Byte
End_Orphan_a_105:
        ldsfld native int ILGEN_0x6d358988::field_0x1
       conv.u8
Start_Orphan_b_106:
       ldsflda unsigned int16 ILGEN_0x6d358988::field_0x0
      initobj [mscorlib]System.UInt16
End_Orphan_b_106:
      conv.u1
Start_Orphan_b_107:
Start_Orphan_a_108:
      nop
End_Orphan_a_108:
Start_Orphan_b_109:
      nop
End_Orphan_b_109:
      ldloca LOCAL_0x0
       ldloc LOCAL_0xa
      call float64 [mscorlib]System.Math::Abs(float64)
     stind.r8
End_Orphan_b_107:
     sub
Start_Orphan_b_10a:
Start_Orphan_a_10b:
      nop
End_Orphan_a_10b:
Start_Orphan_b_10c:
      nop
End_Orphan_b_10c:
      ldsfld int8[] ILGEN_0x6d358988::field_0x4
       ldsfld native int ILGEN_0x6d358988::field_0x1
      conv.u2
ldc.i4 1
clt
     ldelema [mscorlib]System.SByte
Start_Orphan_a_10d:
      ldloca LOCAL_0xd
      ldsflda unsigned int16 ILGEN_0x6d358988::field_0x0
     cpobj [mscorlib]System.UInt16
End_Orphan_a_10d:
       ldloc LOCAL_0xa
      conv.i1
     conv.u1
    stobj [mscorlib]System.SByte
End_Orphan_b_10a:
    conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.UInt32
Start_Orphan_a_10e:
      ldsfld int16[] ILGEN_0x6d358988::field_0xd
Start_Orphan_a_10f:
      nop
End_Orphan_a_10f:
Start_Orphan_b_110:
      nop
End_Orphan_b_110:
      sizeof [mscorlib]System.UInt64
Start_Orphan_b_111:
      nop
End_Orphan_b_111:
ldc.i4 1
clt
      sizeof [mscorlib]System.UInt64
     stelem.i2
End_Orphan_a_10e:
Start_Orphan_a_112:
       nop
End_Orphan_a_112:
        ldc.i8 0xffc06a931cba3838
        ldsfld int64 ILGEN_0x6d358988::field_0x3
Start_Orphan_b_113:
       nop
End_Orphan_b_113:
       add
Start_Orphan_a_114:
       nop
End_Orphan_a_114:
        ldc.i8 0x745deca52bdc5d3c
       conv.u2
      shl
     conv.u8
    conv.u2
Start_Orphan_b_115:
Start_Orphan_a_116:
     ldloca LOCAL_0x7
      ldloc LOCAL_0x7
      ldloc LOCAL_0x7
     sub
    stind.i8
End_Orphan_a_116:
    ldloc LOCAL_0x5
Start_Orphan_a_117:
      nop
End_Orphan_a_117:
       sizeof [mscorlib]System.UInt64
       sizeof [mscorlib]System.Double
      ceq
Start_Orphan_b_118:
       ldloc LOCAL_0x4
       sizeof [mscorlib]System.UInt32
ldc.i4 1
clt
      ldelema [mscorlib]System.UInt32
     pop
End_Orphan_b_118:
     neg
Start_Orphan_a_119:
      nop
End_Orphan_a_119:
       ldc.r4 float32(0xc3d570ef)
Start_Orphan_b_11a:
      nop
End_Orphan_b_11a:
      conv.i1
     conv.i2
    clt.un
Start_Orphan_a_11b:
      nop
End_Orphan_a_11b:
       sizeof [mscorlib]System.Int16
Start_Orphan_b_11c:
      nop
End_Orphan_b_11c:
      conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.Byte
Start_Orphan_a_11d:
      nop
End_Orphan_a_11d:
       ldc.i8 0x989b4e5c484f9c51
       ldc.i8 0x72103bf43f367a91
Start_Orphan_b_11e:
      nop
End_Orphan_b_11e:
      clt
Start_Orphan_b_11f:
      ldloc LOCAL_0xc
     stloc LOCAL_0xc
End_Orphan_b_11f:
ldc.i4 1
clt
     ldelema [mscorlib]System.Byte
Start_Orphan_b_120:
      ldloc LOCAL_0x6
      sizeof [mscorlib]System.UInt32
ldc.i4 1
clt
     ldelema [mscorlib]System.SByte
Start_Orphan_a_121:
     nop
End_Orphan_a_121:
      ldc.i8 0xb6798e85de38ce11
     conv.u1
    stind.i1
End_Orphan_b_120:
    ldobj [mscorlib]System.Byte
pop
ldc.i4 1
clt
ldelem.i4
pop
   //stelem.i4
End_Orphan_b_115:
ldc.i4 1
clt
   ldelema [mscorlib]System.UInt32
Start_Orphan_a_122:
Start_Orphan_a_123:
      ldloca LOCAL_0x1
     pop
End_Orphan_a_123:
Start_Orphan_b_124:
      nop
End_Orphan_b_124:
      sizeof [mscorlib]System.SByte
Start_Orphan_b_125:
      ldsfld unsigned int16[] ILGEN_0x6d358988::field_0xc
     stsfld unsigned int16[] ILGEN_0x6d358988::field_0xa
End_Orphan_b_125:
     conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.Byte
Start_Orphan_a_126:
      nop
End_Orphan_a_126:
       ldsfld native int ILGEN_0x6d358988::field_0x1
       sizeof [mscorlib]System.Double
      sub
     conv.u4
Start_Orphan_b_127:
      ldloc LOCAL_0x9
      ldc.i4.2
     ldelema [mscorlib]System.Int32
    pop
End_Orphan_b_127:
ldc.i4 1
clt
    ldelema [mscorlib]System.Byte
   pop
End_Orphan_a_122:
   ldc.i4.3
  stobj [mscorlib]System.UInt32
End_Orphan_b_f8:
  conv.u4
Start_Orphan_b_128:
Start_Orphan_a_129:
Start_Orphan_a_12a:
      ldloc LOCAL_0x6
      ldsfld unsigned int16 ILGEN_0x6d358988::field_0x0
ldc.i4 1
clt
     ldelema [mscorlib]System.SByte
      ldloc LOCAL_0x2
      ldsfld native int ILGEN_0x6d358988::field_0x1
 bgt.un Branch_0x0

      ldc.i4.1
 br Branch_0x1

 Branch_0x0:

      ldc.i4.7
 Branch_0x1:

    stobj [mscorlib]System.SByte
End_Orphan_a_12a:
Start_Orphan_b_12b:
      ldc.i4.m1
      ldc.i4.8
 bgt.un Branch_0x2

      ldc.r4 float32(0x204254d0)
 br Branch_0x3

 Branch_0x2:

      ldc.r4 float32(0xb553a8a7)
 Branch_0x3:

    pop
End_Orphan_b_12b:
    ldloca LOCAL_0x3
   pop
End_Orphan_a_129:
    ldarg Arg_0x1
   conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.Int32
Start_Orphan_a_12c:
Start_Orphan_a_12d:
      nop
End_Orphan_a_12d:
       ldc.r4 float32(0xa6c55726)
      call float64 [mscorlib]System.Math::Sqrt(float64)
     call float64 [mscorlib]System.Math::Sqrt(float64)
    stloc LOCAL_0x0
End_Orphan_a_12c:
Start_Orphan_a_12e:
       ldsfld int8[] ILGEN_0x6d358988::field_0x4
       ldloc LOCAL_0x2
Start_Orphan_b_12f:
      nop
End_Orphan_b_12f:
ldc.i4 1
clt
      ldelema [mscorlib]System.SByte
Start_Orphan_a_130:
      nop
End_Orphan_a_130:
Start_Orphan_b_131:
      nop
End_Orphan_b_131:
      sizeof [mscorlib]System.Single
     stind.i1
End_Orphan_a_12e:
Start_Orphan_a_132:
       nop
End_Orphan_a_132:
        sizeof [mscorlib]System.UInt32
       conv.i4
        ldloc LOCAL_0xc
        sizeof [mscorlib]System.UInt32
ldc.i4 1
clt
       ldelem.u2
      add
Start_Orphan_a_133:
       ldsfld unsigned int64[] ILGEN_0x6d358988::field_0x9
       sizeof [mscorlib]System.UInt16
ldc.i4 1
clt
       ldloc LOCAL_0x7
      stelem.i8
End_Orphan_a_133:
Start_Orphan_a_134:
       nop
End_Orphan_a_134:
        ldloc LOCAL_0xa
Start_Orphan_b_135:
       nop
End_Orphan_b_135:
       call float64 [mscorlib]System.Math::Cos(float64)
      conv.u
Start_Orphan_b_136:
Start_Orphan_a_137:
      nop
End_Orphan_a_137:
Start_Orphan_b_138:
      nop
End_Orphan_b_138:
      ldloca LOCAL_0x0
Start_Orphan_a_139:
      nop
End_Orphan_a_139:
Start_Orphan_b_13a:
      nop
End_Orphan_b_13a:
      ldloca LOCAL_0x0
     cpobj [mscorlib]System.Double
End_Orphan_b_136:
     add
    conv.i4
Start_Orphan_b_13b:
Start_Orphan_a_13c:
Start_Orphan_a_13d:
     nop
End_Orphan_a_13d:
Start_Orphan_b_13e:
     nop
End_Orphan_b_13e:
     ldloca LOCAL_0xa
     ldloca LOCAL_0xa
    cpobj [mscorlib]System.Single
End_Orphan_a_13c:
    ldloca LOCAL_0x3
Start_Orphan_a_13f:
      ldloc LOCAL_0x4
      ldarg Arg_0x1
Start_Orphan_b_140:
     nop
End_Orphan_b_140:
ldc.i4 1
clt
     ldelema [mscorlib]System.UInt32
      ldc.i4.m1
      ldc.i4.0
     add
    stind.i4
End_Orphan_a_13f:
Start_Orphan_a_141:
 ldc.i4 4 
 Loop_0x0: //WHAT
      ldc.i4.7
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x0
 pop
 Loop_0x1: // End of WHAT
End_Orphan_a_141:
Start_Orphan_a_142:
      nop
End_Orphan_a_142:
       ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
       sizeof [mscorlib]System.UInt64
ldc.i4 1
clt
      ldelema [mscorlib]System.Byte
Start_Orphan_b_143:
      ldloca LOCAL_0xb
      ldnull
     stind.ref
End_Orphan_b_143:
     ldind.u1
Start_Orphan_b_144:
Start_Orphan_a_145:
     nop
End_Orphan_a_145:
      sizeof [mscorlib]System.Single
Start_Orphan_b_146:
     nop
End_Orphan_b_146:
     conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.UInt64
    stsfld unsigned int64[] ILGEN_0x6d358988::field_0x9
End_Orphan_b_144:
    box [mscorlib]System.Int32
   stind.ref
End_Orphan_b_13b:
   conv.u1
ldc.i4 1
clt
  ldelema [mscorlib]System.Int32
        ldloc LOCAL_0xa
       call float64 [mscorlib]System.Math::Round(float64)
Start_Orphan_b_147:
        ldsfld int8[] ILGEN_0x6d358988::field_0x4
        ldc.i4.1
       ldelema [mscorlib]System.SByte
       ldc.i4.3
      stind.i1
End_Orphan_b_147:
      conv.i4
Start_Orphan_a_148:
       nop
End_Orphan_a_148:
        ldloc LOCAL_0x7
        ldsfld int64 ILGEN_0x6d358988::field_0x7
       mul
Start_Orphan_b_149:
       ldc.i4.s 30
      stloc LOCAL_0xd
End_Orphan_b_149:
      conv.u1
     clt
    conv.u1
ldc.i4 255
add
 newarr [mscorlib]System.Byte
Start_Orphan_a_14a:
       ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
       sizeof [mscorlib]System.Double
ldc.i4 1
clt
       ldc.i4.1
      stelem.i1
End_Orphan_a_14a:
Start_Orphan_a_14b:
       nop
End_Orphan_a_14b:
Start_Orphan_b_14c:
       nop
End_Orphan_b_14c:
       ldloca LOCAL_0xa
      unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Single
Start_Orphan_b_14d:
Start_Orphan_a_14e:
      nop
End_Orphan_a_14e:
       ldsfld unsigned int8[] ILGEN_0x6d358988::field_0x6
       ldc.i4.m1
ldc.i4 1
clt
      ldelema [mscorlib]System.Byte
       ldloc LOCAL_0x7
       ldloc LOCAL_0x7
      clt.un
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Byte
End_Orphan_b_14d:
     call float64 [mscorlib]System.Math::Abs(float64)
Start_Orphan_b_14f:
Start_Orphan_b_150:
      ldnull
     stloc LOCAL_0x3
End_Orphan_b_150:
     ldsflda int64 ILGEN_0x6d358988::field_0x3
Start_Orphan_a_151:
      nop
End_Orphan_a_151:
       ldsfld float64 ILGEN_0x6d358988::field_0x8
Start_Orphan_b_152:
      nop
End_Orphan_b_152:
      call float64 [mscorlib]System.Math::Abs(float64)
Start_Orphan_b_153:
      ldsflda int64 ILGEN_0x6d358988::field_0x3
      ldloc LOCAL_0x7
     unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int64
End_Orphan_b_153:
     conv.u8
    stind.i8
End_Orphan_b_14f:
    conv.i2
ldc.i4 1
clt
   ldelema [mscorlib]System.Byte
  unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Byte
 stobj [mscorlib]System.Int32
End_Orphan_b_128:
 add
Start_Orphan_b_154:
 ldloc LOCAL_0xc
pop
End_Orphan_b_154:
conv.u2
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	7
ldc.r4 float32(0xda33b895) 
ldc.i4 0x4358b957 
ldc.i4 0xfb2329c8 

 call unsigned int8  ILGEN_0x6d358988::Method_0xee788a65(float32 Arg_0x0, int16 Arg_0x1, int16 Arg_0x2)
conv.i4
ldc.i4 153
sub
 ret
}

}
