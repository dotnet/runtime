// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b47886' {}
.assembly extern xunit.core {}
.class ILGEN_0xce8ea34 {
.field static	unsigned int32 field_0x3
.field static	int8 field_0x5
.field static	float32[] field_0x7
.field static	int16 field_0xa
.method static int32 Method_0xdf08df47(int16 Arg_0x0, float64 Arg_0x1, unsigned int16 Arg_0x2, int32 Arg_0x3, unsigned int8 Arg_0x4) {
.maxstack	9

 .locals (int32 local_0x0,int64 local_0x1,unsigned int16 local_0x2,unsigned int16 local_0x3,int8[] local_0x4,unsigned int8[] local_0x5,unsigned int64[] local_0x6,unsigned int8 local_0x7,int8[] local_0x8,float32[] local_0x9)

ldc.i4 0xab7a8938 
stloc local_0x0
ldc.i8 0xe62d5b451fad6ab 
stloc local_0x1
ldc.i4 0x492b85e5 
stloc local_0x7
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x8
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x9

ldc.i4 0xf935ba87 
stsfld unsigned int32 ILGEN_0xce8ea34::field_0x3
ldc.i4 0x62fb2732 
stsfld int8 ILGEN_0xce8ea34::field_0x5
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0xce8ea34::field_0x7
ldc.i4 0x2e8a4bc7 
stsfld int16 ILGEN_0xce8ea34::field_0xa

Start_Orphan_0:
 ldc.i4.5
 conv.u1
 newarr [mscorlib]System.UInt64
 ldloc local_0x1
 ldloc local_0x1
 cgt
 ldelem.u8
Start_Orphan_3:
 ldarg Arg_0x1
 ldc.r8 float64(0x49d7c3f784a7c8fb)
 blt Branch_0x2

 nop
 br Branch_0x3

 Branch_0x2:

 nop
 Branch_0x3:

End_Orphan_3:
 ldarg Arg_0x1
 conv.ovf.u2
 conv.u8
 blt.un Branch_0x0

 ldc.i8 0x6c5e76a35d01ede7
 conv.ovf.u8
 pop
 br Branch_0x1

 Branch_0x0:

Start_Orphan_4:
 nop
End_Orphan_4:
 ldc.r8 float64(0x7279967bf122b129)
 ldarg Arg_0x1
 ble Branch_0x6

 ldarg Arg_0x4
 br Branch_0x7

 Branch_0x6:

 ldc.i4.s -18
 Branch_0x7:

 ldc.i4.m1
 ldsfld int8 ILGEN_0xce8ea34::field_0x5
 ble.un Branch_0x8

 ldc.i4.7
 br Branch_0x9

 Branch_0x8:

 ldloc local_0x0
 Branch_0x9:

 bge Branch_0x4

 ldloc local_0x9
 stsfld float32[] ILGEN_0xce8ea34::field_0x7
 br Branch_0x5

 Branch_0x4:

 ldarg Arg_0x0
 ldloc local_0x0
 blt Branch_0xa

 nop
 br Branch_0xb

 Branch_0xa:

 nop
 Branch_0xb:

 Branch_0x5:

 Branch_0x1:

End_Orphan_0:
Start_Orphan_5:
 ldloc local_0x8
 ldc.i4.s -68
 ldsfld int16 ILGEN_0xce8ea34::field_0xa
 stelem.i1
End_Orphan_5:
 ldloc local_0x1
 conv.i2
 ldc.i8 0x20a1160072bd9df4
 ldc.i8 0x3f37165b78c7d6c
 bne.un Branch_0xe

 ldsfld int8 ILGEN_0xce8ea34::field_0x5
 br Branch_0xf

 Branch_0xe:

 ldsfld unsigned int32 ILGEN_0xce8ea34::field_0x3
 Branch_0xf:

 beq Branch_0xc

Start_Orphan_7:
 nop
End_Orphan_7:
 ldc.i4.4
 ldc.i4.6
 beq Branch_0x10

 ldc.i8 0xb525cf2fc048a52
 br Branch_0x11

 Branch_0x10:

 ldloc local_0x1
 Branch_0x11:

 br Branch_0xd

 Branch_0xc:

 ldloc local_0x7
 ldc.i4.2
 bge.un Branch_0x12

 ldloc local_0x1
 br Branch_0x13

 Branch_0x12:

 ldloc local_0x1
 Branch_0x13:

 Branch_0xd:

 ldc.r8 float64(0x7b0c00ad06be983)
 ldarg Arg_0x1
 bgt.un Branch_0x14

 ldc.i8 0x3695df15c3c0f0a9
 br Branch_0x15

 Branch_0x14:

 ldc.i8 0xa8d8b66e923db887
 Branch_0x15:

Start_Orphan_a:
 nop
End_Orphan_a:
 ldloc local_0x1
 ldc.i8 0x8ca3b4f93354c3e1
 div.un
 mul
 clt.un
 ldsflda int16 ILGEN_0xce8ea34::field_0xa
 ldind.i2
Start_Orphan_d:
 nop
End_Orphan_d:
 ldsfld int8 ILGEN_0xce8ea34::field_0x5
 ldc.i4.3
 mul.ovf.un
 ble Branch_0x1a

 ldc.r4 float32(0x86500217)
 call       float64 [mscorlib]System.Math::Round(float64)
 br Branch_0x1b

 Branch_0x1a:

 ldloc local_0x1
 conv.r4
 Branch_0x1b:

Start_Orphan_e:
 nop
End_Orphan_e:
 ldc.i8 0xf63b72c157da0a18
 conv.ovf.u1
 ldc.r8 float64(0x84c39979972f8f4a)
 conv.i1
 bge Branch_0x1c

 ldc.i4.m1
 ldc.i4.6
 ble.un Branch_0x1e

 ldc.r8 float64(0x4f1dfe4c37d8147c)
 br Branch_0x1f

 Branch_0x1e:

 ldarg Arg_0x1
 Branch_0x1f:

 br Branch_0x1d

 Branch_0x1c:

 ldc.r8 float64(0xaea574022dab93b5)
 neg
 Branch_0x1d:

 blt.un Branch_0x16

 ldloc local_0x1
 conv.ovf.i8.un
Start_Orphan_11:
 nop
End_Orphan_11:
 ldc.i8 0x2348cd1120f573e3
 ldc.i8 0x669ff4d74241c35c
 or
 blt Branch_0x20

Start_Orphan_12:
 nop
End_Orphan_12:
 ldc.i4.4
 ldc.i4.m1
 blt Branch_0x22

 ldc.i4.7
 br Branch_0x23

 Branch_0x22:

 ldarg Arg_0x3
 Branch_0x23:

 br Branch_0x21

 Branch_0x20:

 ldloc local_0x1
 ldloc local_0x1
 bgt Branch_0x24

 ldarg Arg_0x2
 br Branch_0x25

 Branch_0x24:

 ldc.i4.7
 Branch_0x25:

 Branch_0x21:

 br Branch_0x17

 Branch_0x16:

 ldc.r8 float64(0x1c2be35e44f9c4f2)
 ldc.r8 float64(0x55932592b767cd88)
 ble.un Branch_0x26

 ldsfld unsigned int32 ILGEN_0xce8ea34::field_0x3
 br Branch_0x27

 Branch_0x26:

 ldc.i4.8
 Branch_0x27:

 ldc.i4.3
 ldc.i4.s -3
 rem.un
 add
 Branch_0x17:

 cgt
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0xa267fe28 
ldc.r8 float64(0xde74825c965ba5f9) 
ldc.i4 0x7c3601b6 
ldc.i4 0xd0a71bb5 
ldc.i4 0xcc2aed8e 

call int32 ILGEN_0xce8ea34::Method_0xdf08df47(int16 Arg_0x0, float64 Arg_0x1, unsigned int16 Arg_0x2, int32 Arg_0x3, unsigned int8 Arg_0x4)
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
