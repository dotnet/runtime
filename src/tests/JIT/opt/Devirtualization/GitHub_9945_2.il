// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
}
.assembly extern xunit.core {}

.assembly GitHub_9945_2
{
}

.class public auto ansi beforefieldinit Base extends [mscorlib]System.Object
{
   // Need enough virtuals here to get F's slot past max virtual slot on object
  .method public hidebysig newslot virtual instance void M0() cil managed { ret }
  .method public hidebysig newslot virtual instance void M1() cil managed { ret }
  .method public hidebysig newslot virtual instance void M2() cil managed { ret }
  .method public hidebysig newslot virtual instance void M3() cil managed { ret }

  .method public hidebysig newslot virtual instance void  F() cil managed
  {
    ldstr      "Base:F"
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit Test_GitHub_9945_2 extends [mscorlib]System.Object
{
  .method public hidebysig static  int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .locals (object)
    .entrypoint
    // Store ref in supertyped local to "forget" type
    newobj     instance void Base::.ctor()
    stloc.0
    ldloc.0
    // Now callvirt via the supertype
    callvirt   instance void Base::F()
    ldc.i4.s   100
    ret
  }
}


