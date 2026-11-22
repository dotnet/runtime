// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading;

public partial class ManualResetEventSlim
{
    private bool _isDisposed;

    private bool IsSetCore { get; set; }
    private int SpinCountCore { get; set; }
    private bool IsDisposed => _isDisposed;

    private void Initialize(bool initialState, int spinCount)
    {
        IsSet = initialState;
        SpinCount = Environment.IsSingleProcessor ? DEFAULT_SPIN_SP : spinCount;
    }

    private void LazyInitializeEvent()
    {
        ManualResetEvent newEvent = new(IsSet);
        if (m_eventObj is not null)
        {
            newEvent.Dispose();
            return;
        }

        m_eventObj = newEvent;
        if (IsSet)
        {
            newEvent.Set();
        }
        else
        {
            newEvent.Reset();
        }
    }

    private void Set(bool duringCancellation)
    {
        IsSet = true;
        if (!duringCancellation)
        {
            m_eventObj?.Set();
        }
    }

    private void ResetCore()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        m_eventObj?.Reset();
        IsSet = false;
    }

    private bool WaitCore(int millisecondsTimeout, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);

        if (IsSet)
        {
            return true;
        }
        if (millisecondsTimeout == 0)
        {
            return false;
        }

        TimeoutHelper.WaitForTimeout(millisecondsTimeout, cancellationToken);
        return false;
    }

    private void DisposeCore(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        if (disposing)
        {
            m_eventObj?.Dispose();
            m_eventObj = null;
        }
    }
}
