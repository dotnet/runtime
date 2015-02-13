// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DoubleConverterTests : ConverterTestBase
    {
        private static TypeConverter s_converter = new DoubleConverter();

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[2, 3]
                {
                    { "1.1  ", (Double)1.1, null },
                    { "+7", (Double)7, CultureInfo.InvariantCulture }
                },
                DoubleConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            Assert.Throws<Exception>(
                () => DoubleConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "0x8"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    { (Double)1.1, "1.1", null },
                    { (Double)1.1, (Single)1.1, CultureInfo.InvariantCulture },
                    { (Double)1.1, (Single)1.1, null }
                },
                DoubleConverterTests.s_converter);
        }
    }
}
