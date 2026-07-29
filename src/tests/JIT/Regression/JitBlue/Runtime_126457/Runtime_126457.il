// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In some cases the Inliner can insert a CAST node between
// a tail call and the return

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
}

.assembly Runtime_126457 { }
.module Runtime_126457.dll

.class interface public abstract auto ansi ISite
{
  .method public hidebysig newslot abstract virtual
    instance bool tailcall() cil managed
  {
  }
}

.class public auto ansi beforefieldinit Component
       extends [System.Runtime]System.Object
{
  .field family class ISite _site

  .method family hidebysig newslot virtual
    instance bool callee() cil managed
  {
    .maxstack 8
    ldarg.0
    ldfld      class ISite Component::_site
    dup
    brtrue.s   CALL
    pop
    ldc.i4.0
    br.s       DONE
  CALL:
    callvirt   instance bool ISite::tailcall()
  DONE:
    ret
  }

  .method public hidebysig specialname rtspecialname
    instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [System.Runtime]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit DerivedComponent
       extends Component
{
  .method public hidebysig virtual
    instance bool caller() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance bool Component::callee()
    ret
  }

  .method public hidebysig specialname rtspecialname
    instance void .ctor() cil managed
  {
    ldarg.0
    call instance void Component::.ctor()
    ret
  }
}

.class private auto ansi beforefieldinit Program
       extends [System.Runtime]System.Object
{
  .method private hidebysig static int32
    Main() cil managed
  {
    .entrypoint
    .maxstack 1
    newobj instance void DerivedComponent::.ctor()
    call bool Program::Test(class DerivedComponent)
    pop
    ldc.i4.s 100
    ret
  }

  .method private hidebysig static bool
    Test(class DerivedComponent c) cil managed noinlining
  {
    .maxstack 8
    ldarg.0
    callvirt instance bool DerivedComponent::caller()
    ret
  }
}
