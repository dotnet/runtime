// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b55923' {}
.assembly extern xunit.core {}
.class ILGEN_0x3194e02a {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	9

 .locals (int64 local_0x0,native int local_0x2)
 .try {
 ldc.i8 0xb27db26754a58e66 
 stloc local_0x0
 ldc.i4 0xf76d8c05 
 stloc local_0x2

 ldc.i8 0xd5c733776321b0ea
 brtrue Branch_0x2
 ldloc local_0x2
 br Branch_0x3
Branch_0x2:
 ldloc local_0x2
Branch_0x3:
 conv.ovf.u4
 ldc.i4.3
 blt.un Branch_0x0
 ldc.r4 float32(0xf3ecd3cd)
 conv.i4
 ldloc local_0x0
 conv.ovf.u2
 ble.un Branch_0x4
 br Branch_0x5
Branch_0x4:
Branch_0x5:
 br Branch_0x1
Branch_0x0:
Branch_0x1:
 leave END
} catch [mscorlib]System.OverflowException {
 pop
 leave END
}
END:
ldc.i4 100
ret
}

}
