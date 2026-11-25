// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Helper-based tail calls keep their arguments in a buffer until the callee is invoked. The interior pointer
// of a Span<T> argument was not reported to the GC there, so a GC in between (e.g. under GC stress) left
// the callee with a stale pointer to the old location of the array.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core { }
.assembly Runtime_136010 { }

.class public auto ansi beforefieldinit Runtime_136010
       extends [System.Runtime]System.Object
{
  .method public static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack 4
    .locals init (int32[] V_0,
                  int32 V_1,
                  int32 V_2)

    ldc.i4.0
    stloc.1

  LOOP:
    ldc.i4.8
    newarr [System.Runtime]System.Int32
    stloc.0
    ldloc.0
    call int32 Runtime_136010::Caller(int32[])
    ldc.i4.8
    bne.un FAIL

    ldc.i4.0
    stloc.2
  CHECK:
    ldloc.2
    ldc.i4.8
    bge NEXT
    ldloc.0
    ldloc.2
    ldelem.i4
    ldloc.2
    bne.un FAIL
    ldloc.2
    ldc.i4.1
    add
    stloc.2
    br CHECK

  NEXT:
    ldloc.1
    ldc.i4.1
    add
    dup
    stloc.1
    ldc.i4.s 20
    blt LOOP

    ldc.i4.s 100
    ret

  FAIL:
    ldc.i4.s 101
    ret
  }

  // The Span is passed from a local, so this tail call can't be a fast tail call and goes through the helpers
  .method private hidebysig static int32 Caller(int32[] arr) cil managed noinlining
  {
    .maxstack 2
    .locals init (valuetype [System.Runtime]System.Span`1<int32> V_0)

    ldloca.s V_0
    ldarg.0
    call instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(!0[])
    ldloc.0
    tail. call int32 Runtime_136010::Fill(valuetype [System.Runtime]System.Span`1<int32>)
    ret
  }

  // Sets span[i] = i and returns the length of the span
  .method private hidebysig static int32 Fill(valuetype [System.Runtime]System.Span`1<int32> span) cil managed noinlining
  {
    .maxstack 3
    .locals init (int32 V_0)

    ldc.i4.0
    stloc.0
    br COND

  BODY:
    ldarga.s span
    ldloc.0
    call instance !0& valuetype [System.Runtime]System.Span`1<int32>::get_Item(int32)
    ldloc.0
    stind.i4
    ldloc.0
    ldc.i4.1
    add
    stloc.0

  COND:
    ldloc.0
    ldarga.s span
    call instance int32 valuetype [System.Runtime]System.Span`1<int32>::get_Length()
    blt BODY

    ldarga.s span
    call instance int32 valuetype [System.Runtime]System.Span`1<int32>::get_Length()
    ret
  }
}
