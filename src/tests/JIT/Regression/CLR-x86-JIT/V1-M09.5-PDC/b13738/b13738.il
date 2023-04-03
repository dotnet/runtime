// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b13738
{
}
.class public ILGEN_4 {
 .method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
.try
{
 ldc.i4 2
 ldc.i4 0xF0430a02
 mul.ovf.un
 pop
 leave erro
}
catch [mscorlib]System.OverflowException
{
 pop
 leave ok
}

erro:
 ldc.i4 1
 br    exit
ok:
 ldc.i4 100
exit:
 ret
}
}
