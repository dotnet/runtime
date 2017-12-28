// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class UInt64ConverterTests : ConverterTestBase
    {
        private static TypeConverter s_converter = new UInt64Converter();

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[3, 3]
                {
                    { "1  ", (UInt64)1, null },
                    { "#2", (UInt64)2, null },
                    { "+7", (UInt64)7, CultureInfo.InvariantCulture }
                },
                UInt64ConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            AssertExtensions.Throws<ArgumentException, Exception>(
                () => UInt64ConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "-8"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    { (UInt64)1, "1", null },
                    { (UInt64)2, (UInt64)2, CultureInfo.InvariantCulture },
                    { (UInt64)3, (Single)3.0, null }
                },
                UInt64ConverterTests.s_converter);
        }
    }
}
