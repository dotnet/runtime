// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class CharConverterTests : ConverterTestBase
    {
        private static TypeConverter s_converter = new CharConverter();

        [Fact]
        public static void CanConvertFrom_WithContext()
        {
            CanConvertFrom_WithContext(new object[2, 2]
                {
                    { typeof(string), true },
                    { typeof(int), false }
                },
                CharConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    {'a', "a", null},
                    {'\0', "", null},
                    {'\u20AC', "\u20AC", CultureInfo.InvariantCulture}
                },
                CharConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[3, 3]
                {
                    { " a  ", 'a', CultureInfo.InvariantCulture },
                    { "    ", '\0', null},
                    { "", '\0', null }
                },
               CharConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            Assert.Throws<FormatException>(
                () => CharConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "aaa"));

            Assert.Throws<NotSupportedException>(
                () => CharConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, null));
        }
    }
}
