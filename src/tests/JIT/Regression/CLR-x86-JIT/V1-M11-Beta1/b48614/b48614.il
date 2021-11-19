// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x670a8598 {}
.assembly extern xunit.core {}
.class ILGEN_0x670a8598 {

.field static	unsigned int64 field_0x0
.field static	int64 field_0x1
.field static	float64[] field_0x2
.field static	unsigned int64[] field_0x3

.method static int32 Method_0xdba0534c(float32 Arg_0x0, float64 Arg_0x1, unsigned int16 Arg_0x2, unsigned int32 Arg_0x3, int8 Arg_0x4, unsigned int32 Arg_0x5, int16 Arg_0x6, unsigned int32 Arg_0x7, int64 Arg_0x8) {
.maxstack	13

 .locals init(float32[] local_0x0,float32 local_0x1)

ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x0
ldc.r4 float32(0x35ad42c3) 
stloc local_0x1

ldc.i8 0xa9f1c9079d2d0917 
stsfld unsigned int64 ILGEN_0x670a8598::field_0x0
ldc.i8 0x9c0fdf0e555b28ee 
stsfld int64 ILGEN_0x670a8598::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x670a8598::field_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x670a8598::field_0x3

 ldarg Arg_0x8
 conv.ovf.u.un
 ldc.i4.2
 ldc.i4.1
 ble Branch_0x6

 ldc.i4.8
 br Branch_0x7

 Branch_0x6:

 ldc.i4.0
 Branch_0x7:

 ble.un Branch_0x4

 ldloc local_0x1
 neg
 br Branch_0x5

 Branch_0x4:

 ldc.i4.s -87
 brfalse Branch_0x8

 ldloc local_0x1
 br Branch_0x9

 Branch_0x8:

 ldarg Arg_0x1
 Branch_0x9:

 Branch_0x5:

 ldc.i4 0x83a0f7aa
 ldarg Arg_0x6
 blt Branch_0xa

 ldloc local_0x1
 br Branch_0xb

 Branch_0xa:

 ldarg Arg_0x1
 Branch_0xb:

 call       float64 [mscorlib]System.Math::Round(float64)
 ble Branch_0x2

 ldc.i4.1
 conv.ovf.i4.un
 ldc.r4 float32(0x1070fb98)
 conv.ovf.i1
 ble.un Branch_0xc

 ldc.r8 float64(0x6027669580a05e15)
 br Branch_0xd

 Branch_0xc:

 ldarg Arg_0x1
 ldc.r4 float32(0xe636ecbc)
 mul 
 Branch_0xd:

 br Branch_0x3

 Branch_0x2:

 ldc.i4.4
 ldc.i4.4
 ble.un Branch_0x12

 ldc.i4.8
 br Branch_0x13

 Branch_0x12:

 ldc.i4.5
 Branch_0x13:

 ldc.i4.8
 conv.i
 bge.un Branch_0x10

 ldarg Arg_0x3
 brtrue Branch_0x14

 ldarg Arg_0x0
 br Branch_0x15

 Branch_0x14:

 ldc.r8 float64(0xa60e984436b6f83f)
 Branch_0x15:

 br Branch_0x11

 Branch_0x10:

 ldc.i4.2
 ldc.i4.8
 bgt Branch_0x16

 ldloc local_0x1
 br Branch_0x17

 Branch_0x16:

 ldc.r8 float64(0x2ea91d6938e3b8e9)
 Branch_0x17:

 Branch_0x11:

 Branch_0x3:

 ldc.i4.4
 conv.i1
 ldc.i4.0
 ldc.i4.7
 ble.un Branch_0x22

 ldc.r4 float32(0xa02be33)
 br Branch_0x23

 Branch_0x22:

 ldarg Arg_0x0
 Branch_0x23:

 ldc.r8 float64(0xc610ddf8af68c042)
 ldarg Arg_0x0
 bgt Branch_0x24

 ldarg Arg_0x0
 br Branch_0x25

 Branch_0x24:

 ldc.r4 float32(0x4c3f738b)
 Branch_0x25:

 clt.un
 ble Branch_0x1e

 ldc.i4.6
 ldc.i4.s -58
 bgt Branch_0x28

 ldarg Arg_0x8
 br Branch_0x29

 Branch_0x28:

 ldsfld int64 ILGEN_0x670a8598::field_0x1
 Branch_0x29:

 ldc.i4.7
 brtrue Branch_0x2a

 ldsfld int64 ILGEN_0x670a8598::field_0x1
 br Branch_0x2b

 Branch_0x2a:

 ldc.i8 0x5a7fb0d4ef748ccf
 Branch_0x2b:

 bne.un Branch_0x26

 ldarg Arg_0x8
 conv.r.un
 br Branch_0x27

 Branch_0x26:

 ldc.r8 float64(0x94382e49e466db7e)
 ldloc local_0x1
 blt Branch_0x2c

 ldarg Arg_0x1
 br Branch_0x2d

 Branch_0x2c:

 ldarg Arg_0x0
 Branch_0x2d:

 Branch_0x27:

 br Branch_0x1f

 Branch_0x1e:

 ldc.i4 0x51d3c89f
 brtrue Branch_0x30

 ldc.i4.5
 br Branch_0x31

 Branch_0x30:

 ldarg Arg_0x4
 Branch_0x31:

 ldc.i4.5
 ldc.i4.5
 blt Branch_0x32

 ldarg Arg_0x5
 br Branch_0x33

 Branch_0x32:

 ldc.i4.0
 Branch_0x33:

 bge Branch_0x2e

 ldarg Arg_0x1
 br Branch_0x2f

 Branch_0x2e:

 ldc.r4 float32(0xb8b5128d)
 ldc.r4 float32(0x982fde21)
 blt Branch_0x34

 ldc.r8 float64(0x26cf3a9970fdeeb6)
 br Branch_0x35

 Branch_0x34:

 ldloc local_0x1
 Branch_0x35:

 Branch_0x2f:

 Branch_0x1f:

 bne.un Branch_0x0

 ldc.i4.6
 conv.ovf.u2
Start_Orphan_11:
 ldc.i4.4
 ldarg Arg_0x6
 bgt.un Branch_0x3a
 br Branch_0x3b

 Branch_0x3a:
 Branch_0x3b:

End_Orphan_11:
 ldc.i4.m1
 ldc.i4.6
 bge.un Branch_0x3e

 ldc.i4.1
 br Branch_0x3f

 Branch_0x3e:

 ldc.i4.1
 Branch_0x3f:

 ldsfld int64 ILGEN_0x670a8598::field_0x1
 conv.ovf.i1.un
 blt Branch_0x3c

 ldarg Arg_0x6
 conv.ovf.u1
 br Branch_0x3d

 Branch_0x3c:

 ldarg Arg_0x4
 Branch_0x3d:

 bge.un Branch_0x36

 ldc.i4.2
 brfalse Branch_0x42

 ldc.i8 0x3b25e57198a4ff47
 br Branch_0x43

 Branch_0x42:

 ldc.i8 0x1b200ab630ffb74d
 Branch_0x43:

 conv.i4
 br Branch_0x37

 Branch_0x36:

 ldarg Arg_0x0
 conv.i4
 Branch_0x37:

 br Branch_0x1

 Branch_0x0:

 ldarg Arg_0x8
 conv.ovf.u8.un
 ldarg Arg_0x5
 conv.ovf.u8.un
 bge Branch_0x4c

 ldc.i4.1
 br Branch_0x4d

 Branch_0x4c:

 ldarg Arg_0x6
 conv.ovf.u.un
 Branch_0x4d:

 ldc.i4.6
 ldc.i4.s 87
 ldarg Arg_0x4
 bge.un Branch_0x50
 ldarg Arg_0x4
 br Branch_0x51
 Branch_0x50:
 ldc.i4.4
 Branch_0x51:
 mul.ovf
 mul.ovf.un
 Branch_0x1:

Start_Orphan_1c:
 ldsflda unsigned int64 ILGEN_0x670a8598::field_0x0
 ldc.i4.4
 ldc.i4.3
 bne.un Branch_0x54
 ldarg Arg_0x8
 br Branch_0x55
 Branch_0x54:
 ldarg Arg_0x8
 Branch_0x55:
 ldarg Arg_0x6
 ldarg Arg_0x4
 bgt Branch_0x56
 br Branch_0x57
 Branch_0x56:
 Branch_0x57:
 stind.i8
End_Orphan_1c:
 conv.ovf.u2
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.r4 float32(0xa1f91438) 
ldc.r8 float64(0xe213003562a62718) 
ldc.i4 0x8df517c3 
ldc.i4 0x2185b645 
ldc.i4 0x675aaf62 
ldc.i4 0x575edec4 
ldc.i4 0xf4cbbbea 
ldc.i4 0xf1f40411 
ldc.i8 0x711f31ac6411e41e 

call int32 ILGEN_0x670a8598::Method_0xdba0534c(float32 Arg_0x0, float64 Arg_0x1, unsigned int16 Arg_0x2, unsigned int32 Arg_0x3, int8 Arg_0x4, unsigned int32 Arg_0x5, int16 Arg_0x6, unsigned int32 Arg_0x7, int64 Arg_0x8)
	pop
	leave X
} catch [mscorlib]System.OverflowException
{
	pop
	leave X
}
X:
ldc.i4 100
 ret
}

}
