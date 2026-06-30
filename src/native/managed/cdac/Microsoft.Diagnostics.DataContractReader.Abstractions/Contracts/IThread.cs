// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

[Flags]
public enum ThreadContextSource
{
    None = 0,
    Debugger = 1,
}

public record struct ThreadStoreData(
    int ThreadCount,
    TargetPointer FirstThread,
    TargetPointer FinalizerThread,
    TargetPointer GCThread);

public record struct ThreadStoreCounts(
    int UnstartedThreadCount,
    int BackgroundThreadCount,
    int PendingThreadCount,
    int DeadThreadCount);

[Flags]
public enum ThreadState
{
    Unknown             = 0x00000000,   // Threads are initialized this way
    AbortRequested      = 0x00000001,   // Abort the thread
    SuspensionTrapped   = 0x00000002,   // Thread is trapped waiting for suspension to complete (was in managed code)
    GCSuspendRedirected = 0x00000004,   // ThreadSuspend::SuspendRuntime has redirected the thread to suspension routine
    DebugSuspendPending = 0x00000008,   // Is the debugger suspending threads?
    GCOnTransitions     = 0x00000010,   // Force a GC on stub transitions (GCStress only)
    SyncBlockCleanup    = 0x00000020,   // The synch block needs to be cleaned up
    ExecutingOnAltStack = 0x00000040,   // Runtime is executing on an alternate stack located anywhere in the memory
    Hijacked            = 0x00000080,   // Return address has been hijacked
    Background          = 0x00000200,   // Thread is a background thread
    Unstarted           = 0x00000400,   // Thread has never been started
    Dead                = 0x00000800,   // Thread has finished shutting down and is about to be terminated by the OS
    WeOwn               = 0x00001000,   // Exposed object initiated this thread
    CoInitialized       = 0x00002000,   // CoInitialize has been called for this thread
    InSTA               = 0x00004000,   // Thread hosts an STA
    InMTA               = 0x00008000,   // Thread is part of the MTA
    Stopped             = 0x00010000,   // Thread has started to shut down
    FullyInitialized    = 0x00020000,   // Thread is fully initialized and we are ready to broadcast its existence to external clients
    SyncSuspended       = 0x00080000,   // Suspended via WaitSuspendEvent
    DebugWillSync       = 0x00100000,   // Debugger will wait for this thread to sync
    StackCrawlNeeded    = 0x00200000,   // A stackcrawl is needed on this thread, such as for thread abort
    ThreadPoolWorker    = 0x01000000,   // Thread is a thread pool worker thread
    WaitSleepJoin       = 0x02000000,   // Thread is in a Sleep(), Wait(), Join()
    Interrupted         = 0x04000000,   // Thread was awakened by an interrupt APC
    AbortInitiated      = 0x10000000,   // Set when abort is begun
    Finalized           = 0x20000000,   // The associated managed Thread object has been finalized
    FailStarted         = 0x40000000,   // The thread failed during startup
    Detached            = unchecked((int)0x80000000), // Thread was detached
}

[Flags]
public enum DebuggerControlledThreadState
{
    None                        = 0x00000000, // Threads are initialized this way
    UserSuspend                 = 0x00000001, // Marked "suspended" by the debugger
}

public record struct ThreadData(
    TargetPointer ThreadAddress,
    uint Id,
    TargetNUInt OSId,
    ThreadState State,
    bool PreemptiveGCDisabled,
    TargetPointer AllocContextPointer,
    TargetPointer AllocContextLimit,
    TargetPointer Frame,
    TargetPointer FirstNestedException,
    TargetPointer ExposedObjectHandle,
    TargetPointer LastThrownObjectHandle,
    TargetPointer CurrentCustomDebuggerNotificationHandle,
    bool LastThrownObjectIsUnhandled,
    bool HasUnhandledException,
    TargetPointer NextThread,
    TargetPointer ThreadHandle,
    bool IsInteropDebuggingHijacked,
    TargetPointer DebuggerFilterContext,
    TargetPointer GCFrame);

public interface IThread : IContract
{
    static string IContract.Name { get; } = nameof(Thread);

    void SetDebuggerControlledThreadState(TargetPointer thread, DebuggerControlledThreadState state) => throw new NotImplementedException();
    void ResetDebuggerControlledThreadState(TargetPointer thread, DebuggerControlledThreadState state) => throw new NotImplementedException();
    ThreadStoreData GetThreadStoreData() => throw new NotImplementedException();
    ThreadStoreCounts GetThreadCounts() => throw new NotImplementedException();
    ThreadData GetThreadData(TargetPointer thread) => throw new NotImplementedException();
    void GetThreadAllocContext(TargetPointer thread, out long allocBytes, out long allocBytesLoh) => throw new NotImplementedException();
    void GetStackLimitData(TargetPointer threadPointer, out TargetPointer stackBase,
                           out TargetPointer stackLimit, out TargetPointer frameAddress) => throw new NotImplementedException();
    TargetPointer IdToThread(uint id) => throw new NotImplementedException();
    TargetPointer GetThreadLocalStaticBase(TargetPointer threadPointer, TargetPointer tlsIndexPtr) => throw new NotImplementedException();
    TargetPointer GetCurrentExceptionHandle(TargetPointer threadPointer) => throw new NotImplementedException();
    byte[] GetWatsonBuckets(TargetPointer threadPointer) => throw new NotImplementedException();
}

public readonly struct Thread : IThread
{
    // Everything throws NotImplementedException
}
