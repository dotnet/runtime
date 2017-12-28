// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class Int32ConverterTests : ConverterTestBase
    {
        private static TypeConverter s_converter = new Int32Converter();

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[3, 3]
                {
                    { "1  ", (Int32)1, null },
                    { "#2", (Int32)2, null },
                    { "+7", (Int32)7, CultureInfo.InvariantCulture }
                },
                Int32ConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            AssertExtensions.Throws<ArgumentException, Exception>(
                () => Int32ConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "8.0"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    {(Int32)1, "1", null},
                    {(Int32)(-2), (Int32)(-2), CultureInfo.InvariantCulture},
                    {(Int32)3, (Single)3.0, null}
                },
                Int32ConverterTests.s_converter);
        }
    }
}
