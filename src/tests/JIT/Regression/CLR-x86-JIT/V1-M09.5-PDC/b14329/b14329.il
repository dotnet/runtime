// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14329
{
}
.class ILGEN_622380794 {

.field static	int32 field_0x4

.method static int32 main() {
.entrypoint
.maxstack	20

 ldc.i4 50
 stsfld int32 ILGEN_622380794::field_0x4

 ldsfld int32 ILGEN_622380794::field_0x4
// ldc.i4 0xcb546abe
 ldc.i4 0x909090be
 shl
 stsfld int32 ILGEN_622380794::field_0x4

ldc.i4 100
ret
}
}
