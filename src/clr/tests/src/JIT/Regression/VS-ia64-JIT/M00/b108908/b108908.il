// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x45f9b5e5 {}
.class ILGEN_0x45f9b5e5 {
.method static int32 Main() {
.entrypoint
.maxstack	9
    ldc.i4 1
    ldc.i4 0x2
   mul.ovf.un
   ldc.i4 2
  div
 ldc.i4 99
 add
 ret
}
}