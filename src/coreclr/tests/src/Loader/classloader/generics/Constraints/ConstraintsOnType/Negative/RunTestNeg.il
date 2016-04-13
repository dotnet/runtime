// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib {}
.assembly extern TypeParam_Constraints_Neg {}
.assembly RunTestNeg {}


.class public auto ansi beforefieldinit GClass`2<(!U) T,class U>
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

.class public auto ansi beforefieldinit GStruct`2<(!U) T, valuetype U>
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


.class public auto ansi beforefieldinit GNew`2<(!U) T,.ctor U>
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

.class public auto ansi beforefieldinit GClassType`2<(!U) T,([TypeParam_Constraints_Neg]A) U>
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

.class public auto ansi beforefieldinit GInterfaceType`2<(!U) T,([TypeParam_Constraints_Neg]I) U>
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

// ===============================================================================

.class public auto ansi beforefieldinit RunGClassTest
       extends [mscorlib]System.Object
{
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    newobj     instance void class GClass`2<class [TypeParam_Constraints_Neg]A,class [TypeParam_Constraints_Neg]B>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
    newobj     instance void class GClass`2<class [TypeParam_Constraints_Neg]I,class [TypeParam_Constraints_Neg]C>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    newobj     instance void class GClass`2<object,class [TypeParam_Constraints_Neg]A[]>::.ctor()
    pop
    ret
  }


  .method public hidebysig static void Test4() cil managed
  {
    .maxstack  1
    newobj     instance void class GClass`2<object,class [mscorlib]System.ValueType>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1
    newobj     instance void class GClass`2<int32,int32>::.ctor()
    pop
    ret
  }
}

// ===============================================================================

.class public auto ansi beforefieldinit RunGStructTest
       extends [mscorlib]System.Object
{
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    newobj     instance void class GStruct`2<class [TypeParam_Constraints_Neg]A,class [TypeParam_Constraints_Neg]B>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
    newobj     instance void class GStruct`2<class [TypeParam_Constraints_Neg]I,class [TypeParam_Constraints_Neg]C>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    newobj     instance void class GStruct`2<object,valuetype [TypeParam_Constraints_Neg]S>::.ctor()
    pop
    ret
  }


  .method public hidebysig static void Test4() cil managed
  {
    .maxstack  1
    newobj     instance void class GStruct`2<valuetype [TypeParam_Constraints_Neg]S,class [mscorlib]System.ValueType>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1
    newobj     instance void class GStruct`2<int32,object>::.ctor()
    pop
    ret
  }

}

// ===============================================================================


.class public auto ansi beforefieldinit RunGNewTest
       extends [mscorlib]System.Object
{
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    newobj     instance void class GNew`2<valuetype [TypeParam_Constraints_Neg]Enum1, class [mscorlib]System.ValueType>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
    newobj     instance void class GNew`2<class [TypeParam_Constraints_Neg]C, class [TypeParam_Constraints_Neg] I>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    newobj     instance void class GNew`2<class [TypeParam_Constraints_Neg]A[],class [TypeParam_Constraints_Neg]A[]>::.ctor()
    pop
    ret
  }


  .method public hidebysig static void Test4() cil managed
  {
    .maxstack  1
    newobj     instance void class GNew`2<object, class [TypeParam_Constraints_Neg]A>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1
    newobj     instance void class GNew`2<int32,class [mscorlib]System.ValueType>::.ctor()
    pop
    ret
  }

}

// ===============================================================================

.class public auto ansi beforefieldinit RunGClassTypeTest
       extends [mscorlib]System.Object
{
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    newobj     instance void class GClassType`2<class [mscorlib]System.String, class [mscorlib]System.Object>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
    newobj     instance void class GClassType`2<class [TypeParam_Constraints_Neg]A, class [mscorlib]System.Object>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    newobj     instance void class GClassType`2<class [TypeParam_Constraints_Neg]A[],class [TypeParam_Constraints_Neg]A[]>::.ctor()
    pop
    ret
  }


  .method public hidebysig static void Test4() cil managed
  {
    .maxstack  1
    newobj     instance void class GClassType`2<object, class [TypeParam_Constraints_Neg]A>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1
    newobj     instance void class GClassType`2<int32,int32>::.ctor()
    pop
    ret
  }

}

// ===============================================================================

.class public auto ansi beforefieldinit RunGInterfaceTypeTest
       extends [mscorlib]System.Object
{
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    newobj     instance void class GInterfaceType`2<class [mscorlib]System.String, class [mscorlib]System.Object>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
    newobj     instance void class GInterfaceType`2<class [TypeParam_Constraints_Neg]I, object>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test3() cil managed
  {
    .maxstack  1
    newobj     instance void class GInterfaceType`2<class [TypeParam_Constraints_Neg]I[],class [TypeParam_Constraints_Neg]I[]>::.ctor()
    pop
    ret
  }


  .method public hidebysig static void Test4() cil managed
  {
    .maxstack  1
    newobj     instance void class GInterfaceType`2<object, class [TypeParam_Constraints_Neg]I>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test5() cil managed
  {
    .maxstack  1
    newobj     instance void class GInterfaceType`2<int32,int32>::.ctor()
    pop
    ret
  }

}


