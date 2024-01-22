// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.ComponentModel.EventBasedAsync.Tests
{
    public class BackgroundWorkerTests
    {
        private const int TimeoutShort = 300;
        private const int TimeoutLong = 30000;

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void TestBackgroundWorkerBasic()
        {
            var original = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);

                const int expectedResult = 42;
                const int expectedReportCallsCount = 5;
                int actualReportCallsCount = 0;
                var worker = new BackgroundWorker() { WorkerReportsProgress = true };
                var progressBarrier = new Barrier(2, barrier => ++actualReportCallsCount);
                var workerCompletedEvent = new ManualResetEventSlim(false);

                worker.DoWork += (sender, e) =>
                {
                    for (int i = 0; i < expectedReportCallsCount; i++)
                    {
                        worker.ReportProgress(i);
                        progressBarrier.SignalAndWait();
                    }

                    e.Result = expectedResult;
                };
                worker.RunWorkerCompleted += (sender, e) =>
                {
                    try
                    {
                        Assert.Equal(expectedResult, (int)e.Result);
                        Assert.False(worker.IsBusy);
                    }
                    finally
                    {
                        workerCompletedEvent.Set();
                    }
                };
                worker.ProgressChanged += (sender, e) =>
                {
                    progressBarrier.SignalAndWait();
                };

                worker.RunWorkerAsync();

                // wait for signal from WhenRunWorkerCompleted
                Assert.True(workerCompletedEvent.Wait(TimeoutLong));
                Assert.False(worker.IsBusy);
                Assert.Equal(expectedReportCallsCount, actualReportCallsCount);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(original);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task RunWorkerAsync_NoOnWorkHandler_SetsResultToNull()
        {
            var tcs = new TaskCompletionSource();
            var backgroundWorker = new BackgroundWorker { WorkerReportsProgress = true };
            backgroundWorker.RunWorkerCompleted += (sender, e) =>
            {
                Assert.Null(e.Result);
                Assert.False(backgroundWorker.IsBusy);
                tcs.SetResult();
            };

            backgroundWorker.RunWorkerAsync();

            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10)); // Usually takes 100th of a sec
        }

        #region TestCancelAsync

        private ManualResetEventSlim manualResetEvent3;

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void TestCancelAsync()
        {
            BackgroundWorker bw = new BackgroundWorker();

            bw.DoWork += DoWorkExpectCancel;
            bw.WorkerSupportsCancellation = true;

            manualResetEvent3 = new ManualResetEventSlim(false);

            bw.RunWorkerAsync("Message");
            bw.CancelAsync();

            bool ret = manualResetEvent3.Wait(TimeoutLong);
            Assert.True(ret);
            // there could be race condition between worker thread cancellation and completion which will set the CancellationPending to false
            // if it is completed already, we don't check cancellation
            if (bw.IsBusy) // not complete
            {
                if (!bw.CancellationPending)
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        Wait(TimeoutShort);
                        if (bw.CancellationPending)
                        {
                            break;
                        }
                    }
                }
                // Check again
                if (bw.IsBusy)
                    Assert.True(bw.CancellationPending, "Cancellation in Main thread");
            }
        }

        private void DoWorkExpectCancel(object sender, DoWorkEventArgs e)
        {
            Assert.Equal("Message", e.Argument);

            var bw = sender as BackgroundWorker;
            if (bw.CancellationPending)
            {
                manualResetEvent3.Set();
                return;
            }

            // we want to wait for cancellation - wait max (1000 * TimeoutShort) milliseconds
            for (int i = 0; i < 1000; i++)
            {
                Wait(TimeoutShort);
                if (bw.CancellationPending)
                {
                    break;
                }
            }

            Assert.True(bw.CancellationPending, "Cancellation in Worker thread");
            // signal no matter what, even if it's not cancelled by now
            manualResetEvent3.Set();
        }

        #endregion

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void TestThrowExceptionInDoWork()
        {
            var original = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);

                const string expectedArgument = "Exception";
                const string expectedExceptionMsg = "Exception from DoWork";
                var bw = new BackgroundWorker();
                var workerCompletedEvent = new ManualResetEventSlim(false);

                bw.DoWork += (sender, e) =>
                {
                    Assert.Same(bw, sender);
                    Assert.Same(expectedArgument, e.Argument);
                    throw new TestException(expectedExceptionMsg);
                };

                bw.RunWorkerCompleted += (sender, e) =>
                {
                    try
                    {
                        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => e.Result);
                        Assert.True(ex.InnerException is TestException);
                        Assert.Equal(expectedExceptionMsg, ex.InnerException.Message);
                    }
                    finally
                    {
                        workerCompletedEvent.Set();
                    }
                };

                bw.RunWorkerAsync(expectedArgument);
                Assert.True(workerCompletedEvent.Wait(TimeoutLong), "Background work timeout");
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(original);
            }
        }

        [Fact]
        public void CtorTest()
        {
            var bw = new BackgroundWorker();
            Assert.False(bw.IsBusy);
            Assert.False(bw.WorkerReportsProgress);
            Assert.False(bw.WorkerSupportsCancellation);
            Assert.False(bw.CancellationPending);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void RunWorkerAsyncTwice()
        {
            var bw = new BackgroundWorker();
            var barrier = new Barrier(2);

            bw.DoWork += (sender, e) =>
            {
                barrier.SignalAndWait();
                barrier.SignalAndWait();
            };

            bw.RunWorkerAsync();
            barrier.SignalAndWait();
            try
            {
                Assert.True(bw.IsBusy);
                Assert.Throws<InvalidOperationException>(() => bw.RunWorkerAsync());
            }
            finally
            {
                barrier.SignalAndWait();
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void TestCancelInsideDoWork()
        {
            var original = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);

                var bw = new BackgroundWorker() { WorkerSupportsCancellation = true };
                var barrier = new Barrier(2);

                bw.DoWork += (sender, e) =>
                {
                    barrier.SignalAndWait();
                    barrier.SignalAndWait();

                    if (bw.CancellationPending)
                    {
                        e.Cancel = true;
                    }
                };

                bw.RunWorkerCompleted += (sender, e) =>
                {
                    Assert.True(e.Cancelled);
                    barrier.SignalAndWait();
                };

                bw.RunWorkerAsync();
                barrier.SignalAndWait();
                bw.CancelAsync();
                barrier.SignalAndWait();
                Assert.True(barrier.SignalAndWait(TimeoutLong), "Background work timeout");
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(original);
            }
        }

        [Fact]
        public void TestCancelAsyncWithoutCancellationSupport()
        {
            var bw = new BackgroundWorker() { WorkerSupportsCancellation = false };
            Assert.Throws<InvalidOperationException>(() => bw.CancelAsync());
        }

        [Fact]
        public void TestReportProgressSync()
        {
            var bw = new BackgroundWorker() { WorkerReportsProgress = true };
            var expectedProgress = new int[] { 1, 2, 3, 4, 5 };
            var actualProgress = new List<int>();

            bw.ProgressChanged += (sender, e) =>
            {
                actualProgress.Add(e.ProgressPercentage);
            };

            foreach (int i in expectedProgress)
            {
                bw.ReportProgress(i);
            }

            Assert.Equal(expectedProgress, actualProgress);
        }

        [Fact]
        public void ReportProgress_NoProgressHandle_Nop()
        {
            var backgroundWorker = new BackgroundWorker { WorkerReportsProgress = true };
            foreach (int i in new int[] { 1, 2, 3, 4, 5 })
            {
                backgroundWorker.ReportProgress(i);
            }
        }

        [Fact]
        public void TestReportProgressWithWorkerReportsProgressFalse()
        {
            var bw = new BackgroundWorker() { WorkerReportsProgress = false };
            Assert.Throws<InvalidOperationException>(() => bw.ReportProgress(42));
        }

        [Fact]
        public void DisposeTwiceShouldNotThrow()
        {
            var bw = new BackgroundWorker();
            bw.Dispose();
            bw.Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void TestFinalization()
        {
            // BackgroundWorker has a finalizer that exists purely for backwards compatibility
            // with existing code that may override Dispose to clean up native resources.
            // https://github.com/dotnet/corefx/pull/752

            ManualResetEventSlim mres = SetEventWhenFinalizedBackgroundWorker.CreateAndThrowAway();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.True(mres.Wait(10000));
        }

        private sealed class SetEventWhenFinalizedBackgroundWorker : BackgroundWorker
        {
            private ManualResetEventSlim _setWhenFinalized;

            internal static ManualResetEventSlim CreateAndThrowAway()
            {
                var mres = new ManualResetEventSlim();
                new SetEventWhenFinalizedBackgroundWorker() { _setWhenFinalized = mres };
                return mres;
            }

            protected override void Dispose(bool disposing)
            {
                _setWhenFinalized.Set();
            }
        }

        private static void Wait(int milliseconds)
        {
            Task.Delay(milliseconds).Wait();
        }
    }
}
