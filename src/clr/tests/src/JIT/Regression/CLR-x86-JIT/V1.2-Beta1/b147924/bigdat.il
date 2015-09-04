// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly BIGDAT {}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89)                         // ".z\V.4.."
  .ver 1:0:5000:0
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
