// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b30869' {}
.class public ILGEN_0xcd4c3cc0 {

//BEGIN STATIC FIELDS
.field static	int32[] field_0x0
.field static	int8 field_0x1
.field static	unsigned int32[] field_0x2
.field static	unsigned int32[] field_0x3
.field static	unsigned int16[] field_0x4
.field static	unsigned int8[] field_0x5
.field static	float32[] field_0x6
//END STATIC FIELDS
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	17

 .locals (int8 local_0x0,int8[] local_0x1,int16 local_0x2,unsigned int8 local_0x3,float64[] local_0x4)
 .try {
//BEGIN INIT LOCALS
ldc.i4 0x26e23ee5 
stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x1
ldc.i4 0x60106bcf 
stloc local_0x2
ldc.i4 0x2f924d23 
stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x4

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0xcd4c3cc0::field_0x0
ldc.i4 0x5b7d35bf 
stsfld int8 ILGEN_0xcd4c3cc0::field_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0xcd4c3cc0::field_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0xcd4c3cc0::field_0x3
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0xcd4c3cc0::field_0x4
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0xcd4c3cc0::field_0x5
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0xcd4c3cc0::field_0x6

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
 ldloca local_0x2
Start_Orphan_1:
 nop
End_Orphan_1:
Start_Orphan_2:
Start_Orphan_3:
 ldc.i4.6
 conv.ovf.i8.un
 pop
End_Orphan_3:
Start_Orphan_4:
 ldloc local_0x4
 ldc.i4.5
 ldelema [mscorlib]System.Double
 ldc.r8 float64(0x2419443a795521c8)
 stind.r8
End_Orphan_4:
 ldc.r4 float32(0x681e6343)
 ldc.r4 float32(0x8e93675)
 div
 conv.ovf.u2
 conv.u1
 newarr [mscorlib]System.UInt16
 stsfld unsigned int16[] ILGEN_0xcd4c3cc0::field_0x4
End_Orphan_2:
Start_Orphan_5:
 ldc.i4.m1
 conv.ovf.u1
 stloc local_0x3
End_Orphan_5:
Start_Orphan_6:
 ldsfld unsigned int8[] ILGEN_0xcd4c3cc0::field_0x5
 stsfld unsigned int8[] ILGEN_0xcd4c3cc0::field_0x5
End_Orphan_6:
Start_Orphan_7:
 nop
End_Orphan_7:
 ldc.i8 0x3cb114b63f5d155a
 conv.r.un
 conv.u
 conv.u4
 conv.ovf.u8
Start_Orphan_8:
 ldc.i4.0
 stsfld int8 ILGEN_0xcd4c3cc0::field_0x1
End_Orphan_8:
Start_Orphan_9:
 ldloc local_0x4
 stloc local_0x4
End_Orphan_9:
 ldc.i8 0x1f0237197ce27106
 ldc.i8 0x4b3630c61f5453ca
 or
 conv.i4
 conv.ovf.u8.un
Start_Orphan_a:
 ldc.i4.8
 pop
End_Orphan_a:
 ldc.i8 0x570f6b964df04ec3
 conv.i8
 ldc.i8 0x500c4f7b2c984597
 conv.ovf.i8.un
 div
Start_Orphan_b:
 ldsflda int8 ILGEN_0xcd4c3cc0::field_0x1
 ldc.i4.8
 stind.i1
End_Orphan_b:
 ldc.i4.5
 conv.ovf.i8
Start_Orphan_c:
 nop
End_Orphan_c:
 ldc.i8 0x797a33e51fc544d
 conv.ovf.i8
 mul
 sub.ovf
 mul
 div.un
 conv.ovf.u1.un
 stind.i2
End_Orphan_0:
Start_Orphan_d:
Start_Orphan_e:
 ldc.i4.4
 stsfld int8 ILGEN_0xcd4c3cc0::field_0x1
End_Orphan_e:
 ldc.i4.7
 conv.ovf.u4.un
 ldc.i4.1
 conv.ovf.i.un
 clt.un
 conv.ovf.i1.un
 conv.u8
 conv.ovf.i
 pop
End_Orphan_d:
 ldc.i8 0x57f5d2b67b7592b
 conv.r.un
Start_Orphan_f:
 nop
End_Orphan_f:
 ldloc local_0x4
 ldc.i4.s 98
 ldelem.r8
 rem
 ldc.i8 0x62f7c0221012d5e
 ldc.i8 0x5b7149551971f46
 or
 conv.r.un
 add
 conv.r8
 conv.r4
Start_Orphan_10:
 ldloca local_0x2
 ldc.i4.4
 stind.i2
End_Orphan_10:
Start_Orphan_11:
 nop
End_Orphan_11:
 ldsfld int8 ILGEN_0xcd4c3cc0::field_0x1
 ldc.i4.s 83
 rem.un
 neg
Start_Orphan_12:
 ldc.i4 0x7d0e3000
 stsfld int8 ILGEN_0xcd4c3cc0::field_0x1
End_Orphan_12:
 ldc.i4 0xfe83e0c
 sub
 conv.ovf.i4.un
 conv.r4
 add
Start_Orphan_13:
Start_Orphan_14:
 ldloca local_0x3
 pop
End_Orphan_14:
 ldsfld float32[] ILGEN_0xcd4c3cc0::field_0x6
 stsfld float32[] ILGEN_0xcd4c3cc0::field_0x6
End_Orphan_13:
Start_Orphan_15:
Start_Orphan_16:
 ldloc local_0x4
 ldc.i4.2
 ldc.r8 float64(0xa5018f268101a24)
 stelem.r8
End_Orphan_16:
Start_Orphan_17:
 nop
End_Orphan_17:
 ldc.r4 float32(0xd376e5b)
 conv.i2
 conv.u1
 newarr [mscorlib]System.Byte
 stsfld unsigned int8[] ILGEN_0xcd4c3cc0::field_0x5
End_Orphan_15:
 ldc.r4 float32(0x35713db8)
 neg
 conv.r4
 rem
Start_Orphan_18:
Start_Orphan_19:
Start_Orphan_1a:
 ldc.i4.4
 conv.u1
 newarr [mscorlib]System.UInt16
 stsfld unsigned int16[] ILGEN_0xcd4c3cc0::field_0x4
End_Orphan_1a:
 ldloca local_0x2
 pop
End_Orphan_19:
 ldloc local_0x4
 pop
End_Orphan_18:
Start_Orphan_1b:
 nop
End_Orphan_1b:
Start_Orphan_1c:
 ldsfld float32[] ILGEN_0xcd4c3cc0::field_0x6
 stsfld float32[] ILGEN_0xcd4c3cc0::field_0x6
End_Orphan_1c:
 ldc.i4.6
 ldc.i4.s -22
 div.un
 conv.ovf.i1
Start_Orphan_1d:
 nop
End_Orphan_1d:
 ldloc local_0x2
 ldc.i4 0x264979d
 sub
 ldloc local_0x0
 conv.u1
 ceq
 shr.un
Start_Orphan_1e:
 ldsfld unsigned int16[] ILGEN_0xcd4c3cc0::field_0x4
 stsfld unsigned int16[] ILGEN_0xcd4c3cc0::field_0x4
End_Orphan_1e:
Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldc.i4.4
 conv.ovf.i2.un
 ldc.i8 0x19943ca47a1dce
 conv.ovf.i.un
 div
 conv.ovf.u4.un
 div.un
Start_Orphan_20:
 ldsflda int8 ILGEN_0xcd4c3cc0::field_0x1
 pop
End_Orphan_20:
 ldc.i8 0x540d4a8f48b21347
 ldc.i8 0x46131b1631b46a05
 or
 conv.u
 conv.ovf.i1.un
Start_Orphan_21:
 ldloc local_0x4
 ldc.i4.8
 ldelema [mscorlib]System.Double
 ldc.r8 float64(0x64c02f8c1c345d7d)
 ldc.r8 float64(0x266718e3310b1e5d)
 add
 stind.r8
End_Orphan_21:
 ldc.i4 0x33aea8
 conv.i
 conv.ovf.i8
 conv.ovf.i
 mul
 sub.ovf
 conv.r8
 conv.r4
 ceq
  pop
  leave exit
} catch [mscorlib]System.OverflowException
{
  pop
  leave exit
}
exit:
  ldc.i4.0
ldc.i4 100
add
ret
}
}
