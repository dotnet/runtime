// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
// this file contains positive test for 
// GStruct<T> where T : struct
// G4<P> : GStruct<P> where P : valuetype S

// C# didn't want to compile such type definition so we are using IL.


.assembly extern mscorlib {}
.assembly extern TypeParam2_Constraints_Pos {}
.assembly RunTestPos2 {}

.class public auto ansi beforefieldinit GStruct`1<valuetype T>
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

.class public auto ansi beforefieldinit G4`1<(valuetype [TypeParam2_Constraints_Pos]S) P>
       extends class GStruct`1<!P>
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

 

.class public sequential ansi sealed beforefieldinit S_PrivCtor
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} 







