// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Xunit;

public partial class ThreadPoolBoundHandleTests
{
    [Fact]
    public unsafe void GetNativeOverlappedState_NullAsNativeOverlapped_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("overlapped", () =>
        {
            ThreadPoolBoundHandle.GetNativeOverlappedState((NativeOverlapped*)null);
        });
    }

    [Fact]
    public unsafe void GetNativeOverlappedState_WhenUnderlyingStateIsNull_ReturnsNull()
    {
        NativeOverlapped* overlapped = AllocateNativeOverlapped((_, __, ___) => { }, (object)null, new byte[0]);

        object result = ThreadPoolBoundHandle.GetNativeOverlappedState(overlapped);

        Assert.Null(result);
    }

    [Fact]
    public unsafe void GetNativeOverlappedState_WhenUnderlyingStateIsObject_ReturnsObject()
    {
        object context = new object();

        NativeOverlapped* overlapped = AllocateNativeOverlapped((_, __, ___) => { }, context, new byte[0]);

        object result = ThreadPoolBoundHandle.GetNativeOverlappedState(overlapped);

        Assert.Same(context, result);
    }

    [Fact]
    public unsafe void GetNativeOverlappedState_WhenUnderlyingStateIsIAsyncResult_ReturnsIAsyncResult()
    {   // CoreCLR/Desktop CLR version of overlapped sits on top of Overlapped class 
        // and treats IAsyncResult specially, which is why we special case this case.

        AsyncResult context = new AsyncResult();

        NativeOverlapped* overlapped = AllocateNativeOverlapped((_, __, ___) => { }, context, new byte[0]);

        object result = ThreadPoolBoundHandle.GetNativeOverlappedState(overlapped);

        Assert.Same(context, result);
    }
}
