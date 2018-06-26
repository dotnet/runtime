// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    // ===================== Constructor =====================

    public class TCConstructor : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCConstructor(ITestOutputHelper output): base(output)
        {
            _output = output;
        }

        [Fact]
        public void SetXmlNameTableToNull()
        {
            XmlSchemaValidator val;

            try
            {
                val = new XmlSchemaValidator(null, new XmlSchemaSet(), new XmlNamespaceManager(new NameTable()), AllFlags);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            _output.WriteLine("ArgumentNullException was not thrown!");
            Assert.True(false);
        }

        [Theory]
        [InlineData("empty")]
        [InlineData("full")]
        public void SetXmlNameTableTo_Empty_Full(string nameTableStatus)
        {
            XmlSchemaValidator val;
            ObservedNameTable nt = new ObservedNameTable();
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaSet sch = CreateSchemaSetFromXml("<root />");

            if (nameTableStatus == "full")
            {
                nt.Add("root");
                nt.Add("foo");
                nt.IsAddCalled = false;
                nt.IsGetCalled = false;
            }

            val = new XmlSchemaValidator(nt, sch, new XmlNamespaceManager(new NameTable()), AllFlags);
            Assert.NotEqual(val, null);

            val.Initialize();
            val.ValidateElement("root", "", info);

            Assert.True(nt.IsAddCalled);
            Assert.Equal(nt.IsGetCalled, false);

            return;
        }

        [Fact]
        public void SetSchemaSetToNull()
        {
            XmlSchemaValidator val;

            try
            {
                val = new XmlSchemaValidator(new NameTable(), null, new XmlNamespaceManager(new NameTable()), AllFlags);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            _output.WriteLine("ArgumentNullException was not thrown!");
            Assert.True(false);
        }

        [Theory]
        [InlineData("empty")]
        [InlineData("notcompiled")]
        [InlineData("compiled")]
        public void SetSchemaSetTo_Empty_NotCompiled_Compiled(string schemaSetStatus)
        {
            XmlSchemaValidator val;
            XmlSchemaSet sch = new XmlSchemaSet();

            if (schemaSetStatus != "empty")
            {
                sch.Add("", Path.Combine(TestData, XSDFILE_NO_TARGET_NAMESPACE));
                if (schemaSetStatus == "compiled")
                    sch.Compile();
            }

            val = new XmlSchemaValidator(new NameTable(), sch, new XmlNamespaceManager(new NameTable()), AllFlags);
            Assert.NotEqual(val, null);

            val.Initialize();
            val.ValidateElement("elem1", "", null);
            val.SkipToEndElement(null);
            val.EndValidation();

            return;
        }

        // BUG 304774 - resolved
        [Fact]
        public void SetSchemaSetWithInvalidContent_TypeCollision()
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();

            XmlSchemaSet sch = new XmlSchemaSet();

            sch.Add("", XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                          "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n" +
                                                          "    <xs:element name=\"root\" type=\"xs:int\" />\n" +
                                                          "</xs:schema>")));
            sch.Add("", XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                          "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n" +
                                                          "    <xs:element name=\"root\" type=\"xs:string\" />\n" +
                                                          "</xs:schema>")));

            try
            {
                val = new XmlSchemaValidator(new NameTable(), sch, new XmlNamespaceManager(new NameTable()), AllFlags);
            }
            catch (XmlSchemaValidationException)
            {
                return;
            }

            return;
        }

        [Fact]
        public void CustomXmlNameSpaceResolverImplementation()
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();

            XmlSchemaSet sch = new XmlSchemaSet();

            ObservedNamespaceManager nsManager = new ObservedNamespaceManager(new NameTable());
            nsManager.AddNamespace("n1", "uri:tempuri");

            val = new XmlSchemaValidator(new NameTable(), sch, nsManager, AllFlags);

            val.AddSchema(XmlSchema.Read(XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                                     "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\n" +
                                                                     "			 xmlns:n1=\"uri:tempuri\"\n" +
                                                                     "			 targetNamespace=\"uri:tempuri1\">\n" +
                                                                     "    <xs:complexType name=\"foo\">\n" +
                                                                     "        <xs:sequence>\n" +
                                                                     "            <xs:element name=\"bar\" />\n" +
                                                                     "        </xs:sequence>\n" +
                                                                     "    </xs:complexType>\n" +
                                                                     "</xs:schema>")), null));

            val.Initialize();
            val.ValidateElement("root", "", info, "n1:foo", null, null, null);

            Assert.True(nsManager.IsLookupNamespaceCalled);

            return;
        }
    }

    // ===================== AddSchema =====================

    public class TCAddSchema : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCAddSchema(ITestOutputHelper output): base(output)
        {
            _output = output;
        }

        [Fact]
        public void PassNull()
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());

            try
            {
                val.AddSchema(null);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void CheckDeepCopyOfXmlSchema()
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());
            XmlSchemaInfo info = new XmlSchemaInfo();

            XmlSchema s = new XmlSchema();
            XmlSchemaElement e1 = new XmlSchemaElement();
            XmlSchemaElement e2 = new XmlSchemaElement();

            e1.Name = "foo";
            e2.Name = "bar";

            s.Items.Add(e1);
            val.AddSchema(s);
            s.Items.Add(e2);

            val.Initialize();
            try
            {
                val.ValidateElement("bar", "", info);
            }
            catch (XmlSchemaValidationException)
            {
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void AddSameXmlSchemaTwice()
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchema s;

            s = XmlSchema.Read(XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                                 "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n" +
                                                                 "    <xs:element name=\"root\" />\n" +
                                                                 "</xs:schema>")), null);
            val.AddSchema(s);
            val.AddSchema(s);

            val.Initialize();
            val.ValidateElement("root", "", info);

            return;
        }

        [Fact]
        public void AddSameXmlSchemaWithTargetNamespaceTwice()
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchema s;

            s = XmlSchema.Read(XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                                 "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\n" +
                                                                 "           xmlns:n1=\"uri:tempuri\"\n" +
                                                                 "           targetNamespace=\"uri:tempuri\">\n" +
                                                                 "    <xs:element name=\"root\" />\n" +
                                                                 "</xs:schema>")), null);
            val.AddSchema(s);
            val.AddSchema(s);

            val.Initialize();
            val.ValidateElement("root", "uri:tempuri", info);

            return;
        }

        [Fact]
        public void AddSchemasWithTypeCollision()
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.AddSchema(XmlSchema.Read(XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                                           "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n" +
                                                                           "    <xs:element name=\"root\" type=\"xs:string\" />\n" +
                                                                           "</xs:schema>")), null));

            try
            {
                val.AddSchema(XmlSchema.Read(XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                                               "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n" +
                                                                               "    <xs:element name=\"root\" type=\"xs:boolean\" />\n" +
                                                                               "</xs:schema>")), null));
            }
            catch (XmlSchemaValidationException)
            {
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void ValidateThenAddAdditionalSchemas()
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();

            val = CreateValidator(new XmlSchemaSet());

            val.AddSchema(XmlSchema.Read(XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                                           "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\n" +
                                                                           "           targetNamespace=\"uri:tempuri1\">\n" +
                                                                           "    <xs:element name=\"foo\" type=\"xs:string\" />\n" +
                                                                           "</xs:schema>")), null));

            val.Initialize();
            val.ValidateElement("foo", "uri:tempuri1", info);
            val.SkipToEndElement(info);

            val.AddSchema(XmlSchema.Read(XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                                            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\n" +
                                                                            "           targetNamespace=\"uri:tempuri2\">\n" +
                                                                            "    <xs:element name=\"bar\" type=\"xs:string\" />\n" +
                                                                            "</xs:schema>")), null));

            val.ValidateElement("bar", "uri:tempuri2", info);
            val.SkipToEndElement(info);
            val.EndValidation();

            return;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ImportAnotherSchemaThat_Is_IsNot_InSchemaSet(bool importTwice)
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            Uri u = new Uri(Uri.UriSchemeFile + Uri.SchemeDelimiter + Path.Combine(Path.GetFullPath(TestData), XSDFILE_TARGET_NAMESPACE));
            XmlSchema s1;
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.XmlResolver = new XmlUrlResolver();

            val = CreateValidator(new XmlSchemaSet());

            s1 = XmlSchema.Read(new StringReader("<?xml version=\"1.0\"?>\n" +
                                                 "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\n" +
                                                 "           xmlns:temp=\"uri:tempuri\">\n" +
                                                 "    <xs:import namespace=\"uri:tempuri\"\n" +
                                                 "               schemaLocation=\"" + u.AbsoluteUri + "\" />\n" +
                                                 "    <xs:element name=\"root\">\n" +
                                                 "        <xs:complexType>\n" +
                                                 "            <xs:sequence>\n" +
                                                 "                <xs:element ref=\"temp:elem1\" />\n" +
                                                 "            </xs:sequence>\n" +
                                                 "        </xs:complexType>\n" +
                                                 "    </xs:element>\n" +
                                                 "</xs:schema>"), null);

            schemas.Add(s1);

            if (importTwice)
            {
                foreach (XmlSchema s in schemas.Schemas("uri:tempuri"))
                    val.AddSchema(s);
            }
            val.AddSchema(s1);

            val.Initialize();
            val.ValidateElement("root", "", info);
            val.ValidateEndOfAttributes(null);
            val.ValidateElement("elem1", "uri:tempuri", info);
            val.ValidateEndOfAttributes(null);
            val.ValidateEndElement(info);
            val.ValidateEndElement(info);
            val.EndValidation();

            return;
        }

        //(BUG #306858)
        [Fact]
        public void SetIgnoreInlineSchemaFlag_AddSchemaShouldDoNothing()
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();

            val = CreateValidator(CreateSchemaSetFromXml("<root />"), XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessSchemaLocation);

            val.AddSchema(XmlSchema.Read(XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                                           "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n" +
                                                                           "    <xs:element name=\"foo\" type=\"xs:string\" />\n" +
                                                                           "</xs:schema>")), null));

            val.Initialize();

            try
            {
                val.ValidateElement("foo", "", info);
                throw new Exception("Additional schema was loaded!");
            }
            catch (XmlSchemaValidationException)
            {
                return;
            }
        }
    }
}
