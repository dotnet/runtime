// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class SingleConverterTests : ConverterTestBase
    {
        private static TypeConverter s_converter = new SingleConverter();

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[2, 3]
                {
                    { 1.1f + " ", (Single)1.1, null },
                    { "+7", (Single)7, CultureInfo.InvariantCulture }
                },
                SingleConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            AssertExtensions.Throws<ArgumentException, Exception>(
                () => SingleConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "0x8"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[2, 3]
                {
                    { (Single)1.1, 1.1f.ToString(), null },
                    { (Single)1.1, 1, CultureInfo.InvariantCulture }
                },
                SingleConverterTests.s_converter);
        }
    }
}
