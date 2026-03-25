// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Xml.Schema;
using Xunit;

namespace System.Xml.XmlSchemaValidatorApiTests
{
    public class ValidationErrorMessageTests
    {
        [Fact]
        public static void ElementValidationError_MessageIsBounded()
        {
            string hexContent = new string('A', 10_001); // odd count, triggers hexBinary validation error
            string xsd = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                              <xs:element name='r' type='xs:hexBinary'/>
                            </xs:schema>";
            string xml = $"<r>{hexContent}</r>";

            var schemaSet = new XmlSchemaSet();
            schemaSet.Add(XmlSchema.Read(new StringReader(xsd), null)!);

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemaSet
            };

            XmlSchemaValidationException? caught = null;
            settings.ValidationEventHandler += (_, e) =>
            {
                if (e.Exception is XmlSchemaValidationException ex)
                    caught = ex;
            };

            using var reader = XmlReader.Create(new StringReader(xml), settings);
            while (reader.Read()) { }

            Assert.NotNull(caught);
            Assert.True(caught.Message.Length < 500,
                $"Validation error message should not contain the entire input. " +
                $"Got {caught.Message.Length} chars (expected < 500).");
        }

        [Fact]
        public static void AttributeValidationError_MessageIsBounded()
        {
            string hexContent = new string('A', 10_001); // odd count
            string xsd = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                              <xs:element name='r'>
                                <xs:complexType>
                                  <xs:attribute name='v' type='xs:hexBinary'/>
                                </xs:complexType>
                              </xs:element>
                            </xs:schema>";
            string xml = $"<r v='{hexContent}'/>";

            var schemaSet = new XmlSchemaSet();
            schemaSet.Add(XmlSchema.Read(new StringReader(xsd), null)!);

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemaSet
            };

            XmlSchemaValidationException? caught = null;
            settings.ValidationEventHandler += (_, e) =>
            {
                if (e.Exception is XmlSchemaValidationException ex)
                    caught = ex;
            };

            using var reader = XmlReader.Create(new StringReader(xml), settings);
            while (reader.Read()) { }

            Assert.NotNull(caught);
            Assert.True(caught.Message.Length < 500,
                $"Validation error message should not contain the entire input. " +
                $"Got {caught.Message.Length} chars (expected < 500).");
        }
    }
}
