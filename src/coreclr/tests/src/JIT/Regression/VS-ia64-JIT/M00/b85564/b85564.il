// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
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
