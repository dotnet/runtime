// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14068
{
}
.class public ILGEN_622380794 {

.field static	int64 field_0x5
.field static	float32 field_0xa

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
 ldsflda int64 ILGEN_622380794::field_0x5
 ldsflda float32 ILGEN_622380794::field_0xa
 ldc.r4 4.4
 stind.r4
 ldc.r8 3.982374e+012
 conv.ovf.u2
 conv.u8
 stind.i8
 leave stop
}
catch [mscorlib]System.OverflowException {
 pop
 leave stop
}
stop:
 ldc.i4 100
 ret
}
}
