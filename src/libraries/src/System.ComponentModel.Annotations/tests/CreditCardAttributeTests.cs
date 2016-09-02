// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class CreditCardAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void DataType_CustomDataType()
        {
            var attribute = new CreditCardAttribute();
            Assert.Equal(DataType.CreditCard, attribute.DataType);
            Assert.Null(attribute.CustomDataType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("0000000000000000")]
        [InlineData("1234567890123452")]
        [InlineData("  1 2 3 4 5 6 7 8 9 0  1 2 34 5 2    ")]
        [InlineData("--1-2-3-4-5-6-7-8-9-0--1-2-34-5-2----")]
        [InlineData(" - 1- -  2 3 --4 5 6 7 -8- -9- -0 - -1 -2 -3-4- --5-- 2    ")]
        [InlineData("1234-5678-9012-3452")]
        [InlineData("1234 5678 9012 3452")]
        public static void Validate_ValidValue_DoesNotThrow(string value)
        {
            var attribute = new CreditCardAttribute();
            attribute.Validate(value, s_testValidationContext);
        }

        [Theory]
        [InlineData("0000000000000001")]
        [InlineData(0)]
        [InlineData("000%000000000001")]
        [InlineData("1234567890123452a")]
        [InlineData("1234567890123452\0")]
        public static void Validate_InvalidValue_ThrowsValidationException(object value)
        {
            var attribute = new CreditCardAttribute();
            Assert.Throws<ValidationException>(() => attribute.Validate(value, s_testValidationContext));
        }

        [Fact]
        public static void Validate_ErrorMessageNotSet_ThrowsInvalidOperationException()
        {
            var attribute = new CreditCardAttribute() { ErrorMessage = null };
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("0000000000000001", s_testValidationContext));
        }

        [Fact]
        public static void Validate_ErrorMessageSet_ErrorMessageResourceNameSet_ThrowsInvalidOperationException()
        {
            var attribute = new CreditCardAttribute() { ErrorMessage = "Some", ErrorMessageResourceName = "Some" };
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("0000000000000001", s_testValidationContext));
        }

        [Fact]
        public static void Validate_ErrorMessageResourceNameSet_ErrorMessageResourceTypeNotSet_ThrowsInvalidOperationException()
        {
            var attribute = new CreditCardAttribute() { ErrorMessageResourceName = "Some", ErrorMessageResourceType = null };
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("0000000000000001", s_testValidationContext));
        }

        [Fact]
        public static void Validate_ErrorMessageResourceNameNotSet_ErrorMessageResourceTypeSet_ThrowsInvalidOperationException()
        {
            var attribute = new CreditCardAttribute() { ErrorMessageResourceName = null, ErrorMessageResourceType = typeof(ErrorMessageResources) };
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("0000000000000001", s_testValidationContext));
        }

        [Fact]
        public static void GetValidationResult_ErrorMessageSet_ReturnsOverridenValue()
        {
            var attribute = new CreditCardAttribute();
            attribute.ErrorMessage = "SomeErrorMessage";
            var toBeTested = new CreditCardClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = nameof(CreditCardClassToBeTested.CreditCardPropertyToBeTested);

            var validationResult = attribute.GetValidationResult(toBeTested, validationContext);
            Assert.Equal("SomeErrorMessage", validationResult.ErrorMessage);
        }

        [Fact]
        public static void GetValidationResult_ErrorMessageNotSet_ReturnsDefaultValue()
        {
            var attribute = new CreditCardAttribute();
            var toBeTested = new CreditCardClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = nameof(CreditCardClassToBeTested.CreditCardPropertyToBeTested);
            attribute.GetValidationResult(toBeTested, validationContext);
        }

        [Fact]
        public static void GetValidationResult_ErrorMessageSetFromResource_ReturnsExpectedValue()
        {
            var attribute = new CreditCardAttribute();
            attribute.ErrorMessageResourceName = "InternalErrorMessageTestProperty";
            attribute.ErrorMessageResourceType = typeof(ErrorMessageResources);
            var toBeTested = new CreditCardClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = nameof(CreditCardClassToBeTested.CreditCardPropertyToBeTested);

            var validationResult = attribute.GetValidationResult(toBeTested, validationContext);
            Assert.Equal("Error Message from ErrorMessageResources.InternalErrorMessageTestProperty", validationResult.ErrorMessage);
        }
    }

    public class CreditCardClassToBeTested
    {
        public string CreditCardPropertyToBeTested => "0000000000000001";
    }
}
