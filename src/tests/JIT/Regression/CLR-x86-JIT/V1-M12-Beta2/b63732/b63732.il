// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x3180c570 {}
.assembly extern xunit.core {}
.class ILGEN_0x3180c570 {
.method static int32 Method_0xa3b20838(unsigned int8 Arg_0x0, unsigned int16 Arg_0x2, float32 Arg_0x3) {
.maxstack	9
Start_Orphan_0:
 ldarg Arg_0x3
 conv.ovf.u2
 starg Arg_0x2
End_Orphan_0:
 ldc.i4.0
 ldarg Arg_0x0
 ble Branch_0xe
 ldarg Arg_0x0
 br Branch_0xf
 Branch_0xe:
 ldarg Arg_0x2
 Branch_0xf:
 conv.ovf.i.un
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
 ldc.i4 0xac145d31 
 ldc.i4 0x9143606c 
 ldc.r4 float32(0xcf0915f2) 
 call int32 ILGEN_0x3180c570::Method_0xa3b20838(unsigned int8 Arg_0x0, unsigned int16 Arg_0x2, float32 Arg_0x3)
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
