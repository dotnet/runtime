// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Threading;

public partial class SemaphoreSlim
{
    private int m_currentCount;
    private ManualResetEvent? m_waitHandle;
    private bool _isDisposed;

    private bool IsDisposed => _isDisposed;

    private WaitHandle AvailableWaitHandleCore
    {
        get
        {
            CheckDispose();
            ManualResetEvent? handle = m_waitHandle;
            if (handle is null)
            {
                handle = new ManualResetEvent(m_currentCount != 0);
                if (m_waitHandle is ManualResetEvent existing)
                {
                    handle.Dispose();
                    handle = existing;
                }
                else
                {
                    m_waitHandle = handle;
                    if (m_currentCount != 0)
                    {
                        handle.Set();
                    }
                    else
                    {
                        handle.Reset();
                    }
                }
            }

            return handle;
        }
    }

    private bool WaitCore(long millisecondsTimeout, CancellationToken cancellationToken)
    {
        CheckDispose();
        cancellationToken.ThrowIfCancellationRequested();

        if (m_currentCount > 0)
        {
            Debug.Assert(m_asyncHead is null);
            m_currentCount--;
            if (m_waitHandle is not null && m_currentCount == 0)
            {
                m_waitHandle.Reset();
            }

            return true;
        }
        if (millisecondsTimeout == 0)
        {
            return false;
        }

        TimeoutHelper.WaitForTimeout(millisecondsTimeout, cancellationToken);
        return false;
    }

    private Task<bool> WaitAsyncCore(long millisecondsTimeout, CancellationToken cancellationToken)
    {
        CheckDispose();
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        return WaitAsyncCoreLocked(millisecondsTimeout, cancellationToken);
    }

    private bool RemoveAsyncWaiterWithLock(TaskNode task) => RemoveAsyncWaiter(task);

    [Conditional("DEBUG")]
    private static void AssertIsLocked()
    {
    }

    private int ReleaseCore(int releaseCount)
    {
        CheckDispose();
        if (releaseCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(releaseCount), releaseCount, SR.SemaphoreSlim_Release_CountWrong);
        }

        int currentCount = m_currentCount;
        int returnCount = currentCount;
        if (m_maxCount - currentCount < releaseCount)
        {
            throw new SemaphoreFullException();
        }

        currentCount += releaseCount;
        while (currentCount > 0 && m_asyncHead is not null)
        {
            TaskNode waiter = m_asyncHead;
            RemoveAsyncWaiter(waiter);
            currentCount--;
            waiter.TrySetResult(result: true);
        }

        m_currentCount = currentCount;
        if (m_waitHandle is not null && returnCount == 0 && currentCount > 0)
        {
            m_waitHandle.Set();
        }

        return returnCount;
    }

    private void DisposeCore(bool disposing)
    {
        if (disposing)
        {
            m_waitHandle?.Dispose();
            m_waitHandle = null;
            _isDisposed = true;
            m_asyncHead = null;
            m_asyncTail = null;
        }
    }
}
