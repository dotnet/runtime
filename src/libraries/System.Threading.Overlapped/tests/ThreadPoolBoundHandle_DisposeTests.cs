// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

public partial class ThreadPoolBoundHandleTests
{
    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        ThreadPoolBoundHandle boundHandle = CreateThreadPoolBoundHandle();

        boundHandle.Dispose();
        boundHandle.Dispose();
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public void Dispose_DoesNotDisposeHandle()
    {
        ThreadPoolBoundHandle boundHandle = CreateThreadPoolBoundHandle();

        Assert.False(boundHandle.Handle.IsClosed);
        
        boundHandle.Dispose();

        Assert.False(boundHandle.Handle.IsClosed);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void Dispose_WithoutFreeingNativeOverlapped_DoesNotThrow()
    {
        ThreadPoolBoundHandle boundHandle = CreateThreadPoolBoundHandle();

        NativeOverlapped* overlapped = boundHandle.AllocateNativeOverlapped((_, __, ___) => { }, new object(), new byte[1024]);

        boundHandle.Dispose();
    }
}
