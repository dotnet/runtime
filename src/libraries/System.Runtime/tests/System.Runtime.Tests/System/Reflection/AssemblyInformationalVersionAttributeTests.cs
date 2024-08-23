// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyInformationalVersionAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        [InlineData("3.4.5.6.7")]
        public void Ctor_String(string informationalVersion)
        {
            var attribute = new AssemblyInformationalVersionAttribute(informationalVersion);
            Assert.Equal(informationalVersion, attribute.InformationalVersion);
        }
    }
}
