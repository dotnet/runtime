// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class TimeSpanConverterTests : ConverterTestBase
    {
        private static TimeSpanConverter s_converter = new TimeSpanConverter();

        [Fact]
        public static void CanConvertFrom_WithContext()
        {
            CanConvertFrom_WithContext(new object[1, 2]
                {
                    { typeof(string), true }
                },
                TimeSpanConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[2, 3]
                {
                    {"1000.00:00:00   ", new TimeSpan(1000, 0, 0, 0, 0),  CultureInfo.InvariantCulture},
                    {"1000", new TimeSpan(1000, 0, 0, 0, 0),  null}
                },
                TimeSpanConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            Assert.Throws<FormatException>(
                () => TimeSpanConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "random string"));
        }
    }
}
