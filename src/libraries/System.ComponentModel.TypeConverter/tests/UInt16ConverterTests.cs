// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    { "1  ", (ushort)1, null },
                    { "#2", (ushort)2, null },
                    { "+7", (ushort)7, CultureInfo.InvariantCulture }
                },
                UInt16ConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            AssertExtensions.Throws<ArgumentException, Exception>(
                () => UInt16ConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "-8"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    { (ushort)1, "1", null },
                    { (ushort)2, (ushort)2, CultureInfo.InvariantCulture },
                    { (ushort)3, (float)3.0, null }
                },
                UInt16ConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_InvalidValue_ExceptionMessageContainsTypeName()
        {
            Exception e = Assert.ThrowsAny<Exception>(() => s_converter.ConvertFrom("badvalue"));
            Assert.Contains(typeof(ushort).Name, e.Message);
        }
    }
}
