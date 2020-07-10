// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x12984aee {}
.class ILGEN_0x12984aee {

//BEGIN STATIC FIELDS
.field static	int32 field_0x0
.field static	float32 field_0x1
.field static native int[] field_0x2
.field static	int16 field_0x3
.field static	int32[] field_0x4
.field static	int16[] field_0x5
.field static	unsigned int16 field_0x6
.field static	unsigned int8 field_0x7
//END STATIC FIELDS
.method static int32 Method_0x786cd2b5(int8 Arg_0x0, unsigned int8 Arg_0x1, int8 Arg_0x2, int64 Arg_0x3, native int Arg_0x4, int16 Arg_0x5, native int Arg_0x6, unsigned int32 Arg_0x7) {
.maxstack	19

 .locals (unsigned int8 local_0x0,float64[] local_0x1,native int local_0x2,unsigned int16 local_0x3)
//BEGIN INIT LOCALS
ldc.i4 0xcff75e18 
stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x1
ldc.i4 0x5fcd568d 
stloc local_0x2
ldc.i4 0xc96d01b2 
stloc local_0x3

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0x88705487 
stsfld int32 ILGEN_0x12984aee::field_0x0
ldc.r4 float32(0x7eb328eb) 
stsfld float32 ILGEN_0x12984aee::field_0x1
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0x12984aee::field_0x2
ldc.i4 0x23c6323f 
stsfld int16 ILGEN_0x12984aee::field_0x3
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0x12984aee::field_0x4
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x12984aee::field_0x5
ldc.i4 0x28219fc5 
stsfld unsigned int16 ILGEN_0x12984aee::field_0x6
ldc.i4 0x8c3c244c 
stsfld unsigned int8 ILGEN_0x12984aee::field_0x7

//END INIT OF STATIC FIELDS

 BLOCK_1:
 ldc.r4 float32(0x8a81e751)
 conv.ovf.u8
 ldc.i8 0xb2b17069c0de667a
 ldc.i8 0xbb6a6440704dc60e
 rem.un
 xor
 conv.i8
 ldarg Arg_0x6
 conv.ovf.u8
 ldsfld int16[] ILGEN_0x12984aee::field_0x5
 ldc.i4.8
 ldelem.i2
 shr
 ldc.i4.7
 conv.ovf.i8.un
 conv.ovf.i1
 shr
 xor
 not
 ldsfld int16 ILGEN_0x12984aee::field_0x3
 ldc.i4.1
 conv.ovf.i
 ldarg Arg_0x6
 ldloc local_0x2
 mul.ovf.un
 mul.ovf
 conv.ovf.i1.un
 mul
 conv.ovf.i8.un
 clt
 conv.u8
Start_Orphan_19:
Start_Orphan_1a:
 ldc.i8 0xaf49269a99363ae5
 starg Arg_0x3
End_Orphan_1a:
End_Orphan_19:
 ldc.i8 0x717e6f21f9444bb6
 ldarg Arg_0x3
 add.ovf.un
 conv.u8
 ldarg Arg_0x3
 conv.ovf.i8.un
 cgt
 conv.ovf.i8.un
Start_Orphan_1d:
 ldloc local_0x0
 starg Arg_0x1
End_Orphan_1d:
 ldc.i8 0x534eafab288da15c
 conv.ovf.u8
Start_Orphan_1e:
 nop
End_Orphan_1e:
 ldarg Arg_0x3
 ldarg Arg_0x3
 rem.un
 add
 ldloc local_0x2
 conv.ovf.u8.un
Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldarg Arg_0x3
 add.ovf.un
 rem
Start_Orphan_20:
 ldsfld int16[] ILGEN_0x12984aee::field_0x5
 stsfld int16[] ILGEN_0x12984aee::field_0x5
End_Orphan_20:
 ldsfld unsigned int16 ILGEN_0x12984aee::field_0x6
 conv.ovf.u8
Start_Orphan_21:
 nop
End_Orphan_21:
 ldc.i8 0x7f542f3259709f16
 neg
 div
 ldloc local_0x2
 conv.ovf.u8.un
 conv.i8
 mul.ovf.un
 mul
 mul
Start_Orphan_22:
Start_Orphan_23:
 ldsfld native int[] ILGEN_0x12984aee::field_0x2
 ldarg Arg_0x5
 ldelema [mscorlib]System.IntPtr
 pop
End_Orphan_23:
 ldsflda float32 ILGEN_0x12984aee::field_0x1
 pop
End_Orphan_22:
Start_Orphan_24:
Start_Orphan_25:
 ldloca local_0x3
 pop
End_Orphan_25:
 ldsfld int32[] ILGEN_0x12984aee::field_0x4
 pop
End_Orphan_24:
Start_Orphan_26:
 ldloc local_0x0
 conv.u1
 newarr [mscorlib]System.Int16
 pop
End_Orphan_26:
Start_Orphan_27:
 nop
End_Orphan_27:
 ldsfld float32 ILGEN_0x12984aee::field_0x1
 conv.u8
Start_Orphan_28:
 nop
End_Orphan_28:
 ldc.i8 0xde5bad61542c01c3
 conv.ovf.i8
 rem.un
Start_Orphan_29:
 ldsflda int16 ILGEN_0x12984aee::field_0x3
 pop
End_Orphan_29:
Start_Orphan_2a:
 nop
End_Orphan_2a:
 ldarg Arg_0x3
 ldarg Arg_0x3
 rem.un
 ldsfld float32 ILGEN_0x12984aee::field_0x1
 conv.i8
 sub.ovf
 rem.un
 ldsfld float32 ILGEN_0x12984aee::field_0x1
 neg
 ldc.r4 float32(0x3ef93ac5)
 neg
 sub  
 conv.u8
 rem.un
Start_Orphan_2b:
Start_Orphan_2c:
 ldloca local_0x3
 pop
End_Orphan_2c:
 ldarg Arg_0x3
 conv.i
Start_Orphan_2d:
 nop
End_Orphan_2d:
 ldarg Arg_0x1
 ldarg Arg_0x4
 add.ovf.un
 or
 starg Arg_0x6
End_Orphan_2b:
Start_Orphan_2e:
 ldloc local_0x1
 ldc.i4.s -99
 ldc.i4 0x3a346180
 sub.ovf
 ldsfld float32 ILGEN_0x12984aee::field_0x1
 conv.r8
 stelem.r8
End_Orphan_2e:
Start_Orphan_2f:
 ldsfld native int[] ILGEN_0x12984aee::field_0x2
 ldc.i4 0x54294ea3
 ldloc local_0x2
 stelem.i
End_Orphan_2f:
 ldarg Arg_0x3
 ldarg Arg_0x3
 or
 not
Start_Orphan_30:
 ldsfld native int[] ILGEN_0x12984aee::field_0x2
 pop
End_Orphan_30:
 ldsfld unsigned int8 ILGEN_0x12984aee::field_0x7
 shr
 not
 mul.ovf
 mul.ovf.un
Start_Orphan_31:
Start_Orphan_32:
Start_Orphan_33:
 nop
End_Orphan_33:
 ldloc local_0x3
 conv.ovf.i4
 stsfld int32 ILGEN_0x12984aee::field_0x0
End_Orphan_32:
Start_Orphan_34:
 ldsfld native int[] ILGEN_0x12984aee::field_0x2
 stsfld native int[] ILGEN_0x12984aee::field_0x2
End_Orphan_34:
Start_Orphan_35:
 nop
End_Orphan_35:
 ldc.i4.5
 conv.ovf.i2.un
 ldloc local_0x2
 conv.i2
 ceq
 conv.u1
 newarr [mscorlib]System.Int16
 stsfld int16[] ILGEN_0x12984aee::field_0x5
End_Orphan_31:
Start_Orphan_36:
 ldsflda unsigned int16 ILGEN_0x12984aee::field_0x6
Start_Orphan_37:
 nop
End_Orphan_37:
 ldc.r8 float64(0xc2bd2e08d3657bc1)
 ldsfld float32 ILGEN_0x12984aee::field_0x1
 clt.un
 stind.i2
End_Orphan_36:
 ldc.i4.m1
 ldc.i8 0xf2dcbbd46c92de7
 ldarg Arg_0x3
 add.ovf
 conv.ovf.u.un
 conv.ovf.u1
 clt
 conv.ovf.i8.un
 conv.ovf.u8.un
 add.ovf
 bge Branch_0x2

Start_Orphan_38:
Start_Orphan_39:
Start_Orphan_3a:
 nop
End_Orphan_3a:
 ldsfld int16[] ILGEN_0x12984aee::field_0x5
 stsfld int16[] ILGEN_0x12984aee::field_0x5
End_Orphan_39:
 ldsfld unsigned int8 ILGEN_0x12984aee::field_0x7
 conv.u1
 newarr [mscorlib]System.Double
Start_Orphan_3b:
 ldarg Arg_0x6
 pop
End_Orphan_3b:
 ldarg Arg_0x6
 ldloc local_0x3
 add
 conv.r4
 conv.i
 conv.ovf.u2
 ldelema [mscorlib]System.Double
 pop
End_Orphan_38:
Start_Orphan_3c:
 ldc.i4.2
 starg Arg_0x2
End_Orphan_3c:
Start_Orphan_3d:
 ldsfld int32[] ILGEN_0x12984aee::field_0x4
Start_Orphan_3e:
 nop
End_Orphan_3e:
 ldsfld int32[] ILGEN_0x12984aee::field_0x4
 ldc.i4.5
 ldelem.i4
Start_Orphan_3f:
 nop
End_Orphan_3f:
 ldsfld native int[] ILGEN_0x12984aee::field_0x2
 ldlen
 stelem.i4
End_Orphan_3d:
Start_Orphan_40:
 ldsfld int32[] ILGEN_0x12984aee::field_0x4
 stsfld int32[] ILGEN_0x12984aee::field_0x4
End_Orphan_40:
Start_Orphan_41:
 nop
End_Orphan_41:
 ldsfld int16[] ILGEN_0x12984aee::field_0x5
Start_Orphan_42:
 nop
End_Orphan_42:
 ldloc local_0x3
 conv.u2
 ldelema [mscorlib]System.Int16
 ldind.i2
 conv.ovf.i8.un
Start_Orphan_43:
Start_Orphan_44:
 nop
End_Orphan_44:
 ldc.i4.1
 conv.u8
 conv.u1
 pop
End_Orphan_43:
Start_Orphan_45:
 ldsfld native int[] ILGEN_0x12984aee::field_0x2
 ldsfld unsigned int16 ILGEN_0x12984aee::field_0x6
 ldloc local_0x2
 stelem.i
End_Orphan_45:
Start_Orphan_46:
 nop
End_Orphan_46:
 ldarg Arg_0x3
 conv.u
 conv.ovf.i4.un
 conv.ovf.u8
Start_Orphan_47:
 nop
End_Orphan_47:
 ldarg Arg_0x3
 ldloc local_0x2
 conv.ovf.i8
 or
 conv.ovf.u8.un
 rem
 sub.ovf
 conv.r4
 conv.ovf.i4
 br Branch_0x3

 Branch_0x2:

Start_Orphan_48:
Start_Orphan_49:
Start_Orphan_4a:
 ldarg Arg_0x3
 conv.ovf.u1
 stloc local_0x0
End_Orphan_4a:
Start_Orphan_4b:
 ldarg Arg_0x1
 starg Arg_0x7
End_Orphan_4b:
 ldsfld int16 ILGEN_0x12984aee::field_0x3
 ldloc local_0x2
 sub
 conv.ovf.u8.un
 ldarg Arg_0x3
 conv.ovf.u1.un
Start_Orphan_4c:
 nop
End_Orphan_4c:
 ldarg Arg_0x3
 conv.ovf.u2.un
 shr.un
 shl
 pop
End_Orphan_49:
Start_Orphan_4d:
Start_Orphan_4e:
 ldc.i4.4
 stloc local_0x0
End_Orphan_4e:
Start_Orphan_4f:
 nop
End_Orphan_4f:
 ldsfld unsigned int16 ILGEN_0x12984aee::field_0x6
 conv.i8
 conv.r4
 pop
End_Orphan_4d:
Start_Orphan_50:
 ldc.i8 0x477d136ba713b7c1
 starg Arg_0x3
End_Orphan_50:
 ldloc local_0x2
 conv.r4
 conv.i8
 conv.ovf.u8.un
Start_Orphan_51:
 ldarg Arg_0x5
 pop
End_Orphan_51:
Start_Orphan_52:
 nop
End_Orphan_52:
 ldarg Arg_0x6
 ldarg Arg_0x4
 rem
 conv.ovf.i2.un
 conv.i8
 ceq
Start_Orphan_53:
 ldarg Arg_0x4
 ldarg Arg_0x0
 add
 ldsfld int32[] ILGEN_0x12984aee::field_0x4
 ldlen
 add.ovf.un
 pop
End_Orphan_53:
Start_Orphan_54:
 ldc.i4.s -90
 starg Arg_0x2
End_Orphan_54:
Start_Orphan_55:
 nop
End_Orphan_55:
 ldloc local_0x2
 conv.ovf.u.un
 conv.ovf.u.un
 conv.ovf.u8
 conv.ovf.u1.un
 shr
 pop
End_Orphan_48:
Start_Orphan_56:
 ldarg Arg_0x4
 conv.u8
 ldc.i8 0x1efc036dfbfa78d2
 ldarg Arg_0x5
 shr
 div
 conv.ovf.i
 pop
End_Orphan_56:
Start_Orphan_57:
 nop
End_Orphan_57:
 ldc.r4 float32(0xbd3729bd)
 conv.ovf.u1
 ldc.i4.5
 ldarg Arg_0x5
 add.ovf
 shl
Start_Orphan_58:
 nop
End_Orphan_58:
 ldarg Arg_0x4
 conv.ovf.i4.un
Start_Orphan_59:
 nop
End_Orphan_59:
 ldloc local_0x0
 add.ovf.un
 mul
 conv.r4
 conv.u8
Start_Orphan_5a:
Start_Orphan_5b:
 ldsfld int16[] ILGEN_0x12984aee::field_0x5
 pop
End_Orphan_5b:
Start_Orphan_5c:
 ldsflda float32 ILGEN_0x12984aee::field_0x1
 ldc.r4 float32(0xcd01de59)
 stind.r4
End_Orphan_5c:
Start_Orphan_5d:
 nop
End_Orphan_5d:
 ldc.i8 0xcd54703998527987
 ldc.i8 0xc9920032a749174e
 div
Start_Orphan_5e:
 nop
End_Orphan_5e:
 ldc.i8 0x7c479891545cdac
 ldc.i4.8
 shr
 add.ovf
Start_Orphan_5f:
 nop
End_Orphan_5f:
 ldsfld float32 ILGEN_0x12984aee::field_0x1
 call float64 [mscorlib]System.Math::Abs(float64)
 conv.i8
 rem
 starg Arg_0x3
End_Orphan_5a:
Start_Orphan_60:
 ldsflda int32 ILGEN_0x12984aee::field_0x0
 ldc.i8 0x8378245d86ca9f14
 conv.i1
 stind.i4
End_Orphan_60:
 ldc.i4.8
 conv.ovf.i.un
 conv.i8
 rem
 conv.ovf.i4
 Branch_0x3:

 ldc.i4.5
 blt Branch_0x0

 ldarg Arg_0x3
 not
 conv.ovf.u4.un
 br Branch_0x1

 Branch_0x0:

Start_Orphan_61:
Start_Orphan_62:
Start_Orphan_63:
 nop
End_Orphan_63:
 ldarg Arg_0x3
 conv.i1
 starg Arg_0x5
End_Orphan_62:
 ldc.i4.5
 conv.u1
 newarr [mscorlib]System.Int32
 pop
End_Orphan_61:
Start_Orphan_64:
Start_Orphan_65:
Start_Orphan_66:
 nop
End_Orphan_66:
 ldsflda float32 ILGEN_0x12984aee::field_0x1
 ldsfld float32 ILGEN_0x12984aee::field_0x1
 stind.r4
End_Orphan_65:
Start_Orphan_67:
 nop
End_Orphan_67:
 ldarg Arg_0x7
 ldloc local_0x2
 sub
 conv.ovf.i2.un
 conv.ovf.i4
 stsfld unsigned int16 ILGEN_0x12984aee::field_0x6
End_Orphan_64:
Start_Orphan_68:
Start_Orphan_69:
 ldsfld int32[] ILGEN_0x12984aee::field_0x4
 stsfld int32[] ILGEN_0x12984aee::field_0x4
End_Orphan_69:
Start_Orphan_6a:
 nop
End_Orphan_6a:
 ldloc local_0x1
Start_Orphan_6b:
 nop
End_Orphan_6b:
 ldarg Arg_0x6
 conv.u1
 ldelema [mscorlib]System.Double
 pop
End_Orphan_68:
 ldloc local_0x2
 ldarg Arg_0x4
 div.un
 conv.ovf.i8
Start_Orphan_6c:
 ldsfld int32[] ILGEN_0x12984aee::field_0x4
 ldarg Arg_0x5
 ldc.i4.m1
 stelem.i4
End_Orphan_6c:
 ldc.i8 0x2aca7e3a8cd8eb7e
 ldc.i8 0x9dd1f8eae6277a8
 cgt.un
 conv.ovf.u8
 sub.ovf
Start_Orphan_6d:
Start_Orphan_6e:
 nop
End_Orphan_6e:
 ldloc local_0x0
 conv.u1
 newarr [mscorlib]System.IntPtr
 stsfld native int[] ILGEN_0x12984aee::field_0x2
End_Orphan_6d:
Start_Orphan_6f:
 ldsflda int32 ILGEN_0x12984aee::field_0x0
 ldc.i4.3
 stind.i
End_Orphan_6f:
Start_Orphan_70:
 nop
End_Orphan_70:
 ldsflda float32 ILGEN_0x12984aee::field_0x1
 ldind.r4
 conv.i8
 add
Start_Orphan_71:
Start_Orphan_72:
 nop
End_Orphan_72:
 ldc.i8 0x990a3943df1280cc
 conv.ovf.u2
 conv.u1
 newarr [mscorlib]System.Double
 stloc local_0x1
End_Orphan_71:
Start_Orphan_73:
 ldsflda int32 ILGEN_0x12984aee::field_0x0
 pop
End_Orphan_73:
Start_Orphan_74:
 nop
End_Orphan_74:
 ldc.i8 0xfbfa7b7591876812
 conv.ovf.i.un
 conv.i8
Start_Orphan_75:
 ldarga Arg_0x5
 pop
End_Orphan_75:
Start_Orphan_76:
 nop
End_Orphan_76:
 ldarg Arg_0x3
 ldc.i8 0x622c10fb3c5d6a41
 sub
Start_Orphan_77:
 nop
End_Orphan_77:
 ldc.i4.0
 conv.i8
 sub.ovf
 div
 ldloc local_0x2
 conv.r8
 ckfinite
 conv.ovf.i8
 rem.un
 or
 conv.r4
 call float64 [mscorlib]System.Math::Sin(float64)
Start_Orphan_78:
Start_Orphan_79:
Start_Orphan_7a:
 nop
End_Orphan_7a:
 ldsfld float32 ILGEN_0x12984aee::field_0x1
 conv.ovf.i
 pop
End_Orphan_79:
 ldloca local_0x0
Start_Orphan_7b:
 ldsflda float32 ILGEN_0x12984aee::field_0x1
 pop
End_Orphan_7b:
 ldc.i8 0x605417debba8f3a7
 neg
 conv.ovf.i1.un
 conv.i1
 stind.i1
End_Orphan_78:
Start_Orphan_7c:
 ldloca local_0x2
 pop
End_Orphan_7c:
 ldloc local_0x1
Start_Orphan_7d:
Start_Orphan_7e:
 nop
End_Orphan_7e:
 ldc.i4.6
 conv.u1
 newarr [mscorlib]System.Int32
Start_Orphan_7f:
 nop
End_Orphan_7f:
 ldc.i4.6
 ldelema [mscorlib]System.Int32
 pop
End_Orphan_7d:
Start_Orphan_80:
 ldloca local_0x3
 ldc.i4.4
 stind.i2
End_Orphan_80:
Start_Orphan_81:
 nop
End_Orphan_81:
 ldloc local_0x2
 ldc.i4.8
 cgt
Start_Orphan_82:
 nop
End_Orphan_82:
 ldc.i4 0x6be3bfb8
 ldloc local_0x2
 sub
 sub
 ldc.i4.1
 ldc.i4.4
 and
 conv.u
 clt.un
 conv.ovf.u2
 ldelema [mscorlib]System.Double
 ldind.r8
 call float64 [mscorlib]System.Math::Sqrt(float64)
 cgt
 Branch_0x1:

EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0xf98ed19f 
ldc.i4 0x54f25e16 
ldc.i4 0x128b1b5b 
ldc.i8 0x56189b78bd6fa3a1 
ldc.i4 0xa3d03d12 conv.i
ldc.i4 0x1ca75d34 
ldc.i4 0xb984efe6 conv.i
ldc.i4 0xd077e11f 

call int32 ILGEN_0x12984aee::Method_0x786cd2b5(int8 Arg_0x0, unsigned int8 Arg_0x1, int8 Arg_0x2, int64 Arg_0x3, native int Arg_0x4, int16 Arg_0x5, native int Arg_0x6, unsigned int32 Arg_0x7)
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
