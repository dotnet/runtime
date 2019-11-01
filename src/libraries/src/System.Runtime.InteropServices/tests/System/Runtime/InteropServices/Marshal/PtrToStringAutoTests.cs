// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class PtrToStringAutoTests
    {
        [Fact]
        public void PtrToStringAuto_ZeroPtrNoLength_ReturnsNull()
        {
            Assert.Null(Marshal.PtrToStringAuto(IntPtr.Zero));
        }

        [Fact]
        public void PtrToStringAuto_ZeroPtrWithLength_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.PtrToStringAuto(IntPtr.Zero, 0));
        }

        [Fact]
        public void PtrToStringAuto_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            string s = "Hello World";
            IntPtr ptr = Marshal.StringToCoTaskMemAuto(s);
            try
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException, ArgumentException>(() => Marshal.PtrToStringAuto(ptr, -1));
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }
    }
}
