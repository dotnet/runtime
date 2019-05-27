// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Composition.Registration.Tests
{
    public class ImportBuilderTests
    {
        interface IFoo { }
        
        class FooImpl { }
        
        [Fact]
        public void AsContractTypeOfT_SetsContractType()
        {
            var builder = new ImportBuilder();
            builder.AsContractType<IFoo>();
            
            ImportAttribute importAtt = GetImportAttribute(builder);
            Assert.Equal(typeof(IFoo), importAtt.ContractType);
            Assert.Null(importAtt.ContractName);
            Assert.False(importAtt.AllowDefault);
            Assert.False(importAtt.AllowRecomposition);
        }
        
        [Fact]
        public void AsContractType_SetsContractType()
        {
            var builder = new ImportBuilder();
            builder.AsContractType(typeof(IFoo));
            
            ImportAttribute importAtt = GetImportAttribute(builder);
            Assert.Equal(typeof(IFoo), importAtt.ContractType);
            Assert.Null(importAtt.ContractName);
            Assert.False(importAtt.AllowDefault);
            Assert.False(importAtt.AllowRecomposition);
        }

        [Fact]
        public void AsContractName_SetsContractName()
        {
            var builder = new ImportBuilder();
            builder.AsContractName("hey");
            
            ImportAttribute importAtt = GetImportAttribute(builder);
            Assert.Equal("hey", importAtt.ContractName);
            Assert.False(importAtt.AllowDefault);
            Assert.False(importAtt.AllowRecomposition);
            Assert.Null(importAtt.ContractType);
    }

        [Fact]
        public void RequiredCreationPolicy_SetsRequiredCreationPolicyProperty()
        {
            var builder = new ImportBuilder();
            builder.RequiredCreationPolicy(CreationPolicy.NonShared);
            
            ImportAttribute importAtt = GetImportAttribute(builder);
            Assert.Equal(CreationPolicy.NonShared, importAtt.RequiredCreationPolicy);
            Assert.False(importAtt.AllowDefault);
            Assert.False(importAtt.AllowRecomposition);
            Assert.Null(importAtt.ContractType);
            Assert.Null(importAtt.ContractName);
        }

        [Fact]
        public void AllowDefault_SetsAllowDefaultProperty()
        {
            var builder = new ImportBuilder();
            builder.AllowDefault();
            
            ImportAttribute importAtt = GetImportAttribute(builder);
            Assert.True(importAtt.AllowDefault);
            Assert.False(importAtt.AllowRecomposition);
            Assert.Null(importAtt.ContractType);
            Assert.Null(importAtt.ContractName);
            }

        [Fact]
        public void AllowRecomposition_SetsAllowRecompositionProperty()
        {
            var builder = new ImportBuilder();
            builder.AllowRecomposition();
            
            ImportAttribute importAtt = GetImportAttribute(builder);
            Assert.True(importAtt.AllowRecomposition);
            Assert.Null(importAtt.ContractType);
            Assert.Null(importAtt.ContractName);
        }

        [Fact]
        public void AsContractName_AndContractType_SetsContractNameAndType()
        {
            var builder = new ImportBuilder();
            builder.AsContractName("hey");
            builder.AsContractType(typeof(IFoo));
            
            ImportAttribute importAtt = GetImportAttribute(builder);
            Assert.Equal("hey", importAtt.ContractName);
            Assert.Equal(typeof(IFoo), importAtt.ContractType);
        }

        [Fact]
        public void AsMany_ChangesGeneratedAttributeToImportMany()
        {
            var builder = new ImportBuilder();
            builder.AsMany();

            var list = new List<Attribute>();
            builder.BuildAttributes(typeof(FooImpl), ref list);
            Assert.Equal(1, list.Count);
            var att = list[0] as ImportManyAttribute;
            Assert.NotNull(att);
            Assert.Null(att.ContractName);
            Assert.Null(att.ContractType);
        }

        [Fact]
        public void AsMany_And_ContractName_ChangesGeneratedAttributeToImportMany()
        {
            var builder = new ImportBuilder();
            builder.AsContractName("hey");
            builder.AsMany();

            var list = new List<Attribute>();
            builder.BuildAttributes(typeof(FooImpl), ref list);
            Assert.Equal(1, list.Count);
            var att = list[0] as ImportManyAttribute;
            Assert.NotNull(att);
            Assert.Equal("hey", att.ContractName);
            Assert.Null(att.ContractType);
        }

        private static ImportAttribute GetImportAttribute(ImportBuilder builder)
        {
            var list = new List<Attribute>();
            builder.BuildAttributes(typeof(FooImpl), ref list);
            Assert.Equal(1, list.Count);

            return list[0] as ImportAttribute;
        }
    }
}
