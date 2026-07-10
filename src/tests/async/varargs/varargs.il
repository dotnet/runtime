// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests for Task/ValueTask-returning methods that use the '__arglist' (vararg
// calling convention) feature in a runtime-async enabled assembly.
//
// '__arglist' is not a supported combination with runtime async. The goal of
// these tests is simply to make sure that:
//
//   * An ordinary (non-async) Task/ValueTask-returning vararg method continues
//     to work the way it always has - it can be called directly (producing a
//     Task/ValueTask) and it can be awaited from a runtime-async method. Such a
//     method must be treated as an ordinary method and no async variant may be
//     generated for it.
//
//   * A runtime-async ('async' MethodImpl) Task/ValueTask-returning vararg
//     method is an unsupported combination and must fail deterministically at
//     type load (with a TypeLoadException) rather than crashing or silently
//     producing a broken async thunk.
//
// These scenarios are authored in IL because C# does not allow the '__arglist'
// feature to be mixed with 'async' when runtime-async codegen is enabled
// (Roslyn reports CS9328).
//
// Managed varargs ('__arglist') are only supported on Windows (see
// compFeatureVarArg in src/coreclr/jit/target.h), so the scenarios that
// actually issue or reference a vararg call are guarded to run only there. The
// vararg call sites are isolated in dedicated helper methods so that on other
// platforms the guarded test methods can be jitted without hitting the
// unsupported vararg codegen.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) .ver 4:0:0:0 }
.assembly extern xunit.core {}

.assembly varargs
{
}

// =====================================================================
// Ordinary (non-async) vararg methods and the tests that exercise them.
// =====================================================================
.class public auto ansi abstract sealed beforefieldinit Async2Varargs
       extends [System.Runtime]System.Object
{
    // --- Ordinary (non-async) vararg callees -----------------------------

    // Ordinary Task-returning method that uses __arglist. It sums the leading
    // argument together with all the vararg int32 arguments and returns the
    // result wrapped in an already-completed Task<int32>.
    .method public hidebysig static class [System.Runtime]System.Threading.Tasks.Task`1<int32>
            OrdinaryTaskVararg(int32 first, ...) cil managed noinlining
    {
        .maxstack 3
        .locals init (
            valuetype [System.Runtime]System.ArgIterator V_args,
            int32 V_sum,
            typedref V_tr
        )

        ldarg.0
        stloc.1

        ldloca.s V_args
        arglist
        call instance void [System.Runtime]System.ArgIterator::.ctor(valuetype [System.Runtime]System.RuntimeArgumentHandle)

    LoopStart:
        ldloca.s V_args
        call instance int32 [System.Runtime]System.ArgIterator::GetRemainingCount()
        brfalse.s LoopEnd

        ldloca.s V_args
        call instance typedref [System.Runtime]System.ArgIterator::GetNextArg()
        stloc.2
        ldloc.1
        ldloc.2
        refanyval [System.Runtime]System.Int32
        ldind.i4
        add
        stloc.1
        br.s LoopStart

    LoopEnd:
        ldloc.1
        call class [System.Runtime]System.Threading.Tasks.Task`1<int32> [System.Runtime]System.Threading.Tasks.Task::FromResult<int32>(!!0)
        ret
    }

    // Ordinary ValueTask<int32>-returning method that uses __arglist.
    .method public hidebysig static valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32>
            OrdinaryValueTaskVararg(int32 first, ...) cil managed noinlining
    {
        .maxstack 3
        .locals init (
            valuetype [System.Runtime]System.ArgIterator V_args,
            int32 V_sum,
            typedref V_tr
        )

        ldarg.0
        stloc.1

        ldloca.s V_args
        arglist
        call instance void [System.Runtime]System.ArgIterator::.ctor(valuetype [System.Runtime]System.RuntimeArgumentHandle)

    LoopStart:
        ldloca.s V_args
        call instance int32 [System.Runtime]System.ArgIterator::GetRemainingCount()
        brfalse.s LoopEnd

        ldloca.s V_args
        call instance typedref [System.Runtime]System.ArgIterator::GetNextArg()
        stloc.2
        ldloc.1
        ldloc.2
        refanyval [System.Runtime]System.Int32
        ldind.i4
        add
        stloc.1
        br.s LoopStart

    LoopEnd:
        ldloc.1
        newobj instance void valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32>::.ctor(!0)
        ret
    }

    // --- Isolated vararg call sites --------------------------------------
    // The 'call vararg' instructions live in these dedicated helpers so that
    // the guarded test methods below can be jitted on platforms where managed
    // varargs are unsupported (the helpers are only invoked on Windows).

    // Direct call of the ordinary Task-returning vararg method, returning its
    // (synchronously available) result.
    .method private hidebysig static int32 CallOrdinaryTaskVararg() cil managed noinlining
    {
        .maxstack 4
        ldc.i4.s 1
        ldc.i4.s 2
        ldc.i4.s 3
        ldc.i4.s 4
        call vararg class [System.Runtime]System.Threading.Tasks.Task`1<int32> Async2Varargs::OrdinaryTaskVararg(int32, ..., int32, int32, int32)
        callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task`1<int32>::get_Result()
        ret
    }

    // Direct call of the ordinary ValueTask-returning vararg method, returning
    // its (synchronously available) result.
    .method private hidebysig static int32 CallOrdinaryValueTaskVararg() cil managed noinlining
    {
        .maxstack 4
        .locals init (
            valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32> V_0
        )
        ldc.i4.s 50
        ldc.i4.s 5
        ldc.i4.s 6
        call vararg valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32> Async2Varargs::OrdinaryValueTaskVararg(int32, ..., int32, int32)
        stloc.0
        ldloca.s V_0
        call instance !0 valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32>::get_Result()
        ret
    }

    // --- runtime-async await wrappers ------------------------------------
    // These runtime-async methods await the result of the ordinary vararg
    // methods. This exercises the 'await' path against a Task/ValueTask that is
    // produced by a vararg call site.

    .method private hidebysig static class [System.Runtime]System.Threading.Tasks.Task`1<int32>
            AwaitOrdinaryTaskVararg() cil managed async noinlining
    {
        .maxstack 8
        ldc.i4.s 10
        ldc.i4.s 20
        ldc.i4.s 12
        call vararg class [System.Runtime]System.Threading.Tasks.Task`1<int32> Async2Varargs::OrdinaryTaskVararg(int32, ..., int32, int32)
        call !!0 [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await<int32>(class [System.Runtime]System.Threading.Tasks.Task`1<!!0>)
        ret
    }

    .method private hidebysig static class [System.Runtime]System.Threading.Tasks.Task`1<int32>
            AwaitOrdinaryValueTaskVararg() cil managed async noinlining
    {
        .maxstack 8
        ldc.i4.s 100
        ldc.i4.s 30
        ldc.i4.s 7
        call vararg valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32> Async2Varargs::OrdinaryValueTaskVararg(int32, ..., int32, int32)
        call !!0 [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await<int32>(valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<!!0>)
        ret
    }

    // Isolated helpers that reference the unsupported runtime-async vararg
    // methods. Loading the declaring type triggers the type load failure; the
    // reference is isolated here so the vararg call site is only jitted on
    // Windows.
    .method private hidebysig static void TriggerAsyncTaskVararg() cil managed noinlining
    {
        .maxstack 8
        ldc.i4.0
        ldc.i4.0
        call vararg class [System.Runtime]System.Threading.Tasks.Task`1<int32> AsyncTaskVarargs::AsyncTaskVararg(int32, ..., int32)
        pop
        ret
    }

    .method private hidebysig static void TriggerAsyncValueTaskVararg() cil managed noinlining
    {
        .maxstack 8
        ldc.i4.0
        ldc.i4.0
        call vararg valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32> AsyncValueTaskVarargs::AsyncValueTaskVararg(int32, ..., int32)
        pop
        ret
    }

    // --- Tests -----------------------------------------------------------

    // Direct call: an ordinary Task-returning vararg method is callable and
    // produces a Task whose result is the sum of its arguments.
    .method public hidebysig static void DirectCallTaskVarargTest() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2

        call bool [System.Runtime]System.OperatingSystem::IsWindows()
        brfalse.s Ok

        call int32 Async2Varargs::CallOrdinaryTaskVararg()
        ldc.i4.s 10
        beq.s Ok

        ldstr "DirectCallTaskVarargTest returned the wrong value"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw

    Ok:
        ret
    }

    // Direct call: an ordinary ValueTask-returning vararg method is callable and
    // produces a ValueTask whose result is the sum of its arguments.
    .method public hidebysig static void DirectCallValueTaskVarargTest() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2

        call bool [System.Runtime]System.OperatingSystem::IsWindows()
        brfalse.s Ok

        call int32 Async2Varargs::CallOrdinaryValueTaskVararg()
        ldc.i4.s 61
        beq.s Ok

        ldstr "DirectCallValueTaskVarargTest returned the wrong value"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw

    Ok:
        ret
    }

    // Await: a runtime-async method can await a Task produced by a vararg call.
    .method public hidebysig static void AwaitTaskVarargTest() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2

        call bool [System.Runtime]System.OperatingSystem::IsWindows()
        brfalse.s Ok

        call class [System.Runtime]System.Threading.Tasks.Task`1<int32> Async2Varargs::AwaitOrdinaryTaskVararg()
        callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task`1<int32>::get_Result()
        ldc.i4.s 42
        beq.s Ok

        ldstr "AwaitTaskVarargTest returned the wrong value"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw

    Ok:
        ret
    }

    // Await: a runtime-async method can await a ValueTask produced by a vararg call.
    .method public hidebysig static void AwaitValueTaskVarargTest() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2

        call bool [System.Runtime]System.OperatingSystem::IsWindows()
        brfalse.s Ok

        call class [System.Runtime]System.Threading.Tasks.Task`1<int32> Async2Varargs::AwaitOrdinaryValueTaskVararg()
        callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task`1<int32>::get_Result()
        ldc.i4.s 137
        beq.s Ok

        ldstr "AwaitValueTaskVarargTest returned the wrong value"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw

    Ok:
        ret
    }

    // A runtime-async ('async' MethodImpl) Task-returning vararg method is an
    // unsupported combination. Loading the type that declares it must fail
    // deterministically with a TypeLoadException rather than crashing.
    .method public hidebysig static void AsyncVarargTaskFailsTest() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 1

        call bool [System.Runtime]System.OperatingSystem::IsWindows()
        brfalse.s Ok

        .try
        {
            call void Async2Varargs::TriggerAsyncTaskVararg()
            ldstr "Expected loading a runtime-async vararg method to fail, but it succeeded."
            newobj instance void [System.Runtime]System.Exception::.ctor(string)
            throw
        }
        catch [System.Runtime]System.TypeLoadException
        {
            pop
            leave.s Ok
        }

    Ok:
        ret
    }

    // A runtime-async ('async' MethodImpl) ValueTask-returning vararg method
    // must also fail deterministically with a TypeLoadException at type load.
    .method public hidebysig static void AsyncVarargValueTaskFailsTest() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 1

        call bool [System.Runtime]System.OperatingSystem::IsWindows()
        brfalse.s Ok

        .try
        {
            call void Async2Varargs::TriggerAsyncValueTaskVararg()
            ldstr "Expected loading a runtime-async vararg method to fail, but it succeeded."
            newobj instance void [System.Runtime]System.Exception::.ctor(string)
            throw
        }
        catch [System.Runtime]System.TypeLoadException
        {
            pop
            leave.s Ok
        }

    Ok:
        ret
    }
}

// =====================================================================
// Unsupported runtime-async ('async' MethodImpl) vararg methods. These types
// must fail to load; they are only referenced through the isolated helpers
// above.
// =====================================================================
.class public auto ansi abstract sealed beforefieldinit AsyncTaskVarargs
       extends [System.Runtime]System.Object
{
    .method public hidebysig static class [System.Runtime]System.Threading.Tasks.Task`1<int32>
            AsyncTaskVararg(int32 first, ...) cil managed async
    {
        .maxstack 8
        .locals init (
            valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable V_0,
            valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter V_1
        )

        call valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable [System.Runtime]System.Threading.Tasks.Task::Yield()
        stloc.0
        ldloca.s V_0
        call instance valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
        stloc.1
        ldloc.1
        call void [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::UnsafeAwaitAwaiter<valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter>(!!0)

        ldarg.0
        ret
    }
}

.class public auto ansi abstract sealed beforefieldinit AsyncValueTaskVarargs
       extends [System.Runtime]System.Object
{
    .method public hidebysig static valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32>
            AsyncValueTaskVararg(int32 first, ...) cil managed async
    {
        .maxstack 8
        .locals init (
            valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable V_0,
            valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter V_1
        )

        call valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable [System.Runtime]System.Threading.Tasks.Task::Yield()
        stloc.0
        ldloca.s V_0
        call instance valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
        stloc.1
        ldloc.1
        call void [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::UnsafeAwaitAwaiter<valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter>(!!0)

        ldarg.0
        ret
    }
}
