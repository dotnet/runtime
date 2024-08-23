// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//
// Verifies that RuntimeFeature::IsSupported("DefaultImplementationsOfInterfaces") matches reality.
// This needs to succeed no matter whether default interfaces are supported.
//
.assembly extern mscorlib { auto }
.assembly extern xunit.core {}
.assembly extern System.Runtime { auto }

.assembly DefaultImplementationsOfInterfaces{}

.class interface public abstract auto ansi DefaultInterface
{
  .method public hidebysig newslot virtual
          instance void  Method() cil managed
  {
    .maxstack  8
    ret
  }
}

.method private hidebysig static void TryLoadDefaultInterface() cil managed noinlining
{
    .maxstack  8
    ldtoken    DefaultInterface
    call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    callvirt   instance string [System.Runtime]System.Object::ToString()
    pop
    ret
}

.method public hidebysig static bool  SupportsDefaultInterfaces() cil managed
{
    .maxstack  1
    .try
    {
      call       void TryLoadDefaultInterface()
      leave.s    Supports

    }
    catch [System.Runtime]System.TypeLoadException
    {
      pop
      leave.s    DoesNotSupport

    }
  Supports:
    ldc.i4.1
    ret

  DoesNotSupport:
    ldc.i4.0
    ret
  }

.class public auto ansi abstract sealed beforefieldinit DefaultImplementationsOfInterfaces
    extends [System.Runtime]System.Object
{
  .method public hidebysig static int32
          Main() cil managed
  {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  2
      ldstr      "DefaultImplementationsOfInterfaces"
      call       bool [System.Runtime]System.Runtime.CompilerServices.RuntimeFeature::IsSupported(string)
      call       bool SupportsDefaultInterfaces()
      beq        Good

      ldc.i4.m1
      ret

    Good:
      ldc.i4     100
      ret
  }
}
