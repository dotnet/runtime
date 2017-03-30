// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    // ===================== GetExpectedAttributes =====================

    public class TCGetExpectedAttributes : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCGetExpectedAttributes(ITestOutputHelper output): base(output)
        {
            _output = output;
        }

        // this test could change if spec bug #303601 is fixed
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CallAtRootLevel_Without_With_PartialValidationSet(bool partialValidation)
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaSet schemas = CreateSchemaSet("", "<?xml version=\"1.0\"?>\n" +
                                                       "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n" +
                                                       "    <xs:attribute name=\"attr1\" />\n" +
                                                       "    <xs:attribute name=\"attr2\" />\n" +
                                                       "</xs:schema>");
            schemas.Compile();
            val = CreateValidator(schemas);

            if (partialValidation)
            {
                val.Initialize(schemas.GlobalAttributes[new XmlQualifiedName("attr1")]);
                CheckExpectedAttributes(val.GetExpectedAttributes(), new XmlQualifiedName[] { new XmlQualifiedName("attr1") });
            }
            else
            {
                val.Initialize();
                CheckExpectedAttributes(val.GetExpectedAttributes(), new XmlQualifiedName[] { });
            }

            return;
        }

        [Fact]
        public void CallOnElementWithNoAttributesAfterValidateElement()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_ATTRIBUTES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("NoAttributesElement", "", info);

            CheckExpectedAttributes(val.GetExpectedAttributes(), new XmlQualifiedName[] { });

            return;
        }

        [Theory]
        [InlineData("Required")]
        [InlineData("Optional")]
        [InlineData("Default")]
        [InlineData("Fixed")]
        [InlineData("FixedRequired")]
        public void CallOnElementWith_Required_Optional_Default_Fixed_FixedRequired_AttributesAfterValidateElement(String attrType)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_ATTRIBUTES);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement(attrType + "AttributesElement", "", info);

            CheckExpectedAttributes(val.GetExpectedAttributes(), new XmlQualifiedName[] { new XmlQualifiedName("a1"), new XmlQualifiedName("a2") });

            return;
        }

        [Theory]
        [InlineData("Required", "before")]
        [InlineData("Required", "after")]
        [InlineData("Optional", "before")]
        [InlineData("Optional", "after")]
        [InlineData("Fixed", "before")]
        [InlineData("Fixed", "after")]
        [InlineData("FixedRequired", "before")]
        [InlineData("FixedRequired", "after")]
        public void Call_Before_After_GetUnspecifiedDefaultAttributeWhenJust_Required_Optional_Fixed_FixedRequired_AttributesAreLeft(String attrType, String callOrder)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_ATTRIBUTES);
            XmlSchemaInfo info = new XmlSchemaInfo();
            ArrayList def = new ArrayList();

            val.Initialize();
            val.ValidateElement(attrType + "AttributesElement", "", info);
            val.ValidateAttribute("a1", "", StringGetter("hgd"), info);

            if (callOrder == "after")
                val.GetUnspecifiedDefaultAttributes(def);

            CheckExpectedAttributes(val.GetExpectedAttributes(), new XmlQualifiedName[] { new XmlQualifiedName("a2") });

            return;
        }

        //after: (bug #306460)
        [Theory]
        [InlineData("before")]
        [InlineData("after")]
        public void Call_Before_After_GetUnspecifiedDefaultAttributesWhenJustDefaultAttributesAreLeft(String callOrder)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_GET_EXPECTED_ATTRIBUTES);
            XmlSchemaInfo info = new XmlSchemaInfo();
            ArrayList def = new ArrayList();
            XmlQualifiedName[] names;

            val.Initialize();
            val.ValidateElement("DefaultAttributesElement", "", info);
            val.ValidateAttribute("a1", "", StringGetter("hgd"), info);

            if (callOrder == "after")
                val.GetUnspecifiedDefaultAttributes(def);

            names = new XmlQualifiedName[] { new XmlQualifiedName("a2") };
            CheckExpectedAttributes(val.GetExpectedAttributes(), names);

            return;
        }

        private void CheckExpectedAttributes(XmlSchemaAttribute[] result, XmlQualifiedName[] names)
        {
            int cntFound;

            Assert.Equal(result.Length, names.Length);

            foreach (XmlQualifiedName n in names)
            {
                cntFound = 0;
                foreach (XmlSchemaAttribute res in result)
                {
                    if (n == res.QualifiedName)
                        cntFound++;
                }
                Assert.True(cntFound != 0);
                Assert.True(cntFound <= 1);
            }
        }
    }
}