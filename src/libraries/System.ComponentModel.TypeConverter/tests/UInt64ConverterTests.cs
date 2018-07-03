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
                    { "1  ", (ulong)1, null },
                    { "#2", (ulong)2, null },
                    { "+7", (ulong)7, CultureInfo.InvariantCulture }
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
                    { (ulong)1, "1", null },
                    { (ulong)2, (ulong)2, CultureInfo.InvariantCulture },
                    { (ulong)3, (float)3.0, null }
                },
                UInt64ConverterTests.s_converter);
        }
    }
}
