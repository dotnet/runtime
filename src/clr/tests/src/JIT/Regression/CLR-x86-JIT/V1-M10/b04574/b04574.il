// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b04574
{
}
.class _simple {

.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	100

.locals (int32 A)
 ldc.i4 1
 stloc.0

 ldc.r4 6
 conv.r4
 ldc.r4 64
 add
 conv.i4
 ldc.i4 1
 ldc.i4 2
 conv.i4
 add
 add
 stloc.0

 ldc.i4 100
 ret
}
}