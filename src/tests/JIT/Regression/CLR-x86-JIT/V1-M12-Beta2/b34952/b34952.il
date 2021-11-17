// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x2a6b42a0 {}
.assembly extern xunit.core {}
.class ILGEN_0x2a6b42a0 {

//BEGIN STATIC FIELDS
.field static	unsigned int64[] field_0x0
.field static	unsigned int16[] field_0x1
.field static	float64[] field_0x2
.field static	float64 field_0x3
.field static	unsigned int32[] field_0x4
//END STATIC FIELDS
.method static int32 Method_0x51d8(int64 Arg_0x0, int32 Arg_0x1, int32 Arg_0x2, int16 Arg_0x3) {
.maxstack	19

 .locals (int16 local_0x0,unsigned int64 local_0x1,float64 local_0x2,int8[] local_0x3,float64 local_0x4,int8 local_0x5,unsigned int64[] local_0x6,unsigned int32 local_0x7,int32 local_0x8)
//BEGIN INIT LOCALS
ldc.i4 0x20f8530c 
stloc local_0x0
ldc.i8 0x3e3e16734404198a 
stloc local_0x1
ldc.r8 float64(0x75a6fa12fe26b87) 
stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x3
ldc.r8 float64(0x19e02f3412a50bf) 
stloc local_0x4
ldc.i4 0x72322371 
stloc local_0x5
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x6
ldc.i4 0x1af72b05 
stloc local_0x7
ldc.i4 0x6f771ecd 
stloc local_0x8

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x2a6b42a0::field_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x2a6b42a0::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x2a6b42a0::field_0x2
ldc.r8 float64(0x59794e5a26842366) 
stsfld float64 ILGEN_0x2a6b42a0::field_0x3
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x2a6b42a0::field_0x4

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
 ldloca local_0x5
Start_Orphan_1:
Start_Orphan_2:
 ldsfld unsigned int16[] ILGEN_0x2a6b42a0::field_0x1
 ldc.i4.2
 ldelema [mscorlib]System.UInt16
 pop
End_Orphan_2:
 ldsfld unsigned int64[] ILGEN_0x2a6b42a0::field_0x0
Start_Orphan_3:
 nop
End_Orphan_3:
 ldc.i8 0x54db4e2167e118cd
 conv.r.un
 ldc.i4.6
 conv.r.un
 cgt.un
Start_Orphan_4:
 nop
End_Orphan_4:
 ldc.i8 0x6c88367f79eb386d
 ldarg Arg_0x0
 sub.ovf.un
 neg
 stelem.i8
End_Orphan_1:
Start_Orphan_5:
 ldc.i4.8
 conv.u1
 newarr [mscorlib]System.UInt32
 stsfld unsigned int32[] ILGEN_0x2a6b42a0::field_0x4
End_Orphan_5:
Start_Orphan_6:
 ldloc local_0x8
 stloc local_0x8
End_Orphan_6:
Start_Orphan_7:
 nop
End_Orphan_7:
 ldc.r4 float32(0x58a51a45)
 conv.ovf.u8
 conv.r.un
 ckfinite
 conv.r4
Start_Orphan_8:
Start_Orphan_9:
 ldarga Arg_0x1
 ldc.i4.6
 stind.i4
End_Orphan_9:
 ldloca local_0x7
Start_Orphan_a:
 ldc.i4.2
 stloc local_0x5
End_Orphan_a:
Start_Orphan_b:
 nop
End_Orphan_b:
 ldloc local_0x7
 conv.ovf.u8.un
 conv.ovf.u4.un
 stind.i4
End_Orphan_8:
 ldc.r4 float32(0x56ad1b9a)
 ckfinite
 ckfinite
Start_Orphan_c:
 ldsfld unsigned int16[] ILGEN_0x2a6b42a0::field_0x1
 ldc.i4.7
 ldc.i4.5
 stelem.i2
End_Orphan_c:
Start_Orphan_d:
 nop
End_Orphan_d:
 ldc.i4.2
 conv.r4
 neg
 rem
 ckfinite
 mul
 conv.ovf.u4
Start_Orphan_e:
 ldloc local_0x3
 pop
End_Orphan_e:
Start_Orphan_f:
 ldloca local_0x1
 pop
End_Orphan_f:
Start_Orphan_10:
 ldloc local_0x5
 ldc.i4.6
 mul.ovf.un
 stloc local_0x7
End_Orphan_10:
Start_Orphan_11:
 ldloc local_0x4
 stloc local_0x4
End_Orphan_11:
Start_Orphan_12:
 nop
End_Orphan_12:
 ldc.i4.3
 ldloc local_0x5
 mul.ovf
Start_Orphan_13:
 nop
End_Orphan_13:
 ldloc local_0x5
 conv.ovf.i4.un
 add.ovf
 conv.ovf.i8.un
 conv.i4
Start_Orphan_14:
Start_Orphan_15:
 ldloca local_0x5
 pop
End_Orphan_15:
 ldloc local_0x1
 conv.i2
 conv.u1
 newarr [mscorlib]System.UInt16
 stsfld unsigned int16[] ILGEN_0x2a6b42a0::field_0x1
End_Orphan_14:
Start_Orphan_16:
 ldc.i4.s -2
 ldloc local_0x0
 clt
 stloc local_0x5
End_Orphan_16:
 ldarg Arg_0x0
 ldarg Arg_0x0
 sub.ovf
 conv.i1
 conv.u1
 newarr [mscorlib]System.UInt64
 ldlen
 xor
Start_Orphan_17:
Start_Orphan_18:
 ldc.i4.m1
 conv.r.un
 stloc local_0x2
End_Orphan_18:
Start_Orphan_19:
 ldc.r4 float32(0x3e4a4dd9)
 pop
End_Orphan_19:
Start_Orphan_1a:
 nop
End_Orphan_1a:
 ldc.i4.3
 ldc.i4.7
 div.un
 ldarg Arg_0x0
 conv.i1
 cgt
 conv.u1
 newarr [mscorlib]System.UInt32
Start_Orphan_1b:
 ldloca local_0x0
 ldloc local_0x8
 stind.i2
End_Orphan_1b:
 ldc.r4 float32(0x50a602f)
 conv.r8
 conv.u4
Start_Orphan_1c:
 ldloc local_0x2
 stloc local_0x2
End_Orphan_1c:
 ldc.r4 float32(0x709715)
 conv.i1
 div
Start_Orphan_1d:
 ldsfld unsigned int16[] ILGEN_0x2a6b42a0::field_0x1
 ldloc local_0x8
 ldc.i4.4
 stelem.i2
End_Orphan_1d:
Start_Orphan_1e:
 nop
End_Orphan_1e:
 ldloc local_0x3
Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldloc local_0x0
 conv.i4
 ldelema [mscorlib]System.SByte
 ldind.i1
 stelem.i4
End_Orphan_17:
Start_Orphan_20:
Start_Orphan_21:
 ldloc local_0x2
 stloc local_0x4
End_Orphan_21:
Start_Orphan_22:
 nop
End_Orphan_22:
 ldc.i4.s -29
 ldc.i4.2
 shr
 conv.u1
 newarr [mscorlib]System.UInt32
 pop
End_Orphan_20:
Start_Orphan_23:
 ldsfld float64[] ILGEN_0x2a6b42a0::field_0x2
 ldc.r4 float32(0x6dfb2ac7)
 conv.u2
 ldsflda float64 ILGEN_0x2a6b42a0::field_0x3
 ldind.r8
 stelem.r8
End_Orphan_23:
 ldloc local_0x5
 ldc.i4.8
 mul.ovf.un
 ldc.i4.m1
 ldloc local_0x8
 add
 div.un
 ldc.r4 float32(0x59333284)
 ldc.r4 float32(0x194d33a2)
 cgt
 ldc.r4 float32(0x20532816)
 conv.u2
 sub.ovf
 rem.un
Start_Orphan_24:
 ldloca local_0x2
 ldloc local_0x2
 stind.r8
End_Orphan_24:
 ldc.r4 float32(0x451834f0)
 conv.ovf.u
 ldc.i4 0x36d948f3
 ldc.i4.0
 cgt.un
 sub
Start_Orphan_25:
 nop
End_Orphan_25:
 ldloc local_0x1
 conv.ovf.i
Start_Orphan_26:
 nop
End_Orphan_26:
 ldloc local_0x5
 ldc.i4.1
 and
 rem.un
 rem
 rem.un
 conv.ovf.i4.un
 rem
 clt.un
 stind.i1
End_Orphan_0:
 ldarg Arg_0x1
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i8 0x58441ab918a909 
ldc.i4 0x2e7010cc 
ldc.i4 0x4d406fc2 
ldc.i4 0x38933fd6 
call int32 ILGEN_0x2a6b42a0::Method_0x51d8(int64 Arg_0x0, int32 Arg_0x1, int32 Arg_0x2, int16 Arg_0x3)
pop
leave sss
} catch [mscorlib]System.DivideByZeroException {
pop
leave sss
}
sss:
ldc.i4 100
ret
}

}
