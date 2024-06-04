// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly test_dd52 {}
.assembly extern mscorlib {}



.class public C27<T0,T1,T2>
extends [mscorlib]System.Object
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void [mscorlib]System.Object::.ctor() 
      ret
   }

   .method public newslot  virtual instance int32 M3()
   {
      ldc.i4 53
      ret
   }
}

.class public C28<T0>
extends class C27<!T0,!T0,!T0>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C27<!T0,!T0,!T0>::.ctor() 
      ret
   }
   .method public newslot  virtual instance int32 M28()
   {
    .override class C27<!T0,!T0,!T0>::M3
      ldc.i4 54
      ret
   }
   .method public virtual instance int32 M3()
   {
      ldc.i4 55
      ret
   }
}

.class public C29
extends class C28<int32>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C28<int32>::.ctor() 
      ret
   }
   .method public virtual instance int32 M28()
   {
    .override class C28<int32>::M3
      ldc.i4 56
      ret
   }
  
}

