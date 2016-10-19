// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Threading
{
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public delegate void ParameterizedThreadStart(object obj);
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class Thread : System.Runtime.ConstrainedExecution.CriticalFinalizerObject
    {
        public Thread(System.Threading.ParameterizedThreadStart start) { }
        public Thread(System.Threading.ThreadStart start) { }
        public static System.Threading.Thread CurrentThread { get { throw null; } }
        public bool IsAlive { get { throw null; } }
        public bool IsBackground { get { throw null; } set { } }
        public int ManagedThreadId { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public System.Threading.ThreadState ThreadState { get { throw null; } }
        ~Thread() { }
        public void Join() { }
        public bool Join(int millisecondsTimeout) { throw null; }
        public static void Sleep(int millisecondsTimeout) { }
        public static void Sleep(System.TimeSpan timeout) { }
        public void Start() { }
        public void Start(object parameter) { }
    }
    public sealed partial class ThreadAbortException : System.SystemException
    {
        private ThreadAbortException() { }
        public object ExceptionState { get { throw null; } }
    }
    public partial class ThreadExceptionEventArgs : System.EventArgs
    {
        public ThreadExceptionEventArgs(System.Exception exception) { }
        public System.Exception Exception { get { throw null; } }
    }
    public delegate void ThreadExceptionEventHandler(object sender, System.Threading.ThreadExceptionEventArgs e);
    public partial class ThreadInterruptedException : System.SystemException
    {
        public ThreadInterruptedException() { }
        public ThreadInterruptedException(string message) { }
        public ThreadInterruptedException(string message, System.Exception innerException) { }
        protected ThreadInterruptedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public delegate void ThreadStart();
    public sealed partial class ThreadStartException : System.SystemException
    {
        internal ThreadStartException() { }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum ThreadState
    {
        Aborted = 256,
        AbortRequested = 128,
        Background = 4,
        Running = 0,
        Stopped = 16,
        StopRequested = 1,
        Suspended = 64,
        SuspendRequested = 2,
        Unstarted = 8,
        WaitSleepJoin = 32,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ThreadStateException : System.SystemException
    {
        public ThreadStateException() { }
        public ThreadStateException(string message) { }
        public ThreadStateException(string message, System.Exception innerException) { }
        protected ThreadStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
}
