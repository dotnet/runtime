// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x2e4e98c {}
.assembly extern xunit.core {}
.class ILGEN_0x2e4e98c {

.field static	int32 field_0x1
.method static float32 Method_0x169f(unsigned int64 Arg_0x0, float32 Arg_0x1) {
.maxstack	7

ldc.i4 0x6ab26839 
stsfld int32 ILGEN_0x2e4e98c::field_0x1


 ldarg Arg_0x0
 conv.r8
 
 ldc.r4 float32(0x78393654)
 ldarg Arg_0x1
 beq Branch_0xe

 ldc.r4 float32(0x34d21e0)
 br Branch_0xf

 Branch_0xe:

 ldarg Arg_0x1
 Branch_0xf:

 ldc.r4 float32(0x18bce55)
 blt.un Branch_0xc

 ldc.i4.5
 ldc.i4.m1
 bgt Branch_0x10

 ldc.r8 float64(0x1cd151f847df3043)
 br Branch_0x11

 Branch_0x10:

 ldc.r8 float64(0x123bea148d05fd0)
 Branch_0x11:

 br Branch_0xd

 Branch_0xc:

 ldc.i8 0x50a66c41657828ac
 conv.r8
 Branch_0xd:

 beq Branch_0x0

 ldc.i4.4

 ldc.i4.2
 ldsfld int32 ILGEN_0x2e4e98c::field_0x1
 div
 bgt Branch_0x14

 ldarg Arg_0x0
 conv.r4
 br Branch_0x15

 Branch_0x14:

 ldsfld int32 ILGEN_0x2e4e98c::field_0x1
 ldc.i4 0x50e45eb2
 ble.un Branch_0x1a

 ldc.r4 float32(0xdbe58cb)
 br Branch_0x1b

 Branch_0x1a:

 ldarg Arg_0x1
 Branch_0x1b:

 Branch_0x15:

 br Branch_0x1

 Branch_0x0:

 ldc.r4 float32(0x112615d6)
 Branch_0x1:

ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
ldc.i8 0x22e0501f452a3596 
ldc.r4 float32(0x2a352db0) 

call float32 ILGEN_0x2e4e98c::Method_0x169f(unsigned int64 Arg_0x0, float32 Arg_0x1)
conv.i4
ldc.i4 100
add 
 ret
}

}
