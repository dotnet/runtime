// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class StructLayoutAttributeTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(4)]
        public void Ctor_ShortLayoutKind(short layoutKind)
        {
            var attribute = new StructLayoutAttribute(layoutKind);
            Assert.Equal((LayoutKind)layoutKind, attribute.Value);
        }

        [Theory]
        [InlineData((LayoutKind)(-1))]
        [InlineData(LayoutKind.Sequential)]
        [InlineData(LayoutKind.Auto)]
        public void Ctor_LayoutKind(LayoutKind layoutKind)
        {
            var attribute = new StructLayoutAttribute(layoutKind);
            Assert.Equal(layoutKind, attribute.Value);
        }
    }
}
