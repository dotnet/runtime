// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b04345
{
}
.class _simple {

.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	100

 ldc.i4 1
 ldc.i8 2
 ldc.i4 1
 conv.i8
 ldc.i8 1
 not
 add
 sub
 conv.i4
 sub
pop
ldc.i4 100
	ret
}
}