// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b115932b' {}
.class public auto ansi Test_b115932b extends [mscorlib] System.Object {
.method public static int32 foo() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  2
  .locals init (string V_0,
           int32& V_1)
  IL_0002:  ldstr      "aaa"
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  stloc.1
  IL_000a:  ldloc.1
  ldind.i4
  IL_0012:  call       void [System.Console]System.Console::WriteLine(int32)
  ldc.i4 100
  IL_0017:  ret
}
}
