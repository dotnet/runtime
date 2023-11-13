// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Xunit;

public static class SafeHandleZeroOrMinusOneIsInvalidTests
{
    [Fact]
    public static void SafeHandleMinusOneIsInvalidTest()
    {
        var sh = new TestSafeHandleMinusOneIsInvalid();
        Assert.True(sh.IsInvalid);

        Marshal.InitHandle(sh, -2);
        Assert.False(sh.IsInvalid);

        Marshal.InitHandle(sh, -1);
        Assert.True(sh.IsInvalid);

        Marshal.InitHandle(sh, 0);
        Assert.False(sh.IsInvalid);
    }

    [Fact]
    public static void SafeHandleZeroOrMinusOneIsInvalidTest()
    {
        var sh = new TestSafeHandleZeroOrMinusOneIsInvalid();
        Assert.True(sh.IsInvalid);

        Marshal.InitHandle(sh, -2);
        Assert.False(sh.IsInvalid);

        Marshal.InitHandle(sh, -1);
        Assert.True(sh.IsInvalid);

        Marshal.InitHandle(sh, 0);
        Assert.True(sh.IsInvalid);

        Marshal.InitHandle(sh, 1);
        Assert.False(sh.IsInvalid);
    }

    private class TestSafeHandleMinusOneIsInvalid : SafeHandleMinusOneIsInvalid
    {
        public TestSafeHandleMinusOneIsInvalid() : base(true)
        {
        }

        protected override bool ReleaseHandle() => true;
    }

    private class TestSafeHandleZeroOrMinusOneIsInvalid : SafeHandleZeroOrMinusOneIsInvalid
    {
        public TestSafeHandleZeroOrMinusOneIsInvalid() : base(true)
        {
        }

        protected override bool ReleaseHandle() => true;
    }
}
