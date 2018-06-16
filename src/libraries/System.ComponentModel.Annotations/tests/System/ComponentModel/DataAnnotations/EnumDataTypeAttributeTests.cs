// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class EnumDataTypeAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), null);
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), "");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), NonFlagsEnumType.A);
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), 10);
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), 100);
            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), FlagsEnumType.X);
            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), FlagsEnumType.X | FlagsEnumType.Y);
            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), 5);
            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), 7);
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), "A");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), "B");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), "C");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), "0");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), "10");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), "100");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), "X");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), "X, Y");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), "X, Y, Z");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), "1");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), "5");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), "7");
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), FlagsEnumType.X);
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), new object());
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), true);
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), 1.1f);
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), 123.456m);
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), '0');
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), "NoSuchValue");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(NonFlagsEnumType)), "42");

            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), 0);
            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), 8);
            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), "NoSuchValue");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), "0");
            yield return new TestCase(new EnumDataTypeAttribute(typeof(FlagsEnumType)), "8");
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(string))]
        [InlineData(typeof(NonFlagsEnumType))]
        [InlineData(typeof(FlagsEnumType))]
        public static void Ctor(Type enumType)
        {
            var attribute = new EnumDataTypeAttribute(enumType);
            Assert.Equal(DataType.Custom, attribute.DataType);
            Assert.Equal("Enumeration", attribute.CustomDataType);

            Assert.Equal(enumType, attribute.EnumType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(string))]
        [InlineData(typeof(NonFlagsEnumType?))]
        [InlineData(typeof(FlagsEnumType?))]
        public static void Validate_InvalidEnumType_ThrowsInvalidOperationException(Type enumType)
        {
            var attribute = new EnumDataTypeAttribute(enumType);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("AnyValue", new ValidationContext(new object())));
        }
        
        private enum NonFlagsEnumType
        {
            A = 0,
            B = 10,
            C = 100
        }

        [Flags]
        private enum FlagsEnumType
        {
            X = 1,
            Y = 2,
            Z = 4
        }
    }
}
