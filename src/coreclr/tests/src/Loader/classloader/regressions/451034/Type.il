// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }


.assembly Type{}
.assembly extern mscorlib {}

.data i1data = int32(43962)

.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class S


// Generics + RVA (Field ValueType)
.class public CReloc8<T>
{	
	.field public static valuetype S s at i1data
        
}


.class public auto ansi beforefieldinit CReloc5<T>
       extends [mscorlib]System.Object
{
  .field public static valuetype S s
  
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyData::.ctor

} // end of class MyData

