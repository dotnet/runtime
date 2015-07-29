// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
