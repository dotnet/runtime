// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly rem.exe{}

.class public _rem {
.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
     ldc.r8	3.5
     ldc.r8	1.0
    rem
    ldc.r8 100
   mul
  conv.i4
  ldc.i4.2
  mul
 ret
}
}
