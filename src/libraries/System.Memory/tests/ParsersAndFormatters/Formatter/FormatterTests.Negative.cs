// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Buffers.Text.Tests
{
    public static partial class FormatterTests
    {
        [Theory]
        [MemberData(nameof(TestData.TypesThatCanBeFormatted), MemberType = typeof(TestData))]
        public static void TestBadFormat(Type integerType)
        {
            object value = Activator.CreateInstance(integerType);
            Assert.Throws<FormatException>(() => TryFormatUtf8(value, Array.Empty<byte>(), out int bytesWritten, new StandardFormat('$', 1)));
        }

        [Theory]
        [MemberData(nameof(TestData.IntegerTypesTheoryData), MemberType = typeof(TestData))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(double))]
        [InlineData(typeof(float))]
        public static void TestGFormatWithPrecisionNotSupported(Type type)
        {
            object value = Activator.CreateInstance(type);
            Assert.Throws<NotSupportedException>(() => TryFormatUtf8(value, Array.Empty<byte>(), out int bytesWritten, new StandardFormat('G', 1)));
            Assert.Throws<NotSupportedException>(() => TryFormatUtf8(value, Array.Empty<byte>(), out int bytesWritten, new StandardFormat('g', 1)));
        }
    }
}

