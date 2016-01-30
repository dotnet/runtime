// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
