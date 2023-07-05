// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
{
    public class ExportOptionsTests
    {
        private readonly ITestOutputHelper _output;

        public ExportOptionsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DefaultOptions()
        {
            ExportOptions options = new ExportOptions();
            Assert.NotNull(options);
            Assert.Null(options.DataContractSurrogate);
            Assert.NotNull(options.KnownTypes);
            Assert.Empty(options.KnownTypes);
        }

        [Fact]
        public void GetImportOptions()
        {
            XsdDataContractExporter exporter = new XsdDataContractExporter();
            var options = new ExportOptions();
            exporter.Options = options;
            Assert.NotNull(exporter.Options);
            Assert.Equal(options, exporter.Options);
        }

        [Fact]
        public void SetImportOptions()
        {
            XsdDataContractExporter e = new XsdDataContractExporter();
            e.Options = new ExportOptions();
            Assert.Empty(e.Options.KnownTypes);
            e.Options.KnownTypes.Add(typeof(Types.Point));
            Assert.Single(e.Options.KnownTypes);
        }

        [Fact]
        public void KnownTypes_Negative()
        {
            XsdDataContractExporter e = new XsdDataContractExporter();
            e.Options = new ExportOptions();
            e.Options.KnownTypes.Add(null);
            var ex = Assert.Throws<ArgumentException>(() => e.Export(typeof(Types.Point)));
            Assert.Equal(@"Cannot export null type provided via KnownTypesCollection.", ex.Message);
        }
    }
}
