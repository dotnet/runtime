// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    // ===================== ValidateElement =====================

    public class TCValidateElement : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCValidateElement(ITestOutputHelper output): base(output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("name", "first")]
        [InlineData("name", "second")]
        [InlineData("ns", "first")]
        [InlineData("ns", "second")]
        public void PassNull_LocalName_NamespaceUri_Invalid_First_Second_Overload(String type, String overload)
        {
            XmlSchemaValidator val = CreateValidator(CreateSchemaSetFromXml("<root />"));
            string name = "root";
            string ns = "";
            XmlSchemaInfo info = new XmlSchemaInfo();

            if (type == "name")
                name = null;
            else
                ns = null;

            val.Initialize();
            try
            {
                if (overload == "first")
                    val.ValidateElement(name, ns, info);
                else
                    val.ValidateElement(name, ns, info, null, null, null, null);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            Assert.True(false);
        }

        [Theory]
        [InlineData("first")]
        [InlineData("second")]
        public void PassNullXmlSchemaInfo__Valid_First_Second_Overload(String overload)
        {
            XmlSchemaValidator val = CreateValidator(CreateSchemaSetFromXml("<root />"));

            val.Initialize();
            if (overload == "first")
                val.ValidateElement("root", "", null);
            else
                val.ValidateElement("root", "", null, null, null, null, null);

            return;
        }

        [Theory]
        [InlineData("first")]
        [InlineData("second")]
        public void PassInvalidName_First_Second_Overload(String overload)
        {
            XmlSchemaValidator val = CreateValidator(CreateSchemaSetFromXml("<root />"));

            val.Initialize();

            try
            {
                if (overload == "first")
                    val.ValidateElement("$$##", "", null);
                else
                    val.ValidateElement("$$##", "", null, null, null, null, null);
            }
            catch (XmlSchemaValidationException)
            {
                //XmlExceptionVerifier.IsExceptionOk(e, "Sch_UndeclaredElement", new string[] { "$$##" });
                return;
            }

            Assert.True(false);
        }

        [Theory]
        [InlineData("SimpleElement", XmlSchemaContentType.TextOnly, "first")]
        [InlineData("ElementOnlyElement", XmlSchemaContentType.ElementOnly, "first")]
        [InlineData("EmptyElement", XmlSchemaContentType.Empty, "first")]
        [InlineData("MixedElement", XmlSchemaContentType.Mixed, "first")]
        [InlineData("SimpleElement", XmlSchemaContentType.TextOnly, "second")]
        [InlineData("ElementOnlyElement", XmlSchemaContentType.ElementOnly, "second")]
        [InlineData("EmptyElement", XmlSchemaContentType.Empty, "second")]
        [InlineData("MixedElement", XmlSchemaContentType.Mixed, "second")]
        public void CallValidateElementAndCHeckXmlSchemaInfoFOr_Simple_Complex_Empty_Mixed_Element_First_Second_Overload(String elemType, XmlSchemaContentType schemaContentType, String overload)
        {
            XmlSchemaValidator val;
            XmlSchemaSet schemas = new XmlSchemaSet();
            XmlSchemaInfo info = new XmlSchemaInfo();
            string name = elemType;

            schemas.Add("", Path.Combine(TestData, XSDFILE_VALIDATE_TEXT));
            schemas.Compile();
            val = CreateValidator(schemas);

            val.Initialize();
            if (overload == "first")
                val.ValidateElement(name, "", info);
            else
                val.ValidateElement(name, "", info, null, null, null, null);

            Assert.Equal(info.ContentType, schemaContentType);
            Assert.Equal(info.Validity, XmlSchemaValidity.NotKnown);
            Assert.Equal(info.SchemaElement, schemas.GlobalElements[new XmlQualifiedName(name)]);
            Assert.Equal(info.IsNil, false);
            Assert.Equal(info.IsDefault, false);
            if (name == "SimpleElement")
                Assert.True(info.SchemaType is XmlSchemaSimpleType);
            else
                Assert.True(info.SchemaType is XmlSchemaComplexType);

            return;
        }

        [Fact]
        public void SanityTestsForNestedElements()
        {
            XmlSchemaValidator val;
            XmlSchemaSet schemas = new XmlSchemaSet();
            XmlSchemaInfo info = new XmlSchemaInfo();

            schemas.Add("", Path.Combine(TestData, XSDFILE_VALIDATE_END_ELEMENT));
            schemas.Compile();
            val = CreateValidator(schemas);

            val.Initialize();
            val.ValidateElement("NestedElement", "", info);
            val.ValidateEndOfAttributes(null);
            Assert.Equal(info.SchemaElement.QualifiedName, new XmlQualifiedName("NestedElement"));
            Assert.True(info.SchemaType is XmlSchemaComplexType);

            val.ValidateElement("foo", "", info);
            val.ValidateEndOfAttributes(null);
            Assert.Equal(info.SchemaElement.QualifiedName, new XmlQualifiedName("foo"));
            Assert.True(info.SchemaType is XmlSchemaComplexType);

            val.ValidateElement("bar", "", info);
            Assert.Equal(info.SchemaElement.QualifiedName, new XmlQualifiedName("bar"));
            Assert.True(info.SchemaType is XmlSchemaSimpleType);
            Assert.Equal(info.SchemaType.TypeCode, XmlTypeCode.String);

            return;
        }

        // ====== second overload ======

        [Theory]
        [InlineData(XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessIdentityConstraints | XmlSchemaValidationFlags.ProcessInlineSchema | XmlSchemaValidationFlags.ProcessSchemaLocation)]
        [InlineData(XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessIdentityConstraints | XmlSchemaValidationFlags.ProcessInlineSchema)]
        public void CheckSchemaLocationIs_UsedWhenSpecified_NotUsedWhenFlagIsNotSet(XmlSchemaValidationFlags allFlags)
        {
            XmlSchemaValidator val;
            XmlNamespaceManager ns = new XmlNamespaceManager(new NameTable());
            XmlSchemaSet schemas = new XmlSchemaSet();
            XmlSchemaInfo info = new XmlSchemaInfo();
            CValidationEventHolder holder = new CValidationEventHolder();

            schemas.Add("", XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                              "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n" +
                                                              "    <xs:element name=\"root\" />\n" +
                                                              "</xs:schema>")));
            val = CreateValidator(schemas, ns, allFlags);
            val.XmlResolver = new XmlUrlResolver();
            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);
            ns.AddNamespace("t", "uri:tempuri");

            val.Initialize();
            val.ValidateElement("root", "", info, "t:type1", null, "uri:tempuri " + Path.Combine(TestData, XSDFILE_TARGET_NAMESPACE), null);

            if ((int)allFlags == (int)AllFlags)
            {
                Assert.True(!holder.IsCalledA);
                Assert.True(info.SchemaType is XmlSchemaComplexType);
            }
            else
            {
                Assert.True(holder.IsCalledA);
                //XmlExceptionVerifier.IsExceptionOk(holder.lastException);
            }

            return;
        }

        [Theory]
        [InlineData(XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessIdentityConstraints | XmlSchemaValidationFlags.ProcessInlineSchema | XmlSchemaValidationFlags.ProcessSchemaLocation)]
        [InlineData(XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessIdentityConstraints | XmlSchemaValidationFlags.ProcessInlineSchema)]
        public void CheckNoNamespaceSchemaLocationIs_UsedWhenSpecified_NotUsedWhenFlagIsSet(XmlSchemaValidationFlags allFlags)
        {
            XmlSchemaValidator val;
            XmlSchemaSet schemas = new XmlSchemaSet();
            XmlSchemaInfo info = new XmlSchemaInfo();
            CValidationEventHolder holder = new CValidationEventHolder();

            schemas.Add("", XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                              "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n" +
                                                              "    <xs:element name=\"root\" />\n" +
                                                              "</xs:schema>")));
            val = CreateValidator(schemas, allFlags);
            val.XmlResolver = new XmlUrlResolver();
            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateElement("root", "", info, "type1", null, null, Path.Combine(TestData, XSDFILE_NO_TARGET_NAMESPACE));

            if ((int)allFlags == (int)AllFlags)
            {
                Assert.True(!holder.IsCalledA);
                Assert.True(info.SchemaType is XmlSchemaComplexType);
            }
            else
            {
                Assert.True(holder.IsCalledA);
                //XmlExceptionVerifier.IsExceptionOk(holder.lastException);
            }

            return;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("false")]
        public void CallWith_Null_False_XsiNil(String xsiNil)
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();

            val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);

            val.Initialize();
            val.ValidateElement("NillableElement", "", info, null, xsiNil, null, null);
            val.ValidateEndOfAttributes(null);

            try
            {
                val.ValidateEndElement(info);
            }
            catch (XmlSchemaValidationException)
            {
                //XmlExceptionVerifier.IsExceptionOk(e, new object[] { "Sch_IncompleteContentExpecting",
																//	new object[] { "Sch_ElementName", "NillableElement" },
																//	new object[] { "Sch_ElementName", "foo" } });
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void CallWithXsiNilTrue()
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();

            val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);

            val.Initialize();
            val.ValidateElement("NillableElement", "", info, null, "true", null, null);
            val.ValidateEndOfAttributes(null);

            val.ValidateEndElement(info);
            Assert.Equal(info.Validity, XmlSchemaValidity.Valid);

            return;
        }

        [Fact]
        public void ProvideValidXsiType()
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlNamespaceManager ns = new XmlNamespaceManager(new NameTable());
            XmlSchemaSet schemas = new XmlSchemaSet();

            schemas.Add("uri:tempuri", Path.Combine(TestData, XSDFILE_TARGET_NAMESPACE));
            val = CreateValidator(schemas, ns, 0);
            ns.AddNamespace("t", "uri:tempuri");

            val.Initialize();
            val.ValidateElement("foo", "uri:tempuri", null, "t:type1", null, null, null);

            return;
        }

        [Fact]
        public void ProvideInvalidXsiType()
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlNamespaceManager ns = new XmlNamespaceManager(new NameTable());
            XmlSchemaSet schemas = new XmlSchemaSet();

            schemas.Add("uri:tempuri", Path.Combine(TestData, XSDFILE_TARGET_NAMESPACE));
            val = CreateValidator(schemas, ns, 0);
            ns.AddNamespace("t", "uri:tempuri");

            val.Initialize();

            try
            {
                val.ValidateElement("foo", "uri:tempuri", null, "type1", null, null, null);
            }
            catch (XmlSchemaValidationException)
            {
                //XmlExceptionVerifier.IsExceptionOk(e, "Sch_XsiTypeNotFound", new string[] { "type1" });
                return;
            }
            Assert.True(false);
        }

        [Fact]
        public void CheckThatWarningOccursWhenInvalidSchemaLocationIsProvided()
        {
            XmlSchemaValidator val;
            XmlSchemaSet schemas = new XmlSchemaSet();
            XmlSchemaInfo info = new XmlSchemaInfo();
            CValidationEventHolder holder = new CValidationEventHolder();
            XmlNamespaceManager ns = new XmlNamespaceManager(new NameTable());

            schemas.Add("uri:tempuri", XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                              "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\n" +
                                                              "           targetNamespace=\"uri:tempuri\">\n" +
                                                              "    <xs:complexType name=\"rootType\">\n" +
                                                              "        <xs:sequence />\n" +
                                                              "    </xs:complexType>\n" +
                                                              "</xs:schema>")));
            val = CreateValidator(schemas, ns, AllFlags);
            val.XmlResolver = new XmlUrlResolver();
            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);
            ns.AddNamespace("t", "uri:tempuri");

            val.Initialize();
            val.ValidateElement("root", "", info, "t:rootType", null, "uri:tempuri " + Path.Combine(TestData, "__NonExistingFile__.xsd"), null);

            Assert.True(holder.IsCalledA);
            Assert.Equal(holder.lastSeverity, XmlSeverityType.Warning);
            //XmlExceptionVerifier.IsExceptionOk(holder.lastException, "Sch_CannotLoadSchema", new string[] { "uri:tempuri", null });

            return;
        }

        [Fact]
        public void CheckThatWarningOccursWhenInvalidNoNamespaceSchemaLocationIsProvided()
        {
            XmlSchemaValidator val;
            XmlSchemaSet schemas = new XmlSchemaSet();
            XmlSchemaInfo info = new XmlSchemaInfo();
            CValidationEventHolder holder = new CValidationEventHolder();

            schemas.Add("", XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                              "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n" +
                                                              "    <xs:complexType name=\"rootType\">\n" +
                                                              "        <xs:sequence />\n" +
                                                              "    </xs:complexType>\n" +
                                                              "</xs:schema>")));
            val = CreateValidator(schemas);
            val.XmlResolver = new XmlUrlResolver();
            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateElement("root", "", info, "rootType", null, null, Path.Combine(TestData, "__NonExistingFile__.xsd"));

            Assert.True(holder.IsCalledA);
            Assert.Equal(holder.lastSeverity, XmlSeverityType.Warning);
            //XmlExceptionVerifier.IsExceptionOk(holder.lastException, "Sch_CannotLoadSchema", new string[] { "", null });

            return;
        }

        [Fact]
        public void CheckThatWarningOccursWhenUndefinedElementIsValidatedWithLaxValidation()
        {
            XmlSchemaValidator val;
            CValidationEventHolder holder = new CValidationEventHolder();

            val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateElement("LaxElement", "", null);
            val.ValidateEndOfAttributes(null);
            val.ValidateElement("undefined", "", null);

            Assert.True(holder.IsCalledA);
            Assert.Equal(holder.lastSeverity, XmlSeverityType.Warning);
            //XmlExceptionVerifier.IsExceptionOk(holder.lastException, "Sch_NoElementSchemaFound", new string[] { "undefined" });

            return;
        }

        [Fact]
        public void CheckThatWarningsDontOccurWhenIgnoreValidationWarningsIsSet()
        {
            XmlSchemaValidator val;
            CValidationEventHolder holder = new CValidationEventHolder();

            val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT, "", XmlSchemaValidationFlags.ProcessIdentityConstraints | XmlSchemaValidationFlags.ProcessInlineSchema | XmlSchemaValidationFlags.ProcessSchemaLocation);
            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateElement("LaxElement", "", null);
            val.ValidateEndOfAttributes(null);
            val.ValidateElement("undefined", "", null);

            Assert.True(!holder.IsCalledA);

            return;
        }

        //342447
        [Fact]
        public void VerifyThatSubstitutionGroupMembersAreResolvedAndAddedToTheList()
        {
            XmlSchemaValidator val;
            XmlSchemaSet schemas = new XmlSchemaSet();
            XmlSchemaParticle[] actualParticles;
            string[] expectedParticles = { "eleA", "eleB", "eleC" };

            schemas.Add("", Path.Combine(TestData, "Bug342447.xsd"));
            schemas.Compile();
            val = CreateValidator(schemas);
            val.Initialize();
            val.ValidateElement("eleSeq", "", null);

            actualParticles = val.GetExpectedParticles();

            Assert.Equal(actualParticles.GetLength(0), expectedParticles.GetLength(0));

            int count = 0;
            foreach (XmlSchemaElement element in actualParticles)
            {
                Assert.Equal(element.QualifiedName.ToString(), expectedParticles[count++]);
            }
            return;
        }

        //Dev10_40497
        [Fact]
        public void StringPassedToValidateEndElementDoesNotSatisfyIdentityConstraints()
        {
            Initialize();
            string xsd =
                "<xs:schema targetNamespace='http://tempuri.org/XMLSchema.xsd' elementFormDefault='qualified' xmlns='http://tempuri.org/XMLSchema.xsd' xmlns:mstns='http://tempuri.org/XMLSchema.xsd' xmlns:xs='http://www.w3.org/2001/XMLSchema'>" +
                    "<xs:element name='root'>" +
                        "<xs:complexType> <xs:sequence> <xs:element name='B' type='mstns:B'/> </xs:sequence> </xs:complexType>" +
                        "<xs:unique name='pNumKey'><xs:selector xpath='mstns:B/mstns:part'/><xs:field xpath='.'/></xs:unique>" +
                    "</xs:element>" +
                    "<xs:complexType name='B'><xs:sequence><xs:element name='part' maxOccurs='unbounded' type='xs:string'></xs:element></xs:sequence></xs:complexType>" +
                "</xs:schema>";

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(XmlSchema.Read(new StringReader(xsd), ValidationCallback));
            ss.Compile();

            string ns = "http://tempuri.org/XMLSchema.xsd";
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(ss.NameTable);
            XmlSchemaValidator val = new XmlSchemaValidator(ss.NameTable, ss, nsmgr, XmlSchemaValidationFlags.ProcessIdentityConstraints);
            val.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            val.Initialize();
            XmlSchemaInfo si = new XmlSchemaInfo();
            val.ValidateElement("root", ns, si);
            val.ValidateEndOfAttributes(si);
            val.ValidateElement("B", ns, si);
            val.ValidateEndOfAttributes(si);

            val.ValidateElement("part", ns, si);
            val.ValidateEndOfAttributes(si);
            val.ValidateText("1");
            val.ValidateEndElement(si);

            val.ValidateElement("part", ns, si);
            val.ValidateEndOfAttributes(si);
            val.ValidateEndElement(si, "1");

            val.ValidateElement("part", ns, si);
            val.ValidateEndOfAttributes(si);
            val.ValidateText("1");
            val.ValidateEndElement(si);

            val.ValidateEndElement(si);
            val.ValidateEndElement(si);

            Assert.Equal(warningCount, 0);
            Assert.Equal(errorCount, 2);
            return;
        }

        //TFS_469834
        [Fact]
        public void XmlSchemaValidatorDoesNotEnforceIdentityConstraintsOnDefaultAttributesInSomeCases()
        {
            Initialize();
            string xml = @"<?xml version='1.0'?>
<root xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:noNamespaceSchemaLocation='idF016.xsd'>
	<uid val='test'/>	<uid/></root>";

            string xsd = @"<?xml version='1.0'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema' elementFormDefault='qualified'>
	<xsd:element name='root'>
		<xsd:complexType>
			<xsd:sequence>
				<xsd:element ref='uid' maxOccurs='unbounded'/>
			</xsd:sequence>
		</xsd:complexType>
		<xsd:unique id='foo123' name='uuid'>
			<xsd:selector xpath='.//uid'/>
			<xsd:field xpath='@val'/>
		</xsd:unique>
	</xsd:element>
	<xsd:element name='uid' nillable='true'>
		<xsd:complexType>
			<xsd:attribute name='val' type='xsd:string' default='test'/>
		</xsd:complexType>
	</xsd:element>
</xsd:schema>";

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(new NameTable());
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add(null, XmlReader.Create(new StringReader(xsd)));
            schemas.Compile();
            XmlSchemaValidationFlags validationFlags = XmlSchemaValidationFlags.ProcessIdentityConstraints |
            XmlSchemaValidationFlags.AllowXmlAttributes;
            XmlSchemaValidator validator = new XmlSchemaValidator(namespaceManager.NameTable, schemas, namespaceManager, validationFlags);
            validator.Initialize();
            using (XmlReader r = XmlReader.Create(new StringReader(xsd)))
            {
                while (r.Read())
                {
                    switch (r.NodeType)
                    {
                        case XmlNodeType.Element:
                            namespaceManager.PushScope();
                            if (r.MoveToFirstAttribute())
                            {
                                do
                                {
                                    if (r.NamespaceURI == "http://www.w3.org/2000/xmlns/")
                                    {
                                        namespaceManager.AddNamespace(r.LocalName, r.Value);
                                    }
                                } while (r.MoveToNextAttribute());
                                r.MoveToElement();
                            }
                            validator.ValidateElement(r.LocalName, r.NamespaceURI, null, null, null, null, null);
                            if (r.MoveToFirstAttribute())
                            {
                                do
                                {
                                    if (r.NamespaceURI != "http://www.w3.org/2000/xmlns/")
                                    {
                                        validator.ValidateAttribute(r.LocalName, r.NamespaceURI, r.Value, null);
                                    }
                                } while (r.MoveToNextAttribute());
                                r.MoveToElement();
                            }
                            validator.ValidateEndOfAttributes(null);
                            if (r.IsEmptyElement) goto case XmlNodeType.EndElement;
                            break;

                        case XmlNodeType.EndElement:
                            validator.ValidateEndElement(null);
                            namespaceManager.PopScope();
                            break;

                        case XmlNodeType.Text:
                            validator.ValidateText(r.Value);
                            break;

                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.Whitespace:
                            validator.ValidateWhitespace(r.Value);
                            break;

                        default:
                            break;
                    }
                }
                validator.EndValidation();
            }
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ValidationType = ValidationType.Schema;
            rs.Schemas.Add(null, XmlReader.Create(new StringReader(xsd)));

            using (XmlReader r = XmlReader.Create(new StringReader(xml), rs))
            {
                try
                {
                    while (r.Read()) ;
                }
                catch (XmlSchemaValidationException e) { _output.WriteLine(e.Message); return; }
            }
            Assert.True(false);
        }

        public void RunTest(ArrayList schemaList, string xml)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.Schemas.XmlResolver = new XmlUrlResolver();

            for (int i = 0; i < schemaList.Count; ++i)
            {
                XmlSchema schema = XmlSchema.Read(new StringReader((string)schemaList[i]), new ValidationEventHandler(ValidationCallback));
                settings.Schemas.Add(schema);
            }
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            using (XmlReader reader = XmlReader.Create(new StringReader(xml), settings))
                while (reader.Read()) ;
        }
    }
}
