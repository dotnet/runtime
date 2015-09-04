// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly s.exe{}
.class public s {
.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	10
     ldc.r8	5.5
     ldc.r8	1.25
    sub
    ldc.r8 100
   mul
  conv.i4
  ldc.i4 325
  sub
 ret
}
}
