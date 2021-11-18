// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x144897ef {}
.assembly extern xunit.core {}
.class ILGEN_0x144897ef {
.field static	unsigned int8 field_0x4
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

 ldsfld unsigned int8 ILGEN_0x144897ef::field_0x4
 neg
 stsfld unsigned int8 ILGEN_0x144897ef::field_0x4

 ldc.i4 100
 ret
}
}
