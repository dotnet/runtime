// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Schema;
using System.Xml.Tests;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.XmlSchemaValidatorApiTests
{
    // ===================== ValidateWhitespace =====================

    public class TCValidateWhitespace_String : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        private ExceptionVerifier _exVerifier;
        public TCValidateWhitespace_String(ITestOutputHelper output): base(output)
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
                val.ValidateWhitespace((string)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            Assert.Fail();
        }

        [Fact]
        public void TopLevelWhitespace()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_TEXT);
            CValidationEventHolder holder = new CValidationEventHolder();

            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateWhitespace(" \t" + Environment.NewLine);
            val.EndValidation();

            Assert.True(!holder.IsCalledA);

            return;
        }

        [Fact]
        public void WhitespaceInsideElement()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_TEXT);
            CValidationEventHolder holder = new CValidationEventHolder();
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateElement("ElementOnlyElement", "", info);
            val.ValidateEndOfAttributes(null);

            val.ValidateWhitespace(" \t" + Environment.NewLine);

            val.ValidateElement("child", "", info);
            val.ValidateEndOfAttributes(null);
            val.ValidateEndElement(info);

            val.ValidateWhitespace(" \t" + Environment.NewLine);

            val.ValidateEndElement(info);
            val.EndValidation();

            Assert.True(!holder.IsCalledA);
            Assert.Equal(XmlSchemaValidity.Valid, info.Validity);
            Assert.Equal(XmlSchemaContentType.ElementOnly, info.ContentType);

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
                val.ValidateWhitespace(" " + Environment.NewLine + "\t");
            }
            catch (XmlSchemaValidationException e)
            {
                _exVerifier.IsExceptionOk(e, "Sch_InvalidWhitespaceInEmpty");
                return;
            }

            Assert.Fail();
        }

        [Fact]
        public void PassNonWhitespaceContent__ShouldNotWork()
        {
            XmlSchemaValidator val = CreateValidator(XSDFILE_VALIDATE_TEXT);

            val.Initialize();
            val.ValidateElement("ElementOnlyElement", "", null);
            val.ValidateEndOfAttributes(null);

            try
            {
                val.ValidateWhitespace("this is not whitespace");
            }
            catch (Exception) // Replace with concrete exception type
            {
                // Verify exception
                Assert.Fail();
            }

            return;
        }
    }
}
