// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    // ===================== GetExpectedParticles =====================

    public class TCGetExpectedParticles : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCGetExpectedParticles(ITestOutputHelper output): base(output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("ctor")]
        [InlineData("init")]
        [InlineData("end")]
        public void CallAfter_Constructor_Initialize_EndValidation(String after)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);

            if (after == "init")
            {
                val.Initialize();
                Assert.Equal(val.GetExpectedParticles().Length, 18);
            }
            else if (after == "end")
            {
                val.Initialize();
                val.EndValidation();
                CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { });
            }
            else
                CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { });

            return;
        }

        [Theory]
        [InlineData("elem")]
        [InlineData("attrib")]
        [InlineData("endof")]
        public void CallAfterValidate_Element_Attribute_EndOfAttributes_ForSequence(String after)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("SequenceElement", "", info);

            if (after == "attrib")
                val.ValidateAttribute("attr1", "", StringGetter("foo"), info);

            if (after == "endof")
            {
                val.ValidateAttribute("attr1", "", StringGetter("foo"), info);
                val.ValidateAttribute("attr2", "", StringGetter("foo"), info);
                val.ValidateEndOfAttributes(null);
            }

            CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { new XmlQualifiedName("elem1") });

            return;
        }

        [Theory]
        [InlineData("inside")]
        [InlineData("end")]
        public void CallForSequence_Between_After_ValidationAllSeqElements(String callOn)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            XmlQualifiedName[] names;

            val.Initialize();
            val.ValidateElement("SequenceElement", "", info);
            val.ValidateAttribute("attr1", "", StringGetter("foo"), info);
            val.ValidateAttribute("attr2", "", StringGetter("foo"), info);
            val.ValidateEndOfAttributes(null);

            val.ValidateElement("elem1", "", info);
            val.SkipToEndElement(info);

            if (callOn == "end")
            {
                val.ValidateElement("elem2", "", info);
                val.SkipToEndElement(info);

                names = new XmlQualifiedName[] { };
            }
            else
            {
                names = new XmlQualifiedName[] { new XmlQualifiedName("elem2") };
            }

            CheckExpectedElements(val.GetExpectedParticles(), names);

            return;
        }

        [Theory]
        [InlineData("elem")]
        [InlineData("attrib")]
        [InlineData("endof")]
        public void CallAfterValidate_Element_Attribute_EndOfAttributes_ForChoice(String after)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("ChoiceElement", "", info);

            if (after == "attrib")
                val.ValidateAttribute("attr1", "", StringGetter("foo"), info);

            if (after == "endof")
            {
                val.ValidateAttribute("attr1", "", StringGetter("foo"), info);
                val.ValidateAttribute("attr2", "", StringGetter("foo"), info);
                val.ValidateEndOfAttributes(null);
            }

            CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { new XmlQualifiedName("elem1"), new XmlQualifiedName("elem2") });

            return;
        }

        [Theory]
        [InlineData("elem1")]
        [InlineData("elem2")]
        public void CallForChoiceAfterValidating_1_2_ChoiceElement(String elemAfter)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            string elem = elemAfter;

            val.Initialize();
            val.ValidateElement("ChoiceElement", "", info);
            val.ValidateAttribute("attr1", "", StringGetter("foo"), info);
            val.ValidateAttribute("attr2", "", StringGetter("foo"), info);
            val.ValidateEndOfAttributes(null);

            val.ValidateElement(elem, "", info);
            val.SkipToEndElement(info);

            CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { });

            return;
        }

        [Theory]
        [InlineData("elem")]
        [InlineData("attrib")]
        [InlineData("endof")]
        public void CallAfterValidate_Element_Attribute_EndOfAttributes_ForAll(String after)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("AllElement", "", info);

            if (after == "attrib")
                val.ValidateAttribute("attr1", "", StringGetter("foo"), info);

            if (after == "endof")
            {
                val.ValidateAttribute("attr1", "", StringGetter("foo"), info);
                val.ValidateAttribute("attr2", "", StringGetter("foo"), info);
                val.ValidateEndOfAttributes(null);
            }

            CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { new XmlQualifiedName("elem1"), new XmlQualifiedName("elem2") });

            return;
        }

        [Theory]
        [InlineData("elem1")]
        [InlineData("elem2")]
        public void CallForAllAfterValidating_1_2_element(string elemAfter)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            string elem = elemAfter;
            string notElem = (elem == "elem1" ? "elem2" : "elem1");

            val.Initialize();
            val.ValidateElement("AllElement", "", info);
            val.ValidateAttribute("attr1", "", StringGetter("foo"), info);
            val.ValidateAttribute("attr2", "", StringGetter("foo"), info);
            val.ValidateEndOfAttributes(null);

            val.ValidateElement(elem, "", info);
            val.SkipToEndElement(info);

            CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { new XmlQualifiedName(notElem) });

            return;
        }

        [Fact]
        public void CallForAllAfterValidatingBothElements()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("AllElement", "", info);
            val.ValidateAttribute("attr1", "", StringGetter("foo"), info);
            val.ValidateAttribute("attr2", "", StringGetter("foo"), info);
            val.ValidateEndOfAttributes(null);

            foreach (string elem in new string[] { "elem1", "elem2" })
            {
                val.ValidateElement(elem, "", info);
                val.SkipToEndElement(info);
            }

            CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { });

            return;
        }

        [Fact]
        public void CallForElementWithReferenceToGlobalElement()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("ReferenceElement", "", info);
            val.ValidateEndOfAttributes(null);

            foreach (string elem in new string[] { "NestedElement", "foo", "bar" })
            {
                CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { new XmlQualifiedName(elem) });

                val.ValidateElement(elem, "", info);
                val.ValidateEndOfAttributes(null);
            }

            foreach (string elem in new string[] { "bar", "foo", "NestedElement" })
            {
                val.ValidateEndElement(info);
                CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { });
            }

            return;
        }

        [Fact]
        public void CallForElementWithZeroMinOccurs()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("MinOccurs0Element", "", info);
            val.ValidateEndOfAttributes(null);

            CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { new XmlQualifiedName("foo"), new XmlQualifiedName("bar") });

            return;
        }

        [Fact]
        public void CallForElementWithZeroMaxOccurs()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("MaxOccurs0Element", "", info);
            val.ValidateEndOfAttributes(null);

            CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { new XmlQualifiedName("bar") });

            return;
        }

        [Theory]
        [InlineData("before")]
        [InlineData("after")]
        public void CallForSequence_Before_After_ValidatingWildcard(String callOrder)
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaSet schemas = new XmlSchemaSet();
            XmlSchemaParticle[] result;

            schemas.Add("", Path.Combine(TestData, XSDFILE_GET_EXPECTED_PARTICLES));
            schemas.Add("uri:tempuri", Path.Combine(TestData, XSDFILE_TARGET_NAMESPACE));
            val = CreateValidator(schemas);

            val.Initialize();
            val.ValidateElement("SequenceWildcardElement", "", info);
            val.ValidateEndOfAttributes(null);

            if (callOrder == "before")
            {
                result = val.GetExpectedParticles();

                Assert.Equal(result.Length, 1);
                Assert.True(result[0] is XmlSchemaAny);
                Assert.Equal((result[0] as XmlSchemaAny).Namespace, "uri:tempuri");
                Assert.Equal((result[0] as XmlSchemaAny).ProcessContents, XmlSchemaContentProcessing.Strict);
            }
            else
            {
                val.ValidateElement("elem1", "uri:tempuri", info);
                val.SkipToEndElement(info);
                CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { new XmlQualifiedName("foo") });
            }

            return;
        }

        [Theory]
        [InlineData("before")]
        [InlineData("after")]
        public void CallForChoice_Before_After_ValidatingWildcard(String callOrder)
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaSet schemas = new XmlSchemaSet();
            XmlSchemaParticle[] result;

            schemas.Add("", Path.Combine(TestData, XSDFILE_GET_EXPECTED_PARTICLES));
            schemas.Add("uri:tempuri", Path.Combine(TestData, XSDFILE_TARGET_NAMESPACE));
            val = CreateValidator(schemas);

            val.Initialize();
            val.ValidateElement("ChoiceWildcardElement", "", info);
            val.ValidateEndOfAttributes(null);

            if (callOrder == "before")
            {
                result = val.GetExpectedParticles();

                Assert.Equal(result.Length, 2);

                if (result[0] is XmlSchemaAny)
                {
                    Assert.Equal((result[0] as XmlSchemaAny).Namespace, "uri:tempuri");
                    Assert.Equal((result[0] as XmlSchemaAny).ProcessContents, XmlSchemaContentProcessing.Strict);

                    Assert.True(result[1] is XmlSchemaElement);
                    Assert.Equal((result[1] as XmlSchemaElement).QualifiedName, new XmlQualifiedName("foo"));
                }
                else
                {
                    Assert.True(result[1] is XmlSchemaAny);
                    Assert.Equal((result[1] as XmlSchemaAny).Namespace, "uri:tempuri");
                    Assert.Equal((result[1] as XmlSchemaAny).ProcessContents, XmlSchemaContentProcessing.Strict);

                    Assert.True(result[0] is XmlSchemaElement);
                    Assert.Equal((result[0] as XmlSchemaElement).QualifiedName, new XmlQualifiedName("foo"));
                }
            }
            else
            {
                val.ValidateElement("elem1", "uri:tempuri", info);
                val.SkipToEndElement(info);

                CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { });
            }

            return;
        }

        [Theory]
        [InlineData("before")]
        [InlineData("after")]
        public void CallForSequenceWithChoiceGroup_Before_After_ValidatingGroupMembers(String callOrder)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlQualifiedName[] names;

            val.Initialize();
            val.ValidateElement("SequenceGroupElement", "", info);
            val.ValidateEndOfAttributes(null);

            if (callOrder == "before")
            {
                names = new XmlQualifiedName[] { new XmlQualifiedName("g1"), new XmlQualifiedName("g2") };
            }
            else
            {
                val.ValidateElement("g1", "", info);
                val.SkipToEndElement(info);

                names = new XmlQualifiedName[] { new XmlQualifiedName("foo") };
            }

            CheckExpectedElements(val.GetExpectedParticles(), names);

            return;
        }

        [Theory]
        [InlineData("before")]
        [InlineData("after")]
        public void CallForChoiceWithSequenceGroup_Before_After_ValidatingGroupMembers(String callOrder)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlQualifiedName[] names;

            val.Initialize();
            val.ValidateElement("ChoiceGroupElement", "", info);
            val.ValidateEndOfAttributes(null);

            if (callOrder == "before")
            {
                names = new XmlQualifiedName[] { new XmlQualifiedName("g1"), new XmlQualifiedName("foo") };
            }
            else
            {
                val.ValidateElement("g1", "", info);
                val.SkipToEndElement(info);

                names = new XmlQualifiedName[] { new XmlQualifiedName("g2") };
            }

            CheckExpectedElements(val.GetExpectedParticles(), names);

            return;
        }

        [Theory]
        [InlineData("before")]
        [InlineData("after")]
        public void CallForExtendedSequence_Before_After_ValidatingSeqOrAllBaseElements(String callOrder)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            XmlQualifiedName[] names;

            val.Initialize();
            val.ValidateElement("SequenceExtensionElement", "", info);
            val.ValidateEndOfAttributes(null);

            if (callOrder == "before")
            {
                names = new XmlQualifiedName[] { new XmlQualifiedName("elem1") };
            }
            else
            {
                val.ValidateElement("elem1", "", info);
                val.ValidateEndElement(info);
                val.ValidateElement("elem2", "", info);
                val.ValidateEndElement(info);

                names = new XmlQualifiedName[] { new XmlQualifiedName("extended") };
            }

            CheckExpectedElements(val.GetExpectedParticles(), names);

            return;
        }

        [Theory]
        [InlineData("before")]
        [InlineData("after")]
        public void CallForExtendedChoice_Before_After_ValidatingBaseChoiceElement(String callOrder)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            XmlQualifiedName[] names;

            val.Initialize();
            val.ValidateElement("ChoiceExtensionElement", "", info);
            val.ValidateEndOfAttributes(null);

            if (callOrder == "before")
            {
                names = new XmlQualifiedName[] { new XmlQualifiedName("elem1"), new XmlQualifiedName("elem2") };
            }
            else
            {
                val.ValidateElement("elem1", "", info);
                val.ValidateEndElement(info);

                names = new XmlQualifiedName[] { new XmlQualifiedName("ext1"), new XmlQualifiedName("ext2") };
            }

            CheckExpectedElements(val.GetExpectedParticles(), names);

            return;
        }

        [Theory]
        [InlineData("Sequence", "before")]
        [InlineData("Sequence", "after")]
        [InlineData("Choice", "before")]
        [InlineData("Choice", "after" )]
        [InlineData("All", "before")]
        [InlineData("All", "after")]
        public void CallForRestricted_Sequence_Choice_All__Before_After_ValidatingSeqElements(String restrType, String callOrder)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            XmlQualifiedName[] names;

            val.Initialize();
            val.ValidateElement(restrType + "RestrictionElement", "", info);
            val.ValidateEndOfAttributes(null);

            if (callOrder == "before")
            {
                names = new XmlQualifiedName[] { new XmlQualifiedName("elem1") };
            }
            else
            {
                val.ValidateElement("elem1", "", info);
                val.ValidateEndElement(info);

                names = new XmlQualifiedName[] { };
            }

            CheckExpectedElements(val.GetExpectedParticles(), names);

            return;
        }

        [Fact]
        public void CallForChoiceWithElementsFromDifferentNamespaces()
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaSet schemas = new XmlSchemaSet();

            schemas.Add("", XmlReader.Create(new StringReader("<?xml version=\"1.0\"?>\n" +
                                                              "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\n" +
                                                              "           xmlns:temp=\"uri:tempuri\">\n" +
                                                              "    <xs:import namespace=\"uri:tempuri\" />\n" +
                                                              "    <xs:element name=\"ImportElement\">\n" +
                                                              "        <xs:complexType>\n" +
                                                              "            <xs:choice>\n" +
                                                              "                <xs:element name=\"elem1\" />\n" +
                                                              "                <xs:element ref=\"temp:elem1\" />\n" +
                                                              "                <xs:element name=\"elem2\" />\n" +
                                                              "            </xs:choice>\n" +
                                                              "        </xs:complexType>\n" +
                                                              "    </xs:element>\n" +
                                                              "</xs:schema>")));
            schemas.Add("uri:tempuri", Path.Combine(TestData, XSDFILE_TARGET_NAMESPACE));
            val = CreateValidator(schemas);

            val.Initialize();
            val.ValidateElement("ImportElement", "", info);
            val.ValidateEndOfAttributes(null);

            CheckExpectedElements(val.GetExpectedParticles(), new XmlQualifiedName[] { new XmlQualifiedName("elem1"), new XmlQualifiedName("elem1", "uri:tempuri"), new XmlQualifiedName("elem2") });

            return;
        }

        [Fact]
        public void CallForElementWithoutTypeDefined()
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaSet schemas = new XmlSchemaSet();
            XmlSchemaParticle[] result;

            val = CreateValidator(XSDFILE_GET_EXPECTED_PARTICLES);

            val.Initialize();
            val.ValidateElement("NoTypeElement", "", info);
            val.ValidateEndOfAttributes(null);

            result = val.GetExpectedParticles();

            Assert.Equal(result.Length, 1);

            Assert.True(result[0] is XmlSchemaAny);
            Assert.Equal((result[0] as XmlSchemaAny).Namespace, null);
            Assert.Equal((result[0] as XmlSchemaAny).ProcessContents, XmlSchemaContentProcessing.Lax);

            return;
        }

        private void CheckExpectedElements(XmlSchemaParticle[] result, XmlQualifiedName[] names)
        {
            int cntFound;

            Assert.Equal(result.Length, names.Length);

            foreach (XmlSchemaParticle res in result)
                Assert.True(res is XmlSchemaElement);

            foreach (XmlQualifiedName n in names)
            {
                cntFound = 0;
                foreach (XmlSchemaParticle res in result)
                {
                    if (n == (res as XmlSchemaElement).QualifiedName)
                        cntFound++;
                }
                Assert.True(cntFound != 0);
                Assert.True(cntFound <= 1);
            }
        }
    }
}
