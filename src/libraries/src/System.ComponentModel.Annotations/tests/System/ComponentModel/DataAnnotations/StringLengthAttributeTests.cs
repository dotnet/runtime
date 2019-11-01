// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
