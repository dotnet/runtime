// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Resources.Tests
{
    public static class NeutralResourcesLanguageAttributeTests
    {
        [Theory]
        [InlineData("en-us")]
        [InlineData("de-DE")]
        [InlineData("fr-FR")]
        [InlineData("")]
        public static void ConstructorBasic(string cultureName)
        {
            NeutralResourcesLanguageAttribute nrla = new NeutralResourcesLanguageAttribute(cultureName);
            Assert.Equal(cultureName, nrla.CultureName);
        }

        [Fact]
        public static void ConstructorArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new NeutralResourcesLanguageAttribute(null));
        }
    }
}
