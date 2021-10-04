// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexGeneratorAttributeTests
    {
        [Theory]
        [InlineData(null, RegexOptions.None, Timeout.Infinite)]
        [InlineData("", (RegexOptions)12345, -2)]
        [InlineData("a.*b", RegexOptions.Compiled | RegexOptions.CultureInvariant, 1)]
        public void Ctor_Roundtrips(string pattern, RegexOptions options, int matchTimeoutMilliseconds)
        {
            RegexGeneratorAttribute a;

            if (matchTimeoutMilliseconds == -1)
            {
                if (options == RegexOptions.None)
                {
                    a = new RegexGeneratorAttribute(pattern);
                    Assert.Equal(pattern, a.Pattern);
                    Assert.Equal(RegexOptions.None, a.Options);
                    Assert.Equal(Timeout.Infinite, a.MatchTimeoutMilliseconds);
                }

                a = new RegexGeneratorAttribute(pattern, options);
                Assert.Equal(pattern, a.Pattern);
                Assert.Equal(options, a.Options);
                Assert.Equal(Timeout.Infinite, a.MatchTimeoutMilliseconds);
            }

            a = new RegexGeneratorAttribute(pattern, options, matchTimeoutMilliseconds);
            Assert.Equal(pattern, a.Pattern);
            Assert.Equal(options, a.Options);
            Assert.Equal(matchTimeoutMilliseconds, a.MatchTimeoutMilliseconds);
        }
    }
}
