// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x142726a2 {}
.assembly extern xunit.core {}
.class ILGEN_0x142726a2 {


.field static	int16[] field_0x0
.field static	unsigned int8 field_0x1
.field static	unsigned int64 field_0x2
.field static	int32 field_0x3
.field static	float32[] field_0x4
.field static	int16 field_0x5
.field static	float64 field_0x6

.method static int32 Method_0x6a72bf0e(int64 Arg_0x0) {
.maxstack	15

 .locals (native int[] local_0x0,int8[] local_0x1,int16 local_0x2,float32 local_0x3,unsigned int8 local_0x4,float64 local_0x5,int32[] local_0x6)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x1
ldc.i4 0xb315298c 
stloc local_0x2
ldc.r4 float32(0x865f72b9) 
stloc local_0x3
ldc.i4 0x42013193 
stloc local_0x4
ldc.r8 float64(0xcc3a1d5ce187988a) 
stloc local_0x5
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x6

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x142726a2::field_0x0
ldc.i4 0xc85cc9f3 
stsfld unsigned int8 ILGEN_0x142726a2::field_0x1
ldc.i8 0x80d60c83d1b837a5 
stsfld unsigned int64 ILGEN_0x142726a2::field_0x2
ldc.i4 0x16c43de1 
stsfld int32 ILGEN_0x142726a2::field_0x3
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x142726a2::field_0x4
ldc.i4 0xdcba5528 
stsfld int16 ILGEN_0x142726a2::field_0x5
ldc.r8 float64(0xb7565d6ad847fd29) 
stsfld float64 ILGEN_0x142726a2::field_0x6

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
 ldc.i8 0x530fc1a357bcff67
 conv.i1
 ldloc local_0x2
 conv.ovf.u4
 bgt.un Branch_0x6

Start_Orphan_1:
 nop
End_Orphan_1:
 ldc.r8 float64(0x57ff93861a44ea40)
 neg
 br Branch_0x7

 Branch_0x6:

Start_Orphan_2:
 nop
End_Orphan_2:
 ldloc local_0x3
 neg
 Branch_0x7:

Start_Orphan_3:
 ldloc local_0x1
 ldc.i4.5
 ldelema [mscorlib]System.SByte
 ldc.i4.8
 stind.i1
End_Orphan_3:
Start_Orphan_4:
 nop
End_Orphan_4:
 ldc.i4.1
 ldc.i4.m1
 bgt.un Branch_0x8

 ldloc local_0x3
 br Branch_0x9

 Branch_0x8:

 ldc.r8 float64(0x5e0b433a28568d2c)
 Branch_0x9:

 call float64 [mscorlib]System.Math::Cos(float64)
 blt Branch_0x4

Start_Orphan_5:
 ldloc local_0x1
 ldc.i4.1
 ldelema [mscorlib]System.SByte
 ldc.i4.s -3
 stind.i1
End_Orphan_5:
Start_Orphan_6:
 nop
End_Orphan_6:
 ldloc local_0x3
 ldloc local_0x3
 clt
 ldloc local_0x6
 ldsfld int32 ILGEN_0x142726a2::field_0x3
 ldelem.i4
 or
 br Branch_0x5

 Branch_0x4:

Start_Orphan_7:
 nop
End_Orphan_7:
 ldloc local_0x1
 ldlen
 Branch_0x5:

Start_Orphan_8:
Start_Orphan_9:
 nop
End_Orphan_9:
 ldloc local_0x3
 ldloc local_0x5
 mul 
 stloc local_0x3
End_Orphan_8:
Start_Orphan_a:
 ldc.i4.s -90
 pop
End_Orphan_a:
 ldloc local_0x5
 conv.ovf.u2
Start_Orphan_b:
 nop
End_Orphan_b:
 ldsfld int32 ILGEN_0x142726a2::field_0x3
 conv.ovf.u2
 clt.un
Start_Orphan_c:
 ldloc local_0x0
 stloc local_0x0
End_Orphan_c:
Start_Orphan_d:
 nop
End_Orphan_d:
 ldloc local_0x5
 ldloc local_0x3
 div 
 conv.u1
 and
 bgt.un Branch_0x2

Start_Orphan_e:
 nop
End_Orphan_e:
 ldc.i4 0xbaee78c7
 conv.ovf.i2
Start_Orphan_f:
 nop
End_Orphan_f:
 ldsfld int32 ILGEN_0x142726a2::field_0x3
 ldsfld int32 ILGEN_0x142726a2::field_0x3
 xor
 cgt
 pop
 br Branch_0x3

 Branch_0x2:

Start_Orphan_10:
 ldloca local_0x5
 pop
End_Orphan_10:
Start_Orphan_11:
 nop
End_Orphan_11:
 ldarg Arg_0x0
 ldsfld unsigned int8 ILGEN_0x142726a2::field_0x1
 shr
 conv.i8
 starg Arg_0x0
 Branch_0x3:

End_Orphan_0:
Start_Orphan_12:
 ldsflda float64 ILGEN_0x142726a2::field_0x6
 pop
End_Orphan_12:
 ldc.i4.7
 ldc.i4.1
 mul
Start_Orphan_13:
 nop
End_Orphan_13:
 ldloc local_0x2
 ldc.i4.2
 add
 div
 conv.u
Start_Orphan_14:
 nop
End_Orphan_14:
 ldc.i4.7
 ldc.i4.3
 or
 conv.ovf.i
Start_Orphan_15:
 ldsfld float32[] ILGEN_0x142726a2::field_0x4
 pop
End_Orphan_15:
Start_Orphan_16:
 nop
End_Orphan_16:
 ldc.i4.s -40
 ldc.i4.7
 mul.ovf
 conv.ovf.u4.un
 add.ovf.un
 add.ovf.un
 conv.r8
Start_Orphan_17:
Start_Orphan_18:
 ldsflda unsigned int8 ILGEN_0x142726a2::field_0x1
 ldarg Arg_0x0
 conv.u1
 stind.i1
End_Orphan_18:
 ldc.i4.3
 conv.ovf.u8
Start_Orphan_19:
 nop
End_Orphan_19:
 ldc.i4.1
 conv.ovf.i8
 sub.ovf.un
 conv.ovf.i8
 starg Arg_0x0
End_Orphan_17:
Start_Orphan_1a:
 ldc.i4.5
 conv.u4
 conv.ovf.u8
 stsfld unsigned int64 ILGEN_0x142726a2::field_0x2
End_Orphan_1a:
Start_Orphan_1b:
 nop
End_Orphan_1b:
 ldarg Arg_0x0
 conv.u1
 conv.r8
Start_Orphan_1c:
 ldc.r8 float64(0xf93242927b89e9da)
 stloc local_0x5
End_Orphan_1c:
 ldc.r8 float64(0x5a3fdb9c7583e9fb)
 ldc.r4 float32(0x5778d35e)
 rem 
 ckfinite
 div 
 neg
 neg
 sub  
Start_Orphan_1d:
Start_Orphan_1e:
 ldc.i4.4
 stsfld int32 ILGEN_0x142726a2::field_0x3
End_Orphan_1e:
 ldc.i4.6
 conv.u1
 newarr [mscorlib]System.SByte
 ldarg Arg_0x0
 conv.ovf.i4
 ldelema [mscorlib]System.SByte
Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldsfld int16 ILGEN_0x142726a2::field_0x5
 conv.u1
 newarr [mscorlib]System.Int32
Start_Orphan_20:
 nop
End_Orphan_20:
 ldc.i4 0xe066093c
 ldc.i4.5
 ceq
 ldelem.i4
 stind.i1
End_Orphan_1d:
 ldc.r4 float32(0xe3782661)
 call float64 [mscorlib]System.Math::Abs(float64)
 call float64 [mscorlib]System.Math::Abs(float64)
 conv.i8
 conv.ovf.u2.un
 conv.r8
 bge Branch_0x0

 ldloc local_0x1
 ldsfld unsigned int8 ILGEN_0x142726a2::field_0x1
 ldelem.i1
 conv.r8
Start_Orphan_21:
 ldsflda int32 ILGEN_0x142726a2::field_0x3
 pop
End_Orphan_21:
 ldloc local_0x5
 ldc.r4 float32(0xd4837dd2)
 rem 
 call float32 [mscorlib]System.Math::Abs(float32)
 mul 
 conv.u4
 conv.ovf.u4.un
 not
 br Branch_0x1

 Branch_0x0:

Start_Orphan_22:
Start_Orphan_23:
 ldloc local_0x6
Start_Orphan_24:
 nop
End_Orphan_24:
 ldc.i4.5
 ldc.i4.3
 clt
 conv.i4
 ldloc local_0x5
 call float64 [mscorlib]System.Math::Round(float64)
 conv.ovf.u2
 stelem.i4
End_Orphan_23:
 ldloca local_0x5
 pop
End_Orphan_22:
Start_Orphan_25:
Start_Orphan_26:
 nop
End_Orphan_26:
 ldc.i4.7
 conv.ovf.u1.un
 pop
End_Orphan_25:
Start_Orphan_27:
 ldloc local_0x1
 pop
End_Orphan_27:
 ldarg Arg_0x0
 conv.u
 ldloc local_0x2
 conv.i
 add.ovf
 conv.i1
 ldarg Arg_0x0
 ldc.i8 0x5abd997f261c9db1
 sub.ovf.un
Start_Orphan_28:
 nop
End_Orphan_28:
 ldc.i4.s 51
 conv.ovf.u8
 cgt
 conv.ovf.i
 div
 conv.u2
 conv.ovf.u4
 Branch_0x1:

EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
 ldc.i8 0x2ab85ed5315dea85 
 call int32 ILGEN_0x142726a2::Method_0x6a72bf0e(int64 Arg_0x0)
 pop
 leave EEE
 } catch [mscorlib]System.OverflowException { 
 pop
 leave EEE
 }
 EEE:
 ldc.i4 100
 ret
}

}
