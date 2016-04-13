// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types{}

.assembly StaticField_DefaultCtorAndClassConstraint_Pos {}


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
  // POSITIVE TEST
  // generic argument is a class with default ctor
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    
    ldsfld int32 class D`1<class [types]ClassWithCtor>::i
    pop

    ret
  }


  // POSITIVE TEST
  // generic argument is an mscorlib class with default ctor
  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1

    ldsfld int32 class D`1<class [mscorlib]System.Object>::i
    pop

    ret
  }


}

.class public auto ansi beforefieldinit SF_DefaultCtorAndClassConstraintGenTypes 
         extends [mscorlib]System.Object
{
  // POSITIVE TEST
  // generic argument is a class with default ctor
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    
    ldsfld int32 class D`1<class [types]GenClassWithCtor<int32>>::i
    pop

    ret
  }
}




