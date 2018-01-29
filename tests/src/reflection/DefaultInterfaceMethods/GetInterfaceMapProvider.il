// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }

.assembly GetInterfaceMapProvider { }

.class interface public abstract auto ansi IFoo
{
  .method public hidebysig newslot virtual instance int32 DefaultMethod(int32) cil managed
  {
    ldarg.1
    ldc.i4 50
    add
    ret
  }

  .method public hidebysig newslot virtual abstract instance int32 OtherMethod(int32) cil managed
  {
  }

  .method public hidebysig instance int32 InstanceMethod(int32) cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig static int32 StaticMethod(int32) cil managed
  {
    ldnull
    throw
  }
}

.class interface public abstract auto ansi IBar implements IFoo
{
  .method public hidebysig newslot virtual final instance int32 OtherMethod(int32) cil managed
  {
    .override IFoo::OtherMethod
    ldarg.1
    ret
  }
}

.class interface public abstract auto ansi IFoo`1<T>
{
  .method public hidebysig newslot virtual instance valuetype [mscorlib]System.RuntimeTypeHandle DefaultMethod() cil managed
  {
    ldtoken !T
    ret
  }

  .method public hidebysig newslot virtual abstract instance int32 OtherMethod(int32) cil managed
  {
  }
}

.class interface public abstract auto ansi IBar`1<T> implements class IFoo`1<!T>
{
  .method public hidebysig newslot virtual final instance int32 OtherMethod(int32) cil managed
  {
    .override class IFoo`1<!T>::OtherMethod
    ldarg.1
    ret
  }

}

.class public auto ansi beforefieldinit Fooer
       extends [mscorlib]System.Object
       implements IBar, class IBar`1<class Fooer>
{
  .method public hidebysig specialname rtspecialname 
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
