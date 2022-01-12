// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x46d17ff0 {}
.assembly extern xunit.core {}
.class ILGEN_0x46d17ff0 {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	9
.try {
 ldc.i4.7
 ldc.i4 0xc0498fb0
 mul
 ldc.i8 0xf89ca96f34f7bb1e
 ldc.i8 0xae841990b9bf1c31
 bgt Branch_0x2
 ldc.i4.4
 br Branch_0x3
Branch_0x2:
 ldc.i4.4
Branch_0x3:
 mul.ovf
 ldc.r8 float64(0x107ae1e85afcb9ce)
 ldc.r8 float64(0xf9d915853926bcf4)
 bgt.un Branch_0x4
 ldc.i4 0x4ca09e33
 br Branch_0x5
Branch_0x4:
 ldc.i4.0
Branch_0x5:
 ble Branch_0x0
 ldc.i4 0x9c8f179c
Branch_0x9:
 br Branch_0x1
Branch_0x0:
 ldc.i4 0x41c7a2fd 
 conv.ovf.u2
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
