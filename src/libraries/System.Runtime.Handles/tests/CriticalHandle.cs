// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        Assert.Equal(false, mch.IsClosed);
        Assert.Equal(true, mch.IsInvalid);
    }

    [Fact]
    public static void CriticalHandle_valid()
    {
        MyCriticalHandle mch = new MyCriticalHandle(new IntPtr(1));
        Assert.Equal(false, mch.IsClosed);
        Assert.Equal(false, mch.IsInvalid);
    }
}