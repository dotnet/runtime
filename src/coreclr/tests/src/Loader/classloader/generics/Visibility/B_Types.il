// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }


.assembly extern mscorlib {}
.assembly B_Types {}

.class public auto ansi beforefieldinit CPublic`1<T>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CPublic`1::.ctor

} // end of class CPublic`1

.class public sequential ansi sealed beforefieldinit SPublic`1<T>
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class SPublic`1

.class private auto ansi beforefieldinit CPrivate`1<T>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CPrivate`1::.ctor

} // end of class CPrivate`1

.class private sequential ansi sealed beforefieldinit SPrivate`1<T>
       extends [mscorlib]System.ValueType
{

.method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8

    IL_0006:  ret
  } // end of method CPrivate`1::.ctor

  .pack 0
  .size 1
} // end of class SPrivate`1
