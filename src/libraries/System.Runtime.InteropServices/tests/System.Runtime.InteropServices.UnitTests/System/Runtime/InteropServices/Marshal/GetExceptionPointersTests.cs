// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class GetExceptionPointersTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMarshalGetExceptionPointersSupported))]
        public void GetExceptionPointers_ReturnsExpected()
        {
            Assert.Equal(IntPtr.Zero, Marshal.GetExceptionPointers());
        }
    }
}
