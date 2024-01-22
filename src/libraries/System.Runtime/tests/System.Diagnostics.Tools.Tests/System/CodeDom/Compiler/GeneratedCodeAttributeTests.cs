// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class GeneratedCodeAttributeTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("Tool", "Version")]
        public void TestConstructor(string tool, string version)
        {
            GeneratedCodeAttribute gca = new GeneratedCodeAttribute(tool, version);

            Assert.Equal(tool, gca.Tool);
            Assert.Equal(version, gca.Version);
        }
    }
}
