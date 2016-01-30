// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public partial class ThreadPoolBoundHandleTests
{
    [Fact]
    public void Handle_ReturnsHandle()
    {
        using(SafeHandle handle = HandleFactory.CreateAsyncFileHandleForWrite())
        {
            using(ThreadPoolBoundHandle boundHandle = CreateThreadPoolBoundHandle(handle))
            {
                Assert.Same(boundHandle.Handle, handle);
            }
        }
    }

    [Fact]
    public void Handle_AfterDisposed_DoesNotThrow()
    {
        using(SafeHandle handle = HandleFactory.CreateAsyncFileHandleForWrite())
        {
            ThreadPoolBoundHandle boundHandle = CreateThreadPoolBoundHandle(handle);
            boundHandle.Dispose();

            Assert.Same(boundHandle.Handle, handle);
        }
    }
}
