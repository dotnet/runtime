// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern System.Console { }

.assembly Runtime_71636 { }

.class public Runtime_71636 extends [System.Runtime]System.Object
{
  .field private static native int s_globalCounter;

  .method public static int32 Main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    call bool Runtime_71636::ProblemWithLdElemA()
    brtrue FAILURE_LDELEMA

    call bool Runtime_71636::ProblemWithLdElem()
    brtrue FAILURE_LDELEM

    call bool Runtime_71636::ProblemWithStElem()
    brtrue FAILURE_STELEM

    call bool Runtime_71636::ProblemWithCpObj()
    brtrue FAILURE_CPOBJ

    call bool Runtime_71636::ProblemWithLdObj()
    brtrue FAILURE_LDOBJ

    ldc.i4 100
    ret

  FAILURE_LDELEMA:
    ldc.i4 101
    ret

  FAILURE_LDELEM:
    ldc.i4 102
    ret

  FAILURE_STELEM:
    ldc.i4 103
    ret

  FAILURE_CPOBJ:
    ldc.i4 104
    ret

  FAILURE_LDOBJ:
    ldc.i4 105
    ret
  }

  .method private static bool ProblemWithLdElemA() noinlining
  {
    .locals (void* a, void* b, void*[] arr, int32 idx)

    call void*[] Runtime_71636::GetArray()
    stloc arr

    ldc.i4 0
    stloc idx

    ldloc arr
    ldloc idx
    readonly.
    ldelema void*
    ldind.i
    stloc a

    ldloc arr
    ldloc idx
    ldc.i4 10
    conv.i
    stelem.i

    ldloc arr
    ldloc idx
    readonly.
    ldelema void*
    ldind.i
    stloc b

    ldloc a
    ldloc b
    ceq
    ret
  }

  .method private static bool ProblemWithLdElem() noinlining
  {
    .locals (void* lcl, void*[] arr, int32 idx)

    call void*[] Runtime_71636::GetArray()
    stloc arr

    ldc.i4 0
    stloc idx

    ldloc arr
    ldloc idx
    ldloca lcl
    stelem.i

    ldloc arr
    ldloc idx
    ldelem void*
    call void [System.Runtime]System.GC::Collect()
    stloc lcl

    ldloc lcl
    ldloca lcl
    bne.un NOT_EQUAL

    ldc.i4 0
    ret

  NOT_EQUAL:
    ldc.i4 1
    ret
  }

  .method private static bool ProblemWithStElem() noinlining
  {
    .locals (void* lcl, void*[] arr, int32 idx)

    call void*[] Runtime_71636::GetArray()
    stloc arr

    ldc.i4 0
    stloc idx

    ldloc arr
    ldloc idx
    ldloca lcl
    stelem void*

    ldloc arr
    ldloc idx
    ldelem.i
    stloc lcl

    ldloc lcl
    ldloca lcl
    bne.un NOT_EQUAL

    ldc.i4 0
    ret

  NOT_EQUAL:
    ldc.i4 1
    ret
  }

  .method private static bool ProblemWithCpObj() noinlining
  {
    .locals (void* lcl, void*[] arr, int32 idx, native int loopIdx)

    call void*[] Runtime_71636::GetArray()
    stloc arr

    ldc.i4 0
    stloc idx

    ldloca lcl
    stloc lcl

    // A loop to force fully interruptible code - this test case could only fail under GCStress(=0xC).
    //
    ldloc arr
    ldloc idx
    ldelem.i
    stloc loopIdx
  LOOP:
    ldloc loopIdx
    ldc.i4 -1
    add
    stloc loopIdx

    ldsfld native int Runtime_71636::s_globalCounter
    ldloc loopIdx
    add
    stsfld native int Runtime_71636::s_globalCounter

    ldloc loopIdx
    ldc.i4 0
    bge LOOP

    ldloc arr
    ldloc idx
    ldelema void*
    ldloca lcl
    cpobj void*

    ldloc arr
    ldloc idx
    ldelem.i
    ldloc lcl
    bne.un NOT_EQUAL

    ldc.i4 0
    ret

  NOT_EQUAL:
    ldc.i4 1
    ret
  }

  .method private static bool ProblemWithLdObj() noinlining
  {
    .locals (void* lcl, void*[] arr, int32 idx)

    call void*[] Runtime_71636::GetArray()
    stloc arr

    ldc.i4 0
    stloc idx

    ldloc arr
    ldloc idx
    ldloca lcl
    stelem.i

    ldloc arr
    ldloc idx
    ldelema void*
    ldobj void*
    call void [System.Runtime]System.GC::Collect()
    stloc lcl

    ldloc lcl
    ldloca lcl
    bne.un NOT_EQUAL

    ldc.i4 0
    ret

  NOT_EQUAL:
    ldc.i4 1
    ret
  }

  .method private static void*[] GetArray() noinlining
  {
    .locals (void*[] arr)
    ldc.i4 1
    newarr void*
    stloc arr

    ldloc arr
    ldc.i4 0
    ldc.i4 0
    conv.i
    stelem.i

    ldloc arr
    ret
  }
}
