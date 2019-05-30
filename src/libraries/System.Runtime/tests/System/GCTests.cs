// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Threading;
using System.Runtime;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public static partial class GCTests
    {
        private static bool s_is32Bits = IntPtr.Size == 4; // Skip IntPtr tests on 32-bit platforms

        [Fact]
        public static void AddMemoryPressure_InvalidBytesAllocated_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bytesAllocated", () => GC.AddMemoryPressure(-1)); // Bytes allocated < 0

            if (s_is32Bits)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("pressure", () => GC.AddMemoryPressure((long)int.MaxValue + 1)); // Bytes allocated > int.MaxValue on 32 bit platforms
            }
        }

        [Fact]
        public static void Collect_Int()
        {
            for (int i = 0; i < GC.MaxGeneration + 10; i++)
            {
                GC.Collect(i);
            }
        }

        [Fact]
        public static void Collect_Int_NegativeGeneration_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("generation", () => GC.Collect(-1)); // Generation < 0
        }

        [Theory]
        [InlineData(GCCollectionMode.Default)]
        [InlineData(GCCollectionMode.Forced)]
        public static void Collect_Int_GCCollectionMode(GCCollectionMode mode)
        {
            for (int gen = 0; gen <= 2; gen++)
            {
                var b = new byte[1024 * 1024 * 10];
                int oldCollectionCount = GC.CollectionCount(gen);
                b = null;

                GC.Collect(gen, mode);

                Assert.True(GC.CollectionCount(gen) > oldCollectionCount);
            }
        }

        [Fact]
        public static void Collect_NegativeGenerationCount_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("generation", () => GC.Collect(-1, GCCollectionMode.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("generation", () => GC.Collect(-1, GCCollectionMode.Default, false));
        }

        [Theory]
        [InlineData(GCCollectionMode.Default - 1)]
        [InlineData(GCCollectionMode.Optimized + 1)]
        public static void Collection_InvalidCollectionMode_ThrowsArgumentOutOfRangeException(GCCollectionMode mode)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("mode", null, () => GC.Collect(2, mode));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("mode", null, () => GC.Collect(2, mode, false)); 
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public static void Collect_CallsFinalizer()
        {
            FinalizerTest.Run();
        }

        private class FinalizerTest
        {
            [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            private static void MakeAndDropTest()
            {
                new TestObject();
            }

            public static void Run()
            {
                MakeAndDropTest();

                GC.Collect();

                // Make sure Finalize() is called
                GC.WaitForPendingFinalizers();

                Assert.True(TestObject.Finalized);
            }

            private class TestObject
            {
                public static bool Finalized { get; private set; }

                ~TestObject()
                {
                    Finalized = true;
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public static void KeepAlive()
        {
            KeepAliveTest.Run();
        }

        private class KeepAliveTest
        {
            [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            private static void MakeAndDropDNKA()
            {
                new DoNotKeepAliveObject();
            }

            public static void Run()
            {
                var keepAlive = new KeepAliveObject();

                MakeAndDropDNKA();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                Assert.True(DoNotKeepAliveObject.Finalized);
                Assert.False(KeepAliveObject.Finalized);

                GC.KeepAlive(keepAlive);
            }

            private class KeepAliveObject
            {
                public static bool Finalized { get; private set; }

                ~KeepAliveObject()
                {
                    Finalized = true;
                }
            }

            private class DoNotKeepAliveObject
            {
                public static bool Finalized { get; private set; }

                ~DoNotKeepAliveObject()
                {
                    Finalized = true;
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public static void KeepAlive_Null()
        {
            KeepAliveNullTest.Run();
        }

        private class KeepAliveNullTest
        {
            [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            private static void MakeAndNull()
            {
                var obj = new TestObject();
                obj = null;
            }

            public static void Run()
            {
                MakeAndNull();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                Assert.True(TestObject.Finalized);
            }

            private class TestObject
            {
                public static bool Finalized { get; private set; }

                ~TestObject()
                {
                    Finalized = true;
                }
            }
        }

        [Fact]
        public static void KeepAlive_Recursive()
        {
            KeepAliveRecursiveTest.Run();
        }

        private class KeepAliveRecursiveTest
        {
            public static void Run()
            {
                int recursionCount = 0;
                RunWorker(new TestObject(), ref recursionCount);
            }

            private static void RunWorker(object obj, ref int recursionCount)
            {
                if (recursionCount++ == 10)
                    return;

                GC.Collect();
                GC.WaitForPendingFinalizers();

                RunWorker(obj, ref recursionCount);

                Assert.False(TestObject.Finalized);
                GC.KeepAlive(obj);
            }

            private class TestObject
            {
                public static bool Finalized { get; private set; }

                ~TestObject()
                {
                    Finalized = true;
                }
            }
        }

        [Fact]
        public static void SuppressFinalizer()
        {
            SuppressFinalizerTest.Run();
        }

        private class SuppressFinalizerTest
        {
            public static void Run()
            {
                var obj = new TestObject();
                GC.SuppressFinalize(obj);

                obj = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Assert.False(TestObject.Finalized);
            }

            private class TestObject
            {
                public static bool Finalized { get; private set; }

                ~TestObject()
                {
                    Finalized = true;
                }
            }
        }

        [Fact]
        public static void SuppressFinalizer_NullObject_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("obj", () => GC.SuppressFinalize(null)); // Obj is null
        }

        [Fact]
        public static void ReRegisterForFinalize()
        {
            ReRegisterForFinalizeTest.Run();
        }

        [Fact]
        public static void ReRegisterFoFinalize_NullObject_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("obj", () => GC.ReRegisterForFinalize(null)); // Obj is null
        }

        private class ReRegisterForFinalizeTest
        {
            public static void Run()
            {
                TestObject.Finalized = false;
                CreateObject();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                Assert.True(TestObject.Finalized);
            }

            private static void CreateObject()
            {
                using (var obj = new TestObject())
                {
                    GC.SuppressFinalize(obj);
                }
            }

            private class TestObject : IDisposable
            {
                public static bool Finalized { get; set; }

                ~TestObject()
                {
                    Finalized = true;
                }

                public void Dispose()
                {
                    GC.ReRegisterForFinalize(this);
                }
            }
        }

        [Fact]
        public static void CollectionCount_NegativeGeneration_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("generation", () => GC.CollectionCount(-1)); // Generation < 0
        }

        [Fact]
        public static void RemoveMemoryPressure_InvalidBytesAllocated_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bytesAllocated", () => GC.RemoveMemoryPressure(-1)); // Bytes allocated < 0

            if (s_is32Bits)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("bytesAllocated", () => GC.RemoveMemoryPressure((long)int.MaxValue + 1)); // Bytes allocated > int.MaxValue on 32 bit platforms
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public static void GetTotalMemoryTest_ForceCollection()
        {
            // We don't test GetTotalMemory(false) at all because a collection
            // could still occur even if not due to the GetTotalMemory call,
            // and as such there's no way to validate the behavior.  We also
            // don't verify a tighter bound for the result of GetTotalMemory
            // because collections could cause significant fluctuations.

            GC.Collect();

            int gen0 = GC.CollectionCount(0);
            int gen1 = GC.CollectionCount(1);
            int gen2 = GC.CollectionCount(2);

            Assert.InRange(GC.GetTotalMemory(true), 1, long.MaxValue);

            Assert.InRange(GC.CollectionCount(0), gen0 + 1, int.MaxValue);
            Assert.InRange(GC.CollectionCount(1), gen1 + 1, int.MaxValue);
            Assert.InRange(GC.CollectionCount(2), gen2 + 1, int.MaxValue);
        }

        [Fact]
        public static void GetGeneration()
        {
            // We don't test a tighter bound on GetGeneration as objects
            // can actually get demoted or stay in the same generation
            // across collections.

            GC.Collect();
            var obj = new object();

            for (int i = 0; i <= GC.MaxGeneration + 1; i++)
            {
                Assert.InRange(GC.GetGeneration(obj), 0, GC.MaxGeneration);
                GC.Collect();
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        [InlineData(GCLargeObjectHeapCompactionMode.CompactOnce)]
        [InlineData(GCLargeObjectHeapCompactionMode.Default)]
        public static void LargeObjectHeapCompactionModeRoundTrips(GCLargeObjectHeapCompactionMode value)
        {
            GCLargeObjectHeapCompactionMode orig = GCSettings.LargeObjectHeapCompactionMode;
            try
            {
                GCSettings.LargeObjectHeapCompactionMode = value;
                Assert.Equal(value, GCSettings.LargeObjectHeapCompactionMode);
            }
            finally
            {
                GCSettings.LargeObjectHeapCompactionMode = orig;
                Assert.Equal(orig, GCSettings.LargeObjectHeapCompactionMode);
            }
        }

        [Theory]
        [InlineData(GCLatencyMode.Batch)]
        [InlineData(GCLatencyMode.Interactive)]
        public static void LatencyRoundtrips(GCLatencyMode value)
        {
            GCLatencyMode orig = GCSettings.LatencyMode;
            try
            {
                GCSettings.LatencyMode = value;
                Assert.Equal(value, GCSettings.LatencyMode);
            }
            finally
            {
                GCSettings.LatencyMode = orig;
                Assert.Equal(orig, GCSettings.LatencyMode);
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)] //Concurrent GC is not enabled on Unix. Recombine to TestLatencyRoundTrips once addressed.
        [InlineData(GCLatencyMode.LowLatency)]
        [InlineData(GCLatencyMode.SustainedLowLatency)]
        public static void LatencyRoundtrips_LowLatency(GCLatencyMode value) => LatencyRoundtrips(value);
    }

    public class GCExtendedTests    {
        private const int TimeoutMilliseconds = 10 * 30 * 1000; //if full GC is triggered it may take a while

        /// <summary>
        /// NoGC regions will be automatically exited if more than the requested budget
        /// is allocated while still in the region. In order to avoid this, the budget is set
        /// to be higher than what the test should be allocating. When running on CoreCLR/DesktopCLR,
        /// these tests generally do not allocate because they are implemented as fcalls into the runtime
        /// itself, but the CoreRT runtime is written in mostly managed code and tends to allocate more.
        ///
        /// This budget should be high enough to avoid exiting no-gc regions when doing normal unit
        /// tests, regardless of the runtime.
        /// </summary>
        private const int NoGCRequestedBudget = 8192;

        [Fact]
        [OuterLoop]
        public static void GetGeneration_WeakReference()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke(() =>
                {

                    Func<WeakReference> getweakref = delegate ()
                    {
                        Version myobj = new Version();
                        var wkref = new WeakReference(myobj);

                        Assert.True(GC.TryStartNoGCRegion(NoGCRequestedBudget));
                        Assert.True(GC.GetGeneration(wkref) >= 0);
                        Assert.Equal(GC.GetGeneration(wkref), GC.GetGeneration(myobj));
                        GC.EndNoGCRegion();

                        myobj = null;
                        return wkref;
                    };

                    WeakReference weakref = getweakref();
                    Assert.True(weakref != null);
#if !DEBUG
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                    Assert.Throws<ArgumentNullException>(() => GC.GetGeneration(weakref));
#endif
                    return RemoteExecutor.SuccessExitCode;
                }, options).Dispose();

        }

        [Fact]
        public static void GCNotificationNegTests()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(-1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(100, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(-1, 100));

            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(10, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(-1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(100, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(10, 100));


            Assert.Throws<ArgumentOutOfRangeException>(() => GC.WaitForFullGCApproach(-2));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.WaitForFullGCComplete(-2));
        }

        [Theory]
        [InlineData(true, -1)]
        [InlineData(false, -1)]
        [InlineData(true, 0)]
        [InlineData(false, 0)]
        [InlineData(true, 100)]
        [InlineData(false, 100)]
        [InlineData(true, int.MaxValue)]
        [InlineData(false, int.MaxValue)]
        [OuterLoop]
        public static void GCNotificationTests(bool approach, int timeout)
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke((approachString, timeoutString) =>
                {
                    TestWait(bool.Parse(approachString), int.Parse(timeoutString));
                    return RemoteExecutor.SuccessExitCode;
                }, approach.ToString(), timeout.ToString(), options).Dispose();
        }

        [Fact]
        [OuterLoop]
        public static void TryStartNoGCRegion_EndNoGCRegion_ThrowsInvalidOperationException()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke(() =>
                {
                    Assert.Throws<InvalidOperationException>(() => GC.EndNoGCRegion());
                    return RemoteExecutor.SuccessExitCode;
                }, options).Dispose();
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void AllocateALot()
        {
            for (int i = 0; i < 10000; i++)
            {
                var array = new long[NoGCRequestedBudget];
                GC.KeepAlive(array);
            }
        }

        [Fact]
        [OuterLoop]
        public static void TryStartNoGCRegion_ExitThroughAllocation()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke(() =>
                {
                    Assert.True(GC.TryStartNoGCRegion(1024));

                    AllocateALot();

                    // at this point, the GC should have booted us out of the no GC region
                    // since we allocated too much.
                    Assert.Throws<InvalidOperationException>(() => GC.EndNoGCRegion());
                    return RemoteExecutor.SuccessExitCode;
                }, options).Dispose();
        }

        [Fact]
        [OuterLoop]
        public static void TryStartNoGCRegion_StartWhileInNoGCRegion()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke(() =>
            {
                Assert.True(GC.TryStartNoGCRegion(NoGCRequestedBudget));
                Assert.Throws<InvalidOperationException>(() => GC.TryStartNoGCRegion(NoGCRequestedBudget));

                Assert.Throws<InvalidOperationException>(() => GC.EndNoGCRegion());

                return RemoteExecutor.SuccessExitCode;
            }, options).Dispose();
        }

        [Fact]
        [OuterLoop]
        public static void TryStartNoGCRegion_StartWhileInNoGCRegion_BlockingCollection()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke(() =>
            {
                Assert.True(GC.TryStartNoGCRegion(NoGCRequestedBudget, true));
                Assert.Throws<InvalidOperationException>(() => GC.TryStartNoGCRegion(NoGCRequestedBudget, true));

                Assert.Throws<InvalidOperationException>(() => GC.EndNoGCRegion());

                return RemoteExecutor.SuccessExitCode;
            }, options).Dispose();
        }

        [Fact]
        [OuterLoop]
        public static void TryStartNoGCRegion_StartWhileInNoGCRegion_LargeObjectHeapSize()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke(() =>
            {
                Assert.True(GC.TryStartNoGCRegion(NoGCRequestedBudget, NoGCRequestedBudget));
                Assert.Throws<InvalidOperationException>(() => GC.TryStartNoGCRegion(NoGCRequestedBudget, NoGCRequestedBudget));

                Assert.Throws<InvalidOperationException>(() => GC.EndNoGCRegion());

                return RemoteExecutor.SuccessExitCode;
            }, options).Dispose();
        }

        [Fact]
        [OuterLoop]
        public static void TryStartNoGCRegion_StartWhileInNoGCRegion_BlockingCollectionAndLOH()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke(() =>
            {
                Assert.True(GC.TryStartNoGCRegion(NoGCRequestedBudget, NoGCRequestedBudget, true));
                Assert.Throws<InvalidOperationException>(() => GC.TryStartNoGCRegion(NoGCRequestedBudget, NoGCRequestedBudget, true));

                Assert.Throws<InvalidOperationException>(() => GC.EndNoGCRegion());

                return RemoteExecutor.SuccessExitCode;
            }, options).Dispose();
        }

        [Fact]
        [OuterLoop]
        public static void TryStartNoGCRegion_SettingLatencyMode_ThrowsInvalidOperationException()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke(() =>
            {
                // The budget for this test is 4mb, because the act of throwing an exception with a message
                // contained in a resource file has to potential to allocate a lot on CoreRT. In particular, when compiling
                // in multi-file mode, this will trigger a resource lookup in System.Private.CoreLib.
                //
                // In addition to this, the Assert.Throws xunit combinator tends to also allocate a lot.
                Assert.True(GC.TryStartNoGCRegion(4000 * 1024, true));
                Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);
                Assert.Throws<InvalidOperationException>(() => GCSettings.LatencyMode = GCLatencyMode.LowLatency);

                GC.EndNoGCRegion();

                return RemoteExecutor.SuccessExitCode;
            }, options).Dispose();
        }

        [Fact]
        [OuterLoop]
        public static void TryStartNoGCRegion_SOHSize()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke(() =>
                {

                    Assert.True(GC.TryStartNoGCRegion(NoGCRequestedBudget));
                    Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);
                    GC.EndNoGCRegion();

                    return RemoteExecutor.SuccessExitCode;

                }, options).Dispose();
        }

        [Fact]
        [OuterLoop]
        public static void TryStartNoGCRegion_SOHSize_BlockingCollection()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke(() =>
            {
                Assert.True(GC.TryStartNoGCRegion(NoGCRequestedBudget, true));
                Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);
                GC.EndNoGCRegion();

                return RemoteExecutor.SuccessExitCode;

            }, options).Dispose();
        }

        [Fact]
        [OuterLoop]
        public static void TryStartNoGCRegion_SOHSize_LOHSize()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke(() =>
            {
                Assert.True(GC.TryStartNoGCRegion(NoGCRequestedBudget, NoGCRequestedBudget));
                Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);
                GC.EndNoGCRegion();

                return RemoteExecutor.SuccessExitCode;

            }, options).Dispose();
        }

        [Fact]
        [OuterLoop]
        public static void TryStartNoGCRegion_SOHSize_LOHSize_BlockingCollection()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke(() =>
            {
                Assert.True(GC.TryStartNoGCRegion(NoGCRequestedBudget, NoGCRequestedBudget, true));
                Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);
                GC.EndNoGCRegion();

                return RemoteExecutor.SuccessExitCode;

            }, options).Dispose();
        }

        [Theory]
        [OuterLoop]
        [InlineData(0)]
        [InlineData(-1)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Difference in behavior, full framework doesn't throw, fixed in .NET Core")]
        public static void TryStartNoGCRegion_TotalSizeOutOfRange(long size)
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke(sizeString =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("totalSize", () => GC.TryStartNoGCRegion(long.Parse(sizeString)));
                return RemoteExecutor.SuccessExitCode;
            }, size.ToString(), options).Dispose();
        }

        [Theory]
        [OuterLoop]
        [InlineData(0)]                   // invalid because lohSize ==
        [InlineData(-1)]                  // invalid because lohSize < 0
        [InlineData(1152921504606846976)] // invalid because lohSize > totalSize
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Difference in behavior, full framework doesn't throw, fixed in .NET Core")]
        public static void TryStartNoGCRegion_LOHSizeInvalid(long size)
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteExecutor.Invoke(sizeString =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("lohSize", () => GC.TryStartNoGCRegion(1024, long.Parse(sizeString)));
                return RemoteExecutor.SuccessExitCode;
            }, size.ToString(), options).Dispose();
        }

        public static void TestWait(bool approach, int timeout)
        {
            GCNotificationStatus result = GCNotificationStatus.Failed;
            Thread cancelProc = null;

            // Since we need to test an infinite (or very large) wait but the API won't return, spawn off a thread which
            // will cancel the wait after a few seconds
            //
            bool cancelTimeout = (timeout == -1) || (timeout > 10000);

            GC.RegisterForFullGCNotification(20, 20);

            try
            {
                if (cancelTimeout)
                {
                    cancelProc = new Thread(new ThreadStart(CancelProc));
                    cancelProc.Start();
                }

                if (approach)
                    result = GC.WaitForFullGCApproach(timeout);
                else
                    result = GC.WaitForFullGCComplete(timeout);
            }
            catch (Exception e)
            {
                Assert.True(false, $"({approach}, {timeout}) Error - Unexpected exception received: {e.ToString()}");
            }
            finally
            {
                if (cancelProc != null)
                    cancelProc.Join();
            }

            if (cancelTimeout)
            {
                Assert.True(result == GCNotificationStatus.Canceled, $"({approach}, {timeout}) Error - WaitForFullGCApproach result not Cancelled");
            }
            else
            {
                Assert.True(result == GCNotificationStatus.Timeout, $"({approach}, {timeout}) Error - WaitForFullGCApproach result not Timeout");
            }
        }

        public static void CancelProc()
        {
            Thread.Sleep(500);
            GC.CancelFullGCNotification();
        }
    }
}
