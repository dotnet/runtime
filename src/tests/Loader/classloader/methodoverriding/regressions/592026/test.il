// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
// running peverify on this assembly we were getting an Assert Failure
// The issue was that we were caling GetDescFromMemberDefOrRefThrowing()
// and this method depends on a fully-loaded type, including parent types, which is not
// always guaranteed (as was in this case).
// see comments in PlaceMembers (sd changelist 1442334)


.assembly test {}
.assembly extern mscorlib {}

.class public C1<T>
extends [mscorlib]System.Object
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public virtual instance int32 M0()
   {
      ldc.i4 0
      ret
   }
   .method public virtual instance int32 M1()
   {
      ldc.i4 1
      ret
   }
   .method public virtual instance int32 M2()
   {
      ldc.i4 2
      ret
   }
   .method public virtual instance int32 M3()
   {
      ldc.i4 3
      ret
   }
}
.class public C2<T>
extends class C1<!T>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C1<!T>::.ctor() 
      ret
   }
   .method public virtual instance int32 M1()
   {
    .override class C1<!T>::M2
      ldc.i4 4
      ret
   }
   .method public virtual instance int32 M5()
   {
      ldc.i4 5
      ret
   }
   .method public newslot  virtual instance int32 M2()
   {
      ldc.i4 6
      ret
   }
   .method public virtual instance int32 M6()
   {
    .override class C1<!T>::M1
      ldc.i4 7
      ret
   }
}
.class public C3<T>
extends class C2<!T>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C2<!T>::.ctor() 
      ret
   }
   .method public virtual instance int32 M0()
   {
    .override class C2<!T>::M5
      ldc.i4 8
      ret
   }
   .method public virtual instance int32 M5()
   {
      ldc.i4 9
      ret
   }
   .method public newslot  virtual instance int32 M6()
   {
    .override class C2<!T>::M2
      ldc.i4 10
      ret
   }
   .method public virtual instance int32 M2()
   {
    .override class C2<!T>::M1
      ldc.i4 11
      ret
   }
}
.class public C4
extends class C3<string>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C3<string>::.ctor() 
      ret
   }
   .method public virtual instance int32 M0()
   {
    .override class C3<string>::M5
      ldc.i4 12
      ret
   }
   .method public newslot  virtual instance int32 M5()
   {
    .override class C3<string>::M0
      ldc.i4 13
      ret
   }
   .method public virtual instance int32 M6()
   {
      ldc.i4 14
      ret
   }
   .method public newslot  virtual instance int32 M7()
   {
    .override class C3<string>::M6
      ldc.i4 15
      ret
   }
}
.class public C5
extends C4
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void C4::.ctor() 
      ret
   }
   .method public virtual instance int32 M0()
   {
      ldc.i4 16
      ret
   }
   .method public virtual instance int32 M7()
   {
    .override C4::M0
      ldc.i4 17
      ret
   }
   .method public virtual instance int32 M8()
   {
      ldc.i4 18
      ret
   }
   .method public virtual instance int32 M9()
   {
    .override C4::M5
      ldc.i4 19
      ret
   }
}
