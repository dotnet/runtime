// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14067a
{
}
.class ILGEN_622380794 {

.field static	int64 field_0x5

.method static int32 main() {
.entrypoint
.maxstack	20

 ldsflda int64 ILGEN_622380794::field_0x5
 ldind.i8
 conv.u
 conv.r.un
 conv.i4
ldc.i4 100
add
 ret
}
}
