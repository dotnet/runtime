// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ceeillegal {}
.method public static void f()
{
.emitbyte 0xee
ret
}
.method public static int32 Main()
{
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
