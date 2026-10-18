// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Helper-based tail calls keep their arguments in a buffer until the callee is invoked. The interior pointer
// of a Span<T> argument was not reported to the GC there, so a GC on another thread could leave the callee
// with a stale pointer to the old location of the array.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Threading.Thread { auto }
.assembly extern xunit.core { }
.assembly Runtime_136010 { }

.class public auto ansi beforefieldinit Runtime_136010
       extends [System.Runtime]System.Object
{
  .field private static bool s_stop

  .method public static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack 4
    .locals init (class [System.Threading.Thread]System.Threading.Thread V_0,
                  int64 V_1,
                  int32[] V_2,
                  int32 V_3,
                  int32 V_4)

    ldnull
    ldftn void Runtime_136010::ForceGCs()
    newobj instance void [System.Threading.Thread]System.Threading.ThreadStart::.ctor(object, native int)
    newobj instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [System.Threading.Thread]System.Threading.ThreadStart)
    stloc.0
    ldloc.0
    callvirt instance void [System.Threading.Thread]System.Threading.Thread::Start()

    call int64 [System.Runtime]System.Environment::get_TickCount64()
    ldc.i8 1000
    add
    stloc.1

    ldc.i4.s 100
    stloc.s V_4

  LOOP:
    ldc.i4.s 64
    newarr [System.Runtime]System.Int32
    stloc.2
    ldloc.2
    call int32 Runtime_136010::Caller(int32[])
    ldc.i4.s 64
    bne.un FAIL

    ldc.i4.0
    stloc.3
  CHECK:
    ldloc.3
    ldc.i4.s 64
    bge NEXT
    ldloc.2
    ldloc.3
    ldelem.i4
    ldloc.3
    bne.un FAIL
    ldloc.3
    ldc.i4.1
    add
    stloc.3
    br CHECK

  NEXT:
    call int64 [System.Runtime]System.Environment::get_TickCount64()
    ldloc.1
    blt LOOP
    br EXIT

  FAIL:
    ldc.i4.s 101
    stloc.s V_4

  EXIT:
    ldc.i4.1
    volatile.
    stsfld bool Runtime_136010::s_stop
    ldloc.0
    callvirt instance void [System.Threading.Thread]System.Threading.Thread::Join()
    ldloc.s V_4
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

  // Compacting GCs move the arrays while Main is in the middle of the tail calls
  .method private hidebysig static void ForceGCs() cil managed
  {
    .maxstack 4

  LOOP:
    volatile.
    ldsfld bool Runtime_136010::s_stop
    brtrue DONE

    ldc.i4.2
    ldc.i4.1
    ldc.i4.1
    ldc.i4.1
    call void [System.Runtime]System.GC::Collect(int32, valuetype [System.Runtime]System.GCCollectionMode, bool, bool)
    br LOOP

  DONE:
    ret
  }
}
