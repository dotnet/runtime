// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class RangeAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            RangeAttribute intRange = new RangeAttribute(1, 3);
            yield return new TestCase(intRange, null);
            yield return new TestCase(intRange, string.Empty);
            yield return new TestCase(intRange, 1);
            yield return new TestCase(intRange, 2);
            yield return new TestCase(intRange, 3);
            yield return new TestCase(new RangeAttribute(1, 1), 1);

            RangeAttribute doubleRange = new RangeAttribute(1.0, 3.0);
            yield return new TestCase(doubleRange, null);
            yield return new TestCase(doubleRange, string.Empty);
            yield return new TestCase(doubleRange, 1.0);
            yield return new TestCase(doubleRange, 2.0);
            yield return new TestCase(doubleRange, 3.0);
            yield return new TestCase(new RangeAttribute(1.0, 1.0), 1);

            RangeAttribute stringIntRange = new RangeAttribute(typeof(int), "1", "3");
            yield return new TestCase(stringIntRange, null);
            yield return new TestCase(stringIntRange, string.Empty);
            yield return new TestCase(stringIntRange, 1);
            yield return new TestCase(stringIntRange, "1");
            yield return new TestCase(stringIntRange, 2);
            yield return new TestCase(stringIntRange, "2");
            yield return new TestCase(stringIntRange, 3);
            yield return new TestCase(stringIntRange, "3");
            
            RangeAttribute stringDoubleRange = new RangeAttribute(typeof(double), (1.0).ToString("F1"), (3.0).ToString("F1"));
            yield return new TestCase(stringDoubleRange, null);
            yield return new TestCase(stringDoubleRange, string.Empty);
            yield return new TestCase(stringDoubleRange, 1.0);
            yield return new TestCase(stringDoubleRange, (1.0).ToString("F1"));
            yield return new TestCase(stringDoubleRange, 2.0);
            yield return new TestCase(stringDoubleRange, (2.0).ToString("F1"));
            yield return new TestCase(stringDoubleRange, 3.0);
            yield return new TestCase(stringDoubleRange, (3.0).ToString("F1"));
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            RangeAttribute intRange = new RangeAttribute(1, 3);
            yield return new TestCase(intRange, 0);
            yield return new TestCase(intRange, 4);
            yield return new TestCase(intRange, "abc");
            yield return new TestCase(intRange, new object());
            // Implements IConvertible (throws NotSupportedException - is caught)
            yield return new TestCase(intRange, new IConvertibleImplementor() { IntThrow = new NotSupportedException() });

            RangeAttribute doubleRange = new RangeAttribute(1.0, 3.0);
            yield return new TestCase(doubleRange, 0.9999999);
            yield return new TestCase(doubleRange, 3.0000001);
            yield return new TestCase(doubleRange, "abc");
            yield return new TestCase(doubleRange, new object());
            // Implements IConvertible (throws NotSupportedException - is caught)
            yield return new TestCase(doubleRange, new IConvertibleImplementor() { DoubleThrow = new NotSupportedException() });

            RangeAttribute stringIntRange = new RangeAttribute(typeof(int), "1", "3");
            yield return new TestCase(stringIntRange, 0);
            yield return new TestCase(stringIntRange, "0");
            yield return new TestCase(stringIntRange, 4);
            yield return new TestCase(stringIntRange, "4");
            yield return new TestCase(stringIntRange, "abc");
            yield return new TestCase(stringIntRange, new object());
            // Implements IConvertible (throws NotSupportedException - is caught)
            yield return new TestCase(stringIntRange, new IConvertibleImplementor() { IntThrow = new NotSupportedException() });

            RangeAttribute stringDoubleRange = new RangeAttribute(typeof(double), (1.0).ToString("F1"), (3.0).ToString("F1"));
            yield return new TestCase(stringDoubleRange, 0.9999999);
            yield return new TestCase(stringDoubleRange, (0.9999999).ToString());
            yield return new TestCase(stringDoubleRange, 3.0000001);
            yield return new TestCase(stringDoubleRange, (3.0000001).ToString());
            yield return new TestCase(stringDoubleRange, "abc");
            yield return new TestCase(stringDoubleRange, new object());
            // Implements IConvertible (throws NotSupportedException - is caught)
            yield return new TestCase(stringDoubleRange, new IConvertibleImplementor() { DoubleThrow = new NotSupportedException() });
        }

        [Fact]
        public static void Ctor_Int_Int()
        {
            var attribute = new RangeAttribute(1, 3);
            Assert.Equal(1, attribute.Minimum);
            Assert.Equal(3, attribute.Maximum);
            Assert.Equal(typeof(int), attribute.OperandType);
        }

        [Fact]
        public static void Ctor_Double_Double()
        {
            var attribute = new RangeAttribute(1.0, 3.0);
            Assert.Equal(1.0, attribute.Minimum);
            Assert.Equal(3.0, attribute.Maximum);
            Assert.Equal(typeof(double), attribute.OperandType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(object))]
        public static void Ctor_Type_String_String(Type type)
        {
            var attribute = new RangeAttribute(type, "SomeMinimum", "SomeMaximum");
            Assert.Equal("SomeMinimum", attribute.Minimum);
            Assert.Equal("SomeMaximum", attribute.Maximum);
            Assert.Equal(type, attribute.OperandType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(object))]
        public static void Validate_InvalidOperandType_ThrowsInvalidOperationException(Type type)
        {
            var attribute = new RangeAttribute(type, "someMinimum", "someMaximum");
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Any", new ValidationContext(new object())));
        }

        [Fact]
        public static void Validate_MinimumGreaterThanMaximum_ThrowsInvalidOperationException()
        {
            var attribute = new RangeAttribute(3, 1);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Any", new ValidationContext(new object())));

            attribute = new RangeAttribute(3.0, 1.0);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Any", new ValidationContext(new object())));

            attribute = new RangeAttribute(typeof(int), "3", "1");
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Any", new ValidationContext(new object())));

            attribute = new RangeAttribute(typeof(double), (3.0).ToString("F1"), (1.0).ToString("F1"));
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Any", new ValidationContext(new object())));

            attribute = new RangeAttribute(typeof(string), "z", "a");
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Any", new ValidationContext(new object())));
        }

        [Theory]
        [InlineData(null, "3")]
        [InlineData("3", null)]
        public static void Validate_MinimumOrMaximumNull_ThrowsInvalidOperationException(string minimum, string maximum)
        {
            RangeAttribute attribute = new RangeAttribute(typeof(int), minimum, maximum);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Any", new ValidationContext(new object())));
        }

        [Theory]
        [InlineData(typeof(DateTime), "Cannot Convert", "2014-03-19")]
        [InlineData(typeof(DateTime), "2014-03-19", "Cannot Convert")]
        [InlineData(typeof(int), "Cannot Convert", "3")]
        [InlineData(typeof(int), "1", "Cannot Convert")]
        [InlineData(typeof(double), "Cannot Convert", "3")]
        [InlineData(typeof(double), "1", "Cannot Convert")]
        public static void Validate_MinimumOrMaximumCantBeConvertedToType_ThrowsFormatException(Type type, string minimum, string maximum)
        {
            RangeAttribute attribute = new RangeAttribute(type, minimum, maximum);
            Assert.Throws<FormatException>(() => attribute.Validate("Any", new ValidationContext(new object())));
        }

        [Theory]
        [InlineData(1, 2, "2147483648")]
        [InlineData(1, 2, "-2147483649")]
        public static void Validate_IntConversionOverflows_ThrowsOverflowException(int minimum, int maximum, object value)
        {
            RangeAttribute attribute = new RangeAttribute(minimum, maximum);
            Assert.Throws<OverflowException>(() => attribute.Validate(value, new ValidationContext(new object())));
        }

        [Theory]
        [InlineData(1.0, 2.0, "2E+308")]
        [InlineData(1.0, 2.0, "-2E+308")]
        public static void Validate_DoubleConversionOverflows_ThrowsOverflowException(double minimum, double maximum, object value)
        {
            RangeAttribute attribute = new RangeAttribute(minimum, maximum);
            Assert.Throws<OverflowException>(() => attribute.Validate(value, new ValidationContext(new object())));
        }

        [Fact]
        public static void Validate_IConvertibleThrowsCustomException_IsNotCaught()
        {
            RangeAttribute attribute = new RangeAttribute(typeof(int), "1", "1");
            Assert.Throws<ArithmeticException>(() => attribute.Validate(new IConvertibleImplementor() { IntThrow = new ArithmeticException() }, new ValidationContext(new object())));
        }
    }
}
