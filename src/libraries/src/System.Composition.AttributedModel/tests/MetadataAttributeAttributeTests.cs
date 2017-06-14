// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Composition.Tests
{
    public class MetadataAttributeAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new MetadataAttributeAttribute();
            Assert.Equal(typeof(MetadataAttributeAttribute), attribute.TypeId);
        }
    }
}
