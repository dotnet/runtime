// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
// model revision #2
// To compile: ilasm /dll nesting3.il

.assembly extern mscorlib {}
.assembly nesting3{}

// Nesting module: PureManaged
// Nesting location: DLL

// enclosing type 
// kind: Class
// flag: abstract
// visibility: public
// generics: Generic
// depends on nested: InstanceField

.class abstract public EnclType<([mscorlib]System.Object) T>
{
  // nested type 
  // kind: Struct
  // flag: sealed
  // visibility: famorassem
  // generics: Generic
  // depends on enclosing: None

  .class nested famorassem value NestedType<([mscorlib]System.Object) M>
  {
    // nested depends on enclosing: None
  }

  // enclosing depends on nested: InstanceField
  // enclosing member visibility: assembly
  .field public valuetype EnclType/NestedType<int32> sNestClass

  .method public specialname instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}


// Client module: PureManaged
// Client location: DLL
// Client interaction: ConstructedTypeEncl

.class public Client<([mscorlib]System.Object) T>
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
     .locals init (valuetype EnclType/NestedType<int32> V_0)

     // Helper2 extends EnclType
     newobj instance void Helper2::.ctor()

     // load instance field of EnclType (which has NestedType)
     ldflda valuetype EnclType/NestedType<int32> class EnclType<int32>::sNestClass
     initobj valuetype EnclType/NestedType<int32>

     ret
  }
}

// we need this Helper class to load EnclType
.class public Helper2 extends class EnclType<int32>
{
  .method public specialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void class EnclType<int32>::.ctor()
    ret
  }

}   
