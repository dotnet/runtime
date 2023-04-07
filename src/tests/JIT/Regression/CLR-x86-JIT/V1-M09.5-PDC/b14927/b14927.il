// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b14927'{}
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
.maxstack	12

ldc.i4 0x7ead4549 
stsfld int32 ILGEN_622380794::field_0x0
ldc.i8 0x6ac2485cd412fcb 
stsfld int64 ILGEN_622380794::field_0x1
ldc.r4 float32(0x35595ddd) 
stsfld float32 ILGEN_622380794::field_0x2
ldc.r8 float64(0x518e5ed056af1d71) 
stsfld float64 ILGEN_622380794::field_0x3


 ldsfld int32 ILGEN_622380794::field_0x0
 conv.i
 conv.u8
 ldsflda float64 ILGEN_622380794::field_0x3
 ldc.i8 0x6cb0643642862c6e
 conv.r8
 stind.r8
 ldsflda int64 ILGEN_622380794::field_0x1
 ldind.i8
 ldsfld float32 ILGEN_622380794::field_0x2
 stsfld float32 ILGEN_622380794::field_0x2
 ldsfld float32 ILGEN_622380794::field_0x2
 conv.r4
 ldc.i4.1
 conv.r4
 clt.un
 shr.un
 conv.u4
 shr
 conv.i1
ldc.i4 100
add
ret
}
}
