// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Win32.SafeHandles;
using Xunit;

public static class CriticalHandleZeroOrMinusOneIsInvalidTests
{
    [Fact]
    public static void CriticalHandleMinusOneIsInvalidTest()
    {
        var ch = new TestCriticalHandleMinusOneIsInvalid();
        Assert.True(ch.IsInvalid);
        ch.SetHandle(new IntPtr(-2));
        Assert.False(ch.IsInvalid);
        ch.SetHandle(new IntPtr(-1));
        Assert.True(ch.IsInvalid);
        ch.SetHandle(IntPtr.Zero);
        Assert.False(ch.IsInvalid);
    }

    [Fact]
    public static void CriticalHandleZeroOrMinusOneIsInvalidTest()
    {
        var ch = new TestCriticalHandleZeroOrMinusOneIsInvalid();
        Assert.True(ch.IsInvalid);
        ch.SetHandle(new IntPtr(-2));
        Assert.False(ch.IsInvalid);
        ch.SetHandle(new IntPtr(-1));
        Assert.True(ch.IsInvalid);
        ch.SetHandle(IntPtr.Zero);
        Assert.True(ch.IsInvalid);
        ch.SetHandle(new IntPtr(1));
        Assert.False(ch.IsInvalid);
    }

    private class TestCriticalHandleMinusOneIsInvalid : CriticalHandleMinusOneIsInvalid
    {
        public TestCriticalHandleMinusOneIsInvalid()
        {
        }

        protected override bool ReleaseHandle() => true;
        public new void SetHandle(IntPtr handle) => base.SetHandle(handle);
    }

    private class TestCriticalHandleZeroOrMinusOneIsInvalid : CriticalHandleZeroOrMinusOneIsInvalid
    {
        public TestCriticalHandleZeroOrMinusOneIsInvalid()
        {
        }

        protected override bool ReleaseHandle() => true;
        public new void SetHandle(IntPtr handle) => base.SetHandle(handle);
    }
}
