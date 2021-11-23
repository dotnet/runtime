// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1245f15f {}
.assembly extern xunit.core {}
.class ILGEN_0x1245f15f {
.method static int32 Method_0x5753b424(int16 Arg_0x0, int8 Arg_0x1, unsigned int16 Arg_0x2, int32 Arg_0x3, int16 Arg_0x4, float32 Arg_0x5, unsigned int16 Arg_0x6) {
.maxstack	7

 .locals (int8[] local_0x0,float64[] local_0x1,float32[] local_0x2,int64[] local_0x3)

ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x1

 ldarg Arg_0x5
conv.i4
ldc.i4 55

 blt Branch_0x0

 ldarg Arg_0x3
 conv.i8
 br Branch_0x1

 Branch_0x0:

 ldarg Arg_0x1
 conv.i8
 Branch_0x1:

 ldc.i4.8
 ldc.i8 0x524fbc96537b4f85
 ldc.i8 0x4e2c83efa4379e43
 ble.un Branch_0x8

 ldc.i4.5
 br Branch_0x9

 Branch_0x8:

 ldarg Arg_0x1
 Branch_0x9:

 beq Branch_0x6

 ldc.i8 0xb9d575bda01ea119
 br Branch_0x7

 Branch_0x6:

 ldc.i8 0xe59b051084e14e53
 ldc.i8 0x689ef78e5d0de1f8
 blt.un Branch_0xa

 ldc.i8 0x37723f075b3bf0d
 br Branch_0xb

 Branch_0xa:

 ldc.i8 0xeb77a60e346e2cc6
 Branch_0xb:

 Branch_0x7:

 ceq
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
ldc.i4 0x715aa05a 
ldc.i4 0x383a16ab 
ldc.i4 0x2a1ddbb4 
ldc.i4 0x5942b5d0 
ldc.i4 0xcad9a380 
ldc.r4 float32(0xcf634e06) 
ldc.i4 0xfedace57 

call int32 ILGEN_0x1245f15f::Method_0x5753b424(int16 Arg_0x0, int8 Arg_0x1, unsigned int16 Arg_0x2, int32 Arg_0x3, int16 Arg_0x4, float32 Arg_0x5, unsigned int16 Arg_0x6)
ldc.i4 100
add
ret
}

}
