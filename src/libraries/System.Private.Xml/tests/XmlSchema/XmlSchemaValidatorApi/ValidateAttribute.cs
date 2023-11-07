// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.IO;
using System.Xml.Schema;
using System.Xml.Tests;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.XmlSchemaValidatorApiTests
{
    // ===================== ValidateAttribute =====================

    public class TCValidateAttribute : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        private ExceptionVerifier _exVerifier;

        public TCValidateAttribute(ITestOutputHelper output) : base(output)
        {
            _output = output;
            _exVerifier = new ExceptionVerifier("System.Xml", _output);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("attr", null)]
        public void PassNull_LocalName_NameSpace__Invalid(string localName, string nameSpace)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("OneAttributeElement", "", null);
            try
            {
                val.ValidateAttribute(localName, nameSpace, StringGetter("foo"), info);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            Assert.Fail();
        }

        [Fact]
        public void PassNullValueGetter__Invalid()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("OneAttributeElement", "", null);
            try
            {
                val.ValidateAttribute("attr", "", (XmlValueGetter)null, info);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            Assert.Fail();
        }

        [Fact]
        public void PassNullXmlSchemaInfo__Valid()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);

            val.Initialize();
            val.ValidateElement("OneAttributeElement", "", null);
            val.ValidateAttribute("attr", "", StringGetter("foo"), null);

            return;
        }

        [Theory]
        [InlineData("RequiredAttribute")]
        [InlineData("OptionalAttribute")]
        [InlineData("DefaultAttribute")]
        [InlineData("FixedAttribute")]
        [InlineData("FixedRequiredAttribute")]
        public void Validate_Required_Optional_Default_Fixed_FixedRequired_Attribute(string attrType)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement(attrType + "Element", "", null);
            val.ValidateAttribute(attrType, "", StringGetter("foo"), info);

            Assert.Equal(info.SchemaAttribute.QualifiedName, new XmlQualifiedName(attrType));
            Assert.Equal(XmlSchemaValidity.Valid, info.Validity);
            Assert.Equal(XmlTypeCode.String, info.SchemaType.TypeCode);

            return;
        }

        [Fact]
        public void ValidateAttributeWithNamespace()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("NamespaceAttributeElement", "", null);
            val.ValidateAttribute("attr1", "uri:tempuri", StringGetter("123"), info);

            Assert.Equal(info.SchemaAttribute.QualifiedName, new XmlQualifiedName("attr1", "uri:tempuri"));
            Assert.Equal(XmlSchemaValidity.Valid, info.Validity);
            Assert.Equal(XmlTypeCode.Int, info.SchemaType.TypeCode);

            return;
        }

        [Fact]
        public void ValidateAnyAttribute()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("AnyAttributeElement", "", null);
            val.ValidateAttribute("SomeAttribute", "", StringGetter("foo"), info);

            Assert.Equal(XmlSchemaValidity.NotKnown, info.Validity);

            return;
        }

        [Fact]
        public void AskForDefaultAttributesAndValidateThem()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_200_DEF_ATTRIBUTES);
            XmlSchemaInfo info = new XmlSchemaInfo();
            ArrayList atts = new ArrayList();

            val.Initialize();
            val.ValidateElement("StressElement", "", null);
            val.GetUnspecifiedDefaultAttributes(atts);

            foreach (XmlSchemaAttribute a in atts)
            {
                val.ValidateAttribute(a.QualifiedName.Name, a.QualifiedName.Namespace, StringGetter(a.DefaultValue), info);
                Assert.Equal(info.SchemaAttribute, a);
            }

            atts.Clear();
            val.GetUnspecifiedDefaultAttributes(atts);
            Assert.Equal(0, atts.Count);

            return;
        }

        [Fact]
        public void ValidateTopLevelAttribute()
        {
            XmlSchemaValidator val;
            XmlSchemaSet schemas = new XmlSchemaSet();
            XmlSchemaInfo info = new XmlSchemaInfo();

            schemas.Add("", Path.Combine(TestData, XSDFILE_200_DEF_ATTRIBUTES));
            schemas.Compile();
            val = CreateValidator(schemas);

            val.Initialize();
            val.ValidateAttribute("BasicAttribute", "", StringGetter("foo"), info);

            Assert.Equal(info.SchemaAttribute, schemas.GlobalAttributes[new XmlQualifiedName("BasicAttribute")]);

            return;
        }

        [Fact]
        public void ValidateSameAttributeTwice()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("RequiredAttributeElement", "", null);
            val.ValidateAttribute("RequiredAttribute", "", StringGetter("foo"), info);

            try
            {
                val.ValidateAttribute("RequiredAttribute", "", StringGetter("foo"), info);
            }
            catch (XmlSchemaValidationException e)
            {
                _exVerifier.IsExceptionOk(e, "Sch_DuplicateAttribute", new string[] { "RequiredAttribute" });
                return;
            }

            Assert.Fail();
        }
    }

    // ===================== GetUnspecifiedDefaultAttributes =====================

    public class TCGetUnspecifiedDefaultAttributes : CXmlSchemaValidatorTestCase
    {
        public TCGetUnspecifiedDefaultAttributes(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void PassNull__Invalid()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);

            val.Initialize();
            val.ValidateElement("OneAttributeElement", "", null);
            try
            {
                val.GetUnspecifiedDefaultAttributes(null);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            Assert.Fail();
        }

        [Fact]
        public void CallTwice()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            ArrayList atts = new ArrayList();

            val.Initialize();
            val.ValidateElement("MixedAttributesElement", "", null);

            val.GetUnspecifiedDefaultAttributes(atts);
            CheckDefaultAttributes(atts, new string[] { "def1", "def2" });

            atts.Clear();
            val.GetUnspecifiedDefaultAttributes(atts);
            CheckDefaultAttributes(atts, new string[] { "def1", "def2" });

            return;
        }

        [Fact]
        public void NestBetweenValidateAttributeCalls()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            ArrayList atts = new ArrayList();

            val.Initialize();
            val.ValidateElement("MixedAttributesElement", "", null);

            val.GetUnspecifiedDefaultAttributes(atts);
            CheckDefaultAttributes(atts, new string[] { "def1", "def2" });

            val.ValidateAttribute("req1", "", StringGetter("foo"), null);

            atts.Clear();
            val.GetUnspecifiedDefaultAttributes(atts);
            CheckDefaultAttributes(atts, new string[] { "def1", "def2" });

            val.ValidateAttribute("req2", "", StringGetter("foo"), null);

            atts.Clear();
            val.GetUnspecifiedDefaultAttributes(atts);
            CheckDefaultAttributes(atts, new string[] { "def1", "def2" });

            return;
        }

        [Fact]
        public void CallAfterGetExpectedAttributes()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            ArrayList atts = new ArrayList();

            val.Initialize();
            val.ValidateElement("MixedAttributesElement", "", null);

            val.ValidateAttribute("req1", "", StringGetter("foo"), null);
            val.GetExpectedAttributes();

            val.GetUnspecifiedDefaultAttributes(atts);
            CheckDefaultAttributes(atts, new string[] { "def1", "def2" });

            return;
        }

        [Fact]
        public void CallAfterValidatingSomeDefaultAttributes()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            ArrayList atts = new ArrayList();

            val.Initialize();
            val.ValidateElement("MixedAttributesElement", "", null);

            val.ValidateAttribute("def1", "", StringGetter("foo"), null);

            val.GetUnspecifiedDefaultAttributes(atts);
            CheckDefaultAttributes(atts, new string[] { "def2" });

            val.ValidateAttribute("def2", "", StringGetter("foo"), null);

            atts.Clear();
            val.GetUnspecifiedDefaultAttributes(atts);
            CheckDefaultAttributes(atts, new string[] { });

            return;
        }

        [Fact]
        public void CallOnElementWithFixedAttribute()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            ArrayList atts = new ArrayList();

            val.Initialize();
            val.ValidateElement("FixedAttributeElement", "", null);

            val.GetUnspecifiedDefaultAttributes(atts);
            CheckDefaultAttributes(atts, new string[] { "FixedAttribute" });

            return;
        }

        [Fact]
        public void v6a()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            ArrayList atts = new ArrayList();

            val.Initialize();
            val.ValidateElement("FixedRequiredAttributeElement", "", null);

            val.GetUnspecifiedDefaultAttributes(atts);
            CheckDefaultAttributes(atts, new string[] { });

            return;
        }

        private void CheckDefaultAttributes(ArrayList actual, string[] expected)
        {
            int nFound;

            Assert.Equal(actual.Count, expected.Length);
            foreach (string str in expected)
            {
                nFound = 0;
                foreach (XmlSchemaAttribute attr in actual)
                {
                    if (attr.QualifiedName.Name == str)
                        nFound++;
                }
                Assert.Equal(1, nFound);
            }
        }
    }

    // ===================== ValidateEndOfAttributes =====================

    public class TCValidateEndOfAttributes : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        private ExceptionVerifier _exVerifier;

        public TCValidateEndOfAttributes(ITestOutputHelper output) : base(output)
        {
            _output = output;
            _exVerifier = new ExceptionVerifier("System.Xml", _output);
        }

        [Fact]
        public void CallOnELementWithNoAttributes()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);

            val.Initialize();
            val.ValidateElement("NoAttributesElement", "", null);
            val.ValidateEndOfAttributes(null);

            return;
        }

        [Fact]
        public void CallAfterValidationOfAllAttributes()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);

            val.Initialize();
            val.ValidateElement("MixedAttributesElement", "", null);
            foreach (string attr in new string[] { "req1", "req2", "def1", "def2" })
                val.ValidateAttribute(attr, "", StringGetter("foo"), null);
            val.ValidateEndOfAttributes(null);

            return;
        }

        [Theory]
        [InlineData("OptionalAttribute")]
        [InlineData("FixedAttribute")]
        public void CallWithoutValidationOf_Optional_Fixed_Attributes(string attrType)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);

            val.Initialize();
            val.ValidateElement(attrType + "Element", "", null);
            val.ValidateEndOfAttributes(null);

            return;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CallWithoutValidationOfDefaultAttributesGetUnspecifiedDefault_Called_NotCalled(bool call)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            ArrayList atts = new ArrayList();

            val.Initialize();
            val.ValidateElement("DefaultAttributeElement", "", null);

            if (call)
                val.GetUnspecifiedDefaultAttributes(atts);

            val.ValidateEndOfAttributes(null);

            return;
        }

        [Fact]
        public void CallWithoutValidationOfRequiredAttribute()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);

            val.Initialize();
            val.ValidateElement("RequiredAttributeElement", "", null);

            try
            {
                val.ValidateEndOfAttributes(null);
            }
            catch (XmlSchemaValidationException e)
            {
                _exVerifier.IsExceptionOk(e, "Sch_MissRequiredAttribute", new string[] { "RequiredAttribute" });
                return;
            }

            Assert.Fail();
        }
    }
}
