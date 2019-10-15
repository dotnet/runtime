// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public partial class ThreadPoolBoundHandleTests
{
    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public void Handle_ReturnsHandle()
    {
        using(SafeHandle handle = HandleFactory.CreateAsyncFileHandleForWrite(GetTestFilePath()))
        {
            using(ThreadPoolBoundHandle boundHandle = CreateThreadPoolBoundHandle(handle))
            {
                Assert.Same(boundHandle.Handle, handle);
            }
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public void Handle_AfterDisposed_DoesNotThrow()
    {
        using(SafeHandle handle = HandleFactory.CreateAsyncFileHandleForWrite(GetTestFilePath()))
        {
            ThreadPoolBoundHandle boundHandle = CreateThreadPoolBoundHandle(handle);
            boundHandle.Dispose();

            Assert.Same(boundHandle.Handle, handle);
        }
    }
}
