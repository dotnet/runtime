// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
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