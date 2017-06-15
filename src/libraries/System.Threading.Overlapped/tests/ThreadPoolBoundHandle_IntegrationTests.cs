// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public partial class ThreadPoolBoundHandleTests
{
    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void SingleOperationOverSingleHandle()
    {
        const int DATA_SIZE = 2;

        SafeHandle handle = HandleFactory.CreateAsyncFileHandleForWrite(Path.Combine(TestDirectory, @"SingleOverlappedOverSingleHandle.tmp"));
        ThreadPoolBoundHandle boundHandle = ThreadPoolBoundHandle.BindHandle(handle);

        OverlappedContext result = new OverlappedContext();

        byte[] data = new byte[DATA_SIZE];
        data[0] = (byte)'A';
        data[1] = (byte)'B';

        NativeOverlapped* overlapped = boundHandle.AllocateNativeOverlapped(OnOverlappedOperationCompleted, result, data);

        fixed (byte* p = data)
        {
            int retval = DllImport.WriteFile(boundHandle.Handle, p, DATA_SIZE, IntPtr.Zero, overlapped);

            if (retval == 0)
            {
                Assert.Equal(DllImport.ERROR_IO_PENDING, Marshal.GetLastWin32Error());                
            }

            // Wait for overlapped operation to complete
            result.Event.WaitOne();
        }

        boundHandle.FreeNativeOverlapped(overlapped);
        boundHandle.Dispose();
        handle.Dispose();

        Assert.Equal(0, result.ErrorCode);
        Assert.Equal(DATA_SIZE, result.BytesWritten);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void MultipleOperationsOverSingleHandle()
    {
        const int DATA_SIZE = 2;

        SafeHandle handle = HandleFactory.CreateAsyncFileHandleForWrite(Path.Combine(TestDirectory, @"MultipleOperationsOverSingleHandle.tmp"));
        ThreadPoolBoundHandle boundHandle = ThreadPoolBoundHandle.BindHandle(handle);

        OverlappedContext result1 = new OverlappedContext();
        OverlappedContext result2 = new OverlappedContext();

        byte[] data1 = new byte[DATA_SIZE];
        data1[0] = (byte)'A';
        data1[1] = (byte)'B';


        byte[] data2 = new byte[DATA_SIZE];
        data2[0] = (byte)'C';
        data2[1] = (byte)'D';

        NativeOverlapped* overlapped1 = boundHandle.AllocateNativeOverlapped(OnOverlappedOperationCompleted, result1, data1);
        NativeOverlapped* overlapped2 = boundHandle.AllocateNativeOverlapped(OnOverlappedOperationCompleted, result2, data2);

        fixed (byte* p1 = data1, p2 = data2)
        {
            int retval = DllImport.WriteFile(boundHandle.Handle, p1, DATA_SIZE, IntPtr.Zero, overlapped1);

            if (retval == 0)
            {
                Assert.Equal(DllImport.ERROR_IO_PENDING, Marshal.GetLastWin32Error());
            }


            // Start the offset after the above write, so that it doesn't overwrite the previous write
            overlapped2->OffsetLow = DATA_SIZE;
            retval = DllImport.WriteFile(boundHandle.Handle, p2, DATA_SIZE, IntPtr.Zero, overlapped2);

            if (retval == 0)
            {
                Assert.Equal(DllImport.ERROR_IO_PENDING, Marshal.GetLastWin32Error());
            }

            // Wait for overlapped operations to complete
            WaitHandle.WaitAll(new WaitHandle[] { result1.Event, result2.Event });
        }

        boundHandle.FreeNativeOverlapped(overlapped1);
        boundHandle.FreeNativeOverlapped(overlapped2);
        boundHandle.Dispose();
        handle.Dispose();

        Assert.Equal(0, result1.ErrorCode);
        Assert.Equal(0, result2.ErrorCode);
        Assert.Equal(DATA_SIZE, result1.BytesWritten);
        Assert.Equal(DATA_SIZE, result2.BytesWritten);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void MultipleOperationsOverMultipleHandles()
    {
        const int DATA_SIZE = 2;

        SafeHandle handle1 = HandleFactory.CreateAsyncFileHandleForWrite(Path.Combine(TestDirectory, @"MultipleOperationsOverMultipleHandle1.tmp"));
        SafeHandle handle2 = HandleFactory.CreateAsyncFileHandleForWrite(Path.Combine(TestDirectory, @"MultipleOperationsOverMultipleHandle2.tmp"));
        ThreadPoolBoundHandle boundHandle1 = ThreadPoolBoundHandle.BindHandle(handle1);
        ThreadPoolBoundHandle boundHandle2 = ThreadPoolBoundHandle.BindHandle(handle2);

        OverlappedContext result1 = new OverlappedContext();
        OverlappedContext result2 = new OverlappedContext();

        byte[] data1 = new byte[DATA_SIZE];
        data1[0] = (byte)'A';
        data1[1] = (byte)'B';


        byte[] data2 = new byte[DATA_SIZE];
        data2[0] = (byte)'C';
        data2[1] = (byte)'D';

        PreAllocatedOverlapped preAlloc1 = new PreAllocatedOverlapped(OnOverlappedOperationCompleted, result1, data1);
        PreAllocatedOverlapped preAlloc2 = new PreAllocatedOverlapped(OnOverlappedOperationCompleted, result2, data2);

        for (int i = 0; i < 10; i++)
        {
            NativeOverlapped* overlapped1 = boundHandle1.AllocateNativeOverlapped(preAlloc1);
            NativeOverlapped* overlapped2 = boundHandle2.AllocateNativeOverlapped(preAlloc2);

            fixed (byte* p1 = data1, p2 = data2)
            {
                int retval = DllImport.WriteFile(boundHandle1.Handle, p1, DATA_SIZE, IntPtr.Zero, overlapped1);

                if (retval == 0)
                {
                    Assert.Equal(DllImport.ERROR_IO_PENDING, Marshal.GetLastWin32Error());
                }


                retval = DllImport.WriteFile(boundHandle2.Handle, p2, DATA_SIZE, IntPtr.Zero, overlapped2);

                if (retval == 0)
                {
                    Assert.Equal(DllImport.ERROR_IO_PENDING, Marshal.GetLastWin32Error());
                }

                // Wait for overlapped operations to complete
                WaitHandle.WaitAll(new WaitHandle[] { result1.Event, result2.Event });
            }

            boundHandle1.FreeNativeOverlapped(overlapped1);
            boundHandle2.FreeNativeOverlapped(overlapped2);

            result1.Event.Reset();
            result2.Event.Reset();

            Assert.Equal(0, result1.ErrorCode);
            Assert.Equal(0, result2.ErrorCode);
            Assert.Equal(DATA_SIZE, result1.BytesWritten);
            Assert.Equal(DATA_SIZE, result2.BytesWritten);
        }

        boundHandle1.Dispose();
        boundHandle2.Dispose();
        preAlloc1.Dispose();
        preAlloc2.Dispose();
        handle1.Dispose();
        handle2.Dispose();
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Active Issue dotnet/corefx#13343")]
    public unsafe void FlowsAsyncLocalsToCallback()
    {   // Makes sure that we flow async locals to callback

        const int DATA_SIZE = 2;

        SafeHandle handle = HandleFactory.CreateAsyncFileHandleForWrite(Path.Combine(TestDirectory, @"AsyncLocal.tmp"));
        ThreadPoolBoundHandle boundHandle = ThreadPoolBoundHandle.BindHandle(handle);

        OverlappedContext context = new OverlappedContext();

        byte[] data = new byte[DATA_SIZE];

        AsyncLocal<int> asyncLocal = new AsyncLocal<int>();
        asyncLocal.Value = 10;

        int? result  = null;
        IOCompletionCallback callback = (_, __, ___) => {
            
            result = asyncLocal.Value;
            OnOverlappedOperationCompleted(_, __, ___);
        };

        NativeOverlapped* overlapped = boundHandle.AllocateNativeOverlapped(callback, context, data);

        fixed (byte* p = data)
        {
            int retval = DllImport.WriteFile(boundHandle.Handle, p, DATA_SIZE, IntPtr.Zero, overlapped);

            if (retval == 0)
            {
                Assert.Equal(DllImport.ERROR_IO_PENDING, Marshal.GetLastWin32Error());
            }

            // Wait for overlapped operation to complete
            context.Event.WaitOne();
        }

        boundHandle.FreeNativeOverlapped(overlapped);
        boundHandle.Dispose();
        handle.Dispose();

        Assert.Equal(10, result);
    }

    private static unsafe void OnOverlappedOperationCompleted(uint errorCode, uint numBytes, NativeOverlapped* overlapped)
    {
        OverlappedContext result = (OverlappedContext)ThreadPoolBoundHandle.GetNativeOverlappedState(overlapped);
        result.ErrorCode = (int)errorCode;
        result.BytesWritten = (int)numBytes;

        // Signal original thread to indicate overlapped completed
        result.Event.Set();
    }
    
    private class OverlappedContext
    {
        public readonly ManualResetEvent Event = new ManualResetEvent(false);
        public int ErrorCode;
        public int BytesWritten;
    }
}
