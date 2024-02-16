// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly constrained3_gm { }

.class interface private abstract auto ansi IFrobber`1<T>
{
  .method public hidebysig newslot virtual instance int32 Frob<U>()
  {
    ldstr "IFrobber<T>:Frob"
    call void [mscorlib]System.Console::WriteLine(string)

    ldc.i4 34
    ret
  }
}

.class interface private abstract auto ansi IRobber`1<T> implements class IFrobber`1<!T>
{
  .method public hidebysig final newslot virtual instance int32 Frob<U>()
  {
    .override class IFrobber`1<!T>::Frob
    ldstr "IRobber<T>:Frob"
    call void [mscorlib]System.Console::WriteLine(string)
    ldc.i4 66
    ret
  }
}

.class interface private abstract auto ansi IGrabber`1<T> implements class IFrobber`1<!T>
{
  .method public hidebysig final newslot virtual instance int32 Frob<U>()
  {
    .override class IFrobber`1<!T>::Frob
    ldstr "IGrabber<T>:Frob"
    call void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.3
    ret
  }
}

.class public value Adder`1<T, U> implements class IFrobber`1<!T>, class IRobber`1<!U>, class IGrabber`1<!U[]>
{
}

.class public auto ansi abstract sealed beforefieldinit constrained3_gm
    extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main()
  {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint

      .locals init (
        valuetype Adder`1<object, string>
      )

      ldloca.s 0
      constrained. valuetype Adder`1<object, string>
      callvirt instance int32 class IFrobber`1<object>::Frob<object>()

      ldloca.s 0
      constrained. valuetype Adder`1<object, string>
      callvirt instance int32 class IFrobber`1<string>::Frob<object>()
      add

      ret
  }
}
