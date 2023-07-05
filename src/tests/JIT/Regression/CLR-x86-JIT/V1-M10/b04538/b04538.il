// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b04538
{
}
.class public _simple {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	100

 ldc.i4 8
 conv.i2
 ldc.i4 5
 ldc.i4 1
 conv.i1
 ldc.i8 0x254136adb6fdf0ed
 not
 conv.i1
 add
 sub
 ldc.i4 4
 ldc.i4 4
 mul
 ldc.i4 4
 sub
 conv.i1
 mul
 sub
 ldc.r4 9.300000e+001
 ldc.r8 1.850000e+002
 conv.r4
 add
 conv.i2
 sub
pop
ldc.i4 100
 ret
}
}
