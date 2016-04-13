// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }

.assembly extern mscorlib{}
.assembly extern types_implementI_extendC{}

.assembly Interface_DefaultCtorAndClassTypeConstraint_Pos {}


// Generic class with new() constraint
.class public auto ansi beforefieldinit F`1<.ctor ([types_implementI_extendC]C, [types_implementI_extendC]I)  T>
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

.class public auto ansi beforefieldinit Interface_DefaultCtorAndClassTypeConstraint 
         extends [mscorlib]System.Object
{
  // POSITIVE TEST
  // generic argument is a class with default ctor that implements I and extends C
  .method public hidebysig static void Test1() cil managed
  {
    .maxstack  1
    
    newobj     instance void class F`1<class [types_implementI_extendC]ClassWithCtor>::.ctor()
    pop

    ret
  }




  // POSITIVE TEST
  // generic argument is a generic class with default ctor that implements I and extends C
  .method public hidebysig static void Test2() cil managed
  {
    .maxstack  1
    
    newobj     instance void class F`1<class [types_implementI_extendC]GenClassWithCtor<int32>>::.ctor()
    pop

    ret
  }

		




}




