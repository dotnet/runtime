// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    // ===================== ValidateText =====================

    public class TCValidateText : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        private ExceptionVerifier _exVerifier;

        public TCValidateText(ITestOutputHelper output): base(output)
        {
            _output = output;
            _exVerifier = new ExceptionVerifier("System.Xml", _output);
        }

        [Fact]
        public void PassNull()
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());

            val.Initialize();
            try
            {
                val.ValidateText((XmlValueGetter)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void TopLevelText()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_TEXT);
            CValidationEventHolder holder = new CValidationEventHolder();

            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateText(StringGetter("foo"));
            val.EndValidation();

            Assert.True(!holder.IsCalledA);

            return;
        }

        [Theory]
        [InlineData("single")]
        [InlineData("multiple")]
        public void SanityTestForSimpleType_MultipleCallInOneContext(string param)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_TEXT);
            CValidationEventHolder holder = new CValidationEventHolder();
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateElement("PatternElement", "", info);
            val.ValidateEndOfAttributes(null);

            if (param == "single")
                val.ValidateText(StringGetter("foo123bar"));
            else
            {
                val.ValidateText(StringGetter("foo"));
                val.ValidateText(StringGetter("123"));
                val.ValidateText(StringGetter("bar"));
            }

            val.ValidateEndElement(info);
            val.EndValidation();

            Assert.True(!holder.IsCalledA);
            Assert.Equal(info.Validity, XmlSchemaValidity.Valid);
            Assert.Equal(info.ContentType, XmlSchemaContentType.TextOnly);

            return;
        }

        [Fact]
        public void MixedContent()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_TEXT);
            CValidationEventHolder holder = new CValidationEventHolder();
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateElement("MixedElement", "", info);
            val.ValidateEndOfAttributes(null);

            val.ValidateText(StringGetter("some text"));

            val.ValidateElement("child", "", info);
            val.ValidateEndOfAttributes(null);
            val.ValidateEndElement(info);

            val.ValidateText(StringGetter("some other text"));

            val.ValidateEndElement(info);
            val.EndValidation();

            Assert.True(!holder.IsCalledA);
            Assert.Equal(info.Validity, XmlSchemaValidity.Valid);
            Assert.Equal(info.ContentType, XmlSchemaContentType.Mixed);

            return;
        }

        [Fact]
        public void ElementOnlyContent()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_TEXT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("ElementOnlyElement", "", info);
            val.ValidateEndOfAttributes(null);

            try
            {
                val.ValidateText(StringGetter("some text"));
            }
            catch (XmlSchemaValidationException e)
            {
                _exVerifier.IsExceptionOk(e, new object[] { "Sch_InvalidTextInElementExpecting",
                new object[] { "Sch_ElementName", "ElementOnlyElement" },
                    new object[] { "Sch_ElementName", "child" } });
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void EmptyContent()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_TEXT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("EmptyElement", "", info);
            val.ValidateEndOfAttributes(null);

            try
            {
                val.ValidateText(StringGetter("some text"));
            }
            catch (XmlSchemaValidationException e)
            {
                _exVerifier.IsExceptionOk(e, "Sch_InvalidTextInEmpty");
                return;
            }

            Assert.True(false);
        }
    }

    // ===================== ValidateWhitespace =====================

    public class TCValidateWhitespace : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        private ExceptionVerifier _exVerifier;

        public TCValidateWhitespace(ITestOutputHelper output): base(output)
        {
            _output = output;
            _exVerifier = new ExceptionVerifier("System.Xml", _output);
        }

        [Fact]
        public void PassNull()
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());

            val.Initialize();
            try
            {
                val.ValidateWhitespace((XmlValueGetter)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void TopLevelWhitespace()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_TEXT);
            CValidationEventHolder holder = new CValidationEventHolder();

            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateWhitespace(StringGetter(" \t" + Environment.NewLine));
            val.EndValidation();

            Assert.True(!holder.IsCalledA);

            return;
        }

        [Fact]
        public void WhitespaceInsideElement_Single()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_TEXT);
            CValidationEventHolder holder = new CValidationEventHolder();
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateElement("ElementOnlyElement", "", info);
            val.ValidateEndOfAttributes(null);

            val.ValidateWhitespace(StringGetter(" \t"+ Environment.NewLine));

            val.ValidateElement("child", "", info);
            val.ValidateEndOfAttributes(null);
            val.ValidateEndElement(info);

            val.ValidateWhitespace(StringGetter(" \t" + Environment.NewLine));

            val.ValidateEndElement(info);
            val.EndValidation();

            Assert.True(!holder.IsCalledA);
            Assert.Equal(info.Validity, XmlSchemaValidity.Valid);
            Assert.Equal(info.ContentType, XmlSchemaContentType.ElementOnly);

            return;
        }

        [Fact]
        public void WhitespaceInEmptyContent__Invalid()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_TEXT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("EmptyElement", "", info);
            val.ValidateEndOfAttributes(null);

            try
            {
                val.ValidateWhitespace(StringGetter(" " + Environment.NewLine + "\t"));
            }
            catch (XmlSchemaValidationException e)
            {
                _exVerifier.IsExceptionOk(e, "Sch_InvalidWhitespaceInEmpty");
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void PassNonWhitespaceContent()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_TEXT);

            val.Initialize();
            val.ValidateElement("ElementOnlyElement", "", null);
            val.ValidateEndOfAttributes(null);

            try
            {
                val.ValidateWhitespace(StringGetter("this is not whitespace"));
            }
            catch (Exception) // Replace with concrete exception type
            {
                // Verify exception ????
                Assert.True(false);
            }

            return;
        }
    }

    // ===================== ValidateEndElement =====================

    public class TCValidateEndElement : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        private ExceptionVerifier _exVerifier;

        public TCValidateEndElement(ITestOutputHelper output): base(output)
        {
            _output = output;
            _exVerifier = new ExceptionVerifier("System.Xml", _output);
        }

        [Fact]
        public void PassNull()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("BasicElement", "", info);
            val.ValidateEndOfAttributes(null);

            val.ValidateEndElement(null);

            return;
        }

        // BUG 305258
        [Fact]
        public void SanityTestForComplexTypes()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("ComplexElement", "", info);
            val.ValidateEndOfAttributes(null);

            foreach (string name in new string[] { "e1", "e2", "e2", "e3" })
            {
                val.ValidateElement(name, "", info);
                val.ValidateEndOfAttributes(null);
                val.ValidateEndElement(info);

                Assert.Equal(info.Validity, XmlSchemaValidity.Valid);
                Assert.Equal(info.ContentType, XmlSchemaContentType.TextOnly);
            }

            val.ValidateEndElement(info);

            Assert.Equal(info.Validity, XmlSchemaValidity.Valid);
            Assert.Equal(info.ContentType, XmlSchemaContentType.ElementOnly);

            val.EndValidation();

            return;
        }

        [Fact]
        public void IncompleteContet__Valid()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("ComplexElement", "", info);
            val.ValidateEndOfAttributes(null);

            foreach (string name in new string[] { "e1", "e2", "e2" })
            {
                val.ValidateElement(name, "", info);
                val.ValidateEndOfAttributes(null);
                val.ValidateEndElement(info);
            }

            val.ValidateEndElement(info);

            Assert.Equal(info.Validity, XmlSchemaValidity.Valid);
            Assert.Equal(info.ContentType, XmlSchemaContentType.ElementOnly);

            val.EndValidation();

            return;
        }

        [Fact]
        public void IncompleteContent__Invalid()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();
            CValidationEventHolder holder = new CValidationEventHolder();

            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateElement("ComplexElement", "", info);
            val.ValidateEndOfAttributes(null);

            foreach (string name in new string[] { "e1", "e2" })
            {
                val.ValidateElement(name, "", info);
                val.ValidateEndOfAttributes(null);
                val.ValidateEndElement(info);
            }

            val.ValidateEndElement(info);

            Assert.True(holder.IsCalledA);
            Assert.Equal(holder.lastSeverity, XmlSeverityType.Error);
            Assert.Equal(info.Validity, XmlSchemaValidity.Invalid);

            return;
        }

        [Fact]
        public void TextNodeWithoutValidateTextCall()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("BasicElement", "", info);
            val.ValidateEndOfAttributes(null);
            val.ValidateEndElement(info);
            val.EndValidation();

            Assert.Equal(info.Validity, XmlSchemaValidity.Valid);
            Assert.Equal(info.ContentType, XmlSchemaContentType.TextOnly);

            return;
        }

        // 2nd overload

        [Fact]
        public void Typed_NullXmlSchemaInfo()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("NumberElement", "", info);
            val.ValidateEndOfAttributes(null);

            val.ValidateEndElement(null, "123");

            return;
        }

        [Fact]
        public void Typed_NullTypedValue()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("NumberElement", "", info);
            val.ValidateEndOfAttributes(null);

            try
            {
                val.ValidateEndElement(info, null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            Assert.True(false);
        }

        [Fact]
        public void CallValidateTextThenValidateEndElementWithTypedValue()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("NumberElement", "", info);
            val.ValidateEndOfAttributes(null);

            val.ValidateText(StringGetter("1"));

            try
            {
                val.ValidateEndElement(info, "23");
            }
            catch (InvalidOperationException e)
            {
                _exVerifier.IsExceptionOk(e, "Sch_InvalidEndElementCall");
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void CheckSchemaInfoAfterCallingValidateEndElementWithTypedValue()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("NumberElement", "", info);
            val.ValidateEndOfAttributes(null);
            val.ValidateEndElement(info, "123");
            val.EndValidation();

            Assert.Equal(info.Validity, XmlSchemaValidity.Valid);
            Assert.Equal(info.ContentType, XmlSchemaContentType.TextOnly);
            Assert.Equal(info.IsDefault, false);
            Assert.Equal(info.IsNil, false);
            Assert.Equal(info.SchemaType.TypeCode, XmlTypeCode.Int);

            return;
        }

        //bug #305258
        [Fact]
        public void SanityTestForEmptyTypes()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("EmptyElement", "", info);
            val.ValidateEndOfAttributes(null);

            val.ValidateEndElement(info);

            Assert.Equal(info.Validity, XmlSchemaValidity.Valid);
            Assert.Equal(info.ContentType, XmlSchemaContentType.Empty);

            val.EndValidation();

            return;
        }

        [Theory]
        [InlineData("valid")]
        [InlineData("duplicate")]
        [InlineData("missing")]
        [InlineData("ignore")]
        public void TestForIdentityConstraints_Valid_InvalidDuplicateKey_InvalidKeyRefMissing_InvalidIdentitiConstraintIsSet(string constrType)
        {
            XmlSchemaValidator val;
            XmlSchemaInfo info = new XmlSchemaInfo();
            string[] keys = new string[] { };
            string[] keyrefs = new string[] { };
            bool secondPass;

            switch (constrType)
            {
                case "valid":
                    keys = new string[] { "1", "2" };
                    keyrefs = new string[] { "1", "1", "2" };
                    break;

                case "duplicate":
                    keys = new string[] { "1", "1" };
                    keyrefs = new string[] { "1", "1", "2" };
                    break;

                case "missing":
                    keys = new string[] { "1", "2" };
                    keyrefs = new string[] { "1", "1", "3" };
                    break;

                case "ignore":
                    keys = new string[] { "1", "1" };
                    keyrefs = new string[] { "2", "2" };
                    break;

                default:
                    Assert.True(false);
                    break;
            }

            if (constrType == "ignore")
                val = CreateValidator(XSDFILE_IDENTITY_CONSTRAINS, "", XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessSchemaLocation | XmlSchemaValidationFlags.ProcessInlineSchema);
            else
                val = CreateValidator(XSDFILE_IDENTITY_CONSTRAINS);

            val.Initialize();
            val.ValidateElement("root", "", info);
            val.ValidateEndOfAttributes(null);

            val.ValidateElement("desc", "", info);
            val.ValidateEndOfAttributes(null);

            foreach (string str in keyrefs)
            {
                val.ValidateElement("elemDesc", "", info);
                val.ValidateAttribute("number", "", StringGetter(str), info);
                val.ValidateEndOfAttributes(null);
                val.ValidateText(StringGetter("foo"));
                val.ValidateEndElement(info);
            }

            val.ValidateEndElement(info);

            secondPass = false;
            foreach (string str in keys)
            {
                val.ValidateElement("elem", "", info);
                val.ValidateAttribute("number", "", StringGetter(str), info);
                val.ValidateEndOfAttributes(null);

                val.ValidateElement("bar", "", info);
                val.ValidateEndOfAttributes(null);
                val.ValidateEndElement(info);

                if (constrType == "duplicate" && secondPass)
                {
                    try
                    {
                        val.ValidateEndElement(info);
                        Assert.True(false);
                    }
                    catch (XmlSchemaValidationException e)
                    {
                        _exVerifier.IsExceptionOk(e, "Sch_DuplicateKey", new string[] { "1", "numberKey" });
                        return;
                    }
                }
                else
                    val.ValidateEndElement(info);
                secondPass = true;
            }

            if (constrType == "missing")
            {
                try
                {
                    val.ValidateEndElement(info);
                    Assert.True(false);
                }
                catch (XmlSchemaValidationException e)
                {
                    _exVerifier.IsExceptionOk(e, "Sch_UnresolvedKeyref", new string[] { "3", "numberKey" });
                    return;
                }
            }
            else
            {
                val.ValidateEndElement(info);
                val.EndValidation();
            }

            return;
        }

        //Bug #305376
        [Fact]
        public void AllXmlSchemaInfoArgsCanBeNull()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);

            val.Initialize();

            val.ValidateElement("WithAttributesElement", "", null);
            val.ValidateAttribute("attr1", "", StringGetter("foo"), null);
            val.ValidateAttribute("attr2", "", StringGetter("foo"), null);
            val.ValidateEndOfAttributes(null);
            val.ValidateEndElement(null);

            val.ValidateElement("foo", "", null, "EmptyType", null, null, null);
            val.SkipToEndElement(null);

            val.ValidateElement("NumberElement", "", null);
            val.ValidateEndOfAttributes(null);
            val.ValidateEndElement(null, "123");

            return;
        }

        [Theory]
        [InlineData("first")]
        [InlineData("second")] //(BUG #307549)
        public void TestXmlSchemaInfoValuesAfterUnionValidation_Without_With_ValidationEndElementOverload(string overload)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();

            val.ValidateElement("UnionElement", "", null);
            val.ValidateEndOfAttributes(null);

            if (overload == "first")
            {
                val.ValidateText(StringGetter("false"));
                val.ValidateEndElement(info);
            }
            else
                val.ValidateEndElement(info, "false");

            Assert.Equal(info.MemberType.TypeCode, XmlTypeCode.Boolean);

            return;
        }

        //BUG #308578
        [Fact]
        public void CallValidateEndElementWithTypedValueForComplexContent()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("ComplexElement", "", info);
            val.ValidateEndOfAttributes(null);

            foreach (string name in new string[] { "e1", "e2", "e2" })
            {
                val.ValidateElement(name, "", info);
                val.ValidateEndOfAttributes(null);
                val.ValidateEndElement(info);
            }

            try
            {
                val.ValidateEndElement(info, "23");
            }
            catch (InvalidOperationException e)
            {
                _exVerifier.IsExceptionOk(e, "Sch_InvalidEndElementCallTyped");
                return;
            }

            Assert.True(false);
        }
    }

    // ===================== SkipToEndElement =====================

    public class TCSkipToEndElement : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        private ExceptionVerifier _exVerifier;

        public TCSkipToEndElement(ITestOutputHelper output): base(output)
        {
            _output = output;
            _exVerifier = new ExceptionVerifier("System.Xml", _output);
        }

        [Fact]
        public void PassNull()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("BasicElement", "", info);
            val.ValidateEndOfAttributes(null);

            val.SkipToEndElement(null);

            return;
        }

        //bug #306869
        [Theory]
        [InlineData("valid")]
        [InlineData("invalid")]
        public void SkipAfterValidating_ValidContent_IncompleteContent(string validity)
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();
            bool valid = (validity == "valid");

            val.Initialize();
            val.ValidateElement("ComplexElement", "", info);
            val.ValidateEndOfAttributes(null);

            string[] tmp;
            if (valid) tmp = new string[] { "e1", "e2", "e2" };
            else tmp = new string[] { "e1", "e2" };

            foreach (string name in tmp)
            {
                val.ValidateElement(name, "", info);
                val.ValidateEndOfAttributes(null);
                val.ValidateEndElement(info);
            }

            val.SkipToEndElement(info);
            val.EndValidation();

            Assert.Equal(info.Validity, XmlSchemaValidity.NotKnown);

            return;
        }

        //bug #306869
        [Fact]
        public void ValidateTextAndSkip()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("BasicElement", "", info);
            val.ValidateEndOfAttributes(null);
            val.ValidateText(StringGetter("foo"));
            val.SkipToEndElement(info);

            Assert.Equal(info.Validity, XmlSchemaValidity.NotKnown);

            return;
        }

        //bug #306869
        [Fact]
        public void ValidateAttributesAndSkip()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("WithAttributesElement", "", info);
            val.ValidateAttribute("attr1", "", StringGetter("foo"), info);
            val.SkipToEndElement(info);

            Assert.Equal(info.Validity, XmlSchemaValidity.NotKnown);

            return;
        }

        [Fact]
        public void CheckThatSkipToEndElementJumpsIntoRightContext()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_END_ELEMENT);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.Initialize();
            val.ValidateElement("NestedElement", "", info);
            val.ValidateEndOfAttributes(null);

            val.ValidateElement("foo", "", info);
            val.ValidateEndOfAttributes(null);

            val.ValidateElement("bar", "", info);
            val.ValidateEndOfAttributes(null);

            val.SkipToEndElement(info);
            val.SkipToEndElement(info);
            val.SkipToEndElement(info);

            try
            {
                val.SkipToEndElement(info);
            }
            catch (InvalidOperationException e)
            {
                _exVerifier.IsExceptionOk(e, "Sch_InvalidEndElementMultiple", new string[] { "SkipToEndElement" });
                return;
            }

            Assert.True(false);
        }
    }
}