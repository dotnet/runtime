// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class CreditCardAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void CreditCardAttribute_creation_DataType_and_CustomDataType()
        {
            var attribute = new CreditCardAttribute();
            Assert.Equal(DataType.CreditCard, attribute.DataType);
            Assert.Null(attribute.CustomDataType);
        }

        [Fact]
        public static void Validate_successful_for_valid_values()
        {
            var attribute = new CreditCardAttribute();

            AssertEx.DoesNotThrow(() => attribute.Validate(null, s_testValidationContext)); // Null is valid
            AssertEx.DoesNotThrow(() => attribute.Validate("0000000000000000", s_testValidationContext)); // Simplest valid value
            AssertEx.DoesNotThrow(() => attribute.Validate("1234567890123452", s_testValidationContext)); // Good checksum
            AssertEx.DoesNotThrow(() => attribute.Validate("1234-5678-9012-3452", s_testValidationContext)); // Good checksum, with dashes
            AssertEx.DoesNotThrow(() => attribute.Validate("1234 5678 9012 3452", s_testValidationContext)); // Good checksum, with spaces
        }

        [Fact]
        public static void Validate_throws_for_invalid_values()
        {
            var attribute = new CreditCardAttribute();

            Assert.Throws<ValidationException>(() => attribute.Validate("0000000000000001", s_testValidationContext)); // Bad checksum
            Assert.Throws<ValidationException>(() => attribute.Validate(0, s_testValidationContext)); // Non-string
            Assert.Throws<ValidationException>(() => attribute.Validate("000%000000000001", s_testValidationContext)); // Non-digit
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessage_is_null()
        {
            var attribute = new CreditCardAttribute();
            attribute.ErrorMessage = null; // note: this overrides the default value
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("0000000000000001", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessage_and_ErrorMessageResourceName_are_set()
        {
            var attribute = new CreditCardAttribute();
            attribute.ErrorMessage = "SomeErrorMessage";
            attribute.ErrorMessageResourceName = "SomeErrorMessageResourceName";
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("0000000000000001", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessageResourceName_set_but_ErrorMessageResourceType_not_set()
        {
            var attribute = new CreditCardAttribute();
            attribute.ErrorMessageResourceName = "SomeErrorMessageResourceName";
            attribute.ErrorMessageResourceType = null;
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("0000000000000001", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessageResourceType_set_but_ErrorMessageResourceName_not_set()
        {
            var attribute = new CreditCardAttribute();
            attribute.ErrorMessageResourceName = null;
            attribute.ErrorMessageResourceType = typeof(ErrorMessageResources);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("0000000000000001", s_testValidationContext));
        }

        [Fact]
        public static void GetValidationResult_returns_ErrorMessage_if_ErrorMessage_overrides_default()
        {
            var attribute = new CreditCardAttribute();
            attribute.ErrorMessage = "SomeErrorMessage";
            var toBeTested = new CreditCardClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = "CreditCardPropertyToBeTested";
            var validationResult = attribute.GetValidationResult(toBeTested, validationContext);
            Assert.Equal("SomeErrorMessage", validationResult.ErrorMessage);
        }


        [Fact]
        public static void GetValidationResult_returns_DefaultErrorMessage_if_ErrorMessage_is_not_set()
        {
            var attribute = new CreditCardAttribute();
            var toBeTested = new CreditCardClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = "CreditCardPropertyToBeTested";
            AssertEx.DoesNotThrow(() => attribute.GetValidationResult(toBeTested, validationContext));
        }

        [Fact]
        public static void GetValidationResult_returns_ErrorMessage_from_resource_if_ErrorMessageResourceName_and_ErrorMessageResourceType_both_set()
        {
            var attribute = new CreditCardAttribute();
            attribute.ErrorMessageResourceName = "InternalErrorMessageTestProperty";
            attribute.ErrorMessageResourceType = typeof(ErrorMessageResources);
            var toBeTested = new CreditCardClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = "CreditCardPropertyToBeTested";
            var validationResult = attribute.GetValidationResult(toBeTested, validationContext);
            Assert.Equal(
                "Error Message from ErrorMessageResources.InternalErrorMessageTestProperty",
                validationResult.ErrorMessage);
        }
    }


    public class CreditCardClassToBeTested
    {
        public string CreditCardPropertyToBeTested
        {
            get { return "0000000000000001"; }
        }
    }
}
