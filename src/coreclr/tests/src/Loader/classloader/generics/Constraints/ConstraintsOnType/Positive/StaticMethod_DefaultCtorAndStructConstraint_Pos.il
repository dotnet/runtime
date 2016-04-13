// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types{}

.assembly StaticMethod_DefaultCtorAndStructConstraint_Pos {}


// Generic class with struct() and new() constraints
.class public auto ansi beforefieldinit E`1<valuetype .ctor T>
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

.class public auto ansi beforefieldinit SM_DefaultCtorAndStructConstraint 
         extends [mscorlib]System.Object
{

  // POSITIVE TEST
  // generic argument is a struct
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    call void class E`1<valuetype [types]NonGenStruct>::method1()
    ret
  }


  // POSITIVE TEST
  // generic argument is an enum
  .method public hidebysig static void Test8() cil managed
  {
    .maxstack  1

    call void class E`1<valuetype [types]Enum1>::method1()
    ret
  }


  // POSITIVE TEST
  // generic argument is an mscorlib struct
  .method public hidebysig static void Test9() cil managed
  {
    .maxstack  1
    
    call void class E`1<valuetype [mscorlib]System.DateTime>::method1()
    ret
  }

}

.class public auto ansi beforefieldinit SM_DefaultCtorAndStructConstraintGenTypes 
         extends [mscorlib]System.Object
{

  // POSITIVE TEST
  // generic argument is a struct 
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    call void class E`1<valuetype [types]GenStruct<int32>>::method1()
    ret
  }

  // POSITIVE TEST
  // generic argument is an mscorlib generic struct
  .method public hidebysig static void Test6() cil managed
  {
    .maxstack  1

    call void class E`1<valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<valuetype [types]NonGenStruct,int32>>::method1()

    ret
  }

}




