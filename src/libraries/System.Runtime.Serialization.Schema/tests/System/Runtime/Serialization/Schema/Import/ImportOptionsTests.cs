// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.Serialization.Schema;
using System.Runtime.Serialization.Schema.Tests.DataContracts;
using System.Xml;
using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Runtime.Serialization.Schema.Tests
{
    public class ImportOptionsTests
    {
        private readonly ITestOutputHelper _output;

        public ImportOptionsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DefaultOptions()
        {
            XsdDataContractImporter importer = SchemaUtils.CreateImporterWithOptions();
            Assert.NotNull(importer);
            Assert.NotNull(importer.Options);
            Assert.False(importer.Options.EnableDataBinding);
            Assert.False(importer.Options.GenerateInternal);
            Assert.False(importer.Options.GenerateSerializable);
            Assert.False(importer.Options.ImportXmlType);
            Assert.Null(importer.Options.CodeProvider);
            Assert.NotNull(importer.Options.Namespaces);
            Assert.Empty(importer.Options.Namespaces);
            Assert.NotNull(importer.Options.ReferencedCollectionTypes);
            Assert.Empty(importer.Options.ReferencedCollectionTypes);
            Assert.NotNull(importer.Options.ReferencedTypes);
            Assert.Empty(importer.Options.ReferencedTypes);
            Assert.Null(importer.Options.DataContractSurrogate);
        }

        [Fact]
        public void GetImportOptions()
        {
            XsdDataContractImporter importer = new XsdDataContractImporter();
            importer.Options = new ImportOptions();
            Assert.NotNull(importer.Options);
        }

        [Fact]
        public void SetImportOptions()
        {
            XsdDataContractImporter e = new XsdDataContractImporter();
            e.Options = new ImportOptions();
            e.Options.Namespaces.Add("Test", "http://schemas.datacontract.org/2004/07/fooNs");
            Assert.Single(e.Options.Namespaces);
        }

        [Fact]
        public void GenerateInternal()
        {
            XsdDataContractImporter importer = SchemaUtils.CreateImporterWithOptions();
            importer.Options.GenerateInternal = true;
            importer.Import(SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[0]);
            _output.WriteLine(SchemaUtils.DumpCode(importer.CodeCompileUnit));
            Assert.True(importer.Options.GenerateInternal);
        }

        [Fact]
        public void EnableDataBinding()
        {
            XsdDataContractImporter importer = SchemaUtils.CreateImporterWithOptions();
            importer.Options.EnableDataBinding = true;
            importer.Import(SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[0]);
            _output.WriteLine(SchemaUtils.DumpCode(importer.CodeCompileUnit));
            Assert.True(importer.Options.EnableDataBinding);
        }

        [Fact]
        public void GenerateSerializable()
        {
            XsdDataContractImporter importer = SchemaUtils.CreateImporterWithOptions();
            importer.Options.GenerateSerializable = true;
            importer.Import(SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[0]);
            _output.WriteLine(SchemaUtils.DumpCode(importer.CodeCompileUnit));
            Assert.True(importer.Options.GenerateSerializable);
        }

        [Fact]
        public void ImportXmlType()
        {
            XsdDataContractImporter importer = SchemaUtils.CreateImporterWithOptions();
            importer.Options.ImportXmlType = true;
            importer.Import(SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[0]);
            _output.WriteLine(SchemaUtils.DumpCode(importer.CodeCompileUnit));
            Assert.True(importer.Options.ImportXmlType);
        }

        [Fact]
        public void CodeProvider()
        {
            XsdDataContractImporter importer = SchemaUtils.CreateImporterWithOptions();
            CodeDomProvider codeProvider = CodeDomProvider.CreateProvider("csharp");
            importer.Options.CodeProvider = codeProvider;
            Console.WriteLine(importer.Options.CodeProvider.GetType().FullName);
            importer.Import(SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[0]);
            _output.WriteLine(SchemaUtils.DumpCode(importer.CodeCompileUnit));
            Assert.Equal(codeProvider, importer.Options.CodeProvider);
        }

        [Theory]
        [InlineData("http://schemas.datacontract.org/2004/07/fooNs", "customizedNamespace")]
        [InlineData("*", "customizedNamespace")]
        [InlineData("null", "customizedNamespace")]
        public void Namespaces(string dcns, string clrns)
        {
            XsdDataContractImporter importer = SchemaUtils.CreateImporterWithOptions();
            Assert.NotNull(importer.Options.Namespaces);
            Assert.Empty(importer.Options.Namespaces);

            importer.Options.Namespaces.Add(dcns, clrns);
            importer.Import(SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[0]);
            _output.WriteLine(SchemaUtils.DumpCode(importer.CodeCompileUnit));
        }

        [Theory]
        [MemberData(nameof(ReferencedTypes_MemberData))]
        public void ReferencedTypes(XmlSchemaSet schemas, XmlQualifiedName qname, Type[] referencedTypes, Type expectedExceptionType = null, string msg = null)
        {
            XsdDataContractImporter importer = SchemaUtils.CreateImporterWithOptions();
            for (int i = 0; i < referencedTypes.Length; i++)
                importer.Options.ReferencedTypes.Add(referencedTypes[i]);

            if (expectedExceptionType == null)
            {
                importer.Import(schemas, qname);
                _output.WriteLine(SchemaUtils.DumpCode(importer.CodeCompileUnit));
            }
            else
            {
                var ex = Assert.Throws(expectedExceptionType, () => importer.Import(schemas, qname));

                if (!string.IsNullOrEmpty(msg))
                    Assert.StartsWith(msg, ex.Message);
            }
        }
        public static IEnumerable<object[]> ReferencedTypes_MemberData()
        {
            yield return new object[] { SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[1], new Type[] { typeof(AnotherValidType) } };
            yield return new object[] { SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[2], new Type[] { typeof(NonAttributedSquare) } };
            yield return new object[] { SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[1], new Type[] { typeof(AnotherValidType), typeof(ConflictingAnotherValidType) },
                    typeof(InvalidOperationException), @"List of referenced types contains more than one type with data contract name 'AnotherValidType' in namespace 'http://schemas.datacontract.org/2004/07/barNs'. Need to exclude all but one of the following types. Only matching types can be valid references:"};
            // These last two are described as "negative" in the original NetFx XsdDCImporterApi test code... but they don't fail here or there.
            yield return new object[] { SchemaUtils.ReferenceSchemas, SchemaUtils.ValidTypeNames[3], new Type[] { typeof(NonRefType) } };
            yield return new object[] { SchemaUtils.ReferenceSchemas, SchemaUtils.ValidTypeNames[4], new Type[] { typeof(RefType1) } };
        }
    }
}
