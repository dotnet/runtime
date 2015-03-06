// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class UInt16ConverterTests : ConverterTestBase
    {
        private static TypeConverter s_converter = new UInt16Converter();

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[3, 3]
                {
                    { "1  ", (UInt16)1, null },
                    { "#2", (UInt16)2, null },
                    { "+7", (UInt16)7, CultureInfo.InvariantCulture }
                },
                UInt16ConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            Assert.Throws<Exception>(
                () => UInt16ConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "-8"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    { (UInt16)1, "1", null },
                    { (UInt16)2, (UInt16)2, CultureInfo.InvariantCulture },
                    { (UInt16)3, (Single)3.0, null }
                },
                UInt16ConverterTests.s_converter);
        }
    }
}
