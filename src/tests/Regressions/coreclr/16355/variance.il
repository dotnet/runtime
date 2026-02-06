// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary { .ver 0:0:0:0 }

.assembly variance { }

.class interface private abstract auto ansi IContravariant`1<- T>
{
  .method public hidebysig newslot abstract virtual instance valuetype [System.Runtime]System.RuntimeTypeHandle Frob(!T t) cil managed
  {
  }
}

.class interface private abstract auto ansi IBar`1<- T>
       implements class IContravariant`1<!T>
{
  .method public hidebysig newslot virtual final instance valuetype [System.Runtime]System.RuntimeTypeHandle Frob(!T t) cil managed
  {
    .override class IContravariant`1<!T>::Frob
    ldtoken !T
    ret
  }
}


.class public auto ansi beforefieldinit Fooer
       extends [System.Runtime]System.Object
       implements class IBar`1<class [System.Runtime]System.Object>
{
  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [System.Runtime]System.Object::.ctor()
    ret
  }
}

.class public auto ansi abstract sealed beforefieldinit variance
    extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, valuetype [Microsoft.DotNet.XUnitExtensions]Xunit.TestRuntimes) = {
        string('ldtoken in default interface methods does not appear to work in minijit or interp')
        int32(0x2) // Mono
    }
    .entrypoint
    .locals (valuetype [System.Runtime]System.RuntimeTypeHandle)
    newobj instance void class Fooer::.ctor()
    ldnull
    callvirt instance valuetype [System.Runtime]System.RuntimeTypeHandle class IContravariant`1<string>::Frob(!0)
    stloc.0
    ldloca 0
    ldtoken object
    call instance bool valuetype [System.Runtime]System.RuntimeTypeHandle::Equals(valuetype [System.Runtime]System.RuntimeTypeHandle)
    brtrue Okay
    ldc.i4.m1
    ret

  Okay:
    ldc.i4 100
    ret
  }
}
