// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    // ===================== Initialize =====================

    public class TCInitialize : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        private ExceptionVerifier _exVerifier;

        public TCInitialize(ITestOutputHelper output): base(output)
        {
            _output = output;
            _exVerifier = new ExceptionVerifier("System.Xml", _output);
        }

        [Fact]
        public void InitializeShouldResetIDConstraints()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_IDENTITY_CONSTRAINS);
            XmlSchemaInfo info = new XmlSchemaInfo();

            for (int i = 0; i < 2; i++)
            {
                val.Initialize();

                val.ValidateElement("rootIDs", "", info);
                val.ValidateEndOfAttributes(null);
                val.ValidateElement("foo", "", info);
                val.ValidateAttribute("attr", "", StringGetter("a1"), info);
                val.ValidateEndOfAttributes(null);
                val.ValidateEndElement(info);
                val.ValidateEndElement(info);

                val.EndValidation();
            }

            return;
        }

        [Fact]
        public void InitializeShouldNotResetLineInfoProvider()
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());
            CDummyLineInfo LineInfo = new CDummyLineInfo(10, 10);

            val.Initialize();
            val.LineInfoProvider = LineInfo;
            val.EndValidation();

            val.Initialize();

            Assert.Equal(LineInfo, val.LineInfoProvider);

            return;
        }

        [Fact]
        public void InitializeShouldNotResetSourceUri()
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());
            Uri srcUri = new Uri("urn:foo.bar");

            val.Initialize();
            val.SourceUri = srcUri;
            val.EndValidation();

            val.Initialize();

            Assert.Equal(srcUri, val.SourceUri);

            return;
        }

        [Fact]
        public void InitializeShouldNotResetValidationEventSender()
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());
            CDummyLineInfo LineInfo = new CDummyLineInfo(10, 10);

            val.Initialize();
            val.ValidationEventSender = LineInfo;
            val.EndValidation();

            val.Initialize();

            Assert.Equal(LineInfo, val.ValidationEventSender);

            return;
        }

        [Fact]
        public void InitializeShouldNotResetInternalSchemaSet()
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();

            val.AddSchema(XmlSchema.Read(XmlReader.Create(new StringReader("<?xml version=\"1.0\" ?>\n" +
                                                                           "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n" +
                                                                           "    <xs:element name=\"root\" type=\"xs:string\" />\n" +
                                                                           "</xs:schema>")), null));

            val.EndValidation();
            val.Initialize();

            val.ValidateElement("root", "", info);

            Assert.True(info.SchemaElement != null);

            return;
        }

        [Fact]
        public void InitializeShouldNotResetXmlResolver()
        {
            XmlSchemaInfo info = new XmlSchemaInfo();

            CXmlTestResolver res = new CXmlTestResolver();
            CResolverHolder holder = new CResolverHolder();

            res.CalledResolveUri += new XmlTestResolverEventHandler(holder.CallBackResolveUri);
            res.CalledGetEntity += new XmlTestResolverEventHandler(holder.CallBackGetEntity);

            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());

            val.Initialize();
            val.XmlResolver = res;
            val.EndValidation();

            val.Initialize();
            val.ValidateElement("foo", "", info, "type1", null, null, Path.Combine(TestData, XSDFILE_NO_TARGET_NAMESPACE));
            val.ValidateEndOfAttributes(null);
            val.ValidateElement("bar", "", info);
            val.ValidateEndOfAttributes(null);
            val.ValidateEndElement(info);
            val.ValidateEndElement(info);
            val.EndValidation();

            Assert.True(holder.IsCalledResolveUri);

            return;
        }

        //  Second overload
        //(BUG #306951)
        [Fact]
        public void PassNull()
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());

            try
            {
                val.Initialize(null);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void InitializeWithElementValidateSameElement()
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaSet schemas = new XmlSchemaSet();

            schemas.Add("", Path.Combine(TestData, XSDFILE_PARTIAL_VALIDATION));
            schemas.Compile();

            val = CreateValidator(schemas);
            val.Initialize(schemas.GlobalElements[new XmlQualifiedName("PartialElement")]);

            val.ValidateElement("PartialElement", "", info);
            val.ValidateEndOfAttributes(null);
            val.ValidateText(StringGetter("123"));
            val.ValidateEndElement(info);

            Assert.Equal(info.Validity, XmlSchemaValidity.Valid);
            Assert.Equal(info.SchemaElement.Name, "PartialElement");

            return;
        }

        [Theory]
        [InlineData("other")]
        [InlineData("type")]
        [InlineData("attribute")]
        public void InitializeWithElementValidate_OtherElement_Type_Attribute(String typeToValidate)
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaSet schemas = new XmlSchemaSet();

            schemas.Add("", Path.Combine(TestData, XSDFILE_PARTIAL_VALIDATION));
            schemas.Compile();

            val = CreateValidator(schemas);
            val.Initialize(schemas.GlobalElements[new XmlQualifiedName("PartialElement")]);

            try
            {
                switch (typeToValidate)
                {
                    case "other":
                        val.ValidateElement("PartialElement2", "", info);
                        break;

                    case "type":
                        val.ValidateElement("foo", "", info, "PartialType", null, null, null);
                        break;

                    case "attribute":
                        val.ValidateAttribute("PartialAttribute", "", StringGetter("123"), info);
                        break;

                    default:
                        Assert.True(false);
                        break;
                }
            }
            catch (XmlSchemaValidationException e)
            {
                switch (typeToValidate)
                {
                    case "other":
                        _exVerifier.IsExceptionOk(e, "Sch_SchemaElementNameMismatch", new string[] { "PartialElement2", "PartialElement" });
                        break;
                    case "type":
                        _exVerifier.IsExceptionOk(e, "Sch_SchemaElementNameMismatch", new string[] { "foo", "PartialElement" });
                        break;
                    case "attribute":
                        _exVerifier.IsExceptionOk(e, "Sch_ValidateAttributeInvalidCall");
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void InitializeWithTypeValidateSameType()
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaSet schemas = new XmlSchemaSet();

            schemas.Add("", Path.Combine(TestData, XSDFILE_PARTIAL_VALIDATION));
            schemas.Compile();

            val = CreateValidator(schemas);
            val.Initialize(schemas.GlobalTypes[new XmlQualifiedName("PartialType")]);

            val.ValidateElement("foo", "", info, "PartialType", null, null, null);
            val.ValidateEndOfAttributes(null);
            val.ValidateEndElement(info);

            Assert.Equal(info.Validity, XmlSchemaValidity.Valid);
            Assert.Equal(info.SchemaType.Name, "PartialType");

            return;
        }

        [Theory]
        [InlineData("other")]
        [InlineData("attribute")]
        public void InitializeWithTypeValidate_OtherType_Attribute(String typeToValidate)
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaSet schemas = new XmlSchemaSet();

            schemas.Add("", Path.Combine(TestData, XSDFILE_PARTIAL_VALIDATION));
            schemas.Compile();

            val = CreateValidator(schemas);
            val.Initialize(schemas.GlobalTypes[new XmlQualifiedName("PartialType")]);

            try
            {
                switch (typeToValidate)
                {
                    case "other":
                        val.ValidateElement("foo", "", info, "PartialType2", null, null, null);
                        break;

                    case "attribute":
                        val.ValidateAttribute("PartialAttribute", "", StringGetter("123"), info);
                        break;

                    default:
                        Assert.True(false);
                        break;
                }
            }
            catch (XmlSchemaValidationException e)
            {
                switch (typeToValidate)
                {
                    case "other":
                        _exVerifier.IsExceptionOk(e, "Sch_XsiTypeBlockedEx", new string[] { "PartialType2", "foo" });
                        break;
                    case "attribute":
                        _exVerifier.IsExceptionOk(e, "Sch_ValidateAttributeInvalidCall");
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void InitializeWithTypeValidateElement()
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaSet schemas = new XmlSchemaSet();

            schemas.Add("", Path.Combine(TestData, XSDFILE_PARTIAL_VALIDATION));
            schemas.Compile();

            val = CreateValidator(schemas);
            val.Initialize(schemas.GlobalTypes[new XmlQualifiedName("PartialType")]);

            try
            {
                val.ValidateElement("PartialElement", "", info);
            }
            catch (XmlSchemaValidationException)
            {
                Assert.True(false);
            }

            return;
        }

        [Fact]
        public void InitializeWithAttributeValidateSameAttribute()
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaSet schemas = new XmlSchemaSet();

            schemas.Add("", Path.Combine(TestData, XSDFILE_PARTIAL_VALIDATION));
            schemas.Compile();

            val = CreateValidator(schemas);
            val.Initialize(schemas.GlobalAttributes[new XmlQualifiedName("PartialAttribute")]);

            val.ValidateAttribute("PartialAttribute", "", StringGetter("123"), info);

            Assert.Equal(info.Validity, XmlSchemaValidity.Valid);
            Assert.Equal(info.SchemaAttribute.Name, "PartialAttribute");

            return;
        }

        [Theory]
        [InlineData("other")]
        [InlineData("element")]
        [InlineData("type")]
        public void InitializeWithAttributeValidate_OtherAttribute_Element_Type(String typeToValidate)
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaSet schemas = new XmlSchemaSet();

            schemas.Add("", Path.Combine(TestData, XSDFILE_PARTIAL_VALIDATION));
            schemas.Compile();

            val = CreateValidator(schemas);
            val.Initialize(schemas.GlobalAttributes[new XmlQualifiedName("PartialAttribute")]);

            try
            {
                switch (typeToValidate)
                {
                    case "other":
                        val.ValidateAttribute("PartialAttribute2", "", StringGetter("123"), info);
                        break;

                    case "element":
                        val.ValidateElement("PartialElement", "", info);
                        break;

                    case "type":
                        val.ValidateElement("foo", "", info, "PartialType", null, null, null);
                        break;

                    default:
                        Assert.True(false);
                        break;
                }
            }
            catch (XmlSchemaValidationException e)
            {
                switch (typeToValidate)
                {
                    case "other":
                        _exVerifier.IsExceptionOk(e, "Sch_SchemaAttributeNameMismatch", new string[] { "PartialAttribute2", "PartialAttribute" });
                        break;
                    case "element":
                        _exVerifier.IsExceptionOk(e, "Sch_ValidateElementInvalidCall");
                        break;
                    case "type":
                        _exVerifier.IsExceptionOk(e, "Sch_ValidateElementInvalidCall");
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
                return;
            }

            Assert.True(false);
        }

        [Theory]
        [InlineData("annotation")]
        [InlineData("group")]
        [InlineData("any")]
        public void Pass_XmlSchemaAnnotation_XmlSchemaGroup_XmlSchemaAny_Invalid(String TypeToPass)
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());
            XmlSchemaObject obj = new XmlSchemaAnnotation();

            switch (TypeToPass)
            {
                case "annotation":
                    obj = new XmlSchemaAnnotation();
                    break;

                case "group":
                    obj = new XmlSchemaGroup();
                    break;

                case "any":
                    obj = new XmlSchemaAny();
                    break;

                default:
                    Assert.True(false);
                    break;
            }

            try
            {
                val.Initialize(obj);
            }
            catch (ArgumentException)
            {
                return;
            }

            Assert.True(false);
        }

        [Theory]
        [InlineData("text")]
        [InlineData("whitespace")]
        public void SetPartiaValidationAndCallValidate_Text_WhiteSpace_Valid(String typeToValidate)
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaSet schemas = new XmlSchemaSet();

            schemas.Add("", Path.Combine(TestData, XSDFILE_PARTIAL_VALIDATION));
            schemas.Compile();

            val = CreateValidator(schemas);
            val.Initialize(schemas.GlobalElements[new XmlQualifiedName("PartialElement")]);

            if (typeToValidate == "text")
                val.ValidateText(StringGetter("foo"));
            else
                val.ValidateWhitespace(StringGetter(Environment.NewLine + "\t "));

            return;
        }
    }

    // ===================== EndValidation =====================

    public class TCEndValidation : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        private ExceptionVerifier _exVerifier;

        public TCEndValidation(ITestOutputHelper output): base(output)
        {
            _output = output;
            _exVerifier = new ExceptionVerifier("System.Xml", _output);
        }

        [Fact]
        public void CallWithoutAnyValidation__AfterInitialize()
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());

            val.Initialize();
            val.EndValidation();

            return;
        }

        [Theory]
        [InlineData("valid")]
        [InlineData("missing")]
        [InlineData("ignore")]
        public void TestForRootLevelIdentityConstraints_Valid_IDREFMissingInvalid_IgnoreIdentityConstraintsIsSetInvalid(String validity)
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            string[] keys = new string[] { };
            string[] keyrefs = new string[] { };

            switch (validity)
            {
                case "valid":
                    keys = new string[] { "a1", "a2" };
                    keyrefs = new string[] { "a1", "a1", "a2" };
                    break;

                case "missing":
                    keys = new string[] { "a1", "a2" };
                    keyrefs = new string[] { "a1", "a1", "a3" };
                    break;

                case "ignore":
                    keys = new string[] { "a1", "a1" };
                    keyrefs = new string[] { "a2", "a2" };
                    break;

                default:
                    Assert.True(false);
                    break;
            }

            if (validity == "ignore")
                val = CreateValidator(XSDFILE_IDENTITY_CONSTRAINS, "", XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessSchemaLocation | XmlSchemaValidationFlags.ProcessInlineSchema);
            else
                val = CreateValidator(XSDFILE_IDENTITY_CONSTRAINS);

            val.Initialize();

            val.ValidateElement("rootIDREFs", "", info);
            val.ValidateEndOfAttributes(null);

            foreach (string str in keyrefs)
            {
                val.ValidateElement("foo", "", info);
                val.ValidateAttribute("attr", "", StringGetter(str), info);
                val.ValidateEndOfAttributes(null);
                val.ValidateEndElement(info);
            }

            val.ValidateEndElement(info);

            val.ValidateElement("rootIDs", "", info);
            val.ValidateEndOfAttributes(null);

            foreach (string str in keys)
            {
                val.ValidateElement("foo", "", info);
                val.ValidateAttribute("attr", "", StringGetter(str), info);
                val.ValidateEndOfAttributes(null);
                val.ValidateEndElement(info);
            }

            val.ValidateEndElement(info);

            if (validity == "missing")
            {
                try
                {
                    val.EndValidation();
                    Assert.True(false);
                }
                catch (XmlSchemaValidationException e)
                {
                    _exVerifier.IsExceptionOk(e, "Sch_UndeclaredId", new string[] { "a3" });
                    return;
                }
            }
            else
                val.EndValidation();

            return;
        }
    }
}
