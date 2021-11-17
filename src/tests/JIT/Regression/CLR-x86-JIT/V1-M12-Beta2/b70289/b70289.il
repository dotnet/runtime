// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x8d1cfa06 {}
.assembly extern xunit.core {}
.class ILGEN_0x8d1cfa06 {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {

 ldc.i4 0xdbb08029 
 ldc.i4.s 112
 bgt.un Branch_0x2
 ldc.r4 float32(0xda3975af)
 br Branch_0x3
Branch_0x2:
 ldc.i4 0x74f929bf 
 ldc.i4.3
 beq Branch_0x8
 ldc.r8 float64(0x206b6e7a4e3b3ee5)
 br Branch_0x9
Branch_0x8:
 ldc.r4 float32(0x7c0c8a0c)
Branch_0x9:
Branch_0x3:
 ldc.i8 0x9715a29058031ffb
 conv.ovf.i4
 conv.r.un
 bne.un Branch_0x0
 ldc.i4.s -113
 br Branch_0x1
Branch_0x0:
 ldc.i4 100
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
