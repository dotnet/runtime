// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class BooleanConverterTests : ConverterTestBase
    {
        private static BooleanConverter s_converter = new BooleanConverter();

        [Fact]
        public static void CanConvertFrom_WithContext()
        {
            CanConvertFrom_WithContext(new object[2, 2]
                {
                    { typeof(string), true },
                    { typeof(int), false }
                },
                BooleanConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[2, 3]
                {
                    { "false  ", false, null },
                    { "true", true, CultureInfo.InvariantCulture }
                },
                BooleanConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            Assert.Throws<FormatException>(
                () => BooleanConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "1"));
        }
    }
}
