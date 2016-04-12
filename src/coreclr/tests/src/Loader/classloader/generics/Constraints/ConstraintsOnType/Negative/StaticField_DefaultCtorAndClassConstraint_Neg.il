// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types{}

.assembly StaticField_DefaultCtorAndClassConstraint_Neg {}


// Generic class with class() and new() constraints
.class public auto ansi beforefieldinit D`1<class .ctor T>
         extends [mscorlib]System.Object
{

    .field public static int32 i


    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      .maxstack  8
      ldarg.0
     call       instance void [mscorlib]System.Object::.ctor()
      ret
    } 



} 

.class public auto ansi beforefieldinit SF_DefaultCtorAndClassConstraint 
         extends [mscorlib]System.Object
{

  // NEGATIVE TEST
  // generic argument is a class with no default ctor
  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
   
    ldsfld int32 class D`1<class [types]ClassNoCtor>::i
    pop
    
    ret
  }


  // NEGATIVE TEST
  // generic argument is a struct 
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    ldsfld int32 class D`1<valuetype [types]NonGenStruct>::i
    pop

    ret
  }

  // NEGATIVE TEST
  // generic argument is a delegate (doesn't have public parameterless constructor)
  .method public hidebysig static void Test4() cil managed
  {
    .maxstack  1
    
    ldsfld int32 class D`1<class [types]Delegate1>::i
    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is an mscorlib abstract class with no default ctor

  .method public hidebysig static void Test6() cil managed
  {
    .maxstack  1

    
    ldsfld int32 class D`1<class [mscorlib]System.ValueType>::i
    pop
    ret
  }


  // NEGATIVE TEST
  // generic argument is an interface 
  .method public hidebysig static void Test7() cil managed
  {
    .maxstack  1

    ldsfld int32 class D`1<class [types]NonGenInterface>::i
    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is an enum
  .method public hidebysig static void Test8() cil managed
  {
    .maxstack  1

    ldsfld int32 class D`1<valuetype [types]Enum1>::i
    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is an mscorlib struct
  .method public hidebysig static void Test9() cil managed
  {
    .maxstack  1
    
    ldsfld int32 class D`1<valuetype [mscorlib]System.DateTime>::i
    pop

    ret
  }

  // NEGATIVE TEST
  // generic argument is an array of classes with default ctor
  .method public hidebysig static void Test10() cil managed
  {
    .maxstack  1
    
    ldsfld int32 class D`1<class [types]ClassWithCtor[]>::i
    pop

    ret
  }



}

.class public auto ansi beforefieldinit SF_DefaultCtorAndClassConstraintGenTypes 
         extends [mscorlib]System.Object
{

  // NEGATIVE TEST
  // generic argument is a class with no default ctor
  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
   
    ldsfld int32 class D`1<class [types]GenClassNoCtor<int32>>::i
    pop
    
    ret
  }


  // NEGATIVE TEST
  // generic argument is a generic struct 
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    ldsfld int32 class D`1<valuetype [types]GenStruct<int32>>::i
    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is an generic interface
  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1

    ldsfld int32 class D`1<class [types]GenInterface<int32>>::i
    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is an mscorlib generic struct
  .method public hidebysig static void Test6() cil managed
  {
    .maxstack  1
    

    ldsfld int32 class D`1<valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<valuetype [types]NonGenStruct,int32>>::i

    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is NUllable<T>
  .method public hidebysig static void Test7() cil managed
  {
    .maxstack  1

    ldsfld int32 class D`1<valuetype [mscorlib]System.'Nullable`1'<int32>>::i
    pop

    ret
  }

}




