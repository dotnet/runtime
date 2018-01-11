// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    { 1.1 + " ", (Double)1.1, null },
                    { "+7", (Double)7, CultureInfo.InvariantCulture }
                },
                DoubleConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            AssertExtensions.Throws<ArgumentException, Exception>(
                () => DoubleConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "0x8"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    { (Double)1.1, 1.1.ToString(), null },
                    { (Double)1.1, (Single)1.1, CultureInfo.InvariantCulture },
                    { (Double)1.1, (Single)1.1, null }
                },
                DoubleConverterTests.s_converter);
        }
    }
}
