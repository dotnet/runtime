// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class PtrToStringUniTests
    {
        [Theory]
        [InlineData("", 0)]
        [InlineData("hello", 0)]
        [InlineData("hello", 1)]
        [InlineData("hello", 4)]
        public void PtrToStringUni_Length_Success(string s, int len)
        {
            IntPtr ptr = Marshal.StringToCoTaskMemUni(s);
            try
            {
                string result = Marshal.PtrToStringUni(ptr, len);
                Assert.Equal(s.Substring(0, len), result);
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [Fact]
        public void PtrToStringUni_ZeroPointer_ReturnsNull()
        {
            Assert.Null(Marshal.PtrToStringUni(IntPtr.Zero));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void PtrToStringUni_Win32AtomPointer_ReturnsNull()
        {
            // Windows Marshal has specific checks that does not do
            // anything if the ptr is less than 64K.
            Assert.Null(Marshal.PtrToStringUni((IntPtr)1));
        }

        [Fact]
        public void PtrToStringUni_ZeroPointer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.PtrToStringUni(IntPtr.Zero, 123));
        }

        [Fact]
        public void PtrToStringUni_NegativeLength_ThrowsArgumentOutOfRangeExeption()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException, ArgumentException>("len", null, () => Marshal.PtrToStringUni(new IntPtr(123), -77));
        }
    }
}
