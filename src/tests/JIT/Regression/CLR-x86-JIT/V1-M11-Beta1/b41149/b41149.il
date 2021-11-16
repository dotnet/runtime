// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x243bc9d5 {}
.assembly extern xunit.core {}
.class ILGEN_0x243bc9d5 {

.method static int32 Method_0xab68837a(unsigned int64 Arg_0x1) {
.maxstack	13

 .locals (int64 local_0x0)

 ldc.i4 55
 ldc.i4 0x9
 ble Branch_0x0
 ldc.i4.4
 ldc.i4.3
 bne.un Branch_0x8
 ldc.i4.4
 br Branch_0x9
Branch_0x8:
 ldc.i4.7
Branch_0x9:
 conv.ovf.u2
 br Branch_0x1
Branch_0x0:
 ldc.i4 0x7 
Branch_0x1:

Start_Orphan_3:
 ldarg Arg_0x1
 stloc local_0x0
End_Orphan_3:

ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
ldc.i8 0x432d7dff7e4e94b56adbd9362d85e958 

call int32 ILGEN_0x243bc9d5::Method_0xab68837a(unsigned int64 Arg_0x1)
ldc.i4.7
sub
ldc.i4 100
add
 ret
}

}
