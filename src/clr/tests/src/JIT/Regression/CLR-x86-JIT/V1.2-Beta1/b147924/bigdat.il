// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly BIGDAT {}
.assembly extern mscorlib
{
}
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
    .entrypoint
            newobj    instance   void BIGDAT/WS_TYPE::.ctor ()
            stsfld      class BIGDAT/WS_TYPE BIGDAT::WS_DATA

            ldstr       "Pass"
            call        void [System.Console]System.Console::WriteLine(string)

            ldc.i4 100
            ret
  }
}
