// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly 'test_404708' {}

.namespace Test
{
  .class public auto ansi beforefieldinit Class1 extends [mscorlib]System.Object
  {

    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
    {
      .maxstack  8
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    }


    .method public hidebysig instance void Clear() cil managed
    {
      .maxstack  2

      .try
      {
        leave.s    DONE
      }
      finally
      {
        ldarg.0
        ldnull
        pop
        pop
        endfinally
      }

      DONE:
      ret
    }


    .method public hidebysig static int32 Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  1

      newobj     instance void Test.Class1::.ctor()
      callvirt   instance void Test.Class1::Clear()

      ldstr      "Passed"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4     100
      ret
    }


  }
  
}

