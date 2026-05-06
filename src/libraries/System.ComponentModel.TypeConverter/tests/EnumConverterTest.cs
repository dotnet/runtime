// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
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
        public static void CanConvertFrom_WithContext_EnumConverter()
        {
            CanConvertFrom_WithContext(new object[2, 2]
                {
                    { typeof(string), true },
                    { typeof(Enum[]), true }
                },
                new EnumConverter(typeof(Enum)));
        }

        [Fact]
        public static void CanConvertTo_WithContext_EnumConverter()
        {
            CanConvertTo_WithContext(new object[2, 2]
                {
                    { typeof(Enum[]), true },
                    { typeof(InstanceDescriptor), true },
                },
                new EnumConverter(typeof(Enum)));
        }

        [Fact]
        public static void ConvertFrom_WithContext_EnumConverter()
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
        public static void ConvertTo_WithContext_EnumConverter()
        {
            ConvertTo_WithContext(new object[1, 3]
                {
                    { SomeEnum.Add, "Add", null }
                },
                EnumConverterTests.s_someEnumConverter);

            object actual = EnumConverterTests.s_someEnumConverter.ConvertTo(TypeConverterTests.s_context, CultureInfo.InvariantCulture, SomeEnum.Sub, typeof(Enum[]));
            VerifyArraysEqual<SomeEnum>(new SomeEnum[1] { SomeEnum.Sub }, actual);

            var actualInstanceDescriptor = (InstanceDescriptor)EnumConverterTests.s_someEnumConverter.ConvertTo(SomeEnum.Add, typeof(InstanceDescriptor));
            var expectedMemberInfo = typeof(SomeEnum).GetField(nameof(SomeEnum.Add));
            Assert.Equal(expectedMemberInfo, actualInstanceDescriptor.MemberInfo);
            Assert.Empty(actualInstanceDescriptor.Arguments);
            Assert.True(actualInstanceDescriptor.IsComplete);
            Assert.Equal(SomeEnum.Add, actualInstanceDescriptor.Invoke());
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


            var actualInstanceDescriptor = (InstanceDescriptor)EnumConverterTests.s_someFlagsEnumConverter.ConvertTo(SomeFlagsEnum.Option1 | SomeFlagsEnum.Option2, typeof(InstanceDescriptor));
            var expectedMemberInfo = typeof(Enum).GetMethod("ToObject", new Type[] { typeof(Type), typeof(int) });
            Assert.Equal(expectedMemberInfo, actualInstanceDescriptor.MemberInfo);
            Assert.Equal(new object[] { typeof(SomeFlagsEnum), (int)(SomeFlagsEnum.Option1 | SomeFlagsEnum.Option2) }, actualInstanceDescriptor.Arguments);
            Assert.True(actualInstanceDescriptor.IsComplete);
            Assert.Equal(SomeFlagsEnum.Option1 | SomeFlagsEnum.Option2, actualInstanceDescriptor.Invoke());
        }

        [Fact]
        public static void ConvertTo_WithContext_Negative()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => EnumConverterTests.s_someEnumConverter.ConvertTo(TypeConverterTests.s_context, null, 3, typeof(string)));
            AssertExtensions.Throws<ArgumentException>("enumType", () => new EnumConverter(typeof(Enum)).ConvertTo(TypeConverterTests.s_context, null, SomeFlagsEnum.Option1, typeof(string)));
        }

        [Fact]
        public static void GetStandardValues_Succeeds()
        {
            var converter = new EnumConverter(typeof(SomeEnum));
            SomeEnum[] standardValues = converter.GetStandardValues().Cast<SomeEnum>().ToArray();
            Assert.Equal(Enum.GetNames(typeof(SomeEnum)).Length, standardValues.Length);
            Assert.All(Enum.GetValues(typeof(SomeEnum)).Cast<SomeEnum>(), value => Assert.Contains(value, standardValues));
        }

        [Fact]
        public static void ConvertFrom_ULongFlagsEnum_String()
        {
            EnumConverter converter = new EnumConverter(typeof(ULongFlagsEnum));
            string str = $"{ULongFlagsEnum.Bit62}, {ULongFlagsEnum.Bit63}";
            ULongFlagsEnum result = (ULongFlagsEnum)converter.ConvertFrom(null, null, str);
            Assert.Equal(ULongFlagsEnum.Bit62 | ULongFlagsEnum.Bit63, result);
        }

        [Fact]
        public static void ConvertFrom_ULongFlagsEnum_EnumArray()
        {
            EnumConverter converter = new EnumConverter(typeof(ULongFlagsEnum));
            Enum[] arr = new Enum[] { ULongFlagsEnum.Bit62, ULongFlagsEnum.Bit63 };
            ULongFlagsEnum result = (ULongFlagsEnum)converter.ConvertFrom(null, null, arr);
            Assert.Equal(ULongFlagsEnum.Bit62 | ULongFlagsEnum.Bit63, result);
        }

        [Fact]
        public static void ConvertTo_ULongFlagsEnum_EnumArray()
        {
            EnumConverter converter = new EnumConverter(typeof(ULongFlagsEnum));
            ULongFlagsEnum value = ULongFlagsEnum.Bit62 | ULongFlagsEnum.Bit63;
            Enum[] result = (Enum[])converter.ConvertTo(null, null, value, typeof(Enum[]));
            Assert.Equal(new Enum[] { ULongFlagsEnum.Bit62, ULongFlagsEnum.Bit63 }, result);
        }

        [Fact]
        public static void ConvertFrom_LongFlagsEnum_String()
        {
            EnumConverter converter = new EnumConverter(typeof(LongFlagsEnum));
            string str = $"{LongFlagsEnum.Bit62}, {LongFlagsEnum.Bit63}";
            LongFlagsEnum result = (LongFlagsEnum)converter.ConvertFrom(null, null, str);
            Assert.Equal(LongFlagsEnum.Bit62 | LongFlagsEnum.Bit63, result);
        }

        [Fact]
        public static void ConvertFrom_LongFlagsEnum_EnumArray()
        {
            EnumConverter converter = new EnumConverter(typeof(LongFlagsEnum));
            Enum[] arr = new Enum[] { LongFlagsEnum.Bit62, LongFlagsEnum.Bit63 };
            LongFlagsEnum result = (LongFlagsEnum)converter.ConvertFrom(null, null, arr);
            Assert.Equal(LongFlagsEnum.Bit62 | LongFlagsEnum.Bit63, result);
        }

        [Fact]
        public static void ConvertTo_LongFlagsEnum_EnumArray()
        {
            EnumConverter converter = new EnumConverter(typeof(LongFlagsEnum));
            LongFlagsEnum value = LongFlagsEnum.Bit62 | LongFlagsEnum.Bit63;
            Enum[] result = (Enum[])converter.ConvertTo(null, null, value, typeof(Enum[]));
            Assert.Equal(new Enum[] { LongFlagsEnum.Bit62, LongFlagsEnum.Bit63 }, result);
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
