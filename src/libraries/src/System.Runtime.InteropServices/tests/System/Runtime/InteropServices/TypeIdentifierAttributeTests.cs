// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class TypeIdentifierAttributetests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new TypeIdentifierAttribute();
            Assert.Null(attribute.Scope);
            Assert.Null(attribute.Identifier);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("scope", "identifier")]
        public void Ctor_Scope_Identifier(string scope, string identifier)
        {
            var attribute = new TypeIdentifierAttribute(scope, identifier);
            Assert.Equal(scope, attribute.Scope);
            Assert.Equal(identifier, attribute.Identifier);
        }
    }
}
