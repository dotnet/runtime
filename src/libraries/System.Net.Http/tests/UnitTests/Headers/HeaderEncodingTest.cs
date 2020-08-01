// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace System.Net.Http.Tests
{
    public class HeaderEncodingTest
    {
        [Theory]
        [InlineData("")]
        [InlineData("foo")]
        [InlineData("\uD83D\uDE03")]
        [InlineData("\0")]
        [InlineData("\x01")]
        [InlineData("\xFF")]
        [InlineData("\uFFFF")]
        [InlineData("\uFFFD")]
        [InlineData("\uD83D\uDE48\uD83D\uDE49\uD83D\uDE4A")]
        public void RoundTripsUtf8(string input)
        {
            byte[] encoded = Encoding.UTF8.GetBytes(input);

            Assert.True(HeaderDescriptor.TryGet("custom-header", out HeaderDescriptor descriptor));
            Assert.Null(descriptor.KnownHeader);
            string roundtrip = descriptor.GetHeaderValue(encoded, Encoding.UTF8);
            Assert.Equal(input, roundtrip);

            Assert.True(HeaderDescriptor.TryGet("Cache-Control", out descriptor));
            Assert.NotNull(descriptor.KnownHeader);
            roundtrip = descriptor.GetHeaderValue(encoded, Encoding.UTF8);
            Assert.Equal(input, roundtrip);
        }
    }
}
