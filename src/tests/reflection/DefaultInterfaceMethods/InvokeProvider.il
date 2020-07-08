// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }

.assembly InvokeProvider { }

.class interface public abstract auto ansi IFoo
{
  .method public hidebysig newslot virtual instance int32 DefaultMethod(int32) cil managed
  {
    ldarg.1
    ldc.i4 50
    add
    ret
  }

  .method public hidebysig instance int32 InstanceMethod(int32) cil managed
  {
    ldarg.1
    ldc.i4 20
    add
    ret
  }

  .method public hidebysig static int32 StaticMethod(int32) cil managed
  {
    ldarg.0
    ldc.i4 30
    add
    ret
  }
}

.class interface public abstract auto ansi IReabstractor implements IFoo
{
  .method public hidebysig newslot virtual abstract final instance int32 DefaultMethod(int32) cil managed
  {
    .override IFoo::DefaultMethod
  }
}

.class interface public abstract auto ansi IFoo`1<T>
{
  .method public hidebysig newslot virtual instance valuetype [mscorlib]System.RuntimeTypeHandle DefaultMethod() cil managed
  {
    ldtoken !T
    ret
  }

  .method public hidebysig instance valuetype [mscorlib]System.RuntimeTypeHandle InstanceMethod() cil managed
  {
    ldtoken !T[]
    ret
  }

  .method public hidebysig static valuetype [mscorlib]System.RuntimeTypeHandle StaticMethod() cil managed
  {
    ldtoken !T[,]
    ret
  }
}

.class public auto ansi beforefieldinit Fooer
       extends [mscorlib]System.Object
       implements IFoo, class IFoo`1<class Fooer>
{
  .method public hidebysig specialname rtspecialname 
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit Reabstractor
       extends [mscorlib]System.Object
       implements IReabstractor
{
  .method public hidebysig specialname rtspecialname 
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public sequential ansi sealed beforefieldinit ValueFooer
       extends [mscorlib]System.ValueType
       implements IFoo, class IFoo`1<class Fooer>
{
}
