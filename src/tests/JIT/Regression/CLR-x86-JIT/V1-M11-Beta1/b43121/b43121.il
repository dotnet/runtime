// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1495dac4 {}
.class ILGEN_0x1495dac4 {

.field static	float64 field_0x0
.field static	unsigned int64[] field_0x1
.field static	int8 field_0x2
.field static	unsigned int32 field_0x3
.field static	unsigned int32[] field_0x4
.field static	unsigned int8[] field_0x5
.method static int32 Method_0xb72b8337(int32 Arg_0x0) {
.maxstack	15

 .locals (unsigned int32[] local_0x0,unsigned int8[] local_0x1,float32[] local_0x2,int64 local_0x3)
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x2
ldc.i8 0x66ae57873565340c 
stloc local_0x3

//BEGIN INIT OF STATIC FIELDS
ldc.r8 float64(0xc1ff5dd7fae1e7a7) 
stsfld float64 ILGEN_0x1495dac4::field_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x1495dac4::field_0x1
ldc.i4 0x4b37527d 
stsfld int8 ILGEN_0x1495dac4::field_0x2
ldc.i4 0x4e999ceb 
stsfld unsigned int32 ILGEN_0x1495dac4::field_0x3
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x1495dac4::field_0x4
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0x1495dac4::field_0x5

//END INIT OF STATIC FIELDS

 BLOCK_1:
 ldsfld unsigned int32 ILGEN_0x1495dac4::field_0x3
Start_Orphan_0:
Start_Orphan_1:
 ldc.i4.5
 ldsfld unsigned int32 ILGEN_0x1495dac4::field_0x3
 bgt.un Branch_0x2

 ldc.i4.4
 starg Arg_0x0
 br Branch_0x3

 Branch_0x2:

 ldsfld unsigned int32 ILGEN_0x1495dac4::field_0x3
 ldc.i4 0x6cb5d927
 bgt.un Branch_0x4
 nop
 br Branch_0x5

 Branch_0x4:

 nop
 Branch_0x5:

 Branch_0x3:

End_Orphan_1:
 ldloc local_0x1
 pop
End_Orphan_0:
 ldc.i4.6
 conv.r8
 ckfinite
 ldc.i4.3
 ldc.i4 0xf18833d1
 shr.un
 conv.r.un
 bgt.un Branch_0x8

 ldc.i4.7
 conv.ovf.i2.un
 conv.u4
 br Branch_0x9

 Branch_0x8:

 ldc.i4.8
 Branch_0x9:

 ldloc local_0x3
 ldc.i8 0x95761a65bda18b6
 sub.ovf
 conv.u
Start_Orphan_8:
 ldsfld unsigned int64[] ILGEN_0x1495dac4::field_0x1
 ldc.i4.5
 ldelema [mscorlib]System.UInt64
 ldloc local_0x3
 stind.i8
End_Orphan_8:
 ldc.i4.1
 conv.u1
 newarr [mscorlib]System.Single
 ldlen
 mul
 cgt
Start_Orphan_9:
 ldsfld unsigned int32[] ILGEN_0x1495dac4::field_0x4
 ldarg Arg_0x0
 ldc.i4.s 84
 clt.un
 ldelema [mscorlib]System.UInt32
Start_Orphan_b:
 ldloc local_0x2
 ldc.i4.6
 ldc.r8 float64(0xbaeef0145159f883)
 stelem.r4
End_Orphan_b:
 ldc.i4.5
 ldc.i4.0
 div
 ldc.i4.s -35
 conv.u2
 rem
 stind.i4
End_Orphan_9:
 ldloc local_0x2
 ldc.i4.8
 ldelem.r4
 conv.ovf.u2
 conv.ovf.i
 ldc.i4.5
 conv.r8
 ckfinite
 ldc.i4.3
 conv.u1
 newarr [mscorlib]System.Single
 ldc.i4.2
 conv.u4
 ldelem.r4
 clt.un
 mul.ovf.un
 bge.un Branch_0x6

 ldc.i4 0x137da1c5
 conv.ovf.u
 ldloc local_0x3
 conv.ovf.i4
 add.ovf
 conv.i4
 conv.ovf.u
 br Branch_0x7

 Branch_0x6:

 ldloca local_0x3
 ldind.i8
 conv.r4
 conv.ovf.i1
Start_Orphan_1a:
 ldc.i4.5
 conv.u1
 newarr [mscorlib]System.UInt64
 ldc.i4.6
 ldsfld unsigned int32 ILGEN_0x1495dac4::field_0x3
 clt.un
 ldloca local_0x3
 ldind.i8
 stelem.i8
End_Orphan_1a:
Start_Orphan_1c:
 ldsflda unsigned int32 ILGEN_0x1495dac4::field_0x3
 ldarg Arg_0x0
 stind.i4
End_Orphan_1c:
 ldc.i4.0
 conv.ovf.i8.un
 conv.ovf.i2.un
 ldsfld unsigned int32 ILGEN_0x1495dac4::field_0x3
 conv.ovf.i4.un
 mul.ovf
 mul.ovf
 Branch_0x7:

 bgt.un Branch_0x0

Start_Orphan_20:
Start_Orphan_21:
 ldloc local_0x2
 ldc.i4.0
 ldelema [mscorlib]System.Single
 ldc.r4 float32(0x51b5a2f7)
 stind.r4
End_Orphan_21:
 ldloc local_0x1
 stsfld unsigned int8[] ILGEN_0x1495dac4::field_0x5
End_Orphan_20:
 ldc.i4.7
 conv.r8
 br Branch_0x1

 Branch_0x0:

 ldloc local_0x3
 conv.r.un
 Branch_0x1:

 conv.i2
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0x622e3099 

call int32 ILGEN_0x1495dac4::Method_0xb72b8337(int32 Arg_0x0)

  pop
  leave out_of_here
} catch [mscorlib]System.DivideByZeroException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100 ret
}

}
