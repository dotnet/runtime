// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b353858' {}

.method assembly static void CreateArrayOfVoid() noinlining
{
    .maxstack 1
    ldc.i4.s   10000
    newarr     [mscorlib]System.Void
    pop
    ret
}


.class public auto ansi Test_b353858 extends [mscorlib] System.Object {
.method public static int32  main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, valuetype [Microsoft.DotNet.XUnitExtensions]Xunit.TestRuntimes) = {
      string('https://github.com/dotnet/runtime/issues/54393')
      int32(0x2) // Mono
  }
  .entrypoint
  .maxstack  1

  .try
  {
    call void CreateArrayOfVoid()
    leave.s    FAIL
  }
  catch [mscorlib]System.InvalidProgramException
  {
    pop
    leave.s    PASS
  }

FAIL:
  ldstr      "FAIL"
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  br.s       END

PASS:
  ldstr      "PASS"
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4     100
  br.s       END

END:

  ret
}
}
