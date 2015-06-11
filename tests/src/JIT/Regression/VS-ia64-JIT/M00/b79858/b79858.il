// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly ILGEN_0x8275b048 {}
.class ILGEN_0x8275b048 {
.method static void  Method_0x98c7d74e() {
.maxstack	11
     ldc.i4.s 5
    conv.i
     ldc.i4 0x446ecca
   xor
  pop
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	3
  call void  ILGEN_0x8275b048::Method_0x98c7d74e()
  ldc.i4 100
 ret
}
}