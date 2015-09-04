// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b05740
{
}
.class _simple {

.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	100

ldc.i4 4
ldc.i4 4
mul.ovf
conv.ovf.i8
conv.i4
ldc.i4 16
sub

ldc.i4 100
add
ret
}
}