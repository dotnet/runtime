// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class Int64ConverterTests : ConverterTestBase
    {
        private static TypeConverter s_converter = new Int64Converter();

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[3, 3]
                {
                    { "1  ", (Int64)1, null },
                    { "#2", (Int64)2, null },
                    { "+7", (Int64)7, CultureInfo.InvariantCulture }
                },
                Int64ConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            Assert.Throws<Exception>(
                () => Int64ConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "8.0"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    {(Int64)1, "1", null},
                    {(Int64)(-2), (Int64)(-2), CultureInfo.InvariantCulture},
                    {(Int64)3, (Single)3.0, null}
                },
                Int64ConverterTests.s_converter);
        }
    }
}
