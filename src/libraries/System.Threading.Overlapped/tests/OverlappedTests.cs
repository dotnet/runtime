// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

public static partial class OverlappedTests
{
    [Fact]
    public static void PropertyTest1()
    {
        IAsyncResult asyncResult = new Task(() => Console.WriteLine("this is a dummy task"));
        var obj = new Overlapped();

        Assert.Null(obj.AsyncResult);
        obj.AsyncResult = asyncResult;
        Assert.Same(obj.AsyncResult, asyncResult);

#pragma warning disable 618
        Assert.Equal(obj.EventHandle, 0);
        obj.EventHandle = 3;
        Assert.Equal(obj.EventHandle, 3);
#pragma warning restore 618

        var _handle = new ManualResetEvent(false).SafeWaitHandle;
        Assert.NotSame(obj.EventHandleIntPtr, IntPtr.Zero);
        obj.EventHandleIntPtr = _handle.DangerousGetHandle();
        Assert.Equal(obj.EventHandleIntPtr, _handle.DangerousGetHandle());

        Assert.Equal(obj.OffsetHigh, 0);
        obj.OffsetHigh = 3;
        Assert.Equal(obj.OffsetHigh, 3);

        Assert.Equal(obj.OffsetLow, 0);
        obj.OffsetLow = 1;
        Assert.Equal(obj.OffsetLow, 1);
    }

    [Fact]
    public static void PropertyTest2()
    {
        IAsyncResult asyncResult = new Task(() => Console.WriteLine("this is a dummy task"));
        var _event = new ManualResetEvent(false);
        var _handle = _event.SafeWaitHandle;

#pragma warning disable 618
        var obj = new Overlapped(1, 3, _event.Handle.ToInt32(), asyncResult);
        Assert.Equal(obj.EventHandle, _event.Handle.ToInt32());
#pragma warning restore 618

        Assert.Same(obj.AsyncResult, asyncResult);
        Assert.Equal(obj.EventHandleIntPtr, _handle.DangerousGetHandle());
        Assert.Equal(obj.OffsetHigh, 3);
        Assert.Equal(obj.OffsetLow, 1);
    }

    [Fact]
    public static void PropertyTest3()
    {
        IAsyncResult asyncResult = new Task(() => Console.WriteLine("this is a dummy task"));
        var _event = new ManualResetEvent(false);
        var _handle = _event.SafeWaitHandle;
        var obj = new Overlapped(1, 3, _handle.DangerousGetHandle(), asyncResult);
        Assert.Same(obj.AsyncResult, asyncResult);

#pragma warning disable 618
        Assert.Equal(obj.EventHandle, _event.Handle.ToInt32());
#pragma warning restore 618

        Assert.Equal(obj.EventHandleIntPtr, _handle.DangerousGetHandle());
        Assert.Equal(obj.OffsetHigh, 3);
        Assert.Equal(obj.OffsetLow, 1);
    }
    [Fact]
    public unsafe static void PackNegTest()
    {
        var helper = new AsyncHelper();
        IOCompletionCallback callback = MyCallback(helper);

        NativeOverlapped* nativeOverlapped;
        Overlapped ov = new Overlapped();
        nativeOverlapped = ov.Pack(new IOCompletionCallback(callback), null);

        try
        {
            Assert.True(nativeOverlapped != null);
            Assert.Throws<InvalidOperationException>(() => ov.Pack(new IOCompletionCallback(callback), null));
        }
        finally
        {
            Overlapped.Free(nativeOverlapped);
        }
    }


    [Fact]
    public unsafe static void PackNegTest1()
    {
#pragma warning disable 618
        var helper = new AsyncHelper();
        IOCompletionCallback callback = MyCallback(helper);

        NativeOverlapped* nativeOverlapped;
        Overlapped ov = new Overlapped();
        nativeOverlapped = ov.Pack(new IOCompletionCallback(callback));

        try
        {
            Assert.True(nativeOverlapped != null);
            Assert.Throws<InvalidOperationException>(() => ov.Pack(new IOCompletionCallback(callback)));
        }
        finally
        {
            Overlapped.Free(nativeOverlapped);
        }
#pragma warning restore 618
    }

    [Fact]
    public unsafe static void UnPackTest()
    {
        Assert.Throws<ArgumentNullException>(() => Overlapped.Unpack(null));

        Overlapped ov = new Overlapped();
        var helper = new AsyncHelper();
        IOCompletionCallback callback = MyCallback(helper);
        NativeOverlapped* nativeOverlapped = ov.Pack(new IOCompletionCallback(callback), null);
        try
        {
            Assert.True(null != nativeOverlapped);

            Overlapped ov1 = Overlapped.Unpack(nativeOverlapped);
            Assert.Same(ov1, ov);
        }
        finally
        {
            Overlapped.Free(nativeOverlapped);
        }
    }

    [Fact]
    public unsafe static void PackPosTest()
    {
#pragma warning disable 618
        Overlapped ov = new Overlapped();
        var helper = new AsyncHelper();
        IOCompletionCallback callback = MyCallback(helper);

        NativeOverlapped* nativeOverlapped = ov.Pack(callback);
        try
        {
            Assert.True(nativeOverlapped != null);

            Assert.True(ThreadPool.UnsafeQueueNativeOverlapped(nativeOverlapped));

            Assert.True(helper.Wait());
        }
        finally
        {
            Overlapped.Free(nativeOverlapped);
        }
#pragma warning restore 618
    }
    [Fact]
    public unsafe static void PackPosTest1()
    {
        Overlapped ov = new Overlapped();
        var helper = new AsyncHelper();
        IOCompletionCallback callback = MyCallback(helper);

        NativeOverlapped* nativeOverlapped = ov.Pack(callback, null);
        try
        {
            Assert.True(nativeOverlapped != null);

            Assert.True(ThreadPool.UnsafeQueueNativeOverlapped(nativeOverlapped));

            Assert.True(helper.Wait());
        }
        finally
        {
            Overlapped.Free(nativeOverlapped);
        }
    }

    unsafe internal static IOCompletionCallback MyCallback(AsyncHelper helper)
    {
        IOCompletionCallback del = delegate (uint param1, uint param2, NativeOverlapped* overlapped)
        {

            Overlapped ov = new Overlapped();
            NativeOverlapped* nativeOverlapped2 = ov.Pack(helper.Callback, null);
            ThreadPool.UnsafeQueueNativeOverlapped(nativeOverlapped2);
        };

        return del;
    }
}

internal class AsyncHelper
{
    ManualResetEvent _event;


    internal AsyncHelper()
    {
        this._event = new ManualResetEvent(false);
    }

    internal bool Wait()
    {
        return this._event.WaitOne();
    }
    unsafe internal void Callback(uint errorCode, uint numBytes, NativeOverlapped* _overlapped)
    {
        try
        {
            this._event.Set();
        }
        finally
        {
            Overlapped.Free(_overlapped);
        }
    }
}
