// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types_implementI{}

.assembly Interface_DefaultCtorAndClassConstraint_Neg {}


// Generic class with new() constraint
.class public auto ansi beforefieldinit D`1<class .ctor ([types_implementI]I)  T>
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

.class public auto ansi beforefieldinit Interface_DefaultCtorAndClassConstraint 
         extends [mscorlib]System.Object
{

  // NEGATIVE TEST
  // generic argument is a class with no default ctor
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
   
    newobj     instance void class D`1<class [types_implementI]ClassNoCtor>::.ctor()
    pop
    

    ret
  }

  // NEGATIVE TEST
  // generic argument is an mscorlib class with default ctor
  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1

    newobj     instance void class D`1<class [mscorlib]System.Object>::.ctor()
    pop

    ret
  }

  // NEGATIVE TEST
  // generic argument is a delegate (doesn't have public parameterless constructor)
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    newobj     instance void class D`1<class [types_implementI]Delegate1>::.ctor()
    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is an enum (has default ctor since it's a valuetype)
  .method public hidebysig static void Test4() cil managed
  {
    .maxstack  1

    newobj     instance void class D`1<valuetype [types_implementI]Enum1>::.ctor()
    pop

    ret
  }

  // NEGATIVE TEST
  // generic argument is an mscorlib abstract class with no default ctor

  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1
    
    newobj     instance void class D`1<class [mscorlib]System.ValueType>::.ctor()
    pop
    ret
  }


  // NEGATIVE TEST
  // generic argument is an interface with no default ctor
  .method public hidebysig static void Test6() cil managed
  {
    .maxstack  1

    newobj     instance void class D`1<class [types_implementI]NonGenInterface>::.ctor()
    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is an array of classes with default ctor
  .method public hidebysig static void Test7() cil managed
  {
    .maxstack  1
    
    newobj     instance void class D`1<class [types_implementI]ClassWithCtor[]>::.ctor()
    pop

    ret
  }

 // NEGATIVE TEST
  // generic argument is a class with no default ctor
  .method public hidebysig static void Test8() cil managed
  {
    .maxstack  1
   
    newobj     instance void class D`1<class [types_implementI]GenClassNoCtor<int32>>::.ctor()
    pop
    
    ret
  }


  // NEGATIVE TEST
  // generic argument is an interface
  .method public hidebysig static void Test9() cil managed
  {
    .maxstack  1

    newobj     instance void class D`1<class [types_implementI]GenInterface<int32>>::.ctor()
    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is NUllable<T>
  .method public hidebysig static void Test10() cil managed
  {
    .maxstack  1

    newobj     instance void class D`1<valuetype [mscorlib]System.'Nullable`1'<int32>>::.ctor()
    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is a struct (valuetypes have public nullary ctors by default)
  .method public hidebysig static void Test11() cil managed
  {
    .maxstack  1
    
    newobj     instance void class D`1<valuetype [types_implementI]NonGenStruct>::.ctor()
    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is a struct
  .method public hidebysig static void Test12() cil managed
  {
    .maxstack  1
    
    newobj     instance void class D`1<valuetype [types_implementI]GenStruct<int32>>::.ctor()
    pop

    ret
  }

}




