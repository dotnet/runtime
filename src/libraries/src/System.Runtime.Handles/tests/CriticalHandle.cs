// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Xunit;

public partial class CriticalHandle_4000_Tests
{
    private class MyCriticalHandle : CriticalHandle
    {
        public MyCriticalHandle()
            : base(IntPtr.Zero)
        {
        }

        public MyCriticalHandle(IntPtr handle)
            : this()
        {
            SetHandle(handle);
        }

        public override bool IsInvalid
        {
            get { return this.handle == IntPtr.Zero; }
        }

        public bool IsReleased { get; private set; }

        protected override bool ReleaseHandle()
        {
            return this.IsReleased = true;
        }
    }

    [Fact]
    public static void CriticalHandle_invalid()
    {
        MyCriticalHandle mch = new MyCriticalHandle();
        Assert.False(mch.IsClosed);
        Assert.True(mch.IsInvalid);
        Assert.False(mch.IsReleased);

        mch.Dispose();
        Assert.True(mch.IsClosed);
        Assert.True(mch.IsInvalid);
        Assert.False(mch.IsReleased);
    }

    [Fact]
    public static void CriticalHandle_valid()
    {
        MyCriticalHandle mch = new MyCriticalHandle(new IntPtr(1));
        Assert.False(mch.IsClosed);
        Assert.False(mch.IsInvalid);
        Assert.False(mch.IsReleased);

        mch.Dispose();
        Assert.True(mch.IsClosed);
        Assert.False(mch.IsInvalid);
        Assert.True(mch.IsReleased);
    }

    [Fact]
    public static void CriticalHandle_invalid_close()
    {
        MyCriticalHandle mch = new MyCriticalHandle();
        mch.Close();
        Assert.True(mch.IsClosed);
        Assert.True(mch.IsInvalid);
        Assert.False(mch.IsReleased);
    }

    [Fact]
    public static void CriticalHandle_valid_close()
    {
        MyCriticalHandle mch = new MyCriticalHandle(new IntPtr(1));
        mch.Close();
        Assert.True(mch.IsClosed);
        Assert.False(mch.IsInvalid);
        Assert.True(mch.IsReleased);
    }
}
