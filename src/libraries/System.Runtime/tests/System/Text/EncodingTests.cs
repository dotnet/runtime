// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class EncodingTests
    {
        [Theory]
        [InlineData(65000)] // UTF-7
        public void GetEncoding_ByCodePage_WithDisallowedEncoding_Throws(int codePage)
        {
            Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding(codePage));
            Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding(codePage, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback));
        }

        [Theory]
        [InlineData("utf-7")]
        public void GetEncoding_ByName_WithDisallowedEncoding_Throws(string name)
        {
            Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding(name));
            Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding(name, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback));
        }
    }
}
