// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexMatchTimeoutExceptionTests
    {
        [Fact]
        public void Ctor()
        {
            RegexMatchTimeoutException e;

            e = new RegexMatchTimeoutException();
            Assert.Empty(e.Input);
            Assert.Empty(e.Pattern);
            Assert.Equal(TimeSpan.FromTicks(-1), e.MatchTimeout);

            const string Message = "some message";
            e = new RegexMatchTimeoutException(Message);
            Assert.Equal(Message, e.Message);
            Assert.Empty(e.Input);
            Assert.Empty(e.Pattern);
            Assert.Equal(TimeSpan.FromTicks(-1), e.MatchTimeout);

            var inner = new FormatException();
            e = new RegexMatchTimeoutException(Message, inner);
            Assert.Equal(Message, e.Message);
            Assert.Same(inner, e.InnerException);
            Assert.Empty(e.Input);
            Assert.Empty(e.Pattern);
            Assert.Equal(TimeSpan.FromTicks(-1), e.MatchTimeout);

            const string Input = "abcdef";
            const string Pattern = "(?:abcdef)*";
            TimeSpan timeout = TimeSpan.FromSeconds(42);
            e = new RegexMatchTimeoutException(Input, Pattern, timeout);
            Assert.Equal(Input, e.Input);
            Assert.Equal(Pattern, e.Pattern);
            Assert.Equal(timeout, e.MatchTimeout);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50942", TestPlatforms.Android)]
        public void SerializationRoundtrip()
        {
            const string Input = "abcdef";
            const string Pattern = "(?:abcdef)*";
            TimeSpan timeout = TimeSpan.FromSeconds(42);
            var e = new RegexMatchTimeoutException(Input, Pattern, timeout);

            var bf = new BinaryFormatter();
            var s = new MemoryStream();
            bf.Serialize(s, e);
            s.Position = 0;
            e = (RegexMatchTimeoutException)bf.Deserialize(s);

            Assert.Equal(Input, e.Input);
            Assert.Equal(Pattern, e.Pattern);
            Assert.Equal(timeout, e.MatchTimeout);
        }
    }
}
