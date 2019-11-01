// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class StringToCoTaskMemAutoTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("Hello World")]
        public void StringToCoTaskMemAuto_NonNullString_Roundtrips(string s)
        {
            IntPtr ptr = Marshal.StringToCoTaskMemAuto(s);
            try
            {
                Assert.NotEqual(IntPtr.Zero, ptr);
                // Make sure the memory roundtrips.
                Assert.Equal(s, Marshal.PtrToStringAuto(ptr));
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [Fact]
        public void StringToCoTaskMemAuto_NullString_ReturnsZero()
        {
            Assert.Equal(IntPtr.Zero, Marshal.StringToCoTaskMemAuto(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void StringToCoTaskMemAuto_PtrToStringAuto_ReturnsExpected(int len)
        {
            string s = "Hello World";
            IntPtr ptr = Marshal.StringToCoTaskMemAuto(s);
            try
            {
                string actual = Marshal.PtrToStringAuto(ptr, len);
                Assert.Equal(s.Substring(0, len), actual);
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }
    }
}
