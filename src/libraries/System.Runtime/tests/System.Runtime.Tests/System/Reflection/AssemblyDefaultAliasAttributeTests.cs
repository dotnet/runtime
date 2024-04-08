// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyDefaultAliasAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("defaultAlias")]
        public void Ctor_String(string defaultAlias)
        {
            var attribute = new AssemblyDefaultAliasAttribute(defaultAlias);
            Assert.Equal(defaultAlias, attribute.DefaultAlias);
        }
    }
}
