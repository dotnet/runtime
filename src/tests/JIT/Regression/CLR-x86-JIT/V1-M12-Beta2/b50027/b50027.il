// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x13e5362e {}
.class ILGEN_0x13e5362e {

//BEGIN STATIC FIELDS
.field static	int64 field_0x0
.field static	unsigned int8 field_0x1
.field static	float64[] field_0x2
.field static	int32 field_0x3
//END STATIC FIELDS
.method static int32 Method_0x6ddf86e(unsigned int16 Arg_0x0, float32 Arg_0x1, int32 Arg_0x2, unsigned int8 Arg_0x3, unsigned int32 Arg_0x4, unsigned int32 Arg_0x5, unsigned int8 Arg_0x6, int64 Arg_0x7, unsigned int64 Arg_0x8) {
.maxstack	19

 .locals (int8[] local_0x0,float32 local_0x1)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x0
ldc.r4 float32(0x711d9a6f) 
stloc local_0x1

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i8 0xa9c71948cdc6bfd 
stsfld int64 ILGEN_0x13e5362e::field_0x0
ldc.i4 0x91e4070d 
stsfld unsigned int8 ILGEN_0x13e5362e::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x13e5362e::field_0x2
ldc.i4 0xcc84b531 
stsfld int32 ILGEN_0x13e5362e::field_0x3

//END INIT OF STATIC FIELDS

Start_Orphan_36:
 ldloc local_0x0
 stloc local_0x0
End_Orphan_36:
 ldarg Arg_0x4
 ldc.i4.0
 mul
 ldc.i4.3
 ldc.i4.m1
 rem.un
 add
Start_Orphan_37:
 nop
End_Orphan_37:
 ldloc local_0x0
 ldc.i4.s 92
 ldelema [mscorlib]System.SByte
 ldind.i1
 add.ovf
 conv.ovf.i8.un
Start_Orphan_38:
Start_Orphan_39:
 ldc.i4.3
 pop
End_Orphan_39:
 ldc.r4 float32(0xd717c000)
 call float64 [mscorlib]System.Math::Round(float64)
 conv.u8
 pop
End_Orphan_38:
Start_Orphan_3a:
 ldsflda int32 ILGEN_0x13e5362e::field_0x3
 ldc.i4.7
 stind.i4
End_Orphan_3a:
Start_Orphan_3b:
 nop
End_Orphan_3b:
 ldloc local_0x1
 conv.ovf.u8
 ldc.i4.s -125
 conv.ovf.i8.un
 xor
 conv.r4
 conv.u8
 rem.un
Start_Orphan_3c:
 ldsflda int32 ILGEN_0x13e5362e::field_0x3
Start_Orphan_3d:
 ldsflda int64 ILGEN_0x13e5362e::field_0x0
 ldarg Arg_0x8
 stind.i8
End_Orphan_3d:
 ldc.i4.4
 conv.i4
 stind.i4
End_Orphan_3c:
Start_Orphan_3e:
 ldsfld float64[] ILGEN_0x13e5362e::field_0x2
 ldarg Arg_0x5
 ldloc local_0x1
 stelem.r8
End_Orphan_3e:
Start_Orphan_3f:
 nop
End_Orphan_3f:
 ldc.i8 0xc92059a35cdf06f3
 conv.ovf.i8.un
 conv.i8
 conv.r4
 conv.ovf.i8
 ldsflda int64 ILGEN_0x13e5362e::field_0x0
 ldind.i8
 rem.un
 mul.ovf.un
 conv.ovf.i4.un
 conv.ovf.u4.un
Start_Orphan_40:
Start_Orphan_41:
Start_Orphan_42:
Start_Orphan_43:
 nop
End_Orphan_43:
 ldarg Arg_0x6
 conv.i8
 starg Arg_0x7
End_Orphan_42:
 ldarg Arg_0x3
 conv.ovf.i
 conv.ovf.i1.un
 conv.r4
 starg Arg_0x1
End_Orphan_41:
Start_Orphan_44:
 nop
End_Orphan_44:
 ldloc local_0x1
 conv.r8
 call float64 [mscorlib]System.Math::Sqrt(float64)
 ldloc local_0x1
 mul 
 conv.u8
Start_Orphan_45:
 ldc.i4.2
 starg Arg_0x4
End_Orphan_45:
Start_Orphan_46:
 ldloc local_0x0
 ldsfld int32 ILGEN_0x13e5362e::field_0x3
 ldelema [mscorlib]System.SByte
 pop
End_Orphan_46:
Start_Orphan_47:
 ldarg Arg_0x3
 starg Arg_0x2
End_Orphan_47:
Start_Orphan_48:
 nop
End_Orphan_48:
 ldsflda int64 ILGEN_0x13e5362e::field_0x0
 ldind.i8
Start_Orphan_49:
 ldarg Arg_0x7
 pop
End_Orphan_49:
 ldarg Arg_0x5
 ldc.i4.s -100
 cgt.un
 conv.u1
 shr.un
Start_Orphan_4a:
 nop
End_Orphan_4a:
 ldc.i8 0xdf4eb7b7640bdce5
 conv.i2
 conv.ovf.u8.un
 ldc.i4.s -84
 shr
 mul.ovf
 rem.un
 pop
End_Orphan_40:
Start_Orphan_4b:
 ldsflda int64 ILGEN_0x13e5362e::field_0x0
 pop
End_Orphan_4b:
Start_Orphan_4c:
 ldarga Arg_0x2
 pop
End_Orphan_4c:
 ldc.i4.3
 conv.ovf.u.un
 conv.ovf.u.un
 ldarg Arg_0x4
Start_Orphan_4d:
 nop
End_Orphan_4d:
 ldc.i4.1
 ldc.i4.0
 add.ovf
 ceq
 ldarg Arg_0x1
 conv.ovf.u8
 ldarg Arg_0x7
 ldc.i4.5
 shr
 cgt
 clt
Start_Orphan_4e:
Start_Orphan_4f:
 nop
End_Orphan_4f:
 ldarga Arg_0x7
 pop
End_Orphan_4e:
Start_Orphan_50:
 nop
End_Orphan_50:
 ldarg Arg_0x7
 ldc.i8 0x5aeccf13aa09820b
 conv.u8
 sub
 conv.u4
 add.ovf
 mul
 conv.ovf.i1.un
 not
Start_Orphan_51:
Start_Orphan_52:
Start_Orphan_53:
 nop
End_Orphan_53:
 ldc.r8 float64(0xfc2951e8395ea6fe)
 conv.ovf.u8
 conv.u8
Start_Orphan_54:
 ldarga Arg_0x2
 ldc.i4.3
 stind.i4
End_Orphan_54:
 ldarg Arg_0x8
 ldc.i8 0x13beb6d6b447fe14
 xor
 ldc.i8 0xe1cf22c3e4910ac1
 conv.ovf.u8.un
 mul
 add.ovf
 starg Arg_0x7
End_Orphan_52:
Start_Orphan_55:
Start_Orphan_56:
 ldarga Arg_0x7
 ldc.i8 0xe679991c27010e63
 stind.i8
End_Orphan_56:
Start_Orphan_57:
 nop
End_Orphan_57:
 ldc.i8 0x5651feefdfb497ba
 ldsfld int64 ILGEN_0x13e5362e::field_0x0
 or
 neg
 starg Arg_0x8
End_Orphan_55:
Start_Orphan_58:
Start_Orphan_59:
 nop
End_Orphan_59:
 ldsflda int32 ILGEN_0x13e5362e::field_0x3
Start_Orphan_5a:
 nop
End_Orphan_5a:
 ldc.i4.1
 ldc.i4.4
 cgt.un
 stind.i4
End_Orphan_58:
Start_Orphan_5b:
 nop
End_Orphan_5b:
 ldc.i4.m1
 conv.ovf.i4.un
Start_Orphan_5c:
 nop
End_Orphan_5c:
 ldarg Arg_0x7
 conv.ovf.i4.un
 mul
Start_Orphan_5d:
 ldc.i8 0x4aa9cf579c29ac77
 stsfld int64 ILGEN_0x13e5362e::field_0x0
End_Orphan_5d:
Start_Orphan_5e:
 nop
End_Orphan_5e:
 ldsfld unsigned int8 ILGEN_0x13e5362e::field_0x1
 ldc.i4.6
 cgt
Start_Orphan_5f:
 nop
End_Orphan_5f:
 ldc.i8 0x557a5d4d515a31c8
 conv.ovf.i4.un
 mul.ovf
 sub.ovf.un
Start_Orphan_60:
 ldarga Arg_0x2
 ldc.r8 float64(0x5f18443d8c5756bf)
 conv.i4
 stind.i4
End_Orphan_60:
Start_Orphan_61:
 ldsfld int64 ILGEN_0x13e5362e::field_0x0
 pop
End_Orphan_61:
 ldc.i4.0
 conv.ovf.i1.un
 ldc.i8 0xfa226da263248c52
 conv.ovf.i2.un
 add.ovf
Start_Orphan_62:
 nop
End_Orphan_62:
 ldarg Arg_0x8
 ldarg Arg_0x7
 mul.ovf
 conv.ovf.i.un
 add.ovf.un
 add.ovf.un
 ldsfld float64[] ILGEN_0x13e5362e::field_0x2
 ldc.i4.5
 ldelema [mscorlib]System.Double
 ldind.r8
 call float64 [mscorlib]System.Math::Abs(float64)
Start_Orphan_63:
 nop
End_Orphan_63:
 ldc.r8 float64(0x6db604258aa84b0f)
 call float64 [mscorlib]System.Math::Round(float64)
Start_Orphan_64:
 nop
End_Orphan_64:
 ldloc local_0x1
 ldarg Arg_0x1
 add 
 add 
 call float64 [mscorlib]System.Math::Sqrt(float64)
 ceq
 mul.ovf.un
 pop
End_Orphan_51:
Start_Orphan_65:
 ldarga Arg_0x2
Start_Orphan_66:
 ldloca local_0x1
 pop
End_Orphan_66:
 ldarg Arg_0x1
 conv.i4
 conv.ovf.u
Start_Orphan_67:
 ldloc local_0x0
 pop
End_Orphan_67:
Start_Orphan_68:
 nop
End_Orphan_68:
 ldsfld int64 ILGEN_0x13e5362e::field_0x0
 conv.ovf.u1
 conv.ovf.i4.un
 clt
Start_Orphan_69:
Start_Orphan_6a:
 nop
End_Orphan_6a:
 ldc.r8 float64(0x7b5150d33ee01258)
 conv.i2
 starg Arg_0x0
End_Orphan_69:
Start_Orphan_6b:
 ldc.i4.1
 starg Arg_0x6
End_Orphan_6b:
 ldc.i4.s 108
 conv.i2
Start_Orphan_6c:
 ldsfld int32 ILGEN_0x13e5362e::field_0x3
 starg Arg_0x5
End_Orphan_6c:
 ldarg Arg_0x6
 conv.i1
 ldc.i4.6
 ldc.i4.5
 cgt
 shl
 mul.ovf
 xor
 stind.i4
End_Orphan_65:
Start_Orphan_6d:
 ldarg Arg_0x5
 starg Arg_0x0
End_Orphan_6d:
Start_Orphan_6e:
 nop
End_Orphan_6e:
 ldc.i4.5
 ldc.i4.7
 add
 ldarg Arg_0x0
 ldc.i4.m1
 rem.un
 sub.ovf.un
 conv.ovf.u1
Start_Orphan_6f:
Start_Orphan_70:
 nop
End_Orphan_70:
 ldarg Arg_0x1
 conv.ovf.u1
 pop
End_Orphan_6f:
Start_Orphan_71:
 ldloc local_0x0
 ldc.i4.4
 ldelema [mscorlib]System.SByte
 pop
End_Orphan_71:
Start_Orphan_72:
 nop
End_Orphan_72:
 ldarg Arg_0x3
 conv.ovf.u
Start_Orphan_73:
 nop
End_Orphan_73:
 ldarg Arg_0x1
 conv.u2
 mul
 conv.ovf.u4.un
 add.ovf.un
Start_Orphan_74:
Start_Orphan_75:
 ldloca local_0x1
 ldc.r8 float64(0x56df421f2a77885)
 stind.r4
End_Orphan_75:
 ldarga Arg_0x7
Start_Orphan_76:
 ldloc local_0x0
 stloc local_0x0
End_Orphan_76:
Start_Orphan_77:
 nop
End_Orphan_77:
 ldc.i4.2
 conv.ovf.i1.un
 conv.ovf.u8.un
 stind.i8
End_Orphan_74:
Start_Orphan_78:
 nop
End_Orphan_78:
Start_Orphan_79:
 nop
End_Orphan_79:
 ldc.i4.7
 conv.r.un
 neg
 conv.u8
 conv.ovf.u4.un
 xor
 conv.ovf.i2.un
Start_Orphan_7a:
Start_Orphan_7b:
Start_Orphan_7c:
 nop
End_Orphan_7c:
 ldc.i8 0x19cb23e0e7569fac
 ldc.i8 0x749c6f7b9694aee1
 cgt.un
Start_Orphan_7d:
 nop
End_Orphan_7d:
 ldsflda int32 ILGEN_0x13e5362e::field_0x3
 ldind.i
 shr.un
 starg Arg_0x2
End_Orphan_7b:
Start_Orphan_7e:
 ldsflda int32 ILGEN_0x13e5362e::field_0x3
Start_Orphan_7f:
 nop
End_Orphan_7f:
 ldc.i4.3
 conv.ovf.u1
 stind.i4
End_Orphan_7e:
Start_Orphan_80:
 ldloc local_0x0
 pop
End_Orphan_80:
Start_Orphan_81:
 nop
End_Orphan_81:
 ldsfld float64[] ILGEN_0x13e5362e::field_0x2
 ldc.i4.6
 ldelema [mscorlib]System.Double
 ldind.r8
 conv.u4
 conv.u1
 newarr [mscorlib]System.Double
Start_Orphan_82:
 ldloc local_0x0
 ldc.i4.s 100
 ldc.i4.s -30
 stelem.i1
End_Orphan_82:
Start_Orphan_83:
 nop
End_Orphan_83:
 ldloc local_0x1
 call float64 [mscorlib]System.Math::Round(float64)
 conv.i2
 ldarg Arg_0x0
 ldarg Arg_0x2
 rem.un
Start_Orphan_84:
 nop
End_Orphan_84:
 ldc.i4.7
 conv.ovf.i.un
 clt
 clt.un
 ldelema [mscorlib]System.Double
 pop
End_Orphan_7a:
 ldc.i4.8
 conv.r8
 neg
 conv.ovf.i8
 ldc.i4.s -100
 conv.ovf.i8.un
 ldc.i4.6
 conv.u4
 shl
 ldarg Arg_0x8
 add.ovf.un
 rem
 ldarg Arg_0x0
 ldc.i4.1
 div.un
 conv.u8
 ldc.r8 float64(0x13524435812af8bc)
 conv.u8
 ldc.i4.m1
 conv.ovf.u8.un
 sub.ovf.un
 rem
 ldsflda int64 ILGEN_0x13e5362e::field_0x0
 ldind.i8
 div.un
 or
 conv.i1
 rem.un
 sub.ovf.un
 bgt Branch_0x0

 ldarg Arg_0x7
 conv.ovf.u8
 conv.ovf.i4.un
 ldc.i4.7
 not
 cgt
 ldarg Arg_0x4
 conv.ovf.u4
 conv.u1
 newarr [mscorlib]System.Double
 ldarg Arg_0x2
 ldarg Arg_0x6
 div
 conv.u4
 ldelem.r8
 conv.r4
 ldc.r8 float64(0x72241712a556105e)
 clt
 xor
 ldsfld unsigned int8 ILGEN_0x13e5362e::field_0x1
 conv.ovf.u8
 conv.u
 div.un
 conv.ovf.u4
 br Branch_0x1

 Branch_0x0:

 ldloc local_0x0
 ldsfld int32 ILGEN_0x13e5362e::field_0x3
 ldelema [mscorlib]System.SByte
 ldind.i1
 ldarg Arg_0x4
 ldarg Arg_0x0
 add.ovf.un
 cgt
 neg
 ldarg Arg_0x3
 conv.u2
 conv.ovf.u2.un
 ldarg Arg_0x6
 rem.un
 mul.ovf
 ldc.i4.7
 conv.ovf.u8
 conv.r8
 conv.u1
 ldarg Arg_0x6
 conv.u
 ldc.i4.2
 ldc.i4.3
 div
 mul
 conv.ovf.i2.un
 sub
 sub
 ldc.i8 0x323ec9cbb4125a4f
 ldc.i4.m1
 shr
 conv.ovf.u1
 conv.ovf.u1.un
 conv.ovf.i4.un
 ldarg Arg_0x3
 ldc.i4.m1
 div
 ldc.i4.1
 ldarg Arg_0x0
 shl
 mul.ovf
 ldc.r8 float64(0xd0b001483abfdef4)
 conv.ovf.i8
 ldarg Arg_0x7
 conv.i8
 clt.un
 div.un
 ldarg Arg_0x7
 conv.ovf.u.un
 conv.r4
 conv.i
 and
 div.un
 mul.ovf
 conv.ovf.u8
 conv.i2
 Branch_0x1:

EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0xc2ee605a 
ldc.r4 float32(0x1eeb461d) 
ldc.i4 0xbff2ca7b 
ldc.i4 0xade94e1c 
ldc.i4 0xd4df5f89 
ldc.i4 0xeba410c0 
ldc.i4 0xff4ccdfc 
ldc.i8 0xd09d457da9000efc 
ldc.i8 0x76fa042d9b96267b 

call int32 ILGEN_0x13e5362e::Method_0x6ddf86e(unsigned int16 Arg_0x0, float32 Arg_0x1, int32 Arg_0x2, unsigned int8 Arg_0x3, unsigned int32 Arg_0x4, unsigned int32 Arg_0x5, unsigned int8 Arg_0x6, int64 Arg_0x7, unsigned int64 Arg_0x8)
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
