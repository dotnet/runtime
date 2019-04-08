// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class PtrToStringUTF8
    {
        [Theory]
        [InlineData("", 0)]
        [InlineData("hello", 0)]
        [InlineData("hello", 1)]
        [InlineData("hello", 4)]
        public void PtrToStringUTF8_Length_Success(string s, int len)
        {
            IntPtr ptr = Marshal.StringToCoTaskMemUTF8(s);
            try
            {
                string result = Marshal.PtrToStringUTF8(ptr, len);
                Assert.Equal(s.Substring(0, len), result);
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [Fact]
        public void PtrToStringUTF8_ZeroPointer_ReturnsNull()
        {
            Assert.Null(Marshal.PtrToStringUTF8(IntPtr.Zero));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void PtrToStringUTF8_Win32AtomPointer_ReturnsNull()
        {
            // Windows Marshal has specific checks that does not do
            // anything if the ptr is less than 64K.
            Assert.Null(Marshal.PtrToStringUTF8((IntPtr)1));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void PtrToStringUTF8_ZeroPointer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.PtrToStringUTF8(IntPtr.Zero, 123));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void PtrToStringUTF8_NegativeLength_ThrowsArgumentExeption()
        {
            AssertExtensions.Throws<ArgumentException>("byteLen", null, () => Marshal.PtrToStringUTF8(new IntPtr(123), -77));
        }
    }
}
