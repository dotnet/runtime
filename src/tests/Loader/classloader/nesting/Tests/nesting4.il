// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
// model revision #2
// To compile: ilasm nesting4.il

.assembly extern mscorlib {}
.assembly nesting4{}

// Nesting module: PureManaged
// Nesting location: MainModule

// enclosing type 
// kind: Class
// flag: None
// visibility: private
// generics: Nongeneric
// depends on nested: InstanceField

.class public EnclType
{
  // nested type 
  // kind: Interface
  // flag: abstract
  // visibility: public
  // generics: Nongeneric
  // depends on enclosing: StaticField

  .class interface abstract nested public NestedType
  {
    // nested depends on enclosing: StaticField
    // nested member visibility: public
    .field public static class EnclType sEnclType
  }

  // enclosing depends on nested: InstanceField
  // enclosing member visibility: famorassem
  .field famorassem class EnclType/NestedType sNestClass

  // entry point location: EnclosingType

  .method public static int32 Main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (class [mscorlib]System.Exception V_0,
                  int32 V_1)

    .try
    {
      newobj instance void class Client::.ctor()
      call instance void  Client::method1()
      ldstr      "PASS"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s   100
      stloc.1
      leave.s    end
    }
    catch [mscorlib]System.Exception 
    {
      stloc.0
      ldstr      "{0}Caught unexpected exception."
      ldloc.0
      call void [System.Console]System.Console::WriteLine(string,object)
      ldstr      "FAIL"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s   101
      stloc.1
      leave.s    end
    }
    end: ldloc.1
    ret
  }

  .method public specialname instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}


// Client module: PureManaged
// Client location: MainModule
// Client interaction: None

.class public Client
{

  .method public specialname instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
  .method public instance void method1()
  {
 
      // load enclosing and nested types

      newobj instance void class EnclType::.ctor()
      stsfld class EnclType class EnclType/NestedType::sEnclType
      ldsfld class EnclType class EnclType/NestedType::sEnclType

      // Helper1 implements EnclType/NestedType

      newobj instance void class Helper1::.ctor()
      stfld class EnclType/NestedType class EnclType::sNestClass
      ret
  }
}

.class public Helper1 implements EnclType/NestedType
{
  .method public specialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }

}   
