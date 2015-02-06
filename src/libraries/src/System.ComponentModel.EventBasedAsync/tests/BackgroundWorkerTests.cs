// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.ComponentModel.EventBasedAsync
{
    public class BackgroundWorkerTests
    {
        private static void Wait(int milliseconds)
        {
            Task.Delay(milliseconds).Wait();
        }

        private const int timeoutShort = 300;
        private const int timeoutLong = 30000;

        private ManualResetEventSlim manualResetEvent1;
        private ManualResetEventSlim manualResetEvent2;

        [Fact]
        public void TestBackgroundWorkerBasic()
        {
            var orignal = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);

                BackgroundWorker worker = new BackgroundWorker();

                worker.DoWork += DoWork;
                worker.RunWorkerCompleted += WhenRunWorkerCompleted;
                worker.ProgressChanged += WhenProgressChanged;
                worker.WorkerReportsProgress = true;

                manualResetEvent1 = new ManualResetEventSlim(false);
                manualResetEvent2 = new ManualResetEventSlim(false);

                worker.RunWorkerAsync();

                // Wait for signal from WhenProgressChanged (which is called by DoWork)
                bool ret1 = manualResetEvent1.Wait(timeoutLong);
                Assert.True(ret1);

                // wait for singal from WhenRunWorkerCompleted
                bool ret2 = manualResetEvent2.Wait(timeoutLong);
                Assert.True(ret2);

                Assert.False(worker.IsBusy);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(orignal);
            }
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            Assert.True(worker.IsBusy);
            int i;
            for (i = 1; i < 5; i++)
            {
                worker.ReportProgress((int)((100.0 * i) / 10));
            }
            e.Result = i;
        }

        private void WhenProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // signal for the first one
            manualResetEvent1.Set();
        }

        private void WhenRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Assert.Equal(5, e.Result);
            Assert.False((sender as BackgroundWorker).IsBusy);
            // signal
            manualResetEvent2.Set();
        }

        private ManualResetEventSlim manualResetEvent3;

        [Fact]
        public void TestCancelAsync()
        {
            BackgroundWorker bw = new BackgroundWorker();

            bw.DoWork += DoWorkExpectCancel;
            bw.WorkerSupportsCancellation = true;

            manualResetEvent3 = new ManualResetEventSlim(false);

            bw.RunWorkerAsync("Message");
            bw.CancelAsync();

            bool ret = manualResetEvent3.Wait(timeoutLong);
            Assert.True(ret);
            // there could be race condition between worker thread cancellation and completion which will set the CancellationPending to false 
            // if it is completed already, we don't check cancellation
            if (bw.IsBusy) // not complete
            {
                if (!bw.CancellationPending)
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        Wait(timeoutShort);
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

            // we want to wait for cancellation - wiat max 100 * timeoutShort millisec
            for (int i = 0; i < 1000; i++)
            {
                Wait(timeoutShort);
                if (bw.CancellationPending)
                {
                    break;
                }
            }

            Assert.True(bw.CancellationPending, "Cancellation in Worker thread");
            // signal no matter what, even if it's not cancelled by now
            manualResetEvent3.Set();
        }

        [Fact]
        public void TestThrowExceptionInDoWork()
        {
            var bw = new TestWorker();
            bw.DoWork += DoWorkWithException;
            bw.RunWorkerAsync("Exception");
        }

        private void DoWorkWithException(object sender, DoWorkEventArgs e)
        {
            Assert.Equal("Exception", e.Argument);
            throw new TestException("Exception from DoWork");
        }

        internal class TestWorker : BackgroundWorker
        {
            protected override void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e)
            {
                // Do not use Assert.Throws<TestException>(() => e.Result);
                // because we want to check the call stack contains the event handler method
                try
                {
                    var r = e.Result;
                    Assert.True(false, "Expect TestException");
                }
                catch (Exception ex)
                {
                    var message = ex.ToString();
                    Assert.True(message.Contains("DoWorkWithException"));
                }
            }
        }
    }

    internal class TestException : Exception
    {
        public TestException(string message) :
            base(message)
        {
        }
    }
}
