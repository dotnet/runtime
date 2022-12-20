// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b39946 {}
.assembly extern xunit.core {}
.class public ILGEN_0x5c383c7 {


.field static	unsigned int8[] field_0x0
.field static	int32[] field_0x1
.field static	int8 field_0x2
.field static	int64 field_0x3
.field static	unsigned int16[] field_0x4
.field static	float32[] field_0x5
.field static	unsigned int64[] field_0x7

.method static float32 Method_0xdab1c815(float32 Arg_0x0, unsigned int8 Arg_0x1, float32 Arg_0x2, int8 Arg_0x3, unsigned int64 Arg_0x4, unsigned int8 Arg_0x5, unsigned int32 Arg_0x6, int64 Arg_0x7) {
.maxstack	19

 .locals init (float32[] local_0x0,float64[] local_0x1,int64 local_0x2)
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x1

Start_Orphan_0:
Start_Orphan_1:
 ldarga Arg_0x7
 pop
End_Orphan_1:
Start_Orphan_2:
 ldsfld float32[] ILGEN_0x5c383c7::field_0x5
 stsfld float32[] ILGEN_0x5c383c7::field_0x5
End_Orphan_2:
 ldloc local_0x2
 conv.i2
 pop
End_Orphan_0:
 ldloc local_0x1
Start_Orphan_3:
Start_Orphan_4:
Start_Orphan_5:
Start_Orphan_6:
 nop
End_Orphan_6:
 ldarga Arg_0x0
Start_Orphan_7:
 nop
End_Orphan_7:
 ldc.i4.3
 conv.r8
 stind.r4
End_Orphan_5:
Start_Orphan_8:
 ldarg Arg_0x3
 starg Arg_0x6
End_Orphan_8:
 ldsfld int32[] ILGEN_0x5c383c7::field_0x1
Start_Orphan_9:
 nop
End_Orphan_9:
 ldc.i4.3
 ldarg Arg_0x3
 xor
 ldc.r4 float32(0x9f41529a)
 conv.ovf.u2
 clt
 ldelema [mscorlib]System.Int32
Start_Orphan_a:
 ldloc local_0x1
 ldc.i4.s 69
 ldelema [mscorlib]System.Double
Start_Orphan_b:
 nop
End_Orphan_b:
 ldc.r4 float32(0x8da3479e)
 ldarg Arg_0x0
 add conv.r4
 stind.r8
End_Orphan_a:
Start_Orphan_c:
 nop
End_Orphan_c:
 ldc.i4.5
 ldc.i4.1
 add
 conv.ovf.i
 stind.i4
End_Orphan_4:
Start_Orphan_d:
Start_Orphan_e:
 nop
End_Orphan_e:
 ldloc local_0x0
 ldc.r4 float32(0x4dc5acd3)
 conv.u4
 ldelema [mscorlib]System.Single
 pop
End_Orphan_d:
 ldsfld int32[] ILGEN_0x5c383c7::field_0x1
 ldlen
 conv.u1
 newarr [mscorlib]System.Byte
 stsfld unsigned int8[] ILGEN_0x5c383c7::field_0x0
End_Orphan_3:
 ldc.i4 0xe918ddce
 ldelem.r8
Start_Orphan_f:
Start_Orphan_10:
Start_Orphan_11:
 ldsfld int32[] ILGEN_0x5c383c7::field_0x1
 ldc.i4.6
 ldc.i4.4
 stelem.i4
End_Orphan_11:
 ldsfld unsigned int64[] ILGEN_0x5c383c7::field_0x7
 ldc.i4.s -15
 ldelema [mscorlib]System.UInt64
 ldind.u8
 conv.i2
 starg Arg_0x6
End_Orphan_10:
Start_Orphan_12:
 nop
End_Orphan_12:
 ldc.i4.7
 ldc.i4.6
 add
 neg
 ldarg Arg_0x3
 ceq
 conv.u2
 conv.u1
 newarr [mscorlib]System.Byte
Start_Orphan_13:
 ldsflda int64 ILGEN_0x5c383c7::field_0x3
 pop
End_Orphan_13:
Start_Orphan_14:
Start_Orphan_15:
 ldloc local_0x1
 ldarg Arg_0x1
 ldelema [mscorlib]System.Double
 pop
End_Orphan_15:
Start_Orphan_16:
 nop
End_Orphan_16:
 ldsfld int64 ILGEN_0x5c383c7::field_0x3
 ldc.i8 0xb58120274c655f85
 ldsfld int64 ILGEN_0x5c383c7::field_0x3
 div.un
 sub.ovf
 stloc local_0x2
End_Orphan_14:
Start_Orphan_17:
 ldsfld unsigned int8[] ILGEN_0x5c383c7::field_0x0
 ldsfld int8 ILGEN_0x5c383c7::field_0x2
 ldc.i4.0
 stelem.i1
End_Orphan_17:
 ldarg Arg_0x2
 conv.r8
 ckfinite
 conv.i8
 conv.ovf.i
 conv.i1
 ldelema [mscorlib]System.Byte
 pop
End_Orphan_f:
Start_Orphan_18:
Start_Orphan_19:
 ldc.i4.5
 ldc.i4.m1
 mul
 conv.ovf.i8
 conv.ovf.i8
 starg Arg_0x4
End_Orphan_19:
Start_Orphan_1c:
 ldsfld unsigned int16[] ILGEN_0x5c383c7::field_0x4
Start_Orphan_1e:
 ldarga Arg_0x1
 pop
End_Orphan_1e:
 ldc.i4.0
 conv.u
 conv.ovf.u2
 ldc.i4.s 24
 conv.ovf.i2
 ldarg Arg_0x6
 and
 stelem.i2
End_Orphan_1c:
 ldc.i4.6
 ldarg Arg_0x6
 add.ovf
 ldc.i4.4
 conv.ovf.i1
 cgt
Start_Orphan_21:
 ldc.i4.0
 stsfld int8 ILGEN_0x5c383c7::field_0x2
End_Orphan_21:
 ldc.i4.7
 ldsfld int8 ILGEN_0x5c383c7::field_0x2
 div
 ldc.r4 float32(0x6b2f7336)
 conv.ovf.u2
 or
 clt.un
 conv.ovf.u4
 conv.u1
 newarr [mscorlib]System.UInt64
 ldc.r8 float64(0xa6c5004c71818e)
 conv.ovf.i
 conv.ovf.i1.un
 conv.ovf.i1.un
Start_Orphan_27:
 ldc.i4.2
 conv.u1
 newarr [mscorlib]System.Int32
 ldc.i4.s 72
 ldc.i4.s -16
 add
 ldc.i4.m1
 conv.ovf.i2
 stelem.i4
End_Orphan_27:
 ldc.r8 float64(0x9d7b1157b29312a1)
 conv.r8
 ldc.i8 0x44819212b5bc07ec
 conv.r4
 sub conv.r4 
 ldarg Arg_0x0
 ldarg Arg_0x0
 sub conv.r4 
 ldc.i4.8
 conv.r4
 rem conv.r4
 div conv.r4
 ldarga Arg_0x0
 ldind.r4
 ldc.i8 0x4ef8780366a2d301
 conv.r.un
 add conv.r4
 ldarg Arg_0x7
 conv.r.un
 ckfinite
 div conv.r4
 cgt.un
 ceq
 ldc.i4.2
 conv.i
 conv.i8
 not
 stelem.i8
End_Orphan_18:
 ldloc local_0x1
Start_Orphan_39:
 ldarga Arg_0x4
 ldc.i8 0x1e9183552a8439b4
 stind.i8
End_Orphan_39:
 ldc.i4.s -80
 conv.ovf.i8
Start_Orphan_3b:
 ldarg Arg_0x4
 conv.ovf.u
 conv.u1
 newarr [mscorlib]System.Byte
 stsfld unsigned int8[] ILGEN_0x5c383c7::field_0x0
End_Orphan_3b:
 ldc.i4.3
 conv.ovf.i4.un
Start_Orphan_44:
 ldsfld int8 ILGEN_0x5c383c7::field_0x2
 starg Arg_0x5
End_Orphan_44:
 ldc.i4 0xfe559194
 ldc.i4 0x678622e7
 conv.ovf.i2
 clt.un
 add
 conv.i8
 ceq
 ldelema [mscorlib]System.Double
 ldind.r8
 mul conv.r4
ret
}
.method public static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.r4 float32(0xd750d2d2) 
ldc.i4 0x581312c8 
ldc.r4 float32(0x30c249c1) 
ldc.i4 0x82adc1e7 
ldc.i8 0xff262efbfd511c87 
ldc.i4 0x50ed1f3a 
ldc.i4 0xbbbaab3f 
ldc.i8 0xdf6ade5f8fc91e19 

call float32 ILGEN_0x5c383c7::Method_0xdab1c815(float32 Arg_0x0, unsigned int8 Arg_0x1, float32 Arg_0x2, int8 Arg_0x3, unsigned int64 Arg_0x4, unsigned int8 Arg_0x5, unsigned int32 Arg_0x6, int64 Arg_0x7)
conv.i4
pop

  leave a1
} catch [mscorlib]System.NullReferenceException {
  pop
  leave a1
}
a1:
ldc.i4 100
 ret
}

}
