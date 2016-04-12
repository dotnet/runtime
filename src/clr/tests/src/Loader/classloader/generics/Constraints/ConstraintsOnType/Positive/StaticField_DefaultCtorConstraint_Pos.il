// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types{}

.assembly StaticField_DefaultCtorConstraint_Pos {}


// Generic class with new() constraint
.class public auto ansi beforefieldinit A`1<.ctor T>
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

.class public auto ansi beforefieldinit SF_DefaultCtorConstraint 
         extends [mscorlib]System.Object
{
  // POSITIVE TEST
  // generic argument is a class with default ctor
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
	
    ldsfld int32 class A`1<class [types]ClassWithCtor>::i
    pop

    ret
  }


  // POSITIVE TEST
  // generic argument is a struct (valuetypes have public nullary ctors by default)
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    ldsfld int32 class A`1<valuetype [types]NonGenStruct>::i
    pop

    ret
  }



  // POSITIVE TEST
  // generic argument is an mscorlib class with default ctor
  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1

    ldsfld int32 class A`1<class [mscorlib]System.Object>::i
    pop

    ret
  }


  // POSITIVE TEST
  // generic argument is an enum with no default ctor
  .method public hidebysig static void Test8() cil managed
  {
    .maxstack  1

    ldsfld int32 class A`1<valuetype [types]Enum1>::i
    pop

    ret
  }

}

.class public auto ansi beforefieldinit SF_DefaultCtorConstraintGenTypes
         extends [mscorlib]System.Object
{
  // POSITIVE TEST
  // generic argument is a class with default ctor
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1

    ldsfld int32 class A`1<class [types]GenClassWithCtor<int32>>::i
    pop

    ret
  }


  // POSITIVE TEST
  // generic argument is a struct
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    ldsfld int32 class A`1<valuetype [types]GenStruct<int32>>::i
    pop

    ret
  }


  // POSITIVE TEST
  // generic argument is NUllable<T>
  .method public hidebysig static void Test6() cil managed
  {
    .maxstack  1

    ldsfld int32 class A`1<valuetype [mscorlib]System.'Nullable`1'<int32>>::i
    pop
  
    ret
  }

}




