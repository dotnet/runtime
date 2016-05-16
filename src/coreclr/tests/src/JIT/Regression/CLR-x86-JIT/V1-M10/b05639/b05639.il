// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b05639
{
}
.class _simple {

.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	100

 ldc.i8 0x20c67fc874f64b52
 ldc.i4.M1
 ldc.i4.M1
 clt.un
 conv.u8
 sub
 ldc.i8 1
 ldc.i8 6
 mul
 cgt
ldc.i4 1
sub

ldc.i4 100
add
ret
}
}