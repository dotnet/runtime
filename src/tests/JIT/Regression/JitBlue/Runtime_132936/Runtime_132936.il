// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The finally handler below does not end with its endfinally instruction, it ends with a
// branch back to it. Mono used to assume that the last byte of a finally handler is the
// endfinally opcode when linking the handler with the target of the leave instruction,
// which created a bogus cfg edge from a bblock which was never emitted. That left an
// unfilled phi argument behind, which the array bounds check removal pass then followed.
// The dead code inside the handler is what pushes the endfinally away from the end.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core { }
.assembly Runtime_132936 { }

.class public sequential ansi sealed beforefieldinit Scope
       extends [System.Runtime]System.ValueType
{
  .field private int32 _dummy

  .method public hidebysig instance void Close() cil managed
  {
    .maxstack 8
    ret
  }
}

.class public auto ansi sealed beforefieldinit Runtime_132936
       extends [System.Runtime]System.Object
{
  .method public static int32 Main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack 2

    ldc.i4.4
    call int32 Runtime_132936::Test(int32)
    ldc.i4.4
    bne.un.s FAIL

    ldc.i4.4
    call int32 Runtime_132936::TestDeadCodeAfterEndFinally(int32)
    ldc.i4.4
    bne.un.s FAIL

    ldc.i4.s 100
    ret

  FAIL:
    ldc.i4.s 101
    ret
  }

  // Returns the number of elements it walked over, i.e. len.
  .method private static int32 Test(int32 len) cil managed
  {
    .maxstack 4
    .locals init (int32[] V_0, valuetype Scope V_1, int32 V_2)

    .try
    {
              ldarg.0
              newarr    [System.Runtime]System.Int32
              ldc.i4.7
              brtrue.s  STORE
              pop
    MID:      nop
              leave.s   AFTER

    STORE:    stloc.0
              br.s      MID
    }
    finally
    {
              ldloca.s  V_1
              ldc.i4.3
              brtrue.s  CLOSE
              pop
    EF:       endfinally

    CLOSE:    call      instance void Scope::Close()
              br.s      EF
    }

  AFTER:      ldc.i4.0
              stloc.2
              br.s      COND

  LOOP:       ldloc.0
              ldloc.2
              ldelem.i4
              pop
              ldloc.2
              ldc.i4.1
              add
              stloc.2

  COND:       ldloc.2
              ldloc.0
              ldlen
              conv.i4
              blt.s     LOOP

              ldloc.2
              ret
  }

  // Same problem, but here the block following the endfinally is unreachable. Mono terminates
  // the llvm basic block of the handler with an implicit branch to the next bblock, which is
  // never emitted because it is unreachable, leaving a block without a terminator behind.
  //
  // Returns the number of elements it walked over, i.e. len.
  .method private static int32 TestDeadCodeAfterEndFinally(int32 len) cil managed
  {
    .maxstack 4
    .locals init (int32[] V_0, int32 V_1)

    .try
    {
              ldarg.0
              newarr    [System.Runtime]System.Int32
              stloc.0
              leave.s   AFTER
    }
    finally
    {
    EF:       endfinally
              br.s      EF
    }

  AFTER:      ldc.i4.0
              stloc.1
              br.s      COND

  LOOP:       ldloc.0
              ldloc.1
              ldelem.i4
              pop
              ldloc.1
              ldc.i4.1
              add
              stloc.1

  COND:       ldloc.1
              ldloc.0
              ldlen
              conv.i4
              blt.s     LOOP

              ldloc.1
              ret
  }
}
