// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Xunit;

public partial class ThreadPoolBoundHandleTests
{
    [Fact]
    public unsafe void FreeNativeOverlapped_NullAsNativeOverlapped_ThrowsArgumentNullException()
    {
        ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle();

        Assert.Throws<ArgumentNullException>("overlapped", () =>
        {
            handle.FreeNativeOverlapped((NativeOverlapped*)null);
        });
    }

    [Fact]
    public unsafe void FreeNativeOverlapped_WhenDisposed_DoesNotThrow()
    {
        ThreadPoolBoundHandle boundHandle = CreateThreadPoolBoundHandle();
        NativeOverlapped* overlapped = boundHandle.AllocateNativeOverlapped((_, __, ___) => { }, new object(), new byte[256]);
        boundHandle.Dispose();
        boundHandle.FreeNativeOverlapped(overlapped);
    }
}
