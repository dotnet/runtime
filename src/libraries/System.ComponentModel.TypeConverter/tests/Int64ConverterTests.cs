// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    { "1  ", (long)1, null },
                    { "#2", (long)2, null },
                    { "+7", (long)7, CultureInfo.InvariantCulture }
                },
                Int64ConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            AssertExtensions.Throws<ArgumentException, Exception>(
                () => Int64ConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "8.0"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    {(long)1, "1", null},
                    {(long)(-2), (long)(-2), CultureInfo.InvariantCulture},
                    {(long)3, (float)3.0, null}
                },
                Int64ConverterTests.s_converter);
        }
    }
}
