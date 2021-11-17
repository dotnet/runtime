// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1ef13d7d {}
.assembly extern xunit.core {}
.class ILGEN_0x1ef13d7d {

.field static	unsigned int64 field_0x0
.field static	int8[] field_0x1
.field static	native int field_0x2
.field static	unsigned int16[] field_0x3
.field static	float64 field_0x4
.field static	int32[] field_0x5
.field static	int8[] field_0x6

.method static int32 Method_0xab0e5036(float64 Arg_0x0) {
.maxstack	11

 .locals (unsigned int64[] local_0x0,int32 local_0x1,int64[] local_0x2,native int[] local_0x3,float64[] local_0x4,float64[] local_0x5,int64 local_0x6)

ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x0
ldc.i4 0x99e5ff78 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x4
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x5
ldc.i8 0x27f11e0d8bda7c3e 
stloc local_0x6

ldc.i8 0x47fb912192a3a213 
stsfld unsigned int64 ILGEN_0x1ef13d7d::field_0x0
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0x1ef13d7d::field_0x1
ldc.i4 0xeda4c42 
stsfld native int ILGEN_0x1ef13d7d::field_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x1ef13d7d::field_0x3
ldc.r8 float64(0x8d28e18c1a9aa705) 
stsfld float64 ILGEN_0x1ef13d7d::field_0x4
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0x1ef13d7d::field_0x5
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0x1ef13d7d::field_0x6

Start_Orphan_0:
Start_Orphan_1:
 ldloc local_0x2
 pop
End_Orphan_1:
Start_Orphan_2:
 nop
End_Orphan_2:
 ldloc local_0x4
 ldsfld native int ILGEN_0x1ef13d7d::field_0x2
 ldsfld native int ILGEN_0x1ef13d7d::field_0x2
 beq Branch_0x2

 ldc.i4.6
 br Branch_0x3

 Branch_0x2:

 ldc.i4.s 6
 Branch_0x3:

 ldelem.r8
 ldc.i4.6
 conv.ovf.u8.un
Start_Orphan_3:
 nop
End_Orphan_3:
 ldsfld native int ILGEN_0x1ef13d7d::field_0x2
 ldsfld native int ILGEN_0x1ef13d7d::field_0x2
 bne.un Branch_0x6

 ldc.i8 0x7a78e2266a6c86bd
 br Branch_0x7

 Branch_0x6:

 ldc.i8 0x11f7728dad61ab1
 Branch_0x7:

 ble Branch_0x4

Start_Orphan_4:
 nop
End_Orphan_4:
 ldsfld native int ILGEN_0x1ef13d7d::field_0x2
 ldc.i4.6
 ble.un Branch_0x8

 ldarg Arg_0x0
 br Branch_0x9

 Branch_0x8:

 ldc.r4 float32(0xaa3e9beb)
 Branch_0x9:

 br Branch_0x5

 Branch_0x4:

Start_Orphan_5:
 nop
End_Orphan_5:
 ldc.r4 float32(0xe5f4d41b)
 call float64 [mscorlib]System.Math::Sin(float64)
 Branch_0x5:

 bge.un Branch_0x0

 ldsfld int8[] ILGEN_0x1ef13d7d::field_0x1
 ldc.i4.3
 ldelema [mscorlib]System.SByte
 pop
 br Branch_0x1

 Branch_0x0:

 ldc.i4.2
 conv.u1
 newarr [mscorlib]System.UInt16
 stsfld unsigned int16[] ILGEN_0x1ef13d7d::field_0x3
 Branch_0x1:

End_Orphan_0:
Start_Orphan_6:
 nop
End_Orphan_6:
Start_Orphan_7:
 ldloc local_0x5
 pop
End_Orphan_7:
Start_Orphan_8:
 nop
End_Orphan_8:
 ldc.i4.4
 conv.r8
 conv.r8
 ldc.r8 float64(0xdcc7a1587486f2fd)
 ceq
Start_Orphan_9:
 nop
End_Orphan_9:
 ldsfld native int ILGEN_0x1ef13d7d::field_0x2
 ldc.i4.6
 add.ovf.un
 conv.i
 ldsfld native int ILGEN_0x1ef13d7d::field_0x2
 ldc.i4.4
 sub.ovf.un
 ldsfld native int ILGEN_0x1ef13d7d::field_0x2
 ldsfld native int ILGEN_0x1ef13d7d::field_0x2
 add.ovf.un
 clt
 div.un
 ceq
 conv.ovf.u2.un
EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
ldc.r8 float64(0x64820c6a7ba2c73a2bb61987b87c8cb) 

call int32 ILGEN_0x1ef13d7d::Method_0xab0e5036(float64 Arg_0x0)
ldc.i4 100
add
 ret
}

}
