// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xa09c7ca6 {}
.assembly extern xunit.core {}
.class ILGEN_0xa09c7ca6 {
.method static int32 Method_0x1ff34cdc(int64 Arg_0x1) {
.maxstack	9

 .locals (native int LOCAL_0x0)

ldc.i4 0x31cfb085 
stloc LOCAL_0x0

Start_Orphan_0:
Start_Orphan_1:
 ldc.i4.0
 brtrue Branch_0x2
 br Branch_0x3
Branch_0x2:
Branch_0x3:
End_Orphan_1:
 ldc.i4.5
 ldloc LOCAL_0x0
 blt Branch_0x4
 ldc.r8 float64(0xa1f6f5088dfa0de0)
 br Branch_0x5
Branch_0x4:
 ldc.r8 float64(0x2ca1eb8d527c149d)
Branch_0x5:
 conv.ovf.i8
ldc.i8 0x22b82bcf59019229 
 ble Branch_0x0
 ldloc LOCAL_0x0
 ldarg Arg_0x1
 conv.ovf.i2.un
 blt Branch_0xa
 br Branch_0xb
Branch_0xa:
 ldloc LOCAL_0x0
 pop
Branch_0xb:
 br Branch_0x1
Branch_0x0:
 ldloc LOCAL_0x0
 conv.ovf.u
 ldc.i4 0x432e72aa
 conv.ovf.i
 ble Branch_0xc
 br Branch_0xd
Branch_0xc:
Branch_0xd:
Branch_0x1:
End_Orphan_0:
 ldloc LOCAL_0x0
 conv.u2
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
ldc.i8 0x22b82bcf59019229 

call int32 ILGEN_0xa09c7ca6::Method_0x1ff34cdc(int64 Arg_0x1)
ldc.i4 45089
sub.ovf.un
 ret
}

}
