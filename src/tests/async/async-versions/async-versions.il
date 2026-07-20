// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests that a (non-async) Task-returning method can transfer control to another
// Task-returning method using the 'jmp' instruction and using a 'tail.'-prefixed
// call. The transferring methods themselves are NOT marked async; the target may
// either be a runtime-async method that suspends, or an ordinary method that
// completes synchronously (returns an already-completed task).

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) .ver 4:0:0:0 }
.assembly extern xunit.core {}

.assembly 'async-versions'
{
}

.class public auto ansi abstract sealed beforefieldinit AsyncVersions
       extends [System.Runtime]System.Object
{
    // --- Callees ---------------------------------------------------------

    // Runtime-async callee that suspends once and then returns 42.
    .method public hidebysig static class [System.Runtime]System.Threading.Tasks.Task`1<int32>
            SuspendingCallee42() cil managed async
    {
        .maxstack 8
        .locals init (
            valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable V_0,
            valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter V_1
        )

        // await Task.Yield();
        call valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable [System.Runtime]System.Threading.Tasks.Task::Yield()
        stloc.0
        ldloca.s V_0
        call instance valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
        stloc.1
        ldloc.1
        call void [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::UnsafeAwaitAwaiter<valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter>(!!0)

        ldc.i4.s 42
        ret
    }

    // Runtime-async callee that suspends once and then returns 84.
    .method public hidebysig static class [System.Runtime]System.Threading.Tasks.Task`1<int32>
            SuspendingCallee84() cil managed async
    {
        .maxstack 8
        .locals init (
            valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable V_0,
            valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter V_1
        )

        // await Task.Yield();
        call valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable [System.Runtime]System.Threading.Tasks.Task::Yield()
        stloc.0
        ldloca.s V_0
        call instance valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
        stloc.1
        ldloc.1
        call void [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::UnsafeAwaitAwaiter<valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter>(!!0)

        ldc.i4.s 84
        ret
    }

    // Ordinary (non-async) callee that completes synchronously without suspending.
    .method public hidebysig static class [System.Runtime]System.Threading.Tasks.Task
            CompletedCallee() cil managed noinlining
    {
        .maxstack 8
        call class [System.Runtime]System.Threading.Tasks.Task [System.Runtime]System.Threading.Tasks.Task::get_CompletedTask()
        ret
    }

    // --- jmp transfer methods (not async) --------------------------------

    // Non-async Task-returning method that uses 'jmp' to a suspending async method.
    .method public hidebysig static class [System.Runtime]System.Threading.Tasks.Task`1<int32>
            JmpToSuspending() cil managed
    {
        .maxstack 8
        jmp class [System.Runtime]System.Threading.Tasks.Task`1<int32> AsyncVersions::SuspendingCallee42()
    }

    // Non-async Task-returning method that uses 'jmp' to a synchronously-completing method.
    .method public hidebysig static class [System.Runtime]System.Threading.Tasks.Task
            JmpToCompleted() cil managed
    {
        .maxstack 8
        jmp class [System.Runtime]System.Threading.Tasks.Task AsyncVersions::CompletedCallee()
    }

    // --- tail. transfer methods (not async) ------------------------------

    // Non-async Task-returning method that uses 'tail.' to a suspending async method.
    .method public hidebysig static class [System.Runtime]System.Threading.Tasks.Task`1<int32>
            TailToSuspending() cil managed
    {
        .maxstack 8
        tail. call class [System.Runtime]System.Threading.Tasks.Task`1<int32> AsyncVersions::SuspendingCallee84()
        ret
    }

    // Non-async Task-returning method that uses 'tail.' to a synchronously-completing method.
    .method public hidebysig static class [System.Runtime]System.Threading.Tasks.Task
            TailToCompleted() cil managed
    {
        .maxstack 8
        tail. call class [System.Runtime]System.Threading.Tasks.Task AsyncVersions::CompletedCallee()
        ret
    }

    // --- async await wrappers --------------------------------------------
    // These runtime-async methods actually await the result of the transfer
    // methods, so the await path (rather than a blocking Task.Result) drives
    // the jmp/tail. target. The non-async test methods below block on these.

    .method public hidebysig static class [System.Runtime]System.Threading.Tasks.Task`1<int32>
            AwaitJmpToSuspending() cil managed async
    {
        .maxstack 8
        call class [System.Runtime]System.Threading.Tasks.Task`1<int32> AsyncVersions::JmpToSuspending()
        call !!0 [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await<int32>(class [System.Runtime]System.Threading.Tasks.Task`1<!!0>)
        ret
    }

    .method public hidebysig static class [System.Runtime]System.Threading.Tasks.Task
            AwaitJmpToCompleted() cil managed async
    {
        .maxstack 8
        call class [System.Runtime]System.Threading.Tasks.Task AsyncVersions::JmpToCompleted()
        call void [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
        ret
    }

    .method public hidebysig static class [System.Runtime]System.Threading.Tasks.Task`1<int32>
            AwaitTailToSuspending() cil managed async
    {
        .maxstack 8
        call class [System.Runtime]System.Threading.Tasks.Task`1<int32> AsyncVersions::TailToSuspending()
        call !!0 [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await<int32>(class [System.Runtime]System.Threading.Tasks.Task`1<!!0>)
        ret
    }

    .method public hidebysig static class [System.Runtime]System.Threading.Tasks.Task
            AwaitTailToCompleted() cil managed async
    {
        .maxstack 8
        call class [System.Runtime]System.Threading.Tasks.Task AsyncVersions::TailToCompleted()
        call void [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
        ret
    }

    // --- Tests -----------------------------------------------------------

    .method public hidebysig static void JmpToSuspendingTest() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2

        call class [System.Runtime]System.Threading.Tasks.Task`1<int32> AsyncVersions::AwaitJmpToSuspending()
        callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task`1<int32>::get_Result()
        ldc.i4.s 42
        beq.s Ok

        ldstr "JmpToSuspending returned the wrong value"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw

    Ok:
        ret
    }

    .method public hidebysig static void JmpToCompletedTest() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 1

        call class [System.Runtime]System.Threading.Tasks.Task AsyncVersions::AwaitJmpToCompleted()
        callvirt instance void [System.Runtime]System.Threading.Tasks.Task::Wait()
        ret
    }

    .method public hidebysig static void TailToSuspendingTest() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2

        call class [System.Runtime]System.Threading.Tasks.Task`1<int32> AsyncVersions::AwaitTailToSuspending()
        callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task`1<int32>::get_Result()
        ldc.i4.s 84
        beq.s Ok

        ldstr "TailToSuspending returned the wrong value"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw

    Ok:
        ret
    }

    .method public hidebysig static void TailToCompletedTest() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 1

        call class [System.Runtime]System.Threading.Tasks.Task AsyncVersions::AwaitTailToCompleted()
        callvirt instance void [System.Runtime]System.Threading.Tasks.Task::Wait()
        ret
    }
}
