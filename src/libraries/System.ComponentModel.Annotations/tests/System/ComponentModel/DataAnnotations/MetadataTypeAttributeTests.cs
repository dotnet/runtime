// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class MetadataTypeAttributeTests
    {
        [Theory]
        [InlineData(typeof(int))]
        public void Ctor_Type(Type metadataClassType)
        {
            var attribute = new MetadataTypeAttribute(metadataClassType);
            Assert.Equal(metadataClassType, attribute.MetadataClassType);
        }

        [Fact]
        public void MetadataClassType_GetWithoutType_ThrowsInvalidOperationException()
        {
            var attribute = new MetadataTypeAttribute(null);
            Assert.Throws<InvalidOperationException>(() => attribute.MetadataClassType);
        }
    }
}
