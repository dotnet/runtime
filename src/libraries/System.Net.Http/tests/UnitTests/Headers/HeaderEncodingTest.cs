// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.Http.Tests
{
    public class HeaderEncodingTest
    {
        public static readonly TheoryData<string, string?> RoundTrips_Data = new TheoryData<string, string?>
        {
            { "", null },
            { "foo", null },
            { "\uD83D\uDE03", null },
            { "\x01", null },
            { "\xFF", null },
            { "\uFFFF", null },
            { "\uFFFD", null },
            { "\uD83D\uDE48\uD83D\uDE49\uD83D\uDE4A", null },
            { "\0", null },
            { "abc\056", null },
            { "abc\rq", null },
            { "abc\r\n", null },
            { "abc\rfoo", null },

            { "", "UTF-8" },
            { "foo", "UTF-8" },
            { "\uD83D\uDE03", "UTF-8" },
            { "\x01", "UTF-8" },
            { "\xFF", "UTF-8" },
            { "\uFFFF", "UTF-8" },
            { "\uFFFD", "UTF-8" },
            { "\uD83D\uDE48\uD83D\uDE49\uD83D\uDE4A", "UTF-8" },
            { "\0", "UTF-8" },
            { "abc\056", "UTF-8" },
            { "abc\rq", "UTF-8" },
            { "abc\r\n", "UTF-8" },
            { "abc\rfoo", "UTF-8" },

            // Fixed, multi byte encodings are discouraged, but we want them to function at HeaderDescriptor level.
            { "", "UTF-16" },
            { "foo", "UTF-16" },
            { "\uD83D\uDE03", "UTF-16" },
            { "\x01", "UTF-16" },
            { "\xFF", "UTF-16" },
            { "\uFFFF", "UTF-16" },
            { "\uFFFD", "UTF-16" },
            { "\uD83D\uDE48\uD83D\uDE49\uD83D\uDE4A", "UTF-16" },
            { "\0", "UTF-16" },
            { "abc\056", "UTF-16" },
            { "abc\rq", "UTF-16" },
            { "abc\r\n", "UTF-16" },
            { "abc\rfoo", "UTF-16" },
        };

        [ConditionalTheory]
        [MemberData(nameof(RoundTrips_Data))]
        public void GetHeaderValue_RoundTrips_ReplacesDangerousCharacters(string input, string? encodingName)
        {
            bool isUnicode = input.Any(c => c > 255);
            if (isUnicode && encodingName == null)
            {
                throw new SkipTestException("The test case is invalid for the default encoding.");
            }

            Encoding encoding = encodingName == null ? null : Encoding.GetEncoding(encodingName);
            byte[] encoded = (encoding ?? Encoding.Latin1).GetBytes(input);
            string expectedValue = input.Replace('\0', ' ').Replace('\r', ' ').Replace('\n', ' ');

            Assert.True(HeaderDescriptor.TryGet("custom-header", out HeaderDescriptor descriptor));
            Assert.Null(descriptor.KnownHeader);
            string roundtrip = descriptor.GetHeaderValue(encoded, encoding);
            Assert.Equal(expectedValue, roundtrip);

            Assert.True(HeaderDescriptor.TryGet("Cache-Control", out descriptor));
            Assert.NotNull(descriptor.KnownHeader);
            roundtrip = descriptor.GetHeaderValue(encoded, encoding);
            Assert.Equal(expectedValue, roundtrip);
        }
    }
}
