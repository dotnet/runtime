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
    // ===================== ValidateAttribute =====================

    public class TCValidateAttribute_String : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCValidateAttribute_String(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("attr", null)]
        public void PassNull_LocalName_Namespace__Invalid(String localName, String nameSpace)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("OneAttributeElement", "", null);
            try
            {
                val.ValidateAttribute(localName, nameSpace, "foo", info);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            Assert.True(false);
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
                val.ValidateAttribute("attr", "", (string)null, info);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void PassNullXmlSchemaInfo__Valid()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("OneAttributeElement", "", null);
            val.ValidateAttribute("attr", "", "foo", null);

            return;
        }

        [Theory]
        [InlineData("RequiredAttribute")]
        [InlineData("OptionalAttribute")]
        [InlineData("DefaultAttribute")]
        [InlineData("FixedAttribute")]
        [InlineData("FixedRequiredAttribute")]
        public void Validate_Required_Optional_Default_Fixed_FixedRequired_Attribute(String attrType)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement(attrType + "Element", "", null);
            val.ValidateAttribute(attrType, "", "foo", info);

            Assert.Equal(info.SchemaAttribute.QualifiedName, new XmlQualifiedName(attrType));
            Assert.Equal(info.Validity, XmlSchemaValidity.Valid);
            Assert.Equal(info.SchemaType.TypeCode, XmlTypeCode.String);

            return;
        }

        [Fact]
        public void ValidateAttributeWithNamespace()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("NamespaceAttributeElement", "", null);
            val.ValidateAttribute("attr1", "uri:tempuri", "123", info);

            Assert.Equal(info.SchemaAttribute.QualifiedName, new XmlQualifiedName("attr1", "uri:tempuri"));
            Assert.Equal(info.Validity, XmlSchemaValidity.Valid);
            Assert.Equal(info.SchemaType.TypeCode, XmlTypeCode.Int);

            return;
        }

        [Fact]
        public void ValidateAnyAttribute()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_ATTRIBUTE);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("AnyAttributeElement", "", null);
            val.ValidateAttribute("SomeAttribute", "", "foo", info);

            Assert.Equal(info.Validity, XmlSchemaValidity.NotKnown);

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
                val.ValidateAttribute(a.QualifiedName.Name, a.QualifiedName.Namespace, a.DefaultValue, info);
                Assert.Equal(info.SchemaAttribute, a);
            }

            atts.Clear();
            val.GetUnspecifiedDefaultAttributes(atts);
            Assert.Equal(atts.Count, 0);

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
            val.ValidateAttribute("BasicAttribute", "", "foo", info);

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
            val.ValidateAttribute("RequiredAttribute", "", "foo", info);

            try
            {
                val.ValidateAttribute("RequiredAttribute", "", "foo", info);
            }
            catch (XmlSchemaValidationException)
            {
                //XmlExceptionVerifier.IsExceptionOk(e, "Sch_DuplicateAttribute", new string[] { "RequiredAttribute" });
                return;
            }

            Assert.True(false);
        }
    }
}
