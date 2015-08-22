// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
