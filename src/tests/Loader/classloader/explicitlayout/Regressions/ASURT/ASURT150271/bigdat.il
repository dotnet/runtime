// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
.assembly BIGDAT {}
.assembly extern mscorlib{}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.module BIGDAT
.class public 'BIGDAT'
{
  .class nested private ansi explicit WS_TYPE
  {
    .field [100000008] public object EXCEPTION_OBJECT_00000242
    .size 100000016
    .method public specialname rtspecialname instance void .ctor ()
    {
              ldarg.0
              call        instance void object ::.ctor ()
              ret
    }
  }
  .field static private class BIGDAT/WS_TYPE WS_DATA

  .method static public int32 BIGDAT()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, valuetype [Microsoft.DotNet.XUnitExtensions]Xunit.TestRuntimes) = {string('needs triage') int32(2)}
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.OuterLoopAttribute::.ctor() = { }
    .entrypoint
            newobj      instance void BIGDAT/WS_TYPE::.ctor ()
            stsfld      class BIGDAT/WS_TYPE BIGDAT::WS_DATA

            ldstr       "Pass"
            call        void [System.Console]System.Console::WriteLine(string)

            ldc.i4 100
            ret
  }
}
