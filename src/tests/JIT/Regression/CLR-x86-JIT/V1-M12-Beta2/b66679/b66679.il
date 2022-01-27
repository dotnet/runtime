// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b66679' {}
.assembly extern xunit.core {}
.class ILGEN_0xb15094d8 {
.method static int32 Method_0xddc6a26d() {
.maxstack	7
 ldc.i4.5
 ldc.r4 float32(0x91167b0a)
 ldc.r4 float32(0x1893b736)
 bne.un Branch_0x8
 ldc.i4.1
 br Branch_0x9
Branch_0x8:
 ldc.i4.6
Branch_0x9:
 bge Branch_0x4
 ldc.i8 0x96ad296f7bd3ee28
 conv.ovf.u4.un
 br Branch_0x5
Branch_0x4:
 ldc.i4.3
 ldc.i4.s 13
 xor
Branch_0x5:
 ldc.i4.1
 ble Branch_0x0
 ldc.i4.2
 ldc.i4.4
 ceq
 br Branch_0x1
 Branch_0x0:
 ldc.i4.s -46
 conv.ovf.u1
Branch_0x1:
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
 call int32 ILGEN_0xb15094d8::Method_0xddc6a26d()
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
