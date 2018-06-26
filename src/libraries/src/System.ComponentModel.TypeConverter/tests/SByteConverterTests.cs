// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class SByteConverterTests : ConverterTestBase
    {
        private static TypeConverter s_converter = new SByteConverter();

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[3, 3]
                {
                    { "1  ", (sbyte)1, null },
                    { "&H6", (sbyte)6, null },
                    { "-7", (sbyte)(-7), CultureInfo.InvariantCulture }
                },
                SByteConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            AssertExtensions.Throws<ArgumentException, Exception>(
                () => SByteConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "8.0"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    { (sbyte)1, "1", null },
                    { (sbyte)(-2), (sbyte)(-2), CultureInfo.InvariantCulture },
                    { (sbyte)3, (float)3.0, null }
                },
                SByteConverterTests.s_converter);
        }
    }
}
