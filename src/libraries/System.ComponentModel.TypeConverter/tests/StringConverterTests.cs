// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class StringConverterTests : ConverterTestBase
    {
        private static StringConverter s_converter = new StringConverter();

        [Fact]
        public static void CanConvertFrom_WithContext()
        {
            CanConvertFrom_WithContext(new object[1, 2]
                {
                    { typeof(string), true }
                },
                StringConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[2, 3]
                {
                    { "some string", "some string", null },
                    { null, string.Empty, null }
                },
                StringConverterTests.s_converter);
        }
    }
}
