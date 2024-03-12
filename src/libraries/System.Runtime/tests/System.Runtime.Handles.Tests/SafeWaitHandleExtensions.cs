// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Threading;
using Xunit;

public partial class SafeWaitHandleExtensionsTests
{
    public class MyWaitHandle : WaitHandle
    {
        public MyWaitHandle()
        { }
    }

    [Fact]
    public static void SafeWaitHandleExtensions_get()
    {
        var wh = new MyWaitHandle();

        var swh = wh.GetSafeWaitHandle();

        Assert.Equal(new IntPtr(-1), swh.DangerousGetHandle());
    }

    [Fact]
    public static void SafeWaitHandleExtensions_set()
    {
        var wh = new MyWaitHandle();

        var swhExpected = new SafeWaitHandle(new IntPtr(5), true);
        wh.SetSafeWaitHandle(swhExpected);

        var swh = wh.GetSafeWaitHandle();

        Assert.Equal(swhExpected, swh);

        wh.SetSafeWaitHandle(null);
        swh = wh.GetSafeWaitHandle();
        Assert.NotNull(swh);
        Assert.Equal(new IntPtr(-1), swh.DangerousGetHandle());
        
        // Prevent finalization. Closing of the bogus handle has unpredictable results.
        swhExpected.SetHandleAsInvalid();
    }

    [Fact]
    public static void SafeWaitHandleExtensions_nullWaitHandle()
    {
        WaitHandle wh = null;

        Assert.Throws<ArgumentNullException>(() => wh.GetSafeWaitHandle());
        Assert.Throws<ArgumentNullException>(() => wh.SetSafeWaitHandle(new SafeWaitHandle(IntPtr.Zero, false)));
    }
}
