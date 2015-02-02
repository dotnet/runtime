// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.ComponentModel
{
    public class BackgroundWorker : IDisposable
    {
        // Private instance members
        private bool canCancelWorker = false;
        private bool workerReportsProgress = false;
        private bool cancellationPending = false;
        private bool isRunning = false;
        private AsyncOperation asyncOperation = null;
        private readonly SendOrPostCallback operationCompleted;
        private readonly SendOrPostCallback progressReporter;

        public BackgroundWorker()
        {
            operationCompleted = new SendOrPostCallback(AsyncOperationCompleted);
            progressReporter = new SendOrPostCallback(ProgressReporter);
        }

        private void AsyncOperationCompleted(object arg)
        {
            isRunning = false;
            cancellationPending = false;
            OnRunWorkerCompleted((RunWorkerCompletedEventArgs)arg);
        }

        public bool CancellationPending
        {
            get { return cancellationPending; }
        }

        public void CancelAsync()
        {
            if (!WorkerSupportsCancellation)
            {
                throw new InvalidOperationException(SR.BackgroundWorker_WorkerDoesntSupportCancellation);
            }

            cancellationPending = true;
        }

        public event DoWorkEventHandler DoWork;

        public bool IsBusy
        {
            get { return isRunning; }
        }

        protected virtual void OnDoWork(DoWorkEventArgs e)
        {
            DoWorkEventHandler handler = DoWork;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e)
        {
            RunWorkerCompletedEventHandler handler = RunWorkerCompleted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnProgressChanged(ProgressChangedEventArgs e)
        {
            ProgressChangedEventHandler handler = ProgressChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event ProgressChangedEventHandler ProgressChanged;

        // Gets invoked through the AsyncOperation on the proper thread. 
        private void ProgressReporter(object arg)
        {
            OnProgressChanged((ProgressChangedEventArgs)arg);
        }

        // Cause progress update to be posted through current AsyncOperation.
        public void ReportProgress(int percentProgress)
        {
            ReportProgress(percentProgress, null);
        }

        // Cause progress update to be posted through current AsyncOperation.
        public void ReportProgress(int percentProgress, object userState)
        {
            if (!WorkerReportsProgress)
            {
                throw new InvalidOperationException(SR.BackgroundWorker_WorkerDoesntReportProgress);
            }

            ProgressChangedEventArgs args = new ProgressChangedEventArgs(percentProgress, userState);

            if (asyncOperation != null)
            {
                asyncOperation.Post(progressReporter, args);
            }
            else
            {
                progressReporter(args);
            }
        }

        public void RunWorkerAsync()
        {
            RunWorkerAsync(null);
        }

        public void RunWorkerAsync(object argument)
        {
            if (isRunning)
            {
                throw new InvalidOperationException(SR.BackgroundWorker_WorkerAlreadyRunning);
            }

            isRunning = true;
            cancellationPending = false;

            asyncOperation = AsyncOperationManager.CreateOperation(null);
            Task.Factory.StartNew(
                        (arg) => WorkerThreadStart(arg),
                        argument,
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default
                    );
        }

        public event RunWorkerCompletedEventHandler RunWorkerCompleted;

        public bool WorkerReportsProgress
        {
            get { return workerReportsProgress; }
            set { workerReportsProgress = value; }
        }

        public bool WorkerSupportsCancellation
        {
            get { return canCancelWorker; }
            set { canCancelWorker = value; }
        }

        private void WorkerThreadStart(object argument)
        {
            object workerResult = null;
            Exception error = null;
            bool cancelled = false;

            try
            {
                DoWorkEventArgs doWorkArgs = new DoWorkEventArgs(argument);
                OnDoWork(doWorkArgs);
                if (doWorkArgs.Cancel)
                {
                    cancelled = true;
                }
                else
                {
                    workerResult = doWorkArgs.Result;
                }
            }
            catch (Exception exception)
            {
                error = exception;
            }

            RunWorkerCompletedEventArgs e =
                new RunWorkerCompletedEventArgs(workerResult, error, cancelled);

            if (asyncOperation != null)
            {
                asyncOperation.PostOperationCompleted(operationCompleted, e);
            }
            else
            {
                operationCompleted(e);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
