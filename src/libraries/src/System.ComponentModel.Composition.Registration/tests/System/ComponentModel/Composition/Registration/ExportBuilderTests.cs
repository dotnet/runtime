// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Composition.Registration.Tests
{
    public class ExportBuilderTests
    {
        interface IFoo { }
        
        class FooImpl {}
    
        [Fact]
        public void AsContractTypeOfT_SetsContractType()
        {
            var builder = new ExportBuilder();
            builder.AsContractType<IFoo>();
            
            ExportAttribute exportAtt = GetExportAttribute(builder);
            Assert.Equal(typeof(IFoo), exportAtt.ContractType);
            Assert.Null(exportAtt.ContractName);
        }
    
        [Fact]
        public void AsContractType_SetsContractType()
        {
            var builder = new ExportBuilder();
            builder.AsContractType(typeof(IFoo));
            
            ExportAttribute exportAtt = GetExportAttribute(builder);
            Assert.Equal(typeof(IFoo), exportAtt.ContractType);
            Assert.Null(exportAtt.ContractName);
        }
    
        [Fact]
        public void AsContractName_SetsContractName()
        {
            var builder = new ExportBuilder();
            builder.AsContractName("hey");
            
            ExportAttribute exportAtt = GetExportAttribute(builder);
            Assert.Equal("hey", exportAtt.ContractName);
            Assert.Null(exportAtt.ContractType);
        }

        [Fact]
        public void AsContractName_AndContractType_SetsContractNameAndType()
        {
            var builder = new ExportBuilder();
            builder.AsContractName("hey");
            builder.AsContractType(typeof (IFoo));
            
            ExportAttribute exportAtt = GetExportAttribute(builder);
            Assert.Equal("hey", exportAtt.ContractName);
            Assert.Equal(typeof(IFoo), exportAtt.ContractType);
        }

        [Fact]
        public void Inherited_AddsInheritedExportAttribute()
        {
            var builder = new ExportBuilder();
            builder.Inherited();

            var list = new List<Attribute>();
            builder.BuildAttributes(typeof(FooImpl), ref list);
            Assert.Equal(1, list.Count);
            var att = list[0] as InheritedExportAttribute;
            Assert.NotNull(att);
        }

        [Fact]
        public void AddMetadata_AddsExportMetadataAttribute()
        {
            var builder = new ExportBuilder();
            builder.AddMetadata("name", "val");

            ExportMetadataAttribute exportAtt = GetExportMetadataAttribute(builder);
            Assert.Equal("name", exportAtt.Name);
            Assert.Equal("val", exportAtt.Value);
        }

        [Fact]
        public void AddMetadataFuncVal_AddsExportMetadataAttribute()
        {
            var builder = new ExportBuilder();
            builder.AddMetadata("name", t => t.Name);

            ExportMetadataAttribute exportAtt = GetExportMetadataAttribute(builder);
            Assert.Equal("name", exportAtt.Name);
            Assert.Equal(typeof(FooImpl).Name, exportAtt.Value);
        }

        private static ExportAttribute GetExportAttribute(ExportBuilder builder)
        {
            var list = new List<Attribute>();
            builder.BuildAttributes(typeof(FooImpl), ref list);
            Assert.Equal(1, list.Count);

            return list[0] as ExportAttribute;
        }

        private static ExportMetadataAttribute GetExportMetadataAttribute(ExportBuilder builder)
        {
            var list = new List<Attribute>();
            builder.BuildAttributes(typeof(FooImpl), ref list);
            Assert.Equal(2, list.Count);

            return list[1] as ExportMetadataAttribute;
        }
	}
}
