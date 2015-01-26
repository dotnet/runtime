// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class PhoneAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void PhoneAttributeTests_creation_DataType_and_CustomDataType()
        {
            var attribute = new PhoneAttribute();
            Assert.Equal(DataType.PhoneNumber, attribute.DataType);
            Assert.Null(attribute.CustomDataType);
        }

        [Fact]
        public static void Validate_successful_for_null_value()
        {
            var attribute = new PhoneAttribute();
            AssertEx.DoesNotThrow(() => attribute.Validate(null, s_testValidationContext)); // Null is valid
        }

        [Fact]
        public static void Validate_successful_for_valid_phone_numbers()
        {
            var attribute = new PhoneAttribute();
            AssertEx.DoesNotThrow(() => attribute.Validate("425-555-1212", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("+1 425-555-1212", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("(425)555-1212", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("(425) 555-1212", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("+44 (3456)987654", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("+777.456.789.123", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("425-555-1212 x123", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("425-555-1212 x 123", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("425-555-1212 ext123", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("425-555-1212 ext 123", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_for_invalid_phone_numbers()
        {
            var attribute = new PhoneAttribute();
            Assert.Throws<ValidationException>(() => attribute.Validate(new object(), s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate(string.Empty, s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("abcdefghij", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("425+555+1212", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("425-555-1212 ext 123 ext 456", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessage_is_null()
        {
            var attribute = new PhoneAttribute();
            attribute.ErrorMessage = null; // note: this overrides the default value
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("abcdefghij", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessage_and_ErrorMessageResourceName_are_set()
        {
            var attribute = new PhoneAttribute();
            attribute.ErrorMessage = "SomeErrorMessage";
            attribute.ErrorMessageResourceName = "SomeErrorMessageResourceName";
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("abcdefghij", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessageResourceName_set_but_ErrorMessageResourceType_not_set()
        {
            var attribute = new PhoneAttribute();
            attribute.ErrorMessageResourceName = "SomeErrorMessageResourceName";
            attribute.ErrorMessageResourceType = null;
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("abcdefghij", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessageResourceType_set_but_ErrorMessageResourceName_not_set()
        {
            var attribute = new PhoneAttribute();
            attribute.ErrorMessageResourceName = null;
            attribute.ErrorMessageResourceType = typeof(ErrorMessageResources);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("abcdefghij", s_testValidationContext));
        }

        [Fact]
        public static void GetValidationResult_returns_ErrorMessage_if_ErrorMessage_overrides_default()
        {
            var attribute = new PhoneAttribute();
            attribute.ErrorMessage = "SomeErrorMessage";
            var toBeTested = new PhoneClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = "PhonePropertyToBeTested";
            var validationResult = attribute.GetValidationResult(toBeTested, validationContext);
            Assert.Equal("SomeErrorMessage", validationResult.ErrorMessage);
        }


        [Fact]
        public static void GetValidationResult_returns_DefaultErrorMessage_if_ErrorMessage_is_not_set()
        {
            var attribute = new PhoneAttribute();
            var toBeTested = new PhoneClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = "PhonePropertyToBeTested";
            AssertEx.DoesNotThrow(() => attribute.GetValidationResult(toBeTested, validationContext));
        }

        [Fact]
        public static void GetValidationResult_returns_ErrorMessage_from_resource_if_ErrorMessageResourceName_and_ErrorMessageResourceType_both_set()
        {
            var attribute = new PhoneAttribute();
            attribute.ErrorMessageResourceName = "InternalErrorMessageTestProperty";
            attribute.ErrorMessageResourceType = typeof(ErrorMessageResources);
            var toBeTested = new PhoneClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = "PhonePropertyToBeTested";
            var validationResult = attribute.GetValidationResult(toBeTested, validationContext);
            Assert.Equal(
                "Error Message from ErrorMessageResources.InternalErrorMessageTestProperty",
                validationResult.ErrorMessage);
        }
    }

    public class PhoneClassToBeTested
    {
        public string PhonePropertyToBeTested
        {
            get { return "abcdefghij"; }
        }
    }
}
