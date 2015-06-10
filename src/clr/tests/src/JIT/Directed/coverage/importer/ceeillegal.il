// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
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
call void [mscorlib]System.Console::WriteLine(string)
leave.s PASS
}
FAIL:
ldstr "FAILED"
call void [mscorlib]System.Console::WriteLine(string)
ldc.i4 1
ret
PASS:
ldc.i4 100
ret
}
