// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly 'b602182' {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}

.class ILGEN_0xad4651aa {

//BEGIN STATIC FIELDS
//END STATIC FIELDS
.method static unsigned int32  Method_0xc3625d79(float64 Arg_0x0, int16 Arg_0x1, unsigned int16 Arg_0x2, unsigned int64 Arg_0x3, int64 Arg_0x4, native int Arg_0x5, int64 Arg_0x6, unsigned int8 Arg_0x7, int8 Arg_0x8, float64 Arg_0x9, int32 Arg_0xa, unsigned int16 Arg_0xb, float32 Arg_0xc) {
.zeroinit 
.maxstack	19

 .locals ()
//BEGIN INIT LOCALS

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_a_0:
Start_Orphan_a_1:
Start_Orphan_a_2:
Start_Orphan_a_3:
     nop
End_Orphan_a_3:
      ldc.i4.0
      ldarg Arg_0x5
     sub
      ldarg Arg_0x5
 brtrue Branch_0x8

      ldc.i4.s -116
 br Branch_0x9

 Branch_0x8:

      ldarg Arg_0x2
 Branch_0x9:

 bgt Branch_0x6

      ldarg Arg_0x4
Start_Orphan_b_4:
     nop
End_Orphan_b_4:
     conv.u
 br Branch_0x7

 Branch_0x6:

Start_Orphan_a_5:
     nop
End_Orphan_a_5:
      ldc.i4.2
      ldarg Arg_0x5
 ble.un Branch_0xa

      ldarg Arg_0x5
 br Branch_0xb

 Branch_0xa:

      ldarg Arg_0x5
 Branch_0xb:

 Branch_0x7:

Start_Orphan_a_6:
     nop
End_Orphan_a_6:
      ldarg Arg_0x2
     conv.u2
Start_Orphan_a_7:
     nop
End_Orphan_a_7:
      ldc.i4 0x65577505
      ldc.i4 0xdd81cf04
     cgt.un
 beq Branch_0xc

Start_Orphan_a_8:
     nop
End_Orphan_a_8:
      ldc.i4.0
      ldarg Arg_0x5
Start_Orphan_b_9:
     nop
End_Orphan_b_9:
     mul
 br Branch_0xd

 Branch_0xc:

Start_Orphan_a_a:
     nop
End_Orphan_a_a:
      ldarg Arg_0xa
      ldarg Arg_0x5
     add
 Branch_0xd:

 beq Branch_0x2

Start_Orphan_a_b:
     nop
End_Orphan_a_b:
Start_Orphan_b_c:
     nop
End_Orphan_b_c:
     ldarga Arg_0xa
Start_Orphan_b_d:
     nop
End_Orphan_b_d:
     ldc.i4.7
    stind.i4
 br Branch_0x3

 Branch_0x2:

      ldc.i4.6
      ldc.i4.6
     shr
    starg Arg_0x2
 Branch_0x3:

End_Orphan_a_2:
Start_Orphan_a_e:
Start_Orphan_a_f:
     nop
End_Orphan_a_f:
     ldarga Arg_0x8
      ldc.r4 float32(0x279f77ca)
      ldc.r4 float32(0xb36d333d)
Start_Orphan_b_10:
     nop
End_Orphan_b_10:
     ceq
    stind.i1
End_Orphan_a_e:
Start_Orphan_a_11:
      nop
End_Orphan_a_11:
      ldc.i4.1
       ldc.i4.4
Start_Orphan_b_12:
      nop
End_Orphan_b_12:
      conv.i1
Start_Orphan_b_13:
      ldarg Arg_0xc
     starg Arg_0x9
End_Orphan_b_13:
     mul
Start_Orphan_b_14:
      ldarg Arg_0x5
Start_Orphan_b_15:
     nop
End_Orphan_b_15:
     conv.i1
    starg Arg_0x2
End_Orphan_b_14:
    conv.r8
   conv.u8
Start_Orphan_a_16:
    ldarga Arg_0x1
   pop
End_Orphan_a_16:
Start_Orphan_a_17:
Start_Orphan_a_18:
     nop
End_Orphan_a_18:
      ldarg Arg_0x6
      ldarg Arg_0x6
Start_Orphan_b_19:
     nop
End_Orphan_b_19:
     sub //rem
    starg Arg_0x4
End_Orphan_a_17:
Start_Orphan_b_1a:
      ldc.i8 0x8129be2bd882ac42
     pop
End_Orphan_b_1a:
     ldc.i8 0xdd549c928698c88a
Start_Orphan_b_1b:
      ldarg Arg_0x9
     conv.r4
    starg Arg_0xc
End_Orphan_b_1b:
    conv.u4
Start_Orphan_b_1c:
Start_Orphan_a_1d:
 ldc.i4 7 
 Loop_0x0: //WHAT
     ldc.i4.8
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x0
 pop
 Loop_0x1: // End of WHAT
End_Orphan_a_1d:
Start_Orphan_b_1e:
     ldc.i4 0x1fee69fa
    starg Arg_0xa
End_Orphan_b_1e:
    ldarga Arg_0x1
   pop
End_Orphan_b_1c:
   conv.u8
 bne.un Branch_0x0

Start_Orphan_a_1f:
Start_Orphan_b_20:
     nop
End_Orphan_b_20:
     newobj     instance void [mscorlib]System.Object::.ctor()
    pop
End_Orphan_a_1f:
    ldarga Arg_0x1
   pop
 br Branch_0x1

 Branch_0x0:

Start_Orphan_a_21:
Start_Orphan_a_22:
     nop
End_Orphan_a_22:
     ldarga Arg_0xc
      ldarg Arg_0xc
      ldarg Arg_0xc
Start_Orphan_b_23:
     nop
End_Orphan_b_23:
     add 
    stind.r4
End_Orphan_a_21:
    ldarga Arg_0x1
   pop
 Branch_0x1:

End_Orphan_a_1:
   ldarg Arg_0x3
Start_Orphan_b_24:
   ldarga Arg_0x8
  pop
End_Orphan_b_24:
  conv.u1
Start_Orphan_a_25:
 ldc.i4 4 
 Loop_0x2: //WHAT
       ldc.i4.1
       ldc.i4.5
      sub
     conv.r4
Start_Orphan_a_26:
      ldarga Arg_0x1
     pop
End_Orphan_a_26:
       ldarg Arg_0xc
       ldarg Arg_0x9
Start_Orphan_b_27:
      nop
End_Orphan_b_27:
      sub  
     call float32 [mscorlib]System.Math::Abs(float32)
Start_Orphan_b_28:
Start_Orphan_a_29:
     nop
End_Orphan_a_29:
     ldarga Arg_0x3
    pop
End_Orphan_b_28:
    sub  
Start_Orphan_a_2a:
      ldc.i4.m1
     starg Arg_0x7
End_Orphan_a_2a:
       ldc.i4.s 59
Start_Orphan_b_2b:
      nop
End_Orphan_b_2b:
      conv.r4
       ldarg Arg_0xc
Start_Orphan_b_2c:
      nop
End_Orphan_b_2c:
      conv.r4
Start_Orphan_b_2d:
      ldarg Arg_0x3
     pop
End_Orphan_b_2d:
     add 
Start_Orphan_b_2e:
      ldarg Arg_0x9
     starg Arg_0x9
End_Orphan_b_2e:
     ldarg Arg_0x9
Start_Orphan_b_2f:
Start_Orphan_a_30:
     nop
End_Orphan_a_30:
     ldarga Arg_0x6
    pop
End_Orphan_b_2f:
    add 
Start_Orphan_b_31:
Start_Orphan_a_32:
     ldc.i4.6
    starg Arg_0x7
End_Orphan_a_32:
      ldarg Arg_0x3
Start_Orphan_b_33:
     nop
End_Orphan_b_33:
     conv.u8
    conv.u8
   starg Arg_0x3
End_Orphan_b_31:
   cgt.un
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x2
 pop
 Loop_0x3: // End of WHAT
End_Orphan_a_25:
Start_Orphan_a_34:
Start_Orphan_a_35:
Start_Orphan_a_36:
     nop
End_Orphan_a_36:
     ldarga Arg_0xa
    pop
End_Orphan_a_35:
Start_Orphan_a_37:
      ldarg Arg_0x0
     starg Arg_0xc
End_Orphan_a_37:
Start_Orphan_a_38:
      nop
End_Orphan_a_38:
       ldarg Arg_0x3
       ldarg Arg_0x4
      mul
       ldc.i4.8
      conv.i8
Start_Orphan_b_39:
      ldarga Arg_0x3
      ldarg Arg_0x3
     stind.i8
End_Orphan_b_39:
     add
Start_Orphan_b_3a:
      ldc.i8 0x8ea94d4a81ea9d9f
     conv.u8
    pop
End_Orphan_b_3a:
    neg
   pop
End_Orphan_a_34:
Start_Orphan_a_3b:
Start_Orphan_a_3c:
      ldarg Arg_0x5
     starg Arg_0x5
End_Orphan_a_3c:
     ldarga Arg_0x7
    pop
End_Orphan_a_3b:
Start_Orphan_a_3d:
Start_Orphan_a_3e:
      nop
End_Orphan_a_3e:
      ldarga Arg_0x4
       ldarg Arg_0x3
       ldarg Arg_0x6
      mul
     stind.i8
End_Orphan_a_3d:
Start_Orphan_a_3f:
       ldc.i4.m1
      starg Arg_0x8
End_Orphan_a_3f:
        ldarg Arg_0x5
        ldc.i4.2
       mul
Start_Orphan_a_40:
       nop
End_Orphan_a_40:
       ldc.i4 0xd484f879
Start_Orphan_b_41:
       ldc.i4.7
      pop
End_Orphan_b_41:
      sub
Start_Orphan_b_42:
Start_Orphan_a_43:
      nop
End_Orphan_a_43:
       ldarg Arg_0x5
       ldc.i4.4
      clt.un
     starg Arg_0x1
End_Orphan_b_42:
     conv.u4
Start_Orphan_b_44:
Start_Orphan_b_45:
      ldarga Arg_0x6
      ldc.i8 0x36f65469807dc69
     stind.i8
End_Orphan_b_45:
     ldarga Arg_0xc
Start_Orphan_a_46:
      ldarga Arg_0x7
      ldarg Arg_0x8
     stind.i1
End_Orphan_a_46:
Start_Orphan_a_47:
      nop
End_Orphan_a_47:
       ldc.r4 float32(0x520689f9)
      conv.i8
     conv.r8
    stind.r4
End_Orphan_b_44:
    conv.i4
Start_Orphan_a_48:
     ldarga Arg_0xc
       ldarga Arg_0x0
Start_Orphan_b_49:
      nop
End_Orphan_b_49:
      ldind.r8
Start_Orphan_b_4a:
      ldarga Arg_0xa
     pop
End_Orphan_b_4a:
     conv.r8
    stind.r4
End_Orphan_a_48:
Start_Orphan_a_4b:
     nop
End_Orphan_a_4b:
Start_Orphan_a_4c:
       nop
End_Orphan_a_4c:
        ldarg Arg_0x5
       conv.i2
        ldarg Arg_0xc
       conv.u
Start_Orphan_b_4d:
       ldarga Arg_0x9
       ldarg Arg_0x9
      stind.r8
End_Orphan_b_4d:
      or
     conv.u1
Start_Orphan_b_4e:
Start_Orphan_b_4f:
      ldarga Arg_0x0
      ldc.r4 float32(0x42674a5b)
     stind.r8
End_Orphan_b_4f:
     ldarga Arg_0x8
Start_Orphan_a_50:
      nop
End_Orphan_a_50:
       ldarg Arg_0x5
       ldarg Arg_0x5
Start_Orphan_b_51:
      nop
End_Orphan_b_51:
      xor
Start_Orphan_b_52:
      ldarg Arg_0x3
     starg Arg_0x6
End_Orphan_b_52:
     conv.u1
    stind.i1
End_Orphan_b_4e:
    conv.i1
   clt
Start_Orphan_b_53:
Start_Orphan_a_54:
      nop
End_Orphan_a_54:
Start_Orphan_b_55:
      nop
End_Orphan_b_55:
      ldarga Arg_0x9
     ldind.r8
       ldc.i4.2
      conv.u4
Start_Orphan_b_56:
      newobj     instance void [mscorlib]System.Object::.ctor()
     pop
End_Orphan_b_56:
     conv.r.un
    mul 
   conv.i1
  starg Arg_0xb
End_Orphan_b_53:
  conv.i2
 sub //rem
pop
End_Orphan_a_0:
Start_Orphan_b_57:
Start_Orphan_a_58:
       ldarga Arg_0x4
       ldc.i8 0x2c05c6edd0b43c07
      stind.i8
End_Orphan_a_58:
        ldc.i4.7
       conv.u
Start_Orphan_b_59:
       ldc.i4.0
      starg Arg_0x1
End_Orphan_b_59:
      conv.i8
     starg Arg_0x4
End_Orphan_b_57:
     ldc.i4.6
Start_Orphan_b_5a:
Start_Orphan_a_5b:
Start_Orphan_b_5c:
      nop
End_Orphan_b_5c:
      ldarga Arg_0x7
       ldarg Arg_0x5
       ldarg Arg_0x5
Start_Orphan_b_5d:
      nop
End_Orphan_b_5d:
      cgt.un
     stind.i1
End_Orphan_a_5b:
     ldarga Arg_0x4
Start_Orphan_b_5e:
       ldc.i4.6
      pop
End_Orphan_b_5e:
      ldc.i8 0x228a990a875aa72a
        ldarg Arg_0xa
        ldarg Arg_0x5
Start_Orphan_b_5f:
       nop
End_Orphan_b_5f:
       add // div
Start_Orphan_b_60:
       ldc.i4.s 71
      starg Arg_0x2
End_Orphan_b_60:
      conv.i8
     xor
    stind.i8
End_Orphan_b_5a:
    conv.i8
Start_Orphan_a_61:
Start_Orphan_a_62:
       nop
End_Orphan_a_62:
        ldc.i4.3
Start_Orphan_b_63:
       nop
End_Orphan_b_63:
       conv.i4
      starg Arg_0xa
End_Orphan_a_61:
Start_Orphan_a_64:
        ldarg Arg_0x3
       starg Arg_0x3
End_Orphan_a_64:
Start_Orphan_a_65:
        nop
End_Orphan_a_65:
         ldarg Arg_0x5
        conv.i8
Start_Orphan_a_66:
        nop
End_Orphan_a_66:
         ldc.i8 0x7a90e759ce601779
         ldarg Arg_0x7
        shr.un
Start_Orphan_b_67:
        ldarga Arg_0x8
        ldarg Arg_0x1
       stind.i1
End_Orphan_b_67:
       or
Start_Orphan_b_68:
       ldarga Arg_0x6
      pop
End_Orphan_b_68:
      conv.u8
Start_Orphan_b_69:
Start_Orphan_a_6a:
      nop
End_Orphan_a_6a:
      newobj     instance void [mscorlib]System.Object::.ctor()
     pop
End_Orphan_b_69:
     conv.r8
    conv.i8
   add // div.un
Start_Orphan_b_6b:
Start_Orphan_a_6c:
Start_Orphan_a_6d:
      ldarga Arg_0x1
      ldc.i4.1
     stind.i2
End_Orphan_a_6d:
     ldarga Arg_0x6
    pop
End_Orphan_a_6c:
    ldarga Arg_0x3
Start_Orphan_b_6e:
Start_Orphan_a_6f:
      nop
End_Orphan_a_6f:
      ldc.r4 float32(0xa676c7d2)
Start_Orphan_b_70:
      ldarga Arg_0x8
      ldc.i4.3
     stind.i1
End_Orphan_b_70:
     call float32 [mscorlib]System.Math::Abs(float32)
    call float64 [mscorlib]System.Math::Round(float64)
   pop
End_Orphan_b_6e:
   ldind.u8
  starg Arg_0x6
End_Orphan_b_6b:
  conv.i1
Start_Orphan_b_71:
Start_Orphan_a_72:
Start_Orphan_a_73:
Start_Orphan_b_74:
     nop
End_Orphan_b_74:
     ldc.i4 0x9ce58939
    starg Arg_0x1
End_Orphan_a_73:
Start_Orphan_a_75:
      ldarga Arg_0x1
      ldarg Arg_0x7
     stind.i2
End_Orphan_a_75:
       ldc.r8 float64(0xe8c250c68c997c79)
Start_Orphan_b_76:
      nop
End_Orphan_b_76:
      ckfinite
     conv.i
Start_Orphan_a_77:
      ldarga Arg_0xc
     pop
End_Orphan_a_77:
       ldc.i8 0x97c68e95cd625695
       ldarg Arg_0x3
      add
     conv.i
Start_Orphan_b_78:
Start_Orphan_a_79:
     nop
End_Orphan_a_79:
     ldarga Arg_0x2
    pop
End_Orphan_b_78:
    add
Start_Orphan_a_7a:
      ldc.i4.4
     starg Arg_0xa
End_Orphan_a_7a:
Start_Orphan_a_7b:
      nop
End_Orphan_a_7b:
       ldarga Arg_0xc
Start_Orphan_b_7c:
      nop
End_Orphan_b_7c:
      ldind.r4
Start_Orphan_b_7d:
      ldarga Arg_0xc
      ldc.r8 float64(0x98b40426fe8cbc62)
     stind.r4
End_Orphan_b_7d:
     conv.u2
Start_Orphan_a_7e:
      nop
End_Orphan_a_7e:
       ldarg Arg_0x5
Start_Orphan_b_7f:
      nop
End_Orphan_b_7f:
      conv.u
     conv.i1
    mul
Start_Orphan_b_80:
Start_Orphan_b_81:
     ldc.i8 0xdf745ea2c48a8b4a
    pop
End_Orphan_b_81:
    ldarga Arg_0x2
   pop
End_Orphan_b_80:
   add
  starg Arg_0x5
End_Orphan_a_72:
Start_Orphan_a_82:
Start_Orphan_a_83:
      ldarg Arg_0x5
Start_Orphan_b_84:
     nop
End_Orphan_b_84:
     conv.i2
    starg Arg_0x5
End_Orphan_a_83:
Start_Orphan_a_85:
      ldc.i4.8
     starg Arg_0x7
End_Orphan_a_85:
Start_Orphan_a_86:
      nop
End_Orphan_a_86:
       ldarg Arg_0x8
       ldarg Arg_0x5
      clt.un
       ldarg Arg_0x5
       ldarg Arg_0x5
      add
     add
Start_Orphan_a_87:
      ldc.i4.0
     pop
End_Orphan_a_87:
       ldarg Arg_0x5
       ldc.i4.0
Start_Orphan_b_88:
      nop
End_Orphan_b_88:
      sub
Start_Orphan_a_89:
      nop
End_Orphan_a_89:
       ldarg Arg_0x9
      conv.i1
     sub //rem
Start_Orphan_b_8a:
     ldarga Arg_0x5
    pop
End_Orphan_b_8a:
    add
   starg Arg_0x5
End_Orphan_a_82:
Start_Orphan_a_8b:
Start_Orphan_a_8c:
      ldarga Arg_0x0
      ldc.r8 float64(0x674b88b2785c8d97)
     stind.r8
End_Orphan_a_8c:
Start_Orphan_a_8d:
      nop
End_Orphan_a_8d:
       ldarg Arg_0xc
      call float64 [mscorlib]System.Math::Abs(float64)
     conv.i8
    starg Arg_0x4
End_Orphan_a_8b:
Start_Orphan_a_8e:
      ldarga Arg_0xa
Start_Orphan_b_8f:
      nop
End_Orphan_b_8f:
      ldc.i4.s -10
     stind.i4
End_Orphan_a_8e:
Start_Orphan_a_90:
       nop
End_Orphan_a_90:
        ldarg Arg_0x5
       conv.u8
        ldarg Arg_0x3
Start_Orphan_b_91:
       nop
End_Orphan_b_91:
       conv.i4
Start_Orphan_b_92:
 ldc.i4 7 
 Loop_0x4: //WHAT
       ldarg Arg_0x8
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x4
 pop
 Loop_0x5: // End of WHAT
End_Orphan_b_92:
      shr
Start_Orphan_a_93:
       ldarga Arg_0x8
      pop
End_Orphan_a_93:
Start_Orphan_a_94:
       nop
End_Orphan_a_94:
        ldarga Arg_0x2
Start_Orphan_b_95:
       nop
End_Orphan_b_95:
       ldind.u2
Start_Orphan_b_96:
       ldarga Arg_0xc
      pop
End_Orphan_b_96:
      conv.u8
     cgt.un
Start_Orphan_b_97:
       ldarg Arg_0x6
      conv.r.un
     call float64 [mscorlib]System.Math::Round(float64)
    starg Arg_0xc
End_Orphan_b_97:
    conv.r.un
   conv.i1
Start_Orphan_b_98:
Start_Orphan_a_99:
      ldc.i8 0xe6825556eb86f8e1
     starg Arg_0x6
End_Orphan_a_99:
Start_Orphan_a_9a:
      nop
End_Orphan_a_9a:
       ldarg Arg_0xc
      call float64 [mscorlib]System.Math::Sin(float64)
     conv.u
Start_Orphan_b_9b:
      ldarg Arg_0x4
     conv.i8
    starg Arg_0x6
End_Orphan_b_9b:
    conv.i8
   conv.i4
  starg Arg_0x7
End_Orphan_b_98:
  conv.r.un
 starg Arg_0xc
End_Orphan_b_71:
 conv.u4
Start_Orphan_b_9c:
Start_Orphan_a_9d:
  nop
End_Orphan_a_9d:
Start_Orphan_b_9e:
Start_Orphan_a_9f:
      ldarg Arg_0x6
      ldarg Arg_0x6
Start_Orphan_b_a0:
     nop
End_Orphan_b_a0:
     sub
    conv.u8
   starg Arg_0x3
End_Orphan_a_9f:
Start_Orphan_b_a1:
    ldarga Arg_0x7
Start_Orphan_a_a2:
     ldarg Arg_0x5
    pop
End_Orphan_a_a2:
      ldc.i4.1
     conv.u
      ldc.i4.2
      ldarg Arg_0x5
Start_Orphan_b_a3:
     nop
End_Orphan_b_a3:
     ceq
    cgt.un
   stind.i1
End_Orphan_b_a1:
   ldarga Arg_0x3
  pop
End_Orphan_b_9e:
  ldarga Arg_0xb
 ldind.u2
starg Arg_0x1
End_Orphan_b_9c:
conv.i1
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	27
ldc.r8 float64(0x875047a061a1b7f) 
ldc.i4 0x6308fcb3 
ldc.i4 0xb3e50dfb 
ldc.i8 0x13b31c01ee522e58 
ldc.i8 0xb4926de436f18f1a 
ldc.i4 0xa393eabf conv.i
ldc.i8 0xfc9db91f944105c9 
ldc.i4 0xc46bd603 
ldc.i4 0x1be2f607 
ldc.r8 float64(0x392abb071348e3ec) 
ldc.i4 0xa0e93daf 
ldc.i4 0x3d4482a8 
ldc.r4 float32(0x9bbcad43) 

 call unsigned int32  ILGEN_0xad4651aa::Method_0xc3625d79(float64 Arg_0x0, int16 Arg_0x1, unsigned int16 Arg_0x2, unsigned int64 Arg_0x3, int64 Arg_0x4, native int Arg_0x5, int64 Arg_0x6, unsigned int8 Arg_0x7, int8 Arg_0x8, float64 Arg_0x9, int32 Arg_0xa, unsigned int16 Arg_0xb, float32 Arg_0xc)
    conv.i4
    ldc.i4 5
    ceq
    brfalse FAIL
    ldc.i4 100
    br DONE
FAIL:
    ldc.i4 101
DONE:
     ret
}

}
