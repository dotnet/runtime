// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1125a118 {}
.class ILGEN_0x1125a118 {

.field static	float32 field_0x0
.field static	int64 field_0x1
.field static	int16[] field_0x2
.field static	int64[] field_0x3

.method static int32 Method_0xa592ccf9(unsigned int64 Arg_0x0, native int Arg_0x1, unsigned int16 Arg_0x2, int8 Arg_0x3, unsigned int32 Arg_0x4) {
.maxstack	19

 .locals (native int[] local_0x0,unsigned int32 local_0x1,unsigned int8[] local_0x2,int16[] local_0x3)

ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stloc local_0x0
ldc.i4 0xcd2d9f13 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x3

ldc.r4 float32(0x59181d5d) 
stsfld float32 ILGEN_0x1125a118::field_0x0
ldc.i8 0x21fb5c0f5a1e9065 
stsfld int64 ILGEN_0x1125a118::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x1125a118::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x1125a118::field_0x3

Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
 ldc.i4.8
Start_Orphan_4:
 ldarg Arg_0x1
 ldc.i4.m1
 beq Branch_0x8

 br Branch_0x9

 Branch_0x8:

 Branch_0x9:

End_Orphan_4:
 ldsfld int64 ILGEN_0x1125a118::field_0x1
 conv.ovf.i2
 ldarg Arg_0x1
 div.un
 bgt.un Branch_0x4

 ldsfld int64[] ILGEN_0x1125a118::field_0x3
 stsfld int64[] ILGEN_0x1125a118::field_0x3
 br Branch_0x5

 Branch_0x4:

 ldc.i4.m1
 conv.u1
 newarr [mscorlib]System.IntPtr
 stloc local_0x0
 Branch_0x5:

End_Orphan_2:
 ldarg Arg_0x0
 conv.ovf.i2.un
 conv.ovf.i4.un
 ldsfld int64 ILGEN_0x1125a118::field_0x1
 conv.u4
 cgt
 stloc local_0x1
End_Orphan_1:
Start_Orphan_6:
Start_Orphan_7:
 ldc.r4 float32(0xfeeac8c7)
 stsfld float32 ILGEN_0x1125a118::field_0x0
End_Orphan_7:
Start_Orphan_8:
 nop
End_Orphan_8:
 ldsfld float32 ILGEN_0x1125a118::field_0x0
 call float32 [mscorlib]System.Math::Abs(float32)
 pop
End_Orphan_6:
 ldc.i4.m1
 conv.r4
Start_Orphan_9:
 ldsfld int16[] ILGEN_0x1125a118::field_0x2
 stloc local_0x3
End_Orphan_9:
Start_Orphan_a:
 ldloc local_0x2
 ldc.i4.m1
 ldelema [mscorlib]System.Byte
 pop
End_Orphan_a:
 ldsfld float32 ILGEN_0x1125a118::field_0x0
Start_Orphan_b:
 nop
End_Orphan_b:
 ldarg Arg_0x1
 ldc.i4.5
 ceq
 conv.r4
 mul 
 call float64 [mscorlib]System.Math::Sqrt(float64)
Start_Orphan_c:
Start_Orphan_d:
 nop
End_Orphan_d:
 ldsfld int64 ILGEN_0x1125a118::field_0x1
 ldarg Arg_0x0
 div
 neg
 starg Arg_0x0
End_Orphan_c:
 ldsflda float32 ILGEN_0x1125a118::field_0x0
 ldind.r4
 rem 
 ceq
 brtrue Branch_0x2

Start_Orphan_e:
Start_Orphan_f:
 ldc.i4.m1
 ldc.i4.5
 ceq
 starg Arg_0x4
End_Orphan_f:
 ldc.i4.3
 conv.r.un
 conv.ovf.i
Start_Orphan_10:
 ldarga Arg_0x1
 pop
End_Orphan_10:
 ldc.r4 float32(0x6cf86b80)
 conv.i2
 div.un
 pop
End_Orphan_e:
Start_Orphan_11:
 nop
End_Orphan_11:
 ldsfld float32 ILGEN_0x1125a118::field_0x0
 conv.ovf.i8
 conv.ovf.u2.un
Start_Orphan_12:
 ldsfld int16[] ILGEN_0x1125a118::field_0x2
 ldc.i4.7
 ldelema [mscorlib]System.Int16
 pop
End_Orphan_12:
Start_Orphan_13:
 nop
End_Orphan_13:
 ldc.r8 float64(0x1e79e8fd5c1af720)
 conv.ovf.u
Start_Orphan_14:
 nop
End_Orphan_14:
 ldc.i4.0
 ldarg Arg_0x3
 shr
 rem.un
 add.ovf
 conv.r4
 conv.ovf.u8
Start_Orphan_15:
 ldsflda float32 ILGEN_0x1125a118::field_0x0
Start_Orphan_16:
Start_Orphan_17:
 nop
End_Orphan_17:
 ldc.i4.3
 conv.r.un
 pop
End_Orphan_16:
Start_Orphan_18:
 ldarg Arg_0x1
 starg Arg_0x1
End_Orphan_18:
Start_Orphan_19:
 nop
End_Orphan_19:
 ldarg Arg_0x1
 ldloc local_0x1
 sub
 conv.ovf.i
 conv.r4
 stind.r4
End_Orphan_15:
 ldloc local_0x3
 ldlen
 shr.un
 br Branch_0x3

 Branch_0x2:

Start_Orphan_1a:
Start_Orphan_1b:
 nop
End_Orphan_1b:
Start_Orphan_1c:
 ldsflda float32 ILGEN_0x1125a118::field_0x0
 pop
End_Orphan_1c:
 ldsflda float32 ILGEN_0x1125a118::field_0x0
 ldind.r4
 call float64 [mscorlib]System.Math::Sqrt(float64)
 stsfld float32 ILGEN_0x1125a118::field_0x0
End_Orphan_1a:
Start_Orphan_1d:
 ldc.i4.s -65
 ldc.i4.4
 div.un
 starg Arg_0x3
End_Orphan_1d:
Start_Orphan_1e:
 nop
End_Orphan_1e:
 ldloca local_0x1
 ldind.u4
 ldc.r8 float64(0xe022fe16f4ef214)
 conv.ovf.i
 add
 conv.ovf.i8.un
Start_Orphan_1f:
 ldsfld int16[] ILGEN_0x1125a118::field_0x2
 ldarg Arg_0x4
 ldelema [mscorlib]System.Int16
Start_Orphan_20:
 nop
End_Orphan_20:
 ldc.i4.8
 stind.i2
End_Orphan_1f:
 ldc.i8 0x42d2dd79be5a96f1
 add.ovf
 conv.ovf.u8
Start_Orphan_21:
Start_Orphan_22:
Start_Orphan_23:
 nop
End_Orphan_23:
 ldsfld int64[] ILGEN_0x1125a118::field_0x3
 ldc.i4.s -26
 ldelema [mscorlib]System.Int64
 pop
End_Orphan_22:
Start_Orphan_24:
 ldloc local_0x3
 stsfld int16[] ILGEN_0x1125a118::field_0x2
End_Orphan_24:
 ldsflda float32 ILGEN_0x1125a118::field_0x0
 ldind.r4
 conv.i1
 conv.u8
 pop
End_Orphan_21:
Start_Orphan_25:
Start_Orphan_26:
 nop
End_Orphan_26:
 ldloc local_0x3
 ldloc local_0x1
 not
 ldelema [mscorlib]System.Int16
 pop
End_Orphan_25:
Start_Orphan_27:
Start_Orphan_28:
 nop
End_Orphan_28:
 ldsfld int64 ILGEN_0x1125a118::field_0x1
 ldsfld int64 ILGEN_0x1125a118::field_0x1
 rem.un
 pop
End_Orphan_27:
Start_Orphan_29:
 ldloc local_0x2
 ldc.i4.0
 ldelema [mscorlib]System.Byte
 ldc.i4.2
 stind.i1
End_Orphan_29:
 ldarg Arg_0x1
 ldloc local_0x1
 div.un
 conv.i8
 conv.ovf.u2.un
 conv.ovf.i8.un
 conv.u8
 mul.ovf
 Branch_0x3:

Start_Orphan_2a:
 ldarg Arg_0x1
 conv.r4
 conv.i1
 ldarg Arg_0x1
 ldc.i4.8
 sub.ovf.un
 ldc.r8 float64(0x5ed4a0a8f8079ff8)
 conv.ovf.i
 sub
 cgt
 starg Arg_0x3
End_Orphan_2a:
 ldarg Arg_0x1
 ldloc local_0x1
 clt
 ldarg Arg_0x1
 ldarg Arg_0x1
 mul.ovf
 cgt
 conv.u
 conv.u1
Start_Orphan_33:
 ldc.i4.7
 conv.u1
 newarr [mscorlib]System.Int16
 ldarg Arg_0x0
 conv.u1
 ldarg Arg_0x1
 conv.ovf.u1.un
 stelem.i2
End_Orphan_33:
 ldsflda int64 ILGEN_0x1125a118::field_0x1
 ldind.i8
 ldarg Arg_0x0
 ldarg Arg_0x0
 cgt
 shr.un
Start_Orphan_35:
 ldsfld int64[] ILGEN_0x1125a118::field_0x3
 stsfld int64[] ILGEN_0x1125a118::field_0x3
End_Orphan_35:
 ldsfld int64 ILGEN_0x1125a118::field_0x1
 ldarg Arg_0x0
 sub
 conv.i8
 cgt.un
 conv.ovf.i4
 mul.ovf.un
 ldarg Arg_0x0
 conv.ovf.u2
 ldarg Arg_0x1
 ldarg Arg_0x1
 and
 add.ovf.un
 conv.ovf.i
 conv.r4
 conv.i1
 shr.un
 conv.ovf.u8.un
 ble Branch_0x0

 nop
 br Branch_0x1

 Branch_0x0:

 ldc.i4.2
 conv.u1
 newarr [mscorlib]System.Byte
 pop
 Branch_0x1:

End_Orphan_0:
Start_Orphan_3c:
Start_Orphan_3d:
Start_Orphan_3f:
 nop
End_Orphan_3f:
 ldsfld int64[] ILGEN_0x1125a118::field_0x3
 ldc.i4.s -69
 ldelema [mscorlib]System.Int64
Start_Orphan_41:
 ldloca local_0x1
 ldarg Arg_0x2
 stind.i4
End_Orphan_41:
 ldc.i8 0x1984885fb550bd1e
 ldc.i8 0x4390dee968ea6b16
 clt
 conv.i8
 stind.i8
End_Orphan_3d:
 ldsflda float32 ILGEN_0x1125a118::field_0x0
Start_Orphan_43:
 ldarg Arg_0x0
 conv.u1
 pop
End_Orphan_43:
Start_Orphan_44:
 ldc.i8 0xf91a1afdfcad163f
 conv.ovf.u2.un
 starg Arg_0x4
End_Orphan_44:
 ldc.r4 float32(0x676ee829)
 neg
 call float64 [mscorlib]System.Math::Cos(float64)
 stind.r4
End_Orphan_3c:
 ldsflda float32 ILGEN_0x1125a118::field_0x0
 ldind.r4
 conv.ovf.u8
 conv.ovf.u.un
 conv.r4
Start_Orphan_4b:
Start_Orphan_4c:
 ldsfld int16[] ILGEN_0x1125a118::field_0x2
 ldc.r4 float32(0x68688daa)
 ldc.r4 float32(0x513b20bc)
 cgt.un
 ldelema [mscorlib]System.Int16
Start_Orphan_4e:
 ldloc local_0x0
 ldc.i4.6
 ldarg Arg_0x1
 stelem.i
End_Orphan_4e:
 ldc.i4.3
 conv.r4
 ldsfld float32 ILGEN_0x1125a118::field_0x0
 call float64 [mscorlib]System.Math::Sin(float64)
 cgt.un
 stind.i2
End_Orphan_4c:
 ldarga Arg_0x4
 ldarg Arg_0x1
 ldarg Arg_0x3
 clt.un
 conv.i4
 conv.ovf.i4
 neg
 stind.i4
End_Orphan_4b:
 ldarg Arg_0x1
 ldarg Arg_0x1
 div.un
 ldarg Arg_0x1
 ldc.i4.5
 rem.un
 rem
 conv.i8
 ldloc local_0x2
 ldc.i4.0
 ldelema [mscorlib]System.Byte
 ldind.u1
 conv.ovf.u8
 not
 div
 ldsflda float32 ILGEN_0x1125a118::field_0x0
 ldind.r4
 ckfinite
 conv.i4
 not
 shr.un
 conv.r8
 add 
 conv.ovf.u4
 conv.ovf.i4
EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i8 0x9bac1c95a7193761 
ldc.i4 0x9a1fc402 conv.i
ldc.i4 0x11e14047 
ldc.i4 0x09685f48a 
ldc.i4 0xbfab54ee 

call int32 ILGEN_0x1125a118::Method_0xa592ccf9(unsigned int64 Arg_0x0, native int Arg_0x1, unsigned int16 Arg_0x2, int8 Arg_0x3, unsigned int32 Arg_0x4)
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
