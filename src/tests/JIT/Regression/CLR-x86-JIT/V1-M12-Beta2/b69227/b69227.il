// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x7af9ed84 {}
.assembly extern xunit.core {}
.class ILGEN_0x7af9ed84 {

.field static	unsigned int32[] field_0x0
.field static	unsigned int16 field_0x1
.field static	unsigned int64[] field_0x2
.field static	unsigned int32[] field_0x3
.field static	int16[] field_0x4
.field static	unsigned int8 field_0x5
.field static	unsigned int64 field_0x6
.field static native int[] field_0x7
.field static	float32[] field_0x8
.field static	int8[] field_0x9
.field static	float32[] field_0xa

.method static int32 Method_0x436202fb() {
.maxstack	11

 .locals (int8 LOCAL_0x0,int16[] LOCAL_0x1)

ldc.i4 0xadbe66d1 
stloc LOCAL_0x0
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc LOCAL_0x1

ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld unsigned int32[] ILGEN_0x7af9ed84::field_0x0
ldc.i4 0xee9c9bbb 
stsfld unsigned int16 ILGEN_0x7af9ed84::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld unsigned int64[] ILGEN_0x7af9ed84::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld unsigned int32[] ILGEN_0x7af9ed84::field_0x3
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x7af9ed84::field_0x4
ldc.i4 0x4bab9395 
stsfld unsigned int8 ILGEN_0x7af9ed84::field_0x5
ldc.i8 0xfb4e6db073698afd 
stsfld unsigned int64 ILGEN_0x7af9ed84::field_0x6
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0x7af9ed84::field_0x7
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x7af9ed84::field_0x8
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0x7af9ed84::field_0x9
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x7af9ed84::field_0xa

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
 ldsfld unsigned int32[] ILGEN_0x7af9ed84::field_0x3
 ldc.i4.3
 ldelema [mscorlib]System.Int32
 ldc.i4 0x44a1120c
 stind.i4
End_Orphan_1:
 ldc.i4.2
 ldc.i4.6
 cgt.un
 conv.u
 ldc.i4.2
 conv.u1
 newarr [mscorlib]System.SByte
 ldlen
 bgt Branch_0x2

 ldsfld unsigned int16 ILGEN_0x7af9ed84::field_0x1
 ldc.i4.2
 bne.un Branch_0x8

 ldc.r4 float32(0x3999f358)
 br Branch_0x9

 Branch_0x8:

 ldc.r8 float64(0xd2c659ed0296c18)
 Branch_0x9:

 ldc.r8 float64(0x4d743b5ed47cbbb)
 call float64 [mscorlib]System.Math::Abs(float64)
 ble Branch_0x6

Start_Orphan_4:
 nop
End_Orphan_4:
 ldc.r4 float32(0xbedd604a)
 conv.r4
 br Branch_0x7

 Branch_0x6:

 ldc.r8 float64(0x2b3fd71172eaa7ad)
 call float64 [mscorlib]System.Math::Abs(float64)
 Branch_0x7:

 br Branch_0x3

 Branch_0x2:

 ldc.i4.8
 conv.ovf.u.un
 ldsfld unsigned int64 ILGEN_0x7af9ed84::field_0x6
 conv.ovf.u4.un
 ble Branch_0xa

 ldc.r4 float32(0xab25d259)
 ldc.r4 float32(0xa924e4a0)
 bge Branch_0xc

 ldc.r4 float32(0xbccce604)
 br Branch_0xd

 Branch_0xc:

 ldc.r4 float32(0x72754c50)
 Branch_0xd:

 br Branch_0xb

 Branch_0xa:

 ldsfld unsigned int8 ILGEN_0x7af9ed84::field_0x5
 ldc.i4.3
 ble.un Branch_0xe

 ldc.r8 float64(0x98d5ab75a8daae6a)
 br Branch_0xf

 Branch_0xe:

 ldc.r8 float64(0x4e7a1b03516aaec9)
 Branch_0xf:

 Branch_0xb:

 Branch_0x3:

 ldsfld unsigned int8 ILGEN_0x7af9ed84::field_0x5
 conv.ovf.i2.un
 ldsfld unsigned int32[] ILGEN_0x7af9ed84::field_0x0
 ldc.i4.8
 ldelema [mscorlib]System.Int32
 ldind.i4
 blt Branch_0x10

 ldc.i4.4
 ldloc LOCAL_0x0
 ble Branch_0x12

 ldc.r4 float32(0xa669362f)
 br Branch_0x13

 Branch_0x12:

 ldc.r4 float32(0xaef6829c)
 Branch_0x13:

 br Branch_0x11

 Branch_0x10:

 ldc.r8 float64(0x568bdb44c846619a)
 ldc.r8 float64(0x8c3f6cdbd0a098cc)
 ble Branch_0x14

 ldc.r8 float64(0xf1d6203c63d4796e)
 br Branch_0x15

 Branch_0x14:

 ldc.r4 float32(0x66ec60cb)
 Branch_0x15:

 Branch_0x11:

 ldc.i8 0x55c1881f6bf32b93
 ldc.i4.2
 shr
 ldsfld unsigned int16 ILGEN_0x7af9ed84::field_0x1
 ldc.i4.8
 bge Branch_0x18

 ldc.i8 0xdf714d172f8e92a1
 br Branch_0x19

 Branch_0x18:

 ldc.i8 0x7cdbc12b5d7dccf5
 Branch_0x19:

 beq Branch_0x16

 ldc.i4.7
 ldc.i4.8
 bge Branch_0x1a

 ldc.r4 float32(0x785cb23f)
 br Branch_0x1b

 Branch_0x1a:

 ldc.r4 float32(0xf220069e)
 Branch_0x1b:

 br Branch_0x17

 Branch_0x16:

Start_Orphan_a:
 nop
End_Orphan_a:
 ldc.i4.3
 ldsfld unsigned int8 ILGEN_0x7af9ed84::field_0x5
 bge Branch_0x1c

 ldc.r8 float64(0x91e9484999c15c45)
 br Branch_0x1d

 Branch_0x1c:

 ldc.r4 float32(0x72fd8475)
 Branch_0x1d:

 Branch_0x17:

 add 
 ble Branch_0x0

 ldsfld unsigned int64[] ILGEN_0x7af9ed84::field_0x2
 stsfld unsigned int64[] ILGEN_0x7af9ed84::field_0x2
 br Branch_0x1

 Branch_0x0:

 ldc.r8 float64(0x9a12ce3c85f3c6d)
 conv.ovf.i4
 brtrue Branch_0x1e

 ldloc LOCAL_0x0
 ldc.i4.2
 blt.un Branch_0x20

 ldc.r4 float32(0xaa429d4c)
 br Branch_0x21

 Branch_0x20:

 ldc.r8 float64(0xb5b4d9172710ccc0)
 Branch_0x21:

 pop
 br Branch_0x1f

 Branch_0x1e:

 ldc.r4 float32(0xe7a182f3)
 ldc.r4 float32(0xc57be134)
 ble.un Branch_0x24

 ldc.i4.2
 br Branch_0x25

 Branch_0x24:

 ldc.i4.2
 Branch_0x25:

 brtrue Branch_0x22

 ldc.i4 0x686f17de
 ldc.i4 0x46b91186
 blt Branch_0x26

 nop
 br Branch_0x27

 Branch_0x26:

 nop
 Branch_0x27:

 br Branch_0x23

 Branch_0x22:

 ldc.i4.1
 ldc.i4.5
 blt.un Branch_0x28

 nop
 br Branch_0x29

 Branch_0x28:

 nop
 Branch_0x29:

 Branch_0x23:

 Branch_0x1f:

 Branch_0x1:

End_Orphan_0:
 ldc.r4 float32(0x5ce586)
 conv.ovf.u8
 conv.ovf.u1
 ldc.i4.3
 ldsfld unsigned int16 ILGEN_0x7af9ed84::field_0x1
 bgt Branch_0x36

 ldc.r8 float64(0x44d4f2b4aa0c8b32)
 br Branch_0x37

 Branch_0x36:

 ldc.r8 float64(0x1406076577205eb5)
 Branch_0x37:

Start_Orphan_14:
 nop
End_Orphan_14:
 ldc.i4 0x758a96b1
 ldc.i4.3
 bgt.un Branch_0x38

 ldc.r4 float32(0x68e46179)
 br Branch_0x39

 Branch_0x38:

 ldc.r8 float64(0x2f1ba86f75cee6b6)
 Branch_0x39:

 bne.un Branch_0x34

Start_Orphan_15:
 nop
End_Orphan_15:
 ldc.i4.6
 ldc.i4.m1
 cgt.un
 br Branch_0x35

 Branch_0x34:

 ldc.i8 0xbf1ff148e56f5745
 ldsfld unsigned int64 ILGEN_0x7af9ed84::field_0x6
 bge Branch_0x3a

 ldc.i4.m1
 br Branch_0x3b

 Branch_0x3a:

 ldc.i4.4
 Branch_0x3b:

 Branch_0x35:

 xor
Start_Orphan_16:
 ldc.i8 0x2b1acf22a8c55993
 ldsfld unsigned int64 ILGEN_0x7af9ed84::field_0x6
 bgt.un Branch_0x3e

 ldc.r4 float32(0x65f5863d)
 br Branch_0x3f

 Branch_0x3e:

 ldc.r8 float64(0x7a5e5e84f67392af)
 Branch_0x3f:

Start_Orphan_17:
 nop
End_Orphan_17:
 ldc.r8 float64(0x953565f6712387d0)
 neg
 bge Branch_0x3c

 ldsfld unsigned int16 ILGEN_0x7af9ed84::field_0x1
 ldc.i4.3
 blt.un Branch_0x40

 nop
 br Branch_0x41

 Branch_0x40:

 nop
 Branch_0x41:

 br Branch_0x3d

 Branch_0x3c:

 ldc.i8 0x2dcadf91fe9c7074
 stsfld unsigned int64 ILGEN_0x7af9ed84::field_0x6
 Branch_0x3d:

End_Orphan_16:
 ldc.i4.0
 ldc.i4.s 105
 ceq
 ldc.r8 float64(0xe674f694c96c7e75)
 conv.ovf.u2
 sub
 conv.u2
 add
 ldc.i4.5
 conv.i2
 ldc.i4.4
 ldsfld unsigned int16 ILGEN_0x7af9ed84::field_0x1
 mul.ovf
 blt Branch_0x42

 ldsfld native int[] ILGEN_0x7af9ed84::field_0x7
 ldc.i4.1
 ldelem.i
 br Branch_0x43

 Branch_0x42:

 ldloc LOCAL_0x0
 ldc.i4.m1
 add.ovf.un
 Branch_0x43:

 conv.ovf.i4
 conv.u1
 bgt.un Branch_0x2a

 ldsfld unsigned int8 ILGEN_0x7af9ed84::field_0x5
 br Branch_0x2b

 Branch_0x2a:

 ldc.i4.8
 ldc.i4.0
 blt.un Branch_0x48

 ldc.i4.5
 br Branch_0x49

 Branch_0x48:

 ldc.i4.2
 Branch_0x49:

Start_Orphan_1d:
 nop
End_Orphan_1d:
 ldc.r8 float64(0xb405cf5526e39b3)
 ldc.r8 float64(0xd282ad79665398f9)
 blt Branch_0x4a

 ldc.i4.3
 br Branch_0x4b

 Branch_0x4a:

 ldsfld unsigned int8 ILGEN_0x7af9ed84::field_0x5
 Branch_0x4b:

 shr.un
 ldc.i4.3
 ldc.i4 0x4b3fd2e2
 bge Branch_0x4c

 ldc.i4 0x7fab8a4d
 br Branch_0x4d

 Branch_0x4c:

 ldc.i4.s -11
 Branch_0x4d:

 conv.u
 bge Branch_0x46

 ldc.i4.2
 ldloc LOCAL_0x0
 div
 ldc.i4.8
 ldc.i4.m1
 cgt
 bne.un Branch_0x50

 ldc.r4 float32(0xb377b644)
 ldc.r8 float64(0xe07da2007e1dbb01)
 bgt.un Branch_0x52

 ldc.r8 float64(0xde0aade5bcd30e1b)
 br Branch_0x53

 Branch_0x52:

 ldc.r4 float32(0xc8c024ab)
 Branch_0x53:

 br Branch_0x51

 Branch_0x50:

 ldc.r4 float32(0xd0bbc70f)
 ckfinite
 Branch_0x51:

 br Branch_0x47

 Branch_0x46:

 ldc.i4.2
Start_Orphan_22:
 nop
End_Orphan_22:
 ldc.i8 0xb3016ac9470b6068
 ldc.i8 0x4ed509fad2404c58
 cgt
 bge Branch_0x56

Start_Orphan_23:
 nop
End_Orphan_23:
 ldloc LOCAL_0x0
 ldsfld unsigned int16 ILGEN_0x7af9ed84::field_0x1
 bgt Branch_0x58

 ldc.r8 float64(0x881883e4a4e65dd3)
 br Branch_0x59

 Branch_0x58:

 ldc.r8 float64(0x94bc4b0f8d724ff1)
 Branch_0x59:

 br Branch_0x57

 Branch_0x56:

 ldc.i4.7
 ldc.i4.0
 bgt.un Branch_0x5a

 ldc.r8 float64(0x2f7721e484f5021c)
 br Branch_0x5b

 Branch_0x5a:

 ldc.r8 float64(0x66ac64f8bee0b15)
 Branch_0x5b:

 Branch_0x57:

 Branch_0x47:

 ldc.i4.5
 ldc.i4.m1
 bgt.un Branch_0x60

 ldsfld unsigned int64 ILGEN_0x7af9ed84::field_0x6
 br Branch_0x61

 Branch_0x60:

 ldsfld unsigned int64 ILGEN_0x7af9ed84::field_0x6
 Branch_0x61:

Start_Orphan_28:
 nop
End_Orphan_28:
 ldsflda unsigned int64 ILGEN_0x7af9ed84::field_0x6
 ldind.u8
 div
 ldsfld unsigned int64 ILGEN_0x7af9ed84::field_0x6
 ldc.i8 0xdc244203ce1d6505
 sub
Start_Orphan_2b:
 nop
End_Orphan_2b:
 ldc.i8 0xe171c397c5266c59
 ldsfld unsigned int64 ILGEN_0x7af9ed84::field_0x6
 div
 mul.ovf.un
 blt Branch_0x5c

Start_Orphan_2c:
 nop
End_Orphan_2c:
 ldloc LOCAL_0x0
 ldc.i4.7
 bne.un Branch_0x64

 ldc.i4.2
 br Branch_0x65

 Branch_0x64:

 ldc.i4.8
 Branch_0x65:

Start_Orphan_2d:
 nop
End_Orphan_2d:
 ldsfld native int[] ILGEN_0x7af9ed84::field_0x7
 ldc.i4.4
 ldelem.i
 blt Branch_0x62

Start_Orphan_2e:
 nop
End_Orphan_2e:
 ldc.r4 float32(0x17ee541c)
 ldc.r8 float64(0xceb1ec75700012d7)
 blt Branch_0x66

 ldc.r8 float64(0x2456ea235e56f5a)
 br Branch_0x67

 Branch_0x66:

 ldc.r8 float64(0xf0f39b4a51f4d6ec)
 Branch_0x67:

 br Branch_0x63

 Branch_0x62:

Start_Orphan_2f:
 nop
End_Orphan_2f:
 ldc.i4.s 22
 ldc.i4 0xa107d116
 bge.un Branch_0x68

 ldc.r4 float32(0x8ecd182a)
 br Branch_0x69

 Branch_0x68:

 ldc.r8 float64(0x372c3608fde5de5)
 Branch_0x69:

 Branch_0x63:

 br Branch_0x5d

 Branch_0x5c:

 ldc.r4 float32(0x15746f07)
 ldc.r4 float32(0x5defdcdc)
 mul 
 call float64 [mscorlib]System.Math::Sin(float64)
 Branch_0x5d:

 ble.un Branch_0x44

 ldc.i4 0xc6f11232
Start_Orphan_31:
 nop
End_Orphan_31:
 ldc.i4.7
 ldsfld unsigned int16 ILGEN_0x7af9ed84::field_0x1
 blt Branch_0x6e

 ldc.i4.s 96
 br Branch_0x6f

 Branch_0x6e:

 ldc.i4 0x5fa69b87
 Branch_0x6f:

 sub
 ldc.i4.8
 brtrue Branch_0x72

 ldc.i4.1
 br Branch_0x73

 Branch_0x72:

 ldc.i4.1
 Branch_0x73:

 ldc.i4.s -126
 ldloc LOCAL_0x0
 blt.un Branch_0x74

 ldc.i4.3
 br Branch_0x75

 Branch_0x74:

 ldc.i4.8
 Branch_0x75:

 ceq
 bge.un Branch_0x6c

 ldc.i4.2
 conv.u8
 conv.ovf.u2.un
 br Branch_0x6d

 Branch_0x6c:

 ldsfld unsigned int64 ILGEN_0x7af9ed84::field_0x6
 ldsfld unsigned int64 ILGEN_0x7af9ed84::field_0x6
 blt.un Branch_0x76

 ldc.i8 0x94223f3f495db0c0
 br Branch_0x77

 Branch_0x76:

 ldc.i8 0x24c4b5f6a8b7c0b2
 Branch_0x77:

 conv.ovf.i1
 Branch_0x6d:

 br Branch_0x45

 Branch_0x44:

 ldc.i4.8
 ldc.i4.7
 xor
Start_Orphan_37:
 nop
End_Orphan_37:
 ldc.r8 float64(0xa98e6652812e6891)
 conv.i2
 cgt.un
 ldsfld unsigned int64 ILGEN_0x7af9ed84::field_0x6
 conv.ovf.i1.un
 ldc.i4.2
 not
 ble Branch_0x7e

 ldc.i4.5
 ldc.i4.2
 add.ovf
 br Branch_0x7f

 Branch_0x7e:

Start_Orphan_39:
 nop
End_Orphan_39:
 ldc.i4.0
 conv.ovf.i2.un
 Branch_0x7f:

pop
pop
ldc.i4.7
 ldloc LOCAL_0x0
 clt.un
 Branch_0x7b:

 Branch_0x45:

 Branch_0x2b:

EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {

call int32 ILGEN_0x7af9ed84::Method_0x436202fb()
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
