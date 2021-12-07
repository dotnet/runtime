// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xe08350d0 {}
.assembly extern xunit.core {}
.class ILGEN_0xe08350d0 {
.method static float32 Method_0x4d9a(float32 Arg_0x0, int64 Arg_0x1) {
.maxstack	7

 ldc.r4 float32(0x71334631)
 conv.r8
 ldarg Arg_0x0
 conv.r8
 sub
 ldarg Arg_0x0
 ldc.r4 float32(0x19c4412c)
 ble.un Branch_0x8

 ldc.i4.1
 br Branch_0x9

 Branch_0x8:

 ldc.i4.7
 Branch_0x9:

 conv.r.un
 blt.un Branch_0x2

 ldc.i4.7
 conv.r4
 br Branch_0x3

 Branch_0x2:

 ldc.i8 0x483376ba21bb13a6
 conv.r4
 Branch_0x3:

ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
ldc.r4 float32(0xfc46528) 
ldc.i8 0x24ba7bf277693bc7 

call float32 ILGEN_0xe08350d0::Method_0x4d9a(float32 Arg_0x0, int64 Arg_0x1)
conv.i4
ldc.i4 7
sub
ldc.i4 100
add
 ret
}

}
