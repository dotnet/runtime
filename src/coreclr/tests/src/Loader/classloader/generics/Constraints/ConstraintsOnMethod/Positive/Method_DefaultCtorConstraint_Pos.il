// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types{}

.assembly Method_DefaultCtorConstraint_Pos {}


.class public auto ansi beforefieldinit A
         extends [mscorlib]System.Object
{

    // Generic method with new() constraint
    .method public hidebysig instance void 
            method1<.ctor T>() cil managed
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

.class public auto ansi beforefieldinit M_DefaultCtorConstraint 
         extends [mscorlib]System.Object
{
  // POSITIVE TEST
  // generic argument is a class with default ctor
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    newobj instance void class A::.ctor()
    call instance void class A::method1<class [types]ClassWithCtor>()
    ret
  }


  // POSITIVE TEST
  // generic argument is a struct (valuetypes have public nullary ctors by default)
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    newobj instance void class A::.ctor()
    call instance void class A::method1<valuetype [types]NonGenStruct>()
    ret
  }


  // POSITIVE TEST
  // generic argument is an mscorlib class with default ctor
  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1

    newobj instance void class A::.ctor()
    call instance void class A::method1<class [mscorlib]System.Object>()
    ret
  }



  // POSITIVE TEST
  // generic argument is an enum with no default ctor
  .method public hidebysig static void Test8() cil managed
  {
    .maxstack  1

    newobj instance void class A::.ctor()
    call instance void class A::method1<valuetype [types]Enum1>()
    ret
  }


}

.class public auto ansi beforefieldinit M_DefaultCtorConstraintGenTypes
         extends [mscorlib]System.Object
{
  // POSITIVE TEST
  // generic argument is a class with default ctor
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1

    newobj instance void class A::.ctor()
    call instance void class A::method1<class [types]GenClassWithCtor<int32>>()
    ret
  }



  // POSITIVE TEST
  // generic argument is a struct
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    newobj instance void class A::.ctor()
    call instance void class A::method1<valuetype [types]GenStruct<int32>>()

    ret
  }



  // POSITIVE TEST
  // generic argument is NUllable<T>
  .method public hidebysig static void Test6() cil managed
  {
    .maxstack  1

    newobj instance void class A::.ctor()
    call instance void class A::method1<valuetype [mscorlib]System.'Nullable`1'<int32>>()

    ret
  }

}




