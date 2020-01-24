// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Microsoft.DotNet.XUnitExtensions.Attributes;

namespace System.Runtime.InteropServices.Tests
{
    public class GetExceptionPointersTests
    {
        [Fact]
        [SkipOnMono("https://github.com/mono/mono/issues/15085 - Marshal Methods WILL NOT BE Implemented in MonoVM")]
        public void GetExceptionPointers_ReturnsExpected()
        {
            Assert.Equal(IntPtr.Zero, Marshal.GetExceptionPointers());
        }
    }
}