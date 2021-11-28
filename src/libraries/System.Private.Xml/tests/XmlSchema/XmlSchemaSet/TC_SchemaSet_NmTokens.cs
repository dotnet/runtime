// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml.Tests
{
    #nullable enable
    public class TC_SchemaSet_NmTokens : TC_SchemaSetBase
    {
        private void TestSchemaCompile(string fileName, bool negative)
        {
            string xsd = Path.Combine(TestData._Root, fileName);
            XmlSchemaSet s = new XmlSchemaSet();
            s.ValidationEventHandler += (sender, args) => {
                Assert.False(args.Severity == XmlSeverityType.Warning);
                Assert.True(negative, args.Message);
            };
            XmlReader r = XmlReader.Create(xsd);
            s.Add(null, r);
            s.Compile();            
        }

        private void TestValidatedReader(string fileName, bool negative)
        {
            var settings = new XmlReaderSettings() {
                ValidationType = ValidationType.Schema,
                ValidationFlags = XmlSchemaValidationFlags.ProcessSchemaLocation | 
                    XmlSchemaValidationFlags.ReportValidationWarnings,
                XmlResolver = new XmlUrlResolver()
            };
            settings.ValidationEventHandler += (sender, args) => {
                Assert.True(negative, args.Message);
            };
            string xml = Path.Combine(TestData._Root, fileName);
            using XmlReader r = XmlReader.Create(xml, settings);
            XmlDocument doc = new XmlDocument();
            doc.Load(r);
        }

        [Fact]
        public void Test_NmTokens_Schema()
        {        
            // Positive test: should not return any validation error during schema compile then default value is the list of NMTOKEN from enumeration
            TestSchemaCompile("issue_60543_1.xsd", false);
            // Positive test: should not return any validation error during schema compile then default value is one value from enumeration
            TestSchemaCompile("issue_60543_2.xsd", false);
            // Negative test: the default value is NMTOKEN not from enumeration
            TestSchemaCompile("issue_60543_3.xsd", true);
            // Negative test: the default value is the list of one NMTOKEN from enumeration and NMTOKEN not from enumeration
            TestSchemaCompile("issue_60543_4.xsd", true);
            // Positive test: the attribute has no default value
            TestSchemaCompile("issue_60543_5.xsd", false);
        }

        [Fact]
        public void Test_NmTokens_ValidatedReader()
        {
            // Positive test: should not return any validation error during schema compile then default value is the list of NMTOKEN from enumeration
            TestValidatedReader("issue_60543_1.xml", false);
            // Positive test: should not return any validation error during schema compile then default value is one value from enumeration
            TestValidatedReader("issue_60543_2.xml", false);
            // Negative test: the attribute value is NMTOKEN not from enumeration
            TestValidatedReader("issue_60543_3.xml", true);
            // Negative test: the attribute value is the list of one NMTOKEN from enumeration and NMTOKEN not from enumeration
            TestValidatedReader("issue_60543_4.xml", true);
        }
    }
}