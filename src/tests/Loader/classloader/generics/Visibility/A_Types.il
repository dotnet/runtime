// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }


.assembly extern mscorlib {}
.assembly A_Types {}



.class public auto ansi beforefieldinit C2Public
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
  } // end of method C2Public::.ctor

} // end of class C2Public

.class public sequential ansi sealed beforefieldinit S2Public
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class S2Public

.class private auto ansi beforefieldinit C2Private
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
  } // end of method C2Private::.ctor

} // end of class C2Private

.class private sequential ansi sealed beforefieldinit S2Private
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class S2Private
