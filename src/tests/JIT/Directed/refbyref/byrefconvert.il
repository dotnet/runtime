// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME {}
.module byrefconvert.exe
.class public auto ansi byrefconvert {
.method public static int32 Main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  2
  .locals init (string V_0,
           uint8& V_1)
  ldstr      "aaa"
  stloc.0
  ldloc.0
  stloc.1
  ldloc.1
  conv.i4
  call       string [mscorlib]System.Convert::ToString(int32)
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4 100
  ret
}
}
