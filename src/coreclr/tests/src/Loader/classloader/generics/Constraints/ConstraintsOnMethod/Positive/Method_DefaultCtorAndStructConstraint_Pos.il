// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types{}

.assembly Method_DefaultCtorAndStructConstraint_Pos {}



.class public auto ansi beforefieldinit E
         extends [mscorlib]System.Object
{

    // Generic method with struct() and new() constraints
    .method public hidebysig instance void 
            method1<valuetype .ctor T>() cil managed
    {
      .maxstack  8
      ret
    }

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      .maxstack  8
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    } 
} 

.class public auto ansi beforefieldinit M_DefaultCtorAndStructConstraint 
         extends [mscorlib]System.Object
{

  // POSITIVE TEST
  // generic argument is a struct
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    newobj instance void class E::.ctor()
    call instance void class E::method1<valuetype [types]NonGenStruct>()

    ret
  }

  // POSITIVE TEST
  // generic argument is an enum
  .method public hidebysig static void Test8() cil managed
  {
    .maxstack  1

    newobj instance void class E::.ctor()
    call instance void class E::method1<valuetype [types]Enum1>()

    ret
  }


  // POSITIVE TEST
  // generic argument is an mscorlib struct
  .method public hidebysig static void Test9() cil managed
  {
    .maxstack  1

    newobj instance void class E::.ctor()
    call instance void class E::method1<valuetype [mscorlib]System.DateTime>()
    ret
  }

}

.class public auto ansi beforefieldinit M_DefaultCtorAndStructConstraintGenTypes 
         extends [mscorlib]System.Object
{

  // POSITIVE TEST
  // generic argument is a struct 
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    newobj instance void class E::.ctor()
    call instance void class E::method1<valuetype [types]GenStruct<int32>>()
    ret
  }


  // POSITIVE TEST
  // generic argument is an mscorlib generic struct
  .method public hidebysig static void Test6() cil managed
  {
    .maxstack  1

    newobj instance void class E::.ctor()
    call instance void class E::method1<valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<valuetype [types]NonGenStruct,int32>>()

    ret
  }

}




