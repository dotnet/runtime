// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            AssertEx.DoesNotThrow(() => attribute.Validate("425-555-1212 ext.123", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("425-555-1212 ext. 123", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_for_invalid_phone_numbers()
        {
            var attribute = new PhoneAttribute();
            Assert.Throws<ValidationException>(() => attribute.Validate(new object(), s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate(string.Empty, s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("abcdefghij", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("425-555-1212 ext 123 ext 456", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("425-555-1212 x", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("425-555-1212 ext", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("425-555-1212 ext.", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("425-555-1212 x abc", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("425-555-1212 ext def", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("425-555-1212 ext. xyz", s_testValidationContext));
        }
    }
}
