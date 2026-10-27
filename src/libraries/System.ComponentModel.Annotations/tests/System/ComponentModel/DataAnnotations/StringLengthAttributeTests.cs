// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class StringLengthAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            yield return new TestCase(new StringLengthAttribute(12), null);
            yield return new TestCase(new StringLengthAttribute(12), string.Empty);
            yield return new TestCase(new StringLengthAttribute(12), "Valid string");
            yield return new TestCase(new StringLengthAttribute(12) { MinimumLength = 5 }, "Valid");
            yield return new TestCase(new StringLengthAttribute(12) { MinimumLength = 5 }, "Valid string");
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new StringLengthAttribute(12), "Invalid string");
            yield return new TestCase(new StringLengthAttribute(12) {MinimumLength = 8 }, "Invalid");
        }

        [Theory]
        [InlineData(42)]
        [InlineData(-1)]
        public static void Ctor_Int(int maximumLength)
        {
            var attribute = new StringLengthAttribute(maximumLength);
            Assert.Equal(maximumLength, attribute.MaximumLength);
            Assert.Equal(0, attribute.MinimumLength);
        }

        [Theory]
        [InlineData(29)]
        public static void MinimumLength_GetSet_RetunsExpected(int newValue)
        {
            var attribute = new StringLengthAttribute(42);
            attribute.MinimumLength = newValue;
            Assert.Equal(newValue, attribute.MinimumLength);
        }

        [Fact]
        public static void FormatMessage_UsesSuppliedFormatAndLengths()
        {
            const string ExternalFormat = "external {0}:{1}:{2}";
            const string ErrorMessageFormat = "internal {0}:{1}:{2}";
            var attribute = new StringLengthAttribute(20)
            {
                ErrorMessage = ErrorMessageFormat,
                MinimumLength = 10
            };

            Assert.Equal("external name:20:10", attribute.FormatMessage(ExternalFormat, "name"));
            Assert.Equal("internal name:20:10", attribute.FormatErrorMessage("name"));
        }

        [Theory]
        [InlineData(0, "The field name must be a string with a maximum length of 20.")]
        [InlineData(10, "The field name must be a string with a minimum length of 10 and a maximum length of 20.")]
        public static void FormatErrorMessage_DefaultTemplateUsesMinimumWhenSpecified(int minimumLength, string expected)
        {
            var attribute = new StringLengthAttribute(20) { MinimumLength = minimumLength };

            Assert.Equal(expected, attribute.FormatErrorMessage("name"));
        }

        [Fact]
        public static void Validate_NegativeMaximumLength_ThrowsInvalidOperationException()
        {
            var attribute = new StringLengthAttribute(-1);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Any", new ValidationContext(new object())));
        }

        [Fact]
        public static void Validate_MinimumLengthGreaterThanMaximumLength_ThrowsInvalidOperationException()
        {
            var attribute = new StringLengthAttribute(42) { MinimumLength = 43 };
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Any", new ValidationContext(new object())));
        }

        [Fact]
        public static void Validate_ValueNotString_ThrowsInvalidCastException()
        {
            var attribute = new StringLengthAttribute(42);
            Assert.Throws<InvalidCastException>(() => attribute.Validate(new object(), new ValidationContext(new object())));
        }
    }
}
