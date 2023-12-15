// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern legacy library mscorlib {}
.assembly legacy library ASSEMBLY_NAME { }
.class public auto ansi beforefieldinit nullsdarr
       extends [mscorlib]System.Object
{
  .method public static void f() cil managed
  {
    .maxstack  1
    IL_0000:  ldnull
    IL_0003:  ldlen
    IL_0004:  conv.i4
    IL_0005:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0010:  ret
  }
  .method public static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack 5
    .try
    {
	call void nullsdarr::f()
        leave.s FAIL
    }
    catch [mscorlib]System.NullReferenceException
    {
        pop
        leave.s PASS
    }
    FAIL:
	ldstr "Should have caught System.NullReferenceException"
	call void [System.Console]System.Console::WriteLine(string)
	ldstr "FAILED"
	call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 1
	ret
    PASS:
	ldstr "Caught System.NullReferenceException as expected"
	call void [System.Console]System.Console::WriteLine(string)
	ldstr "PASSED"
	call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 100
	ret
  }
}
