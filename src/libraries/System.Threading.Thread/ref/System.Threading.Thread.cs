// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public sealed partial class LocalDataStoreSlot
    {
        private LocalDataStoreSlot() { }
        ~LocalDataStoreSlot() { }
    }
}
namespace System.Threading
{
    public enum ApartmentState
    {
        MTA = 1,
        STA = 0,
        Unknown = 2,
    }
    public sealed partial class CompressedStack : System.Runtime.Serialization.ISerializable
    {
        private CompressedStack() { }
        public static System.Threading.CompressedStack Capture() { throw null; }
        public System.Threading.CompressedStack CreateCopy() { throw null; }
        public static System.Threading.CompressedStack GetCompressedStack() { throw null; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public static void Run(System.Threading.CompressedStack compressedStack, System.Threading.ContextCallback callback, object state) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public delegate void ParameterizedThreadStart(object obj);
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class Thread : System.Runtime.ConstrainedExecution.CriticalFinalizerObject
    {
        public Thread(System.Threading.ParameterizedThreadStart start) { }
        public Thread(System.Threading.ParameterizedThreadStart start, int maxStackSize) { }
        public Thread(System.Threading.ThreadStart start) { }
        public Thread(System.Threading.ThreadStart start, int maxStackSize) { }
        [System.ObsoleteAttribute("The ApartmentState property has been deprecated.  Use GetApartmentState, SetApartmentState or TrySetApartmentState instead.", false)]
        public System.Threading.ApartmentState ApartmentState { get { throw null; } set { } }
        public System.Globalization.CultureInfo CurrentCulture { get { throw null; } set { } }
        public static System.Security.Principal.IPrincipal CurrentPrincipal { get { throw null; } set { } }
        public static System.Threading.Thread CurrentThread { get { throw null; } }
        public System.Globalization.CultureInfo CurrentUICulture { get { throw null; } set { } }
        public System.Threading.ExecutionContext ExecutionContext { get { throw null; } }
        public bool IsAlive { get { throw null; } }
        public bool IsBackground { get { throw null; } set { } }
        public bool IsThreadPoolThread { get { throw null; } }
        public int ManagedThreadId { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public System.Threading.ThreadPriority Priority { get { throw null; } set { } }
        public System.Threading.ThreadState ThreadState { get { throw null; } }
        public void Abort() { }
        public void Abort(object stateInfo) { }
        public static System.LocalDataStoreSlot AllocateDataSlot() { throw null; }
        public static System.LocalDataStoreSlot AllocateNamedDataSlot(string name) { throw null; }
        public static void BeginCriticalRegion() { }
        public static void BeginThreadAffinity() { }
        public void DisableComObjectEagerCleanup() { }
        public static void EndCriticalRegion() { }
        public static void EndThreadAffinity() { }
        ~Thread() { }
        public static void FreeNamedDataSlot(string name) { }
        public System.Threading.ApartmentState GetApartmentState() { throw null; }
        [System.ObsoleteAttribute("Thread.GetCompressedStack is no longer supported. Please use the System.Threading.CompressedStack class")]
        public System.Threading.CompressedStack GetCompressedStack() { throw null; }
        public static object GetData(System.LocalDataStoreSlot slot) { throw null; }
        public static System.AppDomain GetDomain() { throw null; }
        public static int GetDomainID() { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.LocalDataStoreSlot GetNamedDataSlot(string name) { throw null; }
        public void Interrupt() { }
        public void Join() { }
        public bool Join(int millisecondsTimeout) { throw null; }
        public bool Join(System.TimeSpan timeout) { throw null; }
        public static void MemoryBarrier() { }
        public static void ResetAbort() { }
        [System.ObsoleteAttribute("Thread.Resume has been deprecated.  Please use other classes in System.Threading, such as Monitor, Mutex, Event, and Semaphore, to synchronize Threads or protect resources.  http://go.microsoft.com/fwlink/?linkid=14202", false)]
        public void Resume() { }
        public void SetApartmentState(System.Threading.ApartmentState state) { }
        [System.ObsoleteAttribute("Thread.SetCompressedStack is no longer supported. Please use the System.Threading.CompressedStack class")]
        public void SetCompressedStack(System.Threading.CompressedStack stack) { }
        public static void SetData(System.LocalDataStoreSlot slot, object data) { }
        public static void Sleep(int millisecondsTimeout) { }
        public static void Sleep(System.TimeSpan timeout) { }
        public static void SpinWait(int iterations) { }
        public void Start() { }
        public void Start(object parameter) { }
        [System.ObsoleteAttribute("Thread.Suspend has been deprecated.  Please use other classes in System.Threading, such as Monitor, Mutex, Event, and Semaphore, to synchronize Threads or protect resources.  http://go.microsoft.com/fwlink/?linkid=14202", false)]
        public void Suspend() { }
        public bool TrySetApartmentState(System.Threading.ApartmentState state) { throw null; }
        public static byte VolatileRead(ref byte address) { throw null; }
        public static double VolatileRead(ref double address) { throw null; }
        public static short VolatileRead(ref short address) { throw null; }
        public static int VolatileRead(ref int address) { throw null; }
        public static long VolatileRead(ref long address) { throw null; }
        public static System.IntPtr VolatileRead(ref System.IntPtr address) { throw null; }
        public static object VolatileRead(ref object address) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte VolatileRead(ref sbyte address) { throw null; }
        public static float VolatileRead(ref float address) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort VolatileRead(ref ushort address) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint VolatileRead(ref uint address) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong VolatileRead(ref ulong address) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UIntPtr VolatileRead(ref System.UIntPtr address) { throw null; }
        public static void VolatileWrite(ref byte address, byte value) { }
        public static void VolatileWrite(ref double address, double value) { }
        public static void VolatileWrite(ref short address, short value) { }
        public static void VolatileWrite(ref int address, int value) { }
        public static void VolatileWrite(ref long address, long value) { }
        public static void VolatileWrite(ref System.IntPtr address, System.IntPtr value) { }
        public static void VolatileWrite(ref object address, object value) { }
        [System.CLSCompliantAttribute(false)]
        public static void VolatileWrite(ref sbyte address, sbyte value) { }
        public static void VolatileWrite(ref float address, float value) { }
        [System.CLSCompliantAttribute(false)]
        public static void VolatileWrite(ref ushort address, ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public static void VolatileWrite(ref uint address, uint value) { }
        [System.CLSCompliantAttribute(false)]
        public static void VolatileWrite(ref ulong address, ulong value) { }
        [System.CLSCompliantAttribute(false)]
        public static void VolatileWrite(ref System.UIntPtr address, System.UIntPtr value) { }
        public static bool Yield() { throw null; }
    }
    public sealed partial class ThreadAbortException : System.SystemException
    {
        private ThreadAbortException() { }
        public object ExceptionState { get { throw null; } }
    }
    public partial class ThreadExceptionEventArgs : System.EventArgs
    {
        public ThreadExceptionEventArgs(System.Exception t) { }
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
    public enum ThreadPriority
    {
        AboveNormal = 3,
        BelowNormal = 1,
        Highest = 4,
        Lowest = 0,
        Normal = 2,
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
