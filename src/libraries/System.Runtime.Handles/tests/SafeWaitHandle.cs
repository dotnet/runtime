// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Win32.SafeHandles;
using Xunit;

public partial class SafeWaitHandle_4000_Tests
{
    [Fact]
    public static void SafeWaitHandle_invalid()
    {
        SafeWaitHandle swh = new SafeWaitHandle(IntPtr.Zero, false);
        Assert.Equal(false, swh.IsClosed);
        Assert.Equal(true, swh.IsInvalid);
    }

    [Fact]
    public static void SafeWaitHandle_valid()
    {
        SafeWaitHandle swh = new SafeWaitHandle(new IntPtr(1), true);
        Assert.Equal(false, swh.IsClosed);
        Assert.Equal(false, swh.IsInvalid);
    }
}
