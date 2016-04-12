// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types{}

.assembly StaticMethod_ClassConstraint_Pos {}


// Generic class with class() constraint
.class public auto ansi beforefieldinit B`1<class T>
         extends [mscorlib]System.Object
{

    .method public static hidebysig void 
            method1() cil managed
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

.class public auto ansi beforefieldinit SM_ClassConstraint 
         extends [mscorlib]System.Object
{
  // POSITIVE TEST
  // generic argument is a class with default ctor
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    
    call void class B`1<class [types]ClassWithCtor>::method1()
    ret
  }

  // POSITIVE TEST
  // generic argument is a class with no default ctor
  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
   
    call void class B`1<class [types]ClassNoCtor>::method1() 
    ret
  }


  // POSITIVE TEST
  // generic argument is a delegate
  .method public hidebysig static void Test4() cil managed
  {
    .maxstack  1
    
    call void class B`1<class [types]Delegate1>::method1()
    ret
  }


  // POSITIVE TEST
  // generic argument is an mscorlib class with default ctor
  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1

    call void class B`1<class [mscorlib]System.Object>::method1()
    ret
  }



  // POSITIVE TEST
  // generic argument is an mscorlib abstract class with no default ctor

  .method public hidebysig static void Test6() cil managed
  {
    .maxstack  1

    
    call void class B`1<class [mscorlib]System.ValueType>::method1()
    ret
  }


  // POSITIVE TEST
  // generic argument is an interface 
  .method public hidebysig static void Test7() cil managed
  {
    .maxstack  1
    call void class B`1<class [types]NonGenInterface>::method1()
    ret
  }

 // POSITIVE TEST
  // generic argument is an array of classes with default ctor
  .method public hidebysig static void Test9() cil managed
  {
    .maxstack  1
    
    call void class B`1<class [types]ClassWithCtor[]>::method1()
    ret
  }



}

.class public auto ansi beforefieldinit SM_ClassConstraintGenTypes 
         extends [mscorlib]System.Object
{
  // POSITIVE TEST
  // generic argument is a class with default ctor
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    
    call void class B`1<class [types]GenClassWithCtor<int32>>::method1()
    ret
  }

  // POSITIVE TEST
  // generic argument is a class with no default ctor
  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
   
    call void class B`1<class [types]GenClassNoCtor<int32>>::method1()
    ret
  }


  // POSITIVE TEST
  // generic argument is an interface with no default ctor
  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1

    call void class B`1<class [types]GenInterface<int32>>::method1()
    ret
  }

}


