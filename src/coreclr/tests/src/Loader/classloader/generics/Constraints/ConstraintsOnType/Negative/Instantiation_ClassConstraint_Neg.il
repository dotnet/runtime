// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types{}

.assembly Instantiation_ClassConstraint_Neg {}


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

  // NEGATIVE TEST
  // generic argument is a struct
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    newobj     instance void class B`1<valuetype [types]NonGenStruct>::.ctor()
    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is an enum with no default ctor
  .method public hidebysig static void Test8() cil managed
  {
    .maxstack  1

    newobj     instance void class B`1<valuetype [types]Enum1>::.ctor()
    pop

    ret
  }
}

.class public auto ansi beforefieldinit ClassConstraintGenTypes 
         extends [mscorlib]System.Object
{
  // NEGATIVE TEST
  // generic argument is a struct 
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    newobj     instance void class B`1<valuetype [types]GenStruct<int32>>::.ctor()
    pop

    ret
  }


  // NEGATIVE TEST
  // generic argument is NUllable<T>
  .method public hidebysig static void Test6() cil managed
  {
    .maxstack  1

    newobj     instance void class B`1<valuetype [mscorlib]System.'Nullable`1'<int32>>::.ctor()
    pop

    ret
  }
}



