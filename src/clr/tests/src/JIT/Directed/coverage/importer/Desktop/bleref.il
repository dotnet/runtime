// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern legacy library mscorlib {}
.assembly bleref { }
.method public static int32 f()
{
ldnull
ldnull
ble OUT
OUT:
ldc.i4 100
ret
}
  .method public static int32 Main() cil managed
  {
    .entrypoint
    .maxstack 5
    .try
    {
	call int32 f()
        leave.s FAIL
    }
    catch [mscorlib]System.Security.VerificationException
    {
        pop
        leave.s PASS
    }
    FAIL:
	ldstr "Should have caught System.Security.VerificationException"
	call void [System.Console]System.Console::WriteLine(string)
	ldstr "FAILED"
	call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 1
	ret
    PASS:
	ldstr "Caught System.Security.VerificationException as expected"
	call void [System.Console]System.Console::WriteLine(string)
	ldstr "PASSED"
	call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 100
	ret
  }
