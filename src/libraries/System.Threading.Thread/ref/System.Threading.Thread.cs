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
    public sealed partial class Thread
    {
        public Thread(System.Threading.ParameterizedThreadStart start) { }
        public Thread(System.Threading.ThreadStart start) { }
        public static System.Threading.Thread CurrentThread { get { return default(System.Threading.Thread); } }
        public bool IsAlive { get { return default(bool); } }
        public bool IsBackground { get { return default(bool); } set { } }
        public int ManagedThreadId { get { return default(int); } }
        public string Name { get { return default(string); } set { } }
        public System.Threading.ThreadState ThreadState { get { return default(System.Threading.ThreadState); } }
        public void Join() { }
        public bool Join(int millisecondsTimeout) { return default(bool); }
        public static void Sleep(int millisecondsTimeout) { }
        public static void Sleep(System.TimeSpan timeout) { }
        public void Start() { }
        public void Start(object parameter) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public delegate void ThreadStart();
    public sealed partial class ThreadStartException : System.Exception
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
    public partial class ThreadStateException : System.Exception
    {
        public ThreadStateException() { }
        public ThreadStateException(string message) { }
        public ThreadStateException(string message, System.Exception innerException) { }
    }
}
