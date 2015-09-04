// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b04384
{
}
.class _simple {

.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	100

 ldc.i8 6
 ldc.i8 1
 neg
 ldc.i8 1
 neg
 add
 sub
 ldc.i8 1
 ldc.i8 1
 sub
 ldc.i8 1
 mul
 add
 conv.i4
pop
ldc.i4 100
 ret
}
}