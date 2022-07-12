// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Schema;
using System.Xml;
using System.Xml.Schema;
using Xunit;

namespace System.Runtime.Serialization.Schema.Tests
{
    public class ImportTests
    {
        [Fact]
        static void ImportXSD()
        {
            XsdDataContractExporter exporter = new XsdDataContractExporter();
            exporter.Export(typeof(Employee));
            XmlSchemaSet schemas = exporter.Schemas;

            ImportOptions opts = new ImportOptions()
            {
                EnableDataBinding = true,
                GenerateInternal = true,
            };
            XsdDataContractImporter importer = new XsdDataContractImporter() { Options = opts };
            Assert.True(importer.CanImport(schemas));
            importer.Import(schemas);

            CodeCompileUnit ccu = importer.CodeCompileUnit;
            Assert.NotNull(ccu);

            string importedTypeCodeCS = CompileCode(ccu, new Microsoft.CSharp.CSharpCodeProvider());
            Assert.Equal(LineEndingsHelper.Normalize(File.ReadAllText(ContractUtils.ExpectedImportedTypeFile)), importedTypeCodeCS);

            //string importedTypeCodeVB = CompileCode(ccu, new Microsoft.VisualBasic.VBCodeProvider());
        }


        static string CompileCode(CodeCompileUnit ccu, CodeDomProvider provider)
        {
            CodeGeneratorOptions options = new CodeGeneratorOptions()
            {
                BlankLinesBetweenMembers = true,
                BracingStyle = "C",
            };

            StringWriter sw = new StringWriter();
            provider.GenerateCodeFromCompileUnit(ccu, sw, options);
            return sw.ToString();
        }
    }
}
