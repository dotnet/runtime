// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x57cc69ce {}
.class ILGEN_0x57cc69ce {
.field static	unsigned int8 field_0x0
.field static	int32 field_0x1
.field static	int8 field_0x2
.field static	int64[] field_0x3
.field static	unsigned int8[] field_0x4
.field static	unsigned int16 field_0x5

.method static int32 Method_0x183df1b1() {
.maxstack	17

 .locals (unsigned int8[] local_0x0,float64 local_0x1,int16[] local_0x2,unsigned int64[] local_0x3,int8[] local_0x4,int8 local_0x5,int16 local_0x6,int32[] local_0x7)

ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x0
ldc.r8 float64(0x58606410) 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x4
ldc.i4 0x5e4ddbb9 
stloc local_0x5
ldc.i4 0x4c8edd7a 
stloc local_0x6
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x7

ldc.i4 0x451fb1db 
stsfld unsigned int8 ILGEN_0x57cc69ce::field_0x0
ldc.i4 0xf79abf8b 
stsfld int32 ILGEN_0x57cc69ce::field_0x1
ldc.i4 0xa9f67f4b 
stsfld int8 ILGEN_0x57cc69ce::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x57cc69ce::field_0x3
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0x57cc69ce::field_0x4
ldc.i4 0xa144f031 
stsfld unsigned int16 ILGEN_0x57cc69ce::field_0x5

Start_Orphan_0:
Start_Orphan_1:
 ldc.i4.6
 ldc.i4.8
 ble Branch_0x8
 ldc.i8 0xa8881947a44f1b98
 br Branch_0x9
Branch_0x8:
 ldc.i8 0xfe6bc7e557af645d
Branch_0x9:
 ldc.r4 float32(0xc18ddab9)
 conv.u8
 bgt.un Branch_0x6
 ldloc local_0x6
 conv.r8
 br Branch_0x7
Branch_0x6:
 ldloc local_0x5
 conv.r8
Branch_0x7:
Start_Orphan_4:
 ldc.r4 float32(0xb3b48f33)
 pop
End_Orphan_4:
 ldsfld int32 ILGEN_0x57cc69ce::field_0x1
 conv.ovf.u.un
 conv.r8
 ble.un Branch_0x4
Start_Orphan_5:
 nop
End_Orphan_5:
 ldc.r4 float32(0xdc5223ee)
 ldc.r4 float32(0x24f2d68b)
 cgt
 ldsfld int32 ILGEN_0x57cc69ce::field_0x1
 conv.u1
 sub
 br Branch_0x5
Branch_0x4:
Start_Orphan_6:
 ldsfld int64[] ILGEN_0x57cc69ce::field_0x3
 stsfld int64[] ILGEN_0x57cc69ce::field_0x3
End_Orphan_6:
 ldc.i4.s 86
 conv.i4
Branch_0x5:
 ldloc local_0x5
 ldloc local_0x4
 ldc.i4.m1
 ldelem.i1
 add.ovf
Start_Orphan_8:
 ldsflda unsigned int16 ILGEN_0x57cc69ce::field_0x5
 pop
End_Orphan_8:
 ldc.r8 float64(0x282b67c5d4f967b)
 conv.i2
 clt.un
 ble Branch_0x2
Start_Orphan_a:
 ldc.i4.7
 stloc local_0x5
End_Orphan_a:
 ldloc local_0x3
 ldc.i4.s 99
 ldelema [mscorlib]System.UInt64
 ldind.u8
 ldc.i8 0xbebf7fde2a0bdebf
 cgt.un
 stsfld unsigned int8 ILGEN_0x57cc69ce::field_0x0
 br Branch_0x3
Branch_0x2:
Start_Orphan_c:
 ldc.i4 0xccfbe394
 stsfld int8 ILGEN_0x57cc69ce::field_0x2
End_Orphan_c:
 ldc.i4.5
 stloc local_0x5
 Branch_0x3:
End_Orphan_1:
 ldc.i4.m1
 conv.ovf.u4
 ldc.i4.4
 ldloc local_0x6
 add
 mul.ovf
 conv.ovf.i4.un
 neg
Start_Orphan_e:
Start_Orphan_f:
 ldsfld int64[] ILGEN_0x57cc69ce::field_0x3
 ldc.i4.0
 ldc.i8 0xafaa07379b79c89a
 stelem.i8
End_Orphan_f:
 ldc.i8 0x422f47e4e13c135
 ldc.i8 0x532fcc24d5fb62d8
 div
 conv.ovf.u1.un
 stloc local_0x5
End_Orphan_e:
Start_Orphan_11:
 ldc.i4.7
 conv.u1
 newarr [mscorlib]System.Int32
 stloc local_0x7
End_Orphan_11:
 ldc.i4 0x227e7d40
 ldsfld int32 ILGEN_0x57cc69ce::field_0x1
 clt.un
 conv.r.un
Start_Orphan_13:
 nop
End_Orphan_13:
 ldc.r4 float32(0xbf3e9c5e)
 ldc.r4 float32(0x5fa6b50e)
 rem conv.r4
 conv.r4
 div conv.r4
 conv.u
 and
Start_Orphan_14:
Start_Orphan_15:
Start_Orphan_16:
 nop
End_Orphan_16:
 ldsfld unsigned int8[] ILGEN_0x57cc69ce::field_0x4
Start_Orphan_17:
 nop
End_Orphan_17:
 ldc.i8 0x5f17ba4ebdbc97af
 conv.i1
 ldc.i4.8
 stelem.i1
End_Orphan_15:
Start_Orphan_18:
 ldloca local_0x1
 pop
End_Orphan_18:
 ldsfld unsigned int16 ILGEN_0x57cc69ce::field_0x5
 ldc.i4.1
 ldc.i4.8
 sub.ovf
 cgt
 conv.u1
 newarr [mscorlib]System.Int32
Start_Orphan_19:
Start_Orphan_1a:
 nop
End_Orphan_1a:
 ldc.i4.5
 conv.u1
 newarr [mscorlib]System.SByte
 ldc.i4.m1
 conv.ovf.u2
Start_Orphan_1b:
 nop
End_Orphan_1b:
 ldloc local_0x2
 ldloc local_0x6
 ldelem.i2
 stelem.i1
End_Orphan_19:
Start_Orphan_1c:
 ldloc local_0x7
 ldc.i4 0x97e0c983
 ldc.i4 0xf0e0673c
 stelem.i4
End_Orphan_1c:
 ldsfld int32 ILGEN_0x57cc69ce::field_0x1
 conv.r4
 conv.u2
Start_Orphan_1d:
 nop
End_Orphan_1d:
 ldsflda int32 ILGEN_0x57cc69ce::field_0x1
 ldind.i4
 sub.ovf
Start_Orphan_1e:
Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldsfld int64[] ILGEN_0x57cc69ce::field_0x3
Start_Orphan_20:
 nop
End_Orphan_20:
 ldsfld int8 ILGEN_0x57cc69ce::field_0x2
 ldc.i4.4
 clt.un
 ldc.i4.s 11
 conv.ovf.u8
 stelem.i8
End_Orphan_1e:
Start_Orphan_21:
 nop
End_Orphan_21:
 ldc.i4.4
 conv.ovf.i2.un
Start_Orphan_22:
 nop
End_Orphan_22:
 ldc.i4.s -17
 ldc.i4.4
 add.ovf
 and
Start_Orphan_23:
 ldsflda int32 ILGEN_0x57cc69ce::field_0x1
 ldc.i4.6
 stind.i4
End_Orphan_23:
Start_Orphan_24:
 nop
End_Orphan_24:
 ldc.r4 float32(0xa63fe030)
 neg
Start_Orphan_25:
 nop
End_Orphan_25:
 ldsfld unsigned int16 ILGEN_0x57cc69ce::field_0x5
 conv.r8
 cgt
 clt.un
 stelem.i4
End_Orphan_14:
Start_Orphan_26:
 ldloc local_0x2
 pop
End_Orphan_26:
Start_Orphan_27:
 ldsfld unsigned int8 ILGEN_0x57cc69ce::field_0x0
 pop
End_Orphan_27:
 ldc.i4.s 87
 conv.u
 ldc.i4.5
 add.ovf.un
Start_Orphan_28:
 nop
End_Orphan_28:
 ldloc local_0x6
 conv.ovf.i2.un
 conv.ovf.i4
 sub.ovf.un
 bgt Branch_0x0

 ldsflda unsigned int16 ILGEN_0x57cc69ce::field_0x5
Start_Orphan_29:
Start_Orphan_2a:
 ldloc local_0x4
 ldsfld unsigned int16 ILGEN_0x57cc69ce::field_0x5
 ldc.i4.4
 stelem.i1
End_Orphan_2a:
 ldloca local_0x5
 ldind.i1
 stsfld unsigned int16 ILGEN_0x57cc69ce::field_0x5
End_Orphan_29:
Start_Orphan_2b:
 ldsfld unsigned int8[] ILGEN_0x57cc69ce::field_0x4
 ldc.i4.7
 ldc.i4.0
 stelem.i1
End_Orphan_2b:
 ldsfld unsigned int8[] ILGEN_0x57cc69ce::field_0x4
 ldlen
Start_Orphan_2c:
 ldsflda int32 ILGEN_0x57cc69ce::field_0x1
 pop
End_Orphan_2c:
 ldc.i8 0x12673d88ca726dc1
 ldc.i8 0x7863067ae1bf978b
 ceq
Start_Orphan_2d:
 nop
End_Orphan_2d:
 ldc.i4.4
 conv.ovf.u1.un
 cgt
 conv.ovf.i
 ceq
 stind.i2
 br Branch_0x1

 Branch_0x0:

 ldloc local_0x3
 pop
 Branch_0x1:

End_Orphan_0:
 ldloc local_0x2
 ldc.i8 0xd4d32fb137d8c025
 conv.r.un
 conv.ovf.i1
 ldelema [mscorlib]System.Int16
 ldind.i2
 conv.u1
 newarr [mscorlib]System.Int64
 ldc.i8 0xd7737a20cc27407f
 ldc.i8 0x1f96e518f6d0ba63
 rem
 conv.ovf.u8.un
 conv.ovf.i1
 conv.r.un
Start_Orphan_3b:
 ldloc local_0x0
 ldc.r4 float32(0xa9d1e6c4)
 conv.ovf.i1
 ldelema [mscorlib]System.Byte
 ldloc local_0x6
 ldc.i4.6
 add.ovf
 conv.i4
 stind.i1
End_Orphan_3b:
 ldc.i8 0x99bf56cb2c627789
 ldsfld int32 ILGEN_0x57cc69ce::field_0x1
 conv.u1
 newarr [mscorlib]System.UInt64
 ldc.i4.1
 ldc.i4 0x2aa9f313
 ceq
 ldelem.u8
 rem
 conv.r8
 ceq
 ldelema [mscorlib]System.Int64
 ldind.i8
 ldc.i4.s 27
 ldc.r4 float32(0xe731e9cd)
 conv.ovf.i2
 ldsfld int32 ILGEN_0x57cc69ce::field_0x1
 ldc.i4.5
 div.un
 div.un
 clt.un
 ldc.i4.2
 ldc.i4.2
 rem.un
 div
 conv.ovf.i8
 conv.ovf.i8
 ldc.r8 float64(0xf330c63d7a1d841a)
 ldc.r8 float64(0x69345d1045fe1d6b)
 add conv.r4
 conv.r8
 ldc.i8 0xefa6508e7b7374d
 conv.ovf.i8.un
 conv.ovf.i.un
 ldc.i4.0
 conv.ovf.u2.un
 ldsfld int8 ILGEN_0x57cc69ce::field_0x2
 ldloc local_0x6
 add
 mul
 or
 ldc.i4.2
 conv.i4
 conv.i
 ldc.i4 0x57c8b54a
 conv.u1
 newarr [mscorlib]System.Int32
 ldc.i4.4
 conv.u1
 ldelem.i4
 clt
 sub
 conv.r8
 cgt
 shl
 clt
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
call int32 ILGEN_0x57cc69ce::Method_0x183df1b1()
	pop
	leave X
} catch [mscorlib]System.IndexOutOfRangeException
{
	pop
	leave X
}
X:
ldc.i4 100
 ret
}

}
