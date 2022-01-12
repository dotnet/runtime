// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x60721d0a {}
.assembly extern xunit.core {}
.class ILGEN_0x60721d0a {

.field static	unsigned int32 field_0x0
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

 ldc.i4 0x8ce36fc4 
 stsfld unsigned int32 ILGEN_0x60721d0a::field_0x0

 ldc.i4.7
 ldsfld unsigned int32 ILGEN_0x60721d0a::field_0x0
 ble Branch_0x5
 
 ldc.r8 float64(0x20b6764a1cc77bcc)

 ldc.i4.6
 ldsfld unsigned int32 ILGEN_0x60721d0a::field_0x0
 bge Branch_0x4c
 br Branch_0x4d
 Branch_0x4c:
 Branch_0x4d:

 pop
 Branch_0x5:

 ldc.i4 100
 ret
}
}
