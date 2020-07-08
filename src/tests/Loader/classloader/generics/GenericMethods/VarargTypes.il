// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
// test that we throw an error for vararg methods inside generic type
// and for generic vararg methods inside non-generic type

.assembly extern mscorlib {}
.assembly VarargTypes {}


// generic type X<T> with non generic vararg method
.class public auto ansi beforefieldinit X`1<T>
       extends [mscorlib]System.Object
{
  .method public hidebysig instance vararg void 
          method1(int32 i) cil managed
  {
    .maxstack  8
    ret
  } // end of method X`1::method1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method X`1::.ctor

} // end of class X`1


// non generic type Y with generic vararg method

.class public auto ansi beforefieldinit Y
       extends [mscorlib]System.Object
{
  .method public hidebysig instance vararg void 
          method1<T>(int32 i) cil managed
  {
    .maxstack  8
    ret
  } // end of method X`1::method1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method X`1::.ctor

} // end of class X`1

