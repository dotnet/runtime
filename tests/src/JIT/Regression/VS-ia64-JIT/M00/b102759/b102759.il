// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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