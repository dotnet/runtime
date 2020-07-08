// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }


.assembly extern mscorlib {}
.assembly Types {}




.class interface public abstract auto ansi I`1<T> {}


// this type should be illegal to instantiate
.class public auto ansi beforefieldinit C`1<T>
       extends [mscorlib]System.Object
       implements class I`1<!T>,
                  class I`1<int32>
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

// this type should be illegal to instantiate
.class public sequential ansi sealed S`1<T>
       extends [mscorlib]System.ValueType
       implements class I`1<!T>,
                  class I`1<int32>
{
  .pack 0
  .size 1
}





.class public Test
{
  // NEGATIVE TEST
  .method public hidebysig static void  Test1() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  1
    .locals init (class C`1<int32> V_0)

    newobj     instance void class C`1<int32>::.ctor()
    stloc.0
    ret
  } // end of method Test::Test1

  // NEGATIVE TEST
  .method public hidebysig static void  Test2() cil managed
  {
    .maxstack  1
    .locals init (valuetype S`1<int32> V_0)

    ldloca.s V_0
    initobj valuetype S`1<int32>

    stloc.0
    ret
  } // end of method Test::Test1

  
}
