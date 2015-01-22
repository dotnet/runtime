// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class EmailAddressAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void EmailAddressAttribute_creation_DataType_and_CustomDataType()
        {
            var attribute = new EmailAddressAttribute();
            Assert.Equal(DataType.EmailAddress, attribute.DataType);
            Assert.Null(attribute.CustomDataType);
        }

        [Fact]
        public static void Validate_successful_for_null_address()
        {
            var attribute = new EmailAddressAttribute();

            AssertEx.DoesNotThrow(() => attribute.Validate(null, s_testValidationContext)); // Null is valid
        }

        [Fact]
        public static void Validate_successful_for_valid_local_part()
        {
            var attribute = new EmailAddressAttribute();

            AssertEx.DoesNotThrow(() => attribute.Validate("someName@someDomain.com", s_testValidationContext)); // Simple valid value
            AssertEx.DoesNotThrow(() => attribute.Validate("1234@someDomain.com", s_testValidationContext)); // numbers are valid
            AssertEx.DoesNotThrow(() => attribute.Validate("firstName.lastName@someDomain.com", s_testValidationContext)); // With dot in name
            AssertEx.DoesNotThrow(() => attribute.Validate("\u00A0@someDomain.com", s_testValidationContext)); // With valid \u character
            AssertEx.DoesNotThrow(() => attribute.Validate("!#$%&'*+-/=?^_`|~@someDomain.com", s_testValidationContext)); // With valid (but unusual) characters
            AssertEx.DoesNotThrow(() => attribute.Validate("\"firstName.lastName\"@someDomain.com", s_testValidationContext)); // quotes around whole local part
        }

        [Fact]
        public static void Validate_successful_for_valid_domain_part()
        {
            var attribute = new EmailAddressAttribute();

            AssertEx.DoesNotThrow(() => attribute.Validate("someName@someDomain.com", s_testValidationContext)); // Simple valid value
            AssertEx.DoesNotThrow(() => attribute.Validate("someName@some~domain.com", s_testValidationContext)); // With tilde
            AssertEx.DoesNotThrow(() => attribute.Validate("someName@some_domain.com", s_testValidationContext)); // With underscore
            AssertEx.DoesNotThrow(() => attribute.Validate("someName@1234.com", s_testValidationContext)); // numbers are valid
            AssertEx.DoesNotThrow(() => attribute.Validate("someName@someDomain\uFFEF.com", s_testValidationContext)); // With valid \u character
        }

        [Fact]
        public static void Validate_throws_for_invalid_local_part()
        {
            var attribute = new EmailAddressAttribute();

            Assert.Throws<ValidationException>(() => attribute.Validate("@someDomain.com", s_testValidationContext)); // no local part
            Assert.Throws<ValidationException>(() => attribute.Validate("\0@someDomain.com", s_testValidationContext)); // illegal character
            Assert.Throws<ValidationException>(() => attribute.Validate(".someName@someDomain.com", s_testValidationContext)); // initial dot not allowed
            Assert.Throws<ValidationException>(() => attribute.Validate("someName.@someDomain.com", s_testValidationContext)); // final dot not allowed
            Assert.Throws<ValidationException>(() => attribute.Validate("firstName..lastName@someDomain.com", s_testValidationContext)); // two adjacent dots not allowed
            Assert.Throws<ValidationException>(() => attribute.Validate("firstName(comment)lastName@someDomain.com", s_testValidationContext)); // parens not allowed
            Assert.Throws<ValidationException>(() => attribute.Validate("firstName\"middleName\"lastName@someDomain.com", s_testValidationContext)); // quotes in middle not allowed
        }

        [Fact]
        public static void Validate_throws_for_invalid_domain_name()
        {
            var attribute = new EmailAddressAttribute();

            Assert.Throws<ValidationException>(() => attribute.Validate("someName", s_testValidationContext)); // no domain
            Assert.Throws<ValidationException>(() => attribute.Validate("someName@", s_testValidationContext)); // no domain
            Assert.Throws<ValidationException>(() => attribute.Validate("someName@someDomain", s_testValidationContext)); // Domain must have at least 1 dot
            Assert.Throws<ValidationException>(() => attribute.Validate("someName@a@b.com", s_testValidationContext)); // multiple @'s
            Assert.Throws<ValidationException>(() => attribute.Validate("someName@\0.com", s_testValidationContext)); // illegal character
            Assert.Throws<ValidationException>(() => attribute.Validate("someName@someDomain..com", s_testValidationContext)); // two adjacent dots not allowed
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessage_is_null()
        {
            var attribute = new EmailAddressAttribute();
            attribute.ErrorMessage = null; // note: this overrides the default value
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("InvalidEmailAddress", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessage_and_ErrorMessageResourceName_are_set()
        {
            var attribute = new EmailAddressAttribute();
            attribute.ErrorMessage = "SomeErrorMessage";
            attribute.ErrorMessageResourceName = "SomeErrorMessageResourceName";
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("InvalidEmailAddress", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessageResourceName_set_but_ErrorMessageResourceType_not_set()
        {
            var attribute = new EmailAddressAttribute();
            attribute.ErrorMessageResourceName = "SomeErrorMessageResourceName";
            attribute.ErrorMessageResourceType = null;
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("InvalidEmailAddress", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessageResourceType_set_but_ErrorMessageResourceName_not_set()
        {
            var attribute = new EmailAddressAttribute();
            attribute.ErrorMessageResourceName = null;
            attribute.ErrorMessageResourceType = typeof(ErrorMessageResources);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("InvalidEmailAddress", s_testValidationContext));
        }

        [Fact]
        public static void GetValidationResult_returns_ErrorMessage_if_ErrorMessage_overrides_default()
        {
            var attribute = new EmailAddressAttribute();
            attribute.ErrorMessage = "SomeErrorMessage";
            var toBeTested = new EmailClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = "EmailPropertyToBeTested";
            var validationResult = attribute.GetValidationResult(toBeTested, validationContext);
            Assert.Equal("SomeErrorMessage", validationResult.ErrorMessage);
        }


        [Fact]
        public static void GetValidationResult_returns_DefaultErrorMessage_if_ErrorMessage_is_not_set()
        {
            var attribute = new EmailAddressAttribute();
            var toBeTested = new EmailClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = "EmailPropertyToBeTested";
            AssertEx.DoesNotThrow(() => attribute.GetValidationResult(toBeTested, validationContext));
        }

        [Fact]
        public static void GetValidationResult_returns_ErrorMessage_from_resource_if_ErrorMessageResourceName_and_ErrorMessageResourceType_both_set()
        {
            var attribute = new EmailAddressAttribute();
            attribute.ErrorMessageResourceName = "InternalErrorMessageTestProperty";
            attribute.ErrorMessageResourceType = typeof(ErrorMessageResources);
            var toBeTested = new EmailClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = "EmailPropertyToBeTested";
            var validationResult = attribute.GetValidationResult(toBeTested, validationContext);
            Assert.Equal(
                "Error Message from ErrorMessageResources.InternalErrorMessageTestProperty",
                validationResult.ErrorMessage);
        }
    }

    public class EmailClassToBeTested
    {
        public string EmailPropertyToBeTested
        {
            get { return "InvalidEmailAddress"; }
        }
    }
}
