// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    // ===================== ValidateText =====================

    public class TCValidateText_String : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        private ExceptionVerifier _exVerifier;

        public TCValidateText_String(ITestOutputHelper output): base(output)
        {
            _output = output;
            _exVerifier = new ExceptionVerifier("System.Xml", _output);
        }

        [Fact]
        //[Variation(Desc = "Pass null", id = 1, Pri = 0)]
        public void v1()
        {
            XmlSchemaValidator val = CreateValidator(new XmlSchemaSet());

            val.Initialize();
            try
            {
                val.ValidateText((string)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            Assert.True(false);
        }

        [Fact]
        //[Variation(Desc = "Top level text", id = 2, Pri = 0)]
        public void v2()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_TEXT);
            CValidationEventHolder holder = new CValidationEventHolder();

            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateText("foo");
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
                val.ValidateText("foo123bar");
            else
            {
                val.ValidateText("foo");
                val.ValidateText("123");
                val.ValidateText("bar");
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

            val.ValidateText("some text");

            val.ValidateElement("child", "", info);
            val.ValidateEndOfAttributes(null);
            val.ValidateEndElement(info);

            val.ValidateText("some other text");

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
                val.ValidateText("some text");
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
                val.ValidateText("some text");
            }
            catch (XmlSchemaValidationException e)
            {
                _exVerifier.IsExceptionOk(e, "Sch_InvalidTextInEmpty");
                return;
            }

            Assert.True(false);
        }
    }
}