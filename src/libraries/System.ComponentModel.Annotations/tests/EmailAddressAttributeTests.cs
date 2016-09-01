// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class EmailAddressAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void DataType_CustomDataType_ReturnExpected()
        {
            var attribute = new EmailAddressAttribute();
            Assert.Equal(DataType.EmailAddress, attribute.DataType);
            Assert.Null(attribute.CustomDataType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("someName@someDomain.com")]
        [InlineData("1234@someDomain.com")]
        [InlineData("firstName.lastName@someDomain.com")]
        [InlineData("\u00A0@someDomain.com")]
        [InlineData("!#$%&'*+-/=?^_`|~@someDomain.com")]
        [InlineData("\"firstName.lastName\"@someDomain.com")]
        [InlineData("someName@someDomain.com")]
        [InlineData("someName@some~domain.com")]
        [InlineData("someName@some_domain.com")]
        [InlineData("someName@1234.com")]
        [InlineData("someName@someDomain\uFFEF.com")]
        public static void Validate_ValidValue_DoesNotThrow(string value)
        {
            var attribute = new EmailAddressAttribute();
            attribute.Validate(value, s_testValidationContext);
        }

        [Theory]
        [InlineData(0)]
        [InlineData("")]
        [InlineData(" \r \t \n")]
        [InlineData("@someDomain.com")]
        [InlineData("@someDomain@abc.com")]
        [InlineData("someName")]
        [InlineData("someName@")]
        [InlineData("someName@a@b.com")]
        public static void Validate_InvalidValue_ThrowsValidationException(object value)
        {
            var attribute = new EmailAddressAttribute();
            Assert.Throws<ValidationException>(() => attribute.Validate(value, s_testValidationContext));
        }

        [Fact]
        public static void Validate_ErrorMessageNotSet_ThrowsInvalidOperationException()
        {
            var attribute = new EmailAddressAttribute() { ErrorMessage = null };
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("InvalidEmailAddress", s_testValidationContext));
        }

        [Fact]
        public static void Validate_ErrorMessageSet_ErrorMessageResourceNameSet_ThrowsInvalidOperationException()
        {
            var attribute = new EmailAddressAttribute() { ErrorMessage = "Some", ErrorMessageResourceName = "Some" };
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("InvalidEmailAddress", s_testValidationContext));
        }

        [Fact]
        public static void Validate_ErrorMessageResourceNameSet_ErrorMessageResourceTypeNotSet_ThrowsInvalidOperationException()
        {
            var attribute = new EmailAddressAttribute() { ErrorMessageResourceName = "Some", ErrorMessageResourceType = null };
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("InvalidEmailAddress", s_testValidationContext));
        }

        [Fact]
        public static void Validate_ErrorMessageResourceNameNotSet_ErrorMessageResourceTypeSet_ThrowsInvalidOperationException()
        {
            var attribute = new EmailAddressAttribute() { ErrorMessageResourceName = null, ErrorMessageResourceType = typeof(ErrorMessageResources) };
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("InvalidEmailAddress", s_testValidationContext));
        }

        [Fact]
        public static void GetValidationResult_ErrorMessageSet_ReturnsOverridenValue()
        {
            var attribute = new EmailAddressAttribute();
            attribute.ErrorMessage = "SomeErrorMessage";
            var toBeTested = new EmailClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = nameof(EmailClassToBeTested.EmailPropertyToBeTested);
            var validationResult = attribute.GetValidationResult(toBeTested, validationContext);
            Assert.Equal("SomeErrorMessage", validationResult.ErrorMessage);
        }

        [Fact]
        public static void GetValidationResult_ErrorMessageNotSet_ReturnsDefaultValue()
        {
            var attribute = new EmailAddressAttribute();
            var toBeTested = new EmailClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = nameof(EmailClassToBeTested.EmailPropertyToBeTested);
            attribute.GetValidationResult(toBeTested, validationContext);
        }

        [Fact]
        public static void GetValidationResult_ErrorMessageSetFromResource_ReturnsExpectedValue()
        {
            var attribute = new EmailAddressAttribute();
            attribute.ErrorMessageResourceName = "InternalErrorMessageTestProperty";
            attribute.ErrorMessageResourceType = typeof(ErrorMessageResources);
            var toBeTested = new EmailClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = nameof(EmailClassToBeTested.EmailPropertyToBeTested);
            var validationResult = attribute.GetValidationResult(toBeTested, validationContext);
            Assert.Equal("Error Message from ErrorMessageResources.InternalErrorMessageTestProperty", validationResult.ErrorMessage);
        }
    }

    public class EmailClassToBeTested
    {
        public string EmailPropertyToBeTested => "InvalidEmailAddress";
    }
}
