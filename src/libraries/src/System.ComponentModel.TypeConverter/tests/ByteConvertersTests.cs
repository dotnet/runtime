// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ByteConverterTests : ConverterTestBase
    {
        private static TypeConverter s_converter = new ByteConverter();

        [Fact]
        public static void CanConvertFrom_WithContext()
        {
            CanConvertFrom_WithContext(new object[2, 2]
                {
                    { typeof(string), true },
                    { typeof(int), false }
                },
                ByteConverterTests.s_converter);
        }

        [Fact]
        public static void CanConvertTo_WithContext()
        {
            CanConvertTo_WithContext(new object[3, 2]
                {
                    { typeof(string), true },
                    { typeof(Byte), true },
                    { typeof(Single), true }
                },
                ByteConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[7, 3]
                {
                    { "1  ", (Byte)1, null },
                    { "#2", (Byte)2, null },
                    { "0x3", (Byte)3, null },
                    { "0X4", (Byte)4, null },
                    { "&h5", (Byte)5, null },
                    { "&H6", (Byte)6, null },
                    { "+7", (Byte)7, CultureInfo.InvariantCulture }
                },
                ByteConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            AssertExtensions.Throws<ArgumentException, Exception>(
                () => ByteConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "8.0"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    { (Byte)1, "1", null },
                    { (Byte)2, (Byte)2, CultureInfo.InvariantCulture },
                    { (Byte)3, (Single)3.0, null }
                },
                ByteConverterTests.s_converter);
        }
    }
}
