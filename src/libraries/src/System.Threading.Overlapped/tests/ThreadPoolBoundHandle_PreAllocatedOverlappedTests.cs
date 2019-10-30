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
    public unsafe void PreAllocatedOverlapped_NullAsCallback_ThrowsArgumentNullException()
    {
        AssertExtensions.Throws<ArgumentNullException>("callback", () =>
        {
            new PreAllocatedOverlapped(null, new object(), new byte[256]);
        });

        // Make sure the PreAllocatedOverlapped finalizer does the right thing in the case where the .ctor failed.
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Fact]
    public unsafe void PreAllocatedOverlapped_NullAsContext_DoesNotThrow()
    {
        using(new PreAllocatedOverlapped((_, __, ___) => { }, (object)null, new byte[256])) {}
    }

    [Fact]
    public unsafe void PreAllocatedOverlapped_NullAsPinData_DoesNotThrow()
    {
        using(new PreAllocatedOverlapped((_, __, ___) => { }, new object(), (byte[])null)) {}
    }

    [Fact]
    public unsafe void PreAllocatedOverlapped_EmptyArrayAsPinData_DoesNotThrow()
    {
        using(new PreAllocatedOverlapped((_, __, ___) => { }, new object(), new byte[0])) {}
    }

    [Fact]
    public unsafe void PreAllocatedOverlapped_NonBlittableTypeAsPinData_Throws()
    {
        AssertExtensions.Throws<ArgumentException>(null, () => new PreAllocatedOverlapped((_, __, ___) => { }, new object(), new NonBlittableType() { s = "foo" }));

        // Make sure the PreAllocatedOverlapped finalizer does the right thing in the case where the .ctor failed.
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Fact]
    public unsafe void PreAllocatedOverlapped_BlittableTypeAsPinData_DoesNotThrow()
    {
        using(new PreAllocatedOverlapped((_, __, ___) => { }, new object(), new BlittableType() { i = 42 })) {}
    }

    [Fact]
    public unsafe void PreAllocatedOverlapped_ObjectArrayAsPinData_DoesNotThrow()
    {
        object[] array = new object[]
        {
            new BlittableType() { i = 1 },
            new byte[5],
        };
        using(new PreAllocatedOverlapped((_, __, ___) => { }, new object(), array)) {}
    }

    [Fact]
    public unsafe void PreAllocatedOverlapped_ObjectArrayWithNonBlittableTypeAsPinData_Throws()
    {
        object[] array = new object[]
        {
            new NonBlittableType() { s = "foo" },
            new byte[5],
        };
        AssertExtensions.Throws<ArgumentException>(null, () => new PreAllocatedOverlapped((_, __, ___) => { }, new object(), array));

        // Make sure the PreAllocatedOverlapped finalizer does the right thing in the case where the .ctor failed.
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Fact]
    public unsafe void PreAllocatedOverlapped_ReturnedNativeOverlapped_InternalLowAndInternalHighSetToZero()
    {
        using(new PreAllocatedOverlapped((_, __, ___) => { }, new object(), new byte[256])) {}
    }

    [Fact]
    public unsafe void PreAllocatedOverlapped_ReturnedNativeOverlapped_OffsetLowAndOffsetHighSetToZero()
    {
        using(new PreAllocatedOverlapped((_, __, ___) => { }, new object(), new byte[256])) {}
    }
}
