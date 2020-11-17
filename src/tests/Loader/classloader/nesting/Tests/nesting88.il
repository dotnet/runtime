// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
// model revision #2
// To compile: ilasm /dll client88.il

.assembly extern client88 {}
.assembly extern mscorlib{}

.assembly nesting88 {}

// Nesting module: PureManaged
// Nesting location: DLL

// enclosing type 
// kind: Class
// flag: abstract
// visibility: public
// generics: Nongeneric
// depends on nested: InstanceField

.class abstract public EnclType
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
    .field public static class EnclType enclType
  }

  // enclosing depends on nested: InstanceField
  // enclosing member visibility: public
  .field public class EnclType/NestedType sNestClass

  .method public specialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

// we need this to load EnclType (since it is an abstract class)
.class public Helper extends EnclType
{
  .method public specialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void EnclType::.ctor()
    ret
  }
}
