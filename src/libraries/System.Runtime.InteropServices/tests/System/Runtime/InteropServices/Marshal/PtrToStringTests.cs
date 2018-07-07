// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class PtrToStringTests
    {
        [Fact]
        public void PtrToStringAnsi()
        {
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.PtrToStringAnsi(IntPtr.Zero, 123));
            Assert.Throws<ArgumentException>(() => Marshal.PtrToStringAnsi(new IntPtr(123), -77));
        }

        [Fact]
        public void PtrToStringUni()
        {
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.PtrToStringUni(IntPtr.Zero, 123));
            Assert.Throws<ArgumentException>(() => Marshal.PtrToStringUni(new IntPtr(123), -77));
        }

        [Fact]
        public void PtrToStringAuto()
        {
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.PtrToStringAuto(IntPtr.Zero, 123));
            Assert.Throws<ArgumentException>(() => Marshal.PtrToStringAuto(new IntPtr(123), -77));
        }

        [Fact]
        public void PtrToStringBSTR()
        {
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.PtrToStringBSTR(IntPtr.Zero));
        }

#if netcoreapp
        [Fact]
        public void PtrToStringUTF8()
        {
            Assert.Null(Marshal.PtrToStringUTF8(IntPtr.Zero, 123));
            Assert.Throws<ArgumentOutOfRangeException>(() => Marshal.PtrToStringUTF8(new IntPtr(123), -77));
        }
#endif
    }
}
