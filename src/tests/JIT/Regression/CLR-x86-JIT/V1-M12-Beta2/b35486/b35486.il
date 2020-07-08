// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xc53351bb {}
.class ILGEN_0xc53351bb {

.field static	int16 field_0x2

.method static float32 Method_0x6cf4(int16 Arg_0x0, int8 Arg_0x1, unsigned int32 Arg_0x2, unsigned int64 Arg_0x3, int32 Arg_0x4, int64 Arg_0x5, int16 Arg_0x6, int16 Arg_0x7, unsigned int8 Arg_0x8, int64 Arg_0x9, float32 Arg_0xa, unsigned int8 Arg_0xb, unsigned int64 Arg_0xc, unsigned int8 Arg_0xd) {
.maxstack	9

 .locals (int64 local_0x5,int64 local_0x9,int32 local_0xa,unsigned int32 local_0xc)

ldc.i8 0x2a4a65245c5f7aa8 
stloc local_0x5
ldc.i8 0x72936dc07da040e4 
stloc local_0x9
ldc.i4 0x12034bf1 
stloc local_0xa
ldc.i4 0x2f7e6c3b 
stloc local_0xc

ldc.i4 0x7c5f2a62 
stsfld int16 ILGEN_0xc53351bb::field_0x2

 ldloc local_0x9
 conv.u8
 conv.i2
 ldc.i4.3
 conv.ovf.u4
 brfalse Branch_0xc

 ldsfld int16 ILGEN_0xc53351bb::field_0x2
 ldloc local_0xa
 bgt.un Branch_0xe

 ldarg Arg_0xc
 br Branch_0xf

 Branch_0xe:

 ldarg Arg_0x3
 Branch_0xf:

 br Branch_0xd

 Branch_0xc:

 ldc.i4.6
 ldarg Arg_0xb
 blt.un Branch_0x10

 ldarg Arg_0x3
 br Branch_0x11

 Branch_0x10:

 ldarg Arg_0x3
 Branch_0x11:

 Branch_0xd:

 ldloc local_0x5
 bne.un Branch_0xa

 ldarg Arg_0xa
 ldc.r4 float32(0x541a4769)
 ble.un Branch_0x14

 ldc.i4.8
 br Branch_0x15

 Branch_0x14:

 ldarg Arg_0x0
 Branch_0x15:

 conv.ovf.u2.un
 br Branch_0xb

 Branch_0xa:

 ldc.i4.6
 ldc.i4.5
 blt.un Branch_0x1a

 ldarg Arg_0xc
 br Branch_0x1b

 Branch_0x1a:

 ldloc local_0x9
 Branch_0x1b:
conv.i4
 brfalse Branch_0x18

 ldarg Arg_0x5
 ldc.i8 0x7d56770370221df
 bge.un Branch_0x1c

 ldc.i4.0
 br Branch_0x1d

 Branch_0x1c:

 ldloc local_0xa
 Branch_0x1d:

 br Branch_0x19

 Branch_0x18:

Start_Orphan_a:
 nop
End_Orphan_a:
 ldc.i4.1
 ldc.i4.m1
 div.un
 Branch_0x19:

 Branch_0xb:

 bne.un Branch_0x0

 ldc.r8 float64(0x775524a911715582)
 conv.u
 brfalse Branch_0x22

 ldloc local_0x5
 conv.ovf.i8.un
 br Branch_0x23

 Branch_0x22:

 ldc.r8 float64(0x72a15a13261c384f)
 conv.ovf.u8
 Branch_0x23:

 ldc.i4.0
 ldarg Arg_0x4
 bgt Branch_0x26

 ldarg Arg_0x8
 br Branch_0x27

 Branch_0x26:

 ldc.i4.s -24
 Branch_0x27:

 ldc.r8 float64(0x4f151f135d917d8)
 conv.ovf.i
 beq Branch_0x24
 ldc.i4 0x6053950
 ldc.i4.m1
 ble Branch_0x28

 ldc.i8 0x41532ca653cb1af4
 br Branch_0x29

 Branch_0x28:

 ldarg Arg_0x5
 Branch_0x29:

 br Branch_0x25

 Branch_0x24:

 ldc.i4.0
 ldarg Arg_0x0
 bgt Branch_0x2a

 ldarg Arg_0x3
 br Branch_0x2b

 Branch_0x2a:

 ldarg Arg_0x9
 Branch_0x2b:

 Branch_0x25:

 bgt.un Branch_0x1e

 ldc.i4.0
 ldc.i4.5
 bge Branch_0x2e

 ldarg Arg_0x1
 br Branch_0x2f

 Branch_0x2e:

 ldsfld int16 ILGEN_0xc53351bb::field_0x2
 Branch_0x2f:

 ldc.i4.4
 ldarg Arg_0x2
 bge Branch_0x30

 ldarg Arg_0xd
 br Branch_0x31

 Branch_0x30:

 ldc.i4.5
 Branch_0x31:

 blt.un Branch_0x2c

 ldc.i4 0x25d95457
 ldarg Arg_0x1
 bgt.un Branch_0x32

 ldarg Arg_0xa
 br Branch_0x33

 Branch_0x32:

 ldarg Arg_0xa
 Branch_0x33:

 br Branch_0x2d

 Branch_0x2c:

 ldarg Arg_0x3
 ldarg Arg_0x3
 bgt Branch_0x34

 ldc.r4 float32(0x6ab36f7b)
 br Branch_0x35

 Branch_0x34:

 ldc.r4 float32(0x71e91d1d)
 Branch_0x35:

 Branch_0x2d:

 br Branch_0x1f

 Branch_0x1e:

 ldloc local_0xa
 ldloc local_0xc
 bge Branch_0x38

 ldc.i4.4
 br Branch_0x39

 Branch_0x38:

 ldarg Arg_0x6
 Branch_0x39:

 ldarg Arg_0xa
 ldc.r4 float32(0x2fc9453b)
 ble Branch_0x3a

 ldc.i4 0x4be41236
 br Branch_0x3b

 Branch_0x3a:

 ldc.i4.7
 Branch_0x3b:

 blt.un Branch_0x36

 ldarg Arg_0xa
 br Branch_0x37

 Branch_0x36:

 ldarg Arg_0x3
 conv.i4
 brtrue Branch_0x3c

 ldc.r4 float32(0xd0b6a00)
 br Branch_0x3d

 Branch_0x3c:

 ldarg Arg_0xa
 Branch_0x3d:

 Branch_0x37:

 Branch_0x1f:

 br Branch_0x1

 Branch_0x0:

 ldc.r4 float32(0x7bb6516b)

 conv.r4
 Branch_0x1:
pop
ldc.r4 44.0
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
ldc.i4 0x20c94c5a 
ldc.i4 0x718f53c9 
ldc.i4 0x524a5cdb 
ldc.i8 0x763b615bd267c7a 
ldc.i4 0x1e5f5691 
ldc.i8 0x4153643639d67b77 
ldc.i4 0x48222f32 
ldc.i4 0x186f3f1f 
ldc.i4 0x6f0c696 
ldc.i8 0x1ab9189f3a247957 
ldc.r4 float32(0x26e07b3e) 
ldc.i4 0x1b1b7754 
ldc.i8 0x67c249375b3e2d2 
ldc.i4 0x527a282c 

call float32 ILGEN_0xc53351bb::Method_0x6cf4(int16 Arg_0x0, int8 Arg_0x1, unsigned int32 Arg_0x2, unsigned int64 Arg_0x3, int32 Arg_0x4, int64 Arg_0x5, int16 Arg_0x6, int16 Arg_0x7, unsigned int8 Arg_0x8, int64 Arg_0x9, float32 Arg_0xa, unsigned int8 Arg_0xb, unsigned int64 Arg_0xc, unsigned int8 Arg_0xd)
conv.i4
ldc.i4 44
sub
ldc.i4 100
add
 ret
}

}
