// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b40411' {}
.assembly extern xunit.core {}
.class ILGEN_0x1a182c40 {
.method static int32 Method_0x36cdd4e(float64 Arg_0x9) {
.maxstack	15

Start_Orphan_1:
 ldc.i4 0x6
 ldarg Arg_0x9
 ldarg Arg_0x9
 clt.un
 bne.un Branch_0x6
 ldc.i4.5
 conv.r4
 br Branch_0x7
Branch_0x6:
 ldc.i4.3
 conv.r.un
Branch_0x7:
 ldc.i4.4
 conv.r8
 add conv.r4
 ldc.r4 float32(0xc8344261)
 ldc.r4 float32(0x19581c4)
 ckfinite
 ldarg Arg_0x9
 ckfinite
 mul conv.r4
 sub conv.r4 
 ble Branch_0x2
 br Branch_0x3
Branch_0x2:
Branch_0x3:

End_Orphan_1:
ldc.i4 55
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
ldc.r8 float64(0x94188556b24089e8b90c9c61f9f3088) 

call int32 ILGEN_0x1a182c40::Method_0x36cdd4e(float64 Arg_0x9)
ldc.i4 55
sub
ldc.i4 100
add
 ret
}

}
