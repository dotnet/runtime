// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
// model revision #2
// To compile: ilasm /dll nesting.il

.assembly extern mscorlib{}
.assembly nesting16{}

// Nesting module: PureManaged
// Nesting location: DLL

// enclosing type 
// kind: Struct
// flag: sealed
// visibility: public
// generics: Nongeneric
// depends on nested: MethodParam

.class public value EnclType
{
  // nested type 
  // kind: Struct
  // flag: sealed
  // visibility: famorassem
  // generics: Nongeneric
  // depends on enclosing: MethodParam

  .class nested famorassem value NestedType
  {
    // nested depends on enclosing: MethodParam
    // nested member visibility: assembly
    .method assembly instance void method2(valuetype EnclType enclType)
    {
      ret
    }
  }

  // enclosing depends on nested: MethodParam
  // enclosing member visibility: famorassem
  .method famorassem instance void method2(valuetype EnclType/NestedType nestClass)
  {
    ret
  }
}


// Client module: PureManaged
// Client location: DLL
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
    .locals init ( valuetype EnclType V_0,
                   valuetype EnclType/NestedType V_1)


    // create EnclType struct
    // create EnclType/NestedType struct

    ldloca.s   V_0
    initobj    EnclType
    ldloca.s   V_1
    initobj    EnclType/NestedType
    
    // call method2(valuetype EnclType/NestedType nestClass) in EnclType

    ldloca.s   V_0
    ldloc.1
    call instance void EnclType::method2(valuetype EnclType/NestedType nestClass)

    // call method2(valuetype EnclType enclType) in EnclType/NestedType

    ldloca.s   V_1
    ldloc.0
    call instance void EnclType/NestedType::method2(valuetype EnclType enclType)

    ret
  }
}
