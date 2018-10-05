// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class NullableConverterTests : ConverterTestBase
    {
        private static NullableConverter s_intNullableConverter = new NullableConverter(typeof(int?));
        private static NullableConverter s_myNullableConverter = new NullableConverter(typeof(SomeValueType?));
        private static int? s_uninitializedInt = null;
        private static int? s_nullableThree = 3;

        [Fact]
        public static void Constructor_Negative()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => new NullableConverter(typeof(string)));
        }

        [Fact]
        public static void Get_UnderlyingType()
        {
            Assert.Equal(NullableConverterTests.s_intNullableConverter.UnderlyingType, typeof(int));
            Assert.Equal(NullableConverterTests.s_myNullableConverter.UnderlyingType, typeof(SomeValueType));
        }

        [Fact]
        public static void Get_NullableType()
        {
            Assert.Equal(NullableConverterTests.s_intNullableConverter.NullableType, typeof(int?));
            Assert.Equal(NullableConverterTests.s_myNullableConverter.NullableType, typeof(SomeValueType?));
        }

        [Fact]
        public static void Get_UnderlyingTypeConverter()
        {
            Assert.True(NullableConverterTests.s_intNullableConverter.UnderlyingTypeConverter is Int32Converter);
            Assert.True(NullableConverterTests.s_myNullableConverter.UnderlyingTypeConverter is TypeConverter);
        }

        [Fact]
        public static void CanConvertFrom_WithContext()
        {
            CanConvertFrom_WithContext(new object[2, 2]
                {
                    { typeof(int), true },
                    { typeof(string), true }
                },
                NullableConverterTests.s_intNullableConverter);
        }

        [Fact]
        public static void CanConvertTo_WithContext()
        {
            CanConvertTo_WithContext(new object[3, 2]
                {
                    { typeof(int), true },
                    { typeof(string), true },
                    { typeof(InstanceDescriptor), true }
                },
                NullableConverterTests.s_intNullableConverter);
        }

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[3, 3]
                {
                    { "1  ", 1, CultureInfo.InvariantCulture },
                    { null, null, null },
                    { 2, 2, null }
                },
                NullableConverterTests.s_intNullableConverter);
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[4, 3]
                {
                    { NullableConverterTests.s_nullableThree, 3, null },
                    { NullableConverterTests.s_uninitializedInt, string.Empty, null },
                    { null, string.Empty, null },
                    { 4, "4", CultureInfo.InvariantCulture }
                },
                NullableConverterTests.s_intNullableConverter);

            var actualInstanceDescriptor = (InstanceDescriptor)NullableConverterTests.s_intNullableConverter.ConvertTo(NullableConverterTests.s_nullableThree, typeof(InstanceDescriptor));
            var expectedMemberInfo = typeof(int?).GetConstructor(new Type[] { typeof(int) });
            Assert.Equal(expectedMemberInfo, actualInstanceDescriptor.MemberInfo);
            Assert.Equal(new[] { NullableConverterTests.s_nullableThree }, actualInstanceDescriptor.Arguments);
            Assert.True(actualInstanceDescriptor.IsComplete);
            Assert.Equal(NullableConverterTests.s_nullableThree, actualInstanceDescriptor.Invoke());

            var actualUnInitInstanceDescriptor = (InstanceDescriptor)NullableConverterTests.s_intNullableConverter.ConvertTo(NullableConverterTests.s_uninitializedInt, typeof(InstanceDescriptor));
            Assert.Equal(expectedMemberInfo, actualUnInitInstanceDescriptor.MemberInfo);
            Assert.Equal(new[] { NullableConverterTests.s_uninitializedInt }, actualUnInitInstanceDescriptor.Arguments);
            Assert.True(actualUnInitInstanceDescriptor.IsComplete);
            // This appears to be a bug present in desktop as well
            // Assert.Equal(NullableConverterTests.s_uninitializedInt, actualUnInitInstanceDescriptor.Invoke());
            Assert.Equal(0, actualUnInitInstanceDescriptor.Invoke());

            SomeValueType v;
            v.a = 10;
            SomeValueType? nullableV = null;
            nullableV = v;
            ConvertTo_WithContext(new object[2, 3]
                {
                    { new SomeValueType(), "System.ComponentModel.Tests.SomeValueType", CultureInfo.InvariantCulture },
                    { nullableV, v, CultureInfo.InvariantCulture }
                },
                NullableConverterTests.s_myNullableConverter);
        }
    }
}
