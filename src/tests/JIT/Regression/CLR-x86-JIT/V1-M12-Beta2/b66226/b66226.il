// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x9b859686 {}
.class ILGEN_0x9b859686 {
.method static int32 Method_0xbe0b5ad5() {
.maxstack	9
 ldc.i8 0x8eb5516c4d2c04c
 ldc.i8 0xcba500615d8e16e3
 ldc.i8 0x371f9a5afaf80d94
 mul.ovf
 ble.un Branch_0x20
 ldc.i4.4
 br Branch_0x21
Branch_0x20:
 ldc.r4 float32(0xe4060d9e)
 ldc.r4 float32(0x2040af2b)
 bgt Branch_0x24
 ldc.i4.7
 br Branch_0x25
Branch_0x24:
 ldc.i4.s -7
Branch_0x25:
Branch_0x21:
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
 call int32 ILGEN_0x9b859686::Method_0xbe0b5ad5()
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
