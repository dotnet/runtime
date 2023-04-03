// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14070
{
}
.class public ILGEN_622380794 {

.field static	int64 field_0x1

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

 .locals (float32 local_0x2)
 ldc.r4 0
 stloc.0
 
 ldsflda int64 ILGEN_622380794::field_0x1
 ldind.i8
 conv.u
 ldloc local_0x2
 conv.u1
 cgt
ldc.i4 100
add
ret
}
}
