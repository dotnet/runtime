// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly constrainedcall { }

.class interface private abstract auto ansi IAdder`1<T>
{
  .method public hidebysig newslot abstract virtual instance int32 Add(int32)
  {
  }

  .method public hidebysig newslot virtual instance int32 PlusPlus()
  {
    ldarg.0
    ldc.i4.1
    callvirt instance int32 class IAdder`1<!0>::Add(int32)
    ret
  }
}

.class interface IGen1`1<T> { }
.class interface IGen2`1<T> { }
.class interface IGen3`1<T> { }
.class value Atom { }

.class public value Adder`1<T> implements class IAdder`1<class IGen1`1<!T>>, class IAdder`1<class IGen2`1<!T>>, class IAdder`1<class IGen3`1<!T>>, class IAdder`1<valuetype Atom>
{
  .field private int32 _field

  .method public hidebysig newslot virtual instance int32 Add(int32) cil managed
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

  .method private hidebysig newslot virtual instance int32  'IAdder<IGen1<T>>.PlusPlus'()
  {
    .override  method instance int32 class IAdder`1<class IGen1`1<!T>>::PlusPlus()
    ldarg.0
    ldc.i4.2
    call instance int32 valuetype Adder`1<!T>::Add(int32)
    ret
  }

  .method private hidebysig newslot virtual instance int32  'IAdder<IGen2<T>>.PlusPlus'()
  {
    .override  method instance int32 class IAdder`1<class IGen2`1<!T>>::PlusPlus()
    ldarg.0
    ldc.i4.3
    call instance int32 valuetype Adder`1<!T>::Add(int32)
    ret
  }
}

.method public hidebysig static int32 Check<(class IAdder`1<!!U>) T,U>(!!T t)
{
  // The constrained calls below may or may not box for a valuetype T
  // depending on whether IAdder`1<!!U>::PlusPlus() ends up calling
  // the default implementation of the interface method.

  ldarga.s 0
  constrained. !!T
  callvirt instance int32 class IAdder`1<!!U>::PlusPlus()
  ldarga.s 0
  constrained. !!T
  callvirt instance int32 class IAdder`1<!!U>::PlusPlus()
  add
  ret
}

.class public auto ansi abstract sealed beforefieldinit constrainedcall
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
        int32,
        valuetype Adder`1<valuetype Atom>
      )

      // This will end up calling the implementation of IAdder<IGen1<object>>.PlusPlus
      // provided by the Adder valuetype.
      // The sum returned by the Check method will be 2+4 = 6.
      ldloc.0
      call int32 Check<valuetype Adder`1<object>,class IGen1`1<object>>(!!0)
      ldc.i4.6
      ceq
      brtrue IGen1_OK

      ldc.i4.1
      ret

  IGen1_OK:

      // This will end up calling the implementation of IAdder<IGen2<object>>.PlusPlus
      // provided by the Adder valuetype.
      // The sum returned by the Check method will be 3+6 = 9.
      ldloca 0
      initobj valuetype Adder`1<object>
      ldloc.0
      call int32 Check<valuetype Adder`1<object>,class IGen2`1<object>>(!!0)
      ldc.i4 9
      ceq
      brtrue IGen2_OK

      ldc.i4.2
      ret

  IGen2_OK:

      ldstr "Constrained calls that require runtime lookup are OK"
      call void [mscorlib]System.Console::WriteLine(string)


      // Store a successful result in case the implementation throws.
      // We consider both a throw and a successful dispatch a success.
      // The throwing behavior is an implementation limitation.
      // Successful dispatch is what the spec mandates.
      ldc.i4.2
      stloc.1

      ldloca 0
      initobj valuetype Adder`1<object>

      // This will end up calling the default implementation of IAdder<IGen3<object>>.PlusPlus
      // Since each constrained call in Check is going to box, the sum will end up 1+1 = 2.
      .try
      {
        ldloc.0
        call int32 Check<valuetype Adder`1<object>,class IGen3`1<object>>(!!0)
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
      ldc.i4.2
      ceq
      brtrue IGen3_OK

      ldc.i4.3
      ret

  IGen3_OK:

      ldloca 0
      initobj valuetype Adder`1<valuetype Atom>

      ldloc.0
      call int32 Check<valuetype Adder`1<valuetype Atom>,valuetype Atom>(!!0)
      ldc.i4 2
      ceq
      brtrue AllOK

      ldc.i4.4
      ret

  AllOK:
      ldstr "Compile time constraint resolution to default interface method OK"
      call void [mscorlib]System.Console::WriteLine(string)

      ldc.i4 100
      ret
  }
}
