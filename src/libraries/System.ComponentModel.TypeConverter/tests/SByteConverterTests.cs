// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                    { "1  ", (SByte)1, null },
                    { "&H6", (SByte)6, null },
                    { "-7", (SByte)(-7), CultureInfo.InvariantCulture }
                },
                SByteConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            Assert.Throws<Exception>(
                () => SByteConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "8.0"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    { (SByte)1, "1", null },
                    { (SByte)(-2), (SByte)(-2), CultureInfo.InvariantCulture },
                    { (SByte)3, (Single)3.0, null }
                },
                SByteConverterTests.s_converter);
        }
    }
}
