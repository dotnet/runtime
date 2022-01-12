// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x4601be46 {}
.assembly extern xunit.core {}
.class ILGEN_0x4601be46 {

.field static	int64 field_0x0

.method static int32 Method_0x9c86375f(unsigned int64 Arg_0x0, unsigned int8 Arg_0x1) {
.maxstack	9

 .locals (unsigned int64[] LOCAL_0x0,unsigned int8[] LOCAL_0x1,int16 LOCAL_0x2,unsigned int64[] LOCAL_0x3)

ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc LOCAL_0x0
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc LOCAL_0x1
ldc.i4 0x22416c9b 
stloc LOCAL_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc LOCAL_0x3

ldc.i8 0xe2f7cba6c23fcdfb 
stsfld int64 ILGEN_0x4601be46::field_0x0

Start_Orphan_0:
 ldc.i4.6
 ldarg Arg_0x1
 ble Branch_0x4

 ldc.i4.m1
 br Branch_0x5

 Branch_0x4:

 ldc.i4.4
 Branch_0x5:

 ldarga Arg_0x1
 ldind.u1
 blt Branch_0x2

 ldc.i4.1
 ldc.i4.m1
 ble Branch_0x6

 ldc.r8 float64(0xbeb0dc7219808b40)
 br Branch_0x7

 Branch_0x6:

 ldc.r8 float64(0x64c1d1f1b0de6dea)
 Branch_0x7:

 br Branch_0x3

 Branch_0x2:

 ldc.r8 float64(0xdb223a7b8fc595a)
 Branch_0x9:

 Branch_0x3:

 ldc.r8 float64(0x42de08c7c9aab7)
 call float64 [mscorlib]System.Math::Round(float64)
 bgt Branch_0x0
 ldc.i4.1
 ldloc LOCAL_0x2
 div.un
 ldarg Arg_0x1
 not
 blt.un Branch_0xc

 br Branch_0xd

 Branch_0xc:

 Branch_0xd:

 br Branch_0x1

 Branch_0x0:

 ldarga Arg_0x0
 pop
 Branch_0x1:

End_Orphan_0:
 ldc.i4.7
Start_Orphan_c:
 ldc.i4.1
 ldc.i4.1
 bge Branch_0x14
 nop
 br Branch_0x15
 Branch_0x14:
 nop
 Branch_0x15:
End_Orphan_c:
 ldc.i4 0x467970e4
 bgt Branch_0x12
 ldarg Arg_0x0
 br Branch_0x13

 Branch_0x12:

 ldarg Arg_0x0
 Branch_0x13:

 conv.i2
EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
ldc.i8 0xb62352d6af44571c 
ldc.i4 0xd9a0f7b2 

call int32 ILGEN_0x4601be46::Method_0x9c86375f(unsigned int64 Arg_0x0, unsigned int8 Arg_0x1)
ldc.i4 22200
sub.ovf.un
 ret
}

}
