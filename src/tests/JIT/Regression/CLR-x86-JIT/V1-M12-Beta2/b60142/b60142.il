// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b60142' {}
.assembly extern xunit.core {}
.class ILGEN_0x8fd0798a {

.field static	unsigned int64 field_0x0
.field static native int[] field_0x1
.field static	unsigned int8 field_0x2
.field static	unsigned int64[] field_0x3

.method static int32 Method_0xf3564a4f(native int Arg_0x0, int32 Arg_0x1, int8 Arg_0x2, float32 Arg_0x3, unsigned int16 Arg_0x4, float32 Arg_0x5) {
.maxstack	15

 .locals (int32[] LOCAL_0x0,float32[] LOCAL_0x1,unsigned int32[] LOCAL_0x2)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc LOCAL_0x0
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc LOCAL_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc LOCAL_0x2

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i8 0x22cf99f0a4ec7c9b 
stsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0x8fd0798a::field_0x1
ldc.i4 0xbcf99024 
stsfld unsigned int8 ILGEN_0x8fd0798a::field_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x8fd0798a::field_0x3

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
 ldarg Arg_0x1
 ldarg Arg_0x0
 mul.ovf.un
 Branch_0x57:

 ldarg Arg_0x0
 ldc.i4.7
 beq Branch_0x5c

 ldarg Arg_0x0
 br Branch_0x5d

 Branch_0x5c:

 ldarg Arg_0x0
 Branch_0x5d:

 pop
 pop
 
 ldc.r8 float64(0x740d8a632973f1a8)
 Branch_0x65:

 conv.i1
 Branch_0x55:

 Branch_0x19:

 ldc.i4.2
 ldc.r8 float64(0x5b7993c214ebb0a8)
 conv.i8
 Branch_0x71:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 Branch_0x77:

Start_Orphan_35:
 nop
End_Orphan_35:
 ldc.i8 0x7b003104a647da0b
 div.un
 cgt
pop
pop

Start_Orphan_3e:
 nop
End_Orphan_3e:

Start_Orphan_41:
 nop
End_Orphan_41:
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 mul
 conv.ovf.i1.un
 Branch_0x87:

 Branch_0x67:

pop
pop

Start_Orphan_54:
Start_Orphan_55:
 nop
End_Orphan_55:
 ldc.i4 0x74dca5d9
 conv.u1
 newarr [mscorlib]System.Int32
 stloc LOCAL_0x0
End_Orphan_54:
Start_Orphan_56:
 ldarg Arg_0x0
 brtrue Branch_0xba

 nop
 br Branch_0xbb

 Branch_0xba:

 nop
 Branch_0xbb:

End_Orphan_56:
 ldc.i4.6
 ldarg Arg_0x2
 bge Branch_0xbc

 ldarg Arg_0x0
 br Branch_0xbd

 Branch_0xbc:

 ldarg Arg_0x0
 Branch_0xbd:

 conv.ovf.i2.un
 neg
Start_Orphan_57:
 nop
End_Orphan_57:
 ldc.i8 0x755e50e447871c4b
 ldarg Arg_0x4
 shr
 conv.u1
 ldc.r8 float64(0x62d5f37670efdb3b)
 ldc.r4 float32(0x99f64e07)
 bgt Branch_0xc0

 ldarg Arg_0x0
 br Branch_0xc1

 Branch_0xc0:

 ldarg Arg_0x0
 Branch_0xc1:

 conv.i2
pop
pop

 ldarg Arg_0x2
 ldarg Arg_0x0
 cgt.un
 ldc.i8 0xfe349e9954359a0e
 conv.ovf.i1
 beq Branch_0xcc

 ldc.i4.3
 conv.ovf.u1.un
 br Branch_0xcd

 Branch_0xcc:

Start_Orphan_59:
 nop
End_Orphan_59:
 ldarg Arg_0x0
 ldarg Arg_0x0
 bge.un Branch_0xce

 ldc.i4.s 114
 br Branch_0xcf

 Branch_0xce:

 ldarg Arg_0x4
 Branch_0xcf:

 Branch_0xcd:

 Branch_0xbf:

 bgt.un Branch_0xb8

Start_Orphan_5a:
 ldarga Arg_0x2
 pop
End_Orphan_5a:
Start_Orphan_5b:
 nop
End_Orphan_5b:
 ldarg Arg_0x0
 ldarg Arg_0x0
 bgt.un Branch_0xd0

 ldc.r4 float32(0x73fc6721)
 br Branch_0xd1

 Branch_0xd0:

 ldc.r4 float32(0xb2d04600)
 Branch_0xd1:

 conv.u4
 ldc.r4 float32(0x6b5ccf85)
 ldc.r4 float32(0xd7fc3a17)
 bne.un Branch_0xd4

 ldc.i8 0xdef041d3756ec9fd
 br Branch_0xd5

 Branch_0xd4:

 ldc.i8 0x63d463b966e548e2
 Branch_0xd5:

Start_Orphan_5c:
 nop
End_Orphan_5c:
 ldarg Arg_0x2
 ldarg Arg_0x1
 beq Branch_0xd6

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 br Branch_0xd7

 Branch_0xd6:

 ldc.i8 0xbc342fc72523b1f6
 Branch_0xd7:

 blt.un Branch_0xd2

 ldarg Arg_0x0
 ldarg Arg_0x0
 bne.un Branch_0xd8

 ldc.i4.3
 br Branch_0xd9

 Branch_0xd8:

 ldarg Arg_0x1
 Branch_0xd9:

 br Branch_0xd3

 Branch_0xd2:

Start_Orphan_5d:
 nop
End_Orphan_5d:
 ldc.r4 float32(0xb3c7e966)
 ldc.r8 float64(0xb7ea029b92f125f7)
 bne.un Branch_0xda

 ldc.i4.5
 br Branch_0xdb

 Branch_0xda:

 ldc.i4.5
 Branch_0xdb:

 Branch_0xd3:

 xor
 br Branch_0xb9

 Branch_0xb8:

Start_Orphan_5e:
 nop
End_Orphan_5e:
 ldc.r4 float32(0x46154213)
 ldc.r8 float64(0x45217129544541f2)
 ble Branch_0xdc

 ldarg Arg_0x0
 br Branch_0xdd

 Branch_0xdc:

 ldarg Arg_0x0
 Branch_0xdd:

 conv.ovf.u4.un
 conv.u4
 Branch_0xb9:

 Branch_0x17:

Start_Orphan_5f:
Start_Orphan_60:
 ldarg Arg_0x0
 ldarg Arg_0x0
 blt.un Branch_0xe2

 nop
 br Branch_0xe3

 Branch_0xe2:

 nop
 Branch_0xe3:

End_Orphan_60:
Start_Orphan_61:
 nop
End_Orphan_61:
 ldc.r8 float64(0x2766a8e5b64c7d58)
 conv.ovf.i1
 ldarg Arg_0x0
 ldc.i4.7
 blt Branch_0xe6

 ldc.i4.3
 br Branch_0xe7

 Branch_0xe6:

 ldc.i4.1
 Branch_0xe7:

 blt.un Branch_0xe4

Start_Orphan_62:
 nop
End_Orphan_62:
 ldarg Arg_0x2
 conv.ovf.i8.un
 br Branch_0xe5

 Branch_0xe4:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldc.i8 0x2ca2d77b76c3986a
 add
 Branch_0xe5:

 ldc.i4.3
 conv.u1
 newarr [mscorlib]System.UInt64
Start_Orphan_63:
 nop
End_Orphan_63:
 ldc.i4.5
 ldc.i4.1
 cgt
 ldelem.u8
 bge Branch_0xe0

 ldc.i4.8
 ldc.i4.2
 cgt.un
 brtrue Branch_0xe8

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 bgt Branch_0xea

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 br Branch_0xeb

 Branch_0xea:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 Branch_0xeb:

 br Branch_0xe9

 Branch_0xe8:

 ldarg Arg_0x0
 ldarg Arg_0x0
 bne.un Branch_0xec

 ldc.i8 0x4ad04eec415ce664
 br Branch_0xed

 Branch_0xec:

 ldc.i8 0x3421934d5867402
 Branch_0xed:

 Branch_0xe9:

 br Branch_0xe1

 Branch_0xe0:

 ldc.i4.m1
 ldarg Arg_0x0
 blt.un Branch_0xee

 ldc.i8 0x4702f8972adc73f2
 br Branch_0xef

 Branch_0xee:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 Branch_0xef:

 ldarg Arg_0x0
 brfalse Branch_0xf0

 ldc.i8 0xc355f281272c9846
 br Branch_0xf1

 Branch_0xf0:

 ldc.i8 0xf4a68ae192f84c35
 Branch_0xf1:

 add.ovf
 Branch_0xe1:

Start_Orphan_64:
Start_Orphan_65:
 nop
End_Orphan_65:
 ldarg Arg_0x0
 ldc.i4.3
 bne.un Branch_0xf4

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 br Branch_0xf5

 Branch_0xf4:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 Branch_0xf5:

Start_Orphan_66:
 nop
End_Orphan_66:
 ldarg Arg_0x0
 ldarg Arg_0x0
 blt.un Branch_0xf6

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 br Branch_0xf7

 Branch_0xf6:

 ldc.i8 0x5eebdf7418bba28e
 Branch_0xf7:

 beq Branch_0xf2

 ldarg Arg_0x5
 ldc.r4 float32(0x75b9b09c)
 ble.un Branch_0xf8

 nop
 br Branch_0xf9

 Branch_0xf8:

 nop
 Branch_0xf9:

 br Branch_0xf3

 Branch_0xf2:

 ldloc LOCAL_0x2
 ldc.i4.1
 ldelema [mscorlib]System.UInt32
 ldc.i4.8
 stind.i4
 Branch_0xf3:

End_Orphan_64:
Start_Orphan_67:
 ldarg Arg_0x0
 ldarg Arg_0x0
 bgt Branch_0xfc

 nop
 br Branch_0xfd

 Branch_0xfc:

 nop
 Branch_0xfd:

End_Orphan_67:
 ldc.r8 float64(0x75fa466fb04b0191)
 ldc.r8 float64(0xf4af7cfe58b26bdc)
 div 
 ldarga Arg_0x3
 ldind.r4
 ble.un Branch_0xfe

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 blt Branch_0x100

 ldarg Arg_0x3
 br Branch_0x101

 Branch_0x100:

 ldarg Arg_0x3
 Branch_0x101:

 br Branch_0xff

 Branch_0xfe:

Start_Orphan_68:
 nop
End_Orphan_68:
 ldc.i4.6
 ldc.i4.3
 ble Branch_0x102

 ldarg Arg_0x5
 br Branch_0x103

 Branch_0x102:

 ldc.r4 float32(0xe9ea496e)
 Branch_0x103:

 Branch_0xff:

Start_Orphan_69:
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldc.i8 0xe09b2e6232a4441b
 beq Branch_0x104

 nop
 br Branch_0x105

 Branch_0x104:

 nop
 Branch_0x105:

End_Orphan_69:
 ldc.i4.0
 ldarg Arg_0x0
 blt Branch_0x108

 ldc.r8 float64(0xc7726aef64f1a55)
 br Branch_0x109

 Branch_0x108:

 ldarg Arg_0x5
 Branch_0x109:

 ldarg Arg_0x0
 ldarg Arg_0x0
 bge Branch_0x10a

 ldarg Arg_0x3
 br Branch_0x10b

 Branch_0x10a:

 ldc.r8 float64(0xe60edabc5b41422d)
 Branch_0x10b:

 blt Branch_0x106

 ldc.i4.5
 conv.r8
 br Branch_0x107

 Branch_0x106:

Start_Orphan_6a:
 nop
End_Orphan_6a:
 ldarg Arg_0x0
 ldc.i4.1
 bge.un Branch_0x10c

 ldc.r4 float32(0xf16575df)
 br Branch_0x10d

 Branch_0x10c:

 ldc.r4 float32(0xa60c6a81)
 Branch_0x10d:

 Branch_0x107:

 ble Branch_0xfa

Start_Orphan_6b:
 nop
End_Orphan_6b:
 ldarg Arg_0x0
 conv.ovf.i4
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 conv.ovf.i4.un
 bge Branch_0x10e

Start_Orphan_6c:
 nop
End_Orphan_6c:
 ldc.i4.2
 ldarg Arg_0x0
 bge Branch_0x110

 ldc.i8 0x7c984a737076edb9
 br Branch_0x111

 Branch_0x110:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 Branch_0x111:

 br Branch_0x10f

 Branch_0x10e:

 ldc.i4.6
 ldc.i4.2
 bgt.un Branch_0x112

 ldc.i8 0x612acce8a1d925c2
 br Branch_0x113

 Branch_0x112:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 Branch_0x113:

 Branch_0x10f:

 br Branch_0xfb

 Branch_0xfa:

 ldc.i4.8
 ldc.i4.s 38
 ble.un Branch_0x114

 ldarg Arg_0x5
 br Branch_0x115

 Branch_0x114:

 ldc.r8 float64(0x6a580aa991517560)
 Branch_0x115:

 conv.i8
 Branch_0xfb:

 bgt.un Branch_0xde

 ldsflda unsigned int8 ILGEN_0x8fd0798a::field_0x2
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 beq Branch_0x116

 ldarg Arg_0x0
 br Branch_0x117

 Branch_0x116:

 ldarg Arg_0x0
 Branch_0x117:

 conv.i4
 stind.i1
 br Branch_0xdf

 Branch_0xde:

Start_Orphan_6d:
 ldarg Arg_0x2
 ldarg Arg_0x4
 beq Branch_0x11a

 nop
 br Branch_0x11b

 Branch_0x11a:

 nop
 Branch_0x11b:

End_Orphan_6d:
 ldarg Arg_0x0
 ldarg Arg_0x0
 bne.un Branch_0x11e

 ldarg Arg_0x0
 br Branch_0x11f

 Branch_0x11e:

 ldarg Arg_0x0
 Branch_0x11f:

Start_Orphan_6e:
 nop
End_Orphan_6e:
 ldc.i4.m1
 ldc.i4.s -93
 add
 bgt Branch_0x11c

 ldarg Arg_0x0
 ldarg Arg_0x0
 bge Branch_0x120

 ldc.i8 0x34f996e874b2092d
 br Branch_0x121

 Branch_0x120:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 Branch_0x121:

 br Branch_0x11d

 Branch_0x11c:

Start_Orphan_6f:
 nop
End_Orphan_6f:
 ldc.i8 0xe77f70b7e40642f0
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ble.un Branch_0x122

 ldc.i8 0x128dcc0ac5adcb78
 br Branch_0x123

 Branch_0x122:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 Branch_0x123:

 Branch_0x11d:

Start_Orphan_70:
 ldloc LOCAL_0x1
 pop
End_Orphan_70:
Start_Orphan_71:
 nop
End_Orphan_71:
 ldc.i8 0xe6138f9ad36ba38d
 ldc.i8 0xe759f875a05f38cd
 bgt Branch_0x124

 ldarg Arg_0x0
 br Branch_0x125

 Branch_0x124:

 ldarg Arg_0x0
 Branch_0x125:

 conv.ovf.u8
 bge.un Branch_0x118

Start_Orphan_72:
 nop
End_Orphan_72:
 ldc.r8 float64(0xb9a6c63367f20abd)
 conv.ovf.u1
 ldc.i4.3
 ldc.i4.7
 or
 ble Branch_0x126

 ldarg Arg_0x0
 ldarg Arg_0x0
 bge.un Branch_0x128

 nop
 br Branch_0x129

 Branch_0x128:

 nop
 Branch_0x129:

 br Branch_0x127

 Branch_0x126:

 ldarg Arg_0x0
 ldc.i4.0
 bgt.un Branch_0x12a

 nop
 br Branch_0x12b

 Branch_0x12a:

 nop
 Branch_0x12b:

 Branch_0x127:

 br Branch_0x119

 Branch_0x118:

 ldarg Arg_0x3
 ldc.r8 float64(0xa832ce921b2863c2)
 bge Branch_0x12e

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 br Branch_0x12f

 Branch_0x12e:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 Branch_0x12f:

Start_Orphan_73:
 nop
End_Orphan_73:
 ldc.i8 0x59e1ce414d3ad2fc
 ldc.i8 0x53aa06cd68f9af08
 bgt.un Branch_0x130

 ldc.i8 0xeb3539334b21a0c
 br Branch_0x131

 Branch_0x130:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 Branch_0x131:

 bgt.un Branch_0x12c

 ldarg Arg_0x0
 ldc.i4.8
 ble Branch_0x132

 nop
 br Branch_0x133

 Branch_0x132:

 nop
 Branch_0x133:

 br Branch_0x12d

 Branch_0x12c:

 ldsfld native int[] ILGEN_0x8fd0798a::field_0x1
 ldc.i4.5
 ldarg Arg_0x0
 stelem.i
 Branch_0x12d:

 Branch_0x119:

 Branch_0xdf:

End_Orphan_5f:
Start_Orphan_74:
Start_Orphan_75:
 nop
End_Orphan_75:
 ldarg Arg_0x0
 ldc.i4.m1
 clt
 ldarg Arg_0x0
 ldc.i4.1
 bgt.un Branch_0x136

 ldarg Arg_0x0
 br Branch_0x137

 Branch_0x136:

 ldarg Arg_0x0
 Branch_0x137:

 mul.ovf.un
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldc.i8 0x31f6a8ae8e4f828c
 cgt
 brtrue Branch_0x138

Start_Orphan_76:
 nop
End_Orphan_76:
 ldc.i4.3
 ldarg Arg_0x4
 blt Branch_0x13a

 ldarg Arg_0x0
 br Branch_0x13b

 Branch_0x13a:

 ldarg Arg_0x0
 Branch_0x13b:

 br Branch_0x139

 Branch_0x138:

Start_Orphan_77:
 nop
End_Orphan_77:
 ldc.i4.1
 brtrue Branch_0x13c

 ldarg Arg_0x0
 br Branch_0x13d

 Branch_0x13c:

 ldarg Arg_0x0
 Branch_0x13d:

 Branch_0x139:

 ble Branch_0x134

Start_Orphan_78:
 nop
End_Orphan_78:
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldc.i8 0xa8a0bf6afe71657e
 add.ovf
Start_Orphan_79:
 nop
End_Orphan_79:
 ldarg Arg_0x0
 ldarg Arg_0x0
 bge.un Branch_0x140

 ldc.i8 0x579ce029c84ca250
 br Branch_0x141

 Branch_0x140:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 Branch_0x141:

 beq Branch_0x13e

 ldarg Arg_0x0
 ldarg Arg_0x0
 ble Branch_0x142

 nop
 br Branch_0x143

 Branch_0x142:

 nop
 Branch_0x143:

 br Branch_0x13f

 Branch_0x13e:

 ldc.i4.s -2
 ldc.i4.4
 blt Branch_0x144

 nop
 br Branch_0x145

 Branch_0x144:

 nop
 Branch_0x145:

 Branch_0x13f:

 br Branch_0x135

 Branch_0x134:

 ldsfld unsigned int64[] ILGEN_0x8fd0798a::field_0x3
 pop
 Branch_0x135:

End_Orphan_74:
Start_Orphan_7a:
Start_Orphan_7b:
 nop
End_Orphan_7b:
 ldc.i4.0
 conv.ovf.i.un
Start_Orphan_7c:
 nop
End_Orphan_7c:
 ldc.i4.7
 ldarg Arg_0x0
 bge Branch_0x148

 ldc.i4.6
 br Branch_0x149

 Branch_0x148:

 ldc.i4.1
 Branch_0x149:

 bgt Branch_0x146

 ldarg Arg_0x0
 ldarg Arg_0x0
 blt.un Branch_0x14a

 nop
 br Branch_0x14b

 Branch_0x14a:

 nop
 Branch_0x14b:

 br Branch_0x147

 Branch_0x146:

 ldarga Arg_0x4
 ldc.i4.2
 stind.i2
 Branch_0x147:

End_Orphan_7a:
Start_Orphan_7d:
 ldarg Arg_0x0
 ldarg Arg_0x0
 bge Branch_0x14c

 nop
 br Branch_0x14d

 Branch_0x14c:

 nop
 Branch_0x14d:

End_Orphan_7d:
 ldc.i4.7
 ldarg Arg_0x0
 sub
 ldarg Arg_0x4
 ldarg Arg_0x0
 bge Branch_0x150

 ldarg Arg_0x4
 br Branch_0x151

 Branch_0x150:

 ldc.i4.s 112
 Branch_0x151:

 blt Branch_0x14e

Start_Orphan_7e:
 nop
End_Orphan_7e:
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 bgt Branch_0x152

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 br Branch_0x153

 Branch_0x152:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 Branch_0x153:

 br Branch_0x14f

 Branch_0x14e:

Start_Orphan_7f:
 nop
End_Orphan_7f:
 ldsflda unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldind.u8
 Branch_0x14f:

Start_Orphan_80:
 nop
End_Orphan_80:
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldc.i8 0x206eabc75bc0c1b
 rem.un
 ldarg Arg_0x4
 ldarg Arg_0x0
 ble Branch_0x154

 ldc.i8 0x1454bd2ab80dba50
 br Branch_0x155

 Branch_0x154:

 ldc.i8 0xa0a4fae09a395584
 Branch_0x155:

 rem.un
 add
 conv.ovf.u1
Start_Orphan_81:
 ldc.r8 float64(0x276c4916ba7f191e)
 conv.i8
 stsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
End_Orphan_81:
Start_Orphan_82:
 ldc.i8 0x56bad973d857e8e1
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 blt.un Branch_0x158

 nop
 br Branch_0x159

 Branch_0x158:

 nop
 Branch_0x159:

End_Orphan_82:
Start_Orphan_83:
 nop
End_Orphan_83:
 ldc.r4 float32(0xd6b52373)
 ldarg Arg_0x3
 blt Branch_0x15c

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 br Branch_0x15d

 Branch_0x15c:

 ldc.i8 0x69f3d77ae0b0e6d5
 Branch_0x15d:

 ldc.i8 0x90cc92803b28796d
 ldc.i8 0xbc87af252a72994e
 bgt Branch_0x15e

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 br Branch_0x15f

 Branch_0x15e:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 Branch_0x15f:

 bne.un Branch_0x15a

 ldarg Arg_0x0
 neg
 br Branch_0x15b

 Branch_0x15a:

 ldc.i4.2
 conv.ovf.i.un
 Branch_0x15b:

Start_Orphan_84:
 ldloc LOCAL_0x2
 stloc LOCAL_0x2
End_Orphan_84:
Start_Orphan_85:
 nop
End_Orphan_85:
 ldc.i4.m1
 ldc.i4.7
 shr
 ldc.i4.s 83
 ldarg Arg_0x0
 clt.un
 bge.un Branch_0x160

Start_Orphan_86:
 nop
End_Orphan_86:
 ldarg Arg_0x0
 ldarg Arg_0x0
 add
 br Branch_0x161

 Branch_0x160:

Start_Orphan_87:
 nop
End_Orphan_87:
 ldarg Arg_0x0
 ldc.i4.8
 add.ovf.un
 Branch_0x161:

 beq Branch_0x156

 ldarga Arg_0x2
 ldind.i1
 br Branch_0x157

 Branch_0x156:

Start_Orphan_88:
 nop
End_Orphan_88:
 ldc.r4 float32(0x1f0d5d66)
 ckfinite
Start_Orphan_89:
 nop
End_Orphan_89:
 ldsfld unsigned int8 ILGEN_0x8fd0798a::field_0x2
 conv.r.un
 blt.un Branch_0x162

 ldloc LOCAL_0x1
 ldlen
 br Branch_0x163

 Branch_0x162:

 ldc.i4 0xb0d7d9ee
 ldc.i4.2
 div
 Branch_0x163:

 Branch_0x157:

 conv.i4
 shr.un
 blt Branch_0x0

 ldarga Arg_0x4
 pop
 br Branch_0x1

 Branch_0x0:

Start_Orphan_8a:
Start_Orphan_8b:
 nop
End_Orphan_8b:
 ldc.i4.m1
 ldc.i4.4
 or
 conv.u1
 newarr [mscorlib]System.UInt32
Start_Orphan_8c:
 ldc.i4.0
 ldarg Arg_0x0
 blt.un Branch_0x166

 nop
 br Branch_0x167

 Branch_0x166:

 nop
 Branch_0x167:

End_Orphan_8c:
Start_Orphan_8d:
 nop
End_Orphan_8d:
 ldarg Arg_0x0
 ldarg Arg_0x0
 mul
 ldarg Arg_0x0
 ldarg Arg_0x0
 mul.ovf.un
 clt
Start_Orphan_8e:
 nop
End_Orphan_8e:
 ldc.i8 0x8f036b10e8548f61
 ldc.i8 0xb7f6d4573ba2f19e
 bge Branch_0x168

 ldc.i4.8
 br Branch_0x169

 Branch_0x168:

 ldc.i4.6
 Branch_0x169:

 ldc.i4.m1
 ldarg Arg_0x0
 bgt Branch_0x16a

 ldc.i4.7
 br Branch_0x16b

 Branch_0x16a:

 ldarg Arg_0x2
 Branch_0x16b:

 add
 stelem.i4
End_Orphan_8a:
Start_Orphan_8f:
 ldc.i4.7
 ldarg Arg_0x0
 bgt.un Branch_0x16e

 ldc.r8 float64(0xe72a869b42052785)
 br Branch_0x16f

 Branch_0x16e:

 ldarg Arg_0x3
 Branch_0x16f:

 ldc.i8 0x2aab8ff068bec315
 ldc.i8 0x3dee1edc21537c0
 ble Branch_0x170

 ldc.r8 float64(0x6a4b7dad151c0cf3)
 br Branch_0x171

 Branch_0x170:

 ldc.r4 float32(0x33defe44)
 Branch_0x171:

 beq Branch_0x16c

 ldsfld unsigned int64[] ILGEN_0x8fd0798a::field_0x3
 stsfld unsigned int64[] ILGEN_0x8fd0798a::field_0x3
 br Branch_0x16d

 Branch_0x16c:

 ldc.r8 float64(0xf41dd97e8d9fa4b7)
 ldarg Arg_0x3
 bge.un Branch_0x172

 nop
 br Branch_0x173

 Branch_0x172:

 nop
 Branch_0x173:

 Branch_0x16d:

End_Orphan_8f:
Start_Orphan_90:
 ldc.i4 0xa1811003
 ldarg Arg_0x0
 bge.un Branch_0x174

 nop
 br Branch_0x175

 Branch_0x174:

 nop
 Branch_0x175:

End_Orphan_90:
Start_Orphan_91:
 nop
End_Orphan_91:
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 not
Start_Orphan_92:
 nop
End_Orphan_92:
 ldarg Arg_0x0
 ldarg Arg_0x0
 ble.un Branch_0x176

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 br Branch_0x177

 Branch_0x176:

 ldc.i8 0x4b731df674688feb
 Branch_0x177:

 xor
 conv.u8
 ldc.i8 0xa2ffa8b39e3627a
 ldc.i8 0xad37504223679007
 mul
 ldc.i8 0x80349a5db274b3f
 not
 div.un
 ldc.i4.3
 ldc.i4.s -47
 bne.un Branch_0x17a

 ldc.i8 0xfb5afb1f22d9a1a8
 br Branch_0x17b

 Branch_0x17a:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 Branch_0x17b:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 clt.un
 shl
 ble.un Branch_0x178

Start_Orphan_93:
 ldc.i4.2
 pop
End_Orphan_93:
Start_Orphan_94:
 nop
End_Orphan_94:
 ldarg Arg_0x0
 ldarg Arg_0x1
 clt
Start_Orphan_95:
 nop
End_Orphan_95:
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldc.i8 0x547ca715d7688b3
 ble.un Branch_0x17c

 ldc.i4.0
 br Branch_0x17d

 Branch_0x17c:

 ldc.i4.2
 Branch_0x17d:

 shr.un
 br Branch_0x179

 Branch_0x178:

 ldarg Arg_0x2
 Branch_0x179:

 shl
Start_Orphan_96:
Start_Orphan_97:
 nop
End_Orphan_97:
 ldarga Arg_0x2
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 bgt.un Branch_0x17e

 ldc.i4 0x12d83ca7
 br Branch_0x17f

 Branch_0x17e:

 ldc.i4.7
 Branch_0x17f:

 stind.i1
End_Orphan_96:
 ldc.i4.4
Start_Orphan_98:
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ble Branch_0x182

 nop
 br Branch_0x183

 Branch_0x182:

 nop
 Branch_0x183:

End_Orphan_98:
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 conv.ovf.i4
 ldc.i4.m1
 conv.u1
 rem.un
 blt Branch_0x180

Start_Orphan_99:
 nop
End_Orphan_99:
 ldc.i4.8
 conv.ovf.i8.un
Start_Orphan_9a:
 nop
End_Orphan_9a:
 ldsfld unsigned int64[] ILGEN_0x8fd0798a::field_0x3
 ldc.i4.0
 ldelem.u8
 beq Branch_0x184

 ldarg Arg_0x0
 ldc.i4.4
 rem.un
 br Branch_0x185

 Branch_0x184:

Start_Orphan_9b:
 nop
End_Orphan_9b:
 ldarg Arg_0x4
 ldarg Arg_0x0
 ble.un Branch_0x186

 ldarg Arg_0x0
 br Branch_0x187

 Branch_0x186:

 ldarg Arg_0x0
 Branch_0x187:

 Branch_0x185:

 br Branch_0x181

 Branch_0x180:

Start_Orphan_9c:
 nop
End_Orphan_9c:
 ldarg Arg_0x0
 ldarg Arg_0x0
 bge.un Branch_0x18a

 ldc.r8 float64(0x194144cd7bd40ba8)
 br Branch_0x18b

 Branch_0x18a:

 ldc.r8 float64(0xe1601780786cfa8f)
 Branch_0x18b:

 ldarg Arg_0x3
 ldc.r4 float32(0xff6f87a1)
 mul 
 bge Branch_0x188

 ldc.i4.8
 ldarg Arg_0x0
 bge.un Branch_0x18c

 ldarg Arg_0x0
 br Branch_0x18d

 Branch_0x18c:

 ldarg Arg_0x0
 Branch_0x18d:

 br Branch_0x189

 Branch_0x188:

 ldc.r8 float64(0x2fce22595d3933b)
 conv.ovf.u
 Branch_0x189:

 Branch_0x181:

 conv.i8
 bgt.un Branch_0x164

Start_Orphan_9d:
 ldc.i4.2
 conv.u1
 newarr [mscorlib]System.UInt32
 ldc.r4 float32(0x6e90ab0e)
 conv.ovf.i4
 ldarga Arg_0x1
 ldind.i4
 stelem.i4
End_Orphan_9d:
Start_Orphan_9e:
 ldsfld unsigned int64[] ILGEN_0x8fd0798a::field_0x3
 stsfld unsigned int64[] ILGEN_0x8fd0798a::field_0x3
End_Orphan_9e:
Start_Orphan_9f:
 nop
End_Orphan_9f:
 ldc.i8 0xa3333e7496defc
 conv.ovf.u1.un
 ldarg Arg_0x0
 ldarg Arg_0x0
 bne.un Branch_0x194

 ldarg Arg_0x0
 br Branch_0x195

 Branch_0x194:

 ldarg Arg_0x0
 Branch_0x195:

 bgt Branch_0x192

 ldarg Arg_0x5
 ldarg Arg_0x5
 div 
 br Branch_0x193

 Branch_0x192:

 ldarg Arg_0x0
 ldarg Arg_0x0
 blt.un Branch_0x196

 ldc.r8 float64(0x35647d9118ef308)
 br Branch_0x197

 Branch_0x196:

 ldc.r4 float32(0xcbbccdf8)
 Branch_0x197:

 Branch_0x193:

Start_Orphan_a0:
 nop
End_Orphan_a0:
 ldarg Arg_0x0
 conv.r8
Start_Orphan_a1:
 nop
End_Orphan_a1:
 ldarg Arg_0x1
 ldc.i4.m1
 blt Branch_0x19a

 ldarg Arg_0x5
 br Branch_0x19b

 Branch_0x19a:

 ldc.r8 float64(0x384d6d845805dbf)
 Branch_0x19b:

 ble.un Branch_0x198

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ldc.i8 0xc8e3666ca52fc7ff
 bge Branch_0x19c

 ldc.r4 float32(0xa1701803)
 br Branch_0x19d

 Branch_0x19c:

 ldarg Arg_0x3
 Branch_0x19d:

 br Branch_0x199

 Branch_0x198:

 ldc.i8 0x629e9f95f6f8198e
 ldc.i8 0x4a2b0f62cff09cfd
 ble Branch_0x19e

 ldc.r8 float64(0x3ffbc4f168e4d64c)
 br Branch_0x19f

 Branch_0x19e:

 ldc.r4 float32(0x8dad11ae)
 Branch_0x19f:

 Branch_0x199:

 bne.un Branch_0x190

Start_Orphan_a2:
 ldc.r4 float32(0x539df91e)
 ldc.r4 float32(0xbe90280d)
 blt.un Branch_0x1a0

 nop
 br Branch_0x1a1

 Branch_0x1a0:

 nop
 Branch_0x1a1:

End_Orphan_a2:
 ldc.i8 0x8d8f7b5df33b662
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 bge.un Branch_0x1a2

 ldc.i4 0x1d7e67c5
 br Branch_0x1a3

 Branch_0x1a2:

 ldc.i4.3
 Branch_0x1a3:

 ldarg Arg_0x0
 ldarg Arg_0x0
 div
 add
 br Branch_0x191

 Branch_0x190:

Start_Orphan_a3:
 nop
End_Orphan_a3:
 ldc.i4.5
 neg
 ldarg Arg_0x0
 neg
 bne.un Branch_0x1a4

 ldc.i4.5
 ldarg Arg_0x1
 beq Branch_0x1a6

 ldarg Arg_0x0
 br Branch_0x1a7

 Branch_0x1a6:

 ldarg Arg_0x0
 Branch_0x1a7:

 br Branch_0x1a5

 Branch_0x1a4:

Start_Orphan_a4:
 nop
End_Orphan_a4:
 ldarg Arg_0x0
 ldc.i4.1
 div
 Branch_0x1a5:

 Branch_0x191:

Start_Orphan_a5:
 ldsfld native int[] ILGEN_0x8fd0798a::field_0x1
 ldarg Arg_0x2
 ldarg Arg_0x0
 stelem.i
End_Orphan_a5:
Start_Orphan_a6:
 nop
End_Orphan_a6:
 ldarg Arg_0x1
 ldarg Arg_0x0
 sub
 ldc.i8 0x25e72bc8ab8af5f2
 ldc.i8 0x28c00ab84c5e0c3c
 ceq
 blt Branch_0x1a8

Start_Orphan_a7:
 nop
End_Orphan_a7:
 ldc.i4.m1
 conv.ovf.u.un
 br Branch_0x1a9

 Branch_0x1a8:

 ldarg Arg_0x5
 conv.i
 Branch_0x1a9:

 conv.i2
 blt.un Branch_0x18e

Start_Orphan_a8:
 ldarg Arg_0x0
 ldarg Arg_0x4
 bge.un Branch_0x1ac

 nop
 br Branch_0x1ad

 Branch_0x1ac:

 nop
 Branch_0x1ad:

End_Orphan_a8:
Start_Orphan_a9:
 nop
End_Orphan_a9:
 ldc.i4.0
 conv.i2
Start_Orphan_aa:
 nop
End_Orphan_aa:
 ldarg Arg_0x4
 ldc.i4.4
 ceq
 bgt.un Branch_0x1ae

 ldc.i8 0x8fdb0537482f1d54
 ldc.i4.5
 shl
 br Branch_0x1af

 Branch_0x1ae:

Start_Orphan_ab:
 nop
End_Orphan_ab:
 ldarg Arg_0x0
 ldarg Arg_0x0
 blt Branch_0x1b0

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 br Branch_0x1b1

 Branch_0x1b0:

 ldc.i8 0xb2276419b3f0b640
 Branch_0x1b1:

 Branch_0x1af:

Start_Orphan_ac:
 nop
End_Orphan_ac:
 ldarg Arg_0x0
 conv.ovf.i4
 conv.ovf.u8
 blt Branch_0x1aa

 ldsfld unsigned int64[] ILGEN_0x8fd0798a::field_0x3
 pop
 br Branch_0x1ab

 Branch_0x1aa:

 ldarg Arg_0x0
 ldarg Arg_0x0
 add.ovf
Start_Orphan_ad:
 nop
End_Orphan_ad:
 ldarg Arg_0x4
 ldsfld unsigned int8 ILGEN_0x8fd0798a::field_0x2
 blt Branch_0x1b4

 ldarg Arg_0x0
 br Branch_0x1b5

 Branch_0x1b4:

 ldarg Arg_0x0
 Branch_0x1b5:

 bgt Branch_0x1b2

 ldarga Arg_0x5
 ldc.r4 float32(0x841e9263)
 stind.r4
 br Branch_0x1b3

 Branch_0x1b2:

 ldc.i4 0xdde5ae4b
 ldarg Arg_0x0
 bgt Branch_0x1b6

 nop
 br Branch_0x1b7

 Branch_0x1b6:

 nop
 Branch_0x1b7:

 Branch_0x1b3:

 Branch_0x1ab:

 br Branch_0x18f

 Branch_0x18e:

Start_Orphan_ae:
 nop
End_Orphan_ae:
 ldloc LOCAL_0x2
Start_Orphan_af:
 nop
End_Orphan_af:
 ldarg Arg_0x1
 ldelema [mscorlib]System.UInt32
 pop
 Branch_0x18f:

 br Branch_0x165

 Branch_0x164:

Start_Orphan_b0:
Start_Orphan_b1:
 nop
End_Orphan_b1:
 ldc.r4 float32(0x3b364dbe)
 ldc.r8 float64(0x31d44f8cf1210f2f)
 bge Branch_0x1ba

 ldarg Arg_0x0
 br Branch_0x1bb

 Branch_0x1ba:

 ldarg Arg_0x0
 Branch_0x1bb:

Start_Orphan_b2:
 nop
End_Orphan_b2:
 ldc.i4.1
 conv.i
 blt.un Branch_0x1b8

 ldc.r4 float32(0x17ac7f86)
 ldc.r8 float64(0x8e0d3bc13373881e)
 bgt.un Branch_0x1bc

 nop
 br Branch_0x1bd

 Branch_0x1bc:

 nop
 Branch_0x1bd:

 br Branch_0x1b9

 Branch_0x1b8:

 ldarga Arg_0x4
 pop
 Branch_0x1b9:

End_Orphan_b0:
Start_Orphan_b3:
 ldc.i8 0x3fceadd0bdc830a0
 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 ble Branch_0x1be

 nop
 br Branch_0x1bf

 Branch_0x1be:

 nop
 Branch_0x1bf:

End_Orphan_b3:
 ldarg Arg_0x5
 ldc.r4 float32(0x8fcbae87)
 ldarg Arg_0x3
 blt Branch_0x1c2

 ldarg Arg_0x3
 br Branch_0x1c3

 Branch_0x1c2:

 ldc.r4 float32(0x52a80b1e)
 Branch_0x1c3:

 ble.un Branch_0x1c0

 ldarg Arg_0x0
 brfalse Branch_0x1c4

 ldc.i4.6
 br Branch_0x1c5

 Branch_0x1c4:

 ldc.i4.5
 Branch_0x1c5:

 br Branch_0x1c1

 Branch_0x1c0:

 ldc.i4.4
 ldc.i4.8
 blt.un Branch_0x1c6

 ldc.i4.5
 br Branch_0x1c7

 Branch_0x1c6:

 ldc.i4.5
 Branch_0x1c7:

 Branch_0x1c1:

 conv.u1
 newarr [mscorlib]System.UInt32
 ldarg Arg_0x0
 ldarg Arg_0x0
 bgt Branch_0x1d2

 ldc.r4 float32(0xd260c8e0)
 br Branch_0x1d3

 Branch_0x1d2:

 ldc.r4 float32(0x122b97a1)
 Branch_0x1d3:

 ldarg Arg_0x3
 ldc.r4 float32(0x70746984)
 bgt.un Branch_0x1d4

 ldc.r8 float64(0x10b9b7edfb69bc96)
 br Branch_0x1d5

 Branch_0x1d4:

 ldc.r8 float64(0x5852f731cd0751fb)
 Branch_0x1d5:

 bgt.un Branch_0x1d0

 ldc.i4.0
 ldarg Arg_0x0
 blt Branch_0x1d6

 ldc.i4.1
 br Branch_0x1d7

 Branch_0x1d6:

 ldarg Arg_0x2
 Branch_0x1d7:

 br Branch_0x1d1

 Branch_0x1d0:

 ldsfld unsigned int64 ILGEN_0x8fd0798a::field_0x0
 conv.ovf.i2
 Branch_0x1d1:

 ldarg Arg_0x1
 ldarg Arg_0x0
 add.ovf
 ldc.i4 0x7e3e4026
 ldc.i4 0x9613f190
 beq Branch_0x1d8

 ldc.i4.4
 br Branch_0x1d9

 Branch_0x1d8:

 ldc.i4.0
 Branch_0x1d9:

 div.un
 clt
 ldarga Arg_0x4
 ldind.u2
 ldc.r8 float64(0x13545c6fb17b1879)
 ldarg Arg_0x5
 bge Branch_0x1e0

 ldarg Arg_0x0
 br Branch_0x1e1

 Branch_0x1e0:

 ldarg Arg_0x0
 Branch_0x1e1:

 blt.un Branch_0x1de

 ldc.i4.3
 br Branch_0x1df

 Branch_0x1de:

ldc.i4.5
 ldc.i4.4
 bge.un Branch_0x1e2

 ldc.i4.1
 br Branch_0x1e3

 Branch_0x1e2:

 ldarg Arg_0x1
 Branch_0x1e3:

 Branch_0x1df:
 ldarg Arg_0x0
 conv.ovf.i4
 conv.i4
 beq Branch_0x1da

 ldc.i4.1
 ldc.i4.2
 bge.un Branch_0x1e6

 ldarg Arg_0x5
 br Branch_0x1e7

 Branch_0x1e6:

 ldc.r4 float32(0xfba9af80)
 Branch_0x1e7:

 ldc.r4 float32(0x22e4a0c9)
 ldc.r8 float64(0x3da92886bd3df3dd)
 bgt Branch_0x1e8

 ldc.r8 float64(0xe5d7d00d6050af24)
 br Branch_0x1e9

 Branch_0x1e8:

 ldc.r8 float64(0xd5560df771aa7ee7)
 Branch_0x1e9:

 bge Branch_0x1e4

 ldc.r4 float32(0xfb28f5f9)
 ldarg Arg_0x5
 bge Branch_0x1ea

 ldc.i4.5
 br Branch_0x1eb

 Branch_0x1ea:

 ldc.i4 0x2d0b0f8c
 Branch_0x1eb:

 br Branch_0x1e5

 Branch_0x1e4:

 ldarg Arg_0x0
 conv.ovf.u2.un
 Branch_0x1e5:

 br Branch_0x1db

 Branch_0x1da:

 ldarg Arg_0x0
 conv.ovf.i1
 conv.ovf.u2.un
 Branch_0x1db:

 stelem.i4
 Branch_0x165:

 Branch_0x1:

End_Orphan_0:
ldc.i4 55

EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0xee2e7455 conv.i
ldc.i4 0x329cf05e 
ldc.i4 0x39eb160d 
ldc.r4 float32(0x9770fe90) 
ldc.i4 0x4115daca 
ldc.r4 float32(0x7bcd9aaa) 

call int32 ILGEN_0x8fd0798a::Method_0xf3564a4f(native int Arg_0x0, int32 Arg_0x1, int8 Arg_0x2, float32 Arg_0x3, unsigned int16 Arg_0x4, float32 Arg_0x5)
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
