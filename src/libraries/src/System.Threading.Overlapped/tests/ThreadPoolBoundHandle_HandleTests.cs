// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
