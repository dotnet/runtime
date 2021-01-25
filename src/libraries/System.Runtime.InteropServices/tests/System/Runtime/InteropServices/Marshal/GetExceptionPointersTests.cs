// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class GetExceptionPointersTests
    {
        [Fact]
        [SkipOnMono("Marshal.GetExceptionPointers will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void GetExceptionPointers_ReturnsExpected()
        {
            Assert.Equal(IntPtr.Zero, Marshal.GetExceptionPointers());
        }
    }
}
