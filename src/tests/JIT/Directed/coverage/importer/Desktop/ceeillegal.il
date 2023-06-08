// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern legacy library mscorlib {}
.assembly ASSEMBLY_NAME { }
.method public static void f()
{
.emitbyte 0xee
ret
}
.class public auto ansi ceeillegal {
.method public static int32 Main()
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.try
{
call void f()
leave.s FAIL
}
catch [mscorlib]System.InvalidProgramException
{
ldstr "PASSED"
call void [System.Console]System.Console::WriteLine(string)
leave.s PASS
}
FAIL:
ldstr "FAILED"
call void [System.Console]System.Console::WriteLine(string)
ldc.i4 1
ret
PASS:
ldc.i4 100
ret
}
}
