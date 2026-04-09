// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
// model revision #1
// to compile: ilasm nesting31.il

// Nesting and Client in the same module

.assembly extern mscorlib{}
.assembly nesting31{}

// Nesting module: Pure Managed
// Nesting location: MainModule

// enclosing type
// kind: class
// flag: abstract
// visibility: public
// generics: nongeneric
// depends on nested: extends


.class abstract public EnclType extends EnclType/NestedType
{

  // nested type
  // kind: class
  // flag: none
  // visibility: public
  // generics: nongeneric
  // depends on enclosing: none

  .class nested public NestedType
  {

     // nested member visibility: assembly
    .field assembly int32 i

    .method public specialname instance void .ctor() cil managed
    {
      .maxstack 8
      ldarg.0
      call instance void [mscorlib]System.Object::.ctor()
      ret
    }
  }

  // enclosing member visibility: assembly
  .field assembly int32 i


  // entrypoint location: Enclosing type
  .method public static int32 Main()
  {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint

      .locals init (class Client V_0,
               class [mscorlib]System.Exception V_1,
               int32 V_2)
      .try
      {
        newobj     instance void Client::.ctor()
        stloc.0
        ldloc.0
        call instance void Client::method4()

        ldstr      "PASS"
        call       void [System.Console]System.Console::WriteLine(string)
        ldc.i4.s   100
        stloc.2
        leave.s    end
      }
      catch [mscorlib]System.Exception
      {
        stloc.1
        ldstr      "{0}Caught unexpected exception."
        ldloc.1
        call       void [System.Console]System.Console::WriteLine(string,object)
        ldstr      "FAIL"
        call       void [System.Console]System.Console::WriteLine(string)
        ldc.i4.s   101
        stloc.2
        leave.s    end
      }
      end: ldloc.2
      ret
  }
}

// Client module: Pure Managed
// Client location: MainModule
// Client interaction: none

.class public Client
{

  .method public specialname instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }

  // since there is no interaction between client and nested/enclosing types
  // method4() is a helper method that allows us to load nested and enclosing types
  .method public void method4()
  {
    .locals init (class EnclType/NestedType V_0)
    newobj instance void class EnclType/NestedType::.ctor()

    // we need this to make sure we test the member of nested type
    stloc.0
    ldloc.0
    ldc.i4.5
    stfld int32 class EnclType/NestedType::i
    ret
  }
}
