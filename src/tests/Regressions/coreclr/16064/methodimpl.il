// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly extern xunit.core {}

.assembly methodimpl { }

.class interface private abstract auto ansi IFoo
{
  .method public hidebysig newslot virtual abstract instance int32 Frob() cil managed
  {
  }

  .method public hidebysig newslot virtual abstract instance int32 Unrelated() cil managed
  {
  }
}

.class interface private abstract auto ansi IBar
       implements IFoo
{
  .method public hidebysig newslot virtual final instance int32 Frob() cil managed
  {
    .override IFoo::Frob
    .override IFoo::Unrelated
    ldc.i4 50
    ret
  }
}

.class public auto ansi beforefieldinit Fooer
       extends [System.Runtime]System.Object
       implements IBar
{
  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [System.Runtime]System.Object::.ctor()
    ret
  }
}

.class public auto ansi abstract sealed beforefieldinit methodimpl
    extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    ldstr "DefaultImplementationsOfInterfaces"
    call bool [System.Runtime]System.Runtime.CompilerServices.RuntimeFeature::IsSupported(string)

    // If default interfaces are not supported, consider the test successful.
    brtrue DoRunTest
    ldc.i4 100
    ret

  DoRunTest:
    newobj instance void Fooer::.ctor()
    callvirt instance int32 IFoo::Frob()
    newobj instance void Fooer::.ctor()
    callvirt instance int32 IFoo::Unrelated()
    add
    ret
  }
}