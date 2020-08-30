// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x4d97cd70 {}
.class ILGEN_0x4d97cd70 {
.method static int32 Main() {
.entrypoint
.maxstack	2
//  ldc.i8 0x1fffff
  ldc.i8 0x200000
  neg
  conv.i4
  ldc.i4 0x200064
  add
  ret
}
}
