// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyVersionAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        [InlineData("5.6.7.8.9")]
        public void Ctor_String(string version)
        {
            var attribute = new AssemblyVersionAttribute(version);
            Assert.Equal(version, attribute.Version);
        }
    }
}
