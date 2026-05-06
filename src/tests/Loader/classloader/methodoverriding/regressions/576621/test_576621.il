// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly test_576621 {}
.assembly extern mscorlib {}

.class public C1
extends [mscorlib]System.Object
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public   virtual instance int32 M1()
   {
      ldc.i4 1
      ret
   }
   .method public   virtual instance int32 M2()
   {
      ldc.i4 2
      ret
   }
   .method public   virtual instance int32 M3()
   {
      ldc.i4 3
      ret
   }
}

.class public C2
extends C1
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void C1::.ctor() 
      ret
   }
   .method public   virtual instance int32 M3()
   {
    .override C1::M2
      ldc.i4 4
      ret
   }
   .method public   virtual instance int32 M4()
   {
    .override C1::M1
      ldc.i4 5
      ret
   }
   .method public virtual instance int32 M1()
   {
    .override C1::M3
      ldc.i4 6
      ret
   }
}
