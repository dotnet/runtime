// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly subovfun1 {}
.method public static uint32 f(uint32 arg)
{
ldarg.0
ldc.i4 1
sub.ovf.un
ret
}
.class public auto ansi subovfun1 {
.method public static int32 Main()
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
ldc.i4 101
call uint32 f(uint32)
conv.i4
ret
}
}
