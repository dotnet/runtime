// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.ComponentModel
{
    public partial class AsyncCompletedEventArgs : System.EventArgs
    {
        public AsyncCompletedEventArgs(System.Exception error, bool cancelled, object userState) { }
        public bool Cancelled { get { return default(bool); } }
        public System.Exception Error { get { return default(System.Exception); } }
        public object UserState { get { return default(object); } }
        protected void RaiseExceptionIfNecessary() { }
    }
    public delegate void AsyncCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
    public sealed partial class AsyncOperation
    {
        internal AsyncOperation() { }
        public System.Threading.SynchronizationContext SynchronizationContext { get { return default(System.Threading.SynchronizationContext); } }
        public object UserSuppliedState { get { return default(object); } }
        ~AsyncOperation() { }
        public void OperationCompleted() { }
        public void Post(System.Threading.SendOrPostCallback d, object arg) { }
        public void PostOperationCompleted(System.Threading.SendOrPostCallback d, object arg) { }
    }
    public static partial class AsyncOperationManager
    {
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        public static System.Threading.SynchronizationContext SynchronizationContext { get { return default(System.Threading.SynchronizationContext); } set { } }
        public static System.ComponentModel.AsyncOperation CreateOperation(object userSuppliedState) { return default(System.ComponentModel.AsyncOperation); }
    }
    public partial class BackgroundWorker
    {
        public BackgroundWorker() { }
        public bool CancellationPending { get { return default(bool); } }
        public bool IsBusy { get { return default(bool); } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool WorkerReportsProgress { get { return default(bool); } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool WorkerSupportsCancellation { get { return default(bool); } set { } }
        public event System.ComponentModel.DoWorkEventHandler DoWork { add { } remove { } }
        public event System.ComponentModel.ProgressChangedEventHandler ProgressChanged { add { } remove { } }
        public event System.ComponentModel.RunWorkerCompletedEventHandler RunWorkerCompleted { add { } remove { } }
        public void CancelAsync() { }
        protected virtual void OnDoWork(System.ComponentModel.DoWorkEventArgs e) { }
        protected virtual void OnProgressChanged(System.ComponentModel.ProgressChangedEventArgs e) { }
        protected virtual void OnRunWorkerCompleted(System.ComponentModel.RunWorkerCompletedEventArgs e) { }
        public void ReportProgress(int percentProgress) { }
        public void ReportProgress(int percentProgress, object userState) { }
        public void RunWorkerAsync() { }
        public void RunWorkerAsync(object argument) { }
    }
    public partial class DoWorkEventArgs : System.EventArgs
    {
        public DoWorkEventArgs(object argument) { }
        public object Argument { get { return default(object); } }
        public object Result { get { return default(object); } set { } }
    }
    public delegate void DoWorkEventHandler(object sender, System.ComponentModel.DoWorkEventArgs e);
    public partial class ProgressChangedEventArgs : System.EventArgs
    {
        public ProgressChangedEventArgs(int progressPercentage, object userState) { }
        public int ProgressPercentage { get { return default(int); } }
        public object UserState { get { return default(object); } }
    }
    public delegate void ProgressChangedEventHandler(object sender, System.ComponentModel.ProgressChangedEventArgs e);
    public partial class RunWorkerCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        public RunWorkerCompletedEventArgs(object result, System.Exception error, bool cancelled) : base(default(System.Exception), default(bool), default(object)) { }
        public object Result { get { return default(object); } }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public new object UserState { get { return default(object); } }
    }
    public delegate void RunWorkerCompletedEventHandler(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e);
}
