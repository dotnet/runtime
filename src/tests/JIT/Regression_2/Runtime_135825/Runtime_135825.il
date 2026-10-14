// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Each method below ends with a conditional branch back to an earlier instruction, so control can
// fall off the end of the method body. The IL is invalid. This test expects InvalidProgramException
// from the existing BADCODE check in fgLinkBasicBlocks, before it adds the fall-through edge to
// the missing next block.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core { }
.assembly Runtime_135825 { }

.class public auto ansi abstract sealed beforefieldinit Runtime_135825
       extends [System.Runtime]System.Object
{
  // IL_0000 ldc.i4.0; IL_0001 brtrue.s IL_0000
  .method private hidebysig static void BrtrueSBackToStart() cil managed noinlining
  {
    .maxstack 1
  L0:
    ldc.i4.0
    brtrue.s L0
  }

  // IL_0000 nop; IL_0001 ldc.i4.0; IL_0002 brtrue.s IL_0001 (the method has two blocks)
  .method private hidebysig static void BrtrueSBackToSecond() cil managed noinlining
  {
    .maxstack 1
    nop
  L1:
    ldc.i4.0
    brtrue.s L1
  }

  // IL_0000 ldc.i4.0; IL_0001 brfalse.s IL_0000
  .method private hidebysig static void BrfalseSBackToStart() cil managed noinlining
  {
    .maxstack 1
  L0:
    ldc.i4.0
    brfalse.s L0
  }

  // IL_0000 ldc.i4.1; IL_0001 brtrue IL_0000 (long form)
  .method private hidebysig static void BrtrueBackToStart() cil managed noinlining
  {
    .maxstack 1
  L0:
    ldc.i4.1
    brtrue L0
  }

  // IL_0000 ldc.i4.0; IL_0001 ldc.i4.1; IL_0002 beq.s IL_0000 (compare-and-branch)
  .method private hidebysig static void BeqSBackToStart() cil managed noinlining
  {
    .maxstack 2
  L0:
    ldc.i4.0
    ldc.i4.1
    beq.s L0
  }

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack 1

    .try
    {
      call void Runtime_135825::BrtrueSBackToStart()
      leave FAIL
    }
    catch [System.Runtime]System.InvalidProgramException
    {
      pop
      leave.s T2
    }
  T2:
    .try
    {
      call void Runtime_135825::BrtrueSBackToSecond()
      leave FAIL
    }
    catch [System.Runtime]System.InvalidProgramException
    {
      pop
      leave.s T3
    }
  T3:
    .try
    {
      call void Runtime_135825::BrfalseSBackToStart()
      leave FAIL
    }
    catch [System.Runtime]System.InvalidProgramException
    {
      pop
      leave.s T4
    }
  T4:
    .try
    {
      call void Runtime_135825::BrtrueBackToStart()
      leave FAIL
    }
    catch [System.Runtime]System.InvalidProgramException
    {
      pop
      leave.s T5
    }
  T5:
    .try
    {
      call void Runtime_135825::BeqSBackToStart()
      leave FAIL
    }
    catch [System.Runtime]System.InvalidProgramException
    {
      pop
      leave.s PASS
    }

  PASS:
    ldc.i4.s 100
    ret

  FAIL:
    ldc.i4.s 101
    ret
  }
}
