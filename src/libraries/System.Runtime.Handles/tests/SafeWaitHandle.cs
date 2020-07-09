// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Win32.SafeHandles;
using Xunit;

public partial class SafeWaitHandleTests
{
    [Fact]
    public static void SafeWaitHandle_invalid()
    {
        SafeWaitHandle swh = new SafeWaitHandle(IntPtr.Zero, false);
        Assert.False(swh.IsClosed);
        Assert.True(swh.IsInvalid);
    }

    [Fact]
    public static void SafeWaitHandle_valid()
    {
        SafeWaitHandle swh = new SafeWaitHandle(new IntPtr(1), true);
        Assert.False(swh.IsClosed);
        Assert.False(swh.IsInvalid);

        // Prevent finalization. Closing of the bogus handle has unpredictable results.
        swh.SetHandleAsInvalid();
    }
}
