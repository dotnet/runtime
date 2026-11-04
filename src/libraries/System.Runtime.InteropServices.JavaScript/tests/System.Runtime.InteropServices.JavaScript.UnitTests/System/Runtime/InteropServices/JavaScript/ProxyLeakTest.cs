// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    // Every async marshaling path eagerly allocates one half of a Task/Promise pair before it knows
    // whether the other half will ever arrive. These tests pin the JSHandle tables to their baseline
    // across all four crossings, for both completion states and for observed as well as abandoned
    // results, so that a missed release on any non-normal path shows up as a growing table.
    //
    // Chromium only: draining a proxy requires forcing a JS collection, and globalThis.gc is exposed
    // by the --expose-gc engine argument this project passes for Chrome. Elsewhere the proxies are
    // released on the engine's own schedule and the counts would not settle within a test.
    //
    // Single-threaded only: with managed threads the census also counts proxies held by other
    // threads, which drain independently of this test, and getAssemblyExports never settles.
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsChromium), nameof(PlatformDetection.IsNotMultithreadingSupported))]
    public class ProxyLeakTest : JSInteropTestBase, IAsyncLifetime
    {
        private const int Iterations = 100;

        // Drains proxies whose peer is already unreachable on either side, so that only genuinely
        // rooted proxies remain counted. WaitForPendingFinalizers is a no-op on single-threaded wasm,
        // so finalizers are driven by yielding between collections.
        private static async Task Quiesce()
        {
            for (int i = 0; i < 3; i++)
            {
                await Task.Yield();
                await JavaScriptTestHelper.Delay(1);
                JavaScriptTestHelper.ForceJsGc();
                GC.Collect();
                await Task.Yield();
                GC.Collect();
            }
        }

        // run is invoked with the number of round trips to perform.
        private static async Task AssertNoLeak(Func<int, Task> run)
        {
            // warm up the bindings so that their one-time allocations are not counted
            await run(1);
            await Quiesce();

            int[] before = JavaScriptTestHelper.GetProxyCounts();
            await run(Iterations);
            await Quiesce();
            int[] after = JavaScriptTestHelper.GetProxyCounts();

            // Only the JSHandle tables are asserted on. They are maintained by explicit release
            // calls, which is precisely where a missed release shows up, and they move only in
            // response to this test. The GCHandle table behind them is drained by the JS
            // FinalizationRegistry a few entries per turn, so it lags by an unbounded amount and
            // would make these assertions fragile rather than stricter.
            // The contract is that a round trip must not add a proxy, so this asserts on growth
            // rather than equality: an unrelated proxy draining mid-test lowers a count without
            // saying anything about the path under test, while a missed release adds Iterations.
            string census = "[csOwnedByJsHandle, csOwnedByJsvHandle, jsOwnedRegistered, jsOwnedAlive, importWrappers]"
                + $"{Environment.NewLine}before: {string.Join(", ", before)}"
                + $"{Environment.NewLine}after:  {string.Join(", ", after)}";
            Assert.True(after[0] <= before[0] && after[1] <= before[1], census);
        }

        // managed Task -> JS Promise, as the return value of a [JSExport]
        // https://github.com/dotnet/runtime/issues/132966
        [Theory]
        [InlineData(nameof(JavaScriptTestHelper.ReturnCompletedTask), "drop")]
        [InlineData(nameof(JavaScriptTestHelper.ReturnCompletedTask), "await")]
        [InlineData(nameof(JavaScriptTestHelper.ReturnCompletedTaskOfInt), "drop")]
        [InlineData(nameof(JavaScriptTestHelper.ReturnCompletedTaskOfInt), "await")]
        [InlineData(nameof(JavaScriptTestHelper.ReturnFaultedTask), "catch")]
        [InlineData(nameof(JavaScriptTestHelper.ThrowBeforeTask), "throws")]
        [InlineData(nameof(JavaScriptTestHelper.ReturnGenuinelyAsyncTask), "drop")]
        [InlineData(nameof(JavaScriptTestHelper.ReturnGenuinelyAsyncTask), "await")]
        [InlineData(nameof(JavaScriptTestHelper.ReturnDelayedTaskOfInt), "drop")]
        [InlineData(nameof(JavaScriptTestHelper.ReturnDelayedTaskOfInt), "await")]
        [InlineData(nameof(JavaScriptTestHelper.ReturnDelayedFaultedTask), "catch")]
        [InlineData(nameof(JavaScriptTestHelper.ReturnVoidSynchronously), "drop")]
        public Task JSExportReturningTask_DoesNotLeakProxies(string exportName, string mode)
            => AssertNoLeak(count => JavaScriptTestHelper.InvokeExportAsyncNTimes(exportName, count, mode));

        // a promise JS abandons while it is still pending must be released once the Task completes
        [Fact]
        public Task JSExportReturningPendingTask_ReleasesProxiesOnCompletion()
            => AssertNoLeak(async count =>
            {
                await JavaScriptTestHelper.InvokeExportAsyncNTimes(nameof(JavaScriptTestHelper.ReturnPendingTaskOfInt), count, "drop");
                JavaScriptTestHelper.CompletePendingExports();
            });

        // JS Promise -> managed Task, as the return value of a [JSImport]
        [Theory]
        [InlineData("resolved", true)]
        [InlineData("resolved", false)]
        [InlineData("delayed", true)]
        [InlineData("delayed", false)]
        [InlineData("rejected", true)]
        [InlineData("rejected", false)]
        public Task JSImportReturningPromise_DoesNotLeakProxies(string kind, bool observed)
            => AssertNoLeak(async count =>
            {
                var started = new List<Task>(count);
                for (int i = 0; i < count; i++)
                {
                    Task task = kind switch
                    {
                        "resolved" => JavaScriptTestHelper.ReturnResolvedPromise(),
                        "delayed" => JavaScriptTestHelper.sleep(1),
                        _ => JavaScriptTestHelper.Reject("intentionally orphaned"),
                    };

                    if (observed)
                    {
                        started.Add(task);
                    }
                }

                foreach (Task task in started)
                {
                    try
                    {
                        await task;
                    }
                    catch (JSException)
                    {
                    }
                }

                // give the abandoned ones a chance to settle before the census is taken
                await JavaScriptTestHelper.Delay(10);
            });

        // managed Task -> JS Promise, as an argument of a [JSImport]
        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public Task JSImportWithTaskArgument_DoesNotLeakProxies(bool completed, bool observedByJs)
            => AssertNoLeak(async count =>
            {
                var pending = new List<TaskCompletionSource>(count);
                for (int i = 0; i < count; i++)
                {
                    // a fresh source every time, so that each iteration marshals a distinct Task
                    var tcs = new TaskCompletionSource();
                    if (completed)
                    {
                        tcs.SetResult();
                    }
                    else
                    {
                        pending.Add(tcs);
                    }

                    if (observedByJs)
                    {
                        JavaScriptTestHelper.thenvoid(tcs.Task);
                    }
                    else
                    {
                        JavaScriptTestHelper.DropTask(tcs.Task);
                    }
                }

                foreach (TaskCompletionSource tcs in pending)
                {
                    tcs.SetResult();
                }

                await JavaScriptTestHelper.Delay(10);
            });

        // JS Promise -> managed Task, as an argument of a [JSExport]
        [Theory]
        [InlineData(nameof(JavaScriptTestHelper.AwaitPromiseParameter), true)]
        [InlineData(nameof(JavaScriptTestHelper.AwaitPromiseParameter), false)]
        [InlineData(nameof(JavaScriptTestHelper.IgnorePromiseParameter), true)]
        [InlineData(nameof(JavaScriptTestHelper.IgnorePromiseParameter), false)]
        public Task JSExportWithPromiseArgument_DoesNotLeakProxies(string exportName, bool settled)
            => AssertNoLeak(count => JavaScriptTestHelper.InvokeExportWithPromiseNTimes(exportName, count, settled));

        // CoreCLR only: its BindAssemblyExports marshals the failure back as a managed exception,
        // while on Mono a missing assembly trips a native assert that aborts the runtime, leaving
        // managed code nothing to catch.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime))]
        public Task FailingGetAssemblyExports_DoesNotLeakProxies()
            => AssertNoLeak(async count =>
            {
                for (int i = 0; i < count; i++)
                {
                    string result = await JavaScriptTestHelper.TryGetAssemblyExports("System.Runtime.InteropServices.JavaScript.Tests.NoSuchAssembly");
                    Assert.DoesNotContain("resolved", result);
                }
            });
    }

    // Separate from ProxyLeakTest because it counts managed PromiseHolders rather than JS proxies.
    // That table is per-context and released explicitly, so it needs neither a forced collection
    // nor a single-threaded runtime to settle.
    public class PromiseHolderLeakTest : JSInteropTestBase, IAsyncLifetime
    {
        private const int Iterations = 100;

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_PromiseHolderCount")]
        private static extern int GetPromiseHolderCount(JSFunctionBinding binding);

        private static async Task ThrowNTimes(int count)
        {
            for (int i = 0; i < count; i++)
            {
                try
                {
                    // the JS side throws instead of returning a Promise, so the eagerly created
                    // holder is never handed over
                    await JavaScriptTestHelper.ThrowBeforePromise();
                    Assert.Fail("expected the JS side to throw");
                }
                catch (JSException)
                {
                }
            }
        }

        [Fact]
        public async Task ThrowingAsyncImport_DoesNotLeakHolders()
        {
            // warm up the binding so its one-time allocations are not counted
            await ThrowNTimes(1);

            int before = GetPromiseHolderCount(null);
            await ThrowNTimes(Iterations);
            int after = GetPromiseHolderCount(null);

            Assert.True(after <= before, $"promise holders before: {before}, after: {after}");
        }
    }
}
