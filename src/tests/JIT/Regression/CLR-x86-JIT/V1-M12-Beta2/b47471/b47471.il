// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x2e3bca41 {}
.assembly extern xunit.core {}
.class ILGEN_0x2e3bca41 {
.method static int32 Method_0x76a09634() {
.maxstack	1
 ldc.i4.2
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
 call int32 ILGEN_0x2e3bca41::Method_0x76a09634()
 ldc.i4.2
 sub
ldc.i4 100
add
 ret
}
}
