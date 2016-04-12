// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
// this file contains positive tests for 
// G<T,U> where T : U where U : struct
// C# didn't want to compile such type definition so we are using IL.


.assembly extern mscorlib {}
.assembly extern TypeParam_Constraints_Pos {}
.assembly RunTestPos {}


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



.class public auto ansi beforefieldinit RunGStructTest
       extends [mscorlib]System.Object
{
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    newobj     instance void class GStruct`2<valuetype [TypeParam_Constraints_Pos]S,valuetype [TypeParam_Constraints_Pos]S>::.ctor()
    pop
    ret
  }

  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
    newobj     instance void class GStruct`2<int32,int32>::.ctor()
    pop
    ret
  }
}



