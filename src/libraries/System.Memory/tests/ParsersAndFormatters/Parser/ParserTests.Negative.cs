// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Buffers.Text.Tests
{
    public static partial class ParserTests
    {
        [Theory]
        [MemberData(nameof(TestData.TypesThatCanBeParsed), MemberType = typeof(TestData))]
        public static void TestParserBadFormat(Type type)
        {
            Assert.Throws<FormatException>(() => TryParseUtf8(type, Array.Empty<byte>(), out object value, out int bytesConsumed, '$'));
        }
    }
}

