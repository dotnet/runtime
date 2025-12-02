// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Makes sure that a type that implements IFoo<string> through a default
// interface method and declares it also implements IFoo<ValueType>
// (but it actually doesn't) will not be loadable.

.assembly extern System.Runtime { }
.assembly extern xunit.core {}

.assembly notimplemented { }

.class interface private abstract auto ansi IFoo`1<T>
{
  .method public hidebysig newslot abstract virtual instance void Frob() cil managed
  {
  }
}

.class interface private abstract auto ansi IBar
       implements class IFoo`1<class [System.Runtime]System.String>
{
  .method public hidebysig newslot virtual final instance void Frob() cil managed
  {
    .override class IFoo`1<class [System.Runtime]System.String>::Frob
    ret
  }
}

.class public auto ansi beforefieldinit Fooer
       extends [System.Runtime]System.Object
       implements IBar, class IFoo`1<class [System.Runtime]System.ValueType>
{
  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [System.Runtime]System.Object::.ctor()
    ret
  }
}

.method public hidebysig static void LoadFooer() cil managed noinlining
{
  newobj instance void Fooer::.ctor()
  pop
  ret
}

.class public auto ansi abstract sealed beforefieldinit notimplemented
    extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    .try
    {
      call void LoadFooer()
      leave DidNotThrow
    }
    catch [System.Runtime]System.TypeLoadException
    {
      pop
      leave Okay
    }

  Okay:
    ldc.i4 100
    ret

  DidNotThrow:
    ldc.i4.m1
    ret
  }
}
