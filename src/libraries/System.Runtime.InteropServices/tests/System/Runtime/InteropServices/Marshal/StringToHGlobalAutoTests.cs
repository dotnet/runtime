// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class StringToHGlobalAutoTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("Hello World")]
        public void StringToHGlobalAuto_NonNullString_Roundtrips(string s)
        {
            IntPtr ptr = Marshal.StringToHGlobalAuto(s);

            try
            {
                // Make sure the memory roundtrips.
                Assert.Equal(s, Marshal.PtrToStringAuto(ptr));
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [Fact]
        public void StringToHGlobalAuto_NullString_ReturnsZero()
        {
            Assert.Equal(IntPtr.Zero, Marshal.StringToHGlobalAuto(null));
        }
    }
}
