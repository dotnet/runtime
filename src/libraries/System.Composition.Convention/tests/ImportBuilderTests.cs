// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition;
using System.Composition.Convention;
using System.Composition.Convention.UnitTests;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Composition.Convention
{
    public class ImportBuilderTests
    {
        public interface IFoo { }

        public class FooImpl
        {
            public IFoo IFooProperty { get; private set; }
        }

        [Fact]
        public void AsContractName_SetsContractName()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().ImportProperty((p) => p.IFooProperty, (c) => c.AsContractName("hey"));

            ImportAttribute importAtt = GetImportAttribute(builder);
            Assert.Equal("hey", importAtt.ContractName);
            Assert.False(importAtt.AllowDefault);
        }

        [Fact]
        public void AsContractName_AndContractType_ComputeContractNameFromType()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().ImportProperty((p) => p.IFooProperty, c => c.AsContractName(t => "Contract:" + t.FullName));

            ImportAttribute importAtt = GetImportAttribute(builder);
            Assert.Equal("Contract:" + typeof(IFoo).FullName, importAtt.ContractName);
        }

        [Fact]
        public void AllowDefault_SetsAllowDefaultProperty()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().ImportProperty((p) => p.IFooProperty, (c) => c.AllowDefault());

            ImportAttribute importAtt = GetImportAttribute(builder);
            Assert.True(importAtt.AllowDefault);
            Assert.Null(importAtt.ContractName);
        }

        [Fact]
        public void AsContractName_AndContractType_SetsContractNameAndType()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().ImportProperty((p) => p.IFooProperty, (c) => c.AsContractName("hey"));

            ImportAttribute importAtt = GetImportAttribute(builder);
            Assert.Equal("hey", importAtt.ContractName);
        }

        [Fact]
        public void AsMany_ChangesGeneratedAttributeToImportMany()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().ImportProperty((p) => p.IFooProperty, (c) => c.AsMany());

            ImportManyAttribute importAtt = GetImportManyAttribute(builder);
            Assert.NotNull(importAtt);
            Assert.Null(importAtt.ContractName);
        }

        [Fact]
        public void AsMany_And_ContractName_ChangesGeneratedAttributeToImportMany()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().ImportProperty((p) => p.IFooProperty, (c) => c.AsContractName("hey").AsMany());

            ImportManyAttribute importAtt = GetImportManyAttribute(builder);
            Assert.NotNull(importAtt);
            Assert.Equal("hey", importAtt.ContractName);
        }

        [Fact]
        public void AddImportConstraint_AddsImportConstraintMetadataAttribute()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().ImportProperty((p) => p.IFooProperty, (c) => c.AddMetadataConstraint("name", "val"));

            ImportMetadataConstraintAttribute importMetadataConstraint = GetImportMetadataConstraintAttribute(builder);
            Assert.Equal("name", importMetadataConstraint.Name);
            Assert.Equal("val", importMetadataConstraint.Value);
        }

        [Fact]
        public void AddImportConstraintFuncVal_AddsImportConstraintMetadataAttribute()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().ImportProperty((p) => p.IFooProperty, (c) => c.AddMetadataConstraint("name", t => t.Name));

            ImportMetadataConstraintAttribute importMetadataConstraint = GetImportMetadataConstraintAttribute(builder);
            Assert.Equal("name", importMetadataConstraint.Name);
            Assert.Equal(typeof(IFoo).Name, importMetadataConstraint.Value);
        }

        private static ImportAttribute GetImportAttribute(ConventionBuilder builder)
        {
            var list = builder.GetDeclaredAttributes(typeof(FooImpl), typeof(FooImpl).GetRuntimeProperties().Where((m) => m.Name == "IFooProperty").First());
            Assert.Equal(1, list.Length);
            return list.OfType<ImportAttribute>().FirstOrDefault();
        }

        private static ImportManyAttribute GetImportManyAttribute(ConventionBuilder builder)
        {
            var list = builder.GetDeclaredAttributes(typeof(FooImpl), typeof(FooImpl).GetRuntimeProperties().Where((m) => m.Name == "IFooProperty").First());
            Assert.Equal(1, list.Length);
            return list.OfType<ImportManyAttribute>().FirstOrDefault();
        }

        private static ImportMetadataConstraintAttribute GetImportMetadataConstraintAttribute(ConventionBuilder builder)
        {
            var list = builder.GetDeclaredAttributes(typeof(FooImpl), typeof(FooImpl).GetRuntimeProperties().Where((m) => m.Name == "IFooProperty").First());
            Assert.Equal(2, list.Length);
            return list.OfType<ImportMetadataConstraintAttribute>().FirstOrDefault();
        }
    }
}
