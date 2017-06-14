// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Composition.Tests
{
    public class ImportAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new ImportAttribute();
            Assert.Null(attribute.ContractName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("ContractName")]
        public void Ctor_ContractName(string contractName)
        {
            var attribute = new ImportAttribute(contractName);
            Assert.Equal(contractName, attribute.ContractName);
        }
    }
}
