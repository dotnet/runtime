// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Composition.AttributeModel.Tests
{
    public class SharedAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new SharedAttribute();
            Assert.Null(attribute.SharingBoundary);
            Assert.Equal("SharingBoundary", attribute.Name);
            Assert.Null(attribute.Value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Name")]
        public void Ctor_SharingBoundaryName(string sharingBoundaryName)
        {
            var attribute = new SharedAttribute(sharingBoundaryName);
            Assert.Equal(sharingBoundaryName, attribute.SharingBoundary);
            Assert.Equal("SharingBoundary", attribute.Name);
            Assert.Equal(sharingBoundaryName, attribute.Value);
        }
    }
}
