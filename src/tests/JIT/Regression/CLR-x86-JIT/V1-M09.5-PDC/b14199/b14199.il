// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14199
{
}
.class public ILGEN_622380794 {


.field static	int64 field_0x1
.field static	float64 field_0x3

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10

 .locals (int32 local_0x0,float64 local_0x3)
 ldc.i4.0
 stloc.0
 ldc.r8 0
 stloc.1

 ldsflda int64 ILGEN_622380794::field_0x1
 ldsflda float64 ILGEN_622380794::field_0x3
 ldloc local_0x3
 stind.r8
 ldloca local_0x0
 ldind.i4
 conv.u8
 stind.i8
 ldsfld float64 ILGEN_622380794::field_0x3
 conv.u

ldc.i4 100
add
ret
}
}
