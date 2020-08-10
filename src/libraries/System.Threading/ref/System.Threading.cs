// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Threading
{
    public partial class AbandonedMutexException : System.SystemException
    {
        public AbandonedMutexException() { }
        public AbandonedMutexException(int location, System.Threading.WaitHandle? handle) { }
        protected AbandonedMutexException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public AbandonedMutexException(string? message) { }
        public AbandonedMutexException(string? message, System.Exception? inner) { }
        public AbandonedMutexException(string? message, System.Exception? inner, int location, System.Threading.WaitHandle? handle) { }
        public AbandonedMutexException(string? message, int location, System.Threading.WaitHandle? handle) { }
        public System.Threading.Mutex? Mutex { get { throw null; } }
        public int MutexIndex { get { throw null; } }
    }
    public partial struct AsyncFlowControl : System.IDisposable
    {
        private object _dummy;
        private int _dummyPrimitive;
        public void Dispose() { }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Threading.AsyncFlowControl obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Threading.AsyncFlowControl a, System.Threading.AsyncFlowControl b) { throw null; }
        public static bool operator !=(System.Threading.AsyncFlowControl a, System.Threading.AsyncFlowControl b) { throw null; }
        public void Undo() { }
    }
    public readonly partial struct AsyncLocalValueChangedArgs<T>
    {
        private readonly T _PreviousValue_k__BackingField;
        private readonly T _CurrentValue_k__BackingField;
        private readonly int _dummyPrimitive;
        public T? CurrentValue { get { throw null; } }
        public T? PreviousValue { get { throw null; } }
        public bool ThreadContextChanged { get { throw null; } }
    }
    public sealed partial class AsyncLocal<T>
    {
        public AsyncLocal() { }
        public AsyncLocal(System.Action<System.Threading.AsyncLocalValueChangedArgs<T>>? valueChangedHandler) { }
        [System.Diagnostics.CodeAnalysis.MaybeNullAttribute]
        public T Value { get { throw null; } set { } }
    }
    public sealed partial class AutoResetEvent : System.Threading.EventWaitHandle
    {
        public AutoResetEvent(bool initialState) : base (default(bool), default(System.Threading.EventResetMode)) { }
    }
    public partial class Barrier : System.IDisposable
    {
        public Barrier(int participantCount) { }
        public Barrier(int participantCount, System.Action<System.Threading.Barrier>? postPhaseAction) { }
        public long CurrentPhaseNumber { get { throw null; } }
        public int ParticipantCount { get { throw null; } }
        public int ParticipantsRemaining { get { throw null; } }
        public long AddParticipant() { throw null; }
        public long AddParticipants(int participantCount) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void RemoveParticipant() { }
        public void RemoveParticipants(int participantCount) { }
        public void SignalAndWait() { }
        public bool SignalAndWait(int millisecondsTimeout) { throw null; }
        public bool SignalAndWait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void SignalAndWait(System.Threading.CancellationToken cancellationToken) { }
        public bool SignalAndWait(System.TimeSpan timeout) { throw null; }
        public bool SignalAndWait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class BarrierPostPhaseException : System.Exception
    {
        public BarrierPostPhaseException() { }
        public BarrierPostPhaseException(System.Exception? innerException) { }
        protected BarrierPostPhaseException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public BarrierPostPhaseException(string? message) { }
        public BarrierPostPhaseException(string? message, System.Exception? innerException) { }
    }
    public delegate void ContextCallback(object? state);
    public partial class CountdownEvent : System.IDisposable
    {
        public CountdownEvent(int initialCount) { }
        public int CurrentCount { get { throw null; } }
        public int InitialCount { get { throw null; } }
        public bool IsSet { get { throw null; } }
        public System.Threading.WaitHandle WaitHandle { get { throw null; } }
        public void AddCount() { }
        public void AddCount(int signalCount) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void Reset() { }
        public void Reset(int count) { }
        public bool Signal() { throw null; }
        public bool Signal(int signalCount) { throw null; }
        public bool TryAddCount() { throw null; }
        public bool TryAddCount(int signalCount) { throw null; }
        public void Wait() { }
        public bool Wait(int millisecondsTimeout) { throw null; }
        public bool Wait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Wait(System.Threading.CancellationToken cancellationToken) { }
        public bool Wait(System.TimeSpan timeout) { throw null; }
        public bool Wait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public enum EventResetMode
    {
        AutoReset = 0,
        ManualReset = 1,
    }
    public partial class EventWaitHandle : System.Threading.WaitHandle
    {
        public EventWaitHandle(bool initialState, System.Threading.EventResetMode mode) { }
        public EventWaitHandle(bool initialState, System.Threading.EventResetMode mode, string? name) { }
        public EventWaitHandle(bool initialState, System.Threading.EventResetMode mode, string? name, out bool createdNew) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Threading.EventWaitHandle OpenExisting(string name) { throw null; }
        public bool Reset() { throw null; }
        public bool Set() { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static bool TryOpenExisting(string name, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Threading.EventWaitHandle? result) { throw null; }
    }
    public sealed partial class ExecutionContext : System.IDisposable, System.Runtime.Serialization.ISerializable
    {
        internal ExecutionContext() { }
        public static System.Threading.ExecutionContext? Capture() { throw null; }
        public System.Threading.ExecutionContext CreateCopy() { throw null; }
        public void Dispose() { }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public static bool IsFlowSuppressed() { throw null; }
        public static void RestoreFlow() { }
        public static void Run(System.Threading.ExecutionContext executionContext, System.Threading.ContextCallback callback, object? state) { }
        public static System.Threading.AsyncFlowControl SuppressFlow() { throw null; }
    }
    public partial class HostExecutionContext : System.IDisposable
    {
        public HostExecutionContext() { }
        public HostExecutionContext(object? state) { }
        protected internal object? State { get { throw null; } set { } }
        public virtual System.Threading.HostExecutionContext CreateCopy() { throw null; }
        public void Dispose() { }
        public virtual void Dispose(bool disposing) { }
    }
    public partial class HostExecutionContextManager
    {
        public HostExecutionContextManager() { }
        public virtual System.Threading.HostExecutionContext? Capture() { throw null; }
        public virtual void Revert(object previousState) { }
        public virtual object SetHostExecutionContext(System.Threading.HostExecutionContext hostExecutionContext) { throw null; }
    }
    public static partial class Interlocked
    {
        public static int Add(ref int location1, int value) { throw null; }
        public static long Add(ref long location1, long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint Add(ref uint location1, uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong Add(ref ulong location1, ulong value) { throw null; }
        public static int And(ref int location1, int value) { throw null; }
        public static long And(ref long location1, long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint And(ref uint location1, uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong And(ref ulong location1, ulong value) { throw null; }
        public static double CompareExchange(ref double location1, double value, double comparand) { throw null; }
        public static int CompareExchange(ref int location1, int value, int comparand) { throw null; }
        public static long CompareExchange(ref long location1, long value, long comparand) { throw null; }
        public static System.IntPtr CompareExchange(ref System.IntPtr location1, System.IntPtr value, System.IntPtr comparand) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("location1")]
        public static object? CompareExchange(ref object? location1, object? value, object? comparand) { throw null; }
        public static float CompareExchange(ref float location1, float value, float comparand) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint CompareExchange(ref uint location1, uint value, uint comparand) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong CompareExchange(ref ulong location1, ulong value, ulong comparand) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("location1")]
        public static T CompareExchange<T>(ref T location1, T value, T comparand) where T : class? { throw null; }
        public static int Decrement(ref int location) { throw null; }
        public static long Decrement(ref long location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint Decrement(ref uint location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong Decrement(ref ulong location) { throw null; }
        public static double Exchange(ref double location1, double value) { throw null; }
        public static int Exchange(ref int location1, int value) { throw null; }
        public static long Exchange(ref long location1, long value) { throw null; }
        public static System.IntPtr Exchange(ref System.IntPtr location1, System.IntPtr value) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("location1")]
        public static object? Exchange([System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")] ref object? location1, object? value) { throw null; }
        public static float Exchange(ref float location1, float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint Exchange(ref uint location1, uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong Exchange(ref ulong location1, ulong value) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("location1")]
        public static T Exchange<T>([System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")] ref T location1, T value) where T : class? { throw null; }
        public static int Increment(ref int location) { throw null; }
        public static long Increment(ref long location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint Increment(ref uint location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong Increment(ref ulong location) { throw null; }
        public static void MemoryBarrier() { }
        public static void MemoryBarrierProcessWide() { }
        public static int Or(ref int location1, int value) { throw null; }
        public static long Or(ref long location1, long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint Or(ref uint location1, uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong Or(ref ulong location1, ulong value) { throw null; }
        public static long Read(ref long location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong Read(ref ulong location) { throw null; }
    }
    public static partial class LazyInitializer
    {
        public static T EnsureInitialized<T>([System.Diagnostics.CodeAnalysis.NotNullAttribute] ref T? target) where T : class { throw null; }
        public static T EnsureInitialized<T>([System.Diagnostics.CodeAnalysis.AllowNullAttribute] ref T target, ref bool initialized, [System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("syncLock")] ref object? syncLock) { throw null; }
        public static T EnsureInitialized<T>([System.Diagnostics.CodeAnalysis.AllowNullAttribute] ref T target, ref bool initialized, [System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("syncLock")] ref object? syncLock, System.Func<T> valueFactory) { throw null; }
        public static T EnsureInitialized<T>([System.Diagnostics.CodeAnalysis.NotNullAttribute] ref T? target, System.Func<T> valueFactory) where T : class { throw null; }
        public static T EnsureInitialized<T>([System.Diagnostics.CodeAnalysis.NotNullAttribute] ref T? target, [System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("syncLock")] ref object? syncLock, System.Func<T> valueFactory) where T : class { throw null; }
    }
    public partial struct LockCookie
    {
        private int _dummyPrimitive;
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Threading.LockCookie obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Threading.LockCookie a, System.Threading.LockCookie b) { throw null; }
        public static bool operator !=(System.Threading.LockCookie a, System.Threading.LockCookie b) { throw null; }
    }
    public partial class LockRecursionException : System.Exception
    {
        public LockRecursionException() { }
        protected LockRecursionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public LockRecursionException(string? message) { }
        public LockRecursionException(string? message, System.Exception? innerException) { }
    }
    public enum LockRecursionPolicy
    {
        NoRecursion = 0,
        SupportsRecursion = 1,
    }
    public sealed partial class ManualResetEvent : System.Threading.EventWaitHandle
    {
        public ManualResetEvent(bool initialState) : base (default(bool), default(System.Threading.EventResetMode)) { }
    }
    public partial class ManualResetEventSlim : System.IDisposable
    {
        public ManualResetEventSlim() { }
        public ManualResetEventSlim(bool initialState) { }
        public ManualResetEventSlim(bool initialState, int spinCount) { }
        public bool IsSet { get { throw null; } }
        public int SpinCount { get { throw null; } }
        public System.Threading.WaitHandle WaitHandle { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void Reset() { }
        public void Set() { }
        public void Wait() { }
        public bool Wait(int millisecondsTimeout) { throw null; }
        public bool Wait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Wait(System.Threading.CancellationToken cancellationToken) { }
        public bool Wait(System.TimeSpan timeout) { throw null; }
        public bool Wait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public static partial class Monitor
    {
        public static long LockContentionCount { get { throw null; } }
        public static void Enter(object obj) { }
        public static void Enter(object obj, ref bool lockTaken) { }
        public static void Exit(object obj) { }
        public static bool IsEntered(object obj) { throw null; }
        public static void Pulse(object obj) { }
        public static void PulseAll(object obj) { }
        public static bool TryEnter(object obj) { throw null; }
        public static void TryEnter(object obj, ref bool lockTaken) { }
        public static bool TryEnter(object obj, int millisecondsTimeout) { throw null; }
        public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken) { }
        public static bool TryEnter(object obj, System.TimeSpan timeout) { throw null; }
        public static void TryEnter(object obj, System.TimeSpan timeout, ref bool lockTaken) { }
        public static bool Wait(object obj) { throw null; }
        public static bool Wait(object obj, int millisecondsTimeout) { throw null; }
        public static bool Wait(object obj, int millisecondsTimeout, bool exitContext) { throw null; }
        public static bool Wait(object obj, System.TimeSpan timeout) { throw null; }
        public static bool Wait(object obj, System.TimeSpan timeout, bool exitContext) { throw null; }
    }
    public sealed partial class Mutex : System.Threading.WaitHandle
    {
        public Mutex() { }
        public Mutex(bool initiallyOwned) { }
        public Mutex(bool initiallyOwned, string? name) { }
        public Mutex(bool initiallyOwned, string? name, out bool createdNew) { throw null; }
        public static System.Threading.Mutex OpenExisting(string name) { throw null; }
        public void ReleaseMutex() { }
        public static bool TryOpenExisting(string name, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Threading.Mutex? result) { throw null; }
    }
    public sealed partial class ReaderWriterLock : System.Runtime.ConstrainedExecution.CriticalFinalizerObject
    {
        public ReaderWriterLock() { }
        public bool IsReaderLockHeld { get { throw null; } }
        public bool IsWriterLockHeld { get { throw null; } }
        public int WriterSeqNum { get { throw null; } }
        public void AcquireReaderLock(int millisecondsTimeout) { }
        public void AcquireReaderLock(System.TimeSpan timeout) { }
        public void AcquireWriterLock(int millisecondsTimeout) { }
        public void AcquireWriterLock(System.TimeSpan timeout) { }
        public bool AnyWritersSince(int seqNum) { throw null; }
        public void DowngradeFromWriterLock(ref System.Threading.LockCookie lockCookie) { }
        public System.Threading.LockCookie ReleaseLock() { throw null; }
        public void ReleaseReaderLock() { }
        public void ReleaseWriterLock() { }
        public void RestoreLock(ref System.Threading.LockCookie lockCookie) { }
        public System.Threading.LockCookie UpgradeToWriterLock(int millisecondsTimeout) { throw null; }
        public System.Threading.LockCookie UpgradeToWriterLock(System.TimeSpan timeout) { throw null; }
    }
    public partial class ReaderWriterLockSlim : System.IDisposable
    {
        public ReaderWriterLockSlim() { }
        public ReaderWriterLockSlim(System.Threading.LockRecursionPolicy recursionPolicy) { }
        public int CurrentReadCount { get { throw null; } }
        public bool IsReadLockHeld { get { throw null; } }
        public bool IsUpgradeableReadLockHeld { get { throw null; } }
        public bool IsWriteLockHeld { get { throw null; } }
        public System.Threading.LockRecursionPolicy RecursionPolicy { get { throw null; } }
        public int RecursiveReadCount { get { throw null; } }
        public int RecursiveUpgradeCount { get { throw null; } }
        public int RecursiveWriteCount { get { throw null; } }
        public int WaitingReadCount { get { throw null; } }
        public int WaitingUpgradeCount { get { throw null; } }
        public int WaitingWriteCount { get { throw null; } }
        public void Dispose() { }
        public void EnterReadLock() { }
        public void EnterUpgradeableReadLock() { }
        public void EnterWriteLock() { }
        public void ExitReadLock() { }
        public void ExitUpgradeableReadLock() { }
        public void ExitWriteLock() { }
        public bool TryEnterReadLock(int millisecondsTimeout) { throw null; }
        public bool TryEnterReadLock(System.TimeSpan timeout) { throw null; }
        public bool TryEnterUpgradeableReadLock(int millisecondsTimeout) { throw null; }
        public bool TryEnterUpgradeableReadLock(System.TimeSpan timeout) { throw null; }
        public bool TryEnterWriteLock(int millisecondsTimeout) { throw null; }
        public bool TryEnterWriteLock(System.TimeSpan timeout) { throw null; }
    }
    public sealed partial class Semaphore : System.Threading.WaitHandle
    {
        public Semaphore(int initialCount, int maximumCount) { }
        public Semaphore(int initialCount, int maximumCount, string? name) { }
        public Semaphore(int initialCount, int maximumCount, string? name, out bool createdNew) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Threading.Semaphore OpenExisting(string name) { throw null; }
        public int Release() { throw null; }
        public int Release(int releaseCount) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static bool TryOpenExisting(string name, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Threading.Semaphore? result) { throw null; }
    }
    public partial class SemaphoreFullException : System.SystemException
    {
        public SemaphoreFullException() { }
        protected SemaphoreFullException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public SemaphoreFullException(string? message) { }
        public SemaphoreFullException(string? message, System.Exception? innerException) { }
    }
    public partial class SemaphoreSlim : System.IDisposable
    {
        public SemaphoreSlim(int initialCount) { }
        public SemaphoreSlim(int initialCount, int maxCount) { }
        public System.Threading.WaitHandle AvailableWaitHandle { get { throw null; } }
        public int CurrentCount { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public int Release() { throw null; }
        public int Release(int releaseCount) { throw null; }
        public void Wait() { }
        public bool Wait(int millisecondsTimeout) { throw null; }
        public bool Wait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Wait(System.Threading.CancellationToken cancellationToken) { }
        public bool Wait(System.TimeSpan timeout) { throw null; }
        public bool Wait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WaitAsync() { throw null; }
        public System.Threading.Tasks.Task<bool> WaitAsync(int millisecondsTimeout) { throw null; }
        public System.Threading.Tasks.Task<bool> WaitAsync(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WaitAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<bool> WaitAsync(System.TimeSpan timeout) { throw null; }
        public System.Threading.Tasks.Task<bool> WaitAsync(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public delegate void SendOrPostCallback(object? state);
    public partial struct SpinLock
    {
        private int _dummyPrimitive;
        public SpinLock(bool enableThreadOwnerTracking) { throw null; }
        public bool IsHeld { get { throw null; } }
        public bool IsHeldByCurrentThread { get { throw null; } }
        public bool IsThreadOwnerTrackingEnabled { get { throw null; } }
        public void Enter(ref bool lockTaken) { }
        public void Exit() { }
        public void Exit(bool useMemoryBarrier) { }
        public void TryEnter(ref bool lockTaken) { }
        public void TryEnter(int millisecondsTimeout, ref bool lockTaken) { }
        public void TryEnter(System.TimeSpan timeout, ref bool lockTaken) { }
    }
    public partial struct SpinWait
    {
        private int _dummyPrimitive;
        public int Count { get { throw null; } }
        public bool NextSpinWillYield { get { throw null; } }
        public void Reset() { }
        public void SpinOnce() { }
        public void SpinOnce(int sleep1Threshold) { }
        public static void SpinUntil(System.Func<bool> condition) { }
        public static bool SpinUntil(System.Func<bool> condition, int millisecondsTimeout) { throw null; }
        public static bool SpinUntil(System.Func<bool> condition, System.TimeSpan timeout) { throw null; }
    }
    public partial class SynchronizationContext
    {
        public SynchronizationContext() { }
        public static System.Threading.SynchronizationContext? Current { get { throw null; } }
        public virtual System.Threading.SynchronizationContext CreateCopy() { throw null; }
        public bool IsWaitNotificationRequired() { throw null; }
        public virtual void OperationCompleted() { }
        public virtual void OperationStarted() { }
        public virtual void Post(System.Threading.SendOrPostCallback d, object? state) { }
        public virtual void Send(System.Threading.SendOrPostCallback d, object? state) { }
        public static void SetSynchronizationContext(System.Threading.SynchronizationContext? syncContext) { }
        protected void SetWaitNotificationRequired() { }
        [System.CLSCompliantAttribute(false)]
        public virtual int Wait(System.IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout) { throw null; }
        [System.CLSCompliantAttribute(false)]
        protected static int WaitHelper(System.IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout) { throw null; }
    }
    public partial class SynchronizationLockException : System.SystemException
    {
        public SynchronizationLockException() { }
        protected SynchronizationLockException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public SynchronizationLockException(string? message) { }
        public SynchronizationLockException(string? message, System.Exception? innerException) { }
    }
    public partial class ThreadLocal<T> : System.IDisposable
    {
        public ThreadLocal() { }
        public ThreadLocal(bool trackAllValues) { }
        public ThreadLocal(System.Func<T> valueFactory) { }
        public ThreadLocal(System.Func<T> valueFactory, bool trackAllValues) { }
        public bool IsValueCreated { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.MaybeNullAttribute]
        public T Value { get { throw null; } set { } }
        public System.Collections.Generic.IList<T> Values { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~ThreadLocal() { }
        public override string? ToString() { throw null; }
    }
    public static partial class Volatile
    {
        public static bool Read(ref bool location) { throw null; }
        public static byte Read(ref byte location) { throw null; }
        public static double Read(ref double location) { throw null; }
        public static short Read(ref short location) { throw null; }
        public static int Read(ref int location) { throw null; }
        public static long Read(ref long location) { throw null; }
        public static System.IntPtr Read(ref System.IntPtr location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Read(ref sbyte location) { throw null; }
        public static float Read(ref float location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort Read(ref ushort location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint Read(ref uint location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong Read(ref ulong location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UIntPtr Read(ref System.UIntPtr location) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("location")]
        public static T Read<T>(ref T location) where T : class? { throw null; }
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
        public static void Write<T>([System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")] ref T location, T value) where T : class? { }
    }
    public partial class WaitHandleCannotBeOpenedException : System.ApplicationException
    {
        public WaitHandleCannotBeOpenedException() { }
        protected WaitHandleCannotBeOpenedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public WaitHandleCannotBeOpenedException(string? message) { }
        public WaitHandleCannotBeOpenedException(string? message, System.Exception? innerException) { }
    }
}
