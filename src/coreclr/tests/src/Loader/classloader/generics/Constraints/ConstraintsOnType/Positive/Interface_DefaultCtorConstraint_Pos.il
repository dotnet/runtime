// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types_implementI{}

.assembly Interface_DefaultCtorConstraint_Pos {}


// Generic class with new() constraint
.class public auto ansi beforefieldinit A`1<.ctor ([types_implementI]I)  T>
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

.class public auto ansi beforefieldinit Interface_DefaultCtorConstraint 
         extends [mscorlib]System.Object
{
  // POSITIVE TEST
  // generic argument is a class with default ctor
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    
    newobj     instance void class A`1<class [types_implementI]ClassWithCtor>::.ctor()
    pop

    ret
  }



  // POSITIVE TEST
  // generic argument is a struct (valuetypes have public nullary ctors by default)
  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
    
    newobj     instance void class A`1<valuetype [types_implementI]NonGenStruct>::.ctor()
    pop

    ret
  }

  // POSITIVE TEST
  // generic argument is a class with default ctor
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    newobj     instance void class A`1<class [types_implementI]GenClassWithCtor<int32>>::.ctor()
    pop

    ret
  }


  // POSITIVE TEST
  // generic argument is a struct
  .method public hidebysig static void Test4() cil managed
  {
    .maxstack  1
    
    newobj     instance void class A`1<valuetype [types_implementI]GenStruct<int32>>::.ctor()
    pop

    ret
  }

}



