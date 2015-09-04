// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b04306
{
}
.class _simple {

.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	100

 ldc.i4 1
 ldc.i4 2
 ldc.i4 1
 sub
 ldc.i4 1
 ldc.i4 2
 mul
 mul
 add
ldc.i4 3
sub
ldc.i4 100
add
 ret
}
}