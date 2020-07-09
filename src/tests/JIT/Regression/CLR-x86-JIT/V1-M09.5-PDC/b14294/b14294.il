// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14294
{
}
.class ILGEN_622380794 {

.field static	int32 field_0x0

.method static int32 main() {
.entrypoint
.maxstack	20

 ldc.i8 0x79c7a1e3779104a
 conv.i
 ldc.i8 0x4b9d41986e337371
 conv.r.un
 conv.i
 clt
 conv.i8
 ldsflda int32 ILGEN_622380794::field_0x0
 ldind.i4
 conv.i8
 mul
 conv.u4
ldc.i4 100
add
ret
}
}
