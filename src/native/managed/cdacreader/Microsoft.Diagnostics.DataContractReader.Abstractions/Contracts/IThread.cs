// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

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
    Unknown             = 0x00000000,
    Hijacked            = 0x00000080,   // Return address has been hijacked
    Background          = 0x00000200,   // Thread is a background thread
    Unstarted           = 0x00000400,   // Thread has never been started
    Dead                = 0x00000800,   // Thread is dead
    ThreadPoolWorker    = 0x01000000,   // Thread is a thread pool worker thread
}

public record struct ThreadData(
    uint Id,
    TargetNUInt OSId,
    ThreadState State,
    bool PreemptiveGCDisabled,
    TargetPointer AllocContextPointer,
    TargetPointer AllocContextLimit,
    TargetPointer Frame,
    TargetPointer FirstNestedException,
    TargetPointer TEB,
    TargetPointer LastThrownObjectHandle,
    TargetPointer NextThread);

public interface IThread : IContract
{
    static string IContract.Name { get; } = nameof(Thread);

    public virtual ThreadStoreData GetThreadStoreData() => throw new NotImplementedException();
    public virtual ThreadStoreCounts GetThreadCounts() => throw new NotImplementedException();
    public virtual ThreadData GetThreadData(TargetPointer thread) => throw new NotImplementedException();
}

public readonly struct Thread : IThread
{
    // Everything throws NotImplementedException
}
