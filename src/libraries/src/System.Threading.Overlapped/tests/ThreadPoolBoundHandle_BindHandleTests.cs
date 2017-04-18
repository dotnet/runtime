// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public partial class ThreadPoolBoundHandleTests
{
    [Fact]
    public void BindHandle_NullAsHandle_ThrowsArgumentNullException()
    {
        AssertExtensions.Throws<ArgumentNullException>("handle", () =>
        {
            ThreadPoolBoundHandle.BindHandle((SafeHandle)null);
        });
    }

    [Fact]
    public void BindHandle_ZeroAsHandle_ThrowsArgumentException()
    {
        using(SafeHandle handle = HandleFactory.CreateHandle(IntPtr.Zero))
        {
            AssertExtensions.Throws<ArgumentException>("handle", () =>
            {
                ThreadPoolBoundHandle.BindHandle(handle);
            });
        }
    }

    [Fact]
    public void BindHandle_MinusOneAsHandle_ThrowsArgumentException()
    {
        using(SafeHandle handle = HandleFactory.CreateHandle(new IntPtr(-1)))
        {
            AssertExtensions.Throws<ArgumentException>("handle", () =>
            {
                ThreadPoolBoundHandle.BindHandle(handle);
            });
        }
    }

    [Fact]
    public void BindHandle_SyncHandleAsHandle_ThrowsArgumentException()
    {   // Can't bind a handle that was not opened for overlapped I/O

        using(SafeHandle handle = HandleFactory.CreateSyncFileHandleFoWrite())
        {
            AssertExtensions.Throws<ArgumentException>("handle", () =>
            {
                ThreadPoolBoundHandle.BindHandle(handle);
            });
        }
    }

    [Fact]
    public void BindHandle_ClosedSyncHandleAsHandle_ThrowsArgumentException()
    {
        using(Win32Handle handle = HandleFactory.CreateSyncFileHandleFoWrite())
        {
            handle.CloseWithoutDisposing();

            AssertExtensions.Throws<ArgumentException>("handle", () =>
            {
                ThreadPoolBoundHandle.BindHandle(handle);
            });
        }
    }

    [Fact]
    public void BindHandle_ClosedAsyncHandleAsHandle_ThrowsArgumentException()
    {
        using(Win32Handle handle = HandleFactory.CreateAsyncFileHandleForWrite())
        {
            handle.CloseWithoutDisposing();

            AssertExtensions.Throws<ArgumentException>("handle", () =>
            {
                ThreadPoolBoundHandle.BindHandle(handle);
            });
        }
    }

    [Fact]
    public void BindHandle_DisposedSyncHandleAsHandle_ThrowsArgumentException()
    {
        Win32Handle handle = HandleFactory.CreateSyncFileHandleFoWrite();
        handle.Dispose();

        AssertExtensions.Throws<ArgumentException>("handle", () =>
        {
            ThreadPoolBoundHandle.BindHandle(handle);
        });
    }


    [Fact]
    public void BindHandle_DisposedAsyncHandleAsHandle_ThrowsArgumentException()
    {
        Win32Handle handle = HandleFactory.CreateAsyncFileHandleForWrite();
        handle.Dispose();

        AssertExtensions.Throws<ArgumentException>("handle", () =>
        {
            ThreadPoolBoundHandle.BindHandle(handle);
        });
    }

    [Fact]
    public void BindHandle_AlreadyBoundHandleAsHandle_ThrowsArgumentException()
    {
        using(SafeHandle handle = HandleFactory.CreateAsyncFileHandleForWrite())
        {
            // Once
            ThreadPoolBoundHandle.BindHandle(handle);

            AssertExtensions.Throws<ArgumentException>("handle", () =>
            {
                // Twice
                ThreadPoolBoundHandle.BindHandle(handle);
            });
        }
    }
}
