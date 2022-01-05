// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b63726' {}
.assembly extern xunit.core {}
.class ILGEN_0xd73e17d6 {

//BEGIN STATIC FIELDS
.field static	unsigned int64[] field_0x0
.field static	unsigned int64[] field_0x1
.field static	unsigned int64[] field_0x2
.field static	unsigned int8[] field_0x3
.field static	float64[] field_0x4
.field static	unsigned int8 field_0x5
.field static	native int field_0x6
.field static	float64[] field_0x7
.field static	unsigned int8[] field_0x8
.field static	unsigned int16[] field_0x9
//END STATIC FIELDS
.method static int32 Method_0x49426e8a(unsigned int8 Arg_0x0, int64 Arg_0x1, int64 Arg_0x2, int32 Arg_0x3, float32 Arg_0x4, int64 Arg_0x5, float64 Arg_0x6, int64 Arg_0x7, unsigned int64 Arg_0x8, unsigned int32 Arg_0x9) {
.maxstack	21

 .locals (float32 LOCAL_0x0,native int[] LOCAL_0x1,unsigned int32 LOCAL_0x2,int32[] LOCAL_0x3,int64 LOCAL_0x4,float64[] LOCAL_0x5,int64 LOCAL_0x6,int64 LOCAL_0x7,unsigned int8 LOCAL_0x8,int32 LOCAL_0x9)
//BEGIN INIT LOCALS
ldc.r4 float32(0x4297982d) 
stloc LOCAL_0x0
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stloc LOCAL_0x1
ldc.i4 0x509cdf45 
stloc LOCAL_0x2
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc LOCAL_0x3
ldc.i8 0xbeb5cdb534dd0ca2 
stloc LOCAL_0x4
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc LOCAL_0x5
ldc.i8 0x30434a09df71bedb 
stloc LOCAL_0x6
ldc.i8 0x390dcf87f6c0cb75 
stloc LOCAL_0x7
ldc.i4 0x3dcaa204 
stloc LOCAL_0x8
ldc.i4 0xafbb5f4d 
stloc LOCAL_0x9

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x0
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x2
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x3
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0xd73e17d6::field_0x4
ldc.i4 0x2195e9a9 
stsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
ldc.i4 0xf97d651e 
stsfld native int ILGEN_0xd73e17d6::field_0x6
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0xd73e17d6::field_0x7
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x8
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
 ldarga Arg_0x4
Start_Orphan_2:
 ldloc LOCAL_0x8
 conv.u8
 conv.ovf.i1.un
 ldc.i4.2
 ldc.i4.5
 div
Start_Orphan_3:
 nop
End_Orphan_3:
 ldc.i4.7
 conv.u4
 bne.un Branch_0x6

 ldarg Arg_0x6
 conv.u
 br Branch_0x7

 Branch_0x6:

 ldarg Arg_0x6
 ldarg Arg_0x4
 ble.un Branch_0x8

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x9

 Branch_0x8:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x9:

 Branch_0x7:

 bgt.un Branch_0x4

Start_Orphan_4:
 nop
End_Orphan_4:
 ldarga Arg_0x9
 ldind.u4
 br Branch_0x5

 Branch_0x4:

Start_Orphan_5:
 ldloc LOCAL_0x8
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble.un Branch_0xa

 nop
 br Branch_0xb

 Branch_0xa:

 nop
 Branch_0xb:

End_Orphan_5:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i1.un
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt Branch_0xc

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0xd

 Branch_0xc:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0xd:

 cgt
 Branch_0x5:

Start_Orphan_6:
 ldc.i4.8
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0xe

 nop
 br Branch_0xf

 Branch_0xe:

 nop
 Branch_0xf:

End_Orphan_6:
 ldloc LOCAL_0x0
 conv.i
 ldc.i4.2
 ldloc LOCAL_0x2
 shr
 bgt.un Branch_0x10

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 rem.un
 br Branch_0x11

 Branch_0x10:

Start_Orphan_7:
 nop
End_Orphan_7:
 ldc.i4.7
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x12

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x13

 Branch_0x12:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x13:

 Branch_0x11:

 conv.ovf.i2.un
 bgt.un Branch_0x2

Start_Orphan_8:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 ldloc LOCAL_0x7
 stelem.i8
End_Orphan_8:
 ldc.i4.6
 ldc.i4.1
 mul.ovf.un
 conv.u1
 newarr [mscorlib]System.Double
 stloc LOCAL_0x5
 br Branch_0x3

 Branch_0x2:

Start_Orphan_9:
 nop
End_Orphan_9:
 ldarg Arg_0x3
 ldc.i4.5
 ldloc LOCAL_0x9
 sub.ovf
 bge.un Branch_0x16

 ldc.i4.m1
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bne.un Branch_0x18

 ldarg Arg_0x4
 br Branch_0x19

 Branch_0x18:

 ldc.r8 float64(0xef4a0822ad3f1c66)
 Branch_0x19:

 br Branch_0x17

 Branch_0x16:

 ldloc LOCAL_0x0
 Branch_0x17:

Start_Orphan_a:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt Branch_0x1a

 nop
 br Branch_0x1b

 Branch_0x1a:

 nop
 Branch_0x1b:

End_Orphan_a:
Start_Orphan_b:
 nop
End_Orphan_b:
 ldloc LOCAL_0x0
 ldloc LOCAL_0x0
 ble.un Branch_0x1e

 ldloc LOCAL_0x8
 br Branch_0x1f

 Branch_0x1e:

 ldc.i4.m1
 Branch_0x1f:

 ldloc LOCAL_0x8
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt.un Branch_0x20

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x21

 Branch_0x20:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x21:

 bge Branch_0x1c

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt.un Branch_0x22

 ldloc LOCAL_0x0
 br Branch_0x23

 Branch_0x22:

 ldarg Arg_0x6
 Branch_0x23:

 br Branch_0x1d

 Branch_0x1c:

 ldc.i4.1
 ldarg Arg_0x0
 blt Branch_0x24

 ldarg Arg_0x4
 br Branch_0x25

 Branch_0x24:

 ldloc LOCAL_0x0
 Branch_0x25:

 Branch_0x1d:

 bge Branch_0x14

 ldc.i4.3
 ldc.i4.m1
 blt Branch_0x28

 ldarg Arg_0x3
 br Branch_0x29

 Branch_0x28:

 ldloc LOCAL_0x9
 Branch_0x29:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brfalse Branch_0x2a

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x2b

 Branch_0x2a:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x2b:

 ble Branch_0x26

 ldc.i8 0x4666531beb99ac5
 ldloc LOCAL_0x7
 bgt.un Branch_0x2c

 nop
 br Branch_0x2d

 Branch_0x2c:

 nop
 Branch_0x2d:

 br Branch_0x27

 Branch_0x26:

 ldarg Arg_0x0
 ldarg Arg_0x3
 blt Branch_0x2e

 nop
 br Branch_0x2f

 Branch_0x2e:

 nop
 Branch_0x2f:

 Branch_0x27:

 br Branch_0x15

 Branch_0x14:

 ldarga Arg_0x6
 pop
 Branch_0x15:

 Branch_0x3:

End_Orphan_2:
Start_Orphan_c:
 ldarg Arg_0x0
 stsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
End_Orphan_c:
Start_Orphan_d:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x30

 nop
 br Branch_0x31

 Branch_0x30:

 nop
 Branch_0x31:

End_Orphan_d:
Start_Orphan_e:
 nop
End_Orphan_e:
 ldc.i4.6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bne.un Branch_0x32

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x33

 Branch_0x32:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x33:

 conv.ovf.i4
Start_Orphan_f:
 ldsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
 stsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
End_Orphan_f:
Start_Orphan_10:
 nop
End_Orphan_10:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.6
 div.un
 ldc.i4.0
 conv.ovf.u
 bge.un Branch_0x34

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x36

 ldc.i4.m1
 br Branch_0x37

 Branch_0x36:

 ldc.i4.4
 Branch_0x37:

 br Branch_0x35

 Branch_0x34:

Start_Orphan_11:
 nop
End_Orphan_11:
 ldc.i4.6
 conv.ovf.u2
 Branch_0x35:

 rem
 conv.r.un
 call float64 [mscorlib]System.Math::Sqrt(float64)
 call float64 [mscorlib]System.Math::Round(float64)
 stind.r4
End_Orphan_1:
Start_Orphan_12:
Start_Orphan_13:
 ldloc LOCAL_0x3
 stloc LOCAL_0x3
End_Orphan_13:
Start_Orphan_14:
Start_Orphan_15:
 ldarg Arg_0x4
 ldarg Arg_0x4
 bge Branch_0x38

 nop
 br Branch_0x39

 Branch_0x38:

 nop
 Branch_0x39:

End_Orphan_15:
 ldloc LOCAL_0x7
 ldc.i8 0x38b738abb5efbad
 bne.un Branch_0x3c

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x3d

 Branch_0x3c:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x3d:

 brfalse Branch_0x3a

 ldarg Arg_0x0
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt Branch_0x3e

 ldarg Arg_0x8
 br Branch_0x3f

 Branch_0x3e:

 ldarg Arg_0x8
 Branch_0x3f:

 br Branch_0x3b

 Branch_0x3a:

 ldc.i8 0x596d087fff23ae85
 ldarg Arg_0x5
 mul
 Branch_0x3b:

 starg Arg_0x8
End_Orphan_14:
Start_Orphan_16:
 ldarg Arg_0x3
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt.un Branch_0x40

 nop
 br Branch_0x41

 Branch_0x40:

 nop
 Branch_0x41:

End_Orphan_16:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i
 conv.ovf.u8.un
 conv.u4
Start_Orphan_17:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.u8
 pop
End_Orphan_17:
Start_Orphan_18:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt Branch_0x42

 nop
 br Branch_0x43

 Branch_0x42:

 nop
 Branch_0x43:

End_Orphan_18:
Start_Orphan_19:
 nop
End_Orphan_19:
 ldloc LOCAL_0x5
 ldlen
 conv.ovf.i
 sub
Start_Orphan_1a:
Start_Orphan_1b:
 ldc.i4.6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt.un Branch_0x46

 nop
 br Branch_0x47

 Branch_0x46:

 nop
 Branch_0x47:

End_Orphan_1b:
 ldc.i4.m1
Start_Orphan_1c:
 ldc.r4 float32(0xf00ef10b)
 ldc.r8 float64(0x9c7f5e1aa7bd8a14)
 bge Branch_0x48

 nop
 br Branch_0x49

 Branch_0x48:

 nop
 Branch_0x49:

End_Orphan_1c:
Start_Orphan_1d:
 nop
End_Orphan_1d:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 and
 ldc.i4.8
 brtrue Branch_0x4a

 ldloc LOCAL_0x8
 br Branch_0x4b

 Branch_0x4a:

 ldc.i4.5
 Branch_0x4b:

 add
 ble Branch_0x44

Start_Orphan_1e:
 nop
End_Orphan_1e:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.u4
 stsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 br Branch_0x45

 Branch_0x44:

Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.1
 bge Branch_0x4e

 ldarg Arg_0x8
 br Branch_0x4f

 Branch_0x4e:

 ldc.i8 0xeaea233cf76ce4c4
 Branch_0x4f:

Start_Orphan_20:
 nop
End_Orphan_20:
 ldloc LOCAL_0x2
 conv.u8
 ble Branch_0x4c

 ldc.i4.4
 ldc.i4.1
 blt.un Branch_0x50

 nop
 br Branch_0x51

 Branch_0x50:

 nop
 Branch_0x51:

 br Branch_0x4d

 Branch_0x4c:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brfalse Branch_0x52

 nop
 br Branch_0x53

 Branch_0x52:

 nop
 Branch_0x53:

 Branch_0x4d:

 Branch_0x45:

End_Orphan_1a:
Start_Orphan_21:
 ldc.i4.0
 conv.u1
 newarr [mscorlib]System.Double
 stsfld float64[] ILGEN_0xd73e17d6::field_0x7
End_Orphan_21:
 ldc.r4 float32(0xe3c7923b)
Start_Orphan_22:
 ldc.i4 0x1d11afe7
 conv.ovf.i2.un
 ldc.i4 0xcbaede87
 bge.un Branch_0x56

 ldloc LOCAL_0x0
 ldarg Arg_0x6
 bgt Branch_0x58

 nop
 br Branch_0x59

 Branch_0x58:

 nop
 Branch_0x59:

 br Branch_0x57

 Branch_0x56:

 ldloc LOCAL_0x9
 brfalse Branch_0x5a

 nop
 br Branch_0x5b

 Branch_0x5a:

 nop
 Branch_0x5b:

 Branch_0x57:

End_Orphan_22:
Start_Orphan_23:
 ldarg Arg_0x9
 stloc LOCAL_0x8
End_Orphan_23:
 ldc.i4.s -32
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0x5c

 ldc.r4 float32(0xc8433660)
 br Branch_0x5d

 Branch_0x5c:

 ldc.r4 float32(0xada028ae)
 Branch_0x5d:

 call float32 [mscorlib]System.Math::Abs(float32)
 call float64 [mscorlib]System.Math::Abs(float64)
 bgt.un Branch_0x54

Start_Orphan_24:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt.un Branch_0x5e

 nop
 br Branch_0x5f

 Branch_0x5e:

 nop
 Branch_0x5f:

End_Orphan_24:
Start_Orphan_25:
 nop
End_Orphan_25:
 ldc.i4 0xc1a0400d
 ldc.i4.2
 bge.un Branch_0x62

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x63

 Branch_0x62:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x63:

Start_Orphan_26:
 nop
End_Orphan_26:
 ldarg Arg_0x6
 ldarg Arg_0x6
 ble.un Branch_0x64

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x65

 Branch_0x64:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x65:

 ble.un Branch_0x60

 ldc.i4.3
 ldc.i4.1
 beq Branch_0x66

 ldc.r8 float64(0x71a6d7e1f7880cba)
 br Branch_0x67

 Branch_0x66:

 ldarg Arg_0x4
 Branch_0x67:

 br Branch_0x61

 Branch_0x60:

Start_Orphan_27:
 nop
End_Orphan_27:
 ldloc LOCAL_0x4
 conv.r.un
 Branch_0x61:

 conv.u
 br Branch_0x55

 Branch_0x54:

Start_Orphan_28:
 ldarg Arg_0x4
 conv.ovf.i
Start_Orphan_29:
 nop
End_Orphan_29:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i.un
 blt Branch_0x68

 ldarga Arg_0x6
 ldarg Arg_0x6
 stind.r8
 br Branch_0x69

 Branch_0x68:

 ldloc LOCAL_0x1
 ldloc LOCAL_0x9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 stelem.i
 Branch_0x69:

End_Orphan_28:
Start_Orphan_2a:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt Branch_0x6c

 nop
 br Branch_0x6d

 Branch_0x6c:

 nop
 Branch_0x6d:

End_Orphan_2a:
Start_Orphan_2b:
 nop
End_Orphan_2b:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brtrue Branch_0x70

 ldc.r4 float32(0x62ae3f87)
 br Branch_0x71

 Branch_0x70:

 ldloc LOCAL_0x0
 Branch_0x71:

 ldc.i4.5
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble.un Branch_0x72

 ldloc LOCAL_0x0
 br Branch_0x73

 Branch_0x72:

 ldc.r8 float64(0x23d6fc8c16869103)
 Branch_0x73:

 ble.un Branch_0x6e

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble Branch_0x74

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x75

 Branch_0x74:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x75:

 br Branch_0x6f

 Branch_0x6e:

Start_Orphan_2c:
 nop
End_Orphan_2c:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 mul
 Branch_0x6f:

Start_Orphan_2d:
 ldloca LOCAL_0x4
 pop
End_Orphan_2d:
Start_Orphan_2e:
 nop
End_Orphan_2e:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 add.ovf.un
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x2
 ldlen
 add.ovf.un
 bge.un Branch_0x6a

 ldarg Arg_0x9
 ldloc LOCAL_0x2
 mul.ovf.un
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i.un
 ble.un Branch_0x76

Start_Orphan_2f:
 nop
End_Orphan_2f:
 ldloc LOCAL_0x2
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x78

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x79

 Branch_0x78:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x79:

 br Branch_0x77

 Branch_0x76:

 ldarg Arg_0x0
 ldc.i4.8
 bge Branch_0x7a

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x7b

 Branch_0x7a:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x7b:

 Branch_0x77:

 br Branch_0x6b

 Branch_0x6a:

Start_Orphan_30:
 nop
End_Orphan_30:
 ldc.i8 0xbf9c317a2c2b2a28
 ldarg Arg_0x8
 bne.un Branch_0x7c

 ldloc LOCAL_0x9
 br Branch_0x7d

 Branch_0x7c:

 ldarg Arg_0x9
 Branch_0x7d:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 and
 rem
 Branch_0x6b:

 Branch_0x55:

 div.un
 pop
End_Orphan_12:
Start_Orphan_31:
Start_Orphan_32:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.2
 bge Branch_0x82

 ldc.r4 float32(0x6159c1cc)
 br Branch_0x83

 Branch_0x82:

 ldarg Arg_0x6
 Branch_0x83:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt.un Branch_0x84

 ldloc LOCAL_0x0
 br Branch_0x85

 Branch_0x84:

 ldc.r8 float64(0x6fde4b4752905014)
 Branch_0x85:

 mul 
 starg Arg_0x6
End_Orphan_32:
Start_Orphan_33:
 nop
End_Orphan_33:
 ldc.i4.4
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 ble.un Branch_0x88

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x89

 Branch_0x88:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x89:

 conv.ovf.i.un
Start_Orphan_34:
 nop
End_Orphan_34:
 ldc.i8 0x6088486c154f81f1
 conv.ovf.i
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.8
 blt Branch_0x8c

 ldarg Arg_0x9
 br Branch_0x8d

 Branch_0x8c:

 ldarg Arg_0x9
 Branch_0x8d:

 bgt.un Branch_0x8a

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 neg
 br Branch_0x8b

 Branch_0x8a:

Start_Orphan_35:
 nop
End_Orphan_35:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.u
 Branch_0x8b:

 add
Start_Orphan_36:
Start_Orphan_37:
 nop
End_Orphan_37:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble Branch_0x90

 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 br Branch_0x91

 Branch_0x90:

 ldc.i4.7
 Branch_0x91:

Start_Orphan_38:
 nop
End_Orphan_38:
 ldloc LOCAL_0x9
 conv.ovf.i4
 blt.un Branch_0x8e

 ldarg Arg_0x7
 starg Arg_0x7
 br Branch_0x8f

 Branch_0x8e:

 ldarga Arg_0x8
 pop
 Branch_0x8f:

End_Orphan_36:
 ldarg Arg_0x8
 ldloc LOCAL_0x4
 div
 ldloc LOCAL_0x3
 ldlen
 shl
 conv.ovf.u.un
 ble Branch_0x86

 ldarg Arg_0x6
 conv.i
Start_Orphan_39:
 nop
End_Orphan_39:
 ldc.i4.2
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble.un Branch_0x94

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x95

 Branch_0x94:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x95:

 bge.un Branch_0x92

 ldc.i8 0x785f95a5cbc02434
 conv.r.un
 br Branch_0x93

 Branch_0x92:

Start_Orphan_3a:
 nop
End_Orphan_3a:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bne.un Branch_0x96

 ldloc LOCAL_0x0
 br Branch_0x97

 Branch_0x96:

 ldc.r4 float32(0x1608c0cb)
 Branch_0x97:

 Branch_0x93:

 conv.ovf.u4
 br Branch_0x87

 Branch_0x86:

 ldc.i4.7
 Branch_0x87:

Start_Orphan_3b:
 ldarga Arg_0x8
Start_Orphan_3c:
 nop
End_Orphan_3c:
 ldc.i8 0x7f050de5c17a5ee6
 ldloc LOCAL_0x7
 cgt
 brfalse Branch_0x98

 ldarg Arg_0x8
 conv.ovf.i8
 br Branch_0x99

 Branch_0x98:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4 0x3b8116c
 ble.un Branch_0x9a

 ldarg Arg_0x1
 br Branch_0x9b

 Branch_0x9a:

 ldarg Arg_0x8
 Branch_0x9b:

 Branch_0x99:

 stind.i8
End_Orphan_3b:
Start_Orphan_3d:
 ldsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
 ldc.i4.s -60
 ldelema [mscorlib]System.UInt16
 pop
End_Orphan_3d:
Start_Orphan_3e:
 nop
End_Orphan_3e:
 ldc.i4.2
 ldc.i4.3
 add
 brtrue Branch_0x9c

Start_Orphan_3f:
 nop
End_Orphan_3f:
 ldloc LOCAL_0x0
 call float32 [mscorlib]System.Math::Abs(float32)
 br Branch_0x9d

 Branch_0x9c:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0x9e

 ldc.r8 float64(0xcb34595c9da9ffb8)
 br Branch_0x9f

 Branch_0x9e:

 ldarg Arg_0x4
 Branch_0x9f:

 Branch_0x9d:

 conv.ovf.u
Start_Orphan_40:
 ldarg Arg_0x4
 ldloc LOCAL_0x0
 beq Branch_0xa0

 nop
 br Branch_0xa1

 Branch_0xa0:

 nop
 Branch_0xa1:

End_Orphan_40:
Start_Orphan_41:
 nop
End_Orphan_41:
 ldc.i4.5
 conv.ovf.u
Start_Orphan_42:
 nop
End_Orphan_42:
 ldc.r4 float32(0x273acc11)
 ldc.r4 float32(0xc8df72d6)
 clt.un
 beq Branch_0xa2

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 sub
 br Branch_0xa3

 Branch_0xa2:

 ldloc LOCAL_0x8
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 ble.un Branch_0xa4

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0xa5

 Branch_0xa4:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0xa5:

 Branch_0xa3:

 conv.ovf.i4
 div
 bgt.un Branch_0x80

Start_Orphan_43:
Start_Orphan_44:
 nop
End_Orphan_44:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.2
 beq Branch_0xa8

 ldarg Arg_0x6
 br Branch_0xa9

 Branch_0xa8:

 ldarg Arg_0x6
 Branch_0xa9:

Start_Orphan_45:
 nop
End_Orphan_45:
 ldc.i4.6
 ldc.i4.6
 beq Branch_0xaa

 ldloc LOCAL_0x0
 br Branch_0xab

 Branch_0xaa:

 ldc.r4 float32(0x76aa8e1f)
 Branch_0xab:

 blt.un Branch_0xa6

 ldarg Arg_0x9
 pop
 br Branch_0xa7

 Branch_0xa6:

 ldsflda native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 stind.i
 Branch_0xa7:

End_Orphan_43:
Start_Orphan_46:
 nop
End_Orphan_46:
 ldloc LOCAL_0x8
 brfalse Branch_0xae

 ldc.r4 float32(0xfa7f7f36)
 br Branch_0xaf

 Branch_0xae:

 ldloc LOCAL_0x0
 Branch_0xaf:

 conv.ovf.i1
Start_Orphan_47:
 nop
End_Orphan_47:
 ldc.i4.7
 brfalse Branch_0xb0

 ldarg Arg_0x8
 br Branch_0xb1

 Branch_0xb0:

 ldc.i8 0xc229eff4d0f3669c
 Branch_0xb1:

 conv.ovf.i.un
 bgt.un Branch_0xac

Start_Orphan_48:
 ldarga Arg_0x6
 pop
End_Orphan_48:
Start_Orphan_49:
 nop
End_Orphan_49:
 ldarg Arg_0x3
 conv.ovf.u.un
Start_Orphan_4a:
 nop
End_Orphan_4a:
 ldloc LOCAL_0x1
 ldc.i4.8
 ldelem.i
 cgt
 br Branch_0xad

 Branch_0xac:

Start_Orphan_4b:
 ldc.i4.0
 brtrue Branch_0xb2

 nop
 br Branch_0xb3

 Branch_0xb2:

 nop
 Branch_0xb3:

End_Orphan_4b:
 ldarg Arg_0x6
 ldarg Arg_0x6
 bge Branch_0xb4

 ldarg Arg_0x4
 br Branch_0xb5

 Branch_0xb4:

 ldarg Arg_0x6
 Branch_0xb5:

 conv.u4
 Branch_0xad:

 stsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 br Branch_0x81

 Branch_0x80:

Start_Orphan_4c:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 pop
End_Orphan_4c:
 ldsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt.un Branch_0xb6

 ldloc LOCAL_0x2
 br Branch_0xb7

 Branch_0xb6:

 ldloc LOCAL_0x8
 Branch_0xb7:

Start_Orphan_4d:
 nop
End_Orphan_4d:
 ldc.i4.4
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 sub
 cgt.un
 ldelema [mscorlib]System.UInt16
 pop
 Branch_0x81:

End_Orphan_31:
Start_Orphan_4e:
 ldloca LOCAL_0x6
 ldind.i8
Start_Orphan_4f:
Start_Orphan_50:
 nop
End_Orphan_50:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i
Start_Orphan_51:
 nop
End_Orphan_51:
 ldc.i4.1
 conv.i
 bge Branch_0xba

 ldc.r4 float32(0x69b6ec90)
 ldc.r4 float32(0x641aaa53)
 bgt Branch_0xbc

 nop
 br Branch_0xbd

 Branch_0xbc:

 nop
 Branch_0xbd:

 br Branch_0xbb

 Branch_0xba:

 ldarg Arg_0x6
 ldc.r4 float32(0xb10a8529)
 ble.un Branch_0xbe

 nop
 br Branch_0xbf

 Branch_0xbe:

 nop
 Branch_0xbf:

 Branch_0xbb:

End_Orphan_4f:
Start_Orphan_52:
 ldc.i4.8
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0xc2

 nop
 br Branch_0xc3

 Branch_0xc2:

 nop
 Branch_0xc3:

End_Orphan_52:
Start_Orphan_53:
 nop
End_Orphan_53:
 ldloc LOCAL_0x9
 ldc.i4.8
 shr.un
Start_Orphan_54:
 nop
End_Orphan_54:
 ldloc LOCAL_0x7
 conv.i
 bge.un Branch_0xc4

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brfalse Branch_0xc6

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0xc7

 Branch_0xc6:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0xc7:

 br Branch_0xc5

 Branch_0xc4:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 or
 Branch_0xc5:

Start_Orphan_55:
 ldc.r4 float32(0xb768dda4)
 ldarg Arg_0x6
 bge.un Branch_0xc8

 nop
 br Branch_0xc9

 Branch_0xc8:

 nop
 Branch_0xc9:

End_Orphan_55:
 ldc.i4.3
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 rem.un
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.i
 mul.ovf.un
 bgt.un Branch_0xc0

Start_Orphan_56:
 nop
End_Orphan_56:
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 conv.u1
 newarr [mscorlib]System.UInt64
 ldc.i4.1
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 beq Branch_0xca

 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 br Branch_0xcb

 Branch_0xca:

 ldc.i4.0
 Branch_0xcb:

 ldelem.u8
 br Branch_0xc1

 Branch_0xc0:

Start_Orphan_57:
 nop
End_Orphan_57:
 ldarg Arg_0x1
 ldc.i4.3
 shr
Start_Orphan_58:
 nop
End_Orphan_58:
 ldloc LOCAL_0x8
 ldc.i4.0
 shr.un
 shl
 Branch_0xc1:

 beq Branch_0xb8

 ldsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x8
Start_Orphan_59:
 ldloc LOCAL_0x3
 stloc LOCAL_0x3
End_Orphan_59:
 ldarga Arg_0x9
 ldind.u4
Start_Orphan_5a:
 ldloc LOCAL_0x1
 ldc.i4.6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 stelem.i
End_Orphan_5a:
 ldc.r8 float64(0x817be58db755eabd)
 conv.u
Start_Orphan_5b:
 nop
End_Orphan_5b:
 ldc.r8 float64(0x5c2f36df63d00986)
 conv.ovf.u4
 bgt Branch_0xcc

Start_Orphan_5c:
 nop
End_Orphan_5c:
 ldsfld float64[] ILGEN_0xd73e17d6::field_0x7
 ldlen
 br Branch_0xcd

 Branch_0xcc:

Start_Orphan_5d:
 nop
End_Orphan_5d:
 ldloc LOCAL_0x6
 ldloc LOCAL_0x4
 bge Branch_0xce

 ldc.i4.1
 br Branch_0xcf

 Branch_0xce:

 ldc.i4.0
 Branch_0xcf:

 Branch_0xcd:

 stelem.i1
 br Branch_0xb9

 Branch_0xb8:

Start_Orphan_5e:
 nop
End_Orphan_5e:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i1
Start_Orphan_5f:
 nop
End_Orphan_5f:
 ldc.i4.7
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 cgt
 shr.un
Start_Orphan_60:
 ldarg Arg_0x4
 ldarg Arg_0x6
 bgt Branch_0xd2

 nop
 br Branch_0xd3

 Branch_0xd2:

 nop
 Branch_0xd3:

End_Orphan_60:
 ldarg Arg_0x8
 ldc.i8 0xd868dc14b8d61d1
 blt.un Branch_0xd6

 ldloc LOCAL_0x7
 br Branch_0xd7

 Branch_0xd6:

 ldarg Arg_0x2
 Branch_0xd7:

Start_Orphan_61:
 nop
End_Orphan_61:
 ldc.i4.0
 conv.i8
 bne.un Branch_0xd4

 ldc.i4.8
 conv.u
 br Branch_0xd5

 Branch_0xd4:

Start_Orphan_62:
 nop
End_Orphan_62:
 ldarg Arg_0x0
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 div
 Branch_0xd5:

 blt.un Branch_0xd0

Start_Orphan_63:
 nop
End_Orphan_63:
 ldarg Arg_0x1
 ldc.i8 0x7f6356c63b0ac0a8
 rem
 ldarg Arg_0x2
 ldloc LOCAL_0x7
 blt.un Branch_0xda

 ldc.i8 0xbf07898eed2b4734
 br Branch_0xdb

 Branch_0xda:

 ldarg Arg_0x2
 Branch_0xdb:

 bge.un Branch_0xd8

 ldarg Arg_0x7
 starg Arg_0x2
 br Branch_0xd9

 Branch_0xd8:

 ldarg Arg_0x7
 ldarg Arg_0x8
 bge Branch_0xdc

 nop
 br Branch_0xdd

 Branch_0xdc:

 nop
 Branch_0xdd:

 Branch_0xd9:

 br Branch_0xd1

 Branch_0xd0:

 ldarg Arg_0x6
 ldc.r8 float64(0x785789883c252747)
 bgt Branch_0xde

 ldc.i4.6
 br Branch_0xdf

 Branch_0xde:

 ldc.i4.7
 Branch_0xdf:

 stloc LOCAL_0x2
 Branch_0xd1:

 Branch_0xb9:

End_Orphan_4e:
Start_Orphan_64:
 ldarga Arg_0x0
Start_Orphan_65:
 nop
End_Orphan_65:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.4
 bgt Branch_0xe2

 ldc.r8 float64(0x6b736572cbaae80)
 br Branch_0xe3

 Branch_0xe2:

 ldarg Arg_0x6
 Branch_0xe3:

 conv.i1
 stind.i1
End_Orphan_64:
 ldc.i4.5
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble.un Branch_0xe4

 ldc.i4 0x1756f2d3
 br Branch_0xe5

 Branch_0xe4:

 ldc.i4.m1
 Branch_0xe5:

 conv.ovf.i8.un
 conv.r4
 conv.u8
Start_Orphan_66:
Start_Orphan_67:
 ldc.i4.0
 stloc LOCAL_0x9
End_Orphan_67:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.0
 sub.ovf
 ldarg Arg_0x4
 conv.ovf.u2
 cgt.un
Start_Orphan_68:
 nop
End_Orphan_68:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i1
Start_Orphan_69:
 nop
End_Orphan_69:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.u1
 bge Branch_0xe8

Start_Orphan_6a:
 nop
End_Orphan_6a:
 ldc.r4 float32(0xcdf0e9ca)
 conv.ovf.i1
 br Branch_0xe9

 Branch_0xe8:

 ldc.i4.6
 not
 Branch_0xe9:

 bgt Branch_0xe6

 ldloc LOCAL_0x8
 conv.i1
 brtrue Branch_0xea

 ldloca LOCAL_0x9
 pop
 br Branch_0xeb

 Branch_0xea:

 ldsflda native int ILGEN_0xd73e17d6::field_0x6
 pop
 Branch_0xeb:

 br Branch_0xe7

 Branch_0xe6:

Start_Orphan_6b:
 nop
End_Orphan_6b:
 ldsflda unsigned int8 ILGEN_0xd73e17d6::field_0x5
 pop
 Branch_0xe7:

End_Orphan_66:
Start_Orphan_6c:
 ldarg Arg_0x8
 ldloc LOCAL_0x4
 bge Branch_0xee

 nop
 br Branch_0xef

 Branch_0xee:

 nop
 Branch_0xef:

End_Orphan_6c:
 ldc.r4 float32(0x4b440b73)
 ldarg Arg_0x6
 add 
Start_Orphan_6d:
 nop
End_Orphan_6d:
 ldc.r4 float32(0xb8a75955)
 call float64 [mscorlib]System.Math::Round(float64)
 bge.un Branch_0xf0

 ldarg Arg_0x6
 ldarg Arg_0x4
 ble Branch_0xf2

 ldc.i8 0x24304a678d8a1ba1
 br Branch_0xf3

 Branch_0xf2:

 ldarg Arg_0x8
 Branch_0xf3:

 br Branch_0xf1

 Branch_0xf0:

Start_Orphan_6e:
 nop
End_Orphan_6e:
 ldarga Arg_0x8
 ldind.u8
 Branch_0xf1:

Start_Orphan_6f:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.8
 ble.un Branch_0xf4

 nop
 br Branch_0xf5

 Branch_0xf4:

 nop
 Branch_0xf5:

End_Orphan_6f:
Start_Orphan_70:
 nop
End_Orphan_70:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldloc LOCAL_0x8
 rem
 conv.ovf.u8
 blt.un Branch_0xec

Start_Orphan_71:
 ldc.i4.s 29
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble Branch_0xf6

 nop
 br Branch_0xf7

 Branch_0xf6:

 nop
 Branch_0xf7:

End_Orphan_71:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldloc LOCAL_0x9
 bgt.un Branch_0xfa

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0xfb

 Branch_0xfa:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0xfb:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0xfc

 ldc.i4.6
 br Branch_0xfd

 Branch_0xfc:

 ldc.i4.3
 Branch_0xfd:

 bge.un Branch_0xf8

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 beq Branch_0xfe

 ldloc LOCAL_0x4
 br Branch_0xff

 Branch_0xfe:

 ldloc LOCAL_0x7
 Branch_0xff:

 br Branch_0xf9

 Branch_0xf8:

Start_Orphan_72:
 nop
End_Orphan_72:
 ldc.i4.1
 ldc.i4.5
 bne.un Branch_0x100

 ldloc LOCAL_0x4
 br Branch_0x101

 Branch_0x100:

 ldloc LOCAL_0x6
 Branch_0x101:

 Branch_0xf9:

 br Branch_0xed

 Branch_0xec:

Start_Orphan_73:
 ldloc LOCAL_0x9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x102

 nop
 br Branch_0x103

 Branch_0x102:

 nop
 Branch_0x103:

End_Orphan_73:
Start_Orphan_74:
 nop
End_Orphan_74:
 ldarg Arg_0x8
 conv.ovf.u.un
 brfalse Branch_0x104

 ldc.i4.4
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 blt Branch_0x106

 ldarg Arg_0x7
 br Branch_0x107

 Branch_0x106:

 ldarg Arg_0x2
 Branch_0x107:

 br Branch_0x105

 Branch_0x104:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.u8
 Branch_0x105:

 Branch_0xed:

Start_Orphan_75:
 ldc.r4 float32(0xa820543d)
 starg Arg_0x6
End_Orphan_75:
Start_Orphan_76:
 ldloc LOCAL_0x5
 pop
End_Orphan_76:
 ldc.i4.4
 ldarg Arg_0x0
 bgt Branch_0x10c

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x10d

 Branch_0x10c:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x10d:

Start_Orphan_77:
 nop
End_Orphan_77:
 ldc.i4.8
 ldc.i4.s 37
 bge Branch_0x10e

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x10f

 Branch_0x10e:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x10f:

 bgt.un Branch_0x10a

Start_Orphan_78:
 nop
End_Orphan_78:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 rem
 br Branch_0x10b

 Branch_0x10a:

 ldarg Arg_0x8
 conv.ovf.u.un
 Branch_0x10b:

Start_Orphan_79:
 ldloc LOCAL_0x0
 ldarg Arg_0x4
 bgt Branch_0x110

 nop
 br Branch_0x111

 Branch_0x110:

 nop
 Branch_0x111:

End_Orphan_79:
Start_Orphan_7a:
 nop
End_Orphan_7a:
 ldc.i4.s -124
Start_Orphan_7b:
 nop
End_Orphan_7b:
 ldc.i4 0x10e7393e
 brfalse Branch_0x114

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x115

 Branch_0x114:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x115:

 bgt.un Branch_0x112

 ldloca LOCAL_0x9
 ldind.i4
 br Branch_0x113

 Branch_0x112:

Start_Orphan_7c:
 nop
End_Orphan_7c:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x0
 ldlen
 Branch_0x113:

 bgt.un Branch_0x108

Start_Orphan_7d:
 nop
End_Orphan_7d:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x2
 ldc.i4.5
 ldelema [mscorlib]System.UInt64
 ldind.u8
 br Branch_0x109

 Branch_0x108:

Start_Orphan_7e:
 nop
End_Orphan_7e:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 cgt
 conv.ovf.u8
 Branch_0x109:

 sub
 bge.un Branch_0xe0

Start_Orphan_7f:
Start_Orphan_80:
 ldsflda native int ILGEN_0xd73e17d6::field_0x6
 pop
End_Orphan_80:
 ldsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
 stsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
End_Orphan_7f:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.7
 bne.un Branch_0x11a

 ldloc LOCAL_0x0
 br Branch_0x11b

 Branch_0x11a:

 ldloc LOCAL_0x0
 Branch_0x11b:

 ldarg Arg_0x9
 ldc.i4.6
 blt Branch_0x11c

 ldc.r4 float32(0x2f7870f1)
 br Branch_0x11d

 Branch_0x11c:

 ldc.r4 float32(0xbb5f6c6a)
 Branch_0x11d:

 rem 
Start_Orphan_81:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble.un Branch_0x11e

 nop
 br Branch_0x11f

 Branch_0x11e:

 nop
 Branch_0x11f:

End_Orphan_81:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 add
 brfalse Branch_0x120

 ldc.i8 0x36fbb67adc00bdaa
 conv.r4
 br Branch_0x121

 Branch_0x120:

 ldarg Arg_0x4
 ldarg Arg_0x6
 blt Branch_0x122

 ldc.r8 float64(0x84b7f95a17ca313b)
 br Branch_0x123

 Branch_0x122:

 ldc.r8 float64(0xd503707c9d8e6f99)
 Branch_0x123:

 Branch_0x121:

 ble Branch_0x118

Start_Orphan_82:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt.un Branch_0x124

 nop
 br Branch_0x125

 Branch_0x124:

 nop
 Branch_0x125:

End_Orphan_82:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x2
Start_Orphan_83:
 nop
End_Orphan_83:
 ldarg Arg_0x9
 ldelem.u8
 br Branch_0x119

 Branch_0x118:

Start_Orphan_84:
 ldarga Arg_0x3
 ldloc LOCAL_0x9
 stind.i4
End_Orphan_84:
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 conv.ovf.i2.un
Start_Orphan_85:
 nop
End_Orphan_85:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4 0x6f7a8be0
 blt Branch_0x128

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x129

 Branch_0x128:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x129:

 bne.un Branch_0x126

Start_Orphan_86:
 nop
End_Orphan_86:
 ldc.i4.4
 conv.ovf.i8
 br Branch_0x127

 Branch_0x126:

 ldloc LOCAL_0x8
 ldc.i4.s -86
 bge.un Branch_0x12a

 ldarg Arg_0x2
 br Branch_0x12b

 Branch_0x12a:

 ldloc LOCAL_0x4
 Branch_0x12b:

 Branch_0x127:

 Branch_0x119:

Start_Orphan_87:
 ldc.i4.m1
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt.un Branch_0x12e

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x12f

 Branch_0x12e:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x12f:

Start_Orphan_88:
 nop
End_Orphan_88:
 ldloc LOCAL_0x8
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 add
 bge Branch_0x12c

 ldc.r8 float64(0xc85975d5f1561da2)
 ldarg Arg_0x6
 blt.un Branch_0x130

 nop
 br Branch_0x131

 Branch_0x130:

 nop
 Branch_0x131:

 br Branch_0x12d

 Branch_0x12c:

 ldc.i4.7
 ldc.i4 0x7fe158ac
 bgt.un Branch_0x132

 nop
 br Branch_0x133

 Branch_0x132:

 nop
 Branch_0x133:

 Branch_0x12d:

End_Orphan_87:
 ldarg Arg_0x8
 beq Branch_0x116

Start_Orphan_89:
 ldarga Arg_0x7
 ldc.i8 0x6225ad6fc6cfc636
 stind.i8
End_Orphan_89:
 ldarg Arg_0x0
Start_Orphan_8a:
 nop
End_Orphan_8a:
 ldc.r8 float64(0xbab51a71296766b5)
 conv.ovf.i4
 bgt.un Branch_0x136

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.4
 mul.ovf.un
 br Branch_0x137

 Branch_0x136:

Start_Orphan_8b:
 nop
End_Orphan_8b:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt.un Branch_0x138

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x139

 Branch_0x138:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x139:

 Branch_0x137:

Start_Orphan_8c:
 ldc.i4.3
 ldc.i4.7
 bge Branch_0x13a

 nop
 br Branch_0x13b

 Branch_0x13a:

 nop
 Branch_0x13b:

End_Orphan_8c:
 ldc.i4.7
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 sub.ovf
Start_Orphan_8d:
 nop
End_Orphan_8d:
 ldc.i4.0
 mul.ovf.un
 blt Branch_0x134

Start_Orphan_8e:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.8
 blt.un Branch_0x13c

 nop
 br Branch_0x13d

 Branch_0x13c:

 nop
 Branch_0x13d:

End_Orphan_8e:
 ldloc LOCAL_0x6
 conv.ovf.u8.un
 conv.r.un
 br Branch_0x135

 Branch_0x134:

Start_Orphan_8f:
 ldarg Arg_0x2
 pop
End_Orphan_8f:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4 0xc17d23e5
 beq Branch_0x140

 ldarg Arg_0x6
 br Branch_0x141

 Branch_0x140:

 ldloc LOCAL_0x0
 Branch_0x141:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x142

 ldarg Arg_0x4
 br Branch_0x143

 Branch_0x142:

 ldarg Arg_0x6
 Branch_0x143:

 blt Branch_0x13e

Start_Orphan_90:
 nop
End_Orphan_90:
 ldloc LOCAL_0x0
 br Branch_0x13f

 Branch_0x13e:

 ldc.r8 float64(0xfcbac1e698a98b73)
 call float32 [mscorlib]System.Math::Abs(float32)
 Branch_0x13f:

 Branch_0x135:

 br Branch_0x117

 Branch_0x116:

Start_Orphan_91:
 ldc.r8 float64(0xfdaa78d6cec3b3bd)
 ldarg Arg_0x4
 ble.un Branch_0x144

 nop
 br Branch_0x145

 Branch_0x144:

 nop
 Branch_0x145:

End_Orphan_91:
 ldarg Arg_0x3
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt Branch_0x146

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x147

 Branch_0x146:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x147:

Start_Orphan_92:
 nop
End_Orphan_92:
 ldc.i4.4
 ldc.i4 0x5555c59
 beq Branch_0x148

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x149

 Branch_0x148:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x149:

 clt.un
 conv.r.un
 Branch_0x117:

 br Branch_0xe1

 Branch_0xe0:

Start_Orphan_93:
Start_Orphan_94:
 ldc.i4.2
 ldc.i4.4
 blt.un Branch_0x14a

 nop
 br Branch_0x14b

 Branch_0x14a:

 nop
 Branch_0x14b:

End_Orphan_94:
 ldloca LOCAL_0x6
 pop
End_Orphan_93:
Start_Orphan_95:
 ldloc LOCAL_0x5
 stloc LOCAL_0x5
End_Orphan_95:
Start_Orphan_96:
 nop
End_Orphan_96:
 ldarg Arg_0x8
 conv.i2
Start_Orphan_97:
 nop
End_Orphan_97:
 ldc.r4 float32(0x1e2a4763)
 ldloc LOCAL_0x0
 ceq
 bne.un Branch_0x150

 ldloc LOCAL_0x9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bne.un Branch_0x152

 ldarg Arg_0x4
 br Branch_0x153

 Branch_0x152:

 ldarg Arg_0x6
 Branch_0x153:

 br Branch_0x151

 Branch_0x150:

Start_Orphan_98:
 nop
End_Orphan_98:
 ldloc LOCAL_0x9
 ldloc LOCAL_0x2
 ble.un Branch_0x154

 ldc.r4 float32(0x890232d6)
 br Branch_0x155

 Branch_0x154:

 ldc.r4 float32(0xcea0e87)
 Branch_0x155:

 Branch_0x151:

Start_Orphan_99:
 ldarg Arg_0x0
 ldc.i4.7
 blt Branch_0x156

 nop
 br Branch_0x157

 Branch_0x156:

 nop
 Branch_0x157:

End_Orphan_99:
 ldc.r4 float32(0xf714a286)
 bge.un Branch_0x14e

Start_Orphan_9a:
 nop
End_Orphan_9a:
 ldarg Arg_0x0
 conv.ovf.u4.un
Start_Orphan_9b:
 nop
End_Orphan_9b:
 ldc.i4.1
 ldarg Arg_0x9
 add
 mul
 br Branch_0x14f

 Branch_0x14e:

 ldc.i4.2
 ldloc LOCAL_0x5
 ldlen
 blt.un Branch_0x158

 ldloc LOCAL_0x9
 not
 br Branch_0x159

 Branch_0x158:

 ldc.i4 0x91b6512f
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt.un Branch_0x15a

 ldc.i4.5
 br Branch_0x15b

 Branch_0x15a:

 ldarg Arg_0x0
 Branch_0x15b:

 Branch_0x159:

 Branch_0x14f:

Start_Orphan_9c:
Start_Orphan_9d:
 nop
End_Orphan_9d:
 ldc.i4.3
 conv.u1
 newarr [mscorlib]System.Int32
 pop
End_Orphan_9c:
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
Start_Orphan_9e:
 nop
End_Orphan_9e:
 ldc.i4.4
 conv.ovf.u4
 conv.ovf.i2
 add.ovf.un
 blt Branch_0x14c

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt Branch_0x160

 ldarg Arg_0x4
 br Branch_0x161

 Branch_0x160:

 ldarg Arg_0x6
 Branch_0x161:

 ldarg Arg_0x1
 conv.r8
 ble Branch_0x15e

Start_Orphan_9f:
 nop
End_Orphan_9f:
 ldc.i4.1
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt.un Branch_0x162

 ldc.r4 float32(0x15f8987)
 br Branch_0x163

 Branch_0x162:

 ldc.r4 float32(0xd7e973ce)
 Branch_0x163:

 br Branch_0x15f

 Branch_0x15e:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x164

 ldc.r8 float64(0xbf263f2d61b69cc7)
 br Branch_0x165

 Branch_0x164:

 ldc.r8 float64(0xe15a173083e813fe)
 Branch_0x165:

 Branch_0x15f:

 ldloc LOCAL_0x1
 ldlen
 brtrue Branch_0x166

Start_Orphan_a0:
 nop
End_Orphan_a0:
 ldc.i4.4
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt.un Branch_0x168

 ldarg Arg_0x6
 br Branch_0x169

 Branch_0x168:

 ldarg Arg_0x6
 Branch_0x169:

 br Branch_0x167

 Branch_0x166:

Start_Orphan_a1:
 nop
End_Orphan_a1:
 ldc.i4.5
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 bgt.un Branch_0x16a

 ldc.r4 float32(0x44968116)
 br Branch_0x16b

 Branch_0x16a:

 ldc.r4 float32(0xcfbf46b4)
 Branch_0x16b:

 Branch_0x167:

 blt.un Branch_0x15c

 ldarg Arg_0x9
 conv.ovf.u.un
Start_Orphan_a2:
 nop
End_Orphan_a2:
 ldarg Arg_0x9
 ldc.i4.3
 cgt.un
 beq Branch_0x16c

Start_Orphan_a3:
 nop
End_Orphan_a3:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldarg Arg_0x3
 bge Branch_0x16e

 ldc.r8 float64(0xac3934a21e19ccde)
 br Branch_0x16f

 Branch_0x16e:

 ldarg Arg_0x6
 Branch_0x16f:

 br Branch_0x16d

 Branch_0x16c:

Start_Orphan_a4:
 nop
End_Orphan_a4:
 ldc.r4 float32(0x507dc41f)
 call float64 [mscorlib]System.Math::Round(float64)
 Branch_0x16d:

 br Branch_0x15d

 Branch_0x15c:

Start_Orphan_a5:
 ldarg Arg_0x0
 ldc.i4.8
 ble.un Branch_0x170

 nop
 br Branch_0x171

 Branch_0x170:

 nop
 Branch_0x171:

End_Orphan_a5:
 ldarg Arg_0x8
 conv.r4
 call float64 [mscorlib]System.Math::Sin(float64)
 Branch_0x15d:

 br Branch_0x14d

 Branch_0x14c:

Start_Orphan_a6:
 ldloca LOCAL_0x6
 pop
End_Orphan_a6:
Start_Orphan_a7:
 nop
End_Orphan_a7:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i.un
Start_Orphan_a8:
 nop
End_Orphan_a8:
 ldc.i8 0x4b0ca10e5135eaa3
 ldloc LOCAL_0x4
 bgt Branch_0x176

 ldc.i4.7
 br Branch_0x177

 Branch_0x176:

 ldc.i4 0xeb70d58
 Branch_0x177:

 beq Branch_0x174

Start_Orphan_a9:
 nop
End_Orphan_a9:
 ldc.i4.4
 br Branch_0x175

 Branch_0x174:

Start_Orphan_aa:
 nop
End_Orphan_aa:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.4
 cgt.un
 Branch_0x175:

Start_Orphan_ab:
 ldc.r8 float64(0x532418c9117b9fb)
 ldc.r8 float64(0x3b6109d27eb2f4fc)
 blt.un Branch_0x178

 nop
 br Branch_0x179

 Branch_0x178:

 nop
 Branch_0x179:

End_Orphan_ab:
Start_Orphan_ac:
 nop
End_Orphan_ac:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 ble.un Branch_0x17a

 ldarg Arg_0x8
 br Branch_0x17b

 Branch_0x17a:

 ldloc LOCAL_0x4
 Branch_0x17b:

 ldarg Arg_0x6
 ldc.r4 float32(0xc747a78a)
 beq Branch_0x17c

 ldloc LOCAL_0x4
 br Branch_0x17d

 Branch_0x17c:

 ldarg Arg_0x8
 Branch_0x17d:

 clt.un
 bne.un Branch_0x172

Start_Orphan_ad:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldarg Arg_0x3
 bgt.un Branch_0x17e

 nop
 br Branch_0x17f

 Branch_0x17e:

 nop
 Branch_0x17f:

End_Orphan_ad:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brfalse Branch_0x182

 ldarg Arg_0x6
 br Branch_0x183

 Branch_0x182:

 ldarg Arg_0x4
 Branch_0x183:

Start_Orphan_ae:
 nop
End_Orphan_ae:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt Branch_0x184

 ldarg Arg_0x6
 br Branch_0x185

 Branch_0x184:

 ldc.r8 float64(0xebb41c37fb914f19)
 Branch_0x185:

 bgt.un Branch_0x180

Start_Orphan_af:
 nop
End_Orphan_af:
 ldarg Arg_0x8
 ldarg Arg_0x8
 blt.un Branch_0x186

 ldarg Arg_0x4
 br Branch_0x187

 Branch_0x186:

 ldc.r4 float32(0xd69daf21)
 Branch_0x187:

 br Branch_0x181

 Branch_0x180:

 ldloc LOCAL_0x0
 ldc.r8 float64(0xd9249c48b12281)
 bge.un Branch_0x188

 ldc.r4 float32(0xc482480d)
 br Branch_0x189

 Branch_0x188:

 ldarg Arg_0x6
 Branch_0x189:

 Branch_0x181:

 br Branch_0x173

 Branch_0x172:

 ldloc LOCAL_0x7
 ldarg Arg_0x8
 clt
Start_Orphan_b0:
 nop
End_Orphan_b0:
 ldarg Arg_0x5
 conv.i4
 bgt Branch_0x18a

 ldloc LOCAL_0x0
 ldarg Arg_0x6
 sub  
 br Branch_0x18b

 Branch_0x18a:

Start_Orphan_b1:
 nop
End_Orphan_b1:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0x18c

 ldarg Arg_0x6
 br Branch_0x18d

 Branch_0x18c:

 ldloc LOCAL_0x0
 Branch_0x18d:

 Branch_0x18b:

 Branch_0x173:

 Branch_0x14d:

 Branch_0xe1:

 conv.ovf.i8
Start_Orphan_b2:
Start_Orphan_b3:
Start_Orphan_b4:
 nop
End_Orphan_b4:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble.un Branch_0x190

 ldloc LOCAL_0x9
 br Branch_0x191

 Branch_0x190:

 ldc.i4.m1
 Branch_0x191:

 conv.u1
Start_Orphan_b5:
 nop
End_Orphan_b5:
 ldarg Arg_0x4
 ldc.r8 float64(0x7ac78959fdaa30ca)
 ble Branch_0x194

 ldc.r4 float32(0xe46860af)
 br Branch_0x195

 Branch_0x194:

 ldloc LOCAL_0x0
 Branch_0x195:

Start_Orphan_b6:
 nop
End_Orphan_b6:
 ldloc LOCAL_0x6
 ldarg Arg_0x7
 ble Branch_0x196

 ldarg Arg_0x6
 br Branch_0x197

 Branch_0x196:

 ldc.r4 float32(0xa636cbe1)
 Branch_0x197:

 blt.un Branch_0x192

 ldarg Arg_0x9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 sub
 br Branch_0x193

 Branch_0x192:

Start_Orphan_b7:
 nop
End_Orphan_b7:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldloc LOCAL_0x8
 blt Branch_0x198

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x199

 Branch_0x198:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x199:

 Branch_0x193:

 bge Branch_0x18e

 ldc.r8 float64(0xb836018b88679e4a)
 conv.ovf.i8
Start_Orphan_b8:
 nop
End_Orphan_b8:
 ldloc LOCAL_0x7
 ldarg Arg_0x2
 bge Branch_0x19c

 ldloc LOCAL_0x4
 br Branch_0x19d

 Branch_0x19c:

 ldc.i8 0x36575fa2a85b3827
 Branch_0x19d:

 ble Branch_0x19a

 ldarg Arg_0x8
 starg Arg_0x8
 br Branch_0x19b

 Branch_0x19a:

 ldsfld float64[] ILGEN_0xd73e17d6::field_0x7
 stloc LOCAL_0x5
 Branch_0x19b:

 br Branch_0x18f

 Branch_0x18e:

Start_Orphan_b9:
 nop
End_Orphan_b9:
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 conv.ovf.i2
 brfalse Branch_0x19e

 ldarga Arg_0x4
 pop
 br Branch_0x19f

 Branch_0x19e:

 ldarg Arg_0x6
 ldc.r4 float32(0x302ae7f9)
 beq Branch_0x1a0

 nop
 br Branch_0x1a1

 Branch_0x1a0:

 nop
 Branch_0x1a1:

 Branch_0x19f:

 Branch_0x18f:

End_Orphan_b3:
 ldarga Arg_0x4
 pop
End_Orphan_b2:
 ldloc LOCAL_0x4
 bgt Branch_0x7e

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brtrue Branch_0x1a6

 ldloc LOCAL_0x0
 br Branch_0x1a7

 Branch_0x1a6:

 ldc.r8 float64(0x376915247a95c5a0)
 Branch_0x1a7:

 ldsfld float64[] ILGEN_0xd73e17d6::field_0x7
 ldc.i4.2
 ldelem.r8
 ble.un Branch_0x1a4

 ldloc LOCAL_0x0
 conv.ovf.u8
 br Branch_0x1a5

 Branch_0x1a4:

 ldc.i4 0xd84d72a5
 ldarg Arg_0x9
 blt Branch_0x1a8

 ldc.i8 0xd3c3b543bd2a8ba6
 br Branch_0x1a9

 Branch_0x1a8:

 ldc.i8 0xd80cdf40232b1262
 Branch_0x1a9:

 Branch_0x1a5:

 ldc.r8 float64(0xe2acd43cc622047b)
 conv.ovf.i
 conv.ovf.u8.un
 ble Branch_0x1a2

Start_Orphan_ba:
 nop
End_Orphan_ba:
 ldarg Arg_0x8
 ldc.i8 0xee651b2f787c433
 sub
 conv.ovf.i.un
 br Branch_0x1a3

 Branch_0x1a2:

Start_Orphan_bb:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 stsfld native int ILGEN_0xd73e17d6::field_0x6
End_Orphan_bb:
Start_Orphan_bc:
 nop
End_Orphan_bc:
 ldarg Arg_0x4
 ldarg Arg_0x4
 add 
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0x1ac

 ldc.r8 float64(0xf6fd0e325ee014b3)
 br Branch_0x1ad

 Branch_0x1ac:

 ldc.r8 float64(0xd4889271ad5a3fd8)
 Branch_0x1ad:

 bge.un Branch_0x1aa

 ldarg Arg_0x9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 mul.ovf
 br Branch_0x1ab

 Branch_0x1aa:

 ldc.r8 float64(0xe24173ad5c7da15a)
 ldarg Arg_0x6
 bne.un Branch_0x1ae

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x1af

 Branch_0x1ae:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x1af:

 Branch_0x1ab:

 Branch_0x1a3:

Start_Orphan_bd:
 ldc.i4.4
 ldc.i4.5
 bgt.un Branch_0x1b2

 nop
 br Branch_0x1b3

 Branch_0x1b2:

 nop
 Branch_0x1b3:

End_Orphan_bd:
 ldarga Arg_0x9
 ldind.u4
Start_Orphan_be:
 ldc.i4.8
 starg Arg_0x0
End_Orphan_be:
 ldarg Arg_0x0
 ldarg Arg_0x3
 shl
Start_Orphan_bf:
 nop
End_Orphan_bf:
 ldc.i4 0xa3ad5ef9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 clt.un
 clt
 bgt.un Branch_0x1b0

Start_Orphan_c0:
 ldloc LOCAL_0x0
 ldc.r8 float64(0x776b787eb82521ae)
 bgt Branch_0x1b4

 nop
 br Branch_0x1b5

 Branch_0x1b4:

 nop
 Branch_0x1b5:

End_Orphan_c0:
Start_Orphan_c1:
 nop
End_Orphan_c1:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brfalse Branch_0x1b6

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x1b7

 Branch_0x1b6:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x1b7:

 ldarg Arg_0x3
 sub
 br Branch_0x1b1

 Branch_0x1b0:

Start_Orphan_c2:
 nop
End_Orphan_c2:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.6
 ceq
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldloc LOCAL_0x9
 mul.ovf.un
 blt.un Branch_0x1b8

Start_Orphan_c3:
 nop
End_Orphan_c3:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldarg Arg_0x9
 mul.ovf
 br Branch_0x1b9

 Branch_0x1b8:

Start_Orphan_c4:
 nop
End_Orphan_c4:
 ldc.r4 float32(0xa7a8537d)
 conv.i
 Branch_0x1b9:

 Branch_0x1b1:

 add.ovf.un
 conv.ovf.u8
Start_Orphan_c5:
Start_Orphan_c6:
 ldarga Arg_0x9
 pop
End_Orphan_c6:
Start_Orphan_c7:
 nop
End_Orphan_c7:
 ldc.i8 0x39e8fcb5d169717
 ldarg Arg_0x1
 mul
 conv.ovf.i1.un
Start_Orphan_c8:
 ldarg Arg_0x6
 ldarg Arg_0x4
 ble.un Branch_0x1be

 nop
 br Branch_0x1bf

 Branch_0x1be:

 nop
 Branch_0x1bf:

End_Orphan_c8:
Start_Orphan_c9:
 nop
End_Orphan_c9:
 ldc.i4.3
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x1c2

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x1c3

 Branch_0x1c2:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x1c3:

 ldc.i4.3
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 add.ovf.un
 bne.un Branch_0x1c0

Start_Orphan_ca:
 nop
End_Orphan_ca:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 cgt
 br Branch_0x1c1

 Branch_0x1c0:

 ldc.i4.7
 conv.i4
 Branch_0x1c1:

 bge Branch_0x1bc

 ldc.i4.6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt.un Branch_0x1c4

 ldc.i8 0x74fff7baa00ed81a
 br Branch_0x1c5

 Branch_0x1c4:

 ldc.i8 0xcbe73439a7842309
 Branch_0x1c5:

Start_Orphan_cb:
 nop
End_Orphan_cb:
 ldarg Arg_0x7
 ldarg Arg_0x8
 rem.un
 sub.ovf
 br Branch_0x1bd

 Branch_0x1bc:

Start_Orphan_cc:
 ldloc LOCAL_0x8
 ldc.i4.s -105
 ble Branch_0x1c6

 nop
 br Branch_0x1c7

 Branch_0x1c6:

 nop
 Branch_0x1c7:

End_Orphan_cc:
Start_Orphan_cd:
 nop
End_Orphan_cd:
 ldarg Arg_0x2
 ldarg Arg_0x8
 or
Start_Orphan_ce:
 nop
End_Orphan_ce:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.u8
 div
 Branch_0x1bd:

 ldc.i4.7
Start_Orphan_cf:
 ldloc LOCAL_0x0
 ldloc LOCAL_0x0
 bge.un Branch_0x1ca

 nop
 br Branch_0x1cb

 Branch_0x1ca:

 nop
 Branch_0x1cb:

End_Orphan_cf:
Start_Orphan_d0:
 nop
End_Orphan_d0:
 ldc.i4.s -54
 brfalse Branch_0x1cc

Start_Orphan_d1:
 nop
End_Orphan_d1:
 ldarg Arg_0x0
 conv.u
 br Branch_0x1cd

 Branch_0x1cc:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.u.un
 Branch_0x1cd:

 bge.un Branch_0x1c8

Start_Orphan_d2:
 ldsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
 stsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
End_Orphan_d2:
Start_Orphan_d3:
 nop
End_Orphan_d3:
 ldc.i4.3
 conv.ovf.u2
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 add.ovf.un
 bgt Branch_0x1ce

 ldc.i4.2
 conv.ovf.i8
 br Branch_0x1cf

 Branch_0x1ce:

 ldloc LOCAL_0x6
 ldarg Arg_0x5
 div.un
 Branch_0x1cf:

 br Branch_0x1c9

 Branch_0x1c8:

 ldarg Arg_0x3
 ldloc LOCAL_0x2
 clt.un
 conv.ovf.u8.un
 Branch_0x1c9:

 bge Branch_0x1ba

Start_Orphan_d4:
 ldc.r8 float64(0xdf6f9183d0115abc)
 ldc.r4 float32(0x9b57f369)
 bgt.un Branch_0x1d0

 nop
 br Branch_0x1d1

 Branch_0x1d0:

 nop
 Branch_0x1d1:

End_Orphan_d4:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x2
Start_Orphan_d5:
 nop
End_Orphan_d5:
 ldc.i8 0xa9720c5a1ca2a2f0
 conv.ovf.i8.un
Start_Orphan_d6:
 nop
End_Orphan_d6:
 ldarg Arg_0x5
 ldarg Arg_0x7
 bgt.un Branch_0x1d2

 ldarg Arg_0x1
 br Branch_0x1d3

 Branch_0x1d2:

 ldarg Arg_0x8
 Branch_0x1d3:

 ceq
Start_Orphan_d7:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble Branch_0x1d4

 nop
 br Branch_0x1d5

 Branch_0x1d4:

 nop
 Branch_0x1d5:

End_Orphan_d7:
 ldarg Arg_0x1
 ldarg Arg_0x5
 bge Branch_0x1d6

 ldarg Arg_0x9
 br Branch_0x1d7

 Branch_0x1d6:

 ldloc LOCAL_0x9
 Branch_0x1d7:

 conv.u8
 stelem.i8
 br Branch_0x1bb

 Branch_0x1ba:

Start_Orphan_d8:
 nop
End_Orphan_d8:
 ldsfld float64[] ILGEN_0xd73e17d6::field_0x4
 ldc.i4.s -1
 conv.ovf.u1
 ldelema [mscorlib]System.Double
 pop
 Branch_0x1bb:

End_Orphan_c5:
Start_Orphan_d9:
Start_Orphan_da:
 nop
End_Orphan_da:
 ldarg Arg_0x4
 ldc.r8 float64(0xd83b175b39e1408a)
 bge Branch_0x1de

 ldarg Arg_0x0
 br Branch_0x1df

 Branch_0x1de:

 ldc.i4.5
 Branch_0x1df:

 brtrue Branch_0x1dc

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x1e0

 ldarg Arg_0x7
 br Branch_0x1e1

 Branch_0x1e0:

 ldc.i8 0x5d8c1057ad057737
 Branch_0x1e1:

 br Branch_0x1dd

 Branch_0x1dc:

Start_Orphan_db:
 nop
End_Orphan_db:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.1
 bne.un Branch_0x1e2

 ldarg Arg_0x2
 br Branch_0x1e3

 Branch_0x1e2:

 ldloc LOCAL_0x7
 Branch_0x1e3:

 Branch_0x1dd:

Start_Orphan_dc:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brtrue Branch_0x1e4

 nop
 br Branch_0x1e5

 Branch_0x1e4:

 nop
 Branch_0x1e5:

End_Orphan_dc:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.6
 bne.un Branch_0x1e8

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x1e9

 Branch_0x1e8:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x1e9:

 ldc.i4.5
 conv.u
 ble Branch_0x1e6

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brtrue Branch_0x1ea

 ldloc LOCAL_0x4
 br Branch_0x1eb

 Branch_0x1ea:

 ldc.i8 0x75d548bc54e02d74
 Branch_0x1eb:

 br Branch_0x1e7

 Branch_0x1e6:

 ldc.i8 0xadd635da2579d35d
 ldarg Arg_0x7
 bgt.un Branch_0x1ec

 ldarg Arg_0x2
 br Branch_0x1ed

 Branch_0x1ec:

 ldc.i8 0x13d45a7f1fede4df
 Branch_0x1ed:

 Branch_0x1e7:

 ble Branch_0x1da

 ldloca LOCAL_0x7
Start_Orphan_dd:
 nop
End_Orphan_dd:
 ldarg Arg_0x8
 ldc.i8 0xf0ad66b393bbac3d
 and
 stind.i8
 br Branch_0x1db

 Branch_0x1da:

Start_Orphan_de:
 nop
End_Orphan_de:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.4
 cgt
 ldloc LOCAL_0x2
 conv.ovf.i4
 beq Branch_0x1ee

 nop
 br Branch_0x1ef

 Branch_0x1ee:

 ldsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x3
 pop
 Branch_0x1ef:

 Branch_0x1db:

End_Orphan_d9:
Start_Orphan_df:
Start_Orphan_e0:
 nop
End_Orphan_e0:
 ldarg Arg_0x6
 conv.ovf.i2
 ldloc LOCAL_0x0
 ldloc LOCAL_0x0
 bne.un Branch_0x1f2

 ldarg Arg_0x9
 br Branch_0x1f3

 Branch_0x1f2:

 ldarg Arg_0x0
 Branch_0x1f3:

 bge Branch_0x1f0

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brfalse Branch_0x1f4

 nop
 br Branch_0x1f5

 Branch_0x1f4:

 nop
 Branch_0x1f5:

 br Branch_0x1f1

 Branch_0x1f0:

 ldsfld float64[] ILGEN_0xd73e17d6::field_0x7
 stsfld float64[] ILGEN_0xd73e17d6::field_0x4
 Branch_0x1f1:

End_Orphan_df:
 ldc.i4 0xe57385c3
Start_Orphan_e1:
 ldarg Arg_0x9
 conv.u1
 newarr [mscorlib]System.Double
 stsfld float64[] ILGEN_0xd73e17d6::field_0x4
End_Orphan_e1:
 ldc.r8 float64(0x682d374c82df4bc0)
 ldc.r8 float64(0x8e8881f8bb11c56f)
 bge.un Branch_0x1f8

 ldloc LOCAL_0x4
 br Branch_0x1f9

 Branch_0x1f8:

 ldloc LOCAL_0x6
 Branch_0x1f9:

 ldloc LOCAL_0x9
 not
 shr
 ldarg Arg_0x5
 ldloc LOCAL_0x4
 bne.un Branch_0x1fc

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x1fd

 Branch_0x1fc:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x1fd:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i4.un
 bne.un Branch_0x1fa

 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x2
 ldloc LOCAL_0x2
 ldelem.u8
 br Branch_0x1fb

 Branch_0x1fa:

Start_Orphan_e2:
 nop
End_Orphan_e2:
 ldarg Arg_0x8
 ldc.i8 0x3c51d82b8b1abdab
 mul.ovf
 Branch_0x1fb:

 bge Branch_0x1f6

Start_Orphan_e3:
 ldc.i4.1
 brtrue Branch_0x1fe

 nop
 br Branch_0x1ff

 Branch_0x1fe:

 nop
 Branch_0x1ff:

End_Orphan_e3:
Start_Orphan_e4:
 nop
End_Orphan_e4:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.3
 bge Branch_0x200

 ldarg Arg_0x6
 br Branch_0x201

 Branch_0x200:

 ldc.r8 float64(0x9e82814425d48686)
 Branch_0x201:

 conv.u
 br Branch_0x1f7

 Branch_0x1f6:

Start_Orphan_e5:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
 pop
End_Orphan_e5:
Start_Orphan_e6:
 nop
End_Orphan_e6:
 ldloc LOCAL_0x6
 ldarg Arg_0x1
 bne.un Branch_0x204

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x205

 Branch_0x204:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x205:

Start_Orphan_e7:
 nop
End_Orphan_e7:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble Branch_0x206

 ldc.i4.1
 br Branch_0x207

 Branch_0x206:

 ldc.i4.s 112
 Branch_0x207:

 blt.un Branch_0x202

Start_Orphan_e8:
 nop
End_Orphan_e8:
 ldsflda native int ILGEN_0xd73e17d6::field_0x6
 ldind.i
 br Branch_0x203

 Branch_0x202:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bne.un Branch_0x208

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x209

 Branch_0x208:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x209:

 Branch_0x203:

 Branch_0x1f7:

 mul.ovf.un
Start_Orphan_e9:
 ldc.i8 0xd1a06c5aab36ab2a
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt.un Branch_0x20e

 ldc.i8 0xa8c1cb06237d422
 br Branch_0x20f

 Branch_0x20e:

 ldarg Arg_0x7
 Branch_0x20f:

 bge.un Branch_0x20c

Start_Orphan_ea:
 nop
End_Orphan_ea:
 ldc.i4.7
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 sub
 br Branch_0x20d

 Branch_0x20c:

Start_Orphan_eb:
 nop
End_Orphan_eb:
 ldarg Arg_0x8
 conv.ovf.i1.un
 Branch_0x20d:

Start_Orphan_ec:
 ldloc LOCAL_0x2
 ldc.i4.1
 bge.un Branch_0x210

 nop
 br Branch_0x211

 Branch_0x210:

 nop
 Branch_0x211:

End_Orphan_ec:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt.un Branch_0x212

 ldc.i4.m1
 br Branch_0x213

 Branch_0x212:

 ldc.i4.5
 Branch_0x213:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.s -58
 add
 clt
 blt.un Branch_0x20a

Start_Orphan_ed:
 nop
End_Orphan_ed:
 ldsflda native int ILGEN_0xd73e17d6::field_0x6
 pop
 br Branch_0x20b

 Branch_0x20a:

 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
 ldlen
 stloc LOCAL_0x2
 Branch_0x20b:

End_Orphan_e9:
Start_Orphan_ee:
Start_Orphan_ef:
 nop
End_Orphan_ef:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.4
 bne.un Branch_0x216

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x217

 Branch_0x216:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x217:

Start_Orphan_f0:
 nop
End_Orphan_f0:
 ldc.i4 0x76ad7c88
 bgt.un Branch_0x214

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.4
 bgt.un Branch_0x218

 nop
 br Branch_0x219

 Branch_0x218:

 nop
 Branch_0x219:

 br Branch_0x215

 Branch_0x214:

 ldloc LOCAL_0x1
 ldc.i4.2
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 stelem.i
 Branch_0x215:

End_Orphan_ee:
Start_Orphan_f1:
 nop
End_Orphan_f1:
 ldloc LOCAL_0x2
 ldc.i4.m1
 ble.un Branch_0x21a

 ldarg Arg_0x9
 br Branch_0x21b

 Branch_0x21a:

 ldc.i4.7
 Branch_0x21b:

 conv.u1
 newarr [mscorlib]System.IntPtr
 ldlen
Start_Orphan_f2:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt Branch_0x21e

 nop
 br Branch_0x21f

 Branch_0x21e:

 nop
 Branch_0x21f:

End_Orphan_f2:
 ldarg Arg_0x3
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 beq Branch_0x222

 ldc.i4.1
 br Branch_0x223

 Branch_0x222:

 ldc.i4.4
 Branch_0x223:

Start_Orphan_f3:
 nop
End_Orphan_f3:
 ldc.i4.0
 bne.un Branch_0x220

 ldarg Arg_0x2
 ldc.i4.6
 shr.un
 br Branch_0x221

 Branch_0x220:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt.un Branch_0x224

 ldarg Arg_0x1
 br Branch_0x225

 Branch_0x224:

 ldarg Arg_0x8
 Branch_0x225:

 Branch_0x221:

 ldarg Arg_0x8
 ldarg Arg_0x2
 div.un
 ldarg Arg_0x5
 ble Branch_0x226

Start_Orphan_f4:
 nop
End_Orphan_f4:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i8.un
 br Branch_0x227

 Branch_0x226:

Start_Orphan_f5:
 nop
End_Orphan_f5:
 ldc.i4.3
 ldc.i4.6
 bne.un Branch_0x228

 ldloc LOCAL_0x7
 br Branch_0x229

 Branch_0x228:

 ldarg Arg_0x8
 Branch_0x229:

 Branch_0x227:

 bgt.un Branch_0x21c

Start_Orphan_f6:
 nop
End_Orphan_f6:
 ldloc LOCAL_0x0
 ldarg Arg_0x4
 blt Branch_0x22c

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x22d

 Branch_0x22c:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x22d:

 ldloc LOCAL_0x8
 conv.ovf.i1
 bgt Branch_0x22a

Start_Orphan_f7:
 nop
End_Orphan_f7:
 ldc.i4.4
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble Branch_0x22e

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x22f

 Branch_0x22e:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x22f:

 br Branch_0x22b

 Branch_0x22a:

 ldloc LOCAL_0x7
 conv.ovf.i2.un
 Branch_0x22b:

 br Branch_0x21d

 Branch_0x21c:

 ldloc LOCAL_0x2
 ldc.i8 0x4136dedf87f3d55b
 ldarg Arg_0x7
 bge Branch_0x232

 ldc.i4.1
 br Branch_0x233

 Branch_0x232:

 ldc.i4.7
 Branch_0x233:

 bne.un Branch_0x230

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 and
 br Branch_0x231

 Branch_0x230:

 ldloc LOCAL_0x8
 brtrue Branch_0x234

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x235

 Branch_0x234:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x235:

 Branch_0x231:

 Branch_0x21d:

 rem.un
 blt Branch_0x1d8

Start_Orphan_f8:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.s -123
 bgt Branch_0x238

 ldc.r8 float64(0x567579931beb905)
 br Branch_0x239

 Branch_0x238:

 ldarg Arg_0x6
 Branch_0x239:

 conv.u
 ldc.i4.0
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0x23c

 ldc.i4.4
 br Branch_0x23d

 Branch_0x23c:

 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 Branch_0x23d:

Start_Orphan_f9:
 nop
End_Orphan_f9:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldloc LOCAL_0x8
 sub
 bge Branch_0x23a

 ldc.i4.s 20
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 add.ovf.un
 br Branch_0x23b

 Branch_0x23a:

Start_Orphan_fa:
 nop
End_Orphan_fa:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.2
 blt Branch_0x23e

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x23f

 Branch_0x23e:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x23f:

 Branch_0x23b:

 bgt Branch_0x236

 ldloc LOCAL_0x0
 ldc.r8 float64(0xe24f09ae2ae9fe7)
 bne.un Branch_0x240

 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 br Branch_0x241

 Branch_0x240:

 ldc.i4.5
 Branch_0x241:

 stloc LOCAL_0x2
 br Branch_0x237

 Branch_0x236:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.u8
Start_Orphan_fb:
 nop
End_Orphan_fb:
 ldc.i4.3
 ldarg Arg_0x9
 bge.un Branch_0x244

 ldc.i8 0xdd55f9c020ee6c57
 br Branch_0x245

 Branch_0x244:

 ldc.i8 0x408e64f85c4c7495
 Branch_0x245:

 blt Branch_0x242

 ldsfld float64[] ILGEN_0xd73e17d6::field_0x4
 pop
 br Branch_0x243

 Branch_0x242:

 ldc.i4.1
 stsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 Branch_0x243:

 Branch_0x237:

End_Orphan_f8:
Start_Orphan_fc:
 nop
End_Orphan_fc:
 ldc.i4 0x4cac8d1d
 ldc.i4.m1
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0x248

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x249

 Branch_0x248:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x249:

 bge.un Branch_0x246

Start_Orphan_fd:
 nop
End_Orphan_fd:
 ldc.i8 0x38c95080994c3437
 ldarg Arg_0x1
 mul
 br Branch_0x247

 Branch_0x246:

Start_Orphan_fe:
 nop
End_Orphan_fe:
 ldarg Arg_0x8
 ldarg Arg_0x2
 add.ovf.un
 Branch_0x247:

 neg
Start_Orphan_ff:
 ldarg Arg_0x9
 ldc.i4 0xd411c8ed
 bge Branch_0x24a

 nop
 br Branch_0x24b

 Branch_0x24a:

 nop
 Branch_0x24b:

End_Orphan_ff:
Start_Orphan_100:
 nop
End_Orphan_100:
 ldc.i4.4
 ldarg Arg_0x9
 bne.un Branch_0x24c

 ldarg Arg_0x8
 br Branch_0x24d

 Branch_0x24c:

 ldarg Arg_0x8
 Branch_0x24d:

 conv.r8
 conv.ovf.u8
 mul.ovf.un
 br Branch_0x1d9

 Branch_0x1d8:

Start_Orphan_101:
 ldc.i8 0x595ee36fcd4fc1ae
 ldc.i8 0xbe03808a9fcacd7
 ble.un Branch_0x250

 nop
 br Branch_0x251

 Branch_0x250:

 nop
 Branch_0x251:

End_Orphan_101:
 ldc.r4 float32(0x9e972073)
 call float64 [mscorlib]System.Math::Abs(float64)
Start_Orphan_102:
 nop
End_Orphan_102:
 ldarga Arg_0x4
 ldind.r4
 blt Branch_0x252

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.6
 bge Branch_0x254

 ldc.i8 0x726463834efb2e39
 br Branch_0x255

 Branch_0x254:

 ldarg Arg_0x1
 Branch_0x255:

 br Branch_0x253

 Branch_0x252:

Start_Orphan_103:
 nop
End_Orphan_103:
 ldloc LOCAL_0x6
 conv.ovf.u8
 Branch_0x253:

 conv.ovf.i2.un
Start_Orphan_104:
 ldarg Arg_0x6
 ldarg Arg_0x4
 bge Branch_0x258

 ldc.i4 0xcef8a42d
 br Branch_0x259

 Branch_0x258:

 ldarg Arg_0x3
 Branch_0x259:

 brtrue Branch_0x256

 ldarga Arg_0x3
 ldc.i4.7
 stind.i
 br Branch_0x257

 Branch_0x256:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt Branch_0x25a

 nop
 br Branch_0x25b

 Branch_0x25a:

 nop
 Branch_0x25b:

 Branch_0x257:

End_Orphan_104:
Start_Orphan_105:
 nop
End_Orphan_105:
 ldarg Arg_0x4
 ldc.r8 float64(0x1452dca9e11c4455)
 bgt.un Branch_0x25e

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x25f

 Branch_0x25e:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x25f:

 ldc.i4 0x8d5e741e
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 add
 ble.un Branch_0x25c

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldloc LOCAL_0x9
 bgt.un Branch_0x260

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x261

 Branch_0x260:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x261:

 br Branch_0x25d

 Branch_0x25c:

Start_Orphan_106:
 nop
End_Orphan_106:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 div.un
 Branch_0x25d:

 ldarg Arg_0x3
 div
 bgt Branch_0x24e

Start_Orphan_107:
 nop
End_Orphan_107:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldloc LOCAL_0x2
 sub
 conv.ovf.i2
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 ldc.i8 0x9c46ddbd2545b2d6
 ldarg Arg_0x8
 bne.un Branch_0x266

 ldc.i4.0
 br Branch_0x267

 Branch_0x266:

 ldc.i4.3
 Branch_0x267:

 bge.un Branch_0x264

 ldloc LOCAL_0x8
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bne.un Branch_0x268

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x269

 Branch_0x268:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x269:

 br Branch_0x265

 Branch_0x264:

Start_Orphan_108:
 nop
End_Orphan_108:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brfalse Branch_0x26a

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x26b

 Branch_0x26a:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x26b:

 Branch_0x265:

 bgt.un Branch_0x262

Start_Orphan_109:
 ldloca LOCAL_0x9
 ldc.i4 0x12e439df
 stind.i4
End_Orphan_109:
 ldloc LOCAL_0x0
 ldc.r4 float32(0xb32f87da)
 bne.un Branch_0x26c

 ldloc LOCAL_0x4
 br Branch_0x26d

 Branch_0x26c:

 ldarg Arg_0x5
 Branch_0x26d:

 ldloc LOCAL_0x8
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble.un Branch_0x26e

 ldarg Arg_0x7
 br Branch_0x26f

 Branch_0x26e:

 ldarg Arg_0x2
 Branch_0x26f:

 add
 br Branch_0x263

 Branch_0x262:

Start_Orphan_10a:
 ldc.i4 9 
 Loop_0x0: //WHAT
 ldc.i4.6
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x0
 pop
 Loop_0x1: // End of WHAT
End_Orphan_10a:
Start_Orphan_10b:
 nop
End_Orphan_10b:
 ldc.r8 float64(0xe874ba9d4ce38a8d)
 ldarg Arg_0x4
 bgt.un Branch_0x272

 ldc.r8 float64(0xc2eb7df538cd71ad)
 br Branch_0x273

 Branch_0x272:

 ldarg Arg_0x4
 Branch_0x273:

 ldc.r8 float64(0x1274dca1a2133d1d)
 conv.r8
 beq Branch_0x270

Start_Orphan_10c:
 nop
End_Orphan_10c:
 ldc.i4.4
 ldc.i4.0
 blt Branch_0x274

 ldloc LOCAL_0x7
 br Branch_0x275

 Branch_0x274:

 ldloc LOCAL_0x4
 Branch_0x275:

 br Branch_0x271

 Branch_0x270:

Start_Orphan_10d:
 nop
End_Orphan_10d:
 ldarg Arg_0x9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 beq Branch_0x276

 ldarg Arg_0x8
 br Branch_0x277

 Branch_0x276:

 ldarg Arg_0x7
 Branch_0x277:

 Branch_0x271:

 Branch_0x263:

 br Branch_0x24f

 Branch_0x24e:

Start_Orphan_10e:
 nop
End_Orphan_10e:
 ldc.i4 0x5cf1b48d
 ldc.i4.7
 ble.un Branch_0x27a

 ldarg Arg_0x2
 br Branch_0x27b

 Branch_0x27a:

 ldarg Arg_0x8
 Branch_0x27b:

 ldloc LOCAL_0x9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x27c

 ldloc LOCAL_0x4
 br Branch_0x27d

 Branch_0x27c:

 ldloc LOCAL_0x4
 Branch_0x27d:

 clt.un
Start_Orphan_10f:
 ldarg Arg_0x6
 starg Arg_0x6
End_Orphan_10f:
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 ldarg Arg_0x0
 ble Branch_0x280

 ldloc LOCAL_0x0
 br Branch_0x281

 Branch_0x280:

 ldc.r4 float32(0x3dc61920)
 Branch_0x281:

 ldloc LOCAL_0x7
 ldloc LOCAL_0x6
 bgt.un Branch_0x282

 ldarg Arg_0x4
 br Branch_0x283

 Branch_0x282:

 ldc.r4 float32(0x3158a94e)
 Branch_0x283:

 bge.un Branch_0x27e

Start_Orphan_110:
 nop
End_Orphan_110:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i4.un
 br Branch_0x27f

 Branch_0x27e:

 ldc.i4.m1
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt.un Branch_0x284

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x285

 Branch_0x284:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x285:

 Branch_0x27f:

 bgt.un Branch_0x278

Start_Orphan_111:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
 ldloc LOCAL_0x2
 ldarg Arg_0x2
 stelem.i8
End_Orphan_111:
 ldloc LOCAL_0x0
 ldc.r4 float32(0x1d0a34ba)
 bge Branch_0x288

 ldarg Arg_0x4
 br Branch_0x289

 Branch_0x288:

 ldc.r4 float32(0x222739f1)
 Branch_0x289:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x28a

 ldloc LOCAL_0x0
 br Branch_0x28b

 Branch_0x28a:

 ldc.r4 float32(0x792e2977)
 Branch_0x28b:

 ble.un Branch_0x286

 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x28c

 ldloc LOCAL_0x4
 br Branch_0x28d

 Branch_0x28c:

 ldarg Arg_0x8
 Branch_0x28d:

 br Branch_0x287

 Branch_0x286:

Start_Orphan_112:
 nop
End_Orphan_112:
 ldarg Arg_0x6
 ldarg Arg_0x4
 ble Branch_0x28e

 ldarg Arg_0x1
 br Branch_0x28f

 Branch_0x28e:

 ldarg Arg_0x1
 Branch_0x28f:

 Branch_0x287:

 br Branch_0x279

 Branch_0x278:

Start_Orphan_113:
 nop
End_Orphan_113:
 ldarg Arg_0x2
 ldarg Arg_0x1
 bne.un Branch_0x292

 ldc.i4.s 18
 br Branch_0x293

 Branch_0x292:

 ldc.i4.m1
 Branch_0x293:

Start_Orphan_114:
 nop
End_Orphan_114:
 ldc.i4.m1
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0x294

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x295

 Branch_0x294:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x295:

 bgt Branch_0x290

 ldarg Arg_0x2
 ldarg Arg_0x5
 beq Branch_0x296

 ldc.i8 0x8cb7106cca312593
 br Branch_0x297

 Branch_0x296:

 ldc.i8 0x3a35950d1e52d4c8
 Branch_0x297:

 br Branch_0x291

 Branch_0x290:

 ldarg Arg_0x4
 ldarg Arg_0x6
 beq Branch_0x298

 ldarg Arg_0x5
 br Branch_0x299

 Branch_0x298:

 ldarg Arg_0x8
 Branch_0x299:

 Branch_0x291:

 Branch_0x279:

 Branch_0x24f:

 Branch_0x1d9:

 div
 br Branch_0x7f

 Branch_0x7e:

Start_Orphan_115:
Start_Orphan_116:
 ldarg Arg_0x0
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0x2a0

 ldc.i4.m1
 br Branch_0x2a1

 Branch_0x2a0:

 ldloc LOCAL_0x9
 Branch_0x2a1:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt.un Branch_0x2a2

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x2a3

 Branch_0x2a2:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x2a3:

 blt Branch_0x29e

 ldsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x3
 ldc.i4.1
 ldelema [mscorlib]System.SByte
 pop
 br Branch_0x29f

 Branch_0x29e:

 ldsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x3
 ldc.i4.0
 ldelema [mscorlib]System.SByte
 ldarg Arg_0x0
 stind.i1
 Branch_0x29f:

End_Orphan_116:
Start_Orphan_117:
 ldsflda native int ILGEN_0xd73e17d6::field_0x6
 pop
End_Orphan_117:
 ldc.i8 0xc62c8c575b38c896
 conv.u1
 conv.u1
 newarr [mscorlib]System.Double
 ldlen
Start_Orphan_118:
Start_Orphan_119:
 nop
End_Orphan_119:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.u4.un
 starg Arg_0x9
End_Orphan_118:
Start_Orphan_11a:
 ldc.i4.4
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 ble.un Branch_0x2a4

 nop
 br Branch_0x2a5

 Branch_0x2a4:

 nop
 Branch_0x2a5:

End_Orphan_11a:
Start_Orphan_11b:
 nop
End_Orphan_11b:
 ldc.i4.4
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 add
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.s -85
 ble Branch_0x2a6

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x2a7

 Branch_0x2a6:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x2a7:

 cgt.un
 ldloc LOCAL_0x6
 ldarg Arg_0x1
 bge.un Branch_0x2a8

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x2a9

 Branch_0x2a8:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x2a9:

 ldarg Arg_0x4
 ldarg Arg_0x6
 beq Branch_0x2aa

 ldloc LOCAL_0x9
 br Branch_0x2ab

 Branch_0x2aa:

 ldc.i4.s -42
 Branch_0x2ab:

 div
 cgt.un
 bge.un Branch_0x29c

Start_Orphan_11c:
 ldarga Arg_0x8
 ldloc LOCAL_0x6
 stind.i8
End_Orphan_11c:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldarg Arg_0x3
 rem
 conv.i
 pop
 br Branch_0x29d

 Branch_0x29c:

Start_Orphan_11d:
 ldsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x8
 ldc.i4.m1
 ldelema [mscorlib]System.SByte
 pop
End_Orphan_11d:
Start_Orphan_11e:
 nop
End_Orphan_11e:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldloc LOCAL_0x2
 blt Branch_0x2b0

 ldc.i8 0x5b97f2677a487d48
 br Branch_0x2b1

 Branch_0x2b0:

 ldc.i8 0xcca4e72d5fb2b68
 Branch_0x2b1:

Start_Orphan_11f:
 nop
End_Orphan_11f:
 ldc.r8 float64(0x648fca88a93b0e1d)
 ldc.r4 float32(0xccc1afbe)
 bne.un Branch_0x2b2

 ldloc LOCAL_0x6
 br Branch_0x2b3

 Branch_0x2b2:

 ldc.i8 0xbad809619356122b
 Branch_0x2b3:

 bge Branch_0x2ae

Start_Orphan_120:
 nop
End_Orphan_120:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldloc LOCAL_0x8
 bne.un Branch_0x2b4

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x2b5

 Branch_0x2b4:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x2b5:

 br Branch_0x2af

 Branch_0x2ae:

Start_Orphan_121:
 nop
End_Orphan_121:
 ldloc LOCAL_0x2
 conv.ovf.i2.un
 Branch_0x2af:

Start_Orphan_122:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 pop
End_Orphan_122:
Start_Orphan_123:
 nop
End_Orphan_123:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brtrue Branch_0x2b6

 ldarg Arg_0x2
 br Branch_0x2b7

 Branch_0x2b6:

 ldarg Arg_0x2
 Branch_0x2b7:

 conv.ovf.u4
 beq Branch_0x2ac

Start_Orphan_124:
 nop
End_Orphan_124:
 ldc.i4 0x9c346f18
 stloc LOCAL_0x9
 br Branch_0x2ad

 Branch_0x2ac:

Start_Orphan_125:
 nop
End_Orphan_125:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 mul
Start_Orphan_126:
 nop
End_Orphan_126:
 ldloc LOCAL_0x7
 conv.ovf.i1.un
 ble Branch_0x2b8

 ldc.i4.2
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt Branch_0x2ba

 nop
 br Branch_0x2bb

 Branch_0x2ba:

 nop
 Branch_0x2bb:

 br Branch_0x2b9

 Branch_0x2b8:

 ldc.i4.m1
 ldc.i4.3
 blt.un Branch_0x2bc

 nop
 br Branch_0x2bd

 Branch_0x2bc:

 nop
 Branch_0x2bd:

 Branch_0x2b9:

 Branch_0x2ad:

 Branch_0x29d:

End_Orphan_115:
Start_Orphan_127:
Start_Orphan_128:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt.un Branch_0x2c2

 nop
 br Branch_0x2c3

 Branch_0x2c2:

 nop
 Branch_0x2c3:

End_Orphan_128:
 ldc.i4.7
 ldarg Arg_0x8
 ldloc LOCAL_0x7
 div
 ldc.i8 0xfca4a269d2ec5c31
 ldc.i4.m1
 shr.un
 bgt.un Branch_0x2c4

 ldloc LOCAL_0x2
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 add.ovf.un
 br Branch_0x2c5

 Branch_0x2c4:

Start_Orphan_129:
 nop
End_Orphan_129:
 ldarg Arg_0x6
 ldc.r4 float32(0xfbf689ea)
 bge Branch_0x2c6

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x2c7

 Branch_0x2c6:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x2c7:

 Branch_0x2c5:

 ble Branch_0x2c0

 ldc.i4.0
 ldc.i4.6
 blt Branch_0x2c8

 ldc.r4 float32(0xef86f0c6)
 br Branch_0x2c9

 Branch_0x2c8:

 ldc.r4 float32(0xf1749c92)
 Branch_0x2c9:

 starg Arg_0x4
 br Branch_0x2c1

 Branch_0x2c0:

Start_Orphan_12a:
 nop
End_Orphan_12a:
 ldc.i4.4
 ldc.i4.0
 shr.un
 ldloc LOCAL_0x0
 ldloc LOCAL_0x0
 bne.un Branch_0x2cc

 ldc.i4.0
 br Branch_0x2cd

 Branch_0x2cc:

 ldarg Arg_0x9
 Branch_0x2cd:

 beq Branch_0x2ca

 ldarg Arg_0x5
 ldloc LOCAL_0x4
 ble Branch_0x2ce

 nop
 br Branch_0x2cf

 Branch_0x2ce:

 nop
 Branch_0x2cf:

 br Branch_0x2cb

 Branch_0x2ca:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brfalse Branch_0x2d0

 nop
 br Branch_0x2d1

 Branch_0x2d0:

 nop
 Branch_0x2d1:

 Branch_0x2cb:

 Branch_0x2c1:

End_Orphan_127:
Start_Orphan_12b:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldarg Arg_0x9
 bgt.un Branch_0x2d6

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x2d7

 Branch_0x2d6:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x2d7:

Start_Orphan_12c:
 nop
End_Orphan_12c:
 ldsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x8
 ldlen
 bgt.un Branch_0x2d4

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 pop
 br Branch_0x2d5

 Branch_0x2d4:

 ldc.i8 0x22e8f6e74886d38b
 ldloc LOCAL_0x7
 bge Branch_0x2d8

 nop
 br Branch_0x2d9

 Branch_0x2d8:

 nop
 Branch_0x2d9:

 Branch_0x2d5:

End_Orphan_12b:
 ldc.i4.8
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.u2.un
 blt.un Branch_0x2dc

Start_Orphan_12d:
 nop
End_Orphan_12d:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i8
 br Branch_0x2dd

 Branch_0x2dc:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.3
 bge Branch_0x2de

 ldarg Arg_0x7
 br Branch_0x2df

 Branch_0x2de:

 ldloc LOCAL_0x6
 Branch_0x2df:

 Branch_0x2dd:

Start_Orphan_12e:
 nop
End_Orphan_12e:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldloc LOCAL_0x9
 bge.un Branch_0x2e0

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x2e1

 Branch_0x2e0:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x2e1:

 conv.u8
 blt.un Branch_0x2da

 ldloc LOCAL_0x5
 ldlen
 br Branch_0x2db

 Branch_0x2da:

Start_Orphan_12f:
 ldarg Arg_0x8
 ldc.i8 0x7f18a866e3de0911
 ble Branch_0x2e2

 nop
 br Branch_0x2e3

 Branch_0x2e2:

 nop
 Branch_0x2e3:

End_Orphan_12f:
 ldloc LOCAL_0x9
 Branch_0x2db:

Start_Orphan_130:
 ldloc LOCAL_0x3
 ldloc LOCAL_0x8
 ldelema [mscorlib]System.Int32
 ldc.r4 float32(0xdcc3d92d)
 conv.i4
 stind.i
End_Orphan_130:
Start_Orphan_131:
 ldc.i4.6
 ldc.i4.6
 ble Branch_0x2e6

 nop
 br Branch_0x2e7

 Branch_0x2e6:

 nop
 Branch_0x2e7:

End_Orphan_131:
 ldarg Arg_0x4
 ldarg Arg_0x6
 blt.un Branch_0x2e8

 ldc.i8 0x8c2ce46769ff0ecc
 br Branch_0x2e9

 Branch_0x2e8:

 ldloc LOCAL_0x7
 Branch_0x2e9:

 conv.u2
Start_Orphan_132:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0x2ea

 nop
 br Branch_0x2eb

 Branch_0x2ea:

 nop
 Branch_0x2eb:

End_Orphan_132:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i1
Start_Orphan_133:
 nop
End_Orphan_133:
 ldc.r8 float64(0x6aed3114e768818a)
 ldarg Arg_0x6
 bge.un Branch_0x2ee

 ldc.i4.7
 br Branch_0x2ef

 Branch_0x2ee:

 ldloc LOCAL_0x8
 Branch_0x2ef:

 blt.un Branch_0x2ec

Start_Orphan_134:
 nop
End_Orphan_134:
 ldsflda native int ILGEN_0xd73e17d6::field_0x6
 ldind.i
 br Branch_0x2ed

 Branch_0x2ec:

Start_Orphan_135:
 nop
End_Orphan_135:
 ldc.i8 0xd14dbe607bfcbd66
 conv.u
 Branch_0x2ed:

 bge.un Branch_0x2e4

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.6
 blt Branch_0x2f0

 ldc.i4 0x949a057c
 br Branch_0x2f1

 Branch_0x2f0:

 ldc.i4.8
 Branch_0x2f1:

Start_Orphan_136:
 nop
End_Orphan_136:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.m1
 ble.un Branch_0x2f2

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x2f3

 Branch_0x2f2:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x2f3:

 rem.un
 br Branch_0x2e5

 Branch_0x2e4:

Start_Orphan_137:
 ldarga Arg_0x9
 ldc.i4.s 81
 stind.i4
End_Orphan_137:
 ldc.i4.4
 ldc.i4.8
 and
 ldc.r8 float64(0x3a9757c174eccb44)
 conv.i
 ble Branch_0x2f4

Start_Orphan_138:
 nop
End_Orphan_138:
 ldc.i4 0xe96b30d2
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble.un Branch_0x2f6

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x2f7

 Branch_0x2f6:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x2f7:

 br Branch_0x2f5

 Branch_0x2f4:

Start_Orphan_139:
 nop
End_Orphan_139:
 ldloc LOCAL_0x8
 ldc.i4 0x45ec754d
 beq Branch_0x2f8

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x2f9

 Branch_0x2f8:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x2f9:

 Branch_0x2f5:

 Branch_0x2e5:

 bge.un Branch_0x2d2

Start_Orphan_13a:
Start_Orphan_13b:
 nop
End_Orphan_13b:
 ldc.i4 0x3b7af9a5
 ldloc LOCAL_0x2
 bge.un Branch_0x2fc

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x2fd

 Branch_0x2fc:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x2fd:

Start_Orphan_13c:
 nop
End_Orphan_13c:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt.un Branch_0x2fe

 ldc.i4.8
 br Branch_0x2ff

 Branch_0x2fe:

 ldarg Arg_0x0
 Branch_0x2ff:

 bgt.un Branch_0x2fa

 ldarg Arg_0x8
 ldarg Arg_0x7
 bge.un Branch_0x300

 nop
 br Branch_0x301

 Branch_0x300:

 nop
 Branch_0x301:

 br Branch_0x2fb

 Branch_0x2fa:

 ldloc LOCAL_0x8
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt Branch_0x302

 nop
 br Branch_0x303

 Branch_0x302:

 nop
 Branch_0x303:

 Branch_0x2fb:

End_Orphan_13a:
 ldc.r8 float64(0x8e85380f9dd4271)
 br Branch_0x2d3

 Branch_0x2d2:

Start_Orphan_13d:
 ldarga Arg_0x3
 ldc.i4.6
 stind.i4
End_Orphan_13d:
 ldc.r4 float32(0x62c548b8)
 conv.ovf.i4
 brfalse Branch_0x304

 ldarg Arg_0x9
 conv.ovf.u4
 br Branch_0x305

 Branch_0x304:

Start_Orphan_13e:
 nop
End_Orphan_13e:
 ldarg Arg_0x8
 ldarg Arg_0x8
 bne.un Branch_0x306

 ldarg Arg_0x3
 br Branch_0x307

 Branch_0x306:

 ldc.i4.4
 Branch_0x307:

 Branch_0x305:

 conv.r8
 Branch_0x2d3:

Start_Orphan_13f:
Start_Orphan_140:
 ldloc LOCAL_0x3
 pop
End_Orphan_140:
Start_Orphan_141:
 nop
End_Orphan_141:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 and
Start_Orphan_142:
 nop
End_Orphan_142:
 ldloc LOCAL_0x3
 ldlen
 cgt.un
 stloc LOCAL_0x2
End_Orphan_13f:
Start_Orphan_143:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldarg Arg_0x0
 bge Branch_0x30c

 ldloc LOCAL_0x4
 br Branch_0x30d

 Branch_0x30c:

 ldc.i8 0x90fed7663a7cd62d
 Branch_0x30d:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0x30e

 ldarg Arg_0x2
 br Branch_0x30f

 Branch_0x30e:

 ldc.i8 0x1b34b1d4ebe73353
 Branch_0x30f:

 beq Branch_0x30a

 ldloc LOCAL_0x7
 ldarg Arg_0x8
 ble.un Branch_0x310

 nop
 br Branch_0x311

 Branch_0x310:

 nop
 Branch_0x311:

 br Branch_0x30b

 Branch_0x30a:

 ldc.i8 0xed48fc309117c4ad
 stloc LOCAL_0x6
 Branch_0x30b:

End_Orphan_143:
Start_Orphan_144:
 ldarg Arg_0x2
 ldarg Arg_0x2
 bgt.un Branch_0x312

 nop
 br Branch_0x313

 Branch_0x312:

 nop
 Branch_0x313:

End_Orphan_144:
 ldsflda unsigned int8 ILGEN_0xd73e17d6::field_0x5
 ldind.u1
 ldc.i4.m1
 ldarg Arg_0x3
 ldarg Arg_0x0
 xor
 bge.un Branch_0x314

 ldarg Arg_0x4
 ldc.r4 float32(0x19474b1f)
 ble Branch_0x316

 ldarg Arg_0x6
 br Branch_0x317

 Branch_0x316:

 ldloc LOCAL_0x0
 Branch_0x317:

 br Branch_0x315

 Branch_0x314:

Start_Orphan_145:
 nop
End_Orphan_145:
 ldloc LOCAL_0x2
 ldc.i4 0x1ad6dae7
 bne.un Branch_0x318

 ldarg Arg_0x6
 br Branch_0x319

 Branch_0x318:

 ldarg Arg_0x6
 Branch_0x319:

 Branch_0x315:

 conv.ovf.u4
 blt Branch_0x308

Start_Orphan_146:
Start_Orphan_147:
 nop
End_Orphan_147:
 ldc.i4.1
 conv.u1
 newarr [mscorlib]System.IntPtr
 pop
End_Orphan_146:
Start_Orphan_148:
 ldarg Arg_0x6
 starg Arg_0x4
End_Orphan_148:
Start_Orphan_149:
 nop
End_Orphan_149:
 ldc.r8 float64(0x204236b8339be13e)
 ldc.r8 float64(0xc4cc32a960d8d086)
 ceq
 conv.ovf.u1.un
 ldc.i4.7
 conv.ovf.i4
Start_Orphan_14a:
 nop
End_Orphan_14a:
 ldloc LOCAL_0x9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x31e

 ldc.i4.4
 br Branch_0x31f

 Branch_0x31e:

 ldc.i4.5
 Branch_0x31f:

 ble Branch_0x31c

Start_Orphan_14b:
 nop
End_Orphan_14b:
 ldarg Arg_0x9
 ldc.i4.2
 cgt.un
 br Branch_0x31d

 Branch_0x31c:

Start_Orphan_14c:
 nop
End_Orphan_14c:
 ldsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x8
 ldc.i4.7
 ldelem.i1
 Branch_0x31d:

 bgt Branch_0x31a

Start_Orphan_14d:
 ldarga Arg_0x8
 ldc.i8 0x7e5093f67c98225e
 stind.i8
End_Orphan_14d:
Start_Orphan_14e:
 nop
End_Orphan_14e:
 ldarg Arg_0x4
 ldc.r4 float32(0x87c27442)
 bge Branch_0x322

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x323

 Branch_0x322:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x323:

Start_Orphan_14f:
 nop
End_Orphan_14f:
 ldloc LOCAL_0x0
 conv.ovf.i
 bne.un Branch_0x320

Start_Orphan_150:
 nop
End_Orphan_150:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldloc LOCAL_0x2
 bne.un Branch_0x324

 ldarg Arg_0x6
 br Branch_0x325

 Branch_0x324:

 ldc.r8 float64(0xacdd57f45a7b4a14)
 Branch_0x325:

 br Branch_0x321

 Branch_0x320:

 ldc.i4.4
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt Branch_0x326

 ldarg Arg_0x6
 br Branch_0x327

 Branch_0x326:

 ldc.r4 float32(0x9516b5ec)
 Branch_0x327:

 Branch_0x321:

 br Branch_0x31b

 Branch_0x31a:

Start_Orphan_151:
 ldarg Arg_0x9
 stloc LOCAL_0x8
End_Orphan_151:
Start_Orphan_152:
 nop
End_Orphan_152:
 ldarg Arg_0x3
 conv.r8
 call float64 [mscorlib]System.Math::Cos(float64)
 Branch_0x31b:

 br Branch_0x309

 Branch_0x308:

Start_Orphan_153:
 nop
End_Orphan_153:
 ldc.r8 float64(0x2664460c2a78e47e)
 ldloc LOCAL_0x0
 ble.un Branch_0x32a

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x32b

 Branch_0x32a:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x32b:

Start_Orphan_154:
 nop
End_Orphan_154:
 ldc.i4 0xc2ced8c8
 conv.i4
 ble.un Branch_0x328

 ldc.i4.0
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble.un Branch_0x32c

 ldc.r4 float32(0x1162218e)
 br Branch_0x32d

 Branch_0x32c:

 ldloc LOCAL_0x0
 Branch_0x32d:

 br Branch_0x329

 Branch_0x328:

Start_Orphan_155:
 nop
End_Orphan_155:
 ldc.i4.m1
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x32e

 ldc.r4 float32(0xfc2c17d7)
 br Branch_0x32f

 Branch_0x32e:

 ldarg Arg_0x4
 Branch_0x32f:

 Branch_0x329:

 call float64 [mscorlib]System.Math::Sqrt(float64)
 Branch_0x309:

 ble Branch_0x2be

Start_Orphan_156:
 ldloc LOCAL_0x3
Start_Orphan_157:
 nop
End_Orphan_157:
 ldarg Arg_0x2
 conv.ovf.u1.un
 ldelema [mscorlib]System.Int32
Start_Orphan_158:
 nop
End_Orphan_158:
 ldloc LOCAL_0x2
 ldc.i4.7
 sub.ovf.un
 conv.ovf.i4
 stind.i
End_Orphan_156:
Start_Orphan_159:
 nop
End_Orphan_159:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.u
 conv.i1
Start_Orphan_15a:
 nop
End_Orphan_15a:
 ldarg Arg_0x8
 ldarg Arg_0x5
 blt.un Branch_0x330

 ldc.i4.s -106
 br Branch_0x331

 Branch_0x330:

 ldc.i4.5
 Branch_0x331:

 conv.u
 clt
Start_Orphan_15b:
 ldloc LOCAL_0x4
 conv.r.un
 starg Arg_0x4
End_Orphan_15b:
Start_Orphan_15c:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt.un Branch_0x334

 nop
 br Branch_0x335

 Branch_0x334:

 nop
 Branch_0x335:

End_Orphan_15c:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.8
 bge Branch_0x338

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x339

 Branch_0x338:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x339:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 cgt.un
 ble.un Branch_0x336

Start_Orphan_15d:
 nop
End_Orphan_15d:
 ldc.i4.8
 ldloc LOCAL_0x2
 sub.ovf.un
 br Branch_0x337

 Branch_0x336:

 ldarg Arg_0x6
 conv.ovf.i1
 Branch_0x337:

Start_Orphan_15e:
 ldc.i4.m1
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt Branch_0x33a

 nop
 br Branch_0x33b

 Branch_0x33a:

 nop
 Branch_0x33b:

End_Orphan_15e:
Start_Orphan_15f:
 nop
End_Orphan_15f:
 ldarg Arg_0x8
 ldarg Arg_0x1
 bgt Branch_0x33e

 ldloc LOCAL_0x0
 br Branch_0x33f

 Branch_0x33e:

 ldc.r4 float32(0x340997f8)
 Branch_0x33f:

 ldarg Arg_0x8
 ldarg Arg_0x8
 bgt.un Branch_0x340

 ldc.r4 float32(0x8be6157f)
 br Branch_0x341

 Branch_0x340:

 ldloc LOCAL_0x0
 Branch_0x341:

 bgt.un Branch_0x33c

Start_Orphan_160:
 nop
End_Orphan_160:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.u.un
 br Branch_0x33d

 Branch_0x33c:

Start_Orphan_161:
 nop
End_Orphan_161:
 ldloc LOCAL_0x0
 ldarg Arg_0x4
 ble Branch_0x342

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x343

 Branch_0x342:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x343:

 Branch_0x33d:

 beq Branch_0x332

Start_Orphan_162:
 ldc.r8 float64(0xc678ede913d0efe5)
 ldarg Arg_0x6
 bgt Branch_0x344

 nop
 br Branch_0x345

 Branch_0x344:

 nop
 Branch_0x345:

End_Orphan_162:
 ldarg Arg_0x8
 conv.u4
Start_Orphan_163:
 nop
End_Orphan_163:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0x348

 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 br Branch_0x349

 Branch_0x348:

 ldc.i4 0xa1cf1f12
 Branch_0x349:

 ble Branch_0x346

Start_Orphan_164:
 nop
End_Orphan_164:
 ldsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x8
 ldc.i4.1
 ldelema [mscorlib]System.SByte
 ldind.i1
 br Branch_0x347

 Branch_0x346:

Start_Orphan_165:
 nop
End_Orphan_165:
 ldc.i4.s -62
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bne.un Branch_0x34a

 ldloc LOCAL_0x9
 br Branch_0x34b

 Branch_0x34a:

 ldc.i4.s -94
 Branch_0x34b:

 Branch_0x347:

 br Branch_0x333

 Branch_0x332:

Start_Orphan_166:
 ldc.i4.s 111
 starg Arg_0x9
End_Orphan_166:
 ldc.i4.0
 Branch_0x333:

 cgt
 br Branch_0x2bf

 Branch_0x2be:

Start_Orphan_167:
Start_Orphan_168:
 ldsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x8
 ldc.i4.8
 ldelema [mscorlib]System.SByte
 pop
End_Orphan_168:
Start_Orphan_169:
 nop
End_Orphan_169:
 ldloc LOCAL_0x0
Start_Orphan_16a:
 nop
End_Orphan_16a:
 ldarg Arg_0x9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0x350

 ldc.r4 float32(0xa4dc0f7e)
 br Branch_0x351

 Branch_0x350:

 ldloc LOCAL_0x0
 Branch_0x351:

 beq Branch_0x34e

 ldc.i4.4
 ldc.i4.5
 bge.un Branch_0x352

 ldarg Arg_0x0
 br Branch_0x353

 Branch_0x352:

 ldc.i4.m1
 Branch_0x353:

 br Branch_0x34f

 Branch_0x34e:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt.un Branch_0x354

 ldc.i4.3
 br Branch_0x355

 Branch_0x354:

 ldc.i4.1
 Branch_0x355:

 Branch_0x34f:

Start_Orphan_16b:
 ldarg Arg_0x9
 ldc.i4.7
 ble Branch_0x356

 nop
 br Branch_0x357

 Branch_0x356:

 nop
 Branch_0x357:

End_Orphan_16b:
 ldc.r8 float64(0x46390004d927776f)
 call float32 [mscorlib]System.Math::Abs(float32)
 ldc.i8 0xa6e8f8f9a429a533
 ldarg Arg_0x8
 bne.un Branch_0x35a

 ldc.r8 float64(0x3a2cf7d7e7d1a73a)
 br Branch_0x35b

 Branch_0x35a:

 ldarg Arg_0x4
 Branch_0x35b:

 blt Branch_0x358

 ldc.i4.1
 conv.ovf.i1.un
 br Branch_0x359

 Branch_0x358:

Start_Orphan_16c:
 nop
End_Orphan_16c:
 ldc.i4.2
 conv.ovf.i1.un
 Branch_0x359:

 beq Branch_0x34c

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.i
Start_Orphan_16d:
 nop
End_Orphan_16d:
 ldarg Arg_0x1
 ldarg Arg_0x8
 bge Branch_0x35e

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x35f

 Branch_0x35e:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x35f:

 blt.un Branch_0x35c

 nop
 br Branch_0x35d

 Branch_0x35c:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.5
 beq Branch_0x360

 nop
 br Branch_0x361

 Branch_0x360:

 nop
 Branch_0x361:

 Branch_0x35d:

 br Branch_0x34d

 Branch_0x34c:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt Branch_0x364

 ldc.i4.0
 br Branch_0x365

 Branch_0x364:

 ldc.i4.4
 Branch_0x365:

 ldc.i4.3
 ldarg Arg_0x3
 shr.un
 blt Branch_0x362

 ldc.i4.4
 ldarg Arg_0x9
 ble.un Branch_0x366

 nop
 br Branch_0x367

 Branch_0x366:

 nop
 Branch_0x367:

 br Branch_0x363

 Branch_0x362:

 ldarg Arg_0x0
 ldc.i4.7
 ble Branch_0x368

 nop
 br Branch_0x369

 Branch_0x368:

 nop
 Branch_0x369:

 Branch_0x363:

 Branch_0x34d:

End_Orphan_167:
Start_Orphan_16e:
Start_Orphan_16f:
 nop
End_Orphan_16f:
 ldloc LOCAL_0x0
 ldarg Arg_0x4
 bgt Branch_0x36c

 ldarg Arg_0x8
 br Branch_0x36d

 Branch_0x36c:

 ldc.i8 0xf1db9acb6ba74055
 Branch_0x36d:

 starg Arg_0x8
End_Orphan_16e:
Start_Orphan_170:
 ldloca LOCAL_0x2
 pop
End_Orphan_170:
 ldc.r8 float64(0xed9d35058b5758a6)
 ldarg Arg_0x4
 ble.un Branch_0x372

 ldarg Arg_0x2
 br Branch_0x373

 Branch_0x372:

 ldarg Arg_0x8
 Branch_0x373:

Start_Orphan_171:
 nop
End_Orphan_171:
 ldarg Arg_0x5
 ldarg Arg_0x7
 mul.ovf
 beq Branch_0x370

Start_Orphan_172:
 nop
End_Orphan_172:
 ldarg Arg_0x7
 ldarg Arg_0x8
 bge Branch_0x374

 ldarg Arg_0x8
 br Branch_0x375

 Branch_0x374:

 ldc.i8 0x4ebeec458f9d93e3
 Branch_0x375:

 br Branch_0x371

 Branch_0x370:

Start_Orphan_173:
 nop
End_Orphan_173:
 ldarg Arg_0x8
 ldarg Arg_0x8
 bge Branch_0x376

 ldc.i8 0xf31dad36f9da2662
 br Branch_0x377

 Branch_0x376:

 ldloc LOCAL_0x7
 Branch_0x377:

 Branch_0x371:

Start_Orphan_174:
 ldarga Arg_0x6
 ldarg Arg_0x6
 stind.r8
End_Orphan_174:
Start_Orphan_175:
 nop
End_Orphan_175:
 ldloc LOCAL_0x8
 ldc.i4.3
 bgt Branch_0x37a

 ldc.i4.0
 br Branch_0x37b

 Branch_0x37a:

 ldc.i4.8
 Branch_0x37b:

 brtrue Branch_0x378

Start_Orphan_176:
 nop
End_Orphan_176:
 ldloc LOCAL_0x7
 ldc.i8 0x24fc5ee7b244384f
 rem.un
 br Branch_0x379

 Branch_0x378:

Start_Orphan_177:
 nop
End_Orphan_177:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 beq Branch_0x37c

 ldarg Arg_0x8
 br Branch_0x37d

 Branch_0x37c:

 ldloc LOCAL_0x6
 Branch_0x37d:

 Branch_0x379:

 bne.un Branch_0x36e

Start_Orphan_178:
 ldloc LOCAL_0x1
 stloc LOCAL_0x1
End_Orphan_178:
 ldloc LOCAL_0x2
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 mul.ovf.un
 ldloc LOCAL_0x2
 neg
 ble.un Branch_0x37e

 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x2
 ldc.i4.4
 ldelem.u8
 br Branch_0x37f

 Branch_0x37e:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brtrue Branch_0x380

 ldloc LOCAL_0x6
 br Branch_0x381

 Branch_0x380:

 ldarg Arg_0x5
 Branch_0x381:

 Branch_0x37f:

 br Branch_0x36f

 Branch_0x36e:

Start_Orphan_179:
 ldloca LOCAL_0x2
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 stind.i4
End_Orphan_179:
 ldloc LOCAL_0x0
 conv.i4
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i4.un
 beq Branch_0x382

Start_Orphan_17a:
 nop
End_Orphan_17a:
 ldarg Arg_0x1
 ldarg Arg_0x8
 blt.un Branch_0x384

 ldloc LOCAL_0x6
 br Branch_0x385

 Branch_0x384:

 ldarg Arg_0x8
 Branch_0x385:

 br Branch_0x383

 Branch_0x382:

Start_Orphan_17b:
 nop
End_Orphan_17b:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i8
 Branch_0x383:

 Branch_0x36f:

Start_Orphan_17c:
Start_Orphan_17d:
 nop
End_Orphan_17d:
 ldarg Arg_0x7
 ldarg Arg_0x8
 bgt.un Branch_0x388

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x389

 Branch_0x388:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x389:

 ldc.r4 float32(0xc41dbba4)
 conv.ovf.i
 blt.un Branch_0x386

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt.un Branch_0x38a

 nop
 br Branch_0x38b

 Branch_0x38a:

 nop
 Branch_0x38b:

 br Branch_0x387

 Branch_0x386:

 ldloc LOCAL_0x1
 stloc LOCAL_0x1
 Branch_0x387:

End_Orphan_17c:
Start_Orphan_17e:
 ldsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x3
 pop
End_Orphan_17e:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldarg Arg_0x0
 rem
Start_Orphan_17f:
 nop
End_Orphan_17f:
 ldarg Arg_0x4
 conv.u1
 sub.ovf
Start_Orphan_180:
 ldarga Arg_0x9
 ldc.i4.5
 stind.i4
End_Orphan_180:
 ldloc LOCAL_0x4
 conv.u1
Start_Orphan_181:
 nop
End_Orphan_181:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
 ldlen
 bge.un Branch_0x38e

 ldc.i4.3
 brtrue Branch_0x390

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x391

 Branch_0x390:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x391:

 br Branch_0x38f

 Branch_0x38e:

 ldc.i4.m1
 ldc.i4.6
 blt.un Branch_0x392

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x393

 Branch_0x392:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x393:

 Branch_0x38f:

 bge.un Branch_0x38c

 ldloc LOCAL_0x9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt.un Branch_0x396

 ldc.i4.2
 br Branch_0x397

 Branch_0x396:

 ldc.i4.5
 Branch_0x397:

 ldarg Arg_0x8
 ldarg Arg_0x2
 bge Branch_0x398

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x399

 Branch_0x398:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x399:

 blt.un Branch_0x394

Start_Orphan_182:
 nop
End_Orphan_182:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 brtrue Branch_0x39a

 ldarg Arg_0x7
 br Branch_0x39b

 Branch_0x39a:

 ldloc LOCAL_0x4
 Branch_0x39b:

 br Branch_0x395

 Branch_0x394:

 ldloc LOCAL_0x7
 ldloc LOCAL_0x4
 div.un
 Branch_0x395:

 br Branch_0x38d

 Branch_0x38c:

Start_Orphan_183:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4 0xe3bbda62
 ble.un Branch_0x39c

 nop
 br Branch_0x39d

 Branch_0x39c:

 nop
 Branch_0x39d:

End_Orphan_183:
Start_Orphan_184:
 nop
End_Orphan_184:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.0
 mul.ovf
 brtrue Branch_0x39e

 ldloc LOCAL_0x7
 conv.ovf.i8.un
 br Branch_0x39f

 Branch_0x39e:

 ldc.i4.m1
 conv.ovf.u8
 Branch_0x39f:

 Branch_0x38d:

 beq Branch_0x36a

Start_Orphan_185:
Start_Orphan_186:
 nop
End_Orphan_186:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 blt.un Branch_0x3a2

 ldarg Arg_0x1
 br Branch_0x3a3

 Branch_0x3a2:

 ldloc LOCAL_0x7
 Branch_0x3a3:

Start_Orphan_187:
 nop
End_Orphan_187:
 ldloc LOCAL_0x8
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 beq Branch_0x3a4

 ldarg Arg_0x7
 br Branch_0x3a5

 Branch_0x3a4:

 ldc.i8 0xd37d720e68cfc726
 Branch_0x3a5:

 blt Branch_0x3a0

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.5
 bge.un Branch_0x3a6

 nop
 br Branch_0x3a7

 Branch_0x3a6:

 nop
 Branch_0x3a7:

 br Branch_0x3a1

 Branch_0x3a0:

 ldc.i4.2
 stsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 Branch_0x3a1:

End_Orphan_185:
Start_Orphan_188:
 ldarg Arg_0x6
 ldloc LOCAL_0x0
 blt.un Branch_0x3a8

 nop
 br Branch_0x3a9

 Branch_0x3a8:

 nop
 Branch_0x3a9:

End_Orphan_188:
Start_Orphan_189:
 nop
End_Orphan_189:
 ldloc LOCAL_0x8
 ldloc LOCAL_0x2
 div.un
 conv.ovf.i.un
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i2.un
 conv.ovf.u2
 cgt.un
 br Branch_0x36b

 Branch_0x36a:

Start_Orphan_18a:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt.un Branch_0x3ac

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x3ad

 Branch_0x3ac:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x3ad:

Start_Orphan_18b:
 nop
End_Orphan_18b:
 ldc.i4.7
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 mul
 bgt.un Branch_0x3aa

 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0x3ae

 nop
 br Branch_0x3af

 Branch_0x3ae:

 nop
 Branch_0x3af:

 br Branch_0x3ab

 Branch_0x3aa:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.s -75
 bge.un Branch_0x3b0

 nop
 br Branch_0x3b1

 Branch_0x3b0:

 nop
 Branch_0x3b1:

 Branch_0x3ab:

End_Orphan_18a:
 ldarg Arg_0x4
 ldloc LOCAL_0x0
 beq Branch_0x3b4

 ldloc LOCAL_0x9
 br Branch_0x3b5

 Branch_0x3b4:

 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 Branch_0x3b5:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.u2.un
 ble Branch_0x3b2

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.4
 blt.un Branch_0x3b6

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x3b7

 Branch_0x3b6:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x3b7:

 br Branch_0x3b3

 Branch_0x3b2:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldarg Arg_0x3
 mul.ovf.un
 Branch_0x3b3:

Start_Orphan_18c:
 ldarg Arg_0x2
 ldloc LOCAL_0x4
 beq Branch_0x3b8

 nop
 br Branch_0x3b9

 Branch_0x3b8:

 nop
 Branch_0x3b9:

End_Orphan_18c:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 rem.un
 ldc.i8 0x556af4058bc2c1dd
 ldarg Arg_0x1
 clt
 add.ovf.un
 clt.un
 Branch_0x36b:

 Branch_0x2bf:

Start_Orphan_18d:
Start_Orphan_18e:
 ldc.i4.0
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 add
Start_Orphan_18f:
 nop
End_Orphan_18f:
 ldarg Arg_0x9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 add.ovf.un
 bge.un Branch_0x3ba

 ldloc LOCAL_0x5
 ldarg Arg_0x3
 ldarg Arg_0x4
 stelem.r8
 br Branch_0x3bb

 Branch_0x3ba:

 ldloc LOCAL_0x7
 ldarg Arg_0x7
 bgt Branch_0x3bc

 nop
 br Branch_0x3bd

 Branch_0x3bc:

 nop
 Branch_0x3bd:

 Branch_0x3bb:

End_Orphan_18e:
Start_Orphan_190:
 ldarg Arg_0x7
 ldloc LOCAL_0x4
 blt.un Branch_0x3be

 nop
 br Branch_0x3bf

 Branch_0x3be:

 nop
 Branch_0x3bf:

End_Orphan_190:
Start_Orphan_191:
 nop
End_Orphan_191:
 ldc.i4.2
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt.un Branch_0x3c0

 ldarg Arg_0x4
 br Branch_0x3c1

 Branch_0x3c0:

 ldc.r8 float64(0x97e517fb7334caaa)
 Branch_0x3c1:

 ldc.i4.5
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bgt Branch_0x3c2

 ldarg Arg_0x6
 br Branch_0x3c3

 Branch_0x3c2:

 ldarg Arg_0x6
 Branch_0x3c3:

 rem 
Start_Orphan_192:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ble.un Branch_0x3c4

 nop
 br Branch_0x3c5

 Branch_0x3c4:

 nop
 Branch_0x3c5:

End_Orphan_192:
Start_Orphan_193:
 nop
End_Orphan_193:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldarg Arg_0x0
 ble.un Branch_0x3c8

 ldloc LOCAL_0x0
 br Branch_0x3c9

 Branch_0x3c8:

 ldloc LOCAL_0x0
 Branch_0x3c9:

 ldc.i4.8
 ldloc LOCAL_0x8
 blt.un Branch_0x3ca

 ldloc LOCAL_0x0
 br Branch_0x3cb

 Branch_0x3ca:

 ldloc LOCAL_0x0
 Branch_0x3cb:

 bgt.un Branch_0x3c6

 ldarg Arg_0x4
 ldc.r4 float32(0x3a39eab7)
 bge.un Branch_0x3cc

 ldarg Arg_0x4
 br Branch_0x3cd

 Branch_0x3cc:

 ldarg Arg_0x4
 Branch_0x3cd:

 br Branch_0x3c7

 Branch_0x3c6:

 ldc.r4 float32(0xbf3770c)
 ldarg Arg_0x4
 add 
 Branch_0x3c7:

 clt.un
 stsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
End_Orphan_18d:
Start_Orphan_194:
 ldloc LOCAL_0x1
 ldc.i4.5
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 stelem.i
End_Orphan_194:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge.un Branch_0x3d2

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 br Branch_0x3d3

 Branch_0x3d2:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 Branch_0x3d3:

Start_Orphan_195:
 nop
End_Orphan_195:
 ldc.i4.2
 conv.ovf.i2
 blt Branch_0x3d0

Start_Orphan_196:
 nop
End_Orphan_196:
 ldloc LOCAL_0x8
 ldarg Arg_0x9
 bgt Branch_0x3d4

 ldarg Arg_0x5
 br Branch_0x3d5

 Branch_0x3d4:

 ldarg Arg_0x7
 Branch_0x3d5:

 br Branch_0x3d1

 Branch_0x3d0:

 ldarg Arg_0x8
 conv.u8
 Branch_0x3d1:

 ldarg Arg_0x0
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 bge Branch_0x3d8

 ldarg Arg_0x1
 br Branch_0x3d9

 Branch_0x3d8:

 ldarg Arg_0x8
 Branch_0x3d9:

Start_Orphan_197:
 nop
End_Orphan_197:
 ldarg Arg_0x0
 conv.ovf.u8
 bgt.un Branch_0x3d6

Start_Orphan_198:
 nop
End_Orphan_198:
 ldc.i4.5
 ldarg Arg_0x3
 ble Branch_0x3da

 ldarg Arg_0x8
 br Branch_0x3db

 Branch_0x3da:

 ldarg Arg_0x8
 Branch_0x3db:

 br Branch_0x3d7

 Branch_0x3d6:

 ldc.i4.s -113
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 beq Branch_0x3dc

 ldc.i8 0x4d9b79aaec4f51e6
 br Branch_0x3dd

 Branch_0x3dc:

 ldc.i8 0x962d95379fe8c98d
 Branch_0x3dd:

 Branch_0x3d7:

 sub.ovf
Start_Orphan_199:
Start_Orphan_19a:
 nop
End_Orphan_19a:
 ldarg Arg_0x8
 ldarg Arg_0x7
 blt Branch_0x3e0

 ldc.i8 0x9d733a7b99a16666
 br Branch_0x3e1

 Branch_0x3e0:

 ldc.i8 0xcdf3ac39774aa82d
 Branch_0x3e1:

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 bge Branch_0x3e2

 ldloc LOCAL_0x6
 br Branch_0x3e3

 Branch_0x3e2:

 ldarg Arg_0x8
 Branch_0x3e3:

 blt Branch_0x3de

 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.1
 beq Branch_0x3e4

 nop
 br Branch_0x3e5

 Branch_0x3e4:

 nop
 Branch_0x3e5:

 br Branch_0x3df

 Branch_0x3de:

 ldarga Arg_0x4
 pop
 Branch_0x3df:

End_Orphan_199:
Start_Orphan_19b:
 ldloc LOCAL_0x2
 ldarg Arg_0x9
 ble.un Branch_0x3e6

 nop
 br Branch_0x3e7

 Branch_0x3e6:

 nop
 Branch_0x3e7:

End_Orphan_19b:
 ldarga Arg_0x4
 ldind.r4
 conv.u
 conv.u8
 blt Branch_0x3ce

Start_Orphan_19c:
 ldarga Arg_0x2
 pop
End_Orphan_19c:
Start_Orphan_19d:
 nop
End_Orphan_19d:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.1
 rem.un
 conv.ovf.u4
Start_Orphan_19e:
 ldloc LOCAL_0x1
 stloc LOCAL_0x1
End_Orphan_19e:
Start_Orphan_19f:
 nop
End_Orphan_19f:
 ldsflda unsigned int8 ILGEN_0xd73e17d6::field_0x5
 ldind.u1
Start_Orphan_1a0:
 nop
End_Orphan_1a0:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 div
 div
 add.ovf
 br Branch_0x3cf

 Branch_0x3ce:

Start_Orphan_1a1:
Start_Orphan_1a2:
 nop
End_Orphan_1a2:
 ldc.i4.2
 conv.u1
 newarr [mscorlib]System.SByte
 stsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x3
End_Orphan_1a1:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.8
 add
Start_Orphan_1a3:
 nop
End_Orphan_1a3:
 ldc.i4.0
 conv.ovf.i2
 sub.ovf.un
Start_Orphan_1a4:
 ldloc LOCAL_0x1
 ldarg Arg_0x9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 stelem.i
End_Orphan_1a4:
 ldc.r8 float64(0xaa879ce1f7cfd318)
 conv.u8
 conv.i1
 mul
 Branch_0x3cf:

Start_Orphan_1a5:
Start_Orphan_1a6:
 ldsfld float64[] ILGEN_0xd73e17d6::field_0x4
 pop
End_Orphan_1a6:
 ldsflda native int ILGEN_0xd73e17d6::field_0x6
 pop
End_Orphan_1a5:
Start_Orphan_1a7:
Start_Orphan_1a8:
 nop
End_Orphan_1a8:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x0
 pop
End_Orphan_1a7:
 ldc.i4.s 11
 conv.ovf.i2
 mul
 ble.un Branch_0x29a

Start_Orphan_1a9:
Start_Orphan_1aa:
 nop
End_Orphan_1aa:
 ldloca LOCAL_0x4
Start_Orphan_1ab:
 nop
End_Orphan_1ab:
 ldc.r8 float64(0x7e714a01fd39b0e4)
 conv.u8
 stind.i8
End_Orphan_1a9:
Start_Orphan_1ac:
 ldarga Arg_0x7
 pop
End_Orphan_1ac:
 ldarg Arg_0x8
 conv.i8
 conv.u8
Start_Orphan_1ad:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
 stsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
End_Orphan_1ad:
Start_Orphan_1ae:
 nop
End_Orphan_1ae:
 ldarg Arg_0x4
 call float64 [mscorlib]System.Math::Sin(float64)
 conv.ovf.i8
 mul.ovf.un
Start_Orphan_1af:
Start_Orphan_1b0:
 nop
End_Orphan_1b0:
 ldloc LOCAL_0x3
 ldc.i4.3
 ldelema [mscorlib]System.Int32
 pop
End_Orphan_1af:
 ldarg Arg_0x9
 ldc.i4.2
 div
 conv.r4
 conv.ovf.i8
 xor
Start_Orphan_1b1:
 nop
End_Orphan_1b1:
 ldc.i4.4
 ldc.i4.3
 rem
 conv.ovf.u2.un
 conv.ovf.u8
Start_Orphan_1b2:
Start_Orphan_1b3:
 nop
End_Orphan_1b3:
 ldc.i4.s 113
 conv.u1
 newarr [mscorlib]System.IntPtr
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.i4
 ldc.i8 0x4aa9b11925d80033
 conv.ovf.i2.un
 stelem.i
End_Orphan_1b2:
 ldarga Arg_0x5
 ldind.i8
 and
 rem.un
 br Branch_0x29b

 Branch_0x29a:

Start_Orphan_1b4:
 nop
End_Orphan_1b4:
 ldsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
 ldlen
 not
 conv.u1
 newarr [mscorlib]System.Int64
 ldlen
 conv.ovf.i8
 Branch_0x29b:

 Branch_0x7f:

 conv.r.un
Start_Orphan_1b5:
Start_Orphan_1b6:
Start_Orphan_1b7:
Start_Orphan_1b8:
 nop
End_Orphan_1b8:
 ldarg Arg_0x0
 ldc.i4.m1
 ceq
 stloc LOCAL_0x9
End_Orphan_1b7:
 ldarga Arg_0x3
 pop
End_Orphan_1b6:
Start_Orphan_1b9:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
 stsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
End_Orphan_1b9:
Start_Orphan_1ba:
 nop
End_Orphan_1ba:
 ldloc LOCAL_0x4
Start_Orphan_1bb:
 nop
End_Orphan_1bb:
 ldarg Arg_0x8
 conv.ovf.i8.un
 rem
 conv.i
 conv.ovf.u1.un
 conv.u1
 newarr [mscorlib]System.UInt64
 stsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x0
End_Orphan_1b5:
 ldarg Arg_0x1
 conv.r8
 bge.un Branch_0x0

Start_Orphan_1bc:
Start_Orphan_1bd:
Start_Orphan_1be:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x0
 ldc.i4.4
 ldelema [mscorlib]System.UInt64
 ldarg Arg_0x5
 ldloc LOCAL_0x6
 mul.ovf
 stind.i8
End_Orphan_1be:
 ldarg Arg_0x8
 starg Arg_0x1
End_Orphan_1bd:
Start_Orphan_1bf:
Start_Orphan_1c0:
 ldarg Arg_0x8
 pop
End_Orphan_1c0:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
Start_Orphan_1c1:
 ldc.i4.3
 stloc LOCAL_0x9
End_Orphan_1c1:
Start_Orphan_1c2:
 nop
End_Orphan_1c2:
 ldc.i4.s -7
 conv.u1
 newarr [mscorlib]System.UInt16
 ldlen
 ldarg Arg_0x8
 ldc.i8 0x1a98bae8e044d7ed
 div
 ldloc LOCAL_0x7
 ldarg Arg_0x1
 and
 rem
 stelem.i8
End_Orphan_1bf:
Start_Orphan_1c3:
Start_Orphan_1c4:
 nop
End_Orphan_1c4:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x2
 ldc.i4.5
 ldelema [mscorlib]System.UInt64
 pop
End_Orphan_1c3:
Start_Orphan_1c5:
 ldarga Arg_0x8
 pop
End_Orphan_1c5:
Start_Orphan_1c6:
 nop
End_Orphan_1c6:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 add.ovf
 conv.ovf.u.un
Start_Orphan_1c7:
 nop
End_Orphan_1c7:
 ldc.r4 float32(0xb6e3b423)
 conv.i2
Start_Orphan_1c8:
 nop
End_Orphan_1c8:
 ldc.i4.s -71
 conv.ovf.u.un
 sub.ovf
 mul.ovf
 conv.ovf.i2
 conv.u1
 newarr [mscorlib]System.Double
 stsfld float64[] ILGEN_0xd73e17d6::field_0x4
End_Orphan_1bc:
Start_Orphan_1c9:
Start_Orphan_1ca:
 ldarg Arg_0x8
 conv.ovf.u1.un
 conv.u1
 newarr [mscorlib]System.Int64
 pop
End_Orphan_1ca:
 ldarga Arg_0x6
 ldind.r8
 stloc LOCAL_0x0
End_Orphan_1c9:
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 conv.u1
 newarr [mscorlib]System.Int32
 stloc LOCAL_0x3
 br Branch_0x1

 Branch_0x0:

Start_Orphan_1cb:
 ldc.r4 float32(0xa3259b42)
 conv.u4
 stloc LOCAL_0x8
End_Orphan_1cb:
Start_Orphan_1cc:
Start_Orphan_1cd:
 ldc.i4.s -3
 ldc.i4.5
 rem.un
 starg Arg_0x0
End_Orphan_1cd:
 ldarg Arg_0x6
 starg Arg_0x4
End_Orphan_1cc:
Start_Orphan_1ce:
 nop
End_Orphan_1ce:
 ldloc LOCAL_0x4
 conv.r8
 ldarg Arg_0x4
 call float64 [mscorlib]System.Math::Abs(float64)
 clt
Start_Orphan_1cf:
 ldloc LOCAL_0x3
 pop
End_Orphan_1cf:
Start_Orphan_1d0:
 nop
End_Orphan_1d0:
 ldc.i4.8
 ldc.i4.2
 and
 ldc.i4.s 86
 conv.u
 sub.ovf
 mul.ovf.un
Start_Orphan_1d1:
Start_Orphan_1d2:
 nop
End_Orphan_1d2:
 ldc.i8 0x43040b8af2de94
 conv.i8
 pop
End_Orphan_1d1:
Start_Orphan_1d3:
 nop
End_Orphan_1d3:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.0
 cgt
 conv.u4
Start_Orphan_1d4:
 ldc.i8 0xc6f54c2afc32566
 stloc LOCAL_0x7
End_Orphan_1d4:
 ldc.r4 float32(0x566d39c0)
 call float64 [mscorlib]System.Math::Round(float64)
 conv.u
 sub.ovf
 cgt
 conv.ovf.i4
 conv.u1
 newarr [mscorlib]System.SByte
Start_Orphan_1d5:
Start_Orphan_1d6:
Start_Orphan_1d7:
 ldarg Arg_0x7
 starg Arg_0x1
End_Orphan_1d7:
Start_Orphan_1d8:
 nop
End_Orphan_1d8:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x2
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.i4
 ldelema [mscorlib]System.UInt64
 pop
End_Orphan_1d6:
Start_Orphan_1d9:
 nop
End_Orphan_1d9:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.2
 div
 conv.ovf.i1
 conv.i4
Start_Orphan_1da:
Start_Orphan_1db:
 nop
End_Orphan_1db:
 ldloca LOCAL_0x9
 ldloc LOCAL_0x0
 conv.ovf.i1
 stind.i
End_Orphan_1da:
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 sub.ovf.un
 stsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
End_Orphan_1d5:
Start_Orphan_1dc:
Start_Orphan_1dd:
 nop
End_Orphan_1dd:
 ldloc LOCAL_0x8
 conv.u1
 newarr [mscorlib]System.Double
Start_Orphan_1de:
 nop
End_Orphan_1de:
 ldc.i4.3
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 clt
 ldelema [mscorlib]System.Double
 pop
End_Orphan_1dc:
Start_Orphan_1df:
 ldsflda native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 stind.i
End_Orphan_1df:
 ldloc LOCAL_0x7
 conv.ovf.i2
 conv.u1
 newarr [mscorlib]System.SByte
 ldlen
Start_Orphan_1e0:
 ldarga Arg_0x3
 pop
End_Orphan_1e0:
 ldarga Arg_0x0
 ldind.u1
 conv.i
 clt.un
 ldelema [mscorlib]System.SByte
 pop
 Branch_0x1:

End_Orphan_0:
Start_Orphan_1e1:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
 pop
End_Orphan_1e1:
Start_Orphan_1e2:
Start_Orphan_1e3:
 nop
End_Orphan_1e3:
 ldc.i4.6
 conv.u1
 newarr [mscorlib]System.Int64
Start_Orphan_1e4:
 nop
End_Orphan_1e4:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.i2
 ldarg Arg_0x7
 ldarg Arg_0x8
 div.un
 stelem.i8
End_Orphan_1e2:
 ldloca LOCAL_0x7
 ldind.i8
Start_Orphan_1e5:
Start_Orphan_1e6:
 ldloca LOCAL_0x6
 ldarg Arg_0x8
 stind.i8
End_Orphan_1e6:
Start_Orphan_1e7:
 nop
End_Orphan_1e7:
 ldc.i8 0x634687396f8ca679
 conv.r8
Start_Orphan_1e8:
 nop
End_Orphan_1e8:
 ldarg Arg_0x6
 mul 
 starg Arg_0x4
End_Orphan_1e5:
Start_Orphan_1e9:
 nop
End_Orphan_1e9:
 ldsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
 ldc.i4.3
 ldelema [mscorlib]System.UInt16
 ldind.u2
 conv.u2
Start_Orphan_1ea:
 ldarga Arg_0x9
 ldc.i4.6
 stind.i4
End_Orphan_1ea:
 ldloc LOCAL_0x8
 conv.ovf.i8
 conv.u
 rem
 conv.ovf.u8
 div.un
 conv.ovf.u8.un
 conv.ovf.u
 conv.ovf.i8.un
 conv.r8
Start_Orphan_1eb:
Start_Orphan_1ec:
Start_Orphan_1ed:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x0
Start_Orphan_1ee:
 ldarga Arg_0x4
 ldc.r8 float64(0xd336491c991901c1)
 stind.r4
End_Orphan_1ee:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x0
 ldlen
 ldelema [mscorlib]System.UInt64
 pop
End_Orphan_1ed:
 ldloca LOCAL_0x8
Start_Orphan_1ef:
 ldloc LOCAL_0x3
 stloc LOCAL_0x3
End_Orphan_1ef:
Start_Orphan_1f0:
Start_Orphan_1f1:
 nop
End_Orphan_1f1:
 ldsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x3
 stsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x3
End_Orphan_1f0:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.i8
 conv.ovf.u8.un
Start_Orphan_1f2:
 nop
End_Orphan_1f2:
 ldc.i8 0x18e43c48892bf65
 ldarg Arg_0x2
 mul
 conv.u1
 shr.un
 conv.i8
Start_Orphan_1f3:
 ldarga Arg_0x4
Start_Orphan_1f4:
 ldsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
 pop
End_Orphan_1f4:
Start_Orphan_1f5:
 nop
End_Orphan_1f5:
 ldc.r4 float32(0x36064d8a)
 ldarg Arg_0x6
 sub  
 ckfinite
 stind.r4
End_Orphan_1f3:
Start_Orphan_1f6:
 ldsflda native int ILGEN_0xd73e17d6::field_0x6
 pop
End_Orphan_1f6:
Start_Orphan_1f7:
 nop
End_Orphan_1f7:
 ldarg Arg_0x8
 conv.u
 conv.ovf.u8.un
 ldarg Arg_0x1
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 shr
Start_Orphan_1f8:
 nop
End_Orphan_1f8:
 ldloc LOCAL_0x4
 ldc.i4.0
 shl
 add.ovf
 div.un
Start_Orphan_1f9:
 ldloc LOCAL_0x3
 ldarg Arg_0x0
 ldarg Arg_0x0
 stelem.i4
End_Orphan_1f9:
Start_Orphan_1fa:
 nop
End_Orphan_1fa:
 ldloc LOCAL_0x0
 ckfinite
 ldarg Arg_0x1
 conv.r8
 div 
 conv.ovf.u8
 sub.ovf
 clt.un
 stind.i1
End_Orphan_1ec:
Start_Orphan_1fb:
Start_Orphan_1fc:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x0
 ldc.i4.0
 ldelema [mscorlib]System.UInt64
 ldc.i4.m1
 conv.ovf.i8.un
 stind.i8
End_Orphan_1fc:
Start_Orphan_1fd:
 ldloc LOCAL_0x1
 ldloc LOCAL_0x9
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 stelem.i
End_Orphan_1fd:
 ldloc LOCAL_0x0
 conv.i2
 conv.ovf.u4.un
Start_Orphan_1fe:
Start_Orphan_1ff:
 nop
End_Orphan_1ff:
 ldsflda native int ILGEN_0xd73e17d6::field_0x6
Start_Orphan_200:
 nop
End_Orphan_200:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 mul
 stind.i
End_Orphan_1fe:
 ldc.i4.m1
 or
 starg Arg_0x9
End_Orphan_1fb:
Start_Orphan_201:
 ldloc LOCAL_0x8
 conv.u1
 newarr [mscorlib]System.IntPtr
 ldc.i4.m1
Start_Orphan_202:
 nop
End_Orphan_202:
 ldsflda native int ILGEN_0xd73e17d6::field_0x6
 ldind.i
 stelem.i
End_Orphan_201:
Start_Orphan_203:
 ldarga Arg_0x6
 pop
End_Orphan_203:
Start_Orphan_204:
 nop
End_Orphan_204:
 ldarg Arg_0x4
 conv.u8
Start_Orphan_205:
 nop
End_Orphan_205:
 ldarg Arg_0x8
 mul.ovf
Start_Orphan_206:
 ldloc LOCAL_0x6
 pop
End_Orphan_206:
 ldloc LOCAL_0x9
 ldc.i4.5
 xor
 conv.i8
 or
 conv.ovf.i4
Start_Orphan_207:
 nop
End_Orphan_207:
 ldc.i4.7
 ldc.i4 0xe5d22da5
 and
 conv.ovf.u.un
Start_Orphan_208:
 nop
End_Orphan_208:
 ldc.i4.0
 conv.ovf.u8.un
 conv.ovf.i2.un
 add.ovf
 conv.u2
 div.un
 conv.ovf.u8
Start_Orphan_209:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x0
Start_Orphan_20a:
Start_Orphan_20b:
 nop
End_Orphan_20b:
 ldloc LOCAL_0x0
 call float64 [mscorlib]System.Math::Abs(float64)
 conv.u2
 stloc LOCAL_0x2
End_Orphan_20a:
Start_Orphan_20c:
Start_Orphan_20d:
 nop
End_Orphan_20d:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
 pop
End_Orphan_20c:
 ldarga Arg_0x3
 ldind.i4
Start_Orphan_20e:
 nop
End_Orphan_20e:
 ldarg Arg_0x8
 ldarg Arg_0x5
 sub.ovf.un
Start_Orphan_20f:
 nop
End_Orphan_20f:
 ldloc LOCAL_0x7
 conv.u8
 sub.ovf
 conv.ovf.u4
 div
Start_Orphan_210:
Start_Orphan_211:
 nop
End_Orphan_211:
 ldarg Arg_0x3
 conv.u1
 newarr [mscorlib]System.UInt16
 ldsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x8
 ldloc LOCAL_0x2
 ldelema [mscorlib]System.SByte
 ldind.i1
 ldelema [mscorlib]System.UInt16
Start_Orphan_212:
 ldarga Arg_0x8
 pop
End_Orphan_212:
Start_Orphan_213:
 nop
End_Orphan_213:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.u4
Start_Orphan_214:
 nop
End_Orphan_214:
 ldloc LOCAL_0x9
 conv.i
 cgt.un
 stind.i2
End_Orphan_210:
Start_Orphan_215:
 ldarga Arg_0x6
 pop
End_Orphan_215:
Start_Orphan_216:
 ldarga Arg_0x3
 ldc.i4.2
 stind.i4
End_Orphan_216:
 ldc.i4.2
 conv.i8
Start_Orphan_217:
 nop
End_Orphan_217:
 ldarg Arg_0x1
 sub
Start_Orphan_218:
 ldsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
 ldc.i4.2
 ldelema [mscorlib]System.UInt16
 ldarg Arg_0x9
 stind.i2
End_Orphan_218:
Start_Orphan_219:
 nop
End_Orphan_219:
 ldarg Arg_0x2
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.ovf.u8
 rem
 sub.ovf.un
 not
 stelem.i8
End_Orphan_209:
 ldarg Arg_0x7
 conv.ovf.i
Start_Orphan_21a:
 ldloc LOCAL_0x3
 ldc.i4.3
 ldc.i4.1
 stelem.i4
End_Orphan_21a:
Start_Orphan_21b:
 nop
End_Orphan_21b:
 ldc.i8 0x3966e38f550fb8e0
 conv.ovf.u1
 ldarg Arg_0x0
 ldc.i4.2
 or
 cgt.un
 rem
Start_Orphan_21c:
 ldc.i8 0x3372fc81578983d9
 starg Arg_0x5
End_Orphan_21c:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 add.ovf.un
Start_Orphan_21d:
 nop
End_Orphan_21d:
 ldc.i4.4
 conv.ovf.i.un
 or
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x0
 ldlen
 conv.ovf.u2
 rem.un
 or
 conv.ovf.i8
 not
 div
 pop
End_Orphan_1eb:
Start_Orphan_21e:
Start_Orphan_21f:
Start_Orphan_220:
Start_Orphan_221:
 ldloc LOCAL_0x3
 pop
End_Orphan_221:
Start_Orphan_222:
 nop
End_Orphan_222:
 ldc.i4.2
 ldarg Arg_0x3
 rem.un
 conv.u1
 newarr [mscorlib]System.Int64
 stsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
End_Orphan_220:
 ldsfld float64[] ILGEN_0xd73e17d6::field_0x4
 stloc LOCAL_0x5
End_Orphan_21f:
 ldsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
Start_Orphan_223:
 ldarga Arg_0x9
 pop
End_Orphan_223:
Start_Orphan_224:
 nop
End_Orphan_224:
Start_Orphan_225:
 ldloca LOCAL_0x2
 ldc.i4.0
 stind.i4
End_Orphan_225:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.u
 conv.u8
 ldarg Arg_0x8
 ldloc LOCAL_0x4
 rem
 ldarg Arg_0x8
 conv.i2
 shr
 xor
 conv.ovf.u1.un
Start_Orphan_226:
Start_Orphan_227:
 ldarga Arg_0x6
 ldarg Arg_0x4
 stind.r8
End_Orphan_227:
Start_Orphan_228:
 nop
End_Orphan_228:
 ldarg Arg_0x6
 call float32 [mscorlib]System.Math::Abs(float32)
 pop
End_Orphan_226:
Start_Orphan_229:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 stsfld native int ILGEN_0xd73e17d6::field_0x6
End_Orphan_229:
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 conv.u1
 newarr [mscorlib]System.UInt16
 ldlen
 mul
 ldelema [mscorlib]System.UInt16
Start_Orphan_22a:
Start_Orphan_22b:
Start_Orphan_22c:
 nop
End_Orphan_22c:
 ldsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x3
Start_Orphan_22d:
 nop
End_Orphan_22d:
 ldc.i4.7
 conv.ovf.i2
 ldelema [mscorlib]System.SByte
Start_Orphan_22e:
 nop
End_Orphan_22e:
 ldsfld unsigned int8[] ILGEN_0xd73e17d6::field_0x3
 ldloc LOCAL_0x9
 ldelema [mscorlib]System.SByte
 ldind.i1
 stind.i1
End_Orphan_22b:
 ldsflda unsigned int8 ILGEN_0xd73e17d6::field_0x5
 pop
End_Orphan_22a:
Start_Orphan_22f:
Start_Orphan_230:
 ldarga Arg_0x6
 ldarg Arg_0x4
 stind.r8
End_Orphan_230:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldarg Arg_0x3
 sub.ovf.un
Start_Orphan_231:
 nop
End_Orphan_231:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 mul.ovf
 div
 pop
End_Orphan_22f:
Start_Orphan_232:
 nop
End_Orphan_232:
 ldc.r8 float64(0x1a49d967a271e00b)
 call float32 [mscorlib]System.Math::Abs(float32)
 call float64 [mscorlib]System.Math::Abs(float64)
 ckfinite
 conv.u2
 conv.ovf.i4
 conv.u2
 stind.i2
End_Orphan_21e:
Start_Orphan_233:
Start_Orphan_234:
Start_Orphan_235:
 nop
End_Orphan_235:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldc.i4.3
 sub.ovf.un
 pop
End_Orphan_234:
 ldloca LOCAL_0x2
 pop
End_Orphan_233:
 ldloc LOCAL_0x1
 ldlen
 conv.r.un
Start_Orphan_236:
Start_Orphan_237:
Start_Orphan_238:
 nop
End_Orphan_238:
 ldloc LOCAL_0x1
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 ldelema [mscorlib]System.IntPtr
 pop
End_Orphan_237:
 ldloc LOCAL_0x6
 neg
 conv.i2
 conv.u1
 newarr [mscorlib]System.UInt64
 pop
End_Orphan_236:
Start_Orphan_239:
Start_Orphan_23a:
 ldc.i4 6 
 Loop_0x2: //WHAT
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x2
 pop
 Loop_0x3: // End of WHAT
End_Orphan_23a:
 ldsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 ldelema [mscorlib]System.UInt16
 ldind.u2
 conv.r4
 starg Arg_0x6
End_Orphan_239:
Start_Orphan_23b:
 ldloc LOCAL_0x5
 ldc.i4.0
 ldarg Arg_0x4
 stelem.r8
End_Orphan_23b:
Start_Orphan_23c:
 nop
End_Orphan_23c:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 ldloc LOCAL_0x8
 mul.ovf
Start_Orphan_23d:
 nop
End_Orphan_23d:
 ldsfld unsigned int16[] ILGEN_0xd73e17d6::field_0x9
 ldloc LOCAL_0x9
 ldelema [mscorlib]System.UInt16
 ldind.u2
 div
 conv.i1
Start_Orphan_23e:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
 ldarg Arg_0x0
 ldelema [mscorlib]System.Int64
Start_Orphan_23f:
 nop
End_Orphan_23f:
 ldloc LOCAL_0x2
 conv.ovf.u8
 stind.i8
End_Orphan_23e:
Start_Orphan_240:
 nop
End_Orphan_240:
 ldloc LOCAL_0x4
 conv.ovf.i1.un
 not
 conv.ovf.i4.un
 sub.ovf.un
 conv.r4
 conv.r4
 ldarg Arg_0x4
Start_Orphan_241:
Start_Orphan_242:
 ldarg Arg_0x1
 ldloc LOCAL_0x4
 and
 starg Arg_0x5
End_Orphan_242:
 ldloc LOCAL_0x1
 pop
End_Orphan_241:
Start_Orphan_243:
 ldc.i4.0
 stloc LOCAL_0x9
End_Orphan_243:
Start_Orphan_244:
 nop
End_Orphan_244:
 ldsfld native int ILGEN_0xd73e17d6::field_0x6
 conv.u4
 conv.u1
 newarr [mscorlib]System.UInt64
Start_Orphan_245:
 ldarga Arg_0x6
 pop
End_Orphan_245:
 ldsfld float64[] ILGEN_0xd73e17d6::field_0x7
 ldarg Arg_0x3
 ldelem.r8
 conv.ovf.u1
 ldelem.u8
Start_Orphan_246:
Start_Orphan_247:
 nop
End_Orphan_247:
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x0
Start_Orphan_248:
 nop
End_Orphan_248:
 ldloc LOCAL_0x0
 conv.ovf.u1
 ldsfld unsigned int64[] ILGEN_0xd73e17d6::field_0x1
 ldsfld unsigned int8 ILGEN_0xd73e17d6::field_0x5
 ldelem.i8
 stelem.i8
End_Orphan_246:
 ldarg Arg_0x9
 conv.ovf.i8.un
 sub.ovf.un
 conv.r4
 mul 
 rem 
 sub  
 call float64 [mscorlib]System.Math::Cos(float64)
 clt.un
EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0x773532d7 
ldc.i8 0x6cd0df7031768022 
ldc.i8 0x2163249f5b909ffd 
ldc.i4 0xfcd57784 
ldc.r4 float32(0x2e41c277) 
ldc.i8 0xe92709c07d69fdc2 
ldc.r8 float64(0x9c9c981bb1ffdb98) 
ldc.i8 0x89cd59047f9a5e83 
ldc.i8 0x35bee1bb4d0a22f1 
ldc.i4 0x40b8143d 

call int32 ILGEN_0xd73e17d6::Method_0x49426e8a(unsigned int8 Arg_0x0, int64 Arg_0x1, int64 Arg_0x2, int32 Arg_0x3, float32 Arg_0x4, int64 Arg_0x5, float64 Arg_0x6, int64 Arg_0x7, unsigned int64 Arg_0x8, unsigned int32 Arg_0x9)
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
