// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types{}

.assembly Instantiation_DefaultCtorConstraint_Neg {}


// Generic class with new() constraint
.class public auto ansi beforefieldinit A`1<.ctor T>
         extends [mscorlib]System.Object
{ 

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      .maxstack  8
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
     ret
    } 

} 

.class public auto ansi beforefieldinit DefaultCtorConstraint 
         extends [mscorlib]System.Object
{

  // NEGATIVE TEST
  // generic argument is a class with no default ctor
  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
   
    newobj     instance void class A`1<class [types]ClassNoCtor>::.ctor()
    pop
    

    ret
  }


  // NEGATIVE TEST
  // generic argument is a delegate (doesn't have public parameterless constructor)
  .method public hidebysig static void Test4() cil managed
  {
    .maxstack  1
    
    newobj     instance void class A`1<class [types]Delegate1>::.ctor()
    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is an mscorlib abstract class with no default ctor

  .method public hidebysig static void Test6() cil managed
  {
    .maxstack  1
    
    newobj     instance void class A`1<class [mscorlib]System.ValueType>::.ctor()
    pop
    ret
  }


  // NEGATIVE TEST
  // generic argument is an interface with no default ctor
  .method public hidebysig static void Test7() cil managed
  {
    .maxstack  1

    newobj     instance void class A`1<class [types]NonGenInterface>::.ctor()
    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is an array of classes with default ctor
  .method public hidebysig static void Test9() cil managed
  {
    .maxstack  1
    
    newobj     instance void class A`1<class [types]ClassWithCtor[]>::.ctor()
    pop

    ret
  }

  // generic argument is NUllable<T>
  .method public hidebysig static void Test10<M>() cil managed
  {
    .maxstack  1

    newobj     instance void class A`1<!!M>::.ctor()
    pop

    ret
  }


}

.class public auto ansi beforefieldinit DefaultCtorConstraintGenTypes
         extends [mscorlib]System.Object
{

  // NEGATIVE TEST
  // generic argument is a class with no default ctor
  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
   
    newobj     instance void class A`1<class [types]GenClassNoCtor<int32>>::.ctor()
    pop
    
    ret
  }



  // NEGATIVE TEST
  // generic argument is an interface
  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1

    newobj     instance void class A`1<class [types]GenInterface<int32>>::.ctor()
    pop

    ret
  }

}




