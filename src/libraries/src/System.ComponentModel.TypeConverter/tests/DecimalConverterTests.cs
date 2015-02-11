// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DecimalConverterTests : ConverterTestBase
    {
        private static TypeConverter s_converter = new DecimalConverter();

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[2, 3]
                {
                    { "1.1  ", (Decimal)1.1, null },
                    { "+7", (Decimal)7, CultureInfo.InvariantCulture }
                },
                DecimalConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            Assert.Throws<Exception>(
                () => DecimalConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "0x8"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    {(Decimal)1.1, "1.1", null},
                    {(Decimal)1.1, (Byte)1, CultureInfo.InvariantCulture},
                    {(Decimal)1.1, (Single)1.1, null}
                },
                DecimalConverterTests.s_converter);
        }
    }
}
