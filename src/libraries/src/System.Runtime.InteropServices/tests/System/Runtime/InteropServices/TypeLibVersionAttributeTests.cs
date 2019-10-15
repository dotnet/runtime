// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class TypeLibVersionAttributeTests
    {
        [Theory]
        [InlineData(-1, -2)]
        [InlineData(0, 0)]
        [InlineData(1, 2)]
        public void Ctor_MajorVersion_MinorVersion(int majorVersion, int minorVersion)
        {
            var attribute = new TypeLibVersionAttribute(majorVersion, minorVersion);
            Assert.Equal(majorVersion, attribute.MajorVersion);
            Assert.Equal(minorVersion, attribute.MinorVersion);
        }
    }
}
