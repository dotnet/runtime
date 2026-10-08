// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.NETCore.Client;
using Tracing.Tests.Common;
using Xunit;

namespace Tracing.Tests.GCDumpRuntimeAsync
{
    // Regression coverage for https://github.com/dotnet/runtime/issues/120800.
    //
    // With Runtime Async enabled the runtime creates dynamically-generated Continuation
    // MethodTables that have no backing metadata (nil TypeDef token, no name). The GCHeapDump
    // BulkType events used to emit those types with a nil TypeNameID and an empty name, so a
    // heap dump rendered every continuation as "Type(0x02000000)". This test captures a heap
    // snapshot while Runtime Async continuations are live and verifies that every logged type
    // carries a non-empty name (the runtime now describes continuations using the base
    // Continuation type).
    public class GCDumpRuntimeAsyncTest
    {
        private static bool _seenGCStart = false;
        private static bool _seenGCStop = false;
        private static int _bulkTypeCount = 0;
        private static int _bulkNodeCount = 0;
        private static int _emptyTypeNameCount = 0;
        private static bool _sawContinuationType = false;

        private static ManualResetEvent _gcStopReceived = new ManualResetEvent(false);

        // Keeps every continuation suspended mid-await so they stay live across the heap snapshot.
        private static readonly TaskCompletionSource<int> s_gate =
            new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        private static List<Task<long>> s_liveContinuations;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<long> Suspended(byte[] captured)
        {
            // 'captured' is live across the suspension point, so the continuation holds a GC
            // reference to it -- this is the metadata-less continuation sub-type the test targets.
            await s_gate.Task;
            long sum = 0;
            for (int i = 0; i < captured.Length; i++)
                sum += captured[i];
            return sum;
        }

        [ActiveIssue("System.Diagnostics.Process is not supported on wasm", TestPlatforms.Browser)]
        [ActiveIssue("Can't find file dotnet-diagnostic-{pid}-*-socket", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsRiscv64Process))]
        [SkipOnCoreClr("This test is sensitive to JIT optimizations.", RuntimeTestModes.AnyJitOptimizationStress)]
        [SkipOnCoreClr("Tracing tests routinely time out with JIT stress and GC stress.", RuntimeTestModes.AnyGCStress)]
        [Fact]
        public static int TestEntryPoint()
        {
            // Verify Runtime Async is actually active; otherwise the test would be vacuous.
            var mi = typeof(GCDumpRuntimeAsyncTest).GetMethod(nameof(Suspended),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            bool runtimeAsync = (mi.MethodImplementationFlags & System.Reflection.MethodImplAttributes.Async) != 0;
            if (!runtimeAsync)
            {
                Console.WriteLine("Runtime Async is not enabled (Suspended is not a runtime-async method); skipping.");
                return 100;
            }

            const int ContinuationCount = 128;
            s_liveContinuations = new List<Task<long>>(ContinuationCount);
            for (int i = 0; i < ContinuationCount; i++)
            {
                byte[] payload = new byte[128];
                payload[0] = (byte)i;
                s_liveContinuations.Add(Suspended(payload));
            }

            List<EventPipeProvider> providers = new List<EventPipeProvider>
            {
                new EventPipeProvider("Microsoft-Windows-DotNETRuntime", eventLevel: EventLevel.Verbose,
                    keywords: (long)ClrTraceEventParser.Keywords.GCHeapSnapshot)
            };

            int ret = IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024, _Validate);

            // Let the continuations complete so the process shuts down cleanly.
            s_gate.SetResult(0);
            Task.WaitAll(s_liveContinuations.ToArray());
            return ret;
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            // This space intentionally left blank
        };

        private static Action _eventGeneratingAction = () =>
        {
            // Wait up to 10 seconds to receive the GCStop event for the heap snapshot.
            _gcStopReceived.WaitOne(10000);
        };

        private static Func<EventPipeEventSource, Func<int>> _Validate = (source) =>
        {
            source.Clr.GCStart += (GCStartTraceData data) =>
            {
                _seenGCStart = true;
            };

            source.Clr.TypeBulkType += (GCBulkTypeTraceData data) =>
            {
                _bulkTypeCount += data.Count;
                for (int i = 0; i < data.Count; i++)
                {
                    string name = data.Values(i).TypeName;
                    if (string.IsNullOrEmpty(name))
                    {
                        _emptyTypeNameCount++;
                    }
                    else if (name.Contains("Continuation"))
                    {
                        _sawContinuationType = true;
                    }
                }
            };

            source.Clr.GCBulkNode += delegate (GCBulkNodeTraceData data)
            {
                _bulkNodeCount += data.Count;
            };

            source.Clr.GCStop += (GCEndTraceData data) =>
            {
                _seenGCStop = true;
                _gcStopReceived.Set();
            };

            return () =>
            {
                // Keep the continuations rooted until validation runs.
                GC.KeepAlive(s_liveContinuations);

                if (_seenGCStart
                    && _seenGCStop
                    && _bulkTypeCount > 50
                    && _bulkNodeCount > 50
                    && _sawContinuationType
                    && _emptyTypeNameCount == 0)
                {
                    return 100;
                }

                Console.WriteLine("Test failed.");
                Console.WriteLine($"_seenGCStart =         {_seenGCStart}");
                Console.WriteLine($"_seenGCStop =          {_seenGCStop}");
                Console.WriteLine($"_bulkTypeCount =       {_bulkTypeCount}");
                Console.WriteLine($"_bulkNodeCount =       {_bulkNodeCount}");
                Console.WriteLine($"_sawContinuationType = {_sawContinuationType}");
                Console.WriteLine($"_emptyTypeNameCount =  {_emptyTypeNameCount} (expected 0; a non-zero count means " +
                                  "metadata-less Runtime Async continuations were logged without a type name -- dotnet/runtime#120800)");
                return -1;
            };
        };
    }
}
