// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14135 { }
.class public ILGEN_622380794 {

.field static	int32 field_0x0
.field static	int64 field_0x1

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10

 .locals (int64 local_0x1,float32 local_0x2)
 ldc.i8 0
 stloc.0
 ldc.r4 0
 stloc.1
 
 ldsflda int64 ILGEN_622380794::field_0x1
 ldloc local_0x1
 stind.i8
 ldc.i4 4
 ldc.i4 2
 ldc.i4 5
 mul
 add
 conv.r4
 stloc local_0x2
 ldsfld int32 ILGEN_622380794::field_0x0
 ldc.i4 3
 mul
 neg

ldc.i4 100
add
 ret
}
}
