// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern xunit.core {}
.assembly extern System.Runtime { auto }

.assembly InvokeNull
{
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module InvokeNull.dll

.class public auto ansi abstract sealed beforefieldinit InvokeNull
       extends [System.Runtime]System.Object
{
  .method public hidebysig static void
          M() cil managed
  {
  }

  .method public hidebysig static int32
          Main() cil managed
  {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .locals init (method void *(class [System.Runtime]System.Action) fnptr)
      .entrypoint
      .maxstack 2
      
      ldnull
      ldftn void InvokeNull::M()
      newobj instance void [System.Runtime]System.Action::.ctor(object, native int)
      ldvirtftn instance void [System.Runtime]System.Action::Invoke()
      stloc.s fnptr

      .try
      {
        ldnull
        ldloc.s fnptr
        calli void(class [System.Runtime]System.Action)
        leave.s Fail
      }
      catch [System.Runtime]System.NullReferenceException
      {
        pop
        leave.s Success
      }

    Fail:
      ldc.i4     101
      ret
    Success:
      ldc.i4     100
      ret
  }
}
