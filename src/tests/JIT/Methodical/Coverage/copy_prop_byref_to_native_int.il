// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly 'copy_prop_byref_to_native_int' { }
.assembly extern xunit.core {}

.typedef [System.Runtime]System.WeakReference as WeakRef
.typedef [System.Runtime]System.GC as GC

.namespace Test_copy_prop_byref_to_native_int_il
{
.class public Program extends [System.Runtime]System.Object
{
  .field private static class WeakRef s_weakArrRef
  .field private static bool s_arrIsAlive

  .method public static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals ( [0] int32 result, [1] int32[] arr )

    ldc.i4 1
    ldc.i4 1  // The array will be pinned.
    call !!0[] GC::AllocateArray<int32>(int32, bool)
    stloc arr

    ldloc arr
    newobj instance void WeakRef::.ctor(object)
    stsfld class WeakRef Test_copy_prop_byref_to_native_int_il.Program::s_weakArrRef

    ldc.i4 1
    stsfld bool Test_copy_prop_byref_to_native_int_il.Program::s_arrIsAlive

    ldloc arr
    ldc.i4 0
    ldelema int32
    dup
    ldc.i4 1
    call unsigned int8& Test_copy_prop_byref_to_native_int_il.Program::Problem(unsigned int8&, native int, int32)
    brtrue SKIP

    // Unreachable, exists to not have "arr"'s liveness extended.
    call unsigned int8& Test_copy_prop_byref_to_native_int_il.Program::SideEffect()
    pop

  SKIP:
    ldsfld bool Test_copy_prop_byref_to_native_int_il.Program::s_arrIsAlive
    brtrue SUCCESS

    ldc.i4 101
    ret

  SUCCESS:
    ldc.i4 100
    ret
  }

  .method private static unsigned int8& Problem(unsigned int8& byrefAddrArg, native int nintAddr, int32 notZeroArg) cil managed noinlining
  {
    .locals ( [0] int32 i, [1] native int nintAddrCopy, [2] unsigned int8& byrefAddr )

    ldarg nintAddr
    stloc byrefAddr

    // Before this point, array is kept alive by "byrefAddrArg".
    ldarg byrefAddrArg
    call void Test_copy_prop_byref_to_native_int_il.Program::Use(unsigned int8&)
    // Now the array should be kept alive by "byrefAddr".

    ldc.i4 0
    stloc i
  LOOP:
    ldloc i
    ldc.i4 1
    add
    stloc i

    call void Test_copy_prop_byref_to_native_int_il.Program::TryCollectArr()

    ldloc i
    ldc.i4 10
    ble LOOP

    ldarg nintAddr
    stloc nintAddrCopy

    ldarg notZeroArg
    ldc.i4 0
    bne.un SKIP

    // This path is unreachable and only exists so
    // that local copy propagation does not substitute
    // "nintAddrCopy" with "nintAddr" too early. We
    // want the fact they're equal to be discovered in VN.
    call unsigned int8& Test_copy_prop_byref_to_native_int_il.Program::SideEffect()
    ret

  SKIP:
    // The below sequence is meant to reduce to just "ldloc byrefAddr; ret;"
    // It exists because for copy propagation to perform the (wrong) substituion
    // of "byrefAddr" with "nintAddr", we need to make it seem that "nintAddr"
    // is live at the point of substition (#Use), so we fake it up with some
    // instructions that VN knows will evaluate to nothing, but it'll take
    // until constant VN-based propagation for that fact to be acted on.

    ldarg nintAddr
    ldloc nintAddrCopy
    ceq
    ldc.i4 0
    ceq
    conv.u

    ldloc byrefAddr // #Use
    ldarg nintAddr
    ldloc nintAddrCopy
    ceq
    ldc.i4 0
    ceq
    conv.u
    add

    add
    ret
  }

  .method private static void TryCollectArr() cil managed noinlining
  {
    .locals ( [0] int32 arrValue )

    ldc.i4 2
    ldc.i4 1 // GCCollectionMode.Forced
    call void GC::Collect(int32, valuetype [System.Runtime]System.GCCollectionMode)
    call void GC::WaitForPendingFinalizers()

    ldsfld class WeakRef Test_copy_prop_byref_to_native_int_il.Program::s_weakArrRef
    callvirt instance bool WeakRef::get_IsAlive()
    ldsfld bool Test_copy_prop_byref_to_native_int_il.Program::s_arrIsAlive
    and
    stsfld bool Test_copy_prop_byref_to_native_int_il.Program::s_arrIsAlive

    ret
  }

  .method private static unsigned int8& SideEffect() cil managed noinlining
  {
    ldc.i4 0
    conv.u
    ret
  }

  .method private static void Use(unsigned int8& byref) cil managed noinlining
  {
    ret
  }
}
}
