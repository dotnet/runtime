// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using Xunit;

public partial class ThreadPoolBoundHandleTests
{
    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        ThreadPoolBoundHandle boundHandle = CreateThreadPoolBoundHandle();

        boundHandle.Dispose();
        boundHandle.Dispose();
    }

    [Fact]
    public void Dispose_DoesNotDisposeHandle()
    {
        ThreadPoolBoundHandle boundHandle = CreateThreadPoolBoundHandle();

        Assert.False(boundHandle.Handle.IsClosed);
        
        boundHandle.Dispose();

        Assert.False(boundHandle.Handle.IsClosed);
    }

    [Fact]
    public unsafe void Dispose_WithoutFreeingNativeOverlapped_DoesNotThrow()
    {
        ThreadPoolBoundHandle boundHandle = CreateThreadPoolBoundHandle();

        NativeOverlapped* overlapped = boundHandle.AllocateNativeOverlapped((_, __, ___) => { }, new object(), new byte[1024]);

        boundHandle.Dispose();
    }
}
