// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly stelem_test {}
.class stelem_test {

.method static int32 main() {
.entrypoint
.maxstack	17

 ldc.i4 255
 newarr [mscorlib]System.Single 
 dup

 ldc.i4 3
 ldc.r8 4.4
 stelem.r4 //NOTE: we are taking an R8 off the stack!

 ldc.i4 3
 ldelem.r4
 conv.i4
 ldc.i4 4
 sub
ldc.i4 100
add
 ret
}
}
