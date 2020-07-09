// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x6bcd7c1d {}
.class ILGEN_0x6bcd7c1d {
.method static int32 Method_0xda4a4950(unsigned int16 Arg_0x1) {
.maxstack	9

Start_Orphan_0:
 ldc.i4.1
 ldc.i4.0
 bne.un Branch_0x4
 ldc.i4.0
 br Branch_0x5
Branch_0x4:
 ldarg Arg_0x1
Branch_0x5:
 conv.ovf.i.un
 ldc.i8 0x4b13c5f6cf9f38ea
 conv.i4
 ble Branch_0x0
 br Branch_0x1
Branch_0x0:
Branch_0x1:
End_Orphan_0:
 ldc.i4.8
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
 ldc.i4 0x99045c3e 
 call int32 ILGEN_0x6bcd7c1d::Method_0xda4a4950(unsigned int16 Arg_0x1)
 ldc.i4 92
 add
 ret
}
}
