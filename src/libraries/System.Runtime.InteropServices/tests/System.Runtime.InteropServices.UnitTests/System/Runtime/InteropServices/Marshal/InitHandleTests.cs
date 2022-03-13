// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class InitHandleTests
    {
        [Fact]
        public void InitHandle_SetsHandle()
        {
            var safeHandle = new TestSafeHandle();
            nint underlyingHandle = 42;

            Marshal.InitHandle(safeHandle, underlyingHandle);

            Assert.Equal((IntPtr)underlyingHandle, safeHandle.DangerousGetHandle());
        }

        class TestSafeHandle : SafeHandle
        {
            public TestSafeHandle() : base(IntPtr.Zero, true) { }

            public override bool IsInvalid => handle == IntPtr.Zero;

            protected override bool ReleaseHandle() => true;
        }
    }
}
