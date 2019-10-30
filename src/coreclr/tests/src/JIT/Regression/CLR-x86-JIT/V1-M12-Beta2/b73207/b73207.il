// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b73207 { }
.class ILGEN_0x2c1d0308 {

//BEGIN STATIC FIELDS
.field static	int8 field_0x0
.field static	unsigned int16 field_0x1
.field static	float64 field_0x2
//END STATIC FIELDS
.method static int32 Method_0x286335f2(float64 Arg_0x0, int32 Arg_0x1, native int Arg_0x2, unsigned int64 Arg_0x3, unsigned int64 Arg_0x4, unsigned int32 Arg_0x5, unsigned int16 Arg_0x6, float32 Arg_0x7, int64 Arg_0x8) {
.maxstack	17

 .locals (float64 local_0x0,float64 local_0x1,unsigned int16 local_0x2,unsigned int8 local_0x3,unsigned int8[] local_0x4,float64 local_0x5,int16 local_0x6,unsigned int16 local_0x7,unsigned int32 local_0x8,unsigned int32 local_0x9,unsigned int32[] local_0xa,float32 local_0xb,unsigned int32[] local_0xc,unsigned int64 local_0xd,int64 local_0xe)
//BEGIN INIT LOCALS
ldc.r8 float64(0xbdbfb0e8c92ea6de) 
stloc local_0x0
ldc.r8 float64(0xee58336c3d951cfe) 
stloc local_0x1
ldc.i4 0x8ac3299e 
stloc local_0x2
ldc.i4 0x7fcb0aab 
stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x4
ldc.r8 float64(0x9a2ac9e7579c83b5) 
stloc local_0x5
ldc.i4 0x1191c035 
stloc local_0x6
ldc.i4 0x6e0320cc 
stloc local_0x7
ldc.i4 0x37ea3451 
stloc local_0x8
ldc.i4 0x3c33ab3a 
stloc local_0x9
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0xa
ldc.r4 float32(0xd520f90f) 
stloc local_0xb
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0xc
ldc.i8 0xb68cbbeb18063c18 
stloc local_0xd
ldc.i8 0xf9684721 
stloc local_0xe

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0x5583f117 
stsfld int8 ILGEN_0x2c1d0308::field_0x0
ldc.i4 0xf29708e5 
stsfld unsigned int16 ILGEN_0x2c1d0308::field_0x1
ldc.r8 float64(0xb21ff0ba25c40f3c) 
stsfld float64 ILGEN_0x2c1d0308::field_0x2

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
 nop
End_Orphan_2:
 ldsflda float64 ILGEN_0x2c1d0308::field_0x2
 pop
End_Orphan_1:
 ldc.i4 0x6014ae39
 ldarg Arg_0x2
 cgt
 ldarg Arg_0x2
 ldloc local_0x2
 bge Branch_0x8

 ldc.i4.6
 br Branch_0x9

 Branch_0x8:

 ldarg Arg_0x5
 Branch_0x9:

 blt Branch_0x6

 ldloc local_0xe
 br Branch_0x7

 Branch_0x6:

 ldarg Arg_0x2
 conv.ovf.i8.un
 Branch_0x7:

 ldarg Arg_0x2
 ldc.i4.m1
 rem
 conv.u8
 bgt.un Branch_0x4

Start_Orphan_3:
 nop
End_Orphan_3:
 ldarg Arg_0x2
 ldc.i4.7
 rem
Start_Orphan_4:
 nop
End_Orphan_4:
 ldarg Arg_0x8
 conv.ovf.u
 add.ovf.un
 br Branch_0x5

 Branch_0x4:

Start_Orphan_5:
 ldarga Arg_0x8
 ldloc local_0xd
 stind.i8
End_Orphan_5:
 ldarg Arg_0x2
 neg
 conv.ovf.i4.un
 Branch_0x5:

Start_Orphan_6:
 ldloca local_0xd
 ldloc local_0xd
 stind.i8
End_Orphan_6:
Start_Orphan_7:
 nop
End_Orphan_7:
 ldloc local_0xe
 ldarg Arg_0x8
 mul.ovf.un
 conv.ovf.i4.un
Start_Orphan_8:
 ldc.i4.5
 stsfld unsigned int16 ILGEN_0x2c1d0308::field_0x1
End_Orphan_8:
 ldarg Arg_0x2
 ldloc local_0x8
 ceq
 ldarg Arg_0x2
 conv.ovf.u
 mul.ovf.un
 sub.ovf
 bgt Branch_0x2

 ldloca local_0x3
Start_Orphan_9:
 ldloc local_0xc
 pop
End_Orphan_9:
Start_Orphan_a:
 nop
End_Orphan_a:
 ldloc local_0x8
 neg
 conv.ovf.u1
 stind.i1
 br Branch_0x3

 Branch_0x2:

Start_Orphan_b:
 ldarga Arg_0x6
 ldsfld unsigned int16 ILGEN_0x2c1d0308::field_0x1
 stind.i2
End_Orphan_b:
 ldloc local_0xd
 conv.u
 ldc.i4.2
 clt
 starg Arg_0x6
 Branch_0x3:

End_Orphan_0:
Start_Orphan_c:
 ldloc local_0x6
 pop
End_Orphan_c:
Start_Orphan_d:
 nop
End_Orphan_d:
 ldloc local_0x4
 ldc.i4.8
 ldelem.u1
 ldloc local_0x6
 ldc.i4.2
 rem.un
 shr
Start_Orphan_e:
 nop
End_Orphan_e:
 ldloc local_0x1
 conv.ovf.u8
 conv.ovf.u.un
 add.ovf.un
 conv.ovf.i1
Start_Orphan_f:
 ldloc local_0x4
 stloc local_0x4
End_Orphan_f:
Start_Orphan_10:
Start_Orphan_11:
 nop
End_Orphan_11:
 ldarg Arg_0x2
 ldc.i4 0xaadbbbc3
 clt.un
 starg Arg_0x5
End_Orphan_10:
 ldloca local_0x6
 ldind.i2
 or
Start_Orphan_12:
 ldc.r8 float64(0xf563bdb7d67a4a00)
 ldc.i4.2
 conv.r4
 call float64 [mscorlib]System.Math::Sqrt(float64)
 mul 
 stloc local_0x0
End_Orphan_12:
Start_Orphan_13:
 ldloc local_0x4
 ldloc local_0x3
 ldelema [mscorlib]System.Byte
 pop
End_Orphan_13:
 ldloc local_0xb
 call float64 [mscorlib]System.Math::Round(float64)
 conv.u
 ldloc local_0x2
 conv.i4
 div
 conv.ovf.i1.un
 sub.ovf.un
Start_Orphan_14:
 ldsflda unsigned int16 ILGEN_0x2c1d0308::field_0x1
 ldarg Arg_0x2
 ldsfld unsigned int16 ILGEN_0x2c1d0308::field_0x1
 rem.un
 conv.ovf.u1
Start_Orphan_15:
 ldloca local_0x6
 pop
End_Orphan_15:
Start_Orphan_16:
 nop
End_Orphan_16:
 ldloc local_0x6
 conv.r.un
 conv.ovf.i2
 clt
 stind.i2
End_Orphan_14:
Start_Orphan_17:
 ldarg Arg_0x4
 pop
End_Orphan_17:
Start_Orphan_18:
 ldarga Arg_0x4
 ldloc local_0xd
 stind.i8
End_Orphan_18:
Start_Orphan_19:
 nop
End_Orphan_19:
 ldloc local_0x0
 conv.ovf.u2
Start_Orphan_1a:
 ldc.i4.s -114
 stloc local_0x6
End_Orphan_1a:
Start_Orphan_1b:
 nop
End_Orphan_1b:
 ldarg Arg_0x2
 ldc.i4.4
 mul.ovf
 ldarg Arg_0x2
 ldc.i4.1
 div
 mul.ovf.un
 mul.ovf.un
Start_Orphan_1c:
 ldloca local_0x8
 ldc.i4.s -6
 stind.i4
End_Orphan_1c:
Start_Orphan_1d:
 ldloc local_0xc
 ldloc local_0x7
 ldc.i4.5
 stelem.i4
End_Orphan_1d:
Start_Orphan_1e:
 nop
End_Orphan_1e:
 ldarg Arg_0x2
 ldloc local_0x9
 sub.ovf.un
 ldarg Arg_0x7
 ldsfld float64 ILGEN_0x2c1d0308::field_0x2
 clt.un
 mul.ovf
Start_Orphan_1f:
 ldloc local_0x8
 stloc local_0x8
End_Orphan_1f:
 ldarg Arg_0x0
 call float64 [mscorlib]System.Math::Abs(float64)
 conv.ovf.i
 div
 cgt.un
 conv.ovf.i8
 conv.ovf.i1.un
 rem
Start_Orphan_20:
Start_Orphan_21:
Start_Orphan_22:
 ldarg Arg_0x8
 stloc local_0xe
End_Orphan_22:
 ldc.i4.0
 stsfld int8 ILGEN_0x2c1d0308::field_0x0
End_Orphan_21:
Start_Orphan_23:
Start_Orphan_24:
 nop
End_Orphan_24:
 ldc.i4.5
 ldc.i4.2
 shr.un
 starg Arg_0x6
End_Orphan_23:
 ldloc local_0xe
 ldarg Arg_0x3
 clt
Start_Orphan_25:
 ldloc local_0xc
 ldc.i4.2
 ldelema [mscorlib]System.UInt32
 pop
End_Orphan_25:
Start_Orphan_26:
 ldsflda float64 ILGEN_0x2c1d0308::field_0x2
 ldloc local_0x5
 stind.r8
End_Orphan_26:
 ldarg Arg_0x2
 ldarg Arg_0x2
 cgt.un
 conv.ovf.i1.un
 ldloc local_0xa
 ldc.i4.m1
 ldelem.u4
 conv.ovf.i1.un
 or
 mul
 pop
End_Orphan_20:
Start_Orphan_27:
 ldc.i4.0
 stloc local_0x7
End_Orphan_27:
Start_Orphan_28:
 ldarg Arg_0x2
 conv.ovf.i2.un
 starg Arg_0x2
End_Orphan_28:
 ldarg Arg_0x4
 conv.ovf.i4.un
 conv.ovf.i8
Start_Orphan_29:
 ldarga Arg_0x8
 ldarg Arg_0x4
 stind.i8
End_Orphan_29:
Start_Orphan_2a:
 nop
End_Orphan_2a:
 ldarg Arg_0x2
 ldarg Arg_0x2
 add
 conv.ovf.u4
 shl
 conv.ovf.i4
 conv.ovf.u4.un
Start_Orphan_2b:
Start_Orphan_2c:
 ldloca local_0x3
 pop
End_Orphan_2c:
 ldloca local_0x9
 pop
End_Orphan_2b:
Start_Orphan_2d:
 ldarg Arg_0x6
 conv.u1
 newarr [mscorlib]System.UInt32
 pop
End_Orphan_2d:
Start_Orphan_2e:
 nop
End_Orphan_2e:
 ldc.i4.6
 ldarg Arg_0x2
 clt
 conv.ovf.i1.un
 ldloc local_0x3
 conv.ovf.u4.un
 conv.i
 add.ovf
Start_Orphan_2f:
 nop
End_Orphan_2f:
 ldarg Arg_0x2
 ldc.i4 0x6447ae64
 sub.ovf
 ldloc local_0xc
 ldlen
 add
Start_Orphan_30:
 ldc.i4.8
 starg Arg_0x6
End_Orphan_30:
 ldsflda int8 ILGEN_0x2c1d0308::field_0x0
 ldind.i1
Start_Orphan_31:
 nop
End_Orphan_31:
 ldarg Arg_0x1
 clt.un
 add
 div.un
 conv.ovf.i4.un
 sub.ovf
 not
 blt Branch_0x0

Start_Orphan_32:
Start_Orphan_33:
 ldc.r4 float32(0x1bf9e3a)
 pop
End_Orphan_33:
 ldloca local_0x6
 pop
End_Orphan_32:
 ldc.i4.2
Start_Orphan_34:
 ldarga Arg_0x7
 pop
End_Orphan_34:
Start_Orphan_35:
 ldloca local_0x8
 ldloc local_0x7
 stind.i4
End_Orphan_35:
 ldarg Arg_0x6
 conv.ovf.i1.un
 conv.ovf.u4
 conv.ovf.u.un
 conv.u8
Start_Orphan_36:
Start_Orphan_37:
 nop
End_Orphan_37:
 ldsflda float64 ILGEN_0x2c1d0308::field_0x2
 ldloc local_0x5
 stind.r8
End_Orphan_36:
Start_Orphan_38:
 nop
End_Orphan_38:
 ldloc local_0xb
 conv.i8
Start_Orphan_39:
 nop
End_Orphan_39:
 ldc.i8 0x8978fd0136b8363a
 conv.ovf.i8
 add.ovf
Start_Orphan_3a:
 ldloc local_0x4
 pop
End_Orphan_3a:
Start_Orphan_3b:
 nop
End_Orphan_3b:
 ldarga Arg_0x5
 ldind.u4
 shl
 ldarg Arg_0x3
 conv.u1
 ldarg Arg_0x2
 conv.i
 div.un
 conv.i2
 shl
 sub.ovf
Start_Orphan_3c:
 ldloca local_0x9
 ldc.i4 0xa15e8e3b
 stind.i4
End_Orphan_3c:
Start_Orphan_3d:
 ldloca local_0x5
 ldloc local_0xb
 stind.r8
End_Orphan_3d:
Start_Orphan_3e:
 nop
End_Orphan_3e:
 ldc.i4.7
 conv.r8
 call float64 [mscorlib]System.Math::Round(float64)
 conv.i
 conv.i8
Start_Orphan_3f:
 ldloc local_0x4
 stloc local_0x4
End_Orphan_3f:
Start_Orphan_40:
Start_Orphan_41:
 nop
End_Orphan_41:
 ldsflda int8 ILGEN_0x2c1d0308::field_0x0
 ldc.i4 0xb8136e35
 ldarg Arg_0x6
 shr.un
 stind.i1
End_Orphan_40:
Start_Orphan_42:
 ldc.r4 float32(0xaf7b092b)
 pop
End_Orphan_42:
 ldarga Arg_0x4
 ldind.u8
 conv.ovf.i8
 not
 add.ovf
 clt.un
 rem
 br Branch_0x1

 Branch_0x0:

Start_Orphan_43:
Start_Orphan_44:
Start_Orphan_45:
Start_Orphan_46:
 nop
End_Orphan_46:
 ldloc local_0x4
 ldarg Arg_0x5
 ldarg Arg_0x2
 ldc.i4.5
 ceq
 stelem.i1
End_Orphan_45:
Start_Orphan_47:
 ldloca local_0x6
 pop
End_Orphan_47:
 ldarg Arg_0x2
 conv.r8
 call float64 [mscorlib]System.Math::Abs(float64)
Start_Orphan_48:
 ldarg Arg_0x8
 starg Arg_0x8
End_Orphan_48:
Start_Orphan_49:
 nop
End_Orphan_49:
 ldc.r8 float64(0x5886e7055b11223b)
 call float64 [mscorlib]System.Math::Sin(float64)
 conv.r4
 mul 
 pop
End_Orphan_44:
 ldarga Arg_0x0
Start_Orphan_4a:
Start_Orphan_4b:
 nop
End_Orphan_4b:
 ldarga Arg_0x5
 pop
End_Orphan_4a:
Start_Orphan_4c:
 nop
End_Orphan_4c:
 ldarg Arg_0x2
 ldarg Arg_0x2
 div.un
 conv.u
 conv.r4
 call float64 [mscorlib]System.Math::Cos(float64)
Start_Orphan_4d:
 ldarg Arg_0x6
 stsfld int8 ILGEN_0x2c1d0308::field_0x0
End_Orphan_4d:
 ldloc local_0x8
 conv.ovf.u2
 conv.r8
 add 
 stind.r8
End_Orphan_43:
 ldarg Arg_0x6
Start_Orphan_4e:
Start_Orphan_4f:
 ldloca local_0x6
 pop
End_Orphan_4f:
 ldarg Arg_0x0
 conv.r8
Start_Orphan_50:
Start_Orphan_51:
 nop
End_Orphan_51:
 ldarga Arg_0x4
 ldarg Arg_0x8
 ldarg Arg_0x8
 sub.ovf.un
 stind.i8
End_Orphan_50:
Start_Orphan_52:
 nop
End_Orphan_52:
 ldc.i4.8
 conv.ovf.u.un
 conv.r4
Start_Orphan_53:
 nop
End_Orphan_53:
 ldc.i4.s -25
 conv.r4
 neg
 add 
 rem 
 pop
End_Orphan_4e:
Start_Orphan_54:
 ldloca local_0x3
 pop
End_Orphan_54:
 ldarg Arg_0x4
 ldloc local_0xe
 add.ovf
 conv.ovf.i2.un
 conv.ovf.i8.un
 conv.r.un
 conv.u1
 conv.ovf.i4
 sub.ovf.un
 Branch_0x1:

EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.r8 float64(0xc0eb60643e9def00) 
ldc.i4 0xce9dc582 
ldc.i4 0x3be5779c conv.i
ldc.i8 0xd8fd7b21754a89f4 
ldc.i8 0xcfe934ec2c8a7e29 
ldc.i4 0x8347bc89 
ldc.i4 0xaf31dad2 
ldc.r4 float32(0x1b3114c7) 
ldc.i8 0x96344796c9c5f316 

call int32 ILGEN_0x2c1d0308::Method_0x286335f2(float64 Arg_0x0, int32 Arg_0x1, native int Arg_0x2, unsigned int64 Arg_0x3, unsigned int64 Arg_0x4, unsigned int32 Arg_0x5, unsigned int16 Arg_0x6, float32 Arg_0x7, int64 Arg_0x8)
 leave.s IL_FAIL
} catch [mscorlib]System.OverflowException {
leave.s IL_END
}
IL_END:
ldc.i4.s 100
ret
IL_FAIL:
ldc.i4.s 101
ret


}

}
