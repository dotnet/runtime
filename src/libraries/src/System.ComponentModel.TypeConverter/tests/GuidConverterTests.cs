// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class GuidConverterTests : ConverterTestBase
    {
        private static GuidConverter s_converter = new GuidConverter();

        [Fact]
        public static void CanConvertFrom_WithContext()
        {
            CanConvertFrom_WithContext(new object[1, 2]
                {
                    { typeof(string), true }
                },
                GuidConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[1, 3]
                {
                    { " {30da92c0-23e8-42a0-ae7c-734a0e5d2782}", new Guid(0x30da92c0, 0x23e8, 0x42a0, 0xae, 0x7c, 0x73, 0x4a, 0x0e, 0x5d, 0x27, 0x82), null }
                },
                GuidConverterTests.s_converter);
        }
    }
}
