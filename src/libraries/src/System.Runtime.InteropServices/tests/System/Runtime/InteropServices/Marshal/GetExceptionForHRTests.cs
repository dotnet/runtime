// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class GetExceptionForHRTests
    {
        [Theory]
        [InlineData(unchecked((int)0x80020006))]
        [InlineData(unchecked((int)0x80020101))]
        [ActiveIssue(30866)]
        public void GetExceptionForHR_EqualsErrorCode(int err)
        {
            Exception ex = Marshal.GetExceptionForHR(err);
            Assert.Equal(err, ex.HResult);
        }
    }
}
