// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib{}
.assembly extern types{}

.assembly Method_DefaultCtorAndClassConstraint_Neg {}



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
  // NEGATIVE TEST
  // generic argument is a class with no default ctor
  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
   
    newobj instance void class D::.ctor()
    call instance void class D::method1<class [types]ClassNoCtor>()
    ret
  }


  // NEGATIVE TEST
  // generic argument is a struct 
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    newobj instance void class D::.ctor()
    call instance void class D::method1<valuetype [types]NonGenStruct>()
    ret
  }

  // NEGATIVE TEST
  // generic argument is a delegate (doesn't have public parameterless constructor)
  .method public hidebysig static void Test4() cil managed
  {
    .maxstack  1
    
    newobj instance void class D::.ctor()
    call instance void class D::method1<class [types]Delegate1>()

    ret
  }



  // NEGATIVE TEST
  // generic argument is an mscorlib abstract class with no default ctor

  .method public hidebysig static void Test6() cil managed
  {
    .maxstack  1
        
    newobj instance void class D::.ctor()
    call instance void class D::method1<class [mscorlib]System.ValueType>()

    ret
  }


  // NEGATIVE TEST
  // generic argument is an interface 
  .method public hidebysig static void Test7() cil managed
  {
    .maxstack  1

    newobj instance void class D::.ctor()
    call instance void class D::method1<class [types]NonGenInterface>()

    ret
  }


  // NEGATIVE TEST
  // generic argument is an enum
  .method public hidebysig static void Test8() cil managed
  {
    .maxstack  1

    newobj instance void class D::.ctor()
    call instance void class D::method1<valuetype [types]Enum1>()

    ret
  }


  // NEGATIVE TEST
  // generic argument is an mscorlib struct
  .method public hidebysig static void Test9() cil managed
  {
    .maxstack  1
   
    newobj instance void class D::.ctor()
    call instance void class D::method1<valuetype [mscorlib]System.DateTime>()

    ret
  }

  // NEGATIVE TEST
  // generic argument is an array of classes with default ctor
  .method public hidebysig static void Test10() cil managed
  {
    .maxstack  1
    
    newobj instance void class D::.ctor()
    call instance void class D::method1<class [types]ClassWithCtor[]>()

    ret
  }

}

.class public auto ansi beforefieldinit M_DefaultCtorAndClassConstraintGenTypes 
         extends [mscorlib]System.Object
{

  // NEGATIVE TEST
  // generic argument is a class with no default ctor
  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
   
    newobj instance void class D::.ctor()
    call instance void class D::method1<class [types]GenClassNoCtor<int32>>()
    ret
  }


  // NEGATIVE TEST
  // generic argument is a generic struct 
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    newobj instance void class D::.ctor()
    call instance void class D::method1<valuetype [types]GenStruct<int32>>()

    ret
  }


  // NEGATIVE TEST
  // generic argument is an generic interface
  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1

    newobj instance void class D::.ctor()
    call instance void class D::method1<class [types]GenInterface<int32>>()

    ret
  }



  // NEGATIVE TEST
  // generic argument is an mscorlib generic struct
  .method public hidebysig static void Test6() cil managed
  {
    .maxstack  1
    
    newobj instance void class D::.ctor()
    call instance void class D::method1<valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<valuetype [types]NonGenStruct,int32>>()


    ret
  }

  // NEGATIVE TEST
  // generic argument is NUllable<T>
  .method public hidebysig static void Test7() cil managed
  {
    .maxstack  1

    newobj instance void class D::.ctor()
    call instance void class D::method1<valuetype [mscorlib]System.'Nullable`1'<int32>>()

    ret
  }

}
