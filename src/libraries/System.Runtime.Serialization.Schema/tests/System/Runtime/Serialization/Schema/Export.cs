// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization.Schema;
using System.Xml;
using System.Xml.Schema;
using Xunit;

namespace System.Runtime.Serialization.Schema.Tests
{
    public class ExportTests
    {
        [Fact]
        static void ExportXSD()
        {
            XsdDataContractExporter exporter = new XsdDataContractExporter();
            Assert.True(exporter.CanExport(typeof(Employee)));
            exporter.Export(typeof(Employee));
            Assert.Equal(3, exporter.Schemas.Count);

            XmlSchemaSet mySchemas = exporter.Schemas;
            XmlQualifiedName XmlNameValue = exporter.GetRootElementName(typeof(Employee));
            string EmployeeNameSpace = XmlNameValue.Namespace;
            Assert.Equal(ContractUtils.TestNamespace, EmployeeNameSpace);

            var employeeSchemas = mySchemas.Schemas(EmployeeNameSpace);
            Assert.Single(employeeSchemas);
            foreach (XmlSchema schema in employeeSchemas)
            {
                StringWriter sw = new StringWriter();
                schema.Write(sw);
                Assert.Equal(ContractUtils.ExpectedEmployeeSchema, sw.ToString());
            }
        }

        [Fact]
        static void GetXmlElementName()
        {
            XsdDataContractExporter myExporter = new XsdDataContractExporter();
            XmlQualifiedName xmlElementName = myExporter.GetRootElementName(typeof(Employee));
            Assert.False(xmlElementName.IsEmpty);
            Assert.Equal(ContractUtils.TestNamespace, xmlElementName.Namespace);
            Assert.Equal("Employee", xmlElementName.Name);
        }
    }
}
