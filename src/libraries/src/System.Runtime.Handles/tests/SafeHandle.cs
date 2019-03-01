// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Xunit;

public partial class SafeHandle_4000_Tests
{
    private class MySafeHandle : SafeHandle
    {
        public MySafeHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public MySafeHandle(IntPtr handle)
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
    public static void SafeHandle_invalid()
    {
        MySafeHandle mch = new MySafeHandle();
        Assert.False(mch.IsClosed);
        Assert.True(mch.IsInvalid);
        Assert.False(mch.IsReleased);

        mch.Dispose();
        Assert.True(mch.IsClosed);
        Assert.True(mch.IsInvalid);
        Assert.False(mch.IsReleased);
    }

    [Fact]
    public static void SafeHandle_valid()
    {
        MySafeHandle mch = new MySafeHandle(new IntPtr(1));
        Assert.False(mch.IsClosed);
        Assert.False(mch.IsInvalid);
        Assert.False(mch.IsReleased);

        mch.Dispose();
        Assert.True(mch.IsClosed);
        Assert.False(mch.IsInvalid);
        Assert.True(mch.IsReleased);
    }

    [Fact]
    public static void SafeHandle_invalid_close()
    {
        MySafeHandle mch = new MySafeHandle();
        mch.Close();
        Assert.True(mch.IsClosed);
        Assert.True(mch.IsInvalid);
        Assert.False(mch.IsReleased);
    }

    [Fact]
    public static void SafeHandle_valid_close()
    {
        MySafeHandle mch = new MySafeHandle(new IntPtr(1));
        mch.Close();
        Assert.True(mch.IsClosed);
        Assert.False(mch.IsInvalid);
        Assert.True(mch.IsReleased);
    }

    [DllImport("Kernel32", SetLastError = true)]
    private extern static void SetLastError(int error);

    private class LastErrorSafeHandle : SafeHandle
    {
        internal LastErrorSafeHandle(IntPtr h)
            : base(h, true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            SetLastError(-1);
            return true;
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void SafeHandle_DangerousReleasePreservesLastError()
    {
        LastErrorSafeHandle handle = new LastErrorSafeHandle((IntPtr)1);

        bool success = false;
        handle.DangerousAddRef(ref success);
        handle.Dispose();

        SetLastError(42);
        handle.DangerousRelease();

        int error = Marshal.GetLastWin32Error();
        Assert.Equal(42, error);
    }
}
