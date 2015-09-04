// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xd3a45436 {}
.class ILGEN_0xd3a45436
{
 .method static int32 Main()
 {
  .entrypoint
   .maxstack	17
     ldc.i4.4
      ldc.i4.1
      ldc.i4.m1
     mul.ovf.un
    shr.un
    ldc.i4 100
    add
   ret
  }
}
