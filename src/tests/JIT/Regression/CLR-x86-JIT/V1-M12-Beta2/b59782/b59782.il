// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x372a9ae6 {}
.class ILGEN_0x372a9ae6 {

//BEGIN STATIC FIELDS
.field static unsigned int64 field_0x0
.field static unsigned	int32[] field_0x1
.field static	float64 field_0x2
.field static	unsigned int64[] field_0x3
.field static unsigned int32[] field_0x4
.field static unsigned int16 field_0x5
//END STATIC FIELDS
.method static int32 Method_0xdc6ff1a4(int8 Arg_0x0, int8 Arg_0x1, int32 Arg_0x2, native int Arg_0x3, unsigned int16 Arg_0x4, float64 Arg_0x5, int64 Arg_0x6, unsigned int64 Arg_0x7) {
.maxstack	19

 .locals (int32[] LOCAL_0x0,int8[] LOCAL_0x1,float64[] LOCAL_0x2,native int[] LOCAL_0x3,float32 LOCAL_0x4)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc LOCAL_0x0
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc LOCAL_0x1
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc LOCAL_0x2
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stloc LOCAL_0x3
ldc.r4 float32(0x59db9b75) 
stloc LOCAL_0x4

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i8 0x227dbe6e3568178 
stsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld unsigned int32[] ILGEN_0x372a9ae6::field_0x1
ldc.r8 float64(0x15229b83426c7a54) 
stsfld float64 ILGEN_0x372a9ae6::field_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld unsigned int32[] ILGEN_0x372a9ae6::field_0x4
ldc.i4 0x75842e8e 
stsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
Start_Orphan_3:
 nop
End_Orphan_3:
 ldc.i4.1
 ldc.i4.6
 ble Branch_0x2

 ldc.r4 float32(0xd409f42c)
 br Branch_0x3

 Branch_0x2:

 ldc.r4 float32(0x656ff3c4)
 Branch_0x3:

 pop
End_Orphan_2:
Start_Orphan_4:
 ldloc LOCAL_0x2
 ldc.i4.5
 ldarg Arg_0x5
 stelem.r8
End_Orphan_4:
 ldc.i4.5
 ldarg Arg_0x2
 clt.un
 ldloc LOCAL_0x4
 ldc.r8 float64(0xe09b6fc9cde2211f)
 blt Branch_0x6

 ldarg Arg_0x3
 br Branch_0x7

 Branch_0x6:

 ldarg Arg_0x3
 Branch_0x7:

 beq Branch_0x4

Start_Orphan_5:
 nop
End_Orphan_5:
 ldc.i4.4
 ldarg Arg_0x3
 cgt
 br Branch_0x5

 Branch_0x4:

Start_Orphan_6:
 nop
End_Orphan_6:
 ldc.i4.3
 ldarg Arg_0x3
 bgt.un Branch_0x8

 ldc.i4.2
 br Branch_0x9

 Branch_0x8:

 ldarg Arg_0x2
 Branch_0x9:

 Branch_0x5:

 conv.i2
 conv.ovf.i
 brtrue Branch_0x0

Start_Orphan_7:
 ldarg Arg_0x3
 ldc.i4.7
 sub
Start_Orphan_8:
 nop
End_Orphan_8:
 ldarg Arg_0x3
 ldarg Arg_0x3
 mul
 bge Branch_0xa

 ldloc LOCAL_0x3
 pop
 br Branch_0xb

 Branch_0xa:

 ldarg Arg_0x5
 ldc.r8 float64(0x2e90a6eb69953800)
 bgt Branch_0xc

 nop
 br Branch_0xd

 Branch_0xc:

 nop
 Branch_0xd:

 Branch_0xb:

End_Orphan_7:
 ldarga Arg_0x2
 pop
 br Branch_0x1

 Branch_0x0:

Start_Orphan_9:
 ldloc LOCAL_0x2
 pop
End_Orphan_9:
Start_Orphan_a:
 nop
End_Orphan_a:
 ldc.r4 float32(0xcc33f5c5)
 conv.r4
 ldarg Arg_0x5
 blt.un Branch_0x10

 ldarg Arg_0x3
 ldc.i4.m1
 bgt.un Branch_0x12

 ldc.i4.4
 br Branch_0x13

 Branch_0x12:

 ldc.i4.7
 Branch_0x13:

 br Branch_0x11

 Branch_0x10:

 ldarg Arg_0x5
 conv.u1
 Branch_0x11:

 ldc.r4 float32(0x66477336)
 ldloc LOCAL_0x4
 ble Branch_0x14

 ldc.i4.6
 br Branch_0x15

 Branch_0x14:

 ldc.i4.8
 Branch_0x15:

Start_Orphan_b:
 nop
End_Orphan_b:
 ldarg Arg_0x7
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 bge.un Branch_0x16

 ldc.i4.1
 br Branch_0x17

 Branch_0x16:

 ldc.i4.5
 Branch_0x17:

 cgt
 mul
 ldarg Arg_0x3
 ldc.i4 0x41d6c2fa
 bge.un Branch_0x18

 ldarg Arg_0x3
 br Branch_0x19

 Branch_0x18:

 ldarg Arg_0x3
 Branch_0x19:

 ldc.i4.m1
 ldc.i4.6
 bgt Branch_0x1a

 ldarg Arg_0x3
 br Branch_0x1b

 Branch_0x1a:

 ldarg Arg_0x3
 Branch_0x1b:

 clt
 conv.ovf.i.un
 ble Branch_0xe

Start_Orphan_c:
 ldloc LOCAL_0x2
 stloc LOCAL_0x2
End_Orphan_c:
 ldarga Arg_0x3
Start_Orphan_d:
 ldarg Arg_0x4
 ldarg Arg_0x3
 ble Branch_0x1c

 nop
 br Branch_0x1d

 Branch_0x1c:

 nop
 Branch_0x1d:

End_Orphan_d:
Start_Orphan_e:
 nop
End_Orphan_e:
 ldc.i4.1
 ldarg Arg_0x3
 blt.un Branch_0x20

 ldc.i4.3
 br Branch_0x21

 Branch_0x20:

 ldc.i4.4
 Branch_0x21:

Start_Orphan_f:
 nop
End_Orphan_f:
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 brtrue Branch_0x22

 ldarg Arg_0x3
 br Branch_0x23

 Branch_0x22:

 ldarg Arg_0x3
 Branch_0x23:

 beq Branch_0x1e

Start_Orphan_10:
 nop
End_Orphan_10:
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 conv.ovf.i1.un
 br Branch_0x1f

 Branch_0x1e:

 ldc.r4 float32(0x638f5c21)
 ldloc LOCAL_0x4
 bge.un Branch_0x24

 ldarg Arg_0x3
 br Branch_0x25

 Branch_0x24:

 ldarg Arg_0x3
 Branch_0x25:

 Branch_0x1f:

 stind.i
 br Branch_0xf

 Branch_0xe:

Start_Orphan_11:
 ldarg Arg_0x7
 pop
End_Orphan_11:
 ldc.i4.1
 ldarg Arg_0x3
 bge Branch_0x2a

 ldc.i8 0x4b2cd314d6f414bb
 br Branch_0x2b

 Branch_0x2a:

 ldarg Arg_0x7
 Branch_0x2b:

 ldc.i4.4
 ldarg Arg_0x3
 ble.un Branch_0x2c

 ldarg Arg_0x6
 br Branch_0x2d

 Branch_0x2c:

 ldarg Arg_0x6
 Branch_0x2d:

 ble Branch_0x28

Start_Orphan_12:
 nop
End_Orphan_12:
 ldarg Arg_0x3
 ldc.i4.m1
 bne.un Branch_0x2e

 ldc.r8 float64(0x7622d4a494f26c1b)
 br Branch_0x2f

 Branch_0x2e:

 ldc.r8 float64(0x2136f68c9bf8e039)
 Branch_0x2f:

 br Branch_0x29

 Branch_0x28:

Start_Orphan_13:
 nop
End_Orphan_13:
 ldc.i4.4
 ldc.i4 0xe39a83a
 bne.un Branch_0x30

 ldarg Arg_0x5
 br Branch_0x31

 Branch_0x30:

 ldloc LOCAL_0x4
 Branch_0x31:

 Branch_0x29:

 ldarg Arg_0x7
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 div.un
 conv.r.un
 ble Branch_0x26

Start_Orphan_14:
 nop
End_Orphan_14:
 ldarg Arg_0x4
 ldarg Arg_0x3
 bgt.un Branch_0x34

 ldc.i8 0x3fabf24515981853
 br Branch_0x35

 Branch_0x34:

 ldarg Arg_0x6
 Branch_0x35:

 ldarg Arg_0x1
 ldc.i4.5
 bge Branch_0x36

 ldarg Arg_0x6
 br Branch_0x37

 Branch_0x36:

 ldc.i8 0x70bb8a1c3d3045a3
 Branch_0x37:

 blt Branch_0x32

 ldarg Arg_0x3
 brfalse Branch_0x38

 nop
 br Branch_0x39

 Branch_0x38:

 nop
 Branch_0x39:

 br Branch_0x33

 Branch_0x32:

 ldarg Arg_0x3
 ldarg Arg_0x3
 bgt.un Branch_0x3a

 nop
 br Branch_0x3b

 Branch_0x3a:

 nop
 Branch_0x3b:

 Branch_0x33:

 br Branch_0x27

 Branch_0x26:

Start_Orphan_15:
 nop
End_Orphan_15:
 ldsflda unsigned int16 ILGEN_0x372a9ae6::field_0x5
Start_Orphan_16:
 nop
End_Orphan_16:
 ldarg Arg_0x3
 conv.ovf.i4
 stind.i2
 Branch_0x27:

 Branch_0xf:

 Branch_0x1:

End_Orphan_1:
Start_Orphan_17:
Start_Orphan_18:
 ldloc LOCAL_0x3
 stloc LOCAL_0x3
End_Orphan_18:
 ldc.i4.5
Start_Orphan_19:
 ldc.i4.8
 ldarg Arg_0x3
 bge Branch_0x40

 nop
 br Branch_0x41

 Branch_0x40:

 nop
 Branch_0x41:

End_Orphan_19:
 ldarg Arg_0x3
 ldc.i4.7
 bne.un Branch_0x44

 ldc.r8 float64(0xc61af91515d01884)
 br Branch_0x45

 Branch_0x44:

 ldc.r4 float32(0x4885d889)
 Branch_0x45:

 ldloca LOCAL_0x4
 ldind.r4
 bgt Branch_0x42

Start_Orphan_1a:
 nop
End_Orphan_1a:
 ldarg Arg_0x3
 ldc.i4.0
 clt.un
 br Branch_0x43

 Branch_0x42:

 ldloc LOCAL_0x1
 ldlen
 Branch_0x43:

 bgt.un Branch_0x3e

 ldc.i4 0x62d95f7b
 ldc.i4 0x5b2aa88d
 bgt Branch_0x48

 ldarg Arg_0x3
 br Branch_0x49

 Branch_0x48:

 ldarg Arg_0x3
 Branch_0x49:

 ldc.i4.3
 brtrue Branch_0x4a

 ldc.i4.m1
 br Branch_0x4b

 Branch_0x4a:

 ldc.i4.0
 Branch_0x4b:

 bne.un Branch_0x46

 ldarg Arg_0x6
 ldarg Arg_0x7
 bgt.un Branch_0x4c

 ldarg Arg_0x5
 br Branch_0x4d

 Branch_0x4c:

 ldc.r8 float64(0xc990cdbcc6e138a7)
 Branch_0x4d:

 br Branch_0x47

 Branch_0x46:

Start_Orphan_1b:
 nop
End_Orphan_1b:
 ldc.i4.7
 ldarg Arg_0x3
 bgt.un Branch_0x4e

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x4f

 Branch_0x4e:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x4f:

 Branch_0x47:

 br Branch_0x3f

 Branch_0x3e:

 ldc.i4.s -111
 ldarg Arg_0x3
 ldc.i4.s 70
 bge Branch_0x52

 ldarg Arg_0x3
 br Branch_0x53

 Branch_0x52:

 ldarg Arg_0x3
 Branch_0x53:

 blt Branch_0x50

Start_Orphan_1c:
 nop
End_Orphan_1c:
 ldc.r4 float32(0x430840c2)
 call float64 [mscorlib]System.Math::Cos(float64)
 br Branch_0x51

 Branch_0x50:

Start_Orphan_1d:
 nop
End_Orphan_1d:
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x51:

 Branch_0x3f:

Start_Orphan_1e:
 ldsflda unsigned int16 ILGEN_0x372a9ae6::field_0x5
 ldc.i4.8
 stind.i2
End_Orphan_1e:
Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldarg Arg_0x3
 ldc.i4.1
 cgt.un
Start_Orphan_20:
 nop
End_Orphan_20:
 ldarg Arg_0x3
 ldarg Arg_0x3
 div
 blt.un Branch_0x56

Start_Orphan_21:
 nop
End_Orphan_21:
 ldc.i4.6
 ldarg Arg_0x3
 blt.un Branch_0x58

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x59

 Branch_0x58:

 ldloc LOCAL_0x4
 Branch_0x59:

 br Branch_0x57

 Branch_0x56:

 ldc.i4.3
 ldarg Arg_0x3
 ble Branch_0x5a

 ldarg Arg_0x5
 br Branch_0x5b

 Branch_0x5a:

 ldarg Arg_0x5
 Branch_0x5b:

 Branch_0x57:

Start_Orphan_22:
 ldloca LOCAL_0x4
 pop
End_Orphan_22:
Start_Orphan_23:
 nop
End_Orphan_23:
 ldarg Arg_0x3
 ldarg Arg_0x3
 ble.un Branch_0x5e

 ldarg Arg_0x7
 br Branch_0x5f

 Branch_0x5e:

 ldarg Arg_0x7
 Branch_0x5f:

Start_Orphan_24:
 nop
End_Orphan_24:
 ldarg Arg_0x3
 ldc.i4.0
 bge.un Branch_0x60

 ldarg Arg_0x7
 br Branch_0x61

 Branch_0x60:

 ldc.i8 0x4ae3353114f42a6c
 Branch_0x61:

 bne.un Branch_0x5c

 ldc.i4.0
 ldc.i4.8
 blt Branch_0x62

 ldarg Arg_0x5
 br Branch_0x63

 Branch_0x62:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x63:

 br Branch_0x5d

 Branch_0x5c:

Start_Orphan_25:
 nop
End_Orphan_25:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldc.i8 0xa1ae39fe183bed74
 beq Branch_0x64

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x65

 Branch_0x64:

 ldc.r8 float64(0x1ca9920039295824)
 Branch_0x65:

 Branch_0x5d:

 ble Branch_0x54

Start_Orphan_26:
 nop
End_Orphan_26:
 ldc.i4.4
 ldarg Arg_0x3
 bge.un Branch_0x68

 ldc.i4.8
 br Branch_0x69

 Branch_0x68:

 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 Branch_0x69:

Start_Orphan_27:
 nop
End_Orphan_27:
 ldc.i4.6
 conv.u4
 bgt.un Branch_0x66

Start_Orphan_28:
 nop
End_Orphan_28:
 ldc.r8 float64(0x5a673fb125467b2a)
 neg
 br Branch_0x67

 Branch_0x66:

 ldarg Arg_0x6
 ldarg Arg_0x7
 bgt.un Branch_0x6a

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x6b

 Branch_0x6a:

 ldc.r8 float64(0x51185b57e8f00f43)
 Branch_0x6b:

 Branch_0x67:

 br Branch_0x55

 Branch_0x54:

Start_Orphan_29:
 ldarg Arg_0x1
 brtrue Branch_0x6c

 nop
 br Branch_0x6d

 Branch_0x6c:

 nop
 Branch_0x6d:

End_Orphan_29:
Start_Orphan_2a:
 nop
End_Orphan_2a:
 ldloc LOCAL_0x4
 ldloc LOCAL_0x4
 bne.un Branch_0x70

 ldarg Arg_0x3
 br Branch_0x71

 Branch_0x70:

 ldarg Arg_0x3
 Branch_0x71:

Start_Orphan_2b:
 nop
End_Orphan_2b:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 conv.ovf.i
 beq Branch_0x6e

Start_Orphan_2c:
 nop
End_Orphan_2c:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldarg Arg_0x7
 blt Branch_0x72

 ldc.r4 float32(0x9ead8c15)
 br Branch_0x73

 Branch_0x72:

 ldc.r8 float64(0x5278e8f46f1db6f8)
 Branch_0x73:

 br Branch_0x6f

 Branch_0x6e:

 ldc.i4.5
 ldc.i4.8
 ble Branch_0x74

 ldarg Arg_0x5
 br Branch_0x75

 Branch_0x74:

 ldloc LOCAL_0x4
 Branch_0x75:

 Branch_0x6f:

 Branch_0x55:

 bgt.un Branch_0x3c

Start_Orphan_2d:
 ldsflda unsigned int16 ILGEN_0x372a9ae6::field_0x5
 ldarg Arg_0x1
 stind.i2
End_Orphan_2d:
Start_Orphan_2e:
 nop
End_Orphan_2e:
 ldarg Arg_0x0
 ldarg Arg_0x4
 bgt.un Branch_0x7a

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x7b

 Branch_0x7a:

 ldloc LOCAL_0x4
 Branch_0x7b:

 ldarg Arg_0x5
 conv.r8
 blt.un Branch_0x78

 ldarga Arg_0x4
 ldind.u2
 br Branch_0x79

 Branch_0x78:

Start_Orphan_2f:
 nop
End_Orphan_2f:
 ldc.i4.2
 ldarg Arg_0x3
 clt
 Branch_0x79:

Start_Orphan_30:
 ldsflda unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldarg Arg_0x7
 stind.i8
End_Orphan_30:
 ldc.i4.3
 ldarg Arg_0x3
 bgt.un Branch_0x7c

 ldarg Arg_0x3
 br Branch_0x7d

 Branch_0x7c:

 ldarg Arg_0x3
 Branch_0x7d:

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 conv.ovf.i4.un
 div.un
 bgt.un Branch_0x76

 ldc.i4.0
 ldarg Arg_0x3
 ble.un Branch_0x80

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 br Branch_0x81

 Branch_0x80:

 ldarg Arg_0x7
 Branch_0x81:

Start_Orphan_31:
 nop
End_Orphan_31:
 ldarg Arg_0x3
 conv.ovf.u8
 bge Branch_0x7e

 ldloc LOCAL_0x3
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 ldarg Arg_0x3
 stelem.i
 br Branch_0x7f

 Branch_0x7e:

 ldarg Arg_0x6
 ldc.i8 0x9b7349f36ffebc4b
 beq Branch_0x82

 nop
 br Branch_0x83

 Branch_0x82:

 nop
 Branch_0x83:

 Branch_0x7f:

 br Branch_0x77

 Branch_0x76:

Start_Orphan_32:
 nop
End_Orphan_32:
 ldsfld unsigned int32[] ILGEN_0x372a9ae6::field_0x1
Start_Orphan_33:
 nop
End_Orphan_33:
 ldarg Arg_0x3
 ldarg Arg_0x4
 blt Branch_0x84

 ldc.i4 0x53c5e4bf
 br Branch_0x85

 Branch_0x84:

 ldarg Arg_0x1
 Branch_0x85:

 ldc.i4.7
 conv.u1
 stelem.i4
 Branch_0x77:

 br Branch_0x3d

 Branch_0x3c:

Start_Orphan_34:
 nop
End_Orphan_34:
 ldarg Arg_0x3
 ldc.i4.s -126
 blt.un Branch_0x88

 ldc.r4 float32(0xca3ebff3)
 br Branch_0x89

 Branch_0x88:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x89:

 conv.ovf.i4
Start_Orphan_35:
 ldloca LOCAL_0x4
 ldc.r8 float64(0xebfdad28dff5d1fb)
 stind.r4
End_Orphan_35:
 ldc.i4.4
 ldarg Arg_0x4
 ble.un Branch_0x8a

 ldarg Arg_0x3
 br Branch_0x8b

 Branch_0x8a:

 ldarg Arg_0x3
 Branch_0x8b:

Start_Orphan_36:
 nop
End_Orphan_36:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 conv.ovf.i
 mul
 bgt.un Branch_0x86

 ldc.r4 float32(0xa12cccd6)
 ldc.r8 float64(0x27bb2b7c58156862)
 blt.un Branch_0x8e

 ldarg Arg_0x3
 br Branch_0x8f

 Branch_0x8e:

 ldarg Arg_0x3
 Branch_0x8f:

 brtrue Branch_0x8c

 ldloc LOCAL_0x3
 ldc.i4 0xec549cad
 ldarg Arg_0x3
 stelem.i
 br Branch_0x8d

 Branch_0x8c:

 ldc.r8 float64(0xe1ea7f1ff81d0747)
 stloc LOCAL_0x4
 Branch_0x8d:

 br Branch_0x87

 Branch_0x86:

Start_Orphan_37:
 nop
End_Orphan_37:
 ldloc LOCAL_0x3
 ldarg Arg_0x6
 conv.ovf.u2
 ldarg Arg_0x3
 ldc.i4.4
 blt.un Branch_0x90

 ldarg Arg_0x3
 br Branch_0x91

 Branch_0x90:

 ldarg Arg_0x3
 Branch_0x91:

 stelem.i
 Branch_0x87:

 Branch_0x3d:

End_Orphan_17:
Start_Orphan_38:
Start_Orphan_39:
 ldarg Arg_0x3
 ldarg Arg_0x3
 blt Branch_0x94

 nop
 br Branch_0x95

 Branch_0x94:

 nop
 Branch_0x95:

End_Orphan_39:
 ldloc LOCAL_0x0
 ldc.i4.0
Start_Orphan_3a:
 nop
End_Orphan_3a:
 ldarg Arg_0x3
 ldarg Arg_0x3
 mul
 ceq
Start_Orphan_3b:
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 ldc.i4.0
 blt.un Branch_0x96

 nop
 br Branch_0x97

 Branch_0x96:

 nop
 Branch_0x97:

End_Orphan_3b:
Start_Orphan_3c:
 nop
End_Orphan_3c:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldc.i8 0xb92805ec34838de6
 and
 conv.i2
 stelem.i4
End_Orphan_38:
Start_Orphan_3d:
Start_Orphan_3e:
 nop
End_Orphan_3e:
 ldloc LOCAL_0x2
Start_Orphan_3f:
 nop
End_Orphan_3f:
 ldarg Arg_0x3
 conv.ovf.u4
 ldarg Arg_0x3
 ldc.i4.0
 blt.un Branch_0x98

 ldc.r8 float64(0x8a30e9d4fab33af)
 br Branch_0x99

 Branch_0x98:

 ldc.r8 float64(0x9dfe0f931ff9b2d0)
 Branch_0x99:

 stelem.r8
End_Orphan_3d:
 ldc.i4.1
 ldc.i4.6
 ble Branch_0x9e

 ldarg Arg_0x2
 br Branch_0x9f

 Branch_0x9e:

 ldarg Arg_0x4
 Branch_0x9f:

Start_Orphan_40:
 nop
End_Orphan_40:
 ldarg Arg_0x6
 conv.ovf.i4
 beq Branch_0x9c

Start_Orphan_41:
 nop
End_Orphan_41:
 ldarg Arg_0x3
 ldarg Arg_0x3
 bge.un Branch_0xa0

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0xa1

 Branch_0xa0:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0xa1:

 br Branch_0x9d

 Branch_0x9c:

Start_Orphan_42:
 nop
End_Orphan_42:
 ldc.i4.1
 brfalse Branch_0xa2

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0xa3

 Branch_0xa2:

 ldc.r4 float32(0x2ebf491e)
 Branch_0xa3:

 Branch_0x9d:

Start_Orphan_43:
 ldloc LOCAL_0x1
 pop
End_Orphan_43:
Start_Orphan_44:
 nop
End_Orphan_44:
 ldc.i4.8
 conv.i1
 ldarg Arg_0x3
 conv.i4
 bne.un Branch_0xa4

Start_Orphan_45:
 nop
End_Orphan_45:
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0xa5

 Branch_0xa4:

Start_Orphan_46:
 nop
End_Orphan_46:
 ldc.i4.1
 ldc.i4.3
 bge.un Branch_0xa6

 ldc.r4 float32(0x7820f974)
 br Branch_0xa7

 Branch_0xa6:

 ldarg Arg_0x5
 Branch_0xa7:

 Branch_0xa5:

 ble Branch_0x9a

Start_Orphan_47:
 nop
End_Orphan_47:
 ldarg Arg_0x3
 ldarg Arg_0x3
 ble Branch_0xaa

 ldarg Arg_0x3
 br Branch_0xab

 Branch_0xaa:

 ldarg Arg_0x3
 Branch_0xab:

Start_Orphan_48:
 nop
End_Orphan_48:
 ldc.i4.7
 not
 bgt Branch_0xa8

Start_Orphan_49:
 nop
End_Orphan_49:
 ldarg Arg_0x2
 ldarg Arg_0x2
 ble.un Branch_0xac

 ldarg Arg_0x3
 br Branch_0xad

 Branch_0xac:

 ldarg Arg_0x3
 Branch_0xad:

 br Branch_0xa9

 Branch_0xa8:

 ldc.i4.1
 ldarg Arg_0x3
 add
 Branch_0xa9:

 br Branch_0x9b

 Branch_0x9a:

 ldarg Arg_0x3
 ldc.i4.6
 mul.ovf
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 ldc.i4.3
 and
 bge Branch_0xae

Start_Orphan_4a:
 nop
End_Orphan_4a:
 ldc.i4.s -35
 ldarg Arg_0x3
 bge.un Branch_0xb0

 ldarg Arg_0x3
 br Branch_0xb1

 Branch_0xb0:

 ldarg Arg_0x3
 Branch_0xb1:

 br Branch_0xaf

 Branch_0xae:

Start_Orphan_4b:
 nop
End_Orphan_4b:
 ldarg Arg_0x3
 conv.ovf.i
 Branch_0xaf:

 Branch_0x9b:

 not
Start_Orphan_4c:
Start_Orphan_4d:
 nop
End_Orphan_4d:
 ldc.i4.s -5
 ldarg Arg_0x3
 bge.un Branch_0xb2

 ldc.r8 float64(0x733e99b33b28b1b7)
 br Branch_0xb3

 Branch_0xb2:

 ldc.r4 float32(0xf308b0c1)
 Branch_0xb3:

Start_Orphan_4e:
 nop
End_Orphan_4e:
 ldarg Arg_0x3
 conv.r8
 cgt.un
 stsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
End_Orphan_4c:
Start_Orphan_4f:
 ldarg Arg_0x3
 conv.ovf.u8
Start_Orphan_50:
 nop
End_Orphan_50:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldc.i8 0x24969e15b3660ca
 add.ovf.un
 bge.un Branch_0xb6

 ldloc LOCAL_0x4
 starg Arg_0x5
 br Branch_0xb7

 Branch_0xb6:

 ldarg Arg_0x3
 ldarg Arg_0x3
 blt.un Branch_0xb8

 nop
 br Branch_0xb9

 Branch_0xb8:

 nop
 Branch_0xb9:

 Branch_0xb7:

End_Orphan_4f:
Start_Orphan_51:
 ldarg Arg_0x3
 brtrue Branch_0xbc

 nop
 br Branch_0xbd

 Branch_0xbc:

 nop
 Branch_0xbd:

End_Orphan_51:
Start_Orphan_52:
 nop
End_Orphan_52:
 ldloc LOCAL_0x4
 ldc.r4 float32(0x3bfb9deb)
 blt Branch_0xbe

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0xbf

 Branch_0xbe:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0xbf:

 conv.u
Start_Orphan_53:
 ldarg Arg_0x3
 ldarg Arg_0x3
 ble Branch_0xc0

 nop
 br Branch_0xc1

 Branch_0xc0:

 nop
 Branch_0xc1:

End_Orphan_53:
Start_Orphan_54:
 nop
End_Orphan_54:
 ldarg Arg_0x3
 conv.i
 ldarg Arg_0x6
 ldarg Arg_0x6
 bge Branch_0xc4

 ldarg Arg_0x3
 br Branch_0xc5

 Branch_0xc4:

 ldarg Arg_0x3
 Branch_0xc5:

 bge Branch_0xc2

 ldarg Arg_0x3
 ldarg Arg_0x3
 bgt.un Branch_0xc6

 ldc.i4.8
 br Branch_0xc7

 Branch_0xc6:

 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 Branch_0xc7:

 br Branch_0xc3

 Branch_0xc2:

Start_Orphan_55:
 nop
End_Orphan_55:
 ldc.i4.2
 ldarg Arg_0x3
 cgt
 Branch_0xc3:

 bgt Branch_0xba

Start_Orphan_56:
 nop
End_Orphan_56:
 ldc.i4.m1
 ldc.i4.s -108
 bgt Branch_0xca

 ldarg Arg_0x4
 br Branch_0xcb

 Branch_0xca:

 ldc.i4 0x8c73b066
 Branch_0xcb:

 ldarg Arg_0x6
 conv.ovf.u1
 bgt.un Branch_0xc8

Start_Orphan_57:
 nop
End_Orphan_57:
 ldarg Arg_0x3
 conv.i4
 br Branch_0xc9

 Branch_0xc8:

 ldc.i4.3
 ldc.i4.4
 bge Branch_0xcc

 ldc.i4.5
 br Branch_0xcd

 Branch_0xcc:

 ldc.i4.s -36
 Branch_0xcd:

 Branch_0xc9:

 br Branch_0xbb

 Branch_0xba:

Start_Orphan_58:
 nop
End_Orphan_58:
 ldarg Arg_0x3
 ldc.i4.7
 mul.ovf
 ldarg Arg_0x3
 conv.ovf.u1
 clt
 Branch_0xbb:

Start_Orphan_59:
Start_Orphan_5a:
 nop
End_Orphan_5a:
 ldarg Arg_0x3
 ldc.i4.s -6
 blt.un Branch_0xce

 ldc.r8 float64(0x200ab76060bac0e5)
 br Branch_0xcf

 Branch_0xce:

 ldc.r4 float32(0x23b2e37b)
 Branch_0xcf:

 pop
End_Orphan_59:
Start_Orphan_5b:
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 ldc.i4.8
 blt Branch_0xd0

 nop
 br Branch_0xd1

 Branch_0xd0:

 nop
 Branch_0xd1:

End_Orphan_5b:
 ldarg Arg_0x5
 conv.ovf.u8
 conv.ovf.i1.un
 not
 bgt Branch_0xb4

Start_Orphan_5c:
Start_Orphan_5d:
 nop
End_Orphan_5d:
 ldloc LOCAL_0x1
Start_Orphan_5e:
 nop
End_Orphan_5e:
 ldarg Arg_0x7
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 bgt Branch_0xd2

 ldc.i4.8
 br Branch_0xd3

 Branch_0xd2:

 ldc.i4.4
 Branch_0xd3:

 ldc.i4.4
 conv.u2
 stelem.i1
End_Orphan_5c:
Start_Orphan_5f:
 ldloca LOCAL_0x4
 pop
End_Orphan_5f:
 ldarg Arg_0x5
 ldc.r4 float32(0xbfba241d)
 blt.un Branch_0xd8

 ldarg Arg_0x3
 br Branch_0xd9

 Branch_0xd8:

 ldarg Arg_0x3
 Branch_0xd9:

Start_Orphan_60:
 nop
End_Orphan_60:
 ldarg Arg_0x3
 conv.ovf.u1
 blt Branch_0xd6

Start_Orphan_61:
 nop
End_Orphan_61:
 ldarg Arg_0x3
 conv.i1
 br Branch_0xd7

 Branch_0xd6:

Start_Orphan_62:
 nop
End_Orphan_62:
 ldarg Arg_0x1
 ldarg Arg_0x4
 cgt.un
 Branch_0xd7:

Start_Orphan_63:
 nop
End_Orphan_63:
 ldarg Arg_0x3
 ldarg Arg_0x3
 sub
Start_Orphan_64:
 nop
End_Orphan_64:
 ldarg Arg_0x3
 ldarg Arg_0x2
 sub.ovf.un
 ceq
 blt Branch_0xd4

Start_Orphan_65:
 ldarga Arg_0x4
 ldarg Arg_0x0
 stind.i2
End_Orphan_65:
Start_Orphan_66:
 nop
End_Orphan_66:
 ldarg Arg_0x3
 conv.ovf.i2
 ldc.i4.6
 ldarg Arg_0x3
 ble.un Branch_0xdc

 ldc.i4.5
 br Branch_0xdd

 Branch_0xdc:

 ldc.i4.s 114
 Branch_0xdd:

 bge.un Branch_0xda

 ldarg Arg_0x3
 ldc.i4.3
 ble Branch_0xde

 ldarg Arg_0x3
 br Branch_0xdf

 Branch_0xde:

 ldarg Arg_0x3
 Branch_0xdf:

 br Branch_0xdb

 Branch_0xda:

 ldarg Arg_0x3
 conv.ovf.i
 Branch_0xdb:

 br Branch_0xd5

 Branch_0xd4:

Start_Orphan_67:
 nop
End_Orphan_67:
 ldc.i4.m1
 ldarg Arg_0x3
 ble Branch_0xe0

 ldarg Arg_0x3
 br Branch_0xe1

 Branch_0xe0:

 ldarg Arg_0x3
 Branch_0xe1:

Start_Orphan_68:
 nop
End_Orphan_68:
 ldarg Arg_0x3
 conv.u
 sub.ovf
 Branch_0xd5:

 br Branch_0xb5

 Branch_0xb4:

Start_Orphan_69:
 ldarg Arg_0x3
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 blt Branch_0xe4

 ldc.i4.3
 br Branch_0xe5

 Branch_0xe4:

 ldc.i4.0
 Branch_0xe5:

 ldarg Arg_0x2
 brtrue Branch_0xe6

 ldarg Arg_0x3
 br Branch_0xe7

 Branch_0xe6:

 ldarg Arg_0x3
 Branch_0xe7:

 blt Branch_0xe2

 ldc.r8 float64(0x84182d578ea78380)
 pop
 br Branch_0xe3

 Branch_0xe2:

 ldarg Arg_0x6
 stsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 Branch_0xe3:

End_Orphan_69:
Start_Orphan_6a:
 ldarg Arg_0x3
 brtrue Branch_0xea

 nop
 br Branch_0xeb

 Branch_0xea:

 nop
 Branch_0xeb:

End_Orphan_6a:
Start_Orphan_6b:
 nop
End_Orphan_6b:
 ldarg Arg_0x2
 ldc.i4.0
 beq Branch_0xee

 ldarg Arg_0x3
 br Branch_0xef

 Branch_0xee:

 ldarg Arg_0x3
 Branch_0xef:

Start_Orphan_6c:
 nop
End_Orphan_6c:
 ldarg Arg_0x6
 ldarg Arg_0x6
 blt.un Branch_0xf0

 ldarg Arg_0x3
 br Branch_0xf1

 Branch_0xf0:

 ldarg Arg_0x3
 Branch_0xf1:

 beq Branch_0xec

Start_Orphan_6d:
 nop
End_Orphan_6d:
 ldloc LOCAL_0x4
 ldarg Arg_0x5
 ble Branch_0xf2

 ldarg Arg_0x7
 br Branch_0xf3

 Branch_0xf2:

 ldarg Arg_0x7
 Branch_0xf3:

 br Branch_0xed

 Branch_0xec:

 ldc.i4 0x621b35a8
 conv.ovf.i8.un
 Branch_0xed:

 ldc.i4.0
 ldarg Arg_0x3
 bge Branch_0xf6

 ldarg Arg_0x3
 br Branch_0xf7

 Branch_0xf6:

 ldarg Arg_0x3
 Branch_0xf7:

 brtrue Branch_0xf4

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 conv.u8
 br Branch_0xf5

 Branch_0xf4:

 ldarg Arg_0x3
 ldarg Arg_0x3
 bge.un Branch_0xf8

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 br Branch_0xf9

 Branch_0xf8:

 ldarg Arg_0x6
 Branch_0xf9:

 Branch_0xf5:

 blt.un Branch_0xe8

Start_Orphan_6e:
 ldsflda unsigned int16 ILGEN_0x372a9ae6::field_0x5
 ldc.i4.8
 stind.i2
End_Orphan_6e:
 ldc.i4.s -5
 conv.ovf.u1
 ldarg Arg_0x1
 ldc.i4.4
 xor
 ble.un Branch_0xfa

Start_Orphan_6f:
 nop
End_Orphan_6f:
 ldarg Arg_0x3
 conv.u
 br Branch_0xfb

 Branch_0xfa:

 ldc.i4.0
 conv.ovf.i
 Branch_0xfb:

 br Branch_0xe9

 Branch_0xe8:

Start_Orphan_70:
 ldarga Arg_0x1
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 stind.i1
End_Orphan_70:
 ldc.i4.m1
 conv.i
 Branch_0xe9:

 Branch_0xb5:

 blt Branch_0x92

Start_Orphan_71:
Start_Orphan_72:
 ldarga Arg_0x4
 ldc.i4.3
 stind.i2
End_Orphan_72:
 ldloca LOCAL_0x4
 pop
End_Orphan_71:
Start_Orphan_73:
 nop
End_Orphan_73:
 ldloc LOCAL_0x4
 ldc.r8 float64(0xe2aaccd8a269846e)
 ble.un Branch_0x100

 ldarg Arg_0x5
 br Branch_0x101

 Branch_0x100:

 ldarg Arg_0x5
 Branch_0x101:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ble.un Branch_0x102

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x103

 Branch_0x102:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x103:

 ble Branch_0xfe

 ldarg Arg_0x3
 ldc.i4.0
 sub.ovf.un
 br Branch_0xff

 Branch_0xfe:

Start_Orphan_74:
 nop
End_Orphan_74:
 ldarg Arg_0x3
 ldarg Arg_0x3
 bne.un Branch_0x104

 ldarg Arg_0x3
 br Branch_0x105

 Branch_0x104:

 ldarg Arg_0x3
 Branch_0x105:

 Branch_0xff:

 ldarg Arg_0x2
 conv.ovf.i2
 div.un
Start_Orphan_75:
 nop
End_Orphan_75:
 ldarg Arg_0x3
 ldc.i4.2
 sub.ovf.un
 ldarg Arg_0x3
 ldarg Arg_0x3
 mul
 xor
 conv.ovf.i.un
 bgt.un Branch_0xfc

Start_Orphan_76:
Start_Orphan_77:
 nop
End_Orphan_77:
 ldarg Arg_0x2
 ldc.i4.1
 blt.un Branch_0x108

 ldarg Arg_0x3
 br Branch_0x109

 Branch_0x108:

 ldarg Arg_0x3
 Branch_0x109:

Start_Orphan_78:
 nop
End_Orphan_78:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 conv.ovf.i
 blt.un Branch_0x106

 ldarg Arg_0x3
 ldc.i4.0
 beq Branch_0x10a

 nop
 br Branch_0x10b

 Branch_0x10a:

 nop
 Branch_0x10b:

 br Branch_0x107

 Branch_0x106:

 ldarg Arg_0x6
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 bge.un Branch_0x10c

 nop
 br Branch_0x10d

 Branch_0x10c:

 nop
 Branch_0x10d:

 Branch_0x107:

End_Orphan_76:
 ldc.i4.3
 ldarg Arg_0x4
 add
 ldarg Arg_0x3
 ldc.i4.7
 bgt.un Branch_0x110

 ldc.i4.5
 br Branch_0x111

 Branch_0x110:

 ldc.i4.5
 Branch_0x111:

 bne.un Branch_0x10e

Start_Orphan_79:
 nop
End_Orphan_79:
 ldc.r4 float32(0xde565f38)
 conv.ovf.i
 br Branch_0x10f

 Branch_0x10e:

 ldarg Arg_0x3
 ldarg Arg_0x3
 bgt Branch_0x112

 ldarg Arg_0x3
 br Branch_0x113

 Branch_0x112:

 ldarg Arg_0x3
 Branch_0x113:

 Branch_0x10f:

Start_Orphan_7a:
 ldarg Arg_0x7
 starg Arg_0x6
End_Orphan_7a:
Start_Orphan_7b:
 nop
End_Orphan_7b:
 ldc.i4.6
 ldarg Arg_0x3
 sub
Start_Orphan_7c:
 nop
End_Orphan_7c:
 ldc.i4.3
 blt.un Branch_0x114

 ldc.i8 0x58f2705f5abfcb8
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 bgt Branch_0x116

 ldarg Arg_0x3
 br Branch_0x117

 Branch_0x116:

 ldarg Arg_0x3
 Branch_0x117:

 br Branch_0x115

 Branch_0x114:

Start_Orphan_7d:
 nop
End_Orphan_7d:
 ldarg Arg_0x3
 conv.u
 Branch_0x115:

 sub.ovf.un
 br Branch_0xfd

 Branch_0xfc:

Start_Orphan_7e:
 ldc.r4 float32(0xe92db06d)
 call float64 [mscorlib]System.Math::Round(float64)
 stsfld float64 ILGEN_0x372a9ae6::field_0x2
End_Orphan_7e:
Start_Orphan_7f:
 ldc.i4.6
 ldarg Arg_0x3
 beq Branch_0x118

 nop
 br Branch_0x119

 Branch_0x118:

 nop
 Branch_0x119:

End_Orphan_7f:
 ldc.i4.m1
 ldc.i4.4
 ldc.i4.0
 bgt Branch_0x11c

 ldc.i4.7
 br Branch_0x11d

 Branch_0x11c:

 ldc.i4.s -51
 Branch_0x11d:

 bgt.un Branch_0x11a

 ldc.i8 0x250224c649021146
 ldarg Arg_0x7
 bgt.un Branch_0x11e

 ldarg Arg_0x3
 br Branch_0x11f

 Branch_0x11e:

 ldarg Arg_0x3
 Branch_0x11f:

 br Branch_0x11b

 Branch_0x11a:

Start_Orphan_80:
 nop
End_Orphan_80:
 ldarg Arg_0x1
 ldarg Arg_0x3
 bge Branch_0x120

 ldarg Arg_0x3
 br Branch_0x121

 Branch_0x120:

 ldarg Arg_0x3
 Branch_0x121:

 Branch_0x11b:

Start_Orphan_81:
 ldarg Arg_0x3
 ldarg Arg_0x3
 ble Branch_0x122

 nop
 br Branch_0x123

 Branch_0x122:

 nop
 Branch_0x123:

End_Orphan_81:
 ldarg Arg_0x3
 ldc.i4.8
 cgt
Start_Orphan_82:
 nop
End_Orphan_82:
 ldarg Arg_0x3
 ldc.i4.0
 add
 sub
 rem.un
 Branch_0xfd:

 br Branch_0x93

 Branch_0x92:

Start_Orphan_83:
Start_Orphan_84:
 ldarga Arg_0x6
 pop
End_Orphan_84:
Start_Orphan_85:
 nop
End_Orphan_85:
 ldarg Arg_0x3
 ldc.i4.8
 ceq
 conv.r8
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 call float64 [mscorlib]System.Math::Sqrt(float64)
 ldloc LOCAL_0x4
 conv.r4
 div 
 beq Branch_0x124

 ldloc LOCAL_0x3
 pop
 br Branch_0x125

 Branch_0x124:

Start_Orphan_86:
 nop
End_Orphan_86:
 ldarg Arg_0x3
 conv.ovf.u4
Start_Orphan_87:
 nop
End_Orphan_87:
 ldarg Arg_0x3
 ldarg Arg_0x3
 sub
 beq Branch_0x126

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 starg Arg_0x6
 br Branch_0x127

 Branch_0x126:

 ldsflda unsigned int16 ILGEN_0x372a9ae6::field_0x5
 pop
 Branch_0x127:

 Branch_0x125:

End_Orphan_83:
Start_Orphan_88:
Start_Orphan_89:
 nop
End_Orphan_89:
 ldc.i4 0x510a8e90
 ldarg Arg_0x3
 bgt Branch_0x12a

 ldarg Arg_0x3
 br Branch_0x12b

 Branch_0x12a:

 ldarg Arg_0x3
 Branch_0x12b:

Start_Orphan_8a:
 nop
End_Orphan_8a:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 conv.i
 ble.un Branch_0x128

 ldarga Arg_0x2
 ldc.i4.s 4
 stind.i
 br Branch_0x129

 Branch_0x128:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ldloc LOCAL_0x4
 bge.un Branch_0x12c

 nop
 br Branch_0x12d

 Branch_0x12c:

 nop
 Branch_0x12d:

 Branch_0x129:

End_Orphan_88:
 ldarg Arg_0x4
 ldc.i4.6
 blt.un Branch_0x12e

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x12f

 Branch_0x12e:

 ldc.r4 float32(0x801a192a)
 Branch_0x12f:

 conv.ovf.i4
 ldloc LOCAL_0x1
 ldc.i4.8
 ldelema [mscorlib]System.SByte
 ldind.i1
 or
 conv.ovf.i.un
 Branch_0x93:

 not
 conv.ovf.i8.un
 stsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
End_Orphan_0:
Start_Orphan_8b:
Start_Orphan_8c:
 nop
End_Orphan_8c:
 ldc.i4.s 20
 ldarg Arg_0x3
 div.un
 conv.ovf.u
Start_Orphan_8d:
 ldarg Arg_0x3
 starg Arg_0x3
End_Orphan_8d:
Start_Orphan_8e:
 nop
End_Orphan_8e:
 ldarg Arg_0x3
 ldarg Arg_0x4
 blt.un Branch_0x138

 ldc.r8 float64(0x26d98fde28af74c8)
 br Branch_0x139

 Branch_0x138:

 ldarg Arg_0x5
 Branch_0x139:

 conv.ovf.i2
 add.ovf
Start_Orphan_8f:
 ldarg Arg_0x6
 ldarg Arg_0x6
 ble Branch_0x13a

 nop
 br Branch_0x13b

 Branch_0x13a:

 nop
 Branch_0x13b:

End_Orphan_8f:
Start_Orphan_90:
 nop
End_Orphan_90:
 ldarg Arg_0x3
 ldc.i4.m1
 bne.un Branch_0x13e

 ldc.i8 0xb7a9b813d5727369
 br Branch_0x13f

 Branch_0x13e:

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 Branch_0x13f:

Start_Orphan_91:
 nop
End_Orphan_91:
 ldc.i4.1
 ldarg Arg_0x4
 bgt Branch_0x140

 ldc.i8 0xda789bba2a99c25f
 br Branch_0x141

 Branch_0x140:

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 Branch_0x141:

 bge.un Branch_0x13c

 ldc.i4.m1
 ldarg Arg_0x3
 ble Branch_0x142

 ldloc LOCAL_0x4
 br Branch_0x143

 Branch_0x142:

 ldarg Arg_0x5
 Branch_0x143:

 br Branch_0x13d

 Branch_0x13c:

 ldarg Arg_0x4
 ldarg Arg_0x2
 bne.un Branch_0x144

 ldc.r4 float32(0x7fcccfda)
 br Branch_0x145

 Branch_0x144:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x145:

 Branch_0x13d:

 conv.i1
 bgt.un Branch_0x136

Start_Orphan_92:
 ldc.i8 0xda815b7cd14173ab
 conv.u
 brtrue Branch_0x146

 ldc.i4.5
 ldarg Arg_0x3
 ble Branch_0x148

 nop
 br Branch_0x149

 Branch_0x148:

 nop
 Branch_0x149:

 br Branch_0x147

 Branch_0x146:

 ldarg Arg_0x6
 ldarg Arg_0x7
 ble.un Branch_0x14a

 nop
 br Branch_0x14b

 Branch_0x14a:

 nop
 Branch_0x14b:

 Branch_0x147:

End_Orphan_92:
Start_Orphan_93:
 ldarg Arg_0x6
 ldarg Arg_0x7
 beq Branch_0x14e

 nop
 br Branch_0x14f

 Branch_0x14e:

 nop
 Branch_0x14f:

End_Orphan_93:
 ldarg Arg_0x3
 brtrue Branch_0x152

 ldc.i4.4
 br Branch_0x153

 Branch_0x152:

 ldc.i4.5
 Branch_0x153:

 brtrue Branch_0x150

Start_Orphan_94:
 nop
End_Orphan_94:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 bne.un Branch_0x154

 ldc.r8 float64(0x2b51d191942afb06)
 br Branch_0x155

 Branch_0x154:

 ldc.r4 float32(0x61266855)
 Branch_0x155:

 br Branch_0x151

 Branch_0x150:

 ldarg Arg_0x3
 ldarg Arg_0x3
 bne.un Branch_0x156

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x157

 Branch_0x156:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x157:

 Branch_0x151:

Start_Orphan_95:
 ldsflda float64 ILGEN_0x372a9ae6::field_0x2
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 stind.r8
End_Orphan_95:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldc.i8 0xac4eef466e9ba7d7
 bgt Branch_0x15a

 ldc.i4.8
 br Branch_0x15b

 Branch_0x15a:

 ldc.i4.8
 Branch_0x15b:

 ldc.i4.8
 conv.ovf.i4
 bne.un Branch_0x158

Start_Orphan_96:
 nop
End_Orphan_96:
 ldc.i4.3
 ldarg Arg_0x3
 blt Branch_0x15c

 ldarg Arg_0x5
 br Branch_0x15d

 Branch_0x15c:

 ldarg Arg_0x5
 Branch_0x15d:

 br Branch_0x159

 Branch_0x158:

 ldarg Arg_0x2
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 blt Branch_0x15e

 ldarg Arg_0x5
 br Branch_0x15f

 Branch_0x15e:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x15f:

 Branch_0x159:

 bge.un Branch_0x14c

 ldc.i4.2
 conv.i
Start_Orphan_97:
 nop
End_Orphan_97:
 ldarg Arg_0x2
 ldarg Arg_0x2
 bgt.un Branch_0x160

 ldc.i4.7
 br Branch_0x161

 Branch_0x160:

 ldarg Arg_0x4
 Branch_0x161:

 add
 br Branch_0x14d

 Branch_0x14c:

Start_Orphan_98:
 ldarg Arg_0x3
 ldc.i4.1
 blt.un Branch_0x162

 nop
 br Branch_0x163

 Branch_0x162:

 nop
 Branch_0x163:

End_Orphan_98:
 ldarg Arg_0x7
 conv.u4
 ldc.i4.6
 ldarg Arg_0x4
 or
 blt Branch_0x164

 ldarg Arg_0x3
 conv.u
 br Branch_0x165

 Branch_0x164:

Start_Orphan_99:
 nop
End_Orphan_99:
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 ldarg Arg_0x3
 mul.ovf
 Branch_0x165:

 Branch_0x14d:

 br Branch_0x137

 Branch_0x136:

Start_Orphan_9a:
Start_Orphan_9b:
 nop
End_Orphan_9b:
 ldc.i4.3
 ldc.i4.4
 beq Branch_0x168

 ldc.i4.8
 br Branch_0x169

 Branch_0x168:

 ldc.i4.3
 Branch_0x169:

Start_Orphan_9c:
 nop
End_Orphan_9c:
 ldc.i4.2
 ldarg Arg_0x3
 bgt.un Branch_0x16a

 ldarg Arg_0x2
 br Branch_0x16b

 Branch_0x16a:

 ldc.i4 0xf75c8c66
 Branch_0x16b:

 ble.un Branch_0x166

 ldc.i4.m1
 ldarg Arg_0x3
 blt Branch_0x16c

 nop
 br Branch_0x16d

 Branch_0x16c:

 nop
 Branch_0x16d:

 br Branch_0x167

 Branch_0x166:

 ldc.i4.7
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 bgt Branch_0x16e

 nop
 br Branch_0x16f

 Branch_0x16e:

 nop
 Branch_0x16f:

 Branch_0x167:

End_Orphan_9a:
Start_Orphan_9d:
 ldarg Arg_0x3
 ldarg Arg_0x3
 blt.un Branch_0x170

 nop
 br Branch_0x171

 Branch_0x170:

 nop
 Branch_0x171:

End_Orphan_9d:
Start_Orphan_9e:
 nop
End_Orphan_9e:
 ldarg Arg_0x6
 ldc.i4.8
 shr
Start_Orphan_9f:
 nop
End_Orphan_9f:
 ldc.i4.1
 ldc.i4.8
 blt.un Branch_0x174

 ldarg Arg_0x7
 br Branch_0x175

 Branch_0x174:

 ldc.i8 0x7d179f8befafe6c9
 Branch_0x175:

 bge.un Branch_0x172

Start_Orphan_a0:
 nop
End_Orphan_a0:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 br Branch_0x173

 Branch_0x172:

 ldarg Arg_0x5
 ldc.r4 float32(0x54477edc)
 bge Branch_0x176

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 br Branch_0x177

 Branch_0x176:

 ldarg Arg_0x6
 Branch_0x177:

 Branch_0x173:

 conv.ovf.i.un
 Branch_0x137:

Start_Orphan_a1:
Start_Orphan_a2:
 nop
End_Orphan_a2:
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 conv.ovf.i4
Start_Orphan_a3:
 nop
End_Orphan_a3:
 ldarg Arg_0x3
 ldarg Arg_0x3
 ble Branch_0x17c

 ldarg Arg_0x3
 br Branch_0x17d

 Branch_0x17c:

 ldarg Arg_0x3
 Branch_0x17d:

 bgt.un Branch_0x17a

 ldc.i4.m1
 ldarg Arg_0x3
 blt.un Branch_0x17e

 ldc.i8 0xb4dffbd6f5dff244
 br Branch_0x17f

 Branch_0x17e:

 ldarg Arg_0x6
 Branch_0x17f:

 br Branch_0x17b

 Branch_0x17a:

Start_Orphan_a4:
 nop
End_Orphan_a4:
 ldarg Arg_0x3
 ldarg Arg_0x3
 blt Branch_0x180

 ldc.i8 0x3d51014b25a482ca
 br Branch_0x181

 Branch_0x180:

 ldarg Arg_0x7
 Branch_0x181:

 Branch_0x17b:

 ldc.i4.2
 ldarg Arg_0x3
 bne.un Branch_0x182

 ldarg Arg_0x6
 br Branch_0x183

 Branch_0x182:

 ldc.i8 0x7e41eb3a4a5a8cf2
 Branch_0x183:

 ldarg Arg_0x3
 ldarg Arg_0x3
 cgt
 shl
 bge Branch_0x178

 ldarga Arg_0x0
 pop
 br Branch_0x179

 Branch_0x178:

Start_Orphan_a5:
 nop
End_Orphan_a5:
 ldarg Arg_0x3
 ldc.i4.2
 blt.un Branch_0x186

 ldarg Arg_0x3
 br Branch_0x187

 Branch_0x186:

 ldarg Arg_0x3
 Branch_0x187:

 brfalse Branch_0x184

 ldc.r4 float32(0x52e26ac7)
 ldc.r4 float32(0x52b03578)
 bge Branch_0x188

 nop
 br Branch_0x189

 Branch_0x188:

 nop
 Branch_0x189:

 br Branch_0x185

 Branch_0x184:

 ldc.i4.7
 brtrue Branch_0x18a

 nop
 br Branch_0x18b

 Branch_0x18a:

 nop
 Branch_0x18b:

 Branch_0x185:

 Branch_0x179:

End_Orphan_a1:
Start_Orphan_a6:
 ldc.r8 float64(0x2e2904dcb47bb3a8)
 stloc LOCAL_0x4
End_Orphan_a6:
Start_Orphan_a7:
 nop
End_Orphan_a7:
 ldarg Arg_0x5
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ble.un Branch_0x18c

 ldc.i8 0x32824015a57815bb
 br Branch_0x18d

 Branch_0x18c:

 ldc.i8 0x546a4759d79d55f
 Branch_0x18d:

 conv.i2
Start_Orphan_a8:
 nop
End_Orphan_a8:
 ldsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
 ldlen
Start_Orphan_a9:
 nop
End_Orphan_a9:
 ldarg Arg_0x2
 ldarg Arg_0x3
 ble.un Branch_0x18e

 ldc.i4.1
 br Branch_0x18f

 Branch_0x18e:

 ldc.i4.3
 Branch_0x18f:

 add.ovf.un
 clt.un
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
Start_Orphan_aa:
 nop
End_Orphan_aa:
 ldarg Arg_0x3
 ldarg Arg_0x3
 bne.un Branch_0x192

 ldarg Arg_0x3
 br Branch_0x193

 Branch_0x192:

 ldarg Arg_0x3
 Branch_0x193:

 bge.un Branch_0x190

Start_Orphan_ab:
 nop
End_Orphan_ab:
 ldc.r4 float32(0xc26fcb00)
 ldloc LOCAL_0x4
 bgt.un Branch_0x194

 ldloc LOCAL_0x4
 br Branch_0x195

 Branch_0x194:

 ldloc LOCAL_0x4
 Branch_0x195:

 br Branch_0x191

 Branch_0x190:

 ldloca LOCAL_0x4
 ldind.r4
 Branch_0x191:

 ldarg Arg_0x3
 ldarg Arg_0x3
 xor
Start_Orphan_ac:
 nop
End_Orphan_ac:
 ldc.i8 0x2b5cf14d806a9366
 ldc.i8 0xbfd54c4dc550b8c4
 bgt Branch_0x198

 ldarg Arg_0x3
 br Branch_0x199

 Branch_0x198:

 ldarg Arg_0x3
 Branch_0x199:

 ble Branch_0x196

 ldc.r8 float64(0xd55b1e98830e307c)
 ldarg Arg_0x5
 bge.un Branch_0x19a

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x19b

 Branch_0x19a:

 ldloc LOCAL_0x4
 Branch_0x19b:

 br Branch_0x197

 Branch_0x196:

Start_Orphan_ad:
 nop
End_Orphan_ad:
 ldc.r8 float64(0x1573b3574951c62)
 ldarg Arg_0x5
 add 
 Branch_0x197:

 clt.un
 shl
 ble Branch_0x134

Start_Orphan_ae:
 ldsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
 stsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
End_Orphan_ae:
Start_Orphan_af:
 ldc.i4.4
 ldarg Arg_0x3
 blt Branch_0x19e

 nop
 br Branch_0x19f

 Branch_0x19e:

 nop
 Branch_0x19f:

End_Orphan_af:
Start_Orphan_b0:
 nop
End_Orphan_b0:
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 bgt Branch_0x1a0

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x1a1

 Branch_0x1a0:

 ldloc LOCAL_0x4
 Branch_0x1a1:

 conv.ovf.u2
 ldarg Arg_0x5
 conv.i2
Start_Orphan_b1:
 nop
End_Orphan_b1:
 ldc.i4.2
 conv.ovf.u4
 blt.un Branch_0x1a2

 ldarg Arg_0x3
 conv.i
 br Branch_0x1a3

 Branch_0x1a2:

 ldarg Arg_0x5
 ldarg Arg_0x5
 bne.un Branch_0x1a4

 ldarg Arg_0x3
 br Branch_0x1a5

 Branch_0x1a4:

 ldarg Arg_0x3
 Branch_0x1a5:

 Branch_0x1a3:

 mul
Start_Orphan_b2:
 ldarg Arg_0x2
 ldarg Arg_0x4
 beq Branch_0x1a6

 ldarg Arg_0x5
 br Branch_0x1a7

 Branch_0x1a6:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x1a7:

 stsfld float64 ILGEN_0x372a9ae6::field_0x2
End_Orphan_b2:
Start_Orphan_b3:
 ldarg Arg_0x4
 ldc.i4.5
 bgt Branch_0x1a8

 nop
 br Branch_0x1a9

 Branch_0x1a8:

 nop
 Branch_0x1a9:

End_Orphan_b3:
Start_Orphan_b4:
 nop
End_Orphan_b4:
 ldarg Arg_0x3
 ldarg Arg_0x3
 rem
 conv.ovf.i1.un
Start_Orphan_b5:
 nop
End_Orphan_b5:
 ldarg Arg_0x6
 ldc.i8 0x2c5713a6b36845eb
 bge Branch_0x1aa

 ldarg Arg_0x3
 br Branch_0x1ab

 Branch_0x1aa:

 ldarg Arg_0x3
 Branch_0x1ab:

 conv.ovf.i4.un
 and
 bge.un Branch_0x19c

 ldloc LOCAL_0x1
 ldc.i4.5
 ldelema [mscorlib]System.SByte
 ldind.i1
Start_Orphan_b6:
 ldarg Arg_0x6
 ldc.i8 0xa25efbf09cf47043
 ble Branch_0x1ae

 nop
 br Branch_0x1af

 Branch_0x1ae:

 nop
 Branch_0x1af:

End_Orphan_b6:
 ldarg Arg_0x7
 conv.i
 bge Branch_0x1ac

 ldarg Arg_0x3
 brfalse Branch_0x1b2

 ldarg Arg_0x3
 br Branch_0x1b3

 Branch_0x1b2:

 ldarg Arg_0x3
 Branch_0x1b3:

 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 bgt.un Branch_0x1b0

 ldarg Arg_0x5
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 bgt Branch_0x1b4

 nop
 br Branch_0x1b5

 Branch_0x1b4:

 nop
 Branch_0x1b5:

 br Branch_0x1b1

 Branch_0x1b0:

 ldarg Arg_0x3
 brtrue Branch_0x1b6

 nop
 br Branch_0x1b7

 Branch_0x1b6:

 nop
 Branch_0x1b7:

 Branch_0x1b1:

 br Branch_0x1ad

 Branch_0x1ac:

Start_Orphan_b7:
 nop
End_Orphan_b7:
 ldc.i4.2
 conv.r8
 pop
 Branch_0x1ad:

 br Branch_0x19d

 Branch_0x19c:

 ldsflda unsigned int16 ILGEN_0x372a9ae6::field_0x5
Start_Orphan_b8:
 ldc.i4.4
 ldc.i4.6
 bgt.un Branch_0x1b8

 nop
 br Branch_0x1b9

 Branch_0x1b8:

 nop
 Branch_0x1b9:

End_Orphan_b8:
Start_Orphan_b9:
 nop
End_Orphan_b9:
 ldarg Arg_0x3
 ldarg Arg_0x3
 blt Branch_0x1ba

 ldc.i4.1
 br Branch_0x1bb

 Branch_0x1ba:

 ldc.i4.2
 Branch_0x1bb:

 conv.i2
 stind.i2
 Branch_0x19d:

 br Branch_0x135

 Branch_0x134:

Start_Orphan_ba:
 ldc.i8 0xeefa1761b5e55a8d
 conv.u2
 ldarg Arg_0x3
 ldarg Arg_0x3
 bgt Branch_0x1c0

 ldarg Arg_0x0
 br Branch_0x1c1

 Branch_0x1c0:

 ldc.i4 0xca9746
 Branch_0x1c1:

 bge.un Branch_0x1be

 ldarg Arg_0x3
 ldarg Arg_0x3
 blt.un Branch_0x1c2

 nop
 br Branch_0x1c3

 Branch_0x1c2:

 nop
 Branch_0x1c3:

 br Branch_0x1bf

 Branch_0x1be:

 ldarg Arg_0x3
 ldc.i4.7
 ble.un Branch_0x1c4

 nop
 br Branch_0x1c5

 Branch_0x1c4:

 nop
 Branch_0x1c5:

 Branch_0x1bf:

End_Orphan_ba:
 ldarg Arg_0x3
 ldarg Arg_0x3
 div
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldc.i8 0x20b23cc794883c
 clt.un
 bne.un Branch_0x1c8

Start_Orphan_bb:
 nop
End_Orphan_bb:
 ldc.i4.4
 ldc.i4.4
 mul.ovf
 br Branch_0x1c9

 Branch_0x1c8:

Start_Orphan_bc:
 nop
End_Orphan_bc:
 ldc.i4.3
 ldarg Arg_0x3
 cgt
 Branch_0x1c9:

Start_Orphan_bd:
 nop
End_Orphan_bd:
 ldloc LOCAL_0x4
 ldarg Arg_0x5
 bge.un Branch_0x1ca

 ldarg Arg_0x3
 br Branch_0x1cb

 Branch_0x1ca:

 ldarg Arg_0x3
 Branch_0x1cb:

 conv.i
 blt.un Branch_0x1c6

Start_Orphan_be:
 ldarg Arg_0x3
 ldc.i4.7
 blt Branch_0x1cc

 nop
 br Branch_0x1cd

 Branch_0x1cc:

 nop
 Branch_0x1cd:

End_Orphan_be:
Start_Orphan_bf:
 nop
End_Orphan_bf:
 ldarga Arg_0x4
 ldind.u2
 conv.i8
 br Branch_0x1c7

 Branch_0x1c6:

 ldarg Arg_0x3
 ldarg Arg_0x3
 blt.un Branch_0x1d0

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x1d1

 Branch_0x1d0:

 ldc.r8 float64(0x8d3079bf175d8acb)
 Branch_0x1d1:

 ldarg Arg_0x3
 ldarg Arg_0x3
 ble.un Branch_0x1d2

 ldarg Arg_0x5
 br Branch_0x1d3

 Branch_0x1d2:

 ldarg Arg_0x5
 Branch_0x1d3:

 blt Branch_0x1ce

 ldarg Arg_0x3
 ldc.i4.5
 ble Branch_0x1d4

 ldarg Arg_0x6
 br Branch_0x1d5

 Branch_0x1d4:

 ldarg Arg_0x7
 Branch_0x1d5:

 br Branch_0x1cf

 Branch_0x1ce:

 ldc.i4.3
 ldarg Arg_0x3
 bne.un Branch_0x1d6

 ldc.i8 0x74a3ffe0f12bf0e1
 br Branch_0x1d7

 Branch_0x1d6:

 ldc.i8 0xf4c07d05955a7880
 Branch_0x1d7:

 Branch_0x1cf:

 Branch_0x1c7:

Start_Orphan_c0:
 ldarg Arg_0x3
 ldc.i4.8
 bge.un Branch_0x1da

 ldarg Arg_0x3
 br Branch_0x1db

 Branch_0x1da:

 ldarg Arg_0x3
 Branch_0x1db:

 ldarg Arg_0x7
 ldarg Arg_0x7
 ceq
 bgt Branch_0x1d8

 ldc.r8 float64(0xf8c10ae8e9004127)
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 blt Branch_0x1dc

 nop
 br Branch_0x1dd

 Branch_0x1dc:

 nop
 Branch_0x1dd:

 br Branch_0x1d9

 Branch_0x1d8:

 ldarg Arg_0x3
 brtrue Branch_0x1de

 nop
 br Branch_0x1df

 Branch_0x1de:

 nop
 Branch_0x1df:

 Branch_0x1d9:

End_Orphan_c0:
 ldarg Arg_0x3
 ldc.i4.8
 ble.un Branch_0x1e0

 ldc.i8 0x5d04f597a53565a1
 br Branch_0x1e1

 Branch_0x1e0:

 ldarg Arg_0x7
 Branch_0x1e1:

 conv.ovf.u8
 ldc.i4.4
 conv.u1
 newarr [mscorlib]System.UInt64
 ldc.i4.3
 ldarg Arg_0x3
 ble Branch_0x1e2

 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 br Branch_0x1e3

 Branch_0x1e2:

 ldc.i4.1
 Branch_0x1e3:

 ldelem.u8
 or
 bge Branch_0x1bc

Start_Orphan_c1:
 ldc.i4.s -76
 ldarg Arg_0x3
 blt.un Branch_0x1e4

 nop
 br Branch_0x1e5

 Branch_0x1e4:

 nop
 Branch_0x1e5:

End_Orphan_c1:
 ldarga Arg_0x5
Start_Orphan_c2:
 ldarg Arg_0x3
 ldarg Arg_0x3
 blt Branch_0x1e6

 nop
 br Branch_0x1e7

 Branch_0x1e6:

 nop
 Branch_0x1e7:

End_Orphan_c2:
Start_Orphan_c3:
 nop
End_Orphan_c3:
 ldarg Arg_0x0
 conv.r.un
 stind.r8
 br Branch_0x1bd

 Branch_0x1bc:

Start_Orphan_c4:
 nop
End_Orphan_c4:
 ldc.i4.8
 ldarg Arg_0x0
 clt.un
 ldc.i4.6
 conv.ovf.i2.un
 blt.un Branch_0x1ea

Start_Orphan_c5:
 nop
End_Orphan_c5:
 ldarg Arg_0x3
 ldarg Arg_0x3
 blt.un Branch_0x1ec

 ldc.r8 float64(0x96efbeabf7a0a8a9)
 br Branch_0x1ed

 Branch_0x1ec:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x1ed:

 br Branch_0x1eb

 Branch_0x1ea:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ldc.r8 float64(0xd2a3603df77d96c6)
 div 
 Branch_0x1eb:

Start_Orphan_c6:
 nop
End_Orphan_c6:
 ldarg Arg_0x3
 ldarg Arg_0x3
 or
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 conv.ovf.i4.un
 bne.un Branch_0x1ee

Start_Orphan_c7:
 nop
End_Orphan_c7:
 ldarg Arg_0x3
 ldarg Arg_0x1
 ble.un Branch_0x1f0

 ldc.r4 float32(0xb889c1d8)
 br Branch_0x1f1

 Branch_0x1f0:

 ldc.r4 float32(0x7199a964)
 Branch_0x1f1:

 br Branch_0x1ef

 Branch_0x1ee:

 ldarg Arg_0x3
 ldarg Arg_0x2
 ble Branch_0x1f2

 ldarg Arg_0x5
 br Branch_0x1f3

 Branch_0x1f2:

 ldc.r4 float32(0xe1059456)
 Branch_0x1f3:

 Branch_0x1ef:

 ble Branch_0x1e8

 ldarg Arg_0x1
 conv.u1
 newarr [mscorlib]System.Int32
 stsfld unsigned int32[] ILGEN_0x372a9ae6::field_0x1
 br Branch_0x1e9

 Branch_0x1e8:

 ldarga Arg_0x4
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldarg Arg_0x7
 blt Branch_0x1f4

 ldc.i4.m1
 br Branch_0x1f5

 Branch_0x1f4:

 ldc.i4.6
 Branch_0x1f5:

 stind.i2
 Branch_0x1e9:

 Branch_0x1bd:

 Branch_0x135:

End_Orphan_8b:
 ldloc LOCAL_0x4
Start_Orphan_c8:
 nop
End_Orphan_c8:
 ldc.i4.0
 ldarg Arg_0x3
 ble Branch_0x1fc

 ldc.i4.4
 br Branch_0x1fd

 Branch_0x1fc:

 ldc.i4.6
 Branch_0x1fd:

Start_Orphan_c9:
 nop
End_Orphan_c9:
 ldarg Arg_0x7
 ldarg Arg_0x7
 blt Branch_0x1fe

 ldarg Arg_0x3
 br Branch_0x1ff

 Branch_0x1fe:

 ldarg Arg_0x3
 Branch_0x1ff:

 ble Branch_0x1fa

Start_Orphan_ca:
 nop
End_Orphan_ca:
 ldarg Arg_0x5
 ldarg Arg_0x5
 bge Branch_0x200

 ldc.i8 0x43c8be60badf0f8c
 br Branch_0x201

 Branch_0x200:

 ldarg Arg_0x7
 Branch_0x201:

 br Branch_0x1fb

 Branch_0x1fa:

 ldc.i4.3
 brfalse Branch_0x202

 ldc.i8 0x31752fad812561d5
 br Branch_0x203

 Branch_0x202:

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 Branch_0x203:

 Branch_0x1fb:

 conv.i8
Start_Orphan_cb:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldc.i8 0x85f4f30eb5967988
 bge Branch_0x204

 nop
 br Branch_0x205

 Branch_0x204:

 nop
 Branch_0x205:

End_Orphan_cb:
 ldc.i4.4
Start_Orphan_cc:
 nop
End_Orphan_cc:
 ldc.i4.1
 conv.i4
 ble Branch_0x206

Start_Orphan_cd:
 nop
End_Orphan_cd:
 ldc.i8 0x1032f2fee8f49910
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 rem.un
 br Branch_0x207

 Branch_0x206:

Start_Orphan_ce:
 nop
End_Orphan_ce:
 ldarg Arg_0x3
 ldarg Arg_0x3
 blt.un Branch_0x208

 ldc.i8 0x6972d38bf5ab165a
 br Branch_0x209

 Branch_0x208:

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 Branch_0x209:

 Branch_0x207:

Start_Orphan_cf:
 nop
End_Orphan_cf:
 ldarga Arg_0x6
 ldind.i8
 rem
 bge.un Branch_0x1f8

Start_Orphan_d0:
Start_Orphan_d1:
 nop
End_Orphan_d1:
 ldc.i4.3
 ldc.i4.2
 ble.un Branch_0x20c

 ldarg Arg_0x3
 br Branch_0x20d

 Branch_0x20c:

 ldarg Arg_0x3
 Branch_0x20d:

 ldc.i4.8
 neg
 bgt Branch_0x20a

 ldarg Arg_0x7
 ldarg Arg_0x7
 blt Branch_0x20e

 nop
 br Branch_0x20f

 Branch_0x20e:

 nop
 Branch_0x20f:

 br Branch_0x20b

 Branch_0x20a:

 ldarg Arg_0x6
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 bgt Branch_0x210

 nop
 br Branch_0x211

 Branch_0x210:

 nop
 Branch_0x211:

 Branch_0x20b:

End_Orphan_d0:
 ldarg Arg_0x3
 ldarg Arg_0x3
 beq Branch_0x214

 ldc.r4 float32(0xecbeedd6)
 br Branch_0x215

 Branch_0x214:

 ldarg Arg_0x5
 Branch_0x215:

 conv.i4
 brtrue Branch_0x212

Start_Orphan_d2:
 nop
End_Orphan_d2:
 ldarg Arg_0x5
 ldarg Arg_0x3
 ldarg Arg_0x2
 blt Branch_0x218

 ldc.r4 float32(0x68ac424a)
 br Branch_0x219

 Branch_0x218:

 ldarg Arg_0x5
 Branch_0x219:

 ble.un Branch_0x216

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 call float64 [mscorlib]System.Math::Round(float64)
 br Branch_0x217

 Branch_0x216:

Start_Orphan_d3:
 nop
End_Orphan_d3:
 ldarg Arg_0x3
 brfalse Branch_0x21a

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x21b

 Branch_0x21a:

 ldarg Arg_0x5
 Branch_0x21b:

 Branch_0x217:

 br Branch_0x213

 Branch_0x212:

Start_Orphan_d4:
 ldarga Arg_0x3
 pop
End_Orphan_d4:
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ldloc LOCAL_0x4
 blt.un Branch_0x21e

 ldarg Arg_0x3
 br Branch_0x21f

 Branch_0x21e:

 ldarg Arg_0x3
 Branch_0x21f:

 brtrue Branch_0x21c

Start_Orphan_d5:
 nop
End_Orphan_d5:
 ldarg Arg_0x7
 ldarg Arg_0x6
 ble.un Branch_0x220

 ldarg Arg_0x5
 br Branch_0x221

 Branch_0x220:

 ldloc LOCAL_0x4
 Branch_0x221:

 br Branch_0x21d

 Branch_0x21c:

Start_Orphan_d6:
 nop
End_Orphan_d6:
 ldc.r4 float32(0xff1e77d1)
 ldarg Arg_0x5
 bgt.un Branch_0x222

 ldloc LOCAL_0x4
 br Branch_0x223

 Branch_0x222:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x223:

 Branch_0x21d:

 Branch_0x213:

 br Branch_0x1f9

 Branch_0x1f8:

 ldarg Arg_0x7
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 xor
Start_Orphan_d7:
 nop
End_Orphan_d7:
 ldc.i8 0xf74fb1195d7544d5
 conv.ovf.u8.un
 bne.un Branch_0x226

Start_Orphan_d8:
 nop
End_Orphan_d8:
 ldarg Arg_0x5
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 rem 
 br Branch_0x227

 Branch_0x226:

 ldc.i4.6
 ldarg Arg_0x3
 bgt.un Branch_0x228

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x229

 Branch_0x228:

 ldarg Arg_0x5
 Branch_0x229:

 Branch_0x227:

Start_Orphan_d9:
 ldarg Arg_0x3
 ldarg Arg_0x0
 bgt Branch_0x22a

 nop
 br Branch_0x22b

 Branch_0x22a:

 nop
 Branch_0x22b:

End_Orphan_d9:
 ldarg Arg_0x3
 ldarg Arg_0x3
 ble.un Branch_0x22e

 ldarg Arg_0x7
 br Branch_0x22f

 Branch_0x22e:

 ldc.i8 0xddaf25cd253bf3fc
 Branch_0x22f:

 ldarg Arg_0x6
 conv.u8
 blt Branch_0x22c

Start_Orphan_da:
 nop
End_Orphan_da:
 ldc.i4.6
 brfalse Branch_0x230

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x231

 Branch_0x230:

 ldloc LOCAL_0x4
 Branch_0x231:

 br Branch_0x22d

 Branch_0x22c:

 ldc.r4 float32(0x6b4c7883)
 call float64 [mscorlib]System.Math::Sin(float64)
 Branch_0x22d:

 bge Branch_0x224

Start_Orphan_db:
 ldc.i4.6
 ldc.i4.s -23
 beq Branch_0x232

 nop
 br Branch_0x233

 Branch_0x232:

 nop
 Branch_0x233:

End_Orphan_db:
Start_Orphan_dc:
 nop
End_Orphan_dc:
 ldarg Arg_0x3
 ldc.i4.s -83
 bge.un Branch_0x236

 ldarg Arg_0x3
 br Branch_0x237

 Branch_0x236:

 ldarg Arg_0x3
 Branch_0x237:

Start_Orphan_dd:
 nop
End_Orphan_dd:
 ldloc LOCAL_0x4
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 bge Branch_0x238

 ldarg Arg_0x3
 br Branch_0x239

 Branch_0x238:

 ldarg Arg_0x3
 Branch_0x239:

 blt.un Branch_0x234

 ldarg Arg_0x5
 ckfinite
 br Branch_0x235

 Branch_0x234:

Start_Orphan_de:
 nop
End_Orphan_de:
 ldloc LOCAL_0x4
 Branch_0x235:

 br Branch_0x225

 Branch_0x224:

 ldarg Arg_0x3
 ldc.i4.3
 blt.un Branch_0x23c

 ldc.r8 float64(0xc9f01d44962db336)
 br Branch_0x23d

 Branch_0x23c:

 ldarg Arg_0x5
 Branch_0x23d:

Start_Orphan_df:
 nop
End_Orphan_df:
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ldc.r4 float32(0x1ba7118)
 div 
 bge Branch_0x23a

 ldarg Arg_0x3
 ldarg Arg_0x4
 ble.un Branch_0x23e

 ldc.r4 float32(0x79f83d)
 br Branch_0x23f

 Branch_0x23e:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x23f:

 br Branch_0x23b

 Branch_0x23a:

 ldc.i4.2
 ldarg Arg_0x3
 beq Branch_0x240

 ldloc LOCAL_0x4
 br Branch_0x241

 Branch_0x240:

 ldc.r8 float64(0x976206e3d8241438)
 Branch_0x241:

 Branch_0x23b:

 Branch_0x225:

 Branch_0x1f9:

 ble Branch_0x1f6

Start_Orphan_e0:
 ldloc LOCAL_0x1
 ldc.i4.5
 ldelema [mscorlib]System.SByte
Start_Orphan_e1:
 nop
End_Orphan_e1:
 ldarg Arg_0x6
 conv.ovf.u2
 stind.i1
End_Orphan_e0:
Start_Orphan_e2:
 ldc.i4.2
 ldarg Arg_0x3
 blt Branch_0x242

 nop
 br Branch_0x243

 Branch_0x242:

 nop
 Branch_0x243:

End_Orphan_e2:
 ldarg Arg_0x3
 ldarg Arg_0x3
 blt Branch_0x246

 ldarg Arg_0x3
 br Branch_0x247

 Branch_0x246:

 ldarg Arg_0x3
 Branch_0x247:

 brfalse Branch_0x244

Start_Orphan_e3:
 nop
End_Orphan_e3:
 ldc.i4.m1
 ldarg Arg_0x4
 bge Branch_0x248

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x249

 Branch_0x248:

 ldarg Arg_0x5
 Branch_0x249:

 br Branch_0x245

 Branch_0x244:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 conv.r4
 Branch_0x245:

 call float64 [mscorlib]System.Math::Round(float64)
 call float64 [mscorlib]System.Math::Sqrt(float64)
 br Branch_0x1f7

 Branch_0x1f6:

Start_Orphan_e4:
 ldarg Arg_0x7
 ldarg Arg_0x6
 blt.un Branch_0x24c

 nop
 br Branch_0x24d

 Branch_0x24c:

 nop
 Branch_0x24d:

End_Orphan_e4:
Start_Orphan_e5:
 nop
End_Orphan_e5:
 ldarg Arg_0x3
 ldc.i4.m1
 cgt.un
 ldc.i4.4
 brtrue Branch_0x250

 ldarg Arg_0x3
 br Branch_0x251

 Branch_0x250:

 ldarg Arg_0x3
 Branch_0x251:

 bne.un Branch_0x24e

Start_Orphan_e6:
 nop
End_Orphan_e6:
 ldc.i4.1
 ldarg Arg_0x3
 add.ovf.un
 br Branch_0x24f

 Branch_0x24e:

Start_Orphan_e7:
 nop
End_Orphan_e7:
 ldarg Arg_0x3
 ldc.i4.3
 sub.ovf.un
 Branch_0x24f:

 ldc.r4 float32(0x833aa2ac)
 ldloc LOCAL_0x4
 cgt.un
 ldloc LOCAL_0x1
 ldlen
 xor
 add.ovf
 ldc.i4.s -104
 brfalse Branch_0x254

 ldarg Arg_0x3
 br Branch_0x255

 Branch_0x254:

 ldarg Arg_0x3
 Branch_0x255:

 conv.ovf.u2.un
Start_Orphan_e8:
 ldarga Arg_0x2
 pop
End_Orphan_e8:
 ldloc LOCAL_0x4
 ldc.r8 float64(0x4d520d4ad7957201)
 ble.un Branch_0x258

 ldarg Arg_0x0
 br Branch_0x259

 Branch_0x258:

 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 Branch_0x259:

 ldarg Arg_0x3
 ldc.i4.s -128
 bge.un Branch_0x25a

 ldarg Arg_0x3
 br Branch_0x25b

 Branch_0x25a:

 ldarg Arg_0x3
 Branch_0x25b:

 bgt Branch_0x256

 ldc.i4.7
 ldarg Arg_0x3
 ble.un Branch_0x25c

 ldc.i4.6
 br Branch_0x25d

 Branch_0x25c:

 ldarg Arg_0x4
 Branch_0x25d:

 br Branch_0x257

 Branch_0x256:

Start_Orphan_e9:
 nop
End_Orphan_e9:
 ldarg Arg_0x3
 ldarg Arg_0x3
 bge Branch_0x25e

 ldarg Arg_0x1
 br Branch_0x25f

 Branch_0x25e:

 ldc.i4 0xbea669d8
 Branch_0x25f:

 Branch_0x257:

 beq Branch_0x252

Start_Orphan_ea:
 nop
End_Orphan_ea:
 ldarg Arg_0x3
 ldarg Arg_0x2
 rem.un
Start_Orphan_eb:
 nop
End_Orphan_eb:
 ldc.i4.4
 ldarg Arg_0x3
 add
 div
 br Branch_0x253

 Branch_0x252:

Start_Orphan_ec:
 ldarg Arg_0x4
 stsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
End_Orphan_ec:
 ldarg Arg_0x3
 ldarg Arg_0x3
 ble.un Branch_0x260

 ldarg Arg_0x3
 br Branch_0x261

 Branch_0x260:

 ldarg Arg_0x3
 Branch_0x261:

Start_Orphan_ed:
 nop
End_Orphan_ed:
 ldarg Arg_0x1
 ldarg Arg_0x3
 beq Branch_0x262

 ldarg Arg_0x3
 br Branch_0x263

 Branch_0x262:

 ldarg Arg_0x3
 Branch_0x263:

 or
 Branch_0x253:

 bne.un Branch_0x24a

Start_Orphan_ee:
Start_Orphan_ef:
 nop
End_Orphan_ef:
 ldarg Arg_0x6
 ldarg Arg_0x6
 sub.ovf
 ldarg Arg_0x3
 ldarg Arg_0x3
 bgt Branch_0x266

 ldarg Arg_0x6
 br Branch_0x267

 Branch_0x266:

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 Branch_0x267:

 bgt.un Branch_0x264

 ldarg Arg_0x3
 ldarg Arg_0x3
 ble Branch_0x268

 nop
 br Branch_0x269

 Branch_0x268:

 nop
 Branch_0x269:

 br Branch_0x265

 Branch_0x264:

 ldloc LOCAL_0x2
 ldc.i4.s 21
 ldc.r8 float64(0xb1d655fbf596342)
 stelem.r8
 Branch_0x265:

End_Orphan_ee:
Start_Orphan_f0:
 ldc.i4.4
 brfalse Branch_0x26a

 nop
 br Branch_0x26b

 Branch_0x26a:

 nop
 Branch_0x26b:

End_Orphan_f0:
Start_Orphan_f1:
 nop
End_Orphan_f1:
 ldc.i4.3
 conv.ovf.i4.un
 conv.r8
 call float32 [mscorlib]System.Math::Abs(float32)
 br Branch_0x24b

 Branch_0x24a:

Start_Orphan_f2:
Start_Orphan_f3:
 nop
End_Orphan_f3:
 ldc.i4.2
 conv.u1
 newarr [mscorlib]System.UInt64
 ldarg Arg_0x1
Start_Orphan_f4:
 nop
End_Orphan_f4:
 ldarg Arg_0x6
 conv.ovf.i8.un
 stelem.i8
End_Orphan_f2:
Start_Orphan_f5:
 ldloc LOCAL_0x3
 ldarg Arg_0x4
 ldarg Arg_0x3
 stelem.i
End_Orphan_f5:
Start_Orphan_f6:
 nop
End_Orphan_f6:
 ldarg Arg_0x7
 ldarg Arg_0x7
 div.un
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ldloc LOCAL_0x4
 ble.un Branch_0x26c

 ldarg Arg_0x6
 br Branch_0x26d

 Branch_0x26c:

 ldarg Arg_0x6
 Branch_0x26d:

 add.ovf
 conv.r4
 Branch_0x24b:

 Branch_0x1f7:

 conv.ovf.i
Start_Orphan_f7:
 ldsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
Start_Orphan_f8:
 ldarg Arg_0x3
 ldarg Arg_0x3
 blt.un Branch_0x26e

 nop
 br Branch_0x26f

 Branch_0x26e:

 nop
 Branch_0x26f:

End_Orphan_f8:
Start_Orphan_f9:
 nop
End_Orphan_f9:
 ldc.i8 0x33f96be1db59bf85
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 add
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 beq Branch_0x270

 ldarg Arg_0x3
 ldarg Arg_0x3
 ble Branch_0x272

 ldc.i4.2
 br Branch_0x273

 Branch_0x272:

 ldc.i4.4
 Branch_0x273:

 br Branch_0x271

 Branch_0x270:

 ldarg Arg_0x4
 ldc.i4.4
 sub
 Branch_0x271:

 ldelema [mscorlib]System.UInt64
 pop
End_Orphan_f7:
 ldc.i4.s 70
 conv.u1
 bgt Branch_0x132

Start_Orphan_fa:
Start_Orphan_fb:
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 ldarg Arg_0x3
 ble Branch_0x27a

 ldc.r4 float32(0x63b0543c)
 br Branch_0x27b

 Branch_0x27a:

 ldarg Arg_0x5
 Branch_0x27b:

Start_Orphan_fc:
 nop
End_Orphan_fc:
 ldc.i4.8
 ldarg Arg_0x3
 bgt Branch_0x27c

 ldarg Arg_0x5
 br Branch_0x27d

 Branch_0x27c:

 ldc.r8 float64(0xc2e2e6dce07dddf8)
 Branch_0x27d:

 ble Branch_0x278

 ldarg Arg_0x5
 stsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x279

 Branch_0x278:

 ldarg Arg_0x3
 ldarg Arg_0x3
 bgt Branch_0x27e

 nop
 br Branch_0x27f

 Branch_0x27e:

 nop
 Branch_0x27f:

 Branch_0x279:

End_Orphan_fb:
Start_Orphan_fd:
 nop
End_Orphan_fd:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 conv.r.un
 conv.ovf.u4
 ldarg Arg_0x5
 conv.u1
 ldarg Arg_0x3
 ldc.i4.2
 mul.ovf
 sub.ovf
 ceq
Start_Orphan_fe:
Start_Orphan_ff:
 nop
End_Orphan_ff:
 ldloc LOCAL_0x0
 stsfld unsigned int32[] ILGEN_0x372a9ae6::field_0x4
End_Orphan_fe:
 ldc.i4.3
 ldarg Arg_0x3
 add
Start_Orphan_100:
 nop
End_Orphan_100:
 ldarg Arg_0x3
 ldc.i4.4
 rem.un
 cgt.un
Start_Orphan_101:
 nop
End_Orphan_101:
 ldc.i8 0x16f083d7ffe0e54
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 blt.un Branch_0x284

 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 br Branch_0x285

 Branch_0x284:

 ldc.i4.m1
 Branch_0x285:

Start_Orphan_102:
 nop
End_Orphan_102:
 ldloc LOCAL_0x2
 ldlen
 beq Branch_0x282

 ldarg Arg_0x3
 ldarg Arg_0x3
 ble.un Branch_0x286

 ldarg Arg_0x3
 br Branch_0x287

 Branch_0x286:

 ldarg Arg_0x3
 Branch_0x287:

 br Branch_0x283

 Branch_0x282:

Start_Orphan_103:
 nop
End_Orphan_103:
 ldarg Arg_0x3
 ldc.i4.0
 add
 Branch_0x283:

 blt Branch_0x280

Start_Orphan_104:
 ldarg Arg_0x3
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 bge Branch_0x288

 nop
 br Branch_0x289

 Branch_0x288:

 nop
 Branch_0x289:

End_Orphan_104:
Start_Orphan_105:
 nop
End_Orphan_105:
 ldarg Arg_0x3
 ldarg Arg_0x2
 bgt Branch_0x28c

 ldarg Arg_0x3
 br Branch_0x28d

 Branch_0x28c:

 ldarg Arg_0x3
 Branch_0x28d:

 ldarg Arg_0x3
 ldarg Arg_0x3
 ble Branch_0x28e

 ldarg Arg_0x3
 br Branch_0x28f

 Branch_0x28e:

 ldarg Arg_0x3
 Branch_0x28f:

 blt Branch_0x28a

Start_Orphan_106:
 nop
End_Orphan_106:
 ldarg Arg_0x3
 ldc.i4.0
 bne.un Branch_0x290

 ldarg Arg_0x3
 br Branch_0x291

 Branch_0x290:

 ldarg Arg_0x3
 Branch_0x291:

 br Branch_0x28b

 Branch_0x28a:

 ldarg Arg_0x3
 ldc.i4.7
 sub.ovf.un
 Branch_0x28b:

 br Branch_0x281

 Branch_0x280:

Start_Orphan_107:
 nop
End_Orphan_107:
 ldarg Arg_0x3
 ldarg Arg_0x3
 add
Start_Orphan_108:
 nop
End_Orphan_108:
 ldarg Arg_0x5
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 bge.un Branch_0x292

 ldarg Arg_0x3
 br Branch_0x293

 Branch_0x292:

 ldarg Arg_0x3
 Branch_0x293:

 add.ovf.un
 Branch_0x281:

 bne.un Branch_0x276

Start_Orphan_109:
 ldc.i8 0xdcf430734ef72b29
 conv.i4
Start_Orphan_10a:
 nop
End_Orphan_10a:
 ldc.i4.6
 brtrue Branch_0x296

 ldarg Arg_0x3
 br Branch_0x297

 Branch_0x296:

 ldarg Arg_0x3
 Branch_0x297:

 blt.un Branch_0x294

 ldarga Arg_0x3
 pop
 br Branch_0x295

 Branch_0x294:

 ldloc LOCAL_0x1
 ldarg Arg_0x0
 ldarg Arg_0x2
 stelem.i1
 Branch_0x295:

End_Orphan_109:
Start_Orphan_10b:
 ldc.i4.7
 ldc.i4.3
 ble Branch_0x29a

 nop
 br Branch_0x29b

 Branch_0x29a:

 nop
 Branch_0x29b:

End_Orphan_10b:
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 ldarg Arg_0x3
 div
Start_Orphan_10c:
 nop
End_Orphan_10c:
 ldarg Arg_0x6
 conv.u1
 bgt.un Branch_0x29c

 ldc.r8 float64(0xfcce278945d616ee)
 ldarg Arg_0x5
 bgt.un Branch_0x29e

 ldarg Arg_0x7
 br Branch_0x29f

 Branch_0x29e:

 ldc.i8 0x21af93f11daab90a
 Branch_0x29f:

 br Branch_0x29d

 Branch_0x29c:

Start_Orphan_10d:
 nop
End_Orphan_10d:
 ldc.i4.2
 conv.ovf.i8
 Branch_0x29d:

Start_Orphan_10e:
 ldloc LOCAL_0x2
 pop
End_Orphan_10e:
 ldarg Arg_0x3
 ldc.i4.8
 mul
Start_Orphan_10f:
 nop
End_Orphan_10f:
 ldc.i4.s 33
 ldarg Arg_0x3
 bne.un Branch_0x2a2

 ldarg Arg_0x3
 br Branch_0x2a3

 Branch_0x2a2:

 ldarg Arg_0x3
 Branch_0x2a3:

 beq Branch_0x2a0

 ldarg Arg_0x6
 conv.i8
 br Branch_0x2a1

 Branch_0x2a0:

Start_Orphan_110:
 nop
End_Orphan_110:
 ldarg Arg_0x3
 ldc.i4.s -116
 blt.un Branch_0x2a4

 ldarg Arg_0x7
 br Branch_0x2a5

 Branch_0x2a4:

 ldc.i8 0xa8eb7b90d591acdf
 Branch_0x2a5:

 Branch_0x2a1:

 bgt.un Branch_0x298

 ldc.i4.m1
 conv.u1
 newarr [mscorlib]System.UInt64
 ldarg Arg_0x2
 ldarg Arg_0x3
 beq Branch_0x2a6

 ldc.i4.5
 br Branch_0x2a7

 Branch_0x2a6:

 ldc.i4.m1
 Branch_0x2a7:

 ldelem.u8
 br Branch_0x299

 Branch_0x298:

Start_Orphan_111:
 ldc.i4.8
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 bgt Branch_0x2a8

 nop
 br Branch_0x2a9

 Branch_0x2a8:

 nop
 Branch_0x2a9:

End_Orphan_111:
Start_Orphan_112:
 nop
End_Orphan_112:
 ldarg Arg_0x1
 conv.i8
 ldc.i8 0x7438116e942addf8
 conv.i2
 shr
 Branch_0x299:

 br Branch_0x277

 Branch_0x276:

Start_Orphan_113:
 nop
End_Orphan_113:
 ldarg Arg_0x3
 ldc.i4.3
 bgt Branch_0x2ae

 ldc.i4.s 81
 br Branch_0x2af

 Branch_0x2ae:

 ldc.i4 0x5c9603c6
 Branch_0x2af:

 ldarg Arg_0x7
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ble Branch_0x2b0

 ldarg Arg_0x3
 br Branch_0x2b1

 Branch_0x2b0:

 ldarg Arg_0x3
 Branch_0x2b1:

 bne.un Branch_0x2ac

 ldarg Arg_0x3
 ldc.i4.3
 ble Branch_0x2b2

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x2b3

 Branch_0x2b2:

 ldloc LOCAL_0x4
 Branch_0x2b3:

 br Branch_0x2ad

 Branch_0x2ac:

Start_Orphan_114:
 nop
End_Orphan_114:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 conv.r4
 Branch_0x2ad:

Start_Orphan_115:
 ldarg Arg_0x6
 pop
End_Orphan_115:
Start_Orphan_116:
 nop
End_Orphan_116:
 ldc.i8 0x3c1b2f503d97b192
 conv.ovf.i4.un
 ldc.i4.6
 ldarg Arg_0x3
 bne.un Branch_0x2b6

 ldc.i4 0xc4ff01c8
 br Branch_0x2b7

 Branch_0x2b6:

 ldc.i4.5
 Branch_0x2b7:

 blt.un Branch_0x2b4

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ldloc LOCAL_0x4
 bgt.un Branch_0x2b8

 ldc.r8 float64(0x1487a972c679ac9)
 br Branch_0x2b9

 Branch_0x2b8:

 ldarg Arg_0x5
 Branch_0x2b9:

 br Branch_0x2b5

 Branch_0x2b4:

Start_Orphan_117:
 nop
End_Orphan_117:
 ldarg Arg_0x6
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 blt Branch_0x2ba

 ldc.r4 float32(0x332bb8bd)
 br Branch_0x2bb

 Branch_0x2ba:

 ldloc LOCAL_0x4
 Branch_0x2bb:

 Branch_0x2b5:

 blt.un Branch_0x2aa

 ldarg Arg_0x3
 ldarg Arg_0x3
 mul.ovf.un
Start_Orphan_118:
 nop
End_Orphan_118:
 ldarg Arg_0x3
 conv.ovf.u
 bge Branch_0x2bc

 ldc.i4.1
 ldc.i4.0
 bgt.un Branch_0x2be

 ldarg Arg_0x6
 br Branch_0x2bf

 Branch_0x2be:

 ldarg Arg_0x7
 Branch_0x2bf:

 br Branch_0x2bd

 Branch_0x2bc:

Start_Orphan_119:
 nop
End_Orphan_119:
 ldarg Arg_0x3
 ldarg Arg_0x3
 bgt.un Branch_0x2c0

 ldc.i8 0x2bb1fed81dbadcc
 br Branch_0x2c1

 Branch_0x2c0:

 ldarg Arg_0x7
 Branch_0x2c1:

 Branch_0x2bd:

 br Branch_0x2ab

 Branch_0x2aa:

Start_Orphan_11a:
 nop
End_Orphan_11a:
 ldc.i4.m1
 ldarg Arg_0x3
 sub.ovf.un
Start_Orphan_11b:
 nop
End_Orphan_11b:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 bgt Branch_0x2c4

 ldarg Arg_0x3
 br Branch_0x2c5

 Branch_0x2c4:

 ldarg Arg_0x3
 Branch_0x2c5:

 beq Branch_0x2c2

 ldarg Arg_0x6
 conv.ovf.u8.un
 br Branch_0x2c3

 Branch_0x2c2:

 ldarg Arg_0x2
 conv.u8
 Branch_0x2c3:

 Branch_0x2ab:

 Branch_0x277:

Start_Orphan_11c:
 ldloca LOCAL_0x4
Start_Orphan_11d:
 nop
End_Orphan_11d:
 ldloc LOCAL_0x2
 ldc.i4.7
 ldelem.r8
 stind.r4
End_Orphan_11c:
Start_Orphan_11e:
 ldsflda float64 ILGEN_0x372a9ae6::field_0x2
 ldarg Arg_0x5
 stind.r8
End_Orphan_11e:
Start_Orphan_11f:
 nop
End_Orphan_11f:
 ldarg Arg_0x3
 ldarg Arg_0x3
 ble.un Branch_0x2c8

 ldarg Arg_0x6
 br Branch_0x2c9

 Branch_0x2c8:

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 Branch_0x2c9:

 conv.ovf.u8.un
 ldarg Arg_0x6
 add.ovf
Start_Orphan_120:
 ldarg Arg_0x7
 ldc.i8 0xce726bc07532372d
 bgt.un Branch_0x2cc

 ldc.i4.1
 br Branch_0x2cd

 Branch_0x2cc:

 ldc.i4.2
 Branch_0x2cd:

Start_Orphan_121:
 nop
End_Orphan_121:
 ldarg Arg_0x3
 ldarg Arg_0x2
 clt
 beq Branch_0x2ca

 ldarga Arg_0x4
 pop
 br Branch_0x2cb

 Branch_0x2ca:

 ldarga Arg_0x4
 pop
 Branch_0x2cb:

End_Orphan_120:
Start_Orphan_122:
 nop
End_Orphan_122:
 ldloc LOCAL_0x4
 ldarg Arg_0x5
 ble.un Branch_0x2d0

 ldarg Arg_0x3
 br Branch_0x2d1

 Branch_0x2d0:

 ldarg Arg_0x3
 Branch_0x2d1:

 ldarg Arg_0x7
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ble.un Branch_0x2d2

 ldc.i4.4
 br Branch_0x2d3

 Branch_0x2d2:

 ldc.i4.6
 Branch_0x2d3:

 ble Branch_0x2ce

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldarg Arg_0x7
 add.ovf.un
 br Branch_0x2cf

 Branch_0x2ce:

 ldarg Arg_0x5
 ldc.r8 float64(0x152f605aa791af80)
 bne.un Branch_0x2d4

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 br Branch_0x2d5

 Branch_0x2d4:

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 Branch_0x2d5:

 Branch_0x2cf:

 ldsflda unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldind.i8
 add.ovf.un
 bge Branch_0x2c6

Start_Orphan_123:
 nop
End_Orphan_123:
 ldarg Arg_0x0
 ldarg Arg_0x3
 beq Branch_0x2d8

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 br Branch_0x2d9

 Branch_0x2d8:

 ldarg Arg_0x7
 Branch_0x2d9:

 conv.ovf.u.un
Start_Orphan_124:
 ldc.r4 float32(0xfcd5e0ab)
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 bge Branch_0x2da

 nop
 br Branch_0x2db

 Branch_0x2da:

 nop
 Branch_0x2db:

End_Orphan_124:
 ldc.i4.7
 blt.un Branch_0x2d6

 ldarg Arg_0x2
 brfalse Branch_0x2dc

 ldarg Arg_0x3
 ldc.i4.7
 ble.un Branch_0x2de

 ldc.i8 0xbd79f4259019ad54
 br Branch_0x2df

 Branch_0x2de:

 ldarg Arg_0x6
 Branch_0x2df:

 br Branch_0x2dd

 Branch_0x2dc:

Start_Orphan_125:
 nop
End_Orphan_125:
 ldc.i4.5
 conv.ovf.u8.un
 Branch_0x2dd:

 br Branch_0x2d7

 Branch_0x2d6:

Start_Orphan_126:
 nop
End_Orphan_126:
Start_Orphan_127:
 nop
End_Orphan_127:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 mul.ovf
Start_Orphan_128:
 nop
End_Orphan_128:
 ldc.i8 0x920db821f2c59a71
 and
 Branch_0x2d7:

 br Branch_0x2c7

 Branch_0x2c6:

Start_Orphan_129:
 ldc.i4.s -13
 starg Arg_0x4
End_Orphan_129:
Start_Orphan_12a:
 nop
End_Orphan_12a:
 ldc.r4 float32(0x882af164)
 call float64 [mscorlib]System.Math::Abs(float64)
Start_Orphan_12b:
 nop
End_Orphan_12b:
 ldc.i4.6
 conv.r.un
 ble.un Branch_0x2e2

 ldloc LOCAL_0x4
 ldarg Arg_0x5
 ble Branch_0x2e4

 ldarg Arg_0x5
 br Branch_0x2e5

 Branch_0x2e4:

 ldloc LOCAL_0x4
 Branch_0x2e5:

 br Branch_0x2e3

 Branch_0x2e2:

 ldarg Arg_0x3
 ldc.i4.m1
 beq Branch_0x2e6

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x2e7

 Branch_0x2e6:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x2e7:

 Branch_0x2e3:

Start_Orphan_12c:
 ldarga Arg_0x0
 ldc.i4.4
 stind.i1
End_Orphan_12c:
Start_Orphan_12d:
 nop
End_Orphan_12d:
 ldc.i4.1
 ldarg Arg_0x3
 sub.ovf
 conv.r4
 bne.un Branch_0x2e0

 ldarg Arg_0x4
 ldarg Arg_0x3
 bne.un Branch_0x2ea

 ldc.i8 0x8afc415ae484c3ae
 br Branch_0x2eb

 Branch_0x2ea:

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 Branch_0x2eb:

Start_Orphan_12e:
 nop
End_Orphan_12e:
 ldarg Arg_0x7
 conv.ovf.i8
 ble.un Branch_0x2e8

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 sub.ovf.un
 br Branch_0x2e9

 Branch_0x2e8:

 ldc.i4.0
 brtrue Branch_0x2ec

 ldc.i8 0x82fefaf37457ac6e
 br Branch_0x2ed

 Branch_0x2ec:

 ldarg Arg_0x7
 Branch_0x2ed:

 Branch_0x2e9:

 br Branch_0x2e1

 Branch_0x2e0:

Start_Orphan_12f:
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ldloc LOCAL_0x4
 blt Branch_0x2ee

 nop
 br Branch_0x2ef

 Branch_0x2ee:

 nop
 Branch_0x2ef:

End_Orphan_12f:
 ldarg Arg_0x3
 ldc.i4.s 59
 sub.ovf
 ldarg Arg_0x3
 ldc.i4.2
 add.ovf.un
 blt.un Branch_0x2f0

 ldc.r4 float32(0x9cfe11f8)
 ldarg Arg_0x5
 blt Branch_0x2f2

 ldarg Arg_0x6
 br Branch_0x2f3

 Branch_0x2f2:

 ldarg Arg_0x7
 Branch_0x2f3:

 br Branch_0x2f1

 Branch_0x2f0:

Start_Orphan_130:
 nop
End_Orphan_130:
 ldarg Arg_0x6
 conv.ovf.i8
 Branch_0x2f1:

 Branch_0x2e1:

 Branch_0x2c7:

 bgt.un Branch_0x274

Start_Orphan_131:
 ldc.i4.1
 ldc.i4 0xf74ea580
 bgt.un Branch_0x2f6

 ldc.i4.m1
 br Branch_0x2f7

 Branch_0x2f6:

 ldarg Arg_0x0
 Branch_0x2f7:

 brfalse Branch_0x2f4

 ldarg Arg_0x3
 ldarg Arg_0x0
 ble Branch_0x2f8

 nop
 br Branch_0x2f9

 Branch_0x2f8:

 nop
 Branch_0x2f9:

 br Branch_0x2f5

 Branch_0x2f4:

 ldc.i4.0
 ldc.i4.4
 ble Branch_0x2fa

 nop
 br Branch_0x2fb

 Branch_0x2fa:

 nop
 Branch_0x2fb:

 Branch_0x2f5:

End_Orphan_131:
 ldsfld unsigned int32[] ILGEN_0x372a9ae6::field_0x1
 stloc LOCAL_0x0
 br Branch_0x275

 Branch_0x274:

 ldc.i4.8
Start_Orphan_132:
Start_Orphan_133:
 nop
End_Orphan_133:
 ldarg Arg_0x3
 ldarg Arg_0x3
 ble Branch_0x300

 ldarg Arg_0x4
 br Branch_0x301

 Branch_0x300:

 ldarg Arg_0x2
 Branch_0x301:

Start_Orphan_134:
 nop
End_Orphan_134:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldarg Arg_0x6
 ceq
 bgt.un Branch_0x2fe

 ldarg Arg_0x3
 ldarg Arg_0x3
 ble.un Branch_0x302

 nop
 br Branch_0x303

 Branch_0x302:

 nop
 Branch_0x303:

 br Branch_0x2ff

 Branch_0x2fe:

 ldsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
 ldarg Arg_0x4
 ldarg Arg_0x7
 stelem.i8
 Branch_0x2ff:

End_Orphan_132:
Start_Orphan_135:
 nop
End_Orphan_135:
 ldarg Arg_0x6
 ldc.i8 0x5fce18fd12c78e8c
 bge Branch_0x304

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x305

 Branch_0x304:

 ldloc LOCAL_0x4
 Branch_0x305:

 ldarg Arg_0x3
 ldc.i4.5
 blt.un Branch_0x306

 ldloc LOCAL_0x4
 br Branch_0x307

 Branch_0x306:

 ldarg Arg_0x5
 Branch_0x307:

 ceq
Start_Orphan_136:
 nop
End_Orphan_136:
 ldc.i4.7
 ldc.i4.7
 sub.ovf.un
 conv.ovf.i.un
 sub
 ble.un Branch_0x2fc

 ldarg Arg_0x5
 call float64 [mscorlib]System.Math::Sqrt(float64)
Start_Orphan_137:
 nop
End_Orphan_137:
 ldc.r8 float64(0x4b01fbd26e281f43)
 ldarg Arg_0x5
 ble Branch_0x30a

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x30b

 Branch_0x30a:

 ldarg Arg_0x5
 Branch_0x30b:

 div 
Start_Orphan_138:
 ldarga Arg_0x2
 pop
End_Orphan_138:
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 conv.u4
Start_Orphan_139:
 nop
End_Orphan_139:
 ldarg Arg_0x3
 ldarg Arg_0x3
 add.ovf
 bgt.un Branch_0x30c

Start_Orphan_13a:
 nop
End_Orphan_13a:
 ldarg Arg_0x5
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 blt.un Branch_0x30e

 ldc.r8 float64(0x7d5e5f129d11f9b0)
 br Branch_0x30f

 Branch_0x30e:

 ldarg Arg_0x5
 Branch_0x30f:

 br Branch_0x30d

 Branch_0x30c:

 ldarg Arg_0x5
 Branch_0x30d:

 bge.un Branch_0x308

Start_Orphan_13b:
 nop
End_Orphan_13b:
 ldsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
 ldlen
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ldc.r8 float64(0x90116fc0676af361)
 bgt Branch_0x312

 ldc.i4.4
 br Branch_0x313

 Branch_0x312:

 ldc.i4.1
 Branch_0x313:

 ble.un Branch_0x310

 ldarg Arg_0x3
 ldc.i4.1
 blt.un Branch_0x314

 nop
 br Branch_0x315

 Branch_0x314:

 nop
 Branch_0x315:

 br Branch_0x311

 Branch_0x310:

 ldc.r8 float64(0x62f1f9857f3aac94)
 ldc.r8 float64(0xd5f088c8e59b212c)
 ble.un Branch_0x316

 nop
 br Branch_0x317

 Branch_0x316:

 nop
 Branch_0x317:

 Branch_0x311:

 br Branch_0x309

 Branch_0x308:

Start_Orphan_13c:
 nop
End_Orphan_13c:
 ldarg Arg_0x3
 ldarg Arg_0x3
 div.un
Start_Orphan_13d:
 nop
End_Orphan_13d:
 ldarg Arg_0x3
 ldarg Arg_0x3
 bge.un Branch_0x31a

 ldarg Arg_0x3
 br Branch_0x31b

 Branch_0x31a:

 ldarg Arg_0x3
 Branch_0x31b:

 blt Branch_0x318

 ldc.r4 float32(0xa08337f6)
 ldarg Arg_0x5
 beq Branch_0x31c

 nop
 br Branch_0x31d

 Branch_0x31c:

 nop
 Branch_0x31d:

 br Branch_0x319

 Branch_0x318:

 ldc.i4.3
 brfalse Branch_0x31e

 nop
 br Branch_0x31f

 Branch_0x31e:

 nop
 Branch_0x31f:

 Branch_0x319:

 Branch_0x309:

 br Branch_0x2fd

 Branch_0x2fc:

Start_Orphan_13e:
 nop
End_Orphan_13e:
 ldc.i4.8
 ldc.i4.8
 bge.un Branch_0x322

 ldloc LOCAL_0x4
 br Branch_0x323

 Branch_0x322:

 ldc.r4 float32(0xedd17240)
 Branch_0x323:

 call float64 [mscorlib]System.Math::Sin(float64)
Start_Orphan_13f:
 ldc.r4 float32(0x235de59e)
 ldc.r8 float64(0xd3de7a2478fc3001)
 blt.un Branch_0x324

 nop
 br Branch_0x325

 Branch_0x324:

 nop
 Branch_0x325:

End_Orphan_13f:
Start_Orphan_140:
 nop
End_Orphan_140:
 ldc.i4.4
 ldarg Arg_0x3
 blt.un Branch_0x328

 ldarg Arg_0x3
 br Branch_0x329

 Branch_0x328:

 ldarg Arg_0x3
 Branch_0x329:

 ldc.i4.0
 ldarg Arg_0x3
 clt
 blt.un Branch_0x326

Start_Orphan_141:
 nop
End_Orphan_141:
 ldarg Arg_0x4
 conv.r8
 br Branch_0x327

 Branch_0x326:

Start_Orphan_142:
 nop
End_Orphan_142:
 ldc.i4.8
 ldarg Arg_0x3
 blt.un Branch_0x32a

 ldc.r4 float32(0x6d286e62)
 br Branch_0x32b

 Branch_0x32a:

 ldloc LOCAL_0x4
 Branch_0x32b:

 Branch_0x327:

 bgt.un Branch_0x320

 ldc.r4 float32(0x60426aa6)
 ldc.r8 float64(0x486ee91dc94228ea)
 beq Branch_0x32e

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 br Branch_0x32f

 Branch_0x32e:

 ldarg Arg_0x7
 Branch_0x32f:

Start_Orphan_143:
 nop
End_Orphan_143:
 ldc.i4.s 67
 conv.ovf.u8
 blt.un Branch_0x32c

 ldarga Arg_0x2
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 stind.i
 br Branch_0x32d

 Branch_0x32c:

 ldc.r4 float32(0x645bb825)
 ldc.r8 float64(0x8ee41cf47889da5d)
 beq Branch_0x330

 nop
 br Branch_0x331

 Branch_0x330:

 nop
 Branch_0x331:

 Branch_0x32d:

 br Branch_0x321

 Branch_0x320:

 ldarg Arg_0x3
 brtrue Branch_0x334

 ldarg Arg_0x3
 br Branch_0x335

 Branch_0x334:

 ldarg Arg_0x3
 Branch_0x335:

Start_Orphan_144:
 nop
End_Orphan_144:
 ldarg Arg_0x5
 conv.u
 blt.un Branch_0x332

 ldc.i4.6
 ldc.i4.6
 bgt Branch_0x336

 nop
 br Branch_0x337

 Branch_0x336:

 nop
 Branch_0x337:

 br Branch_0x333

 Branch_0x332:

 ldloc LOCAL_0x1
 ldc.i4 0x46417e49
 ldc.i4.0
 stelem.i1
 Branch_0x333:

 Branch_0x321:

 Branch_0x2fd:

 Branch_0x275:

End_Orphan_fa:
Start_Orphan_145:
Start_Orphan_146:
 nop
End_Orphan_146:
 ldc.i4.5
 conv.u1
 newarr [mscorlib]System.IntPtr
 pop
End_Orphan_145:
Start_Orphan_147:
 nop
End_Orphan_147:
 ldc.r8 float64(0x48eaa356a1a6ec7)
 conv.u4
 ldarg Arg_0x3
 ldarg Arg_0x3
 ceq
 ble Branch_0x33a

 ldarg Arg_0x5
 conv.u
 br Branch_0x33b

 Branch_0x33a:

 ldarg Arg_0x3
 ldarg Arg_0x3
 bne.un Branch_0x33c

 ldarg Arg_0x3
 br Branch_0x33d

 Branch_0x33c:

 ldarg Arg_0x3
 Branch_0x33d:

 Branch_0x33b:

Start_Orphan_148:
 ldc.i4.3
 ldarg Arg_0x3
 bge.un Branch_0x33e

 nop
 br Branch_0x33f

 Branch_0x33e:

 nop
 Branch_0x33f:

End_Orphan_148:
 ldarg Arg_0x5
 conv.i2
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 conv.ovf.u4.un
 sub.ovf.un
 beq Branch_0x338

Start_Orphan_149:
 ldarg Arg_0x3
 brfalse Branch_0x340

 nop
 br Branch_0x341

 Branch_0x340:

 nop
 Branch_0x341:

End_Orphan_149:
 ldc.i4.7
 conv.ovf.i4
 ldc.i8 0xccf6f22aaf9aec89
 ldarg Arg_0x7
 bge Branch_0x342

 ldarg Arg_0x3
 br Branch_0x343

 Branch_0x342:

 ldarg Arg_0x3
 Branch_0x343:

 add
 br Branch_0x339

 Branch_0x338:

 ldarg Arg_0x6
 conv.ovf.u
 ldarg Arg_0x3
 ldc.i4.m1
 blt.un Branch_0x344

 ldc.i4.1
 br Branch_0x345

 Branch_0x344:

 ldc.i4.m1
 Branch_0x345:

 sub.ovf
 Branch_0x339:

Start_Orphan_14a:
 ldc.i4.0
 ldc.i4.6
 beq Branch_0x348

 nop
 br Branch_0x349

 Branch_0x348:

 nop
 Branch_0x349:

End_Orphan_14a:
 ldc.i4.4
 ldarg Arg_0x3
 clt.un
 ldarg Arg_0x3
 ldarg Arg_0x0
 bge.un Branch_0x34a

 ldc.i4.s 72
 br Branch_0x34b

 Branch_0x34a:

 ldc.i4.7
 Branch_0x34b:

 rem.un
 ldarg Arg_0x5
 ldc.r8 float64(0xf6a82c0756f026de)
 rem 
Start_Orphan_14b:
 nop
End_Orphan_14b:
 ldc.i8 0x78b9cc6fb60f48ed
 ldarg Arg_0x7
 bge Branch_0x34e

 ldarg Arg_0x5
 br Branch_0x34f

 Branch_0x34e:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x34f:

 ble.un Branch_0x34c

 ldloc LOCAL_0x3
 ldc.i4.1
 ldelem.i
 br Branch_0x34d

 Branch_0x34c:

 ldarg Arg_0x3
 conv.ovf.i.un
 Branch_0x34d:

 bge.un Branch_0x346

 ldarg Arg_0x3
 ldc.i4.5
 rem.un
 ldarg Arg_0x4
 ldc.i4.5
 clt
 ceq
 br Branch_0x347

 Branch_0x346:

Start_Orphan_14c:
 nop
End_Orphan_14c:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldarg Arg_0x6
 and
 conv.ovf.i4
 Branch_0x347:

 sub.ovf
Start_Orphan_14d:
Start_Orphan_14e:
 ldarg Arg_0x5
 ldloc LOCAL_0x4
 ble.un Branch_0x352

 nop
 br Branch_0x353

 Branch_0x352:

 nop
 Branch_0x353:

End_Orphan_14e:
 ldc.i4.m1
Start_Orphan_14f:
 nop
End_Orphan_14f:
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ldc.r8 float64(0xef1dd01acf05434)
 beq Branch_0x354

 ldarg Arg_0x2
 br Branch_0x355

 Branch_0x354:

 ldc.i4.0
 Branch_0x355:

 ldarg Arg_0x5
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ble.un Branch_0x356

 ldarg Arg_0x3
 br Branch_0x357

 Branch_0x356:

 ldarg Arg_0x3
 Branch_0x357:

 mul
 bge Branch_0x350

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldc.i4.s -6
 shr
 ldarg Arg_0x3
 ldc.i4.5
 bgt Branch_0x35a

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 br Branch_0x35b

 Branch_0x35a:

 ldarg Arg_0x6
 Branch_0x35b:

 bge.un Branch_0x358

 ldarg Arg_0x2
 stsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 br Branch_0x359

 Branch_0x358:

 ldsflda unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldarg Arg_0x6
 stind.i8
 Branch_0x359:

 br Branch_0x351

 Branch_0x350:

 ldarg Arg_0x3
 conv.i8
 ldarg Arg_0x3
 conv.i8
 bgt.un Branch_0x35c

 ldarg Arg_0x5
 ldloc LOCAL_0x4
 blt.un Branch_0x35e

 nop
 br Branch_0x35f

 Branch_0x35e:

 nop
 Branch_0x35f:

 br Branch_0x35d

 Branch_0x35c:

 ldc.i8 0x21c654f08516e95b
 ldarg Arg_0x7
 beq Branch_0x360

 nop
 br Branch_0x361

 Branch_0x360:

 nop
 Branch_0x361:

 Branch_0x35d:

 Branch_0x351:

End_Orphan_14d:
Start_Orphan_150:
 ldc.i4.1
 ldarg Arg_0x4
 blt.un Branch_0x362

 nop
 br Branch_0x363

 Branch_0x362:

 nop
 Branch_0x363:

End_Orphan_150:
 ldarg Arg_0x3
 ldarg Arg_0x4
 beq Branch_0x366

 ldc.i4.5
 br Branch_0x367

 Branch_0x366:

 ldc.i4.6
 Branch_0x367:

Start_Orphan_151:
 nop
End_Orphan_151:
 ldc.i4.s 57
 ldc.i4.2
 ble Branch_0x368

 ldarg Arg_0x3
 br Branch_0x369

 Branch_0x368:

 ldarg Arg_0x3
 Branch_0x369:

 blt.un Branch_0x364

 ldc.i4.s 16
 ldc.i4.m1
 bge.un Branch_0x36a

 ldarg Arg_0x7
 br Branch_0x36b

 Branch_0x36a:

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 Branch_0x36b:

 br Branch_0x365

 Branch_0x364:

Start_Orphan_152:
 nop
End_Orphan_152:
 ldc.i4.7
 conv.i8
 Branch_0x365:

 conv.ovf.i2.un
 ldc.i4.7
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 shr
Start_Orphan_153:
 nop
End_Orphan_153:
 ldc.i8 0x5b31f22efd68c76d
 ldc.i8 0xfab57f9f90d7e493
 bne.un Branch_0x36e

 ldarg Arg_0x3
 br Branch_0x36f

 Branch_0x36e:

 ldarg Arg_0x3
 Branch_0x36f:

 div
Start_Orphan_154:
 nop
End_Orphan_154:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 conv.r4
 conv.u4
 bne.un Branch_0x36c

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 ldc.r8 float64(0x41405455925311d9)
 blt.un Branch_0x372

 ldarg Arg_0x5
 br Branch_0x373

 Branch_0x372:

 ldc.r8 float64(0xf45ac94fe732224e)
 Branch_0x373:

 ldc.r4 float32(0x8497c13b)
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 blt.un Branch_0x374

 ldarg Arg_0x5
 br Branch_0x375

 Branch_0x374:

 ldloc LOCAL_0x4
 Branch_0x375:

 beq Branch_0x370

 ldarg Arg_0x1
 ldarg Arg_0x3
 beq Branch_0x376

 ldarg Arg_0x3
 br Branch_0x377

 Branch_0x376:

 ldarg Arg_0x3
 Branch_0x377:

 br Branch_0x371

 Branch_0x370:

Start_Orphan_155:
 nop
End_Orphan_155:
 ldc.i4.0
 brfalse Branch_0x378

 ldarg Arg_0x3
 br Branch_0x379

 Branch_0x378:

 ldarg Arg_0x3
 Branch_0x379:

 Branch_0x371:

 br Branch_0x36d

 Branch_0x36c:

Start_Orphan_156:
 ldarg Arg_0x3
 ldc.i4.s -106
 blt.un Branch_0x37a

 nop
 br Branch_0x37b

 Branch_0x37a:

 nop
 Branch_0x37b:

End_Orphan_156:
Start_Orphan_157:
 nop
End_Orphan_157:
 ldarg Arg_0x6
 ldc.i8 0x867aabc33ce97d29
 sub
 ldarg Arg_0x2
 ldarg Arg_0x4
 bgt Branch_0x37e

 ldarg Arg_0x7
 br Branch_0x37f

 Branch_0x37e:

 ldc.i8 0xa81770d27117a986
 Branch_0x37f:

 blt.un Branch_0x37c

 ldarg Arg_0x3
 ldarg Arg_0x3
 rem.un
 br Branch_0x37d

 Branch_0x37c:

Start_Orphan_158:
 nop
End_Orphan_158:
 ldarg Arg_0x7
 conv.i
 Branch_0x37d:

 Branch_0x36d:

 or
 mul.ovf.un
 conv.ovf.u2
 br Branch_0x133

 Branch_0x132:

Start_Orphan_159:
Start_Orphan_15a:
 ldloc LOCAL_0x2
 stloc LOCAL_0x2
End_Orphan_15a:
 ldsflda unsigned int16 ILGEN_0x372a9ae6::field_0x5
Start_Orphan_15b:
 ldsflda unsigned int16 ILGEN_0x372a9ae6::field_0x5
 ldc.i4.5
 stind.i2
End_Orphan_15b:
Start_Orphan_15c:
 nop
End_Orphan_15c:
 ldarg Arg_0x3
 ldarg Arg_0x3
 div.un
 conv.i
Start_Orphan_15d:
 ldloc LOCAL_0x4
 ldc.r4 float32(0x40f707e5)
 bge.un Branch_0x382

 nop
 br Branch_0x383

 Branch_0x382:

 nop
 Branch_0x383:

End_Orphan_15d:
 ldloc LOCAL_0x4
 conv.ovf.u8
 ldarg Arg_0x5
 ldc.r8 float64(0x6be1b95cfe109c16)
 bgt Branch_0x386

 ldarg Arg_0x6
 br Branch_0x387

 Branch_0x386:

 ldarg Arg_0x7
 Branch_0x387:

 ble.un Branch_0x384

Start_Orphan_15e:
 nop
End_Orphan_15e:
 ldarg Arg_0x3
 ldc.i4.6
 add.ovf.un
 br Branch_0x385

 Branch_0x384:

 ldarg Arg_0x3
 ldc.i4 0xc098cff9
 sub
 Branch_0x385:

 bgt Branch_0x380

Start_Orphan_15f:
 nop
End_Orphan_15f:
 ldarg Arg_0x7
 ldc.i8 0x88b020646ac394ba
 ble Branch_0x388

 ldarg Arg_0x3
 br Branch_0x389

 Branch_0x388:

 ldarg Arg_0x3
 Branch_0x389:

 conv.r8
 br Branch_0x381

 Branch_0x380:

 ldarg Arg_0x6
 conv.ovf.i2.un
Start_Orphan_160:
 nop
End_Orphan_160:
 ldarg Arg_0x3
 ldarg Arg_0x3
 add.ovf
 ble Branch_0x38a

 ldarg Arg_0x5
 ckfinite
 br Branch_0x38b

 Branch_0x38a:

 ldarg Arg_0x3
 ldc.i4.0
 bgt.un Branch_0x38c

 ldc.r8 float64(0x69467e99f5f92671)
 br Branch_0x38d

 Branch_0x38c:

 ldloc LOCAL_0x4
 Branch_0x38d:

 Branch_0x38b:

 Branch_0x381:

 conv.ovf.i1
 stind.i2
End_Orphan_159:
Start_Orphan_161:
Start_Orphan_162:
 nop
End_Orphan_162:
 ldc.i4.s -2
 ldarg Arg_0x3
 add.ovf.un
 ldc.i4.5
 ldc.i4.5
 bgt Branch_0x392

 ldarg Arg_0x3
 br Branch_0x393

 Branch_0x392:

 ldarg Arg_0x3
 Branch_0x393:

 blt.un Branch_0x390

 ldc.i4.m1
 ldarg Arg_0x3
 blt.un Branch_0x394

 nop
 br Branch_0x395

 Branch_0x394:

 nop
 Branch_0x395:

 br Branch_0x391

 Branch_0x390:

 ldc.i4.2
 ldc.i4.m1
 ble Branch_0x396

 nop
 br Branch_0x397

 Branch_0x396:

 nop
 Branch_0x397:

 Branch_0x391:

End_Orphan_161:
 ldc.r4 float32(0x97bac068)
 conv.ovf.u
 ldc.r8 float64(0x825cf7b4fc4c05e)
 ldc.r8 float64(0xbf7477b5cbc0b006)
 ble Branch_0x39a

 ldarg Arg_0x3
 br Branch_0x39b

 Branch_0x39a:

 ldarg Arg_0x3
 Branch_0x39b:

 bge.un Branch_0x398

 ldc.r8 float64(0xdc900662a871a4e2)
 ldc.r8 float64(0x552619852e1b34c8)
 beq Branch_0x39c

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x39d

 Branch_0x39c:

 ldloc LOCAL_0x4
 Branch_0x39d:

 br Branch_0x399

 Branch_0x398:

Start_Orphan_163:
 nop
End_Orphan_163:
 ldarg Arg_0x5
 ldc.r4 float32(0x7a584735)
 beq Branch_0x39e

 ldarg Arg_0x5
 br Branch_0x39f

 Branch_0x39e:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x39f:

 Branch_0x399:

 conv.i1
 conv.ovf.i.un
Start_Orphan_164:
Start_Orphan_165:
 ldloc LOCAL_0x4
 ldloc LOCAL_0x4
 bgt Branch_0x3a2

 nop
 br Branch_0x3a3

 Branch_0x3a2:

 nop
 Branch_0x3a3:

End_Orphan_165:
 ldarg Arg_0x3
Start_Orphan_166:
 nop
End_Orphan_166:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldarg Arg_0x7
 clt.un
Start_Orphan_167:
 nop
End_Orphan_167:
 ldc.i4.4
 ldarg Arg_0x3
 rem.un
 add
 bge.un Branch_0x3a0

Start_Orphan_168:
 nop
End_Orphan_168:
 ldarga Arg_0x3
 ldc.i4.3
 conv.ovf.i
 stind.i
 br Branch_0x3a1

 Branch_0x3a0:

Start_Orphan_169:
 nop
End_Orphan_169:
 ldsflda float64 ILGEN_0x372a9ae6::field_0x2
Start_Orphan_16a:
 nop
End_Orphan_16a:
 ldc.r8 float64(0x8b1d54217d53f848)
 ldloc LOCAL_0x4
 blt Branch_0x3a4

 ldloc LOCAL_0x4
 br Branch_0x3a5

 Branch_0x3a4:

 ldarg Arg_0x5
 Branch_0x3a5:

 stind.r8
 Branch_0x3a1:

End_Orphan_164:
Start_Orphan_16b:
 ldarg Arg_0x3
 brfalse Branch_0x3a8

 ldarg Arg_0x3
 br Branch_0x3a9

 Branch_0x3a8:

 ldarg Arg_0x3
 Branch_0x3a9:

 brfalse Branch_0x3a6

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldarg Arg_0x7
 bge.un Branch_0x3aa

 nop
 br Branch_0x3ab

 Branch_0x3aa:

 nop
 Branch_0x3ab:

 br Branch_0x3a7

 Branch_0x3a6:

 ldc.i4.4
 ldc.i4.3
 beq Branch_0x3ac

 nop
 br Branch_0x3ad

 Branch_0x3ac:

 nop
 Branch_0x3ad:

 Branch_0x3a7:

End_Orphan_16b:
Start_Orphan_16c:
 ldloc LOCAL_0x3
 stloc LOCAL_0x3
End_Orphan_16c:
 ldarg Arg_0x3
 conv.i4
Start_Orphan_16d:
 nop
End_Orphan_16d:
 ldc.i4.8
 brfalse Branch_0x3b0

 ldarg Arg_0x3
 br Branch_0x3b1

 Branch_0x3b0:

 ldarg Arg_0x3
 Branch_0x3b1:

 beq Branch_0x3ae

Start_Orphan_16e:
 nop
End_Orphan_16e:
 ldloc LOCAL_0x4
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 bge.un Branch_0x3b2

 ldarg Arg_0x6
 br Branch_0x3b3

 Branch_0x3b2:

 ldarg Arg_0x6
 Branch_0x3b3:

 br Branch_0x3af

 Branch_0x3ae:

 ldarg Arg_0x3
 ldarg Arg_0x3
 ble Branch_0x3b4

 ldarg Arg_0x6
 br Branch_0x3b5

 Branch_0x3b4:

 ldarg Arg_0x7
 Branch_0x3b5:

 Branch_0x3af:

 conv.i1
Start_Orphan_16f:
Start_Orphan_170:
 nop
End_Orphan_170:
 ldc.r8 float64(0xf91d85165ed718f9)
 ldarg Arg_0x5
 blt.un Branch_0x3b6

 ldsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
 stsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
 br Branch_0x3b7

 Branch_0x3b6:

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 bge Branch_0x3b8

 nop
 br Branch_0x3b9

 Branch_0x3b8:

 nop
 Branch_0x3b9:

 Branch_0x3b7:

End_Orphan_16f:
Start_Orphan_171:
 nop
End_Orphan_171:
 ldarg Arg_0x3
 ldarg Arg_0x3
 add.ovf.un
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 clt
 bgt.un Branch_0x3ba

Start_Orphan_172:
 nop
End_Orphan_172:
 ldc.i4 0x83d6a7fc
 ldc.i4.1
 blt Branch_0x3bc

 ldc.i4.0
 br Branch_0x3bd

 Branch_0x3bc:

 ldc.i4.7
 Branch_0x3bd:

 br Branch_0x3bb

 Branch_0x3ba:

 ldarg Arg_0x4
 Branch_0x3bb:

 conv.ovf.u4.un
 add.ovf
 clt.un
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
Start_Orphan_173:
 ldarg Arg_0x3
 ldc.i4.1
 beq Branch_0x3c2

 nop
 br Branch_0x3c3

 Branch_0x3c2:

 nop
 Branch_0x3c3:

End_Orphan_173:
Start_Orphan_174:
 nop
End_Orphan_174:
 ldarg Arg_0x3
 conv.ovf.u4.un
Start_Orphan_175:
 nop
End_Orphan_175:
 ldarg Arg_0x2
 blt Branch_0x3c4

 ldarg Arg_0x3
 conv.ovf.i8.un
 br Branch_0x3c5

 Branch_0x3c4:

 ldc.i8 0x445ad0e7149ed6a7
 Branch_0x3c5:

 ldarg Arg_0x3
 ldarg Arg_0x3
 mul
Start_Orphan_176:
 nop
End_Orphan_176:
 ldarg Arg_0x3
 ldc.i4.1
 add
 ble.un Branch_0x3c6

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldarg Arg_0x6
 mul.ovf.un
 br Branch_0x3c7

 Branch_0x3c6:

Start_Orphan_177:
 nop
End_Orphan_177:
 ldarg Arg_0x3
 ldarg Arg_0x3
 beq Branch_0x3c8

 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 br Branch_0x3c9

 Branch_0x3c8:

 ldarg Arg_0x6
 Branch_0x3c9:

 Branch_0x3c7:

 ble Branch_0x3c0

Start_Orphan_178:
 nop
End_Orphan_178:
 ldarg Arg_0x3
 conv.i2
Start_Orphan_179:
 nop
End_Orphan_179:
 ldc.i4.s 76
 conv.ovf.u4.un
 blt Branch_0x3ca

Start_Orphan_17a:
 nop
End_Orphan_17a:
 ldc.i4.8
 ldarg Arg_0x3
 ble Branch_0x3cc

 ldloc LOCAL_0x4
 br Branch_0x3cd

 Branch_0x3cc:

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 Branch_0x3cd:

 br Branch_0x3cb

 Branch_0x3ca:

Start_Orphan_17b:
 nop
End_Orphan_17b:
 ldarga Arg_0x5
 ldind.r8
 Branch_0x3cb:

 br Branch_0x3c1

 Branch_0x3c0:

 ldsflda float64 ILGEN_0x372a9ae6::field_0x2
 ldind.r8
 Branch_0x3c1:

 bgt Branch_0x3be

Start_Orphan_17c:
 ldc.r4 float32(0x8ffc336c)
 ldc.r8 float64(0x7b81fe91422ed12d)
 cgt
Start_Orphan_17d:
 nop
End_Orphan_17d:
 ldarg Arg_0x3
 ldarg Arg_0x3
 bne.un Branch_0x3d0

 ldarg Arg_0x1
 br Branch_0x3d1

 Branch_0x3d0:

 ldc.i4.4
 Branch_0x3d1:

 ble Branch_0x3ce

 ldloc LOCAL_0x3
 ldc.i4.1
 ldarg Arg_0x3
 stelem.i
 br Branch_0x3cf

 Branch_0x3ce:

 ldarg Arg_0x3
 ldc.i4.s 84
 blt Branch_0x3d2

 nop
 br Branch_0x3d3

 Branch_0x3d2:

 nop
 Branch_0x3d3:

 Branch_0x3cf:

End_Orphan_17c:
Start_Orphan_17e:
 ldarg Arg_0x0
 ldarg Arg_0x3
 blt.un Branch_0x3d6

 nop
 br Branch_0x3d7

 Branch_0x3d6:

 nop
 Branch_0x3d7:

End_Orphan_17e:
 ldloc LOCAL_0x3
 ldc.i4.m1
 conv.ovf.u1.un
 ldelem.i
 ldarg Arg_0x5
 ldc.r4 float32(0x64ba3ee0)
 blt Branch_0x3d8

 ldarg Arg_0x3
 br Branch_0x3d9

 Branch_0x3d8:

 ldarg Arg_0x3
 Branch_0x3d9:

 neg
 ble.un Branch_0x3d4

Start_Orphan_17f:
 nop
End_Orphan_17f:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldarg Arg_0x6
 beq Branch_0x3da

 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 br Branch_0x3db

 Branch_0x3da:

 ldloc LOCAL_0x4
 Branch_0x3db:

 ldc.r4 float32(0x1e2a3cf8)
 ldarg Arg_0x5
 blt.un Branch_0x3dc

 ldc.r4 float32(0xae71951b)
 br Branch_0x3dd

 Branch_0x3dc:

 ldc.r4 float32(0xb81759a0)
 Branch_0x3dd:

 div 
 br Branch_0x3d5

 Branch_0x3d4:

 ldc.i8 0x7667da1dfceb2eb
 conv.ovf.u4
 ldarg Arg_0x3
 ldarg Arg_0x3
 div.un
 bge.un Branch_0x3de

 ldc.i4 0xcf575f84
 ldarg Arg_0x2
 bgt.un Branch_0x3e0

 ldarg Arg_0x5
 br Branch_0x3e1

 Branch_0x3e0:

 ldarg Arg_0x5
 Branch_0x3e1:

 br Branch_0x3df

 Branch_0x3de:

Start_Orphan_180:
 nop
End_Orphan_180:
 ldarg Arg_0x3
 conv.r4
 Branch_0x3df:

 Branch_0x3d5:

 br Branch_0x3bf

 Branch_0x3be:

Start_Orphan_181:
 nop
End_Orphan_181:
 ldarg Arg_0x7
 neg
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 cgt
Start_Orphan_182:
 nop
End_Orphan_182:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldarg Arg_0x7
 and
 ldc.i4 0xf6a355c1
 conv.ovf.u8.un
 bge.un Branch_0x3e4

 ldc.i4.6
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 blt Branch_0x3e6

 ldarg Arg_0x2
 br Branch_0x3e7

 Branch_0x3e6:

 ldc.i4.4
 Branch_0x3e7:

 br Branch_0x3e5

 Branch_0x3e4:

 ldc.i4.5
 ldc.i4.1
 ceq
 Branch_0x3e5:

 blt.un Branch_0x3e2

Start_Orphan_183:
 ldarga Arg_0x6
 ldarg Arg_0x7
 stind.i8
End_Orphan_183:
Start_Orphan_184:
 nop
End_Orphan_184:
 ldloc LOCAL_0x2
 ldarg Arg_0x0
 ldelem.r8
 call float64 [mscorlib]System.Math::Sqrt(float64)
 br Branch_0x3e3

 Branch_0x3e2:

Start_Orphan_185:
 ldarga Arg_0x7
 pop
End_Orphan_185:
 ldloc LOCAL_0x4
 Branch_0x3e3:

 Branch_0x3bf:

 conv.u4
 bge Branch_0x38e

Start_Orphan_186:
Start_Orphan_187:
 nop
End_Orphan_187:
 ldarg Arg_0x5
 conv.u8
 conv.ovf.i2
 conv.u1
 newarr [mscorlib]System.SByte
 pop
End_Orphan_186:
 ldarg Arg_0x3
Start_Orphan_188:
Start_Orphan_189:
 nop
End_Orphan_189:
 ldc.i8 0xba1edfb4242dce8e
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 shr
 conv.ovf.i4
 starg Arg_0x0
End_Orphan_188:
Start_Orphan_18a:
Start_Orphan_18b:
 nop
End_Orphan_18b:
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 conv.r4
 pop
End_Orphan_18a:
 ldarga Arg_0x3
 ldind.i
Start_Orphan_18c:
 ldarg Arg_0x2
 conv.r8
 pop
End_Orphan_18c:
 ldc.i4.8
Start_Orphan_18d:
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 stsfld float64 ILGEN_0x372a9ae6::field_0x2
End_Orphan_18d:
Start_Orphan_18e:
 nop
End_Orphan_18e:
 ldc.i4.m1
 conv.u1
 newarr [mscorlib]System.IntPtr
Start_Orphan_18f:
 nop
End_Orphan_18f:
 ldarg Arg_0x7
 ldarg Arg_0x6
 clt
 ldelem.i
 rem.un
 and
 cgt
 br Branch_0x38f

 Branch_0x38e:

Start_Orphan_190:
 ldsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
Start_Orphan_191:
 ldloc LOCAL_0x1
 ldc.i4.s -113
 ldc.i4.m1
 stelem.i1
End_Orphan_191:
 ldarg Arg_0x3
 ldarg Arg_0x3
 cgt
 conv.ovf.i
Start_Orphan_192:
 nop
End_Orphan_192:
 ldc.r8 float64(0x6e01eca7ea3adec2)
 ckfinite
 conv.u
 cgt
 ldsflda unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldind.i8
 stelem.i8
End_Orphan_190:
Start_Orphan_193:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 conv.ovf.i
Start_Orphan_194:
 nop
End_Orphan_194:
 ldloc LOCAL_0x4
 conv.ovf.i
 mul.ovf.un
 pop
End_Orphan_193:
 ldc.i4.5
Start_Orphan_195:
 ldc.r8 float64(0x341bb9b87e7c5d9)
 conv.r4
 conv.ovf.i
 pop
End_Orphan_195:
 ldarg Arg_0x0
 ceq
 Branch_0x38f:

 Branch_0x133:

Start_Orphan_196:
 ldsflda unsigned int64 ILGEN_0x372a9ae6::field_0x0
Start_Orphan_197:
 ldc.i4.4
 pop
End_Orphan_197:
Start_Orphan_198:
 nop
End_Orphan_198:
 ldarg Arg_0x7
 conv.u1
 conv.i
 conv.ovf.u8
Start_Orphan_199:
 ldsfld unsigned int32[] ILGEN_0x372a9ae6::field_0x1
 ldc.i4.5
 ldelema [mscorlib]System.Int32
 ldc.i4.5
 ldc.i4.6
 sub.ovf
 stind.i
End_Orphan_199:
Start_Orphan_19a:
 ldsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
 stsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
End_Orphan_19a:
 ldarg Arg_0x6
Start_Orphan_19b:
 ldarga Arg_0x4
 ldarg Arg_0x2
 stind.i2
End_Orphan_19b:
Start_Orphan_19c:
 nop
End_Orphan_19c:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 ldarg Arg_0x6
 div.un
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 conv.ovf.i8.un
 div
 or
 sub.ovf
 stind.i8
End_Orphan_196:
Start_Orphan_19d:
 ldc.i4 8 
 Loop_0x0: //WHAT
Start_Orphan_19e:
 nop
End_Orphan_19e:
 ldc.i4.6
 conv.ovf.i8
 conv.u2
 conv.ovf.u2.un
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x0
 pop
 Loop_0x1: // End of WHAT
End_Orphan_19d:
Start_Orphan_19f:
Start_Orphan_1a0:
 nop
End_Orphan_1a0:
 ldarg Arg_0x6
 ldarg Arg_0x7
 sub.ovf
 starg Arg_0x6
End_Orphan_19f:
 ldsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
Start_Orphan_1a1:
 ldloc LOCAL_0x1
 ldc.i4.7
 ldc.i4.s -76
 stelem.i1
End_Orphan_1a1:
Start_Orphan_1a2:
 nop
End_Orphan_1a2:
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 conv.r4
 conv.ovf.u1
 ldelem.u8
Start_Orphan_1a3:
 ldloc LOCAL_0x3
 stloc LOCAL_0x3
End_Orphan_1a3:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 conv.u8
 mul.ovf
 conv.ovf.i1.un
 conv.ovf.i8.un
 conv.ovf.u2
 blt.un Branch_0x130

Start_Orphan_1a4:
Start_Orphan_1a5:
Start_Orphan_1a6:
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 pop
End_Orphan_1a6:
Start_Orphan_1a7:
 nop
End_Orphan_1a7:
 ldc.i4.2
 conv.ovf.u4
 conv.r.un
 ldarg Arg_0x3
 conv.r4
 conv.r4
 mul 
 stsfld float64 ILGEN_0x372a9ae6::field_0x2
End_Orphan_1a5:
Start_Orphan_1a8:
 ldloc LOCAL_0x2
 ldc.i4.7
Start_Orphan_1a9:
 nop
End_Orphan_1a9:
 ldc.i4.3
 conv.r8
 stelem.r8
End_Orphan_1a8:
Start_Orphan_1aa:
 ldc.i4.4
 pop
End_Orphan_1aa:
 ldc.i4.3
 ldarg Arg_0x3
 div.un
 neg
 conv.ovf.i2
 conv.u1
 newarr [mscorlib]System.IntPtr
 ldarg Arg_0x7
 ldc.i4.6
 shr
 ldarg Arg_0x3
 conv.ovf.u8
 add.ovf.un
 conv.ovf.u4.un
 conv.ovf.u2.un
 ldelema [mscorlib]System.IntPtr
Start_Orphan_1ab:
 ldsfld unsigned int32[] ILGEN_0x372a9ae6::field_0x1
Start_Orphan_1ac:
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 stsfld float64 ILGEN_0x372a9ae6::field_0x2
End_Orphan_1ac:
 ldc.r8 float64(0xf040378b6cc459bc)
 conv.u4
 ldelema [mscorlib]System.Int32
Start_Orphan_1ad:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 starg Arg_0x6
End_Orphan_1ad:
 ldc.r8 float64(0xb475c052ef1b71f4)
 ldc.r4 float32(0x7059ddaf)
 rem 
Start_Orphan_1ae:
 nop
End_Orphan_1ae:
 ldc.r4 float32(0x32465496)
 cgt
 conv.ovf.i4
 stind.i4
End_Orphan_1ab:
Start_Orphan_1af:
Start_Orphan_1b0:
 ldsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
 ldc.i4.5
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 stelem.i8
End_Orphan_1b0:
 ldarg Arg_0x5
 conv.ovf.u2
 conv.u1
 newarr [mscorlib]System.SByte
Start_Orphan_1b1:
 ldloc LOCAL_0x1
 stloc LOCAL_0x1
End_Orphan_1b1:
 ldc.r4 float32(0x6935f089)
 call float64 [mscorlib]System.Math::Sqrt(float64)
 conv.i4
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 neg
 conv.ovf.u4.un
 stelem.i1
End_Orphan_1af:
Start_Orphan_1b2:
Start_Orphan_1b3:
 nop
End_Orphan_1b3:
 ldarga Arg_0x4
 ldc.i4.s -59
 conv.ovf.u1.un
 stind.i2
End_Orphan_1b2:
Start_Orphan_1b4:
 ldarga Arg_0x0
 ldc.i4.4
 stind.i1
End_Orphan_1b4:
Start_Orphan_1b5:
 nop
End_Orphan_1b5:
 ldarg Arg_0x3
 conv.u
Start_Orphan_1b6:
 nop
End_Orphan_1b6:
 ldc.i4.7
 conv.u2
 sub
 conv.ovf.u1.un
 conv.ovf.i8
 conv.ovf.u
 stind.i
End_Orphan_1a4:
Start_Orphan_1b7:
Start_Orphan_1b8:
 ldarga Arg_0x7
 pop
End_Orphan_1b8:
 ldarg Arg_0x3
 ldarg Arg_0x3
 clt.un
 ldarg Arg_0x3
 rem
Start_Orphan_1b9:
 nop
End_Orphan_1b9:
 ldarg Arg_0x3
 conv.u
 ldarga Arg_0x0
 ldind.i1
 div
 clt.un
 starg Arg_0x4
End_Orphan_1b7:
 ldsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
Start_Orphan_1ba:
 ldloc LOCAL_0x3
 pop
End_Orphan_1ba:
 ldarg Arg_0x3
 ldc.i4.s -103
 cgt
 conv.i8
 neg
 ldc.i4.5
 conv.i
 conv.i4
Start_Orphan_1bb:
 nop
End_Orphan_1bb:
 ldc.r4 float32(0x10c204d1)
 ckfinite
 conv.u2
 div
 shr.un
 conv.ovf.u4
 ldelema [mscorlib]System.UInt64
 ldind.u8
 br Branch_0x131

 Branch_0x130:

Start_Orphan_1bc:
Start_Orphan_1bd:
 ldloca LOCAL_0x4
 ldloc LOCAL_0x2
 ldc.i4.2
 ldelem.r8
 stind.r4
End_Orphan_1bd:
 ldsflda unsigned int16 ILGEN_0x372a9ae6::field_0x5
Start_Orphan_1be:
Start_Orphan_1bf:
 nop
End_Orphan_1bf:
 ldloc LOCAL_0x2
 ldsfld unsigned int16 ILGEN_0x372a9ae6::field_0x5
 ldelema [mscorlib]System.Double
 ldloc LOCAL_0x4
 call float32 [mscorlib]System.Math::Abs(float32)
 stind.r8
End_Orphan_1be:
 ldc.i4.6
 ldarg Arg_0x3
 mul
 conv.i8
 conv.i1
 stind.i2
End_Orphan_1bc:
Start_Orphan_1c0:
Start_Orphan_1c1:
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 stloc LOCAL_0x4
End_Orphan_1c1:
 ldsfld unsigned int32[] ILGEN_0x372a9ae6::field_0x4
Start_Orphan_1c2:
 nop
End_Orphan_1c2:
 ldarg Arg_0x3
 conv.i1
 ldelema [mscorlib]System.Int32
 pop
End_Orphan_1c0:
Start_Orphan_1c3:
Start_Orphan_1c4:
 nop
End_Orphan_1c4:
 ldarga Arg_0x6
Start_Orphan_1c5:
 nop
End_Orphan_1c5:
 ldarg Arg_0x3
 conv.ovf.i8.un
 stind.i8
End_Orphan_1c3:
 ldsflda unsigned int16 ILGEN_0x372a9ae6::field_0x5
 ldind.i2
 conv.ovf.i8.un
Start_Orphan_1c6:
 ldloc LOCAL_0x3
Start_Orphan_1c7:
Start_Orphan_1c8:
 nop
End_Orphan_1c8:
 ldsfld unsigned int64[] ILGEN_0x372a9ae6::field_0x3
 ldarg Arg_0x6
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 clt
Start_Orphan_1c9:
 nop
End_Orphan_1c9:
 ldarg Arg_0x7
 ldarg Arg_0x6
 rem.un
 stelem.i8
End_Orphan_1c7:
Start_Orphan_1ca:
 nop
End_Orphan_1ca:
 ldc.i8 0xe9eba421fd2c04da
 ldarg Arg_0x6
 div
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 conv.ovf.u8.un
 clt.un
 conv.ovf.u1.un
 ldarg Arg_0x5
 conv.i8
 conv.ovf.u4
 conv.ovf.i1.un
 stelem.i
End_Orphan_1c6:
Start_Orphan_1cb:
 ldsflda unsigned int16 ILGEN_0x372a9ae6::field_0x5
 pop
End_Orphan_1cb:
Start_Orphan_1cc:
 ldarga Arg_0x4
 pop
End_Orphan_1cc:
 ldc.r4 float32(0x946adaa2)
 conv.i4
 conv.ovf.u8
 ldloca LOCAL_0x4
 ldind.r4
 conv.i8
 mul.ovf
 conv.ovf.i.un
Start_Orphan_1cd:
 ldc.i4 3 
 Loop_0x2: //WHAT
Start_Orphan_1ce:
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 pop
End_Orphan_1ce:
Start_Orphan_1cf:
 nop
End_Orphan_1cf:
 ldarg Arg_0x7
 ldsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
 cgt.un
Start_Orphan_1d0:
 nop
End_Orphan_1d0:
 ldarg Arg_0x1
 rem
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x2
 pop
 Loop_0x3: // End of WHAT
End_Orphan_1cd:
Start_Orphan_1d1:
 ldsflda unsigned int16 ILGEN_0x372a9ae6::field_0x5
 ldarg Arg_0x3
 ldarg Arg_0x3
 clt.un
 stind.i2
End_Orphan_1d1:
Start_Orphan_1d2:
 nop
End_Orphan_1d2:
 ldc.r4 float32(0xa05bf89a)
 call float64 [mscorlib]System.Math::Round(float64)
 conv.i2
 conv.u1
 newarr [mscorlib]System.IntPtr
Start_Orphan_1d3:
 ldarg Arg_0x4
 conv.u1
 newarr [mscorlib]System.Int32
 stloc LOCAL_0x0
End_Orphan_1d3:
Start_Orphan_1d4:
 nop
End_Orphan_1d4:
 ldsfld float64 ILGEN_0x372a9ae6::field_0x2
 call float32 [mscorlib]System.Math::Abs(float32)
 conv.ovf.i
 conv.ovf.u1.un
 ldelem.i
 clt
 shr
Start_Orphan_1d5:
 ldc.r4 float32(0xa0cfd2d7)
 stsfld float64 ILGEN_0x372a9ae6::field_0x2
End_Orphan_1d5:
Start_Orphan_1d6:
Start_Orphan_1d7:
 ldloc LOCAL_0x3
 stloc LOCAL_0x3
End_Orphan_1d7:
 ldc.i4 0x2d465acf
 conv.u1
 newarr [mscorlib]System.Int32
 ldc.i4.8
Start_Orphan_1d8:
 ldarg Arg_0x3
 conv.ovf.u8
 stsfld unsigned int64 ILGEN_0x372a9ae6::field_0x0
End_Orphan_1d8:
Start_Orphan_1d9:
 ldsfld unsigned int32[] ILGEN_0x372a9ae6::field_0x1
 stsfld unsigned int32[] ILGEN_0x372a9ae6::field_0x1
End_Orphan_1d9:
 ldarg Arg_0x6
 ldarg Arg_0x6
 add.ovf
 conv.ovf.i
 conv.i2
 stelem.i4
End_Orphan_1d6:
 ldc.i4.4
 conv.u1
 newarr [mscorlib]System.Double
 ldlen
 shr.un
 Branch_0x131:

 conv.ovf.i2
EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0x3a5af767 
ldc.i4 0x9df742d2 
ldc.i4 0x76551662 
ldc.i4 0xc43d51da conv.i
ldc.i4 0x58c20d67 
ldc.r8 float64(0x46e287334caf0c7e) 
ldc.i8 0xd86af212d4e659c3 
ldc.i8 0x7aee4499d8d82056 

call int32 ILGEN_0x372a9ae6::Method_0xdc6ff1a4(int8 Arg_0x0, int8 Arg_0x1, int32 Arg_0x2, native int Arg_0x3, unsigned int16 Arg_0x4, float64 Arg_0x5, int64 Arg_0x6, unsigned int64 Arg_0x7)
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
