// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Design.Serialization.Tests
{
    public class DefaultSerializationProviderAttributeTests
    {
        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(DefaultSerializationProviderAttributeTests))]
        public void Ctor_ProviderType(Type providerType)
        {
            var attribute = new DefaultSerializationProviderAttribute(providerType);
            Assert.Equal(providerType.AssemblyQualifiedName, attribute.ProviderTypeName);
        }

        [Fact]
        public void Ctor_NullProviderType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("providerType", () => new DefaultSerializationProviderAttribute((Type)null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("Name")]
        public void Ctor_Name(string providerTypeName)
        {
            var attribute = new DefaultSerializationProviderAttribute(providerTypeName);
            Assert.Same(providerTypeName, attribute.ProviderTypeName);
        }

        [Fact]
        public void Ctor_NullProviderTypeName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("providerTypeName", () => new DefaultSerializationProviderAttribute((string)null));
        }
    }
}
