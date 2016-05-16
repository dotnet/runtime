// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class DefaultPropertyAttributeTests
    {
        [Fact]
        public static void Equals_SameName()
        {
            var name = "name";
            var firstAttribute = new DefaultPropertyAttribute(name);
            var secondAttribute = new DefaultPropertyAttribute(name);

            Assert.True(firstAttribute.Equals(secondAttribute));
        }

        [Fact]
        public static void GetName()
        {
            var name = "name";
            var attribute = new DefaultPropertyAttribute(name);

            Assert.Equal(name, attribute.Name);
        }
    }
}
