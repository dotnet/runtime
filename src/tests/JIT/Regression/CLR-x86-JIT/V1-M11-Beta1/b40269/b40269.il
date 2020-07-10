// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x689f8929 {}
.class ILGEN_0x689f8929 {

.method static int32 Method_0xbc6f9e41(unsigned int32 Arg_0x1, int64 Arg_0x2) {
.maxstack	13

 .locals (float32 local_0x1,unsigned int32 local_0x4)
ldc.r4 float32(0xff14e4e8) 
stloc local_0x1
ldc.i4 0xb3478d0d 
stloc local_0x4

Start_Orphan_0:
 ldloca local_0x1
 ldc.i4.m1
 ldc.i4.0
 blt Branch_0x8
 ldc.i4.3
 br Branch_0x9
Branch_0x8:
 ldc.i4.s 92
Branch_0x9:

 ldc.i4.8
 bge.un Branch_0x6
 ldarg Arg_0x1
 br Branch_0x7
Branch_0x6:
 ldloc local_0x4
 neg
Branch_0x7:

 ldc.i4.8
 ble Branch_0x4
 ldarg Arg_0x2
 conv.r4
 br Branch_0x5
Branch_0x4:
 ldc.r8 float64(0xc599fdb09f4ba560)
Branch_0x5:
 stind.r4
End_Orphan_0:

 ldc.i4.0
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
ldc.i4 0x33071faf 
ldc.i8 0x680b543da592f625 

call int32 ILGEN_0x689f8929::Method_0xbc6f9e41(unsigned int32 Arg_0x1, int64 Arg_0x2)
ldc.i4 100
add
 ret
}

}
