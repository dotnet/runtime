// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Runtime { }

// TODO: use the contract once this is exposed from contracts
.assembly extern System.Private.CoreLib { }

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

.class private auto ansi beforefieldinit Gen`1<(IFoo) T>
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
    catch [System.Private.CoreLib]System.Runtime.AmbiguousImplementationException
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

.method public hidebysig static int32 Main() cil managed
{
  .entrypoint
  .locals init (valuetype Fooer)
  ldloc.0
  call int32 class Gen`1<valuetype Fooer>::Check(!0)
  ret
}