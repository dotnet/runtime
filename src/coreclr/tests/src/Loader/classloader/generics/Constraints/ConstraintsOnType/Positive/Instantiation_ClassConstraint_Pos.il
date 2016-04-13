// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types{}

.assembly Instantiation_ClassConstraint_Pos {}


// Generic class with class() constraint
.class public auto ansi beforefieldinit B`1<class T>
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

.class public auto ansi beforefieldinit ClassConstraint 
         extends [mscorlib]System.Object
{
  // POSITIVE TEST
  // generic argument is a class with default ctor
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    
    newobj     instance void class B`1<class [types]ClassWithCtor>::.ctor()
    pop

    ret
  }

  // POSITIVE TEST
  // generic argument is a class with no default ctor
  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
   
    newobj     instance void class B`1<class [types]ClassNoCtor>::.ctor()
    pop
    
    ret
  }


  // POSITIVE TEST
  // generic argument is a delegate
  .method public hidebysig static void Test4() cil managed
  {
    .maxstack  1
    
    newobj     instance void class B`1<class [types]Delegate1>::.ctor()
    pop

    ret
  }


  // POSITIVE TEST
  // generic argument is an mscorlib class with default ctor
  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1

    newobj     instance void class B`1<class [mscorlib]System.Object>::.ctor()
    pop

    ret
  }



  // POSITIVE TEST
  // generic argument is an mscorlib abstract class with no default ctor

  .method public hidebysig static void Test6() cil managed
  {
    .maxstack  1

    
    newobj     instance void class B`1<class [mscorlib]System.ValueType>::.ctor()
    pop
    ret
  }


  // POSITIVE TEST
  // generic argument is an interface 
  .method public hidebysig static void Test7() cil managed
  {
    .maxstack  1

    newobj     instance void class B`1<class [types]NonGenInterface>::.ctor()
    pop

    ret
  }

  // POSITIVE TEST
  // generic argument is an array of classes with default ctor
  .method public hidebysig static void Test9() cil managed
  {
    .maxstack  1
    
    newobj     instance void class B`1<class [types]ClassWithCtor[]>::.ctor()
    pop

    ret
  }

}

.class public auto ansi beforefieldinit ClassConstraintGenTypes 
         extends [mscorlib]System.Object
{
  // POSITIVE TEST
  // generic argument is a class with default ctor
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    
    newobj     instance void class B`1<class [types]GenClassWithCtor<int32>>::.ctor()
    pop

    ret
  }

  // POSITIVE TEST
  // generic argument is a class with no default ctor
  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
   
    newobj     instance void class B`1<class [types]GenClassNoCtor<int32>>::.ctor()
    pop
    
    ret
  }


  // POSITIVE TEST
  // generic argument is an interface with no default ctor
  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1

    newobj     instance void class B`1<class [types]GenInterface<int32>>::.ctor()
    pop

    ret
  }
}





