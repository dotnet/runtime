// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class Int16ConverterTests : ConverterTestBase
    {
        private static TypeConverter s_converter = new Int16Converter();

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[3, 3]
                {
                    { "-1  ", (Int16)(-1), null },
                    { "#2", (Int16)2, null },
                    { "+7", (Int16)7, CultureInfo.InvariantCulture }
                },
                Int16ConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            Assert.Throws<Exception>(
                () => Int16ConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "8.0"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    { (Int16)(-1), "-1", null },
                    { (Int16)2, (Int16)2, CultureInfo.InvariantCulture },
                    { (Int16)3, (Single)3.0, null }
                },
                Int16ConverterTests.s_converter);
        }
    }
}
