// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x2193e4b5 {}
.class ILGEN_0x2193e4b5 {
.field static	unsigned int64 field_0x0
.field static	unsigned int8 field_0x1

.method static int32 Method_0xeffa27b5(unsigned int8 Arg_0x0, int64 Arg_0x1, float32 Arg_0x2, float32 Arg_0x3, int8 Arg_0x4, float64 Arg_0x5, unsigned int32 Arg_0x6, unsigned int8 Arg_0x7, int16 Arg_0x8) {
.maxstack	11

 .locals (int32 LOCAL_0x0,int64 LOCAL_0x1,unsigned int32[] LOCAL_0x2)

ldc.i4 0x3957a6ec 
stloc LOCAL_0x0
ldc.i8 0x2e4231f3ac10d76c 
stloc LOCAL_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc LOCAL_0x2

ldc.i8 0x8c463c274846f3e7 
stsfld unsigned int64 ILGEN_0x2193e4b5::field_0x0
ldc.i4 0x7023db82 
stsfld unsigned int8 ILGEN_0x2193e4b5::field_0x1

 ldloc LOCAL_0x1
 ldc.i4.0
 ldc.i4.4
 clt.un
 ldc.i4.m1
 ldloc LOCAL_0x0
 bge.un Branch_0x5c

 ldc.i4.0
 br Branch_0x5d

 Branch_0x5c:

 ldc.i4.m1
 Branch_0x5d:

 ble.un Branch_0x5a

 ldc.i4.s 30
 ldc.i4.3
 bgt.un Branch_0x5e

 ldarg Arg_0x8
 br Branch_0x5f

 Branch_0x5e:

 ldarg Arg_0x8
 Branch_0x5f:

 br Branch_0x5b

 Branch_0x5a:

 ldloc LOCAL_0x1
 ldsfld unsigned int64 ILGEN_0x2193e4b5::field_0x0
 beq Branch_0x60

 ldc.i4.6
 br Branch_0x61

 Branch_0x60:

 ldc.i4.m1
 Branch_0x61:

 Branch_0x5b:

 ldarga Arg_0x6
 ldind.u4
 ldarg Arg_0x6
 conv.ovf.i4.un
 div.un
 bge Branch_0x58

 ldc.i4 0x8b441ad5
 ldc.i4.s 77
 bgt.un Branch_0x64

 ldc.i4.4
 br Branch_0x65

 Branch_0x64:

 ldc.i4.5
 Branch_0x65:

 conv.ovf.i
 br Branch_0x59

 Branch_0x58:

 ldc.i4.2
 ldc.i4.4
 bge.un Branch_0x68

 ldc.i4.4
 br Branch_0x69

 Branch_0x68:

 ldsfld unsigned int8 ILGEN_0x2193e4b5::field_0x1
 Branch_0x69:

 ldc.i4.5
 ldarg Arg_0x4
 or
 bge.un Branch_0x66

 ldc.i4 0x8d22c26f
 ldc.i4 0x4d7fdbbe
 bgt Branch_0x6a

 ldc.i4.m1
 br Branch_0x6b

 Branch_0x6a:

 ldc.i4.7
 Branch_0x6b:

 br Branch_0x67

 Branch_0x66:

 ldc.i4.m1
 ldc.i4.0
 mul.ovf
 Branch_0x67:

 Branch_0x59:

Start_Orphan_1f:
 ldarga Arg_0x3
 ldc.i4.4
 ldc.i4.0
 blt Branch_0x6c

 ldarg Arg_0x5
 br Branch_0x6d

 Branch_0x6c:

 ldarg Arg_0x5
 Branch_0x6d:

 stind.r4
End_Orphan_1f:
 ldc.r4 float32(0x44db96f)
 ldarg Arg_0x2
 ble Branch_0x70

 ldc.r8 float64(0x4ee17a60416cb151)
 br Branch_0x71

 Branch_0x70:

 ldc.r4 float32(0xf2257f6)
 Branch_0x71:

 ldarg Arg_0x8
 ldc.i4.1
 bge.un Branch_0x72

 ldc.r4 float32(0x8a72068d)
 br Branch_0x73

 Branch_0x72:

 ldc.r8 float64(0x4d12f0ceb44cba3f)
 Branch_0x73:

 blt Branch_0x6e

Start_Orphan_21:
 nop
End_Orphan_21:
 ldc.i4.7
 ldc.i4.s -32
 beq Branch_0x74

 ldarg Arg_0x7
 br Branch_0x75

 Branch_0x74:

 ldarg Arg_0x7
 Branch_0x75:

 br Branch_0x6f

 Branch_0x6e:

 ldc.i4.2
 ldc.i4.0
 blt Branch_0x76

 ldc.i4.5
 br Branch_0x77

 Branch_0x76:

 ldc.i4.1
 Branch_0x77:

 Branch_0x6f:

Start_Orphan_22:
 nop
End_Orphan_22:
 ldarg Arg_0x3
 ldc.r8 float64(0x261078bcb602e68d)
 blt.un Branch_0x7a

 ldc.i4.s -13
 br Branch_0x7b

 Branch_0x7a:

 ldc.i4.1
 Branch_0x7b:

 ldc.r8 float64(0x8eb3eeea2edaba9f)
 conv.u1
 bgt.un Branch_0x78

Start_Orphan_23:
 nop
End_Orphan_23:
 ldloc LOCAL_0x0
 ldc.i4.1
 div.un
 br Branch_0x79

 Branch_0x78:

Start_Orphan_24:
 nop
End_Orphan_24:
 ldc.i4.6
 ldc.i4.8
 sub.ovf
 Branch_0x79:

 ceq
 beq Branch_0x52

Start_Orphan_25:
 ldc.i4.1
 ldsfld unsigned int8 ILGEN_0x2193e4b5::field_0x1
 bge.un Branch_0x7e

 nop
 br Branch_0x7f

 Branch_0x7e:

 nop
 Branch_0x7f:

End_Orphan_25:
 ldloc LOCAL_0x1
 ldsfld unsigned int64 ILGEN_0x2193e4b5::field_0x0
 ble.un Branch_0x82

 ldc.r4 float32(0x6b34e47c)
 br Branch_0x83

 Branch_0x82:

 ldc.r4 float32(0xc7c85b1)
 Branch_0x83:

 ldarg Arg_0x2
 bne.un Branch_0x80

 ldc.i4 0x25b86e4
 ldc.i4.2
 bge.un Branch_0x84

 ldc.r8 float64(0xfadc32fd4131ced1)
 br Branch_0x85

 Branch_0x84:

 ldc.r4 float32(0xc3d6728b)
 Branch_0x85:

 br Branch_0x81

 Branch_0x80:

 ldloc LOCAL_0x1
 ldloc LOCAL_0x1
 ble Branch_0x86

 ldarg Arg_0x5
 br Branch_0x87

 Branch_0x86:

 ldarg Arg_0x5
 Branch_0x87:

 Branch_0x81:

Start_Orphan_26:
 ldsfld unsigned int64 ILGEN_0x2193e4b5::field_0x0
 ldarg Arg_0x1
 blt.un Branch_0x88

 nop
 br Branch_0x89

 Branch_0x88:

 nop
 Branch_0x89:

End_Orphan_26:
 ldarg Arg_0x8
 ldc.i4.8
 beq Branch_0x8c

 ldc.i4.m1
 br Branch_0x8d

 Branch_0x8c:

 ldc.i4.0
 Branch_0x8d:

 ldc.i8 0x25e5b22dbb3ba152
 ldc.i8 0x227305b3201272a5
 cgt
 bgt.un Branch_0x8a

 ldc.r8 float64(0xeaa6eea260b6c574)
 call float64 [mscorlib]System.Math::Abs(float64)
 br Branch_0x8b

 Branch_0x8a:

 ldarg Arg_0x8
 conv.r8
 Branch_0x8b:

 beq Branch_0x7c

 ldc.i4.6
 ldc.i4.m1
 beq Branch_0x90

 ldc.i4.s 9
 br Branch_0x91

 Branch_0x90:

 ldc.i4.6
 Branch_0x91:

 ldc.i4.m1
 conv.ovf.u
 blt.un Branch_0x8e

 ldc.i4.0
 ldc.i4.8
 ble Branch_0x92

 ldc.i8 0x32cce7582a0350d
 br Branch_0x93

 Branch_0x92:

 ldsfld unsigned int64 ILGEN_0x2193e4b5::field_0x0
 Branch_0x93:

 br Branch_0x8f

 Branch_0x8e:

Start_Orphan_27:
 nop
End_Orphan_27:
 ldc.i4 0xb6dfc404
 conv.i8
 Branch_0x8f:

 br Branch_0x7d

 Branch_0x7c:

 ldloc LOCAL_0x0
 ldc.i4.6
 blt Branch_0x94

 ldloc LOCAL_0x1
 br Branch_0x95

 Branch_0x94:

 ldc.i8 0x3173903ead2924d7
 Branch_0x95:

Start_Orphan_28:
 nop
End_Orphan_28:
 ldc.i4.6
 brfalse Branch_0x96

 ldloc LOCAL_0x1
 br Branch_0x97

 Branch_0x96:

 ldc.i8 0x6dfb5850e0fc78e5
 Branch_0x97:

 rem.un
 Branch_0x7d:

 br Branch_0x53

 Branch_0x52:

Start_Orphan_29:
 ldloc LOCAL_0x2
 ldc.i4 0xd915af6d
 ldc.i4.8
 stelem.i4
End_Orphan_29:
 ldc.i4.3
 ldarg Arg_0x8
 add.ovf
 ldarg Arg_0x8
 ldc.i4.3
 sub
 ble.un Branch_0x98

Start_Orphan_2a:
 nop
End_Orphan_2a:
 ldc.i8 0xd68611dd4d4a5e17
 conv.r.un
 br Branch_0x99

 Branch_0x98:

 ldarg Arg_0x5
 ldc.r8 float64(0x56a5a3f1acf81da9)
 sub  
 Branch_0x99:

 conv.i8
 Branch_0x53:

 bgt Branch_0x36

Start_Orphan_2b:
Start_Orphan_2c:
 ldarg Arg_0x4
 ldc.i4.0
 blt.un Branch_0x9a

 nop
 br Branch_0x9b

 Branch_0x9a:

 nop
 Branch_0x9b:

End_Orphan_2c:
 ldc.r8 float64(0xe64abbd3d73d41f4)
 starg Arg_0x3
End_Orphan_2b:
Start_Orphan_2d:
 ldarga Arg_0x3
Start_Orphan_2e:
 nop
End_Orphan_2e:
 ldarg Arg_0x0
 ldsfld unsigned int8 ILGEN_0x2193e4b5::field_0x1
 bgt Branch_0x9e

 ldarg Arg_0x2
 br Branch_0x9f

 Branch_0x9e:

 ldarg Arg_0x5
 Branch_0x9f:

 stind.r4
End_Orphan_2d:
Start_Orphan_2f:
 ldarga Arg_0x4
 ldloc LOCAL_0x0
 stind.i1
End_Orphan_2f:
Start_Orphan_30:
 nop
End_Orphan_30:
 ldc.i4.7
 conv.u2
 conv.i8
 conv.ovf.u2.un
Start_Orphan_31:
 nop
End_Orphan_31:
 ldsfld unsigned int64 ILGEN_0x2193e4b5::field_0x0
 ldarg Arg_0x1
 xor
Start_Orphan_32:
 nop
End_Orphan_32:
 ldarg Arg_0x0
 conv.ovf.i8.un
 blt Branch_0xa0

 ldsfld unsigned int64 ILGEN_0x2193e4b5::field_0x0
 ldsfld unsigned int64 ILGEN_0x2193e4b5::field_0x0
 and
 br Branch_0xa1

 Branch_0xa0:

Start_Orphan_33:
 nop
End_Orphan_33:
 ldarg Arg_0x7
 conv.ovf.u8.un
 Branch_0xa1:

 conv.ovf.u2
 bge.un Branch_0x9c

Start_Orphan_34:
 ldc.i4.1
 ldc.i4.m1
 ble.un Branch_0xa4

 ldc.i4.5
 br Branch_0xa5

 Branch_0xa4:

 ldc.i4.3
 Branch_0xa5:

 ldc.i4.0
 ldarg Arg_0x0
 bge Branch_0xa6

 ldc.i4.1
 br Branch_0xa7

 Branch_0xa6:

 ldc.i4.5
 Branch_0xa7:

 beq Branch_0xa2

 ldc.i4.5
 stsfld unsigned int8 ILGEN_0x2193e4b5::field_0x1
 br Branch_0xa3

 Branch_0xa2:

 ldarg Arg_0x8
 ldc.i4.s -77
 bge Branch_0xa8

 nop
 br Branch_0xa9

 Branch_0xa8:

 nop
 Branch_0xa9:

 Branch_0xa3:

End_Orphan_34:
Start_Orphan_35:
 ldc.i4.7
 ldarg Arg_0x7
 ble Branch_0xac

 nop
 br Branch_0xad

 Branch_0xac:

 nop
 Branch_0xad:

End_Orphan_35:
Start_Orphan_36:
 nop
End_Orphan_36:
 ldc.i4.5
 ldc.i4.0
 beq Branch_0xae

 ldc.r4 float32(0x9aefb36e)
 br Branch_0xaf

 Branch_0xae:

 ldc.r4 float32(0x8a27fcff)
 Branch_0xaf:

Start_Orphan_37:
 nop
End_Orphan_37:
 ldloc LOCAL_0x0
 ldc.i4.4
 ble.un Branch_0xb0

 ldc.r4 float32(0x376b8e3)
 br Branch_0xb1

 Branch_0xb0:

 ldc.r8 float64(0xa9b1e05fea9042d2)
 Branch_0xb1:

 rem 
 ldc.r4 float32(0x61887f06)
 bge Branch_0xaa

 ldc.i4.6
 ldarg Arg_0x6
 bgt Branch_0xb2

 ldc.i4.s -77
 br Branch_0xb3

 Branch_0xb2:

 ldc.i4.6
 Branch_0xb3:

 conv.i1
 br Branch_0xab

 Branch_0xaa:

Start_Orphan_38:
 nop
End_Orphan_38:
 ldc.r8 float64(0x7fca6724db03244b)
 ldc.r4 float32(0xb907ca4c)
 bgt Branch_0xb4

 ldarg Arg_0x4
 br Branch_0xb5

 Branch_0xb4:

 ldc.i4.0
 Branch_0xb5:

 conv.ovf.i1
 Branch_0xab:

 br Branch_0x9d

 Branch_0x9c:

 ldc.i8 0xb1e0a88137ac2a92
 conv.i8
Start_Orphan_39:
 nop
End_Orphan_39:
 ldsfld unsigned int64 ILGEN_0x2193e4b5::field_0x0
 ldloc LOCAL_0x1
 xor
 ble Branch_0xb6

 ldc.i4.5
 ldc.i4.8
 mul.ovf.un
 br Branch_0xb7

 Branch_0xb6:

 ldc.i4.1
 not
 Branch_0xb7:

 conv.ovf.i2
 Branch_0x9d:

 br Branch_0x37

 Branch_0x36:

 ldc.i4.2
 ldloc LOCAL_0x0
 cgt
 conv.r8
 conv.r4
Start_Orphan_3c:
 ldc.i4.6
 ldc.i4.8
 mul.ovf
 ldc.i4.4
 bge Branch_0xba

 ldarg Arg_0x1
 ldloc LOCAL_0x1
 beq Branch_0xbc

 nop
 br Branch_0xbd

 Branch_0xbc:

 nop
 Branch_0xbd:

 br Branch_0xbb

 Branch_0xba:

 ldc.i4.s -22
 starg Arg_0x6
 Branch_0xbb:

End_Orphan_3c:
 ldarg Arg_0x8
 ldc.i4.1
 conv.u
 ldarg Arg_0x4
 ldc.i4.8
 blt.un Branch_0xc4

 ldc.i4.s 66
 br Branch_0xc5

 Branch_0xc4:

 ldc.i4.3
 Branch_0xc5:

 beq Branch_0xc2

 ldsfld unsigned int64 ILGEN_0x2193e4b5::field_0x0
 conv.ovf.i1.un
 br Branch_0xc3

 Branch_0xc2:

 ldc.i4.4
 ldc.i4.s -124
 ble.un Branch_0xc6

 ldc.i4.2
 br Branch_0xc7

 Branch_0xc6:

 ldc.i4.8
 Branch_0xc7:

 Branch_0xc3:

 bgt Branch_0xbe

 ldc.i4.5
 ldloc LOCAL_0x0
 blt Branch_0xc8

 ldloc LOCAL_0x1
 br Branch_0xc9

 Branch_0xc8:

 ldc.i8 0xde8d57cce6dd6f54
 Branch_0xc9:

 conv.r4
 br Branch_0xbf

 Branch_0xbe:

 ldc.i4.8
 ldc.i4.s -73
 and
 ldc.i4.3
 ldc.i4.7
 sub.ovf
 bne.un Branch_0xca

 ldarg Arg_0x0
 brtrue Branch_0xcc

 ldc.r8 float64(0x888ac4c319853c1)
 br Branch_0xcd

 Branch_0xcc:

 ldarg Arg_0x5
 Branch_0xcd:

 br Branch_0xcb

 Branch_0xca:

 ldarg Arg_0x5
 ldarg Arg_0x5
 blt.un Branch_0xce

 ldc.r4 float32(0x6385deaa)
 br Branch_0xcf

 Branch_0xce:

 ldarg Arg_0x3
 Branch_0xcf:

 Branch_0xcb:

 Branch_0xbf:

 clt
 Branch_0x37:

EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0xc2c0e6e8 
ldc.i8 0x885dc91b9e246766 
ldc.r4 float32(0xbb4f1c4) 
ldc.r4 float32(0xd9b6eff7) 
ldc.i4 0xb5d5870d 
ldc.r8 float64(0x662c23bb8ee47a26) 
ldc.i4 0x679002d4 
ldc.i4 0x8e584689 
ldc.i4 0x721008c8 

call int32 ILGEN_0x2193e4b5::Method_0xeffa27b5(unsigned int8 Arg_0x0, int64 Arg_0x1, float32 Arg_0x2, float32 Arg_0x3, int8 Arg_0x4, float64 Arg_0x5, unsigned int32 Arg_0x6, unsigned int8 Arg_0x7, int16 Arg_0x8)
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
