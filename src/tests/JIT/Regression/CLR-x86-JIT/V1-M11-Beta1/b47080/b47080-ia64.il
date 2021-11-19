// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x2be45b24 {}
.assembly extern xunit.core {}
.class ILGEN_0x2be45b24 {

.field static	int8 field_0x0
.field static	float64 field_0x1
.field static	int16 field_0x2
.field static	int8 field_0x3
.field static	float64 field_0x4
.field static	unsigned int32[] field_0x5

.method static int32 Method_0xa3cb4747(float32 Arg_0x0, int64 Arg_0x1, float32 Arg_0x2, float64 Arg_0x3, float64 Arg_0x4, unsigned int64 Arg_0x5, float32 Arg_0x6, int8 Arg_0x7) {
.maxstack	17

 .locals (int16[] local_0x0,int32 local_0x1,float64[] local_0x2,float64 local_0x3,float32 local_0x4,int32 local_0x5,int64[] local_0x6,unsigned int32 local_0x7,int32[] local_0x8)
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x0
ldc.i4 0x7e651bc7 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x2
ldc.r8 float64(0x3f263b0736b81b66) 
stloc local_0x3
ldc.r4 float32(0xff5d44b) 
stloc local_0x4
ldc.i4 0x93705de4 
stloc local_0x5
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x6
ldc.i4 0x741869c8 
stloc local_0x7
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x8

ldc.i4 0x281f5452 
stsfld int16 ILGEN_0x2be45b24::field_0x2

 ldc.i8 0x5b6ff7592bcc6c33
 conv.ovf.i4
Start_Orphan_e:
 ldc.i4.4
 stsfld int16 ILGEN_0x2be45b24::field_0x2
End_Orphan_e:
 brfalse Branch_0x2

 ldc.r8 float64(0xdbd9ba7e5460f81)
 conv.i8
Start_Orphan_17:
 ldc.i4.2
 conv.u1
 newarr [mscorlib]System.UInt32
 ldarg Arg_0x1
 conv.i4
 ldelema [mscorlib]System.UInt32
 ldc.i4.4
 ldloc local_0x7
 div.un
 stind.i4
End_Orphan_17:
 ldc.i4.s -1
 conv.ovf.i8
Start_Orphan_1d:
 ldloc local_0x8
 ldc.i4.1
 ldelema [mscorlib]System.Int32
 ldc.i4.s 44
 stind.i
End_Orphan_1d:
 ldarg Arg_0x5
 ldarg Arg_0x5
 ldc.i8 0xa3eaf24c32e0ad25
 ldarg Arg_0x1
 and
 div
 ldloc local_0x2
 ldc.i4.s -51
 ldelem.r8
 conv.ovf.u8
 add.ovf
 add
 and
 or
 br Branch_0x3

 Branch_0x2:

 ldc.i8 0xac93ea86e28f2b75
 conv.u8
Start_Orphan_2b:
Start_Orphan_2c:
 ldc.i8 0x6a638167c634ce1d
 conv.ovf.u1
 stloc local_0x5
End_Orphan_2c:
End_Orphan_2b:
 ldc.i4.m1
 conv.ovf.i8.un
 conv.ovf.u8
 ldarg Arg_0x1
 mul
 ldc.i4.8
 conv.u1
 newarr [mscorlib]System.UInt32
 ldlen
 ldloc local_0x3
 ldc.r4 float32(0xf1d4eccc)
 ldsfld float64 ILGEN_0x2be45b24::field_0x1
 div conv.r4
 cgt
 sub.ovf.un
 shr.un
 conv.i8
 sub.ovf.un
 Branch_0x3:

Start_Orphan_33:
 ldarg Arg_0x5
 conv.ovf.i8
 ldc.r4 float32(0x48933f86)
 conv.ovf.i8
 add.ovf.un
 conv.r8
 neg
 starg Arg_0x3
End_Orphan_33:
 ldloc local_0x4
 conv.r8
Start_Orphan_41:
 ldloc local_0x6
 ldc.i4.s 32
 ldloc local_0x7
 ceq
 conv.ovf.u4
 ldc.i4.5
 conv.u8
 conv.i8
 stelem.i8
End_Orphan_41:
 conv.i8
 blt Branch_0x0

 ldloc local_0x1
conv.i4
br Branch_0x1

 Branch_0x0:

Start_Orphan_4f:
 ldloc local_0x2
 ldsflda int16 ILGEN_0x2be45b24::field_0x2
 ldind.i2
 ldc.i4.7
 conv.ovf.i4
Start_Orphan_58:
 ldloca local_0x7
 ldc.i4.3
 stind.i4
End_Orphan_58:
 ldc.i4.s 53
 not
 conv.ovf.i2
 rem.un
 conv.r8
Start_Orphan_59:
 ldloca local_0x4
 pop
End_Orphan_59:
 ldarg Arg_0x6
 add conv.r4
 stelem.r8
End_Orphan_4f:
 ldc.i4.4
 Branch_0x1:

ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.r4 float32(0xf29dba7d) 
ldc.i8 0x6b28ee7faf20ea30 
ldc.r4 float32(0x2d3f7113) 
ldc.r8 float64(0x37421129370d9b58) 
ldc.r8 float64(0x3b905f6a56663f0a) 
ldc.i8 0x45d97ac0dcdc52b3 
ldc.r4 float32(0x485902d9) 
ldc.i4 0x939caa89 

call int32 ILGEN_0x2be45b24::Method_0xa3cb4747(float32 Arg_0x0, int64 Arg_0x1, float32 Arg_0x2, float64 Arg_0x3, float64 Arg_0x4, unsigned int64 Arg_0x5, float32 Arg_0x6, int8 Arg_0x7)

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100
 ret
}

}
