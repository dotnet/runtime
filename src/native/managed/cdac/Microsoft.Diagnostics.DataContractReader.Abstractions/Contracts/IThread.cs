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
    SuspensionTrapped   = 0x00000002,   // Thread is trapped waiting for suspension to complete (was in managed code)
    GCSuspendRedirected = 0x00000004,   // Thread has been redirected to suspension routine
    DebugSuspendPending = 0x00000008,   // Is the debugger suspending threads?
    Hijacked            = 0x00000080,   // Return address has been hijacked
    Background          = 0x00000200,   // Thread is a background thread
    Unstarted           = 0x00000400,   // Thread has never been started
    CoInitialized       = 0x00002000,   // CoInitialize has been called for this thread
    InSTA               = 0x00004000,   // Thread hosts an STA
    InMTA               = 0x00008000,   // Thread is part of the MTA
    Stopped             = 0x00010000,   // Thread has started to shut down
    DebugSyncSuspended  = 0x00080000,   // Thread has suspended itself at a safe point in response to a debugger suspend request
    DebugWillSync       = 0x00100000,   // Debugger will wait for this thread to sync
    ThreadPoolWorker    = 0x01000000,   // Thread is a thread pool worker thread
    WaitSleepJoin       = 0x02000000,   // Thread is in a Sleep(), Wait(), Join()
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
    TargetPointer GCFrame,
    bool IsExceptionInProgress,
    TargetPointer OSExceptionRecord,
    TargetPointer OSExceptionContextRecord);

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
}

public readonly struct Thread : IThread
{
    // Everything throws NotImplementedException
}
