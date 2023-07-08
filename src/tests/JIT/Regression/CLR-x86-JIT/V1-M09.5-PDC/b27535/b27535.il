// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b27535' {}
.class public stelem_test {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
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
