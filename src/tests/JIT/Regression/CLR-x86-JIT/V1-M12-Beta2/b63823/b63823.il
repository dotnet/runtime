// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b63823' {}
.assembly extern xunit.core {}
.class ILGEN_0x12b94a25 {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	2
.try {
 ldc.i4 0xe5829c93
 ldc.i8 0xc00748c6c8aa31a8
 conv.ovf.u2
 ble Branch_0x0
 ldc.i4 100
 br Branch_0x1
 Branch_0x0:
 ldc.i4.s 0
 conv.u1
 Branch_0x1:
 pop
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
