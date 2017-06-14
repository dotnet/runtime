// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Composition.Tests
{
    public class ExportAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new ExportAttribute();
            Assert.Null(attribute.ContractName);
            Assert.Null(attribute.ContractType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("ContractName")]
        public void Ctor_ContractName(string contractName)
        {
            var attribute = new ExportAttribute(contractName);
            Assert.Equal(contractName, attribute.ContractName);
            Assert.Null(attribute.ContractType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(string))]
        public void Ctor_ContractName(Type contractType)
        {
            var attribute = new ExportAttribute(contractType);
            Assert.Null(attribute.ContractName);
            Assert.Equal(contractType, attribute.ContractType);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("ContractName", typeof(string))]
        public void Ctor_ContractName_ContractType(string contractName, Type contractType)
        {
            var attribute = new ExportAttribute(contractName, contractType);
            Assert.Equal(contractName, attribute.ContractName);
            Assert.Equal(contractType, attribute.ContractType);
        }
    }
}
