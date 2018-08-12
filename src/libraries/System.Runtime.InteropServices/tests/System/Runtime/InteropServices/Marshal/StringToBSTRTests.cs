// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class StringToBSTRTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("pizza")]
        [InlineData("pepperoni")]
        [InlineData("password")]
        [InlineData("P4ssw0rdAa1")]
        [InlineData("\uD800")]
        [InlineData("\uD800\uDC00")]
        [InlineData("\u1234")]
        [InlineData("\0")]
        [InlineData("abc\0def")]
        public void StringToBSTR_InvokePtrToStringBSTR_ReturnsExpected(string s)
        {
            IntPtr ptr = Marshal.StringToBSTR(s);
            try
            {
                Assert.NotEqual(IntPtr.Zero, ptr);  
                Assert.Equal(s, Marshal.PtrToStringBSTR(ptr));
            }
            finally
            {
                Marshal.ZeroFreeBSTR(ptr);
            }
        }

        [Fact]
        public void StringToBSTR_NullString_ReturnsZero()
        {
            Assert.Equal(IntPtr.Zero, Marshal.StringToBSTR(null));
        }
    }
}
