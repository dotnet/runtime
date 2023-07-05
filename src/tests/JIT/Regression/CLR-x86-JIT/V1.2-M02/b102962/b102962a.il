// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b102962a {}
.class public auto ansi Test_b102962a extends [mscorlib] System.Object {
.method public static int32
        main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  2
  .locals init (unsigned int8& V_0,
           string V_1,
           char& V_2)
  IL_0000:  ldnull
  IL_0001:  stloc.1
  IL_0002:  ldstr      "hello"
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.s   12
  IL_000d:  add
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  stloc.2
  IL_0011:  ldloc.2
  IL_0012:  ldind.u2
  IL_0013:  call       void [System.Console]System.Console::WriteLine(char)
  IL_0018:  ldc.i4 100
  IL_0019:  ret
}
}
