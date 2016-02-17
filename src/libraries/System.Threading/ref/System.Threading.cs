// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Threading
{
    public partial class AbandonedMutexException : System.Exception
    {
        public AbandonedMutexException() { }
        public AbandonedMutexException(int location, System.Threading.WaitHandle handle) { }
        public AbandonedMutexException(string message) { }
        public AbandonedMutexException(string message, System.Exception inner) { }
        public AbandonedMutexException(string message, System.Exception inner, int location, System.Threading.WaitHandle handle) { }
        public AbandonedMutexException(string message, int location, System.Threading.WaitHandle handle) { }
        public System.Threading.Mutex Mutex { get { return default(System.Threading.Mutex); } }
        public int MutexIndex { get { return default(int); } }
    }
    public sealed partial class AsyncLocal<T>
    {
        public AsyncLocal() { }
        [System.Security.SecurityCriticalAttribute]
        public AsyncLocal(System.Action<System.Threading.AsyncLocalValueChangedArgs<T>> valueChangedHandler) { }
        public T Value { get { return default(T); } set { } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct AsyncLocalValueChangedArgs<T>
    {
        public T CurrentValue { get { return default(T); } }
        public T PreviousValue { get { return default(T); } }
        public bool ThreadContextChanged { get { return default(bool); } }
    }
    public sealed partial class AutoResetEvent : System.Threading.EventWaitHandle
    {
        public AutoResetEvent(bool initialState) : base(default(bool), default(System.Threading.EventResetMode)) { }
    }
    public partial class Barrier : System.IDisposable
    {
        public Barrier(int participantCount) { }
        public Barrier(int participantCount, System.Action<System.Threading.Barrier> postPhaseAction) { }
        public long CurrentPhaseNumber { get { return default(long); } }
        public int ParticipantCount { get { return default(int); } }
        public int ParticipantsRemaining { get { return default(int); } }
        public long AddParticipant() { return default(long); }
        public long AddParticipants(int participantCount) { return default(long); }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void RemoveParticipant() { }
        public void RemoveParticipants(int participantCount) { }
        public void SignalAndWait() { }
        public bool SignalAndWait(int millisecondsTimeout) { return default(bool); }
        public bool SignalAndWait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { return default(bool); }
        public void SignalAndWait(System.Threading.CancellationToken cancellationToken) { }
        public bool SignalAndWait(System.TimeSpan timeout) { return default(bool); }
        public bool SignalAndWait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { return default(bool); }
    }
    public partial class BarrierPostPhaseException : System.Exception
    {
        public BarrierPostPhaseException() { }
        public BarrierPostPhaseException(System.Exception innerException) { }
        public BarrierPostPhaseException(string message) { }
        public BarrierPostPhaseException(string message, System.Exception innerException) { }
    }
    public delegate void ContextCallback(object state);
    public partial class CountdownEvent : System.IDisposable
    {
        public CountdownEvent(int initialCount) { }
        public int CurrentCount { get { return default(int); } }
        public int InitialCount { get { return default(int); } }
        public bool IsSet { get { return default(bool); } }
        public System.Threading.WaitHandle WaitHandle { get { return default(System.Threading.WaitHandle); } }
        public void AddCount() { }
        public void AddCount(int signalCount) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void Reset() { }
        public void Reset(int count) { }
        public bool Signal() { return default(bool); }
        public bool Signal(int signalCount) { return default(bool); }
        public bool TryAddCount() { return default(bool); }
        public bool TryAddCount(int signalCount) { return default(bool); }
        public void Wait() { }
        public bool Wait(int millisecondsTimeout) { return default(bool); }
        public bool Wait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { return default(bool); }
        public void Wait(System.Threading.CancellationToken cancellationToken) { }
        public bool Wait(System.TimeSpan timeout) { return default(bool); }
        public bool Wait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { return default(bool); }
    }
    public enum EventResetMode
    {
        AutoReset = 0,
        ManualReset = 1,
    }
    public partial class EventWaitHandle : System.Threading.WaitHandle
    {
        public EventWaitHandle(bool initialState, System.Threading.EventResetMode mode) { }
        [System.Security.SecurityCriticalAttribute]
        public EventWaitHandle(bool initialState, System.Threading.EventResetMode mode, string name) { }
        [System.Security.SecurityCriticalAttribute]
        public EventWaitHandle(bool initialState, System.Threading.EventResetMode mode, string name, out bool createdNew) { createdNew = default(bool); }
        [System.Security.SecurityCriticalAttribute]
        public static System.Threading.EventWaitHandle OpenExisting(string name) { return default(System.Threading.EventWaitHandle); }
        public bool Reset() { return default(bool); }
        public bool Set() { return default(bool); }
        [System.Security.SecurityCriticalAttribute]
        public static bool TryOpenExisting(string name, out System.Threading.EventWaitHandle result) { result = default(System.Threading.EventWaitHandle); return default(bool); }
    }
    public sealed partial class ExecutionContext
    {
        internal ExecutionContext() { }
        public static System.Threading.ExecutionContext Capture() { return default(System.Threading.ExecutionContext); }
        [System.Security.SecurityCriticalAttribute]
        public static void Run(System.Threading.ExecutionContext executionContext, System.Threading.ContextCallback callback, object state) { }
    }
    public static partial class Interlocked
    {
        public static int Add(ref int location1, int value) { return default(int); }
        public static long Add(ref long location1, long value) { return default(long); }
        public static double CompareExchange(ref double location1, double value, double comparand) { return default(double); }
        public static int CompareExchange(ref int location1, int value, int comparand) { return default(int); }
        public static long CompareExchange(ref long location1, long value, long comparand) { return default(long); }
        public static System.IntPtr CompareExchange(ref System.IntPtr location1, System.IntPtr value, System.IntPtr comparand) { return default(System.IntPtr); }
        public static object CompareExchange(ref object location1, object value, object comparand) { return default(object); }
        public static float CompareExchange(ref float location1, float value, float comparand) { return default(float); }
        public static T CompareExchange<T>(ref T location1, T value, T comparand) where T : class { return default(T); }
        public static int Decrement(ref int location) { return default(int); }
        public static long Decrement(ref long location) { return default(long); }
        public static double Exchange(ref double location1, double value) { return default(double); }
        public static int Exchange(ref int location1, int value) { return default(int); }
        public static long Exchange(ref long location1, long value) { return default(long); }
        public static System.IntPtr Exchange(ref System.IntPtr location1, System.IntPtr value) { return default(System.IntPtr); }
        public static object Exchange(ref object location1, object value) { return default(object); }
        public static float Exchange(ref float location1, float value) { return default(float); }
        public static T Exchange<T>(ref T location1, T value) where T : class { return default(T); }
        public static int Increment(ref int location) { return default(int); }
        public static long Increment(ref long location) { return default(long); }
        public static void MemoryBarrier() { }
        public static long Read(ref long location) { return default(long); }
    }
    public static partial class LazyInitializer
    {
        public static T EnsureInitialized<T>(ref T target) where T : class { return default(T); }
        public static T EnsureInitialized<T>(ref T target, ref bool initialized, ref object syncLock) { return default(T); }
        public static T EnsureInitialized<T>(ref T target, ref bool initialized, ref object syncLock, System.Func<T> valueFactory) { return default(T); }
        public static T EnsureInitialized<T>(ref T target, System.Func<T> valueFactory) where T : class { return default(T); }
    }
    public partial class LockRecursionException : System.Exception
    {
        public LockRecursionException() { }
        public LockRecursionException(string message) { }
        public LockRecursionException(string message, System.Exception innerException) { }
    }
    public enum LockRecursionPolicy
    {
        NoRecursion = 0,
        SupportsRecursion = 1,
    }
    public sealed partial class ManualResetEvent : System.Threading.EventWaitHandle
    {
        public ManualResetEvent(bool initialState) : base(default(bool), default(System.Threading.EventResetMode)) { }
    }
    public partial class ManualResetEventSlim : System.IDisposable
    {
        public ManualResetEventSlim() { }
        public ManualResetEventSlim(bool initialState) { }
        public ManualResetEventSlim(bool initialState, int spinCount) { }
        public bool IsSet { get { return default(bool); } }
        public int SpinCount { get { return default(int); } }
        public System.Threading.WaitHandle WaitHandle { get { return default(System.Threading.WaitHandle); } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void Reset() { }
        public void Set() { }
        public void Wait() { }
        public bool Wait(int millisecondsTimeout) { return default(bool); }
        public bool Wait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { return default(bool); }
        public void Wait(System.Threading.CancellationToken cancellationToken) { }
        public bool Wait(System.TimeSpan timeout) { return default(bool); }
        public bool Wait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { return default(bool); }
    }
    public static partial class Monitor
    {
        public static void Enter(object obj) { }
        public static void Enter(object obj, ref bool lockTaken) { }
        public static void Exit(object obj) { }
        public static bool IsEntered(object obj) { return default(bool); }
        public static void Pulse(object obj) { }
        public static void PulseAll(object obj) { }
        public static bool TryEnter(object obj) { return default(bool); }
        public static void TryEnter(object obj, ref bool lockTaken) { }
        public static bool TryEnter(object obj, int millisecondsTimeout) { return default(bool); }
        public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken) { }
        public static bool TryEnter(object obj, System.TimeSpan timeout) { return default(bool); }
        public static void TryEnter(object obj, System.TimeSpan timeout, ref bool lockTaken) { }
        public static bool Wait(object obj) { return default(bool); }
        public static bool Wait(object obj, int millisecondsTimeout) { return default(bool); }
        public static bool Wait(object obj, System.TimeSpan timeout) { return default(bool); }
    }
    public sealed partial class Mutex : System.Threading.WaitHandle
    {
        public Mutex() { }
        public Mutex(bool initiallyOwned) { }
        [System.Security.SecurityCriticalAttribute]
        public Mutex(bool initiallyOwned, string name) { }
        [System.Security.SecurityCriticalAttribute]
        public Mutex(bool initiallyOwned, string name, out bool createdNew) { createdNew = default(bool); }
        [System.Security.SecurityCriticalAttribute]
        public static System.Threading.Mutex OpenExisting(string name) { return default(System.Threading.Mutex); }
        public void ReleaseMutex() { }
        [System.Security.SecurityCriticalAttribute]
        public static bool TryOpenExisting(string name, out System.Threading.Mutex result) { result = default(System.Threading.Mutex); return default(bool); }
    }
    public partial class ReaderWriterLockSlim : System.IDisposable
    {
        public ReaderWriterLockSlim() { }
        public ReaderWriterLockSlim(System.Threading.LockRecursionPolicy recursionPolicy) { }
        public int CurrentReadCount { get { return default(int); } }
        public bool IsReadLockHeld { get { return default(bool); } }
        public bool IsUpgradeableReadLockHeld { get { return default(bool); } }
        public bool IsWriteLockHeld { get { return default(bool); } }
        public System.Threading.LockRecursionPolicy RecursionPolicy { get { return default(System.Threading.LockRecursionPolicy); } }
        public int RecursiveReadCount { get { return default(int); } }
        public int RecursiveUpgradeCount { get { return default(int); } }
        public int RecursiveWriteCount { get { return default(int); } }
        public int WaitingReadCount { get { return default(int); } }
        public int WaitingUpgradeCount { get { return default(int); } }
        public int WaitingWriteCount { get { return default(int); } }
        public void Dispose() { }
        public void EnterReadLock() { }
        public void EnterUpgradeableReadLock() { }
        public void EnterWriteLock() { }
        public void ExitReadLock() { }
        public void ExitUpgradeableReadLock() { }
        public void ExitWriteLock() { }
        public bool TryEnterReadLock(int millisecondsTimeout) { return default(bool); }
        public bool TryEnterReadLock(System.TimeSpan timeout) { return default(bool); }
        public bool TryEnterUpgradeableReadLock(int millisecondsTimeout) { return default(bool); }
        public bool TryEnterUpgradeableReadLock(System.TimeSpan timeout) { return default(bool); }
        public bool TryEnterWriteLock(int millisecondsTimeout) { return default(bool); }
        public bool TryEnterWriteLock(System.TimeSpan timeout) { return default(bool); }
    }
    public sealed partial class Semaphore : System.Threading.WaitHandle
    {
        public Semaphore(int initialCount, int maximumCount) { }
        public Semaphore(int initialCount, int maximumCount, string name) { }
        public Semaphore(int initialCount, int maximumCount, string name, out bool createdNew) { createdNew = default(bool); }
        public static System.Threading.Semaphore OpenExisting(string name) { return default(System.Threading.Semaphore); }
        public int Release() { return default(int); }
        public int Release(int releaseCount) { return default(int); }
        public static bool TryOpenExisting(string name, out System.Threading.Semaphore result) { result = default(System.Threading.Semaphore); return default(bool); }
    }
    public partial class SemaphoreFullException : System.Exception
    {
        public SemaphoreFullException() { }
        public SemaphoreFullException(string message) { }
        public SemaphoreFullException(string message, System.Exception innerException) { }
    }
    public partial class SemaphoreSlim : System.IDisposable
    {
        public SemaphoreSlim(int initialCount) { }
        public SemaphoreSlim(int initialCount, int maxCount) { }
        public System.Threading.WaitHandle AvailableWaitHandle { get { return default(System.Threading.WaitHandle); } }
        public int CurrentCount { get { return default(int); } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public int Release() { return default(int); }
        public int Release(int releaseCount) { return default(int); }
        public void Wait() { }
        public bool Wait(int millisecondsTimeout) { return default(bool); }
        public bool Wait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { return default(bool); }
        public void Wait(System.Threading.CancellationToken cancellationToken) { }
        public bool Wait(System.TimeSpan timeout) { return default(bool); }
        public bool Wait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { return default(bool); }
        public System.Threading.Tasks.Task WaitAsync() { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task<bool> WaitAsync(int millisecondsTimeout) { return default(System.Threading.Tasks.Task<bool>); }
        public System.Threading.Tasks.Task<bool> WaitAsync(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<bool>); }
        public System.Threading.Tasks.Task WaitAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task<bool> WaitAsync(System.TimeSpan timeout) { return default(System.Threading.Tasks.Task<bool>); }
        public System.Threading.Tasks.Task<bool> WaitAsync(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<bool>); }
    }
    public delegate void SendOrPostCallback(object state);
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct SpinLock
    {
        public SpinLock(bool enableThreadOwnerTracking) { throw new System.NotImplementedException(); }
        public bool IsHeld { get { return default(bool); } }
        public bool IsHeldByCurrentThread { get { return default(bool); } }
        public bool IsThreadOwnerTrackingEnabled { get { return default(bool); } }
        public void Enter(ref bool lockTaken) { }
        public void Exit() { }
        public void Exit(bool useMemoryBarrier) { }
        public void TryEnter(ref bool lockTaken) { }
        public void TryEnter(int millisecondsTimeout, ref bool lockTaken) { }
        public void TryEnter(System.TimeSpan timeout, ref bool lockTaken) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct SpinWait
    {
        public int Count { get { return default(int); } }
        public bool NextSpinWillYield { get { return default(bool); } }
        public void Reset() { }
        public void SpinOnce() { }
        public static void SpinUntil(System.Func<bool> condition) { }
        public static bool SpinUntil(System.Func<bool> condition, int millisecondsTimeout) { return default(bool); }
        public static bool SpinUntil(System.Func<bool> condition, System.TimeSpan timeout) { return default(bool); }
    }
    public partial class SynchronizationContext
    {
        public SynchronizationContext() { }
        public static System.Threading.SynchronizationContext Current { get { return default(System.Threading.SynchronizationContext); } }
        public virtual System.Threading.SynchronizationContext CreateCopy() { return default(System.Threading.SynchronizationContext); }
        public virtual void OperationCompleted() { }
        public virtual void OperationStarted() { }
        public virtual void Post(System.Threading.SendOrPostCallback d, object state) { }
        public virtual void Send(System.Threading.SendOrPostCallback d, object state) { }
        [System.Security.SecurityCriticalAttribute]
        public static void SetSynchronizationContext(System.Threading.SynchronizationContext syncContext) { }
    }
    public partial class SynchronizationLockException : System.Exception
    {
        public SynchronizationLockException() { }
        public SynchronizationLockException(string message) { }
        public SynchronizationLockException(string message, System.Exception innerException) { }
    }
    public partial class ThreadLocal<T> : System.IDisposable
    {
        public ThreadLocal() { }
        public ThreadLocal(bool trackAllValues) { }
        public ThreadLocal(System.Func<T> valueFactory) { }
        public ThreadLocal(System.Func<T> valueFactory, bool trackAllValues) { }
        public bool IsValueCreated { get { return default(bool); } }
        public T Value { get { return default(T); } set { } }
        public System.Collections.Generic.IList<T> Values { get { return default(System.Collections.Generic.IList<T>); } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~ThreadLocal() { }
        public override string ToString() { return default(string); }
    }
    public static partial class Volatile
    {
        public static bool Read(ref bool location) { return default(bool); }
        public static byte Read(ref byte location) { return default(byte); }
        public static double Read(ref double location) { return default(double); }
        public static short Read(ref short location) { return default(short); }
        public static int Read(ref int location) { return default(int); }
        public static long Read(ref long location) { return default(long); }
        public static System.IntPtr Read(ref System.IntPtr location) { return default(System.IntPtr); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Read(ref sbyte location) { return default(sbyte); }
        public static float Read(ref float location) { return default(float); }
        [System.CLSCompliantAttribute(false)]
        public static ushort Read(ref ushort location) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static uint Read(ref uint location) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static ulong Read(ref ulong location) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static System.UIntPtr Read(ref System.UIntPtr location) { return default(System.UIntPtr); }
        public static T Read<T>(ref T location) where T : class { return default(T); }
        public static void Write(ref bool location, bool value) { }
        public static void Write(ref byte location, byte value) { }
        public static void Write(ref double location, double value) { }
        public static void Write(ref short location, short value) { }
        public static void Write(ref int location, int value) { }
        public static void Write(ref long location, long value) { }
        public static void Write(ref System.IntPtr location, System.IntPtr value) { }
        [System.CLSCompliantAttribute(false)]
        public static void Write(ref sbyte location, sbyte value) { }
        public static void Write(ref float location, float value) { }
        [System.CLSCompliantAttribute(false)]
        public static void Write(ref ushort location, ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public static void Write(ref uint location, uint value) { }
        [System.CLSCompliantAttribute(false)]
        public static void Write(ref ulong location, ulong value) { }
        [System.CLSCompliantAttribute(false)]
        public static void Write(ref System.UIntPtr location, System.UIntPtr value) { }
        public static void Write<T>(ref T location, T value) where T : class { }
    }
    public partial class WaitHandleCannotBeOpenedException : System.Exception
    {
        public WaitHandleCannotBeOpenedException() { }
        public WaitHandleCannotBeOpenedException(string message) { }
        public WaitHandleCannotBeOpenedException(string message, System.Exception innerException) { }
    }
}
