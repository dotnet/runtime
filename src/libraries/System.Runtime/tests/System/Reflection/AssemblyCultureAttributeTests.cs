// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyCultureAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Czech")]
        public void Ctor_String(string culture)
        {
            var attribute = new AssemblyCultureAttribute(culture);
            Assert.Equal(culture, attribute.Culture);
        }
    }
}
