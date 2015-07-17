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
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            Assert.Throws<ArgumentNullException>("overlapped", () =>
            {
                handle.FreeNativeOverlapped((NativeOverlapped*)null);
            });
        }
    }

    [Fact]
    public unsafe void FreeNativeOverlapped_WhenDisposed_DoesNotThrow()
    {
        ThreadPoolBoundHandle boundHandle = CreateThreadPoolBoundHandle();
        NativeOverlapped* overlapped = boundHandle.AllocateNativeOverlapped((_, __, ___) => { }, new object(), new byte[256]);
        boundHandle.Dispose();
        boundHandle.FreeNativeOverlapped(overlapped);
    }

    [Fact]
    public unsafe void FreeNativeOverlapped_WithWrongHandle_ThrowsArgumentException()
    {
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            NativeOverlapped* overlapped = handle.AllocateNativeOverlapped((_, __, ___) => { }, (object)null, (byte[])null);

            using (ThreadPoolBoundHandle handle2 = CreateThreadPoolBoundHandle())
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    handle2.FreeNativeOverlapped(overlapped);
                });
            }

            handle.FreeNativeOverlapped(overlapped);
        }
    }
}
