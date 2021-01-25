// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14264
{
}
.class ILGEN_622380794 {

.field static	float64 field_0x3

.method static int32 main() {
.entrypoint
.maxstack	10

 .locals (float64 local_0x3)
 ldc.r8 0
 stloc.0
 ldsfld float64 ILGEN_622380794::field_0x3
 ldsfld float64 ILGEN_622380794::field_0x3
 ldsfld float64 ILGEN_622380794::field_0x3
 ldloc local_0x3
 sub
 sub
 ldloc local_0x3
 ldloc local_0x3
 add
 mul
 add
 conv.i2
ldc.i4 100
add
ret
}
}
