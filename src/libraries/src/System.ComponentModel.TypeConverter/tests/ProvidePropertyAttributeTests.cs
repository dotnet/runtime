// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel
{
    public class ProvidePropertyAttributeTests
    {
        [Fact]
        public void Equals_SameNames()
        {
            var propertyName = "property";
            var receiverTypeName = "receiver";
            var firstAttribute = new ProvidePropertyAttribute(propertyName, receiverTypeName);
            var secondAttribute = new ProvidePropertyAttribute(propertyName, receiverTypeName);

            Assert.True(firstAttribute.Equals(secondAttribute));
        }

        [Fact]
        public void GetPropertyName()
        {
            var propertyName = "property";
            var attribute = new ProvidePropertyAttribute(propertyName, string.Empty);

            Assert.Equal(propertyName, attribute.PropertyName);
        }

        [Fact]
        public void GetReceiverTypeName()
        {
            var type = typeof(ProvidePropertyAttribute);
            var attribute = new ProvidePropertyAttribute(string.Empty, type);

            Assert.Equal(type.AssemblyQualifiedName, attribute.ReceiverTypeName);
        }
    }
}
