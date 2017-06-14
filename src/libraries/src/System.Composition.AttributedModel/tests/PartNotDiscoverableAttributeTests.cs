// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Composition.Tests
{
    public class PartNotDiscoverableAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new PartNotDiscoverableAttribute();
            Assert.Equal(typeof(PartNotDiscoverableAttribute), attribute.TypeId);
        }
    }
}
