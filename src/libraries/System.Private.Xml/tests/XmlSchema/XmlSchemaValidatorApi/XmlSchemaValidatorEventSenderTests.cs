// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Xml.Schema;
using Xunit;

namespace System.Xml.XmlSchemaValidatorApiTests
{
    public class XmlSchemaValidatorEventSenderTests
    {
        [Theory]
        [InlineData("default")]
        [InlineData("custom")]
        [InlineData("null")]
        public void IncompleteContentUsesValidationEventSender(string senderKind)
        {
            const string schema = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:element name="root"><xs:complexType><xs:sequence>
                    <xs:element name="child" type="xs:string"/>
                  </xs:sequence></xs:complexType></xs:element>
                </xs:schema>
                """;
            var schemas = new XmlSchemaSet();
            using XmlReader reader = XmlReader.Create(new StringReader(schema));
            schemas.Add(null, reader);
            schemas.Compile();
            var names = new NameTable();
            var validator = new XmlSchemaValidator(names, schemas, new XmlNamespaceManager(names), XmlSchemaValidationFlags.None);
            object expected = senderKind == "custom" ? new object() : senderKind == "null" ? null : validator;
            if (senderKind != "default")
            {
                validator.ValidationEventSender = expected;
            }
            int errors = 0;
            validator.ValidationEventHandler += (sender, e) =>
            {
                Assert.Same(expected, sender);
                Assert.Equal(XmlSeverityType.Error, e.Severity);
                errors++;
            };
            validator.Initialize();
            validator.ValidateElement("root", "", null);
            validator.ValidateEndOfAttributes(null);
            validator.ValidateEndElement(null);
            validator.EndValidation();
            Assert.Equal(1, errors);
        }
    }
}
