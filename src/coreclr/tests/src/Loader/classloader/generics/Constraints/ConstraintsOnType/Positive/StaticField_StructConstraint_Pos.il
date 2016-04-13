// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types{}

.assembly StaticField_StructConstraint_Pos {}


// Generic class with class() constraint
.class public auto ansi beforefieldinit C`1<valuetype T>
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

.class public auto ansi beforefieldinit SF_StructConstraint 
         extends [mscorlib]System.Object
{

  // POSITIVE TEST
  // generic argument is a struct 
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    ldsfld int32 class C`1<valuetype [types]NonGenStruct>::i
    pop

    ret
  }

 
  // POSITIVE TEST
  // generic argument is an enum 
  .method public hidebysig static void Test8() cil managed
  {
    .maxstack  1

    ldsfld int32 class C`1<valuetype [types]Enum1>::i
    pop

    ret
  }


  // POSITIVE TEST
  // generic argument is an mscorlib struct
  .method public hidebysig static void Test9() cil managed
  {
    .maxstack  1
    
    ldsfld int32 class C`1<valuetype [mscorlib]System.DateTime>::i
    pop

    ret
  }

}

.class public auto ansi beforefieldinit SF_StructConstraintGenTypes 
         extends [mscorlib]System.Object
{
 
  // POSITIVE TEST
  // generic argument is a struct
  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    
    ldsfld int32 class C`1<valuetype [types]GenStruct<int32>>::i
    pop

    ret
  }


  // POSITIVE TEST
  // generic argument is an mscorlib generic struct
  .method public hidebysig static void Test6() cil managed
  {
    .maxstack  1
    
    ldsfld int32 class C`1<valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<valuetype [types]NonGenStruct,int32>>::i

    pop

    ret
  }

}




