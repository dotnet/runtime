// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Composition.Convention.Tests
{
    public class ExportBuilderTests
    {
        private interface IFoo { }

        private class FooImpl : IFoo { }


        [Fact]
        public void ExportInterfaceWithTypeOf1()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().Export<IFoo>();

            Collections.Generic.IEnumerable<ExportAttribute> exports = builder.GetCustomAttributes(typeof(FooImpl), typeof(FooImpl).GetTypeInfo()).Where<Attribute>(e => e is ExportAttribute).Cast<ExportAttribute>();
            Assert.Equal(1, exports.Count());
            Assert.Equal(exports.First().ContractType, typeof(IFoo));
        }

        [Fact]
        public void ExportInterfaceWithTypeOf2()
        {
            var builder = new ConventionBuilder();
            builder.ForType(typeof(FooImpl)).Export((c) => c.AsContractType(typeof(IFoo)));

            Collections.Generic.IEnumerable<ExportAttribute> exports = builder.GetDeclaredAttributes(typeof(FooImpl), typeof(FooImpl).GetTypeInfo()).Where<Attribute>(e => e is ExportAttribute).Cast<ExportAttribute>();
            Assert.Equal(1, exports.Count());
            Assert.Equal(exports.First().ContractType, typeof(IFoo));
        }


        [Fact]
        public void AsContractTypeOfT_SetsContractType()
        {
            var builder = new ConventionBuilder();
            builder.ForTypesDerivedFrom<IFoo>().Export((e) => e.AsContractType<IFoo>());

            ExportAttribute exportAtt = GetExportAttribute(builder);
            Assert.Equal(typeof(IFoo), exportAtt.ContractType);
            Assert.Null(exportAtt.ContractName);
        }

        [Fact]
        public void AsContractType_SetsContractType()
        {
            var builder = new ConventionBuilder();
            builder.ForTypesDerivedFrom<IFoo>().Export((e) => e.AsContractType(typeof(IFoo)));

            ExportAttribute exportAtt = GetExportAttribute(builder);
            Assert.Equal(typeof(IFoo), exportAtt.ContractType);
            Assert.Null(exportAtt.ContractName);
        }

        [Fact]
        public void AsContractName_SetsContractName()
        {
            var builder = new ConventionBuilder();
            builder.ForTypesDerivedFrom<IFoo>().Export((e) => e.AsContractName("hey"));

            ExportAttribute exportAtt = GetExportAttribute(builder);
            Assert.Equal("hey", exportAtt.ContractName);
            Assert.Null(exportAtt.ContractType);
        }

        [Fact]
        public void AsContractName_AndContractType_SetsContractNameAndType()
        {
            var builder = new ConventionBuilder();
            builder.ForTypesDerivedFrom<IFoo>().Export((e) => e.AsContractName("hey").AsContractType(typeof(IFoo)));

            ExportAttribute exportAtt = GetExportAttribute(builder);
            Assert.Equal("hey", exportAtt.ContractName);
            Assert.Equal(typeof(IFoo), exportAtt.ContractType);
        }

        [Fact]
        public void AsContractName_AndContractType_ComputeContractNameFromType()
        {
            var builder = new ConventionBuilder();
            builder.ForTypesDerivedFrom<IFoo>().Export(e => e.AsContractName(t => "Contract:" + t.FullName).AsContractType<IFoo>());

            ExportAttribute exportAtt = GetExportAttribute(builder);
            Assert.Equal("Contract:" + typeof(FooImpl).FullName, exportAtt.ContractName);
            Assert.Equal(typeof(IFoo), exportAtt.ContractType);
        }

        [Fact]
        public void AddMetadata_AddsExportMetadataAttribute()
        {
            var builder = new ConventionBuilder();
            builder.ForTypesDerivedFrom<IFoo>().Export(e => e.AddMetadata("name", "val"));

            ExportMetadataAttribute exportAtt = GetExportMetadataAttribute(builder);
            Assert.Equal("name", exportAtt.Name);
            Assert.Equal("val", exportAtt.Value);
        }

        [Fact]
        public void AddMetadataFuncVal_AddsExportMetadataAttribute()
        {
            var builder = new ConventionBuilder();
            builder.ForTypesDerivedFrom<IFoo>().Export(e => e.AddMetadata("name", t => t.Name));

            ExportMetadataAttribute exportAtt = GetExportMetadataAttribute(builder);
            Assert.Equal("name", exportAtt.Name);
            Assert.Equal(typeof(FooImpl).Name, exportAtt.Value);
        }

        private static ExportAttribute GetExportAttribute(ConventionBuilder builder)
        {
            Attribute[] list = builder.GetDeclaredAttributes(typeof(FooImpl), typeof(FooImpl).GetTypeInfo());
            Assert.Equal(1, list.Length);
            return list[0] as ExportAttribute;
        }

        private static ExportMetadataAttribute GetExportMetadataAttribute(ConventionBuilder builder)
        {
            Attribute[] list = builder.GetDeclaredAttributes(typeof(FooImpl), typeof(FooImpl).GetTypeInfo());
            Assert.Equal(2, list.Length);
            return list[1] as ExportMetadataAttribute;
        }
    }
}
