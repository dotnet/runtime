// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class TypeListConverterTests : ConverterTestBase
    {
        // TypeListConverter is an abstract type
        private static TypeListConverter s_converter = new MyTypeListConverter(new Type[] { typeof(bool), typeof(int) });

        [Fact]
        public static void CanConvertFrom_WithContext()
        {
            CanConvertFrom_WithContext(new object[2, 2]
                {
                    { typeof(string), true },
                    { typeof(int), false }
                },
                TypeListConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[1, 3]
                {
                    { "System.Int32", typeof(int), null }
                },
                TypeListConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[2, 3]
                {
                    { typeof(char), "System.Char", null },   // the base class is not verifying if this type is not in the list
                    { null, "(none)", CultureInfo.InvariantCulture }
                },
                TypeListConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertTo_WithContext_Negative()
        {
            Assert.Throws<InvalidCastException>(
                () => TypeListConverterTests.s_converter.ConvertTo(TypeConverterTests.s_context, null, 3, typeof(string)));
        }
    }
}
