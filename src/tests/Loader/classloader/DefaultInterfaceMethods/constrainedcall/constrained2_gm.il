// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly constrained2_gm { }

.class interface private abstract auto ansi IAdder`1<T>
{
  .method public hidebysig newslot abstract virtual instance int32 Add<U>(int32)
  {
  }

  .method public hidebysig newslot virtual instance int32 PlusPlus<U>()
  {
    ldstr "IAdder`1<T>:PlusPlus"
    call void [mscorlib]System.Console::WriteLine(string)

    ldarg.0
    ldc.i4.1
    callvirt instance int32 class IAdder`1<!0>::Add<!!0>(int32)
    ret
  }
}

.class public value Adder`1<T> implements class IAdder`1<!T>, class IAdder`1<class [mscorlib]System.String>
{
  .field private int32 _field

  .method public hidebysig newslot virtual instance int32 Add<U>(int32) cil managed
  {
    ldarg.0
    dup
    ldfld int32 valuetype Adder`1<!T>::_field
    ldarg.1
    add
    stfld int32 valuetype Adder`1<!T>::_field
    ldarg.0
    ldfld int32 valuetype Adder`1<!T>::_field
    ret
  }

  .method private hidebysig newslot virtual instance int32  'IAdder<System.String>.PlusPlus'<U>()
  {
    .override class IAdder`1<class [mscorlib]System.String>::PlusPlus

    ldstr "Adder`1<T>:'IAdder<System.String>.PlusPlus'"
    call void [mscorlib]System.Console::WriteLine(string)

    ldarg.0
    ldc.i4.2
    call instance int32 valuetype Adder`1<!T>::Add<!!U>(int32)
    ret
  }
}

.method public hidebysig static int32 Check<(class IAdder`1<!!U>, class IAdder`1<string>) T,U>(!!T t)
{
  // The constrained calls below may or may not box for a valuetype T
  // depending on whether IAdder`1<!!U>::PlusPlus() ends up calling
  // the default implementation of the interface method.

  ldarga.s 0
  constrained. !!T
  callvirt instance int32 class IAdder`1<!!U>::PlusPlus<object>()
  ldarga.s 0
  constrained. !!T
  callvirt instance int32 class IAdder`1<!!U>::PlusPlus<object>()
  add

  ldarga.s 0
  constrained. !!T
  callvirt instance int32 class IAdder`1<string>::PlusPlus<object>()
  ldarga.s 0
  constrained. !!T
  callvirt instance int32 class IAdder`1<string>::PlusPlus<object>()
  add

  add

  ret
}

.class public auto ansi abstract sealed beforefieldinit constrained2_gm
    extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main()
  {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint

      .locals init (
        valuetype Adder`1<object>,
        int32
      )

      // This will end up calling the implementation of IAdder<string>.PlusPlus
      // provided by the Adder valuetype.
      // The sum returned by the Check method will be 2+4+6+8 = 20.
      ldloc.0
      call int32 Check<valuetype Adder`1<object>,string>(!!0)
      ldc.i4 20
      ceq
      brtrue String_OK

      ldc.i4 1
      ret

  String_OK:

      // This will end up calling the implementation of IAdder<object>.PlusPlus
      // provided by the default interface method.
      // The sum returned by the Check method will be 1+1+2+4 = 8.

      // This requires a runtime that can generate boxing stubs. We will support
      // both the case when this is not supported, and when it's implemented.
      ldc.i4.8
      stloc.1

      .try
      {
        ldloca 0
        initobj valuetype Adder`1<object>
        ldloc.0
        call int32 Check<valuetype Adder`1<object>,object>(!!0)
        stloc.1

        ldstr "Runtime supports lookups with runtime determined boxing"
        call void [mscorlib]System.Console::WriteLine(string)

        leave AfterBoxingCall
      }
      catch [mscorlib]System.Exception
      {
        pop
        leave AfterFailedBoxingCall
      }

  AfterFailedBoxingCall:
      ldstr "Runtime does not support lookups with runtime determined boxing"
      call void [mscorlib]System.Console::WriteLine(string)

  AfterBoxingCall:
      ldloc.1
      ldc.i4 8
      ceq
      brtrue Object_OK

      ldc.i4.2
      ret

  Object_OK:

      ldc.i4 100
      ret
  }
}
