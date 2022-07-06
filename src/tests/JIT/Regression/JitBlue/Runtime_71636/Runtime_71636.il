// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly extern System.Console { }

.assembly Runtime_71636.dll { }

.class Runtime_71636 extends [System.Runtime]System.Object
{
  .method public static int32 Main()
  {
    .entrypoint

    call bool Runtime_71636::ProblemWithLdElemA()
    brtrue FAILURE_LDELEMA

    call bool Runtime_71636::ProblemWithLdElem()
    brtrue FAILURE_LDELEM

    call bool Runtime_71636::ProblemWithStElem()
    brtrue FAILURE_STELEM

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
