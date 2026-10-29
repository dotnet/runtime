// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading;

internal sealed class Condition
{
    private readonly Lock _lock;

    public Condition(Lock @lock)
    {
#pragma warning disable CS9216
        ArgumentNullException.ThrowIfNull(@lock);
#pragma warning restore CS9216
        _lock = @lock;
    }

    internal Lock AssociatedLock => _lock;

    public bool Wait(int millisecondsTimeout, object associatedObjectForMonitorWait) =>
        _lock.Wait(millisecondsTimeout, associatedObjectForMonitorWait);

    public void SignalOne() => _lock.Pulse();

    public void SignalAll() => _lock.PulseAll();
}
