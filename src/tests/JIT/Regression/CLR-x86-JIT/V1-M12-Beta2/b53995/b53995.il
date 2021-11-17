// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1c45e3d8 {}
.assembly extern xunit.core {}
.class ILGEN_0x1c45e3d8 {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
 .locals (unsigned int64 local_0x5)
 .try {
 ldc.i8 0xfdde291f2b0cf6d 
 stloc local_0x5
 ldc.i8 3
 ldloc local_0x5
 ldc.i4 5
 shl
 ldc.i8 0x7b82b0ef900b256
 add.ovf.un
 ceq
 pop
 leave EEE
 } catch [mscorlib]System.OverflowException { 
 pop
 leave EEE
 }
 EEE:
 ldc.i4 100
 ret
}
}
