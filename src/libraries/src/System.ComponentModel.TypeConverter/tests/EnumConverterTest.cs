// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class EnumConverterTests : ConverterTestBase
    {
        private static EnumConverter s_someEnumConverter = new EnumConverter(typeof(SomeEnum));
        private static EnumConverter s_someFlagsEnumConverter = new EnumConverter(typeof(SomeFlagsEnum));

        [Fact]
        public static void CanConvertFrom_WithContext()
        {
            CanConvertFrom_WithContext(new object[2, 2]
                {
                    { typeof(string), true },
                    { typeof(Enum[]), true }
                },
                new EnumConverter(typeof(Enum)));
        }

        [Fact]
        public static void CanConvertTo_WithContext()
        {
            CanConvertTo_WithContext(new object[1, 2]
                {
                    { typeof(Enum[]), true }
                },
                new EnumConverter(typeof(Enum)));
        }

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[2, 3]
                {
                    { "Add  ", SomeEnum.Add, null },
                    { "Sub", SomeEnum.Sub, CultureInfo.InvariantCulture }
                },
                EnumConverterTests.s_someEnumConverter);

            ConvertFrom_WithContext(new object[2, 3]
                {
                    { "Option1, Option2  ", SomeFlagsEnum.Option1 | SomeFlagsEnum.Option2, null },
                    { new Enum[2] { SomeFlagsEnum.Option1, SomeFlagsEnum.Option2 }, SomeFlagsEnum.Option1 | SomeFlagsEnum.Option2, CultureInfo.InvariantCulture },
                },
                EnumConverterTests.s_someFlagsEnumConverter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            Assert.Throws<FormatException>(
                () => EnumConverterTests.s_someEnumConverter.ConvertFrom(TypeConverterTests.s_context, null, "<random string>"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[1, 3]
                {
                    { SomeEnum.Add, "Add", null }
                },
                EnumConverterTests.s_someEnumConverter);

            object actual = EnumConverterTests.s_someEnumConverter.ConvertTo(TypeConverterTests.s_context, CultureInfo.InvariantCulture, SomeEnum.Sub, typeof(Enum[]));
            VerifyArraysEqual<SomeEnum>(new SomeEnum[1] { SomeEnum.Sub }, actual);
        }

        [Fact]
        public static void ConvertTo_WithContext_Flags()
        {
            ConvertTo_WithContext(new object[1, 3]
                {
                    { SomeFlagsEnum.Option1 | SomeFlagsEnum.Option2, "Option1, Option2", null }
                },
                EnumConverterTests.s_someFlagsEnumConverter);

            object actual = EnumConverterTests.s_someFlagsEnumConverter.ConvertTo(TypeConverterTests.s_context, CultureInfo.InvariantCulture, SomeFlagsEnum.Option1 | SomeFlagsEnum.Option2, typeof(Enum[]));
            VerifyArraysEqual<SomeFlagsEnum>(new SomeFlagsEnum[2] { SomeFlagsEnum.Option1, SomeFlagsEnum.Option2 }, actual);

            actual = EnumConverterTests.s_someFlagsEnumConverter.ConvertTo(TypeConverterTests.s_context, null, SomeFlagsEnum.Option3, typeof(Enum[]));
            VerifyArraysEqual<SomeFlagsEnum>(new SomeFlagsEnum[1] { SomeFlagsEnum.Option3 }, actual);
        }

        [Fact]
        public static void ConvertTo_WithContext_Negative()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => EnumConverterTests.s_someEnumConverter.ConvertTo(TypeConverterTests.s_context, null, 3, typeof(string)));
            AssertExtensions.Throws<ArgumentException>(null, "enumType", () => new EnumConverter(typeof(Enum)).ConvertTo(TypeConverterTests.s_context, null, SomeFlagsEnum.Option1, typeof(string)));
        }

        [Fact]
        public static void GetStandardValues_Succeeds()
        {
            var converter = new EnumConverter(typeof(SomeEnum));
            SomeEnum[] standardValues = converter.GetStandardValues().Cast<SomeEnum>().ToArray();
            Assert.Equal(Enum.GetNames(typeof(SomeEnum)).Length, standardValues.Length);
            Assert.All(Enum.GetValues(typeof(SomeEnum)).Cast<SomeEnum>(), value => Assert.Contains(value, standardValues));
        }

        private static void VerifyArraysEqual<T>(T[] expected, object actual)
        {
            Assert.NotNull(actual);
            Assert.True(actual is Enum[]);
            Array actualArray = actual as Array;
            Assert.NotNull(actualArray); // Conversion result should be an array of enum values.
            Assert.Equal(expected.Length, actualArray.Length);

            for (int j = 0; j < expected.Length; j++)
            {
                T actualEnum = (T)actualArray.GetValue(j);
                T expectedEnum = (T)expected.GetValue(j);
                Assert.Equal(expectedEnum, actualEnum);
            }
        }
    }
}
