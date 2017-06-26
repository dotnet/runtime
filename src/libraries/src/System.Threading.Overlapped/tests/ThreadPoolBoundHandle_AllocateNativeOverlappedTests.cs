// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public partial class ThreadPoolBoundHandleTests
{
    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_NullAsCallback_ThrowsArgumentNullException()
    {
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            AssertExtensions.Throws<ArgumentNullException>("callback", () =>
            {
                handle.AllocateNativeOverlapped(null, new object(), new byte[256]);
            });
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_PreAllocated_ThrowsArgumentNullException()
    {
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            AssertExtensions.Throws<ArgumentNullException>("preAllocated", () =>
            {
                handle.AllocateNativeOverlapped((PreAllocatedOverlapped)null);
            });
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_NullAsContext_DoesNotThrow()
    {
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            NativeOverlapped* result = handle.AllocateNativeOverlapped((_, __, ___) => { }, (object)null, new byte[256]);

            Assert.True(result != null);

            handle.FreeNativeOverlapped(result);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_NullAsPinData_DoesNotThrow()
    {
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            NativeOverlapped* result = handle.AllocateNativeOverlapped((_, __, ___) => { }, new object(), (byte[])null);

            Assert.True(result != null);

            handle.FreeNativeOverlapped(result);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_EmptyArrayAsPinData_DoesNotThrow()
    {
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            NativeOverlapped* result = handle.AllocateNativeOverlapped((_, __, ___) => { }, new object(), new byte[0]);

            Assert.True(result != null);

            handle.FreeNativeOverlapped(result);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_NonBlittableTypeAsPinData_Throws()
    {
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            AssertExtensions.Throws<ArgumentException>(null, () => handle.AllocateNativeOverlapped((_, __, ___) => { }, new object(), new NonBlittableType() { s = "foo" }));
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_BlittableTypeAsPinData_DoesNotThrow()
    {
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            NativeOverlapped* result = handle.AllocateNativeOverlapped((_, __, ___) => { }, new object(), new BlittableType() { i = 42 });

            Assert.True(result != null);

            handle.FreeNativeOverlapped(result);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_ObjectArrayAsPinData_DoesNotThrow()
    {
        object[] array = new object[]
        {
            new BlittableType() { i = 1 },
            new byte[5],
        };
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            NativeOverlapped* result = handle.AllocateNativeOverlapped((_, __, ___) => { }, new object(), array);

            Assert.True(result != null);

            handle.FreeNativeOverlapped(result);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_ObjectArrayWithNonBlittableTypeAsPinData_Throws()
    {
        object[] array = new object[]
        {
            new NonBlittableType() { s = "foo" },
            new byte[5],
        };
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            AssertExtensions.Throws<ArgumentException>(null, () => handle.AllocateNativeOverlapped((_, __, ___) => { }, new object(), array));
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_ReturnedNativeOverlapped_AllFieldsZero()
    {
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            NativeOverlapped* overlapped = handle.AllocateNativeOverlapped((_, __, ___) => { }, new object(), new byte[256]);

            Assert.Equal(IntPtr.Zero, overlapped->InternalLow);
            Assert.Equal(IntPtr.Zero, overlapped->InternalHigh);
            Assert.Equal(0, overlapped->OffsetLow);
            Assert.Equal(0, overlapped->OffsetHigh);
            Assert.Equal(IntPtr.Zero, overlapped->EventHandle);

            handle.FreeNativeOverlapped(overlapped);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_PreAllocated_ReturnedNativeOverlapped_AllFieldsZero()
    {
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            using(PreAllocatedOverlapped preAlloc = new PreAllocatedOverlapped((_, __, ___) => { }, new object(), new byte[256]))
            {
                NativeOverlapped* overlapped = handle.AllocateNativeOverlapped(preAlloc);

                Assert.Equal(IntPtr.Zero, overlapped->InternalLow);
                Assert.Equal(IntPtr.Zero, overlapped->InternalHigh);
                Assert.Equal(0, overlapped->OffsetLow);
                Assert.Equal(0, overlapped->OffsetHigh);
                Assert.Equal(IntPtr.Zero, overlapped->EventHandle);

                handle.FreeNativeOverlapped(overlapped);
            }
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_PossibleReusedReturnedNativeOverlapped_OffsetLowAndOffsetHighSetToZero()
    {   // The CLR reuses NativeOverlapped underneath, check to make sure that they reset fields back to zero

        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            NativeOverlapped* overlapped = handle.AllocateNativeOverlapped((_, __, ___) => { }, new object(), new byte[256]);
            overlapped->OffsetHigh = 1;
            overlapped->OffsetLow = 1;
            handle.FreeNativeOverlapped(overlapped);

            overlapped = handle.AllocateNativeOverlapped((errorCode, numBytes, overlap) => { }, new object(), new byte[256]);

            Assert.Equal(IntPtr.Zero, overlapped->InternalLow);
            Assert.Equal(IntPtr.Zero, overlapped->InternalHigh);
            Assert.Equal(0, overlapped->OffsetLow);
            Assert.Equal(0, overlapped->OffsetHigh);
            Assert.Equal(IntPtr.Zero, overlapped->EventHandle);

            handle.FreeNativeOverlapped(overlapped);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    [ActiveIssue("https://github.com/dotnet/corefx/issues/18058", TargetFrameworkMonikers.Uap)]
    public unsafe void AllocateNativeOverlapped_PreAllocated_ReusedReturnedNativeOverlapped_OffsetLowAndOffsetHighSetToZero()
    {   // The CLR reuses NativeOverlapped underneath, check to make sure that they reset fields back to zero

        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            PreAllocatedOverlapped preAlloc = new PreAllocatedOverlapped((_, __, ___) => { }, new object(), new byte[256]);
            NativeOverlapped* overlapped = handle.AllocateNativeOverlapped(preAlloc);
            overlapped->OffsetHigh = 1;
            overlapped->OffsetLow = 1;
            handle.FreeNativeOverlapped(overlapped);

            overlapped = handle.AllocateNativeOverlapped(preAlloc);

            Assert.Equal(IntPtr.Zero, overlapped->InternalLow);
            Assert.Equal(IntPtr.Zero, overlapped->InternalHigh);
            Assert.Equal(0, overlapped->OffsetLow);
            Assert.Equal(0, overlapped->OffsetHigh);
            Assert.Equal(IntPtr.Zero, overlapped->EventHandle);

            handle.FreeNativeOverlapped(overlapped);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_WhenDisposed_ThrowsObjectDisposedException()
    {
        ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle();
        handle.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            handle.AllocateNativeOverlapped((_, __, ___) => { }, new object(), new byte[256]);
        });
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_PreAllocated_WhenDisposed_ThrowsObjectDisposedException()
    {
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            PreAllocatedOverlapped preAlloc = new PreAllocatedOverlapped(delegate { }, null, null);
            preAlloc.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                handle.AllocateNativeOverlapped(preAlloc);
            });
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_PreAllocated_WhenHandleDisposed_ThrowsObjectDisposedException()
    {
        ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle();
        handle.Dispose();

        PreAllocatedOverlapped preAlloc = new PreAllocatedOverlapped(delegate { }, null, null);

        Assert.Throws<ObjectDisposedException>(() =>
        {
            handle.AllocateNativeOverlapped(preAlloc);
        });
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void AllocateNativeOverlapped_PreAllocated_WhenAlreadyAllocated_ThrowsArgumentException()
    {
        using(ThreadPoolBoundHandle handle = CreateThreadPoolBoundHandle())
        {
            using(PreAllocatedOverlapped preAlloc = new PreAllocatedOverlapped(delegate { }, null, null))
            {
                NativeOverlapped* overlapped = handle.AllocateNativeOverlapped(preAlloc);

                AssertExtensions.Throws<ArgumentException>("preAllocated", () =>
                {
                    handle.AllocateNativeOverlapped(preAlloc);
                });

                handle.FreeNativeOverlapped(overlapped);
            }
        }
    }
}
