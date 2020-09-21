// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b79250 {}
.class ILGEN_0xdea951c0 {

//BEGIN STATIC FIELDS
.field static	float32 field_0x0
.field static	unsigned int64[] field_0x1
.field static	int32 field_0x2
.field static	int8[] field_0x3
//END STATIC FIELDS
.method static float64  Method_0x5a7bd7a1(unsigned int64 Arg_0x0, int16 Arg_0x1, int8 Arg_0x2, unsigned int8 Arg_0x3, native int Arg_0x4, native int Arg_0x5, unsigned int16 Arg_0x6, native int Arg_0x7, int32 Arg_0x8) {
.maxstack	31

 .locals (native int LOCAL_0x0,float64 LOCAL_0x1,float32[] LOCAL_0x2)
//BEGIN INIT LOCALS
ldc.i4 0x75811f64 
stloc LOCAL_0x0
ldc.r8 float64(0xfd87276a3bb66d64) 
stloc LOCAL_0x1
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc LOCAL_0x2

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.r4 float32(0x8c9e0b89) 
stsfld float32 ILGEN_0xdea951c0::field_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
ldc.i4 0x8c6c2653 
stsfld int32 ILGEN_0xdea951c0::field_0x2
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0xdea951c0::field_0x3

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
Start_Orphan_3:
 ldc.i4 10 
 Loop_0x0: //WHAT
Start_Orphan_4:
Start_Orphan_5:
         ldarga Arg_0x1
        pop
End_Orphan_5:
Start_Orphan_6:
         nop
End_Orphan_6:
          ldsfld int32 ILGEN_0xdea951c0::field_0x2
          ldc.i4.8
         mul
          ldarg Arg_0x5
          ldarg Arg_0x5
         clt.un
        div.un
       pop
End_Orphan_4:
         ldarg Arg_0x6
           ldc.i4.7
           ldarg Arg_0x7
          mul.ovf.un
           ldloc LOCAL_0x0
          conv.i2
         sub.ovf
        div
Start_Orphan_7:
          ldc.i8 0x7336b13fb3992af9
         neg
        starg Arg_0x0
End_Orphan_7:
Start_Orphan_8:
          ldarga Arg_0x8
          ldarg Arg_0x1
         stind.i4
End_Orphan_8:
           ldloc LOCAL_0x0
          conv.ovf.i1.un
Start_Orphan_9:
          nop
End_Orphan_9:
           ldloc LOCAL_0x0
           ldc.i4.2
          add.ovf
         rem.un
Start_Orphan_a:
          nop
End_Orphan_a:
           ldsfld int8[] ILGEN_0xdea951c0::field_0x3
          ldlen
Start_Orphan_b:
          nop
End_Orphan_b:
           ldc.i8 0xf364ec0f9dcc7ee
           ldarg Arg_0x0
          ceq
         or
        clt.un
       clt
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x0
 pop
 Loop_0x1: // End of WHAT
End_Orphan_3:
Start_Orphan_c:
Start_Orphan_d:
Start_Orphan_e:
         nop
End_Orphan_e:
         ldarga Arg_0x0
Start_Orphan_f:
         nop
End_Orphan_f:
          ldarg Arg_0x4
         conv.ovf.u8
        stind.i8
End_Orphan_d:
        ldarga Arg_0x1
Start_Orphan_10:
          ldsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
          ldc.i4.0
          ldarg Arg_0x0
         stelem.i8
End_Orphan_10:
         ldarga Arg_0x3
        ldind.u1
       stind.i2
End_Orphan_c:
Start_Orphan_11:
Start_Orphan_12:
          nop
End_Orphan_12:
          ldsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
           ldc.r4 float32(0x3747570)
          conv.u1
         ldelema [mscorlib]System.UInt64
        pop
End_Orphan_11:
Start_Orphan_13:
           ldarga Arg_0x6
          ldind.u2
         starg Arg_0x6
End_Orphan_13:
Start_Orphan_14:
           ldsflda float32 ILGEN_0xdea951c0::field_0x0
           ldsfld float32 ILGEN_0xdea951c0::field_0x0
          stind.r4
End_Orphan_14:
          ldsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
            ldarg Arg_0x0
            ldc.i4.7
           shr.un
          conv.i2
         ldelem.u8
        conv.ovf.u8
       conv.i1
      conv.ovf.u8
     conv.ovf.i2
Start_Orphan_15:
        ldsfld int8[] ILGEN_0xdea951c0::field_0x3
       pop
End_Orphan_15:
Start_Orphan_16:
           ldarga Arg_0x3
          pop
End_Orphan_16:
Start_Orphan_17:
           nop
End_Orphan_17:
            ldarg Arg_0x7
           conv.ovf.u8.un
          conv.ovf.i4.un
Start_Orphan_18:
           ldarga Arg_0x2
          pop
End_Orphan_18:
Start_Orphan_19:
           nop
End_Orphan_19:
            ldc.i4.2
           conv.ovf.i
            ldloc LOCAL_0x0
           conv.i
          add
         or
        conv.ovf.i2.un
Start_Orphan_1a:
Start_Orphan_1b:
          nop
End_Orphan_1b:
           ldarg Arg_0x0
           ldarg Arg_0x0
          cgt
         pop
End_Orphan_1a:
Start_Orphan_1c:
           ldc.i8 0x42f57717fab3298c
          starg Arg_0x0
End_Orphan_1c:
Start_Orphan_1d:
           nop
End_Orphan_1d:
           ldc.i4.4
            ldarg Arg_0x7
            ldc.i4.7
           mul
          div
Start_Orphan_1e:
           ldarg Arg_0x1
          stsfld int32 ILGEN_0xdea951c0::field_0x2
End_Orphan_1e:
Start_Orphan_1f:
           nop
End_Orphan_1f:
            ldc.i4.2
           conv.r.un
          conv.u
         clt
Start_Orphan_20:
           ldc.i4.8
          conv.u1
 newarr [mscorlib]System.UInt64
         pop
End_Orphan_20:
Start_Orphan_21:
           ldarga Arg_0x6
          pop
End_Orphan_21:
Start_Orphan_22:
           nop
End_Orphan_22:
            ldloc LOCAL_0x2
            ldc.i4.5
           ldelema [mscorlib]System.Single
          ldind.r4
         conv.i
        div
       cgt.un
Start_Orphan_23:
Start_Orphan_24:
Start_Orphan_25:
         nop
End_Orphan_25:
         ldarg Arg_0x2
        starg Arg_0x3
End_Orphan_24:
Start_Orphan_26:
          ldsflda float32 ILGEN_0xdea951c0::field_0x0
          ldc.r8 float64(0x53945962b5ae8ae1)
         stind.r4
End_Orphan_26:
           ldc.i4.8
           ldloc LOCAL_0x0
          ceq
         conv.ovf.u8.un
        conv.u1
       stsfld int32 ILGEN_0xdea951c0::field_0x2
End_Orphan_23:
Start_Orphan_27:
Start_Orphan_28:
          nop
End_Orphan_28:
           ldloc LOCAL_0x0
          conv.r8
         conv.i2
        starg Arg_0x6
End_Orphan_27:
Start_Orphan_29:
           nop
End_Orphan_29:
            ldloc LOCAL_0x0
           conv.r8
            ldloc LOCAL_0x1
            ldsfld float32 ILGEN_0xdea951c0::field_0x0
           rem 
          rem 
         conv.i8
        conv.u1
       conv.ovf.u1.un
      shr.un
     conv.u
 beq Branch_0x8

      ldarga Arg_0x2
     pop
 br Branch_0x9

 Branch_0x8:

Start_Orphan_2a:
Start_Orphan_2b:
          ldloc LOCAL_0x0
         stloc LOCAL_0x0
End_Orphan_2b:
Start_Orphan_2c:
          nop
End_Orphan_2c:
           ldc.i4.0
          conv.u1
 newarr [mscorlib]System.UInt64
Start_Orphan_2d:
          nop
End_Orphan_2d:
           ldarg Arg_0x0
          conv.i2
         ldelema [mscorlib]System.UInt64
        pop
End_Orphan_2a:
Start_Orphan_2e:
           nop
End_Orphan_2e:
            ldarg Arg_0x0
           conv.r8
          call float64 [mscorlib]System.Math::Round(float64)
         conv.u1
        conv.u1
 newarr [mscorlib]System.SByte
Start_Orphan_2f:
         ldarga Arg_0x6
Start_Orphan_30:
          ldc.r4 float32(0x2b1c1c1c)
         pop
End_Orphan_30:
Start_Orphan_31:
          nop
End_Orphan_31:
           ldc.r4 float32(0x81ed68d7)
          call float64 [mscorlib]System.Math::Sqrt(float64)
           ldloc LOCAL_0x1
           ldloc LOCAL_0x1
          mul 
         ceq
        stind.i2
End_Orphan_2f:
Start_Orphan_32:
          ldarga Arg_0x6
         pop
End_Orphan_32:
Start_Orphan_33:
           nop
End_Orphan_33:
            ldc.i8 0x5478abe7c2987dd8
           conv.ovf.i2
            ldarg Arg_0x5
           neg
          cgt
         conv.i8
        conv.u4
       ldelema [mscorlib]System.SByte
      ldind.i1
     stsfld int32 ILGEN_0xdea951c0::field_0x2
 Branch_0x9:

End_Orphan_2:
Start_Orphan_34:
      ldloca LOCAL_0x0
Start_Orphan_35:
Start_Orphan_36:
         ldarga Arg_0x0
        pop
End_Orphan_36:
        ldloca LOCAL_0x1
       ldind.r8
      stloc LOCAL_0x1
End_Orphan_35:
Start_Orphan_37:
        ldloc LOCAL_0x2
       stloc LOCAL_0x2
End_Orphan_37:
Start_Orphan_38:
           nop
End_Orphan_38:
            ldarg Arg_0x2
            ldc.i4.3
           and
            ldloc LOCAL_0x0
            ldc.i4.2
           sub.ovf
          mul.ovf
         conv.ovf.u2.un
        conv.ovf.i8.un
Start_Orphan_39:
Start_Orphan_3a:
          newobj     instance void [mscorlib]System.Object::.ctor()
         pop
End_Orphan_3a:
         ldsflda int32 ILGEN_0xdea951c0::field_0x2
Start_Orphan_3b:
          ldarga Arg_0x6
         pop
End_Orphan_3b:
           ldc.i8 0x6bbca8078d24d303
           ldc.i8 0x1a8a9723ab9c9c79
          mul
         conv.ovf.u1
        stind.i
End_Orphan_39:
Start_Orphan_3c:
           ldc.i4.6
          starg Arg_0x3
End_Orphan_3c:
            ldarg Arg_0x0
            ldarg Arg_0x2
           shl
Start_Orphan_3d:
           nop
End_Orphan_3d:
            ldarg Arg_0x4
           conv.u8
          rem.un
Start_Orphan_3e:
           nop
End_Orphan_3e:
            ldc.i8 0x4a4d4089be6dc2ae
           conv.ovf.i8
          conv.i8
         and
        conv.ovf.i8
       ceq
Start_Orphan_3f:
Start_Orphan_40:
Start_Orphan_41:
         nop
End_Orphan_41:
          ldarg Arg_0x0
         not
        pop
End_Orphan_40:
        ldarga Arg_0x8
        ldc.i4.2
       stind.i4
End_Orphan_3f:
Start_Orphan_42:
Start_Orphan_43:
          nop
End_Orphan_43:
           ldsfld int8[] ILGEN_0xdea951c0::field_0x3
           ldarg Arg_0x2
          ldelema [mscorlib]System.SByte
Start_Orphan_44:
          nop
End_Orphan_44:
           ldc.r8 float64(0x955bd138a22b7f9)
          conv.ovf.u2
         stind.i1
End_Orphan_42:
          ldc.i4 0x1ede3a7e
Start_Orphan_45:
           nop
End_Orphan_45:
            ldloc LOCAL_0x0
           conv.ovf.i8
          conv.ovf.u1
         sub
         ldc.i4.8
        rem
Start_Orphan_46:
Start_Orphan_47:
          nop
End_Orphan_47:
          ldarga Arg_0x0
           ldc.i4.2
          conv.ovf.i8.un
         stind.i8
End_Orphan_46:
Start_Orphan_48:
           ldarga Arg_0x5
           ldarg Arg_0x5
          stind.i
End_Orphan_48:
Start_Orphan_49:
           nop
End_Orphan_49:
            ldarg Arg_0x1
            ldsfld int32 ILGEN_0xdea951c0::field_0x2
           mul.ovf
          conv.u1
 newarr [mscorlib]System.SByte
         ldlen
Start_Orphan_4a:
Start_Orphan_4b:
          nop
End_Orphan_4b:
          ldarga Arg_0x2
         pop
End_Orphan_4a:
Start_Orphan_4c:
           ldsflda int32 ILGEN_0xdea951c0::field_0x2
           ldc.i4 0x6b643a63
          stind.i
End_Orphan_4c:
Start_Orphan_4d:
           nop
End_Orphan_4d:
            ldloc LOCAL_0x0
            ldarg Arg_0x5
           or
            ldloc LOCAL_0x0
            ldloc LOCAL_0x0
           sub
          rem
Start_Orphan_4e:
           ldloc LOCAL_0x2
          stloc LOCAL_0x2
End_Orphan_4e:
Start_Orphan_4f:
           nop
End_Orphan_4f:
            ldc.i4.2
            ldloc LOCAL_0x0
           mul
          conv.u4
         mul
        rem
       mul
      rem.un
     stind.i
End_Orphan_34:
     ldloca LOCAL_0x0
    ldind.i
Start_Orphan_50:
Start_Orphan_51:
Start_Orphan_52:
Start_Orphan_53:
         ldloc LOCAL_0x0
        conv.u8
       pop
End_Orphan_53:
          ldarg Arg_0x0
          ldc.i8 0x3bef0a08b3ae395
         rem.un
        conv.r.un
       conv.i8
      pop
End_Orphan_52:
Start_Orphan_54:
Start_Orphan_55:
         ldarga Arg_0x6
        pop
End_Orphan_55:
        ldarga Arg_0x1
       pop
End_Orphan_54:
Start_Orphan_56:
         ldarga Arg_0x3
Start_Orphan_57:
         nop
End_Orphan_57:
          ldloc LOCAL_0x0
         conv.ovf.u1.un
        stind.i1
End_Orphan_56:
Start_Orphan_58:
          nop
End_Orphan_58:
           ldarg Arg_0x4
          conv.ovf.i2
         conv.ovf.u1
        conv.r.un
        ldloc LOCAL_0x1
       cgt.un
      conv.u4
     starg Arg_0x3
End_Orphan_51:
     ldarga Arg_0x1
Start_Orphan_59:
Start_Orphan_5a:
         ldc.r8 float64(0x19b8a9cf29b70c0f)
        stloc LOCAL_0x1
End_Orphan_5a:
Start_Orphan_5b:
          ldarga Arg_0x2
          ldc.i4.m1
         stind.i1
End_Orphan_5b:
Start_Orphan_5c:
          nop
End_Orphan_5c:
           ldarg Arg_0x5
          conv.r8
         call float32 [mscorlib]System.Math::Abs(float32)
        conv.u4
Start_Orphan_5d:
          nop
End_Orphan_5d:
           ldarg Arg_0x4
          conv.ovf.i2.un
         not
        conv.ovf.u2.un
       shr
Start_Orphan_5e:
Start_Orphan_5f:
         ldarga Arg_0x8
         ldc.i4.1
        stind.i
End_Orphan_5f:
        ldsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
       pop
End_Orphan_5e:
Start_Orphan_60:
Start_Orphan_61:
         nop
End_Orphan_61:
         ldarga Arg_0x6
          ldarg Arg_0x0
         conv.ovf.u1
        stind.i2
End_Orphan_60:
Start_Orphan_62:
          ldarg Arg_0x3
         starg Arg_0x6
End_Orphan_62:
Start_Orphan_63:
          nop
End_Orphan_63:
           ldarg Arg_0x0
          conv.i4
           ldloc LOCAL_0x0
          neg
         rem
Start_Orphan_64:
          ldsfld int8[] ILGEN_0xdea951c0::field_0x3
          ldarg Arg_0x3
          ldc.i4.6
         stelem.i1
End_Orphan_64:
           ldsfld float32 ILGEN_0xdea951c0::field_0x0
          conv.u8
Start_Orphan_65:
          nop
End_Orphan_65:
           ldc.i8 0x72bcad4db58e82c7
           ldarg Arg_0x0
          and
         clt
        ceq
Start_Orphan_66:
Start_Orphan_67:
         nop
End_Orphan_67:
          ldc.i4 0x53bf602a
         conv.u1
 newarr [mscorlib]System.UInt64
        stsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
End_Orphan_66:
Start_Orphan_68:
          nop
End_Orphan_68:
           ldloca LOCAL_0x0
          ldind.i
Start_Orphan_69:
          nop
End_Orphan_69:
           ldc.i4.5
           ldc.i4.7
          rem.un
         mul
Start_Orphan_6a:
          ldloc LOCAL_0x2
          ldc.i4.3
          ldloc LOCAL_0x1
         stelem.r4
End_Orphan_6a:
Start_Orphan_6b:
          nop
End_Orphan_6b:
           ldarg Arg_0x0
          conv.ovf.u2.un
         conv.ovf.u2.un
        mul
       sub.ovf
      div
     starg Arg_0x4
End_Orphan_59:
Start_Orphan_6c:
Start_Orphan_6d:
         newobj     instance void [mscorlib]System.Object::.ctor()
        pop
End_Orphan_6d:
        ldsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
Start_Orphan_6e:
          ldarg Arg_0x1
         pop
End_Orphan_6e:
           ldarg Arg_0x7
           ldloc LOCAL_0x0
          add.ovf
         not
Start_Orphan_6f:
          ldsfld int8[] ILGEN_0xdea951c0::field_0x3
         stsfld int8[] ILGEN_0xdea951c0::field_0x3
End_Orphan_6f:
Start_Orphan_70:
          nop
End_Orphan_70:
           ldc.r4 float32(0xf69cd09)
          conv.ovf.i
           ldarg Arg_0x2
          conv.ovf.u
         sub
        clt
           ldc.i8 0x91451800ddefcd1b
           ldarg Arg_0x0
          sub.ovf.un
           ldc.i8 0xedd7ca7b84db79c6
           ldarg Arg_0x2
          shl
         mul
           ldc.r4 float32(0xbdb49dea)
          conv.u8
Start_Orphan_71:
          nop
End_Orphan_71:
          ldarg Arg_0x0
         sub
        add.ovf
       stelem.i8
End_Orphan_6c:
       ldarg Arg_0x0
      conv.i8
     conv.ovf.u1.un
    stind.i2
End_Orphan_50:
Start_Orphan_72:
Start_Orphan_73:
        ldarga Arg_0x6
       pop
End_Orphan_73:
         ldloc LOCAL_0x1
        conv.r8
       call float64 [mscorlib]System.Math::Sqrt(float64)
      stloc LOCAL_0x1
End_Orphan_72:
      ldsflda int32 ILGEN_0xdea951c0::field_0x2
     ldind.i4
    conv.ovf.u
 bne.un Branch_0x6

Start_Orphan_74:
Start_Orphan_75:
       ldloca LOCAL_0x1
Start_Orphan_76:
          nop
End_Orphan_76:
           ldc.i4.s 67
          conv.ovf.i8.un
         conv.ovf.u8.un
        conv.ovf.u8
       conv.r4
      stind.r8
End_Orphan_75:
Start_Orphan_77:
Start_Orphan_78:
          ldarg Arg_0x2
         conv.u1
 newarr [mscorlib]System.SByte
          ldarg Arg_0x4
         conv.ovf.i1
Start_Orphan_79:
         nop
End_Orphan_79:
          ldsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
         ldlen
        stelem.i1
End_Orphan_78:
Start_Orphan_7a:
          ldsflda float32 ILGEN_0xdea951c0::field_0x0
          ldsfld float32 ILGEN_0xdea951c0::field_0x0
         stind.r4
End_Orphan_7a:
Start_Orphan_7b:
          nop
End_Orphan_7b:
           ldloc LOCAL_0x0
          conv.ovf.u2.un
         conv.u1
 newarr [mscorlib]System.Single
Start_Orphan_7c:
          ldc.i4.s -126
         pop
End_Orphan_7c:
Start_Orphan_7d:
          nop
End_Orphan_7d:
           ldc.i8 0xff0bb190e149f7e4
          conv.ovf.i1
          ldarg Arg_0x2
         clt
        ldelema [mscorlib]System.Single
Start_Orphan_7e:
          nop
End_Orphan_7e:
           ldarg Arg_0x7
          conv.r4
Start_Orphan_7f:
          nop
End_Orphan_7f:
           ldsfld float32 ILGEN_0xdea951c0::field_0x0
           ldc.r4 float32(0xdc307100)
          div 
         clt.un
        conv.r8
       stind.r4
End_Orphan_77:
       ldloc LOCAL_0x2
Start_Orphan_80:
Start_Orphan_81:
          ldc.i4.5
         conv.ovf.u4.un
        starg Arg_0x8
End_Orphan_81:
        ldloc LOCAL_0x2
       pop
End_Orphan_80:
Start_Orphan_82:
           ldc.i8 0x427d176f17a0455f
          conv.u1
         starg Arg_0x2
End_Orphan_82:
Start_Orphan_83:
           nop
End_Orphan_83:
            ldloc LOCAL_0x0
           conv.i8
            ldc.i8 0xd45c765c97f60ce6
            ldarg Arg_0x0
           clt
          shr
         conv.ovf.u4
        conv.u1
 newarr [mscorlib]System.SByte
Start_Orphan_84:
           ldc.i4.s -9
          conv.u1
 newarr [mscorlib]System.Single
         pop
End_Orphan_84:
Start_Orphan_85:
 ldc.i4 4 
 Loop_0x2: //WHAT
           ldc.i4.0
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x2
 pop
 Loop_0x3: // End of WHAT
End_Orphan_85:
            ldc.i4.2
           conv.r.un
            ldloc LOCAL_0x1
            ldsfld float32 ILGEN_0xdea951c0::field_0x0
           add 
          add 
         call float64 [mscorlib]System.Math::Round(float64)
        conv.u4
       ldelem.i1
      ldelema [mscorlib]System.Single
Start_Orphan_86:
Start_Orphan_87:
        ldsfld int8[] ILGEN_0xdea951c0::field_0x3
       pop
End_Orphan_87:
Start_Orphan_88:
          ldc.i8 0xb3e64a95a69c89bd
         starg Arg_0x0
End_Orphan_88:
           ldarg Arg_0x7
          conv.ovf.u.un
         conv.ovf.i4.un
        conv.i2
       conv.u1
 newarr [mscorlib]System.SByte
Start_Orphan_89:
          nop
End_Orphan_89:
           ldarg Arg_0x0
           ldc.i4 0x51c2cd93
          shl
Start_Orphan_8a:
          nop
End_Orphan_8a:
           ldarg Arg_0x1
          conv.i8
         sub.ovf.un
        conv.r4
       conv.u2
       ldc.i4.3
      stelem.i1
End_Orphan_86:
         ldc.r8 float64(0x4642d00b885eac4f)
        call float64 [mscorlib]System.Math::Sin(float64)
       conv.ovf.i4
      conv.r.un
     stind.r4
End_Orphan_74:
Start_Orphan_8b:
        ldsflda float32 ILGEN_0xdea951c0::field_0x0
       ldind.r4
      stloc LOCAL_0x1
End_Orphan_8b:
      ldarg Arg_0x8
     conv.u1
 newarr [mscorlib]System.Single
    pop
 br Branch_0x7

 Branch_0x6:

Start_Orphan_8c:
Start_Orphan_8d:
         ldsflda int32 ILGEN_0xdea951c0::field_0x2
Start_Orphan_8e:
          ldarg Arg_0x7
         starg Arg_0x5
End_Orphan_8e:
Start_Orphan_8f:
          nop
End_Orphan_8f:
           ldloc LOCAL_0x0
          conv.ovf.i2.un
Start_Orphan_90:
          nop
End_Orphan_90:
           ldloc LOCAL_0x1
          conv.u
         clt.un
        stind.i4
End_Orphan_8d:
Start_Orphan_91:
Start_Orphan_92:
          nop
End_Orphan_92:
          ldsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
Start_Orphan_93:
          nop
End_Orphan_93:
          ldc.i4.3
Start_Orphan_94:
          nop
End_Orphan_94:
          ldc.i8 0xe780624f086a53e
         stelem.i8
End_Orphan_91:
Start_Orphan_95:
           newobj     instance void [mscorlib]System.Object::.ctor()
          pop
End_Orphan_95:
          ldarg Arg_0x5
Start_Orphan_96:
           ldsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
          pop
End_Orphan_96:
Start_Orphan_97:
           nop
End_Orphan_97:
            ldarg Arg_0x0
            ldarg Arg_0x0
           or
          conv.ovf.u.un
         clt.un
Start_Orphan_98:
          ldloc LOCAL_0x2
           ldarg Arg_0x8
           ldc.i4 0x1046c630
          ceq
           ldsfld float32 ILGEN_0xdea951c0::field_0x0
           ldloc LOCAL_0x1
          sub  
         stelem.r4
End_Orphan_98:
         ldc.i4.8
        div
       conv.r4
      stsfld float32 ILGEN_0xdea951c0::field_0x0
End_Orphan_8c:
      ldloc LOCAL_0x2
Start_Orphan_99:
       ldarga Arg_0x3
Start_Orphan_9a:
           ldloc LOCAL_0x0
          conv.ovf.u1.un
         conv.u1
 newarr [mscorlib]System.UInt64
        pop
End_Orphan_9a:
Start_Orphan_9b:
          ldsflda int32 ILGEN_0xdea951c0::field_0x2
         pop
End_Orphan_9b:
            ldc.i8 0x902b36adc9301c2a
           conv.ovf.i2.un
Start_Orphan_9c:
           nop
End_Orphan_9c:
            ldc.i8 0xb5a239be76a2f6bf
           conv.ovf.u2
          sub
         conv.i8
Start_Orphan_9d:
Start_Orphan_9e:
          nop
End_Orphan_9e:
          ldsflda int32 ILGEN_0xdea951c0::field_0x2
Start_Orphan_9f:
          nop
End_Orphan_9f:
           ldarg Arg_0x0
          conv.i1
         stind.i
End_Orphan_9d:
            ldarg Arg_0x1
           conv.u1
 newarr [mscorlib]System.UInt64
            ldarg Arg_0x4
           conv.u2
          ldelema [mscorlib]System.UInt64
         ldind.u8
        clt.un
       conv.ovf.i1
      stind.i1
End_Orphan_99:
Start_Orphan_a0:
Start_Orphan_a1:
           ldc.i4.5
          conv.ovf.i1
         conv.i2
        starg Arg_0x1
End_Orphan_a1:
        ldloca LOCAL_0x1
       pop
End_Orphan_a0:
Start_Orphan_a2:
Start_Orphan_a3:
           ldarg Arg_0x2
          starg Arg_0x6
End_Orphan_a3:
          ldsfld int8[] ILGEN_0xdea951c0::field_0x3
         pop
End_Orphan_a2:
Start_Orphan_a4:
            ldc.i4.2
           pop
End_Orphan_a4:
             ldc.i8 0xce1c62438cf96e37
             ldc.i8 0x74d2d5f51d802c7a
            rem.un
           conv.ovf.i4
          conv.ovf.u.un
         conv.ovf.i8.un
Start_Orphan_a5:
Start_Orphan_a6:
           nop
End_Orphan_a6:
            ldc.i8 0xbe99df0565e137b1
           conv.u2
          conv.u1
 newarr [mscorlib]System.SByte
         stsfld int8[] ILGEN_0xdea951c0::field_0x3
End_Orphan_a5:
             ldc.i8 0x81e90a95be901fcd
            conv.ovf.u4
           conv.r4
          conv.u1
         conv.u8
        add.ovf
Start_Orphan_a7:
Start_Orphan_a8:
           ldc.r8 float64(0x1c897db112624f99)
          stsfld float32 ILGEN_0xdea951c0::field_0x0
End_Orphan_a8:
Start_Orphan_a9:
           nop
End_Orphan_a9:
            ldsfld int8[] ILGEN_0xdea951c0::field_0x3
           ldlen
          conv.u1
 newarr [mscorlib]System.Single
            ldarg Arg_0x0
           conv.ovf.i4
            ldc.i4 0xc378f2c5
            ldc.i4.0
           or
          cgt
Start_Orphan_aa:
           nop
End_Orphan_aa:
            ldarg Arg_0x0
           conv.r.un
          ckfinite
         stelem.r4
End_Orphan_a7:
             ldc.r4 float32(0xe8131f54)
             ldloc LOCAL_0x1
            div 
           conv.u8
          not
Start_Orphan_ab:
            ldsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
            ldc.i4.s -1
           ldelema [mscorlib]System.UInt64
          pop
End_Orphan_ab:
             ldarg Arg_0x1
             ldc.i4.4
            div.un
           conv.u1
Start_Orphan_ac:
            ldc.i4.4
           starg Arg_0x2
End_Orphan_ac:
             ldarg Arg_0x8
             ldloc LOCAL_0x0
            cgt.un
           conv.ovf.u4.un
          xor
         shr
             ldc.i4.0
            conv.ovf.i.un
           conv.u8
             ldc.i8 0x4691426f586a3f7
             ldarg Arg_0x0
            add
Start_Orphan_ad:
            nop
End_Orphan_ad:
             ldloc LOCAL_0x0
            conv.ovf.u8
           mul.ovf
          add.ovf.un
            ldsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
Start_Orphan_ae:
            nop
End_Orphan_ae:
             ldc.i4.6
            conv.ovf.u1.un
           ldelema [mscorlib]System.UInt64
          ldind.u8
         sub.ovf.un
        sub.ovf
       cgt.un
Start_Orphan_af:
Start_Orphan_b0:
Start_Orphan_b1:
          nop
End_Orphan_b1:
           ldsfld int8[] ILGEN_0xdea951c0::field_0x3
           ldarg Arg_0x8
          ldelema [mscorlib]System.SByte
         pop
End_Orphan_b0:
         ldsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
        stsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
End_Orphan_af:
Start_Orphan_b2:
           ldarga Arg_0x1
          pop
End_Orphan_b2:
          ldsfld float32 ILGEN_0xdea951c0::field_0x0
         neg
        conv.u4
       conv.ovf.i4.un
      cgt
     ldelema [mscorlib]System.Single
    pop
 Branch_0x7:

End_Orphan_1:
   ldc.i8 0x511527cad3a974cb
Start_Orphan_b3:
    ldc.i4.7
   starg Arg_0x6
End_Orphan_b3:
Start_Orphan_b4:
Start_Orphan_b5:
Start_Orphan_b6:
Start_Orphan_b7:
Start_Orphan_b8:
         nop
End_Orphan_b8:
         ldloc LOCAL_0x2
        stloc LOCAL_0x2
End_Orphan_b7:
        ldarga Arg_0x1
Start_Orphan_b9:
Start_Orphan_ba:
         nop
End_Orphan_ba:
          ldarg Arg_0x6
         conv.u1
 newarr [mscorlib]System.SByte
          ldc.i4.6
          ldc.i4.4
         shl
          ldc.i4.5
         conv.ovf.u1
        stelem.i1
End_Orphan_b9:
Start_Orphan_bb:
          nop
End_Orphan_bb:
           ldc.i8 0xb6d338a14bf10dab
           ldc.i8 0x239daa5758a0a66d
          clt
Start_Orphan_bc:
          nop
End_Orphan_bc:
           ldc.i4.7
           ldc.i4.4
          mul
         mul
Start_Orphan_bd:
          ldarg Arg_0x5
         starg Arg_0x7
End_Orphan_bd:
           ldc.i4.0
          conv.ovf.i8.un
         conv.ovf.u1
        ceq
       stind.i2
End_Orphan_b6:
       ldsfld int8[] ILGEN_0xdea951c0::field_0x3
      pop
End_Orphan_b5:
Start_Orphan_be:
Start_Orphan_bf:
          ldloc LOCAL_0x2
         stloc LOCAL_0x2
End_Orphan_bf:
         ldarga Arg_0x0
        pop
End_Orphan_be:
Start_Orphan_c0:
Start_Orphan_c1:
           nop
End_Orphan_c1:
           ldarga Arg_0x3
          pop
End_Orphan_c0:
Start_Orphan_c2:
            nop
End_Orphan_c2:
             ldc.i4.s 37
            conv.ovf.i8.un
           conv.ovf.u2.un
          conv.ovf.i4.un
Start_Orphan_c3:
Start_Orphan_c4:
           nop
End_Orphan_c4:
            ldc.i4.0
           conv.ovf.i4
          stsfld int32 ILGEN_0xdea951c0::field_0x2
End_Orphan_c3:
Start_Orphan_c5:
            ldarga Arg_0x6
            ldarg Arg_0x6
           stind.i2
End_Orphan_c5:
Start_Orphan_c6:
            nop
End_Orphan_c6:
             ldloc LOCAL_0x0
             ldarg Arg_0x4
            mul
Start_Orphan_c7:
            nop
End_Orphan_c7:
             ldarg Arg_0x0
            conv.ovf.i1
           div
             ldloc LOCAL_0x0
             ldc.i4.s 102
            add
Start_Orphan_c8:
            nop
End_Orphan_c8:
             ldc.i4.2
            conv.ovf.u
           mul
          rem
         and
        conv.ovf.i8.un
Start_Orphan_c9:
          ldarga Arg_0x7
         pop
End_Orphan_c9:
Start_Orphan_ca:
Start_Orphan_cb:
           nop
End_Orphan_cb:
            ldc.r8 float64(0x13d12d3643e00b63)
           call float64 [mscorlib]System.Math::Round(float64)
          pop
End_Orphan_ca:
             ldc.r8 float64(0xb36da0bcce5880ec)
            conv.ovf.u8
           conv.ovf.u8.un
             ldc.i4.1
             ldarg Arg_0x8
            cgt
           conv.i1
          shr
             ldarg Arg_0x7
             ldarg Arg_0x8
            mul.ovf.un
             ldc.i8 0xd8d8b53b91e37cc8
            conv.u4
           add.ovf
          conv.ovf.u4.un
         shl
Start_Orphan_ce:
            ldsflda int32 ILGEN_0xdea951c0::field_0x2
           ldind.i4
          conv.u1
 newarr [mscorlib]System.UInt64
            ldc.i8 0x5fb1a00f752d59e7
            ldc.i4.3
           shl
          conv.ovf.u2
            ldc.i8 0xd8c18c1f116e1084
            ldarg Arg_0x0
           xor
            ldarg Arg_0x0
            ldc.i4.0
           shr.un
          rem.un
         stelem.i8
End_Orphan_ce:
             ldc.i4.8
            conv.u8
           conv.ovf.i2.un
          conv.r4
         conv.u1
        shr.un
       rem.un
      conv.ovf.u8
     conv.i8
    pop
End_Orphan_b4:
         ldarg Arg_0x1
        conv.u1
 newarr [mscorlib]System.Single
          ldarg Arg_0x0
              ldloc LOCAL_0x0
             conv.ovf.u.un
              ldc.i4.1
             conv.i4
            add
           conv.ovf.i8.un
              ldarg Arg_0x8
             conv.ovf.u4
            conv.i8
              ldarg Arg_0x0
             conv.ovf.i8.un
            conv.ovf.i8.un
           mul.ovf
          add.ovf.un
         clt
              ldloc LOCAL_0x0
             conv.i2
            conv.i8
           conv.ovf.i4.un
              ldloc LOCAL_0x0
             conv.i4
            conv.ovf.i
           conv.ovf.u
          and
             ldloc LOCAL_0x2
              ldc.i4.1
             not
            ldelema [mscorlib]System.Single
           ldind.r4
          conv.i2
         sub.ovf
        cgt.un
       ldelem.r4
      call float64 [mscorlib]System.Math::Abs(float64)
              ldc.i8 0xe893c90a6c1ea0b5
              ldarg Arg_0x1
             shr
              ldc.i8 0x123dbc7da82b7c27
             conv.ovf.u8.un
            sub.ovf.un
           conv.r.un
          call float64 [mscorlib]System.Math::Abs(float64)
         conv.i1
        conv.r.un
       call float64 [mscorlib]System.Math::Sqrt(float64)
      neg
     cgt
    conv.ovf.i2.un
   conv.u8
 bne.un Branch_0x4

    ldloca LOCAL_0x0
              ldc.i8 0x21aa9fbab61303e8
             conv.u
            conv.i8
           conv.i4
          conv.r4
         conv.ovf.i2
        conv.ovf.i8.un
       conv.r4
      conv.ovf.u8
              ldc.i8 0xfd9a19af334f1ea1
              ldarg Arg_0x0
             mul.ovf.un
            conv.ovf.i1
           conv.u8
          conv.ovf.u1
         conv.r.un
        conv.ovf.i4
       conv.ovf.u8.un
              ldloc LOCAL_0x0
             conv.ovf.u.un
            conv.u8
           conv.u
          conv.i8
              ldloc LOCAL_0x0
              ldc.i4.m1
             sub
            conv.ovf.i8.un
           conv.u8
            ldarg Arg_0x1
             ldarga Arg_0x5
            ldind.i
           clt
          shl
         div.un
           ldarg Arg_0x0
           ldarg Arg_0x0
          add
         conv.ovf.u8.un
        mul
       not
      sub
     clt
              ldc.i8 0x502a30b7b90dfdaf
              ldarg Arg_0x0
             mul.ovf.un
              ldc.i8 0xdfea8a90571359fd
              ldc.i8 0x51975f727329df58
             mul
            sub
              ldarg Arg_0x0
              ldc.i4.1
             shl
              ldarga Arg_0x0
             ldind.u8
            sub
           or
              ldc.i4.8
              ldloc LOCAL_0x0
             sub.ovf.un
            conv.ovf.u2
              ldsfld unsigned int64[] ILGEN_0xdea951c0::field_0x1
             ldlen
              ldarg Arg_0x0
             conv.ovf.u
            div
           cgt.un
          shl
              ldc.i8 0xe9ee540d1286d32b
              ldc.i8 0x28c6aa2d38ca8021
             xor
            conv.ovf.i4.un
           conv.ovf.i8.un
          conv.u8
         and
              ldarg Arg_0x0
              ldarg Arg_0x0
             mul.ovf.un
            conv.ovf.u1
           conv.ovf.i8.un
          conv.i8
              ldarg Arg_0x8
             conv.ovf.u8.un
              ldarg Arg_0x0
              ldarg Arg_0x0
             rem
            mul
              ldc.i8 0xd30e12f7f01fcdfc
              ldc.i8 0x96d3b752b17664b5
             xor
              ldarga Arg_0x0
             ldind.u8
            add.ovf
           add.ovf.un
          conv.ovf.u2.un
         shr
        rem.un
       conv.r4
      conv.i4
        ldc.i4.7
       conv.ovf.u1
      conv.u
     div.un
    sub.ovf.un
   stind.i
 br Branch_0x5

 Branch_0x4:

    ldloca LOCAL_0x0
   pop
 Branch_0x5:

End_Orphan_0:
ldc.r8 4.4

 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	19
.locals (int32)
ldc.i4 101
stloc.0
.try {
ldc.i8 0x1ebefcbfa56b496 
ldc.i4 0x932c2d53 
ldc.i4 0x714040db 
ldc.i4 0xe9264bfd 
ldc.i4 0x60da3429 conv.i
ldc.i4 0xd3610838 conv.i
ldc.i4 0x9e0e9615 
ldc.i4 0x1b546a2b conv.i
ldc.i4 0x311b99db 

 call float64  ILGEN_0xdea951c0::Method_0x5a7bd7a1(unsigned int64 Arg_0x0, int16 Arg_0x1, int8 Arg_0x2, unsigned int8 Arg_0x3, native int Arg_0x4, native int Arg_0x5, unsigned int16 Arg_0x6, native int Arg_0x7, int32 Arg_0x8)
conv.i4
leave.s IL_END
} catch [mscorlib]System.DivideByZeroException {
ldc.i4 100
stloc.0
leave.s IL_END
}
IL_END:
  ldloc.0
 ret
}

}
