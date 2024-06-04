// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly extern xunit.core {}

// TODO: use the contract once this is exposed from contracts
.assembly extern System.Runtime { }

.assembly ambiguousconstraint { }

.class interface private abstract auto ansi IFoo
{
  .method public hidebysig newslot abstract virtual instance void Frob() cil managed
  {
  }
}

.class interface private abstract auto ansi IBar
       implements IFoo
{
  .method public hidebysig newslot virtual final instance void Frob() cil managed
  {
    .override IFoo::Frob
    ret
  }
}

.class interface private abstract auto ansi IBaz
       implements IFoo
{
  .method public hidebysig newslot virtual final instance void Frob() cil managed
  {
    .override IFoo::Frob
    ret
  }
}

.class private auto ansi sealed beforefieldinit Fooer
       extends [System.Runtime]System.ValueType
       implements IBar, IBaz
{
}

.class public auto ansi beforefieldinit Gen`1<(IFoo) T>
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Check(!T x) cil managed
  {
    .try
    {
      ldarga.s 0
      constrained. !T
      callvirt instance void IFoo::Frob()
      leave.s Fail
    }
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
      pop
      leave.s Success
    }
  Fail:
    ldc.i4 -1
    ret
  Success:
    ldc.i4 100
    ret
  }
}

.class public auto ansi abstract sealed beforefieldinit ambiguousconstraint
    extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (valuetype Fooer)
    ldloc.0
    call int32 class Gen`1<valuetype Fooer>::Check(!0)
    ret
  }
}
