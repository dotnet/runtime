// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.ComponentModel
{
    public class BackgroundWorker : Component
    {
        // Private instance members
        private bool _canCancelWorker;
        private bool _workerReportsProgress;
        private bool _cancellationPending;
        private bool _isRunning;
        private AsyncOperation? _asyncOperation;
        private readonly SendOrPostCallback _operationCompleted;
        private readonly SendOrPostCallback _progressReporter;

        public BackgroundWorker()
        {
            _operationCompleted = new SendOrPostCallback(AsyncOperationCompleted!);
            _progressReporter = new SendOrPostCallback(ProgressReporter!);
        }

        private void AsyncOperationCompleted(object arg)
        {
            _isRunning = false;
            _cancellationPending = false;
            OnRunWorkerCompleted((RunWorkerCompletedEventArgs)arg);
        }

        public bool CancellationPending
        {
            get
            {
                return _cancellationPending;
            }
        }

        public void CancelAsync()
        {
            if (!WorkerSupportsCancellation)
            {
                throw new InvalidOperationException(SR.BackgroundWorker_WorkerDoesntSupportCancellation);
            }

            _cancellationPending = true;
        }

        public event DoWorkEventHandler? DoWork;

        public bool IsBusy
        {
            get
            {
                return _isRunning;
            }
        }

        protected virtual void OnDoWork(DoWorkEventArgs e)
        {
            DoWork?.Invoke(this, e);
        }

        protected virtual void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e)
        {
            RunWorkerCompleted?.Invoke(this, e);
        }

        protected virtual void OnProgressChanged(ProgressChangedEventArgs e)
        {
            ProgressChanged?.Invoke(this, e);
        }

        public event ProgressChangedEventHandler? ProgressChanged;

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
        public void ReportProgress(int percentProgress, object? userState)
        {
            if (!WorkerReportsProgress)
            {
                throw new InvalidOperationException(SR.BackgroundWorker_WorkerDoesntReportProgress);
            }

            ProgressChangedEventArgs args = new ProgressChangedEventArgs(percentProgress, userState);

            if (_asyncOperation != null)
            {
                _asyncOperation.Post(_progressReporter, args);
            }
            else
            {
                _progressReporter(args);
            }
        }

        public void RunWorkerAsync()
        {
            RunWorkerAsync(null);
        }

        public void RunWorkerAsync(object? argument)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException(SR.BackgroundWorker_WorkerAlreadyRunning);
            }

            _isRunning = true;
            _cancellationPending = false;

            _asyncOperation = AsyncOperationManager.CreateOperation(null);
            Task.Factory.StartNew(
                        WorkerThreadStart,
                        argument,
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default
                    );
        }

        public event RunWorkerCompletedEventHandler? RunWorkerCompleted;

        public bool WorkerReportsProgress
        {
            get
            {
                return _workerReportsProgress;
            }

            set
            {
                _workerReportsProgress = value;
            }
        }

        public bool WorkerSupportsCancellation
        {
            get
            {
                return _canCancelWorker;
            }

            set
            {
                _canCancelWorker = value;
            }
        }

        private void WorkerThreadStart(object? argument)
        {
            Debug.Assert(_asyncOperation != null, "_asyncOperation not initialized");

            object? workerResult = null;
            Exception? error = null;
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

            var e = new RunWorkerCompletedEventArgs(workerResult, error, cancelled);
            _asyncOperation.PostOperationCompleted(_operationCompleted, e);
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
