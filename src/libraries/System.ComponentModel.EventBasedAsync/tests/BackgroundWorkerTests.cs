// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class BackgroundWorkerTests
{
    private static ManualResetEventSlim manualResetEvent1 = new ManualResetEventSlim(false);
    private static ManualResetEventSlim manualResetEvent2 = new ManualResetEventSlim(false);
    private static ManualResetEventSlim manualResetEvent3 = new ManualResetEventSlim(false);

    private static void Wait(int milliseconds)
    {
        System.Threading.Tasks.Task.Delay(milliseconds).Wait();
    }

    const int timeoutShort = 300;
    const int timeoutLong = 3000;

    [Fact]
    public static void BackgroundWorkerWorks()
    {
        BackgroundWorker worker = new BackgroundWorker();

        worker.DoWork += worker_DoWork;
        worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        worker.ProgressChanged += worker_ProgressChanged;
        worker.WorkerReportsProgress = true;

        manualResetEvent1.Reset();
        manualResetEvent2.Reset();

        worker.RunWorkerAsync();

        // Wait for signal from worker_ProgressChanged (which is called by worker_DoWork)
        manualResetEvent1.Wait(timeoutLong);

        // wait for singal from worker_RunWorkerCompleted
        manualResetEvent2.Wait(timeoutLong);

        Assert.False(worker.IsBusy);
    }

    static void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        // signal for first one
        manualResetEvent1.Set();
    }

    static void worker_DoWork(object sender, DoWorkEventArgs e)
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

    static void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        Assert.Equal(5, e.Result);
        Assert.False((sender as BackgroundWorker).IsBusy);
        // signal
        manualResetEvent2.Set();
    }

    [Fact]
    public static void CancelAsyncWorks()
    {
        BackgroundWorker bw = new BackgroundWorker();

        bw.DoWork += bw_DoWork;
        bw.WorkerSupportsCancellation = true;

        manualResetEvent3.Reset();

        bw.RunWorkerAsync("Message");
        bw.CancelAsync();

        manualResetEvent3.Wait();
        // there could be race condition between worker thread cancellation and completion which will set the CancellationPending to false 
        // if complete, we don't check cancellation
        if (bw.IsBusy) // not complete
        {
            if (!bw.CancellationPending)
            {
                for (int i = 0; i < 100; i++)
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

    static void bw_DoWork(object sender, DoWorkEventArgs e)
    {
        Assert.Equal("Message", e.Argument);

        var bw = sender as BackgroundWorker;
        if (bw.CancellationPending)
        {
            manualResetEvent3.Set();
            return;
        }

        // we want to wait for cancellation - wiat max 100 * timeoutShort millisec
        for (int i = 0; i < 100; i++)
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
}
