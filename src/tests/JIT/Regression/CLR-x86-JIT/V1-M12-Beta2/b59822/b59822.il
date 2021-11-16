// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1db2cfa3 {}
.assembly extern xunit.core {}
.class ILGEN_0x1db2cfa3 {

.field static	int16[] field_0x0
.field static	unsigned int32 field_0x1
.field static	int16[] field_0x2

.method static int32 Method_0x8618f02c(unsigned int16 Arg_0x0, float64 Arg_0x1, int16 Arg_0x2, int8 Arg_0x3) {
.maxstack	9

 .locals (float64 LOCAL_0x0)

ldc.r8 float64(0xb3e5a6c0c2213032) 
stloc LOCAL_0x0

ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x1db2cfa3::field_0x0
ldc.i4 0xaa1ee15d 
stsfld unsigned int32 ILGEN_0x1db2cfa3::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x1db2cfa3::field_0x2

Start_Orphan_0:
 ldc.i4.4
 ldc.i4.4
 blt.un Branch_0x2

 ldc.i4 0x7ffc0de1
 br Branch_0x3

 Branch_0x2:

 ldarg Arg_0x3
 Branch_0x3:

 ldc.i4.5
 conv.ovf.u2
 sub.ovf
 brfalse Branch_0x0

 ldarg Arg_0x2

 ldc.i4.7
 ldarg Arg_0x3
 sub.ovf.un
 blt Branch_0x4

 ldarg Arg_0x0
 ldc.i4 0xa8b835ba
 blt.un Branch_0x6

 nop
 br Branch_0x7

 Branch_0x6:

 nop
 Branch_0x7:

 br Branch_0x5

 Branch_0x4:

 ldc.i4.6
 pop
 Branch_0x5:

 br Branch_0x1

 Branch_0x0:

Start_Orphan_5:
 nop
End_Orphan_5:
 ldc.i4.7
 starg Arg_0x3
 Branch_0x1:

End_Orphan_0:
Start_Orphan_6:
 ldc.i8 0x5925e03414e7e3d
 ldc.i8 0x2cd3ef8495c5a496
 ble.un Branch_0xc

 ldc.r4 float32(0x38c8d88)
 br Branch_0xd

 Branch_0xc:

 ldloc LOCAL_0x0
 Branch_0xd:

 ldc.r4 float32(0xe0b1de46)
 ldloc LOCAL_0x0
 blt.un Branch_0xe

 ldarg Arg_0x1
 br Branch_0xf

 Branch_0xe:

 ldarg Arg_0x1
 Branch_0xf:

 blt.un Branch_0xa

 ldc.i4.0
 brfalse Branch_0x10

 nop
 br Branch_0x11

 Branch_0x10:

 nop
 Branch_0x11:

 br Branch_0xb

 Branch_0xa:

 ldc.i4.3
 starg Arg_0x3
 Branch_0xb:

End_Orphan_6:
 ldc.r8 float64(0x7862fdb68acafd9c)
 ldc.r8 float64(0x76907a1f4b6057e7)
 div 
 ldc.i4.2
 ldc.i4.0
 bgt.un Branch_0x14

 ldarg Arg_0x1
 br Branch_0x15

 Branch_0x14:

 ldc.r8 float64(0x3c87af0733184e7a)
 Branch_0x15:

 bge Branch_0x12

Start_Orphan_9:
 nop
End_Orphan_9:
 ldc.r4 float32(0x9498a7de)
 ldloc LOCAL_0x0
 bgt Branch_0x16

 ldarg Arg_0x1
 br Branch_0x17

 Branch_0x16:

 ldarg Arg_0x1
 Branch_0x17:

 br Branch_0x13

 Branch_0x12:

 ldc.i4.1
 ldc.i4 0xda80c75
 blt Branch_0x18

 ldloc LOCAL_0x0
 br Branch_0x19

 Branch_0x18:

 ldc.r4 float32(0xf906415b)
 Branch_0x19:

 Branch_0x13:

 call float32 [mscorlib]System.Math::Abs(float32)
 ldc.i4.m1
 ldc.i4.1
 ble.un Branch_0x1e

 ldloc LOCAL_0x0
 br Branch_0x1f

 Branch_0x1e:

 ldarg Arg_0x1
 Branch_0x1f:

 ldc.i4.1
 ldc.i4.8
 beq Branch_0x20

 ldarg Arg_0x1
 br Branch_0x21

 Branch_0x20:

 ldc.r8 float64(0x66495bc9dd6d9f98)
 Branch_0x21:

 blt.un Branch_0x1c

 ldc.i8 0x7a1fb451eb9892f3
 ldc.i8 0x79548f17c169f4d5
 bgt Branch_0x22

 ldc.i4.1
 br Branch_0x23

 Branch_0x22:

 ldarg Arg_0x0
 Branch_0x23:

 br Branch_0x1d

 Branch_0x1c:

 ldc.i4.2
 ldarg Arg_0x0
 rem
 Branch_0x1d:

 ldc.i4.2
 conv.ovf.u.un
 ldc.i4.3
 neg
 bgt Branch_0x26

 ldc.r4 float32(0xb5cc67e0)
 ldc.r8 float64(0x47c3f8995a046e6f)
 ble.un Branch_0x28

 ldc.i4.2
 br Branch_0x29

 Branch_0x28:

 ldc.i4.0
 Branch_0x29:

 br Branch_0x27

 Branch_0x26:

 ldloc LOCAL_0x0
 ldc.r8 float64(0x467a68464f1eec71)
 ble.un Branch_0x2a

 ldc.i4.m1
 br Branch_0x2b

 Branch_0x2a:

 ldarg Arg_0x3
 Branch_0x2b:

 Branch_0x27:

 bgt.un Branch_0x1a

 ldarg Arg_0x1
 ldc.r8 float64(0x27489ea903e70e)
 cgt
 brtrue Branch_0x2e

 ldc.r8 float64(0x718bd6405642b3a3)
 neg
 br Branch_0x2f

 Branch_0x2e:

 ldc.i8 0xdb290afca166e699
 ldc.i8 0x99a929295595e8de
 blt.un Branch_0x30

 ldc.r8 float64(0x4a036596eac7ea67)
 br Branch_0x31

 Branch_0x30:

 ldloc LOCAL_0x0
 Branch_0x31:

 Branch_0x2f:

 br Branch_0x1b

 Branch_0x1a:

 ldc.i4.6
 conv.u8
 ldc.i8 0xcd67d57c7d2f3e1b
 blt.un Branch_0x32

 ldloc LOCAL_0x0
 call float64 [mscorlib]System.Math::Sin(float64)
 br Branch_0x33

 Branch_0x32:

 ldc.i8 0x6b38a65c7849ae82
 conv.r4
 Branch_0x33:

 Branch_0x1b:

 blt.un Branch_0x8

 ldc.i4.0
 ldc.i4 0xd99a455e
 bge.un Branch_0x3c

 ldc.i4.3
 br Branch_0x3d

 Branch_0x3c:

 ldc.i4.3
 Branch_0x3d:

 br Branch_0x3b

 Branch_0x3b:

 br Branch_0x9

 Branch_0x8:

 ldc.i4.3
 Branch_0x9:

EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0xbb0ad68a 
ldc.r8 float64(0xbbb42c889dd60564) 
ldc.i4 0x4773ecd8 
ldc.i4 0x96c51ab9 

call int32 ILGEN_0x1db2cfa3::Method_0x8618f02c(unsigned int16 Arg_0x0, float64 Arg_0x1, int16 Arg_0x2, int8 Arg_0x3)
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
