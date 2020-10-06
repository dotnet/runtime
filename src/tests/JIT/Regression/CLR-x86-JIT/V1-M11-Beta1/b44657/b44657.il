// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1957992d {}
.class ILGEN_0x1957992d {

.field static	unsigned int32 field_0x0
.field static	unsigned int16 field_0x1
.field static	int16[] field_0x2
.method static int32 Method_0xc2445738(int8 Arg_0x0, float64 Arg_0x1, int64 Arg_0x2, unsigned int64 Arg_0x3) {
.maxstack	17

 .locals (unsigned int64[] local_0x0,int8[] local_0x1)

ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x1

ldc.i4 0xafc240a 
stsfld unsigned int32 ILGEN_0x1957992d::field_0x0
ldc.i4 0x9fec0077 
stsfld unsigned int16 ILGEN_0x1957992d::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x1957992d::field_0x2
Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
 ldarga Arg_0x0
 pop
End_Orphan_2:
 ldarg Arg_0x1
 ldc.r8 float64(0xa83cd66b402af736)
 clt
 ldc.r8 float64(0x62f3a2ba1fb3165a)
 ldc.r4 float32(0x9aeaae07)
 bne.un Branch_0x6

 ldarg Arg_0x0
 br Branch_0x7

 Branch_0x6:

 ldc.i4.m1
 Branch_0x7:

 bge.un Branch_0x4

 ldc.i4.4
 ldc.i4.8
 ble.un Branch_0x8

 ldc.i4.0
 br Branch_0x9

 Branch_0x8:

 ldc.i4.4
 Branch_0x9:

 br Branch_0x5

 Branch_0x4:

 ldc.i4.2
 Branch_0x5:

 brtrue Branch_0x2

 ldloc local_0x1
 ldc.i4.0
 ldelem.i1
 pop
 br Branch_0x3

 Branch_0x2:

 ldarg Arg_0x2
 conv.ovf.u4
 starg Arg_0x0
 Branch_0x3:

End_Orphan_1:
 ldsfld int16[] ILGEN_0x1957992d::field_0x2
 ldc.i4.3
 ldelem.i2
 conv.ovf.i2.un
 conv.ovf.u2
 ldarg Arg_0x0
 conv.r4
 conv.ovf.i1
 bgt Branch_0x0

Start_Orphan_e:
 ldarga Arg_0x3
 pop
End_Orphan_e:
 ldc.i4.8
 newarr [mscorlib]System.UInt64
 ldarg Arg_0x3
 conv.i2
 ldelem.u8
 br Branch_0x1

 Branch_0x0:

 ldc.i4.5
 not
 conv.ovf.i8
 Branch_0x1:

 starg Arg_0x2
End_Orphan_0:
 ldarg Arg_0x2
 conv.ovf.u2.un
 ldarg Arg_0x2
 conv.ovf.i2.un
Start_Orphan_1e:
 ldsflda unsigned int32 ILGEN_0x1957992d::field_0x0
 ldc.i4.8
 ldc.i4 0xafebe032
 clt.un
 stind.i4
End_Orphan_1e:
 ldsfld unsigned int16 ILGEN_0x1957992d::field_0x1
 conv.r8
 conv.i
 sub.ovf
 sub.ovf
 conv.ovf.i2.un
Start_Orphan_21:
 ldarg Arg_0x2
 starg Arg_0x3
End_Orphan_21:
 ldarg Arg_0x2
 ldc.i4.1
 conv.ovf.u8.un
 rem
 conv.ovf.i1.un
 ceq
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0x36c18b61 
ldc.r8 float64(0xf49a9f8dc0f00f00) 
ldc.i8 0x6c3419ea3b31233a 
ldc.i8 0x231fc748d189bce78 

call int32 ILGEN_0x1957992d::Method_0xc2445738(int8 Arg_0x0, float64 Arg_0x1, int64 Arg_0x2, unsigned int64 Arg_0x3)

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100 ret
}

}
