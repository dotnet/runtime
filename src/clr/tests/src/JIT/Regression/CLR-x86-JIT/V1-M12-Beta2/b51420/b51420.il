// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xbc077bd {}
.class ILGEN_0xbc077bd {

//BEGIN STATIC FIELDS
.field static	unsigned int8[] field_0x0
.field static	unsigned int64[] field_0x1
.field static	unsigned int32[] field_0x2
.field static	unsigned int64 field_0x3
.field static	unsigned int8 field_0x4
.field static	unsigned int32 field_0x5
.field static	unsigned int16 field_0x6
.field static	int8[] field_0x7
//END STATIC FIELDS
.method static int32 Method_0xa67f86e1(unsigned int64 Arg_0x0, float64 Arg_0x1, int16 Arg_0x2, int32 Arg_0x3, float32 Arg_0x4, unsigned int32 Arg_0x5, int16 Arg_0x6, unsigned int32 Arg_0x7, unsigned int32 Arg_0x8) {
.maxstack	15

 .locals (float64[] local_0x0,unsigned int16 local_0x1,int8[] local_0x2,float32 local_0x3,unsigned int8[] local_0x4,unsigned int16 local_0x5,float64 local_0x6,float32[] local_0x7,float64 local_0x8)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x0
ldc.i4 0x778b35c4 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x2
ldc.r4 float32(0xcb9e538) 
stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x4
ldc.i4 0x36f4aeee 
stloc local_0x5
ldc.r8 float64(0xf2d16469981244b9) 
stloc local_0x6
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x7
ldc.r8 float64(0x2c8c074f6947b871) 
stloc local_0x8

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0xbc077bd::field_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0xbc077bd::field_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0xbc077bd::field_0x2
ldc.i8 0x5c865dca7dd07e0e 
stsfld unsigned int64 ILGEN_0xbc077bd::field_0x3
ldc.i4 0xe708cd84 
stsfld unsigned int8 ILGEN_0xbc077bd::field_0x4
ldc.i4 0x2d97448 
stsfld unsigned int32 ILGEN_0xbc077bd::field_0x5
ldc.i4 0x792ae58c 
stsfld unsigned int16 ILGEN_0xbc077bd::field_0x6
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0xbc077bd::field_0x7

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
 ldc.r4 float32(0xcce5bd7e)
 stloc local_0x3
End_Orphan_1:
Start_Orphan_2:
 ldsfld unsigned int64 ILGEN_0xbc077bd::field_0x3
 brfalse Branch_0x6

 nop
 br Branch_0x7

 Branch_0x6:

 nop
 Branch_0x7:

End_Orphan_2:
 ldc.i4.2
 ldloc local_0x1
 mul.ovf.un
Start_Orphan_3:
 nop
End_Orphan_3:
 ldarg Arg_0x0
 conv.ovf.i
 blt Branch_0x8

Start_Orphan_4:
 nop
End_Orphan_4:
 ldloc local_0x1
 conv.i1
 br Branch_0x9

 Branch_0x8:

Start_Orphan_5:
 nop
End_Orphan_5:
 ldc.i4.7
 conv.ovf.u2.un
 Branch_0x9:

 ldsfld unsigned int32 ILGEN_0xbc077bd::field_0x5
 ldsfld unsigned int32 ILGEN_0xbc077bd::field_0x5
 mul.ovf
 conv.u
 sub.ovf
Start_Orphan_6:
 nop
End_Orphan_6:
 ldarg Arg_0x4
 conv.ovf.u1
 conv.ovf.u
 ldloc local_0x1
 ldc.i4 0x5ffa72ae
 sub
 ldc.i8 0xbf9b80f7489b0cb5
 ldsfld unsigned int64 ILGEN_0xbc077bd::field_0x3
 clt.un
 sub
 or
 bge Branch_0x4

Start_Orphan_7:
 ldsfld unsigned int32[] ILGEN_0xbc077bd::field_0x2
 ldarg Arg_0x4
 ldc.r4 float32(0xe3f49ca3)
 clt.un
 ldc.i4.2
 conv.i2
 stelem.i4
End_Orphan_7:
Start_Orphan_8:
 ldc.i4.6
 starg Arg_0x3
End_Orphan_8:
 ldc.i4.0
 ldsfld unsigned int16 ILGEN_0xbc077bd::field_0x6
 cgt
 conv.ovf.i2
Start_Orphan_9:
 nop
End_Orphan_9:
 ldc.i4.0
 conv.ovf.u
 conv.u1
 rem.un
 br Branch_0x5

 Branch_0x4:

Start_Orphan_a:
 ldsfld int8[] ILGEN_0xbc077bd::field_0x7
 stsfld int8[] ILGEN_0xbc077bd::field_0x7
End_Orphan_a:
 ldc.i4.6
 conv.i4
 Branch_0x5:

Start_Orphan_b:
 ldsfld int8[] ILGEN_0xbc077bd::field_0x7
 stloc local_0x2
End_Orphan_b:
Start_Orphan_c:
 nop
End_Orphan_c:
 ldsfld unsigned int16 ILGEN_0xbc077bd::field_0x6
 ldloc local_0x1
 rem.un
 ldsfld unsigned int32 ILGEN_0xbc077bd::field_0x5
 conv.ovf.u
 rem.un
 not
Start_Orphan_d:
 ldc.i4.2
 conv.i2
 stsfld unsigned int8 ILGEN_0xbc077bd::field_0x4
End_Orphan_d:
 ldarg Arg_0x2
 ldc.i4.7
 sub.ovf
Start_Orphan_e:
 nop
End_Orphan_e:
 ldarg Arg_0x0
 conv.ovf.i.un
 sub.ovf
Start_Orphan_f:
 nop
End_Orphan_f:
 ldc.i4.s -125
 ldloc local_0x5
 sub.ovf
 conv.ovf.i4.un
 sub.ovf.un
 add
 bge.un Branch_0x2

Start_Orphan_10:
Start_Orphan_11:
 ldarga Arg_0x5
 pop
End_Orphan_11:
 ldarga Arg_0x0
Start_Orphan_12:
 ldloc local_0x8
 stloc local_0x8
End_Orphan_12:
Start_Orphan_13:
 nop
End_Orphan_13:
 ldarg Arg_0x4
 conv.ovf.u8
Start_Orphan_14:
 nop
End_Orphan_14:
 ldsfld unsigned int32 ILGEN_0xbc077bd::field_0x5
 conv.u8
 xor
 stind.i8
End_Orphan_10:
 ldc.i4 0x60e66acf
 ldc.i4.m1
 mul.ovf.un
 not
 ldarg Arg_0x0
 ldsfld unsigned int64 ILGEN_0xbc077bd::field_0x3
 sub.ovf
 conv.ovf.u.un
 add.ovf
Start_Orphan_15:
 ldc.i4.s -5
 stsfld unsigned int32 ILGEN_0xbc077bd::field_0x5
End_Orphan_15:
 ldc.i4 0x63444501
 ldc.i4.1
 add.ovf.un
 ldarg Arg_0x3
 ldc.i4 0xbfd9badb
 sub.ovf
 and
Start_Orphan_16:
 ldloc local_0x7
 stloc local_0x7
End_Orphan_16:
Start_Orphan_17:
 nop
End_Orphan_17:
 ldloc local_0x1
 ldc.i4.1
 shr.un
Start_Orphan_18:
 nop
End_Orphan_18:
 ldc.i4 0x32c57ece
 ldc.i4.m1
 clt.un
 div
 add
 div
 br Branch_0x3

 Branch_0x2:

Start_Orphan_19:
 ldsfld unsigned int8[] ILGEN_0xbc077bd::field_0x0
Start_Orphan_1a:
 ldloc local_0x8
 pop
End_Orphan_1a:
 ldsfld unsigned int64 ILGEN_0xbc077bd::field_0x3
 conv.r.un
Start_Orphan_1b:
 nop
End_Orphan_1b:
 ldc.r4 float32(0x38578570)
 call float64 [mscorlib]System.Math::Sin(float64)
 clt.un
Start_Orphan_1c:
 nop
End_Orphan_1c:
 ldc.i4.s -89
Start_Orphan_1d:
 nop
End_Orphan_1d:
 ldarg Arg_0x0
 ldsfld unsigned int64 ILGEN_0xbc077bd::field_0x3
 clt
 shl
 stelem.i1
End_Orphan_19:
Start_Orphan_1e:
 ldarg Arg_0x7
 conv.u1
 newarr [mscorlib]System.Single
 stloc local_0x7
End_Orphan_1e:
Start_Orphan_1f:
 ldc.i4.7
 stsfld unsigned int8 ILGEN_0xbc077bd::field_0x4
End_Orphan_1f:
 ldsflda unsigned int16 ILGEN_0xbc077bd::field_0x6
 ldind.u2
Start_Orphan_20:
 ldloca local_0x1
 pop
End_Orphan_20:
Start_Orphan_21:
 nop
End_Orphan_21:
 ldarg Arg_0x7
 ldc.i4.7
 div.un
 conv.ovf.i.un
Start_Orphan_22:
 nop
End_Orphan_22:
 ldarg Arg_0x0
 conv.ovf.u8.un
 conv.ovf.i1.un
 sub.ovf.un
 mul.ovf
 Branch_0x3:

Start_Orphan_23:
Start_Orphan_24:
 ldloc local_0x4
 pop
End_Orphan_24:
 ldsfld int8[] ILGEN_0xbc077bd::field_0x7
 stloc local_0x2
End_Orphan_23:
Start_Orphan_25:
Start_Orphan_26:
 nop
End_Orphan_26:
 ldsfld unsigned int8[] ILGEN_0xbc077bd::field_0x0
 stsfld unsigned int8[] ILGEN_0xbc077bd::field_0x0
End_Orphan_25:
Start_Orphan_27:
 nop
End_Orphan_27:
 ldc.i4.1
 conv.ovf.i4
 conv.r4
 neg
 conv.ovf.u
Start_Orphan_28:
 nop
End_Orphan_28:
 ldloc local_0x3
 conv.ovf.u8
 conv.ovf.i2
Start_Orphan_29:
Start_Orphan_2a:
 nop
End_Orphan_2a:
 ldsfld unsigned int32[] ILGEN_0xbc077bd::field_0x2
 stsfld unsigned int32[] ILGEN_0xbc077bd::field_0x2
End_Orphan_29:
Start_Orphan_2b:
 ldsfld unsigned int8[] ILGEN_0xbc077bd::field_0x0
 pop
End_Orphan_2b:
 ldc.r4 float32(0xe259773e)
 conv.u1
 shr
 ceq
 bgt Branch_0x0

Start_Orphan_2c:
 ldarga Arg_0x1
 pop
End_Orphan_2c:
 ldloc local_0x5
 ldsfld unsigned int8 ILGEN_0xbc077bd::field_0x4
 sub.ovf
 conv.ovf.i8.un
 conv.r.un
Start_Orphan_2d:
 nop
End_Orphan_2d:
 ldc.r8 float64(0x93e38957ed27a76f)
 call float64 [mscorlib]System.Math::Cos(float64)
 conv.i4
 conv.r.un
 mul 
 starg Arg_0x4
 br Branch_0x1

 Branch_0x0:

Start_Orphan_2e:
Start_Orphan_2f:
 ldarga Arg_0x0
 ldsfld unsigned int64 ILGEN_0xbc077bd::field_0x3
 stind.i8
End_Orphan_2f:
 ldc.r8 float64(0x9d262155ba17f4ab)
 call float32 [mscorlib]System.Math::Abs(float32)
 stloc local_0x8
End_Orphan_2e:
 ldc.r4 float32(0x9e104e01)
 ldc.r4 float32(0xefdb95eb)
 conv.r8
 call float64 [mscorlib]System.Math::Sin(float64)
 clt
 conv.r8
 stloc local_0x6
 Branch_0x1:

End_Orphan_0:
Start_Orphan_30:
 nop
End_Orphan_30:
 ldsfld unsigned int32 ILGEN_0xbc077bd::field_0x5
 ldc.i4.2
 sub
 conv.r4
 ckfinite
Start_Orphan_31:
 nop
End_Orphan_31:
 ldc.r8 float64(0x1852c3d1133d3722)
 conv.r8
 conv.r4
 sub  
 call float64 [mscorlib]System.Math::Abs(float64)
Start_Orphan_32:
Start_Orphan_33:
 ldsfld unsigned int32[] ILGEN_0xbc077bd::field_0x2
 pop
End_Orphan_33:
Start_Orphan_34:
 nop
End_Orphan_34:
 ldloc local_0x0
 ldlen
 conv.u1
 newarr [mscorlib]System.SByte
 stsfld int8[] ILGEN_0xbc077bd::field_0x7
End_Orphan_32:
Start_Orphan_35:
 ldloc local_0x6
 starg Arg_0x4
End_Orphan_35:
 ldc.i4.7
 conv.r4
 call float64 [mscorlib]System.Math::Round(float64)
Start_Orphan_36:
 ldloc local_0x6
 ldloc local_0x3
 mul 
 starg Arg_0x1
End_Orphan_36:
Start_Orphan_37:
 ldc.i4.8
 pop
End_Orphan_37:
 ldloc local_0x7
Start_Orphan_38:
 nop
End_Orphan_38:
 ldc.r8 float64(0x297b1492be2ee485)
 conv.i4
 ldelema [mscorlib]System.Single
 ldind.r4
 call float64 [mscorlib]System.Math::Sin(float64)
 rem 
 sub  
 conv.u1
EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i8 0x56e2f28dfaf9a75a 
ldc.r8 float64(0x031cfeed7ccc2023) 
ldc.i4 0x3b5c5902 
ldc.i4 0xee7fff68 
ldc.r4 float32(0x5b41ce5a) 
ldc.i4 0xd62f6a01 
ldc.i4 0xa599e842 
ldc.i4 0x7030bcd0 
ldc.i4 0xa763e299 

call int32 ILGEN_0xbc077bd::Method_0xa67f86e1(unsigned int64 Arg_0x0, float64 Arg_0x1, int16 Arg_0x2, int32 Arg_0x3, float32 Arg_0x4, unsigned int32 Arg_0x5, int16 Arg_0x6, unsigned int32 Arg_0x7, unsigned int32 Arg_0x8)
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
