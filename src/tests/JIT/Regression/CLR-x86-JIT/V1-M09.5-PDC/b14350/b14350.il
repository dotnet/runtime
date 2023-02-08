// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14350 { }
.class public ILGEN_622380794 {

.field static	int32 field_0x0
.field static	int64 field_0x1
.field static	float32 field_0x2
.field static	float64 field_0x3

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

 ldsflda float64 ILGEN_622380794::field_0x3
 ldsfld float64 ILGEN_622380794::field_0x3
 ldsflda int64 ILGEN_622380794::field_0x1
 ldsfld int64 ILGEN_622380794::field_0x1
 stind.i8
 ldsfld float32 ILGEN_622380794::field_0x2
 //conv.r.un
 sub
 ldsflda int32 ILGEN_622380794::field_0x0
 ldsfld float64 ILGEN_622380794::field_0x3
 conv.i1
 conv.u
 stind.i4
 pop
 ldsfld int64 ILGEN_622380794::field_0x1
 conv.r.un
 stind.r8
 ldc.i4 100
 ret
}
}
