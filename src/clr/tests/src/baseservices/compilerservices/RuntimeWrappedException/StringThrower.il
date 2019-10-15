// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}

.assembly StringThrower {}

.class public auto ansi StringThrowerClass
       extends [mscorlib]System.Object
{
   .field public static int32 intStatic


  .method public hidebysig instance void 
          InstanceMethod() cil managed noinlining
  {
    .maxstack  8

    ldstr      "Inside StringThrower"
    throw
    ret
  } // end of method A::methodA


   .method public hidebysig specialname rtspecialname 
        instance void  .ctor() cil managed
   {
 
     .maxstack  8
     ldarg.0
     call       instance void class [mscorlib]System.Object::.ctor()
     ret
   } // end of method Test1::.ctor
	
}
