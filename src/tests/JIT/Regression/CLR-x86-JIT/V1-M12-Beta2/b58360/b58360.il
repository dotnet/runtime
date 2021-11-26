// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xa2afe952 {}
.assembly extern xunit.core {}
.class ILGEN_0xa2afe952 {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	11
.try {
 ldc.i4 0x8
Start_Orphan_d:
 ldc.i4 1 
Loop_0x0:
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x0
 pop
Loop_0x1:
End_Orphan_d:
 ldc.i4 0xcfb8e612
 conv.ovf.u8
 ldc.i8 88
 clt.un
 clt.un
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
