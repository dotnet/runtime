// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b71179' {}
.assembly extern xunit.core {}
.class ILGEN_0x13230206 {

//BEGIN STATIC FIELDS
.field static	unsigned int32[] field_0x0
.field static	native int field_0x1
.field static	int64 field_0x2
.field static	unsigned int16[] field_0x3
.field static	int64 field_0x4
.field static	unsigned int16[] field_0x5
.field static	native int field_0x6
.field static	unsigned int32 field_0x7
.field static	unsigned int32[] field_0x8
.field static	unsigned int16[] field_0x9
.field static	unsigned int32[] field_0xa
//END STATIC FIELDS
.method static int32 Method_0xce57b468(unsigned int64 Arg_0x0, float64 Arg_0x1, int8 Arg_0x2, unsigned int64 Arg_0x3) {
.maxstack	17

 .locals (int8[] local_0x0,float32 local_0x1,native int[] local_0x2,float64 local_0x3,native int local_0x4,int16 local_0x5)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x0
ldc.r4 float32(0xc720280b) 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stloc local_0x2
ldc.r8 float64(0x489d6614979e0f2) 
stloc local_0x3
ldc.i4 0xe9ea54a9 
stloc local_0x4
ldc.i4 0xeedf9af6 
stloc local_0x5

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x13230206::field_0x0
ldc.i4 0xc0473f5d 
stsfld native int ILGEN_0x13230206::field_0x1
ldc.i8 0xbea0a6926b7c7623 
stsfld int64 ILGEN_0x13230206::field_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x13230206::field_0x3
ldc.i8 0x46cae53116ea74e9 
stsfld int64 ILGEN_0x13230206::field_0x4
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x13230206::field_0x5
ldc.i4 0x8048155a 
stsfld native int ILGEN_0x13230206::field_0x6
ldc.i4 0xbe8413af 
stsfld unsigned int32 ILGEN_0x13230206::field_0x7
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x13230206::field_0x8
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x13230206::field_0x9
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x13230206::field_0xa

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
 ldsfld unsigned int16[] ILGEN_0x13230206::field_0x9
 ldlen
 ldc.i4.3
 ldloc local_0x5
 shr
 blt Branch_0x2

Start_Orphan_2:
 nop
End_Orphan_2:
 ldc.i4.8
 ldloc local_0x4
 ble Branch_0x4

 ldc.i4 0x77d1b76a
 br Branch_0x5

 Branch_0x4:

 ldc.i4.5
 Branch_0x5:

 br Branch_0x3

 Branch_0x2:

Start_Orphan_3:
 nop
End_Orphan_3:
 ldloc local_0x5
 Branch_0x3:

 stsfld unsigned int32 ILGEN_0x13230206::field_0x7
End_Orphan_1:
 ldloca local_0x1
 ldarg Arg_0x1
 conv.u
 conv.r4
 stind.r4
End_Orphan_0:
 ldsfld int64 ILGEN_0x13230206::field_0x4
 conv.ovf.u
 neg
 ldc.i4.s -71
 clt.un
 conv.u
 conv.u
 conv.ovf.i4.un
Start_Orphan_b:
Start_Orphan_c:
 ldloca local_0x4
 ldloc local_0x4
 conv.u
 conv.u
 stind.i
End_Orphan_c:
Start_Orphan_d:
Start_Orphan_e:
 nop
End_Orphan_e:
 ldsfld unsigned int16[] ILGEN_0x13230206::field_0x3
 ldc.i4.4
 ldelema [mscorlib]System.UInt16
 ldsflda unsigned int32 ILGEN_0x13230206::field_0x7
 ldind.u4
 stind.i2
End_Orphan_d:
 ldc.i4.4
 conv.u1
 newarr [mscorlib]System.SByte
Start_Orphan_f:
Start_Orphan_10:
 nop
End_Orphan_10:
 ldc.i4.4
 ldloc local_0x4
 div
 conv.ovf.u2
 pop
End_Orphan_f:
 ldsfld unsigned int16[] ILGEN_0x13230206::field_0x3
Start_Orphan_11:
 ldloca local_0x5
 pop
End_Orphan_11:
 ldc.i4.3
 conv.ovf.i4
 conv.ovf.u1.un
Start_Orphan_12:
 nop
End_Orphan_12:
Start_Orphan_13:
 nop
End_Orphan_13:
 ldsfld native int ILGEN_0x13230206::field_0x6
 ldloc local_0x4
 rem
 conv.ovf.i
 clt.un
 ldelem.u2
Start_Orphan_14:
Start_Orphan_15:
 nop
End_Orphan_15:
 ldc.r8 float64(0x38f1571a8e59d305)
 conv.ovf.u8
 conv.ovf.u4
 stloc local_0x5
End_Orphan_14:
 ldc.i4.8
 stelem.i1
End_Orphan_b:
Start_Orphan_16:
Start_Orphan_17:
 nop
End_Orphan_17:
 ldc.i4 0x76ab670f
 conv.u1
 newarr [mscorlib]System.UInt32
 ldc.i4.0
 ldloc local_0x4
 conv.ovf.i2
 stelem.i4
End_Orphan_16:
Start_Orphan_18:
 ldsfld unsigned int16[] ILGEN_0x13230206::field_0x9
 ldarg Arg_0x2
 ldc.i4 0xa86ddf1b
 stelem.i2
End_Orphan_18:
 ldc.i4.5
 conv.ovf.u.un
 conv.ovf.i8
 conv.ovf.i
 neg
Start_Orphan_19:
 ldsfld unsigned int32[] ILGEN_0x13230206::field_0x0
 stsfld unsigned int32[] ILGEN_0x13230206::field_0xa
End_Orphan_19:
Start_Orphan_1a:
 nop
End_Orphan_1a:
 ldsfld native int ILGEN_0x13230206::field_0x6
 conv.ovf.u4.un
 conv.ovf.i1.un
 conv.i4
 conv.i4
 conv.ovf.u
Start_Orphan_1b:
Start_Orphan_1c:
Start_Orphan_1d:
 nop
End_Orphan_1d:
 ldsfld unsigned int32[] ILGEN_0x13230206::field_0x8
 pop
End_Orphan_1c:
Start_Orphan_1e:
 ldsfld unsigned int16[] ILGEN_0x13230206::field_0x5
 ldc.i4.5
 ldelema [mscorlib]System.UInt16
 ldc.i4.4
 stind.i2
End_Orphan_1e:
Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldloc local_0x4
 ldsfld unsigned int32 ILGEN_0x13230206::field_0x7
 rem
 conv.ovf.i8
 conv.r.un
 stloc local_0x3
End_Orphan_1b:
 ldc.i4.0
 sub.ovf.un
 xor
 ble Branch_0x0

Start_Orphan_20:
Start_Orphan_21:
Start_Orphan_22:
 ldsflda native int ILGEN_0x13230206::field_0x1
 ldsfld native int ILGEN_0x13230206::field_0x6
 stind.i
End_Orphan_22:
 ldsflda unsigned int32 ILGEN_0x13230206::field_0x7
Start_Orphan_23:
 ldloc local_0x1
 pop
End_Orphan_23:
 ldsfld native int ILGEN_0x13230206::field_0x1
 conv.i8
 conv.ovf.u1
 stind.i4
End_Orphan_21:
Start_Orphan_24:
 ldarga Arg_0x1
 pop
End_Orphan_24:
 ldsfld unsigned int16[] ILGEN_0x13230206::field_0x3
Start_Orphan_25:
 ldsfld unsigned int32 ILGEN_0x13230206::field_0x7
 conv.u1
 newarr [mscorlib]System.UInt16
 stsfld unsigned int16[] ILGEN_0x13230206::field_0x9
End_Orphan_25:
Start_Orphan_26:
 nop
End_Orphan_26:
 ldloc local_0x1
 conv.u
 conv.u4
 ldelema [mscorlib]System.UInt16
Start_Orphan_27:
Start_Orphan_28:
 nop
End_Orphan_28:
 ldsfld native int ILGEN_0x13230206::field_0x6
 conv.ovf.i2.un
Start_Orphan_29:
 nop
End_Orphan_29:
 ldsfld native int ILGEN_0x13230206::field_0x6
 ldloc local_0x4
 xor
 div.un
 stsfld native int ILGEN_0x13230206::field_0x6
End_Orphan_27:
 ldc.i4.4
 ldarg Arg_0x2
 xor
 conv.i8
 conv.i4
Start_Orphan_2a:
 ldsflda native int ILGEN_0x13230206::field_0x1
Start_Orphan_2b:
 nop
End_Orphan_2b:
 ldsfld native int ILGEN_0x13230206::field_0x6
 ldsfld native int ILGEN_0x13230206::field_0x1
 add
 stind.i
End_Orphan_2a:
Start_Orphan_2c:
 nop
End_Orphan_2c:
 ldarg Arg_0x0
 ldc.i4.0
 shr
 conv.ovf.i4.un
Start_Orphan_2d:
 ldloc local_0x4
 stloc local_0x4
End_Orphan_2d:
Start_Orphan_2e:
 nop
End_Orphan_2e:
 ldc.i4.s 14
 ldc.i4.7
 rem
 ldsfld int64 ILGEN_0x13230206::field_0x2
 conv.ovf.i1.un
 mul
 clt.un
 xor
 stind.i2
End_Orphan_20:
Start_Orphan_2f:
Start_Orphan_30:
Start_Orphan_31:
 nop
End_Orphan_31:
 ldc.i4.0
 conv.u1
 newarr [mscorlib]System.IntPtr
 pop
End_Orphan_30:
 ldc.i4.5
 stsfld unsigned int32 ILGEN_0x13230206::field_0x7
End_Orphan_2f:
Start_Orphan_32:
Start_Orphan_33:
 ldloc local_0x2
 ldsfld unsigned int32 ILGEN_0x13230206::field_0x7
 ldsfld native int ILGEN_0x13230206::field_0x1
 stelem.i
End_Orphan_33:
 ldsflda unsigned int32 ILGEN_0x13230206::field_0x7
 pop
End_Orphan_32:
Start_Orphan_34:
 ldsfld unsigned int32[] ILGEN_0x13230206::field_0x8
Start_Orphan_35:
 nop
End_Orphan_35:
 ldc.r8 float64(0xe64efba4a61ca9fc)
 conv.i2
Start_Orphan_36:
 nop
End_Orphan_36:
 ldloc local_0x3
 conv.ovf.i4
 stelem.i4
End_Orphan_34:
Start_Orphan_37:
 ldarga Arg_0x2
 pop
End_Orphan_37:
Start_Orphan_38:
 nop
End_Orphan_38:
 ldarg Arg_0x1
 neg
 call float64 [mscorlib]System.Math::Abs(float64)
Start_Orphan_39:
 nop
End_Orphan_39:
 ldarg Arg_0x3
 conv.r4
 div 
Start_Orphan_3a:
 nop
End_Orphan_3a:
 ldsfld native int ILGEN_0x13230206::field_0x6
 conv.ovf.u8
Start_Orphan_3b:
 nop
End_Orphan_3b:
 ldc.i8 0xae5eb308278c32ef
 ldc.i4.4
 shr
 mul.ovf
 conv.r.un
 div 
 call float64 [mscorlib]System.Math::Round(float64)
 call float64 [mscorlib]System.Math::Round(float64)
 conv.u2
 br Branch_0x1

 Branch_0x0:

Start_Orphan_3c:
Start_Orphan_3d:
 ldloca local_0x5
Start_Orphan_3e:
 ldc.i4.3
 conv.u1
 newarr [mscorlib]System.SByte
 stloc local_0x0
End_Orphan_3e:
Start_Orphan_3f:
 nop
End_Orphan_3f:
 ldc.r4 float32(0x3bde19b6)
 conv.ovf.u2
 stind.i2
End_Orphan_3d:
 ldsflda int64 ILGEN_0x13230206::field_0x4
Start_Orphan_40:
 ldc.i4.1
 conv.u4
 ldc.i4.4
 ldloc local_0x4
 ceq
 shl
 conv.u1
 newarr [mscorlib]System.UInt16
 pop
End_Orphan_40:
Start_Orphan_41:
Start_Orphan_42:
 ldloca local_0x5
 ldc.i4.5
 stind.i2
End_Orphan_42:
 ldarg Arg_0x1
 neg
Start_Orphan_43:
 nop
End_Orphan_43:
 ldc.i4.1
 conv.r8
 mul 
 stloc local_0x3
End_Orphan_41:
Start_Orphan_44:
 ldsfld unsigned int32[] ILGEN_0x13230206::field_0x0
 pop
End_Orphan_44:
 ldc.r8 float64(0x80ec3a7517bd7eb6)
 conv.ovf.u8
 conv.u8
 ldarg Arg_0x2
 ldc.i4 0x9457af01
 cgt
 conv.ovf.u8.un
 add.ovf.un
 conv.ovf.u8
Start_Orphan_45:
 ldsfld unsigned int32[] ILGEN_0x13230206::field_0x0
 ldc.i4.8
 ldc.i4 0x2018367e
 stelem.i4
End_Orphan_45:
 ldsfld int64 ILGEN_0x13230206::field_0x4
 ldarg Arg_0x3
 rem
 not
Start_Orphan_46:
 nop
End_Orphan_46:
 ldarg Arg_0x3
Start_Orphan_47:
 nop
End_Orphan_47:
 ldc.i4 0xf979971f
 conv.ovf.i8.un
 rem
 or
Start_Orphan_48:
Start_Orphan_49:
 nop
End_Orphan_49:
 ldsfld unsigned int32 ILGEN_0x13230206::field_0x7
 pop
End_Orphan_48:
 ldc.i4.5
 shr
 mul.ovf.un
 stind.i8
End_Orphan_3c:
Start_Orphan_4a:
 ldarg Arg_0x1
 ckfinite
 conv.r4
 stloc local_0x1
End_Orphan_4a:
 ldc.i8 0xb8de494c3ab5692f
Start_Orphan_4b:
 ldsfld unsigned int32[] ILGEN_0x13230206::field_0x8
 ldc.i4.2
 ldc.i4.7
 ldc.i4.0
 div
 stelem.i4
End_Orphan_4b:
Start_Orphan_4c:
 nop
End_Orphan_4c:
 conv.u
 conv.i4
 Branch_0x1:

EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i8 0x2066bdddae0a6021 
ldc.r8 float64(0xb84301d0b2c00fb4) 
ldc.i4 0x1406e495 
ldc.i8 0xab23aa78832f97dc 

call int32 ILGEN_0x13230206::Method_0xce57b468(unsigned int64 Arg_0x0, float64 Arg_0x1, int8 Arg_0x2, unsigned int64 Arg_0x3)
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
