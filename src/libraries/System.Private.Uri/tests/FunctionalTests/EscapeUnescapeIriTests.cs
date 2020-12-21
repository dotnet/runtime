// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.PrivateUri.Tests
{
    public class EscapeUnescapeIriTests
    {
        public static IEnumerable<object[]> ReplacesStandaloneSurrogatesWithReplacementChar_Data()
        {
            const string UrlEncodedReplacementChar = "%EF%BF%BD";
            const string HighSurrogate = "\ud83f";
            const string LowSurrogate = "\udffe";

            yield return new object[] { "a", "a" };
            yield return new object[] { HighSurrogate + LowSurrogate, "%F0%9F%BF%BE" };
            yield return new object[] { HighSurrogate, UrlEncodedReplacementChar };
            yield return new object[] { LowSurrogate, UrlEncodedReplacementChar };
            yield return new object[] { LowSurrogate + HighSurrogate, UrlEncodedReplacementChar + UrlEncodedReplacementChar };
            yield return new object[] { LowSurrogate + LowSurrogate, UrlEncodedReplacementChar + UrlEncodedReplacementChar };
            yield return new object[] { HighSurrogate + HighSurrogate, UrlEncodedReplacementChar + UrlEncodedReplacementChar };
        }

        [Theory]
        [MemberData(nameof(ReplacesStandaloneSurrogatesWithReplacementChar_Data))]
        public static void ReplacesStandaloneSurrogatesWithReplacementChar(string input, string expected)
        {
            const string Prefix = "scheme:";
            Uri uri = new Uri(Prefix + input);
            string actual = uri.AbsoluteUri.Substring(Prefix.Length);
            Assert.Equal(expected, actual);
        }
    }
}
