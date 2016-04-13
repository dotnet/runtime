// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types{}

.assembly Method_DefaultCtorAndClassConstraint_Pos {}



.class public auto ansi beforefieldinit D
         extends [mscorlib]System.Object
{

    // Generic method with class() and new() constraints
    .method public hidebysig instance void 
            method1<class .ctor T>() cil managed
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

.class public auto ansi beforefieldinit M_DefaultCtorAndClassConstraint 
         extends [mscorlib]System.Object
{
  // POSITIVE TEST
  // generic argument is a class with default ctor
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    
    newobj instance void class D::.ctor()
    call instance void class D::method1<class [types]ClassWithCtor>()
    ret
  }


  // POSITIVE TEST
  // generic argument is an mscorlib class with default ctor
  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1

    newobj instance void class D::.ctor()
    call instance void class D::method1<class [mscorlib]System.Object>()

    ret
  }

}

.class public auto ansi beforefieldinit M_DefaultCtorAndClassConstraintGenTypes 
         extends [mscorlib]System.Object
{
  // POSITIVE TEST
  // generic argument is a class with default ctor
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    
    newobj instance void class D::.ctor()
    call instance void class D::method1<class [types]GenClassWithCtor<int32>>()

    ret
  }
}
