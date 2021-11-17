// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x77e9b0c3 {}
.assembly extern xunit.core {}
.class ILGEN_0x77e9b0c3 {

.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
 ldc.r4 4.0
 ldc.r4 5.0
 add conv.r4
 neg conv.r4
 conv.i4
 ldc.i4 109
 add
 ret
}
}
