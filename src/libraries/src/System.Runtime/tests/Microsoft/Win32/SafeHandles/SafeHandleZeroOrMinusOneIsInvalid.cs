// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Win32.SafeHandles;
using Xunit;

public static class SafeHandleZeroOrMinusOneIsInvalidTests
{
    [Fact]
    public static void SafeHandleMinusOneIsInvalidTest()
    {
        var sh = new TestSafeHandleMinusOneIsInvalid();
        Assert.True(sh.IsInvalid);
        sh.SetHandle(new IntPtr(-2));
        Assert.False(sh.IsInvalid);
        sh.SetHandle(new IntPtr(-1));
        Assert.True(sh.IsInvalid);
        sh.SetHandle(IntPtr.Zero);
        Assert.False(sh.IsInvalid);
    }

    [Fact]
    public static void SafeHandleZeroOrMinusOneIsInvalidTest()
    {
        var sh = new TestSafeHandleZeroOrMinusOneIsInvalid();
        Assert.True(sh.IsInvalid);
        sh.SetHandle(new IntPtr(-2));
        Assert.False(sh.IsInvalid);
        sh.SetHandle(new IntPtr(-1));
        Assert.True(sh.IsInvalid);
        sh.SetHandle(IntPtr.Zero);
        Assert.True(sh.IsInvalid);
        sh.SetHandle(new IntPtr(1));
        Assert.False(sh.IsInvalid);
    }

    private class TestSafeHandleMinusOneIsInvalid : SafeHandleMinusOneIsInvalid
    {
        public TestSafeHandleMinusOneIsInvalid() : base(true)
        {
        }

        protected override bool ReleaseHandle() => true;
        public new void SetHandle(IntPtr handle) => base.SetHandle(handle);
    }

    private class TestSafeHandleZeroOrMinusOneIsInvalid : SafeHandleZeroOrMinusOneIsInvalid
    {
        public TestSafeHandleZeroOrMinusOneIsInvalid() : base(true)
        {
        }

        protected override bool ReleaseHandle() => true;
        public new void SetHandle(IntPtr handle) => base.SetHandle(handle);
    }
}
