// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml.Tests
{
    public class TC_SchemaSet_NmTokens : TC_SchemaSetBase
    {
        [Theory]
        // Positive test: should not return any validation error during schema compile then default value is the list of NMTOKEN from enumeration
        [InlineData("nmtokens_restriction_multiple_default_values.xsd", false)] 
        // Positive test: should not return any validation error during schema compile then default value is one value from enumeration
        [InlineData("nmtokens_restriction_single_default_value.xsd", false)] 
        // Negative test: the default value is NMTOKEN not from enumeration
        [InlineData("nmtokens_restriction_bad_default_value.xsd", true)]  
        // Negative test: the default value is the list of one NMTOKEN from enumeration and NMTOKEN not from enumeration
        [InlineData("nmtokens_restriction_bad_multiple_default_values.xsd", true)]  
        // Positive test: the attribute has no default value
        [InlineData("nmtokens_restriction_no_default_value.xsd", false)] 
        public void TestSchemaCompile(string fileName, bool negative)
        {
            string xsd = Path.Combine(TestData._Root, fileName);
            XmlSchemaSet s = new XmlSchemaSet();
            int numevents = 0;
            s.ValidationEventHandler += (sender, args) => {
                Assert.NotEqual(XmlSeverityType.Warning, args.Severity);
                Assert.True(negative, args.Message);
                numevents++;
            };            
            XmlReader r = XmlReader.Create(xsd);
            s.Add(null, r);
            s.Compile();            
            Assert.False(negative && numevents != 1);
        }

        [Theory]
        // Positive test: should not return any validation error during schema compile then default value is the list of NMTOKEN from enumeration
        [InlineData("nmtokens_restriction_multiple_default_values.xml", false)] 
        // Positive test: should not return any validation error during schema compile then default value is one value from enumeration
        [InlineData("nmtokens_restriction_single_default_value.xml", false)] 
        // Negative test: the attribute value is NMTOKEN not from enumeration
        [InlineData("nmtokens_restriction_bad_default_value.xml", true)]  
        // Negative test: the attribute value is the list of one NMTOKEN from enumeration and NMTOKEN not from enumeration
        [InlineData("nmtokens_restriction_bad_multiple_default_values.xml", true)]  
        public void TestValidatedReader(string fileName, bool negative)
        {
            var settings = new XmlReaderSettings() {
                ValidationType = ValidationType.Schema,
                ValidationFlags = XmlSchemaValidationFlags.ProcessSchemaLocation | 
                    XmlSchemaValidationFlags.ReportValidationWarnings,
                XmlResolver = new XmlUrlResolver()
            };
            int numevents = 0;
            settings.ValidationEventHandler += (sender, args) => {
                Assert.True(negative, args.Message);
                numevents++;
            };
            string xml = Path.Combine(TestData._Root, fileName);
            using XmlReader r = XmlReader.Create(xml, settings);
            XmlDocument doc = new XmlDocument();
            doc.Load(r);
            Assert.False(negative && numevents != 1);
        }
    }
}
