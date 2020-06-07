// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b06436
{
}
.class _simple {

.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	100

   ldc.i4 0x21221699
   
     ldc.i4 0x21221699
     ldc.i4 8
    clt.un
    conv.i8
    ldc.r4 float32(0x6d6a6640)
    conv.u8
    ldc.i8 3
    ldc.i4 2
    ldc.i4 4
   cgt.un
   conv.u8
   sub
  sub
 or
 conv.i2
or
pop
ldc.i4 100
ret
}
}