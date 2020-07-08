// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
.assembly check2 {}
.assembly extern mscorlib {}

.assembly extern Types {}


 .class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
  {
     .method public hidebysig specialname rtspecialname
         instance void  .ctor() cil managed
     {
         ldarg.0
         call       instance void [mscorlib]System.Object::.ctor()
         ret
     }
  }

 .class public auto ansi beforefieldinit D
    extends [mscorlib]System.Object
  {
     .method public hidebysig specialname rtspecialname
         instance void  .ctor() cil managed
     {
         ldarg.0
         call       instance void [mscorlib]System.Object::.ctor()
         ret
     }
  }


.class interface public abstract I0<M0>
{
}


.class interface public abstract I4<N0>
implements class I0<!N0>
{
}


.class interface public abstract I6<T0,T1>
implements class I0<!T0>, 
           class I4<!T1>
{
}


.class public C7
implements class I6<class D,class C>
{

 .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void [mscorlib]System.Object::.ctor() 
      ret
   }
   
}
