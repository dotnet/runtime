// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
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