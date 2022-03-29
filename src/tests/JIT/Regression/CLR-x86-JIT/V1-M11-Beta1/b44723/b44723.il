// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b44723' {}
.assembly extern xunit.core {}
.class ILGEN_0xa4f49b04 {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.locals (unsigned int8[] local_0x6)
 ldc.i4 0x3
 ldc.i4 0x2
 bge Branch_0x0
 ldc.i4.3
 br Branch_0x1
Branch_0x0:
 ldc.i4.1
Branch_0x1:
 conv.ovf.i2
Start_Orphan_3:
 ldc.i4.2
 newarr [mscorlib]System.Byte
 stloc local_0x6
End_Orphan_3:
 ldc.i4.8
 mul.ovf.un
 ldc.i4.8
 sub
ldc.i4 100
add
 ret
}
}
