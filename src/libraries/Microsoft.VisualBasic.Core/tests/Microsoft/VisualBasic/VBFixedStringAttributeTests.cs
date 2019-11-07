// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class VBFixedStringAttributeTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(32767)]
        public void Ctor_Int(int length)
        {
            var attribute = new VBFixedStringAttribute(length);
            Assert.Equal(length, attribute.Length);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(32768)]
        public void Ctor_InvalidLength_ThrowsArgumentException(int length)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new VBFixedStringAttribute(length));
        }
    }
}
