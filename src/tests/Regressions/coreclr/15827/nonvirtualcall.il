// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Verifies that we're able to call virtual interface methods non-virtually.
// Corresponds to the C# "base(IFoo).Frob()" syntax.
//

.assembly extern System.Runtime { }
.assembly extern xunit.core {}

.assembly nonvirtualcall { }

.class interface private abstract auto ansi IFoo
{
  .method public hidebysig newslot virtual instance int32 Frob() cil managed
  {
    ldc.i4 99
    ret
  }
}

.class interface private abstract auto ansi IBar
       implements IFoo
{
  .method public hidebysig newslot virtual final instance int32 Frob() cil managed
  {
    .override IFoo::Frob
    ldarg.0
    call instance int32 class IFoo::Frob()
    ldc.i4.1
    add
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

.method public hidebysig static int32 RunTest() cil managed
{
  newobj instance void Fooer::.ctor()
  callvirt instance int32 IFoo::Frob()
  ret
}

.class public auto ansi abstract sealed beforefieldinit nonvirtualcall
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
    call int32 RunTest()
    ret
  }
}