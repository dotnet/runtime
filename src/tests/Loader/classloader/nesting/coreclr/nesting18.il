// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
// model revision #1
// to compile: ilasm nesting18.il

// Nesting and Client in the same module

.assembly extern mscorlib{}
.assembly nesting18 {}

// Nesting module: Pure Managed
// Nesting location: MainModule

// enclosing type 
// kind: class
// flag: abstract
// visibility: private
// generics: nongeneric
// depends on nested: static field

.class public abstract EnclType
{
   // enclosing member visibility: assembly
  .field assembly static class EnclType/NestedType nestField

  // nested type 
  // kind: class
  // flag: sealed
  // visibility: public
  // generics: nongeneric
  // depends on enclosing: extends

  .class sealed nested public NestedType extends EnclType
  {

    // nested member visibility: famorassem 
    .field famorassem static class EnclType/NestedType nestField


    // entry point location: nested type
    .method public static int32 Main()
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
 
      .locals init (class Client<class EnclType/NestedType> V_0,
               class [mscorlib]System.Exception V_1,
               int32 V_2)
      .try
      {
        newobj     instance void class Client<class EnclType/NestedType>::.ctor()
        pop
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

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      .maxstack  8
      ldarg.0
      call       instance void EnclType::.ctor()
      ret
    }

   // need static constructor to initialize the static field
    .method public hidebysig specialname rtspecialname static void .cctor() il managed
    {
      newobj     instance void EnclType/NestedType::.ctor()
      stsfld     class EnclType/NestedType EnclType/NestedType::nestField
      ret
    }
  } 

 
  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }  

} 


// Client module: Pure Managed
// Client location: MainModule
// Client interaction: constructed type nested

.class public Client<([mscorlib]System.Object) T>
{

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } 
} 
