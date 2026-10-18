// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// A tail call that can't be a fast tail call goes through the tailcall helpers, which keep the arguments in a
// TailCallArgBuffer between the store-args and call-target stubs. The interior pointer of a Span<T> argument
// must be reported to the GC there, otherwise a compacting GC leaves it pointing to the old location of the array.

namespace Runtime_136010

open System
open System.Diagnostics
open System.Runtime.CompilerServices
open System.Threading
open Xunit
open Microsoft.DotNet.XUnitExtensions

module Main =

    // Ends with a tail call to a closure, which makes F# emit "tail." for calls to this method.
    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let fill (span: Span<int>) (finish: int -> int) : int =
        for i = 0 to span.Length - 1 do
            span[i] <- i
        finish span.Length

    // The Span is a local here, so the "tail." call can't be a fast tail call and uses the helpers.
    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let fillViaTailCall (arr: int[]) (finish: int -> int) : int =
        fill (Span<int>(arr)) finish

    let mutable failures = 0

    let runTest () : int =
        use cts = new CancellationTokenSource()

        let worker () =
            let finish = fun n -> n
            while not cts.IsCancellationRequested do
                let arr = Array.zeroCreate<int> 64
                let length = fillViaTailCall arr finish
                let mutable ok = (length = arr.Length)
                for i = 0 to arr.Length - 1 do
                    if arr[i] <> i then ok <- false
                if not ok then
                    Interlocked.Increment(&failures) |> ignore

        let threads = [| for _ in 1 .. 4 -> Thread(ThreadStart(worker)) |]
        for t in threads do
            t.Start()

        // Compacting GCs move the arrays while the workers are in the middle of the tail calls
        let sw = Stopwatch.StartNew()
        while sw.Elapsed.TotalSeconds < 2.0 && Volatile.Read(&failures) = 0 do
            GC.Collect(2, GCCollectionMode.Forced, true, true)

        cts.Cancel()
        for t in threads do
            t.Join()

        failures

    [<ConditionalFact(typeof<TestLibrary.PlatformDetection>, [| "IsMultithreadingSupported" |])>]
    [<SkipOnMono("Not supported on Mono runtime")>]
    [<ActiveIssue("Disabled on NativeAOT", typeof<TestLibrary.Utilities>, [| "IsNativeAot" |])>]
    [<SkipOnCoreClr("Too slow with GC stress", RuntimeTestModes.AnyGCStress)>]
    let main () =
        Assert.Equal(0, runTest ())

    [<EntryPoint>]
    let entryPoint _argv =
        let failureCount = runTest ()
        if failureCount = 0 then
            printfn "PASS"
            100
        else
            printfn "FAIL: %d iterations got a Span pointing to the wrong location" failureCount
            -1
