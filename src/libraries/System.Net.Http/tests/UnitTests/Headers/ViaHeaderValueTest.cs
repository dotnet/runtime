// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class ViaHeaderValueTest
    {
        [Fact]
        public void Ctor_ProtocolVersionAndReceivedByOnlyOverload_CallForwardedToOtherCtor()
        {
            ViaHeaderValue via = new ViaHeaderValue("1.1", ".token");
            Assert.Equal("1.1", via.ProtocolVersion);
            Assert.Equal(".token", via.ReceivedBy);
            Assert.Null(via.ProtocolName);
            Assert.Null(via.Comment);

            via = new ViaHeaderValue("x11", "[::1]:1818");
            Assert.Equal("x11", via.ProtocolVersion);
            Assert.Equal("[::1]:1818", via.ReceivedBy);

            AssertExtensions.Throws<ArgumentNullException>("protocolVersion", () => { new ViaHeaderValue(null, "host"); });
            AssertExtensions.Throws<ArgumentException>("protocolVersion", () => { new ViaHeaderValue("", "host"); });
            Assert.Throws<FormatException>(() => { new ViaHeaderValue("x y", "h"); });
            Assert.Throws<FormatException>(() => { new ViaHeaderValue("x ", "h"); });
            Assert.Throws<FormatException>(() => { new ViaHeaderValue(" x", "h"); });
            AssertExtensions.Throws<ArgumentNullException>("receivedBy", () => { new ViaHeaderValue("1.1", null); });
            AssertExtensions.Throws<ArgumentException>("receivedBy", () => { new ViaHeaderValue("1.1", ""); });
            Assert.Throws<FormatException>(() => { new ViaHeaderValue("v", "x y"); });
            Assert.Throws<FormatException>(() => { new ViaHeaderValue("v", "x "); });
            Assert.Throws<FormatException>(() => { new ViaHeaderValue("v", " x"); });
        }

        [Fact]
        public void Ctor_ProtocolVersionReceivedByAndProtocolNameOnlyOverload_CallForwardedToOtherCtor()
        {
            ViaHeaderValue via = new ViaHeaderValue("1.1", "host", "HTTP");
            Assert.Equal("1.1", via.ProtocolVersion);
            Assert.Equal("host", via.ReceivedBy);
            Assert.Equal("HTTP", via.ProtocolName);
            Assert.Null(via.Comment);

            Assert.Throws<FormatException>(() => { new ViaHeaderValue("v", "h", "x y"); });
            Assert.Throws<FormatException>(() => { new ViaHeaderValue("v", "h", "x "); });
            Assert.Throws<FormatException>(() => { new ViaHeaderValue("v", "h", " x"); });
        }

        [Fact]
        public void Ctor_AllParams_AllFieldsInitializedCorrectly()
        {
            ViaHeaderValue via = new ViaHeaderValue("1.1", "host", "HTTP", "(comment)");
            Assert.Equal("1.1", via.ProtocolVersion);
            Assert.Equal("host", via.ReceivedBy);
            Assert.Equal("HTTP", via.ProtocolName);
            Assert.Equal("(comment)", via.Comment);

            Assert.Throws<FormatException>(() => { new ViaHeaderValue("v", "h", "p", "(x"); });
            Assert.Throws<FormatException>(() => { new ViaHeaderValue("v", "h", "p", "x)"); });
        }

        [Fact]
        public void ToString_UseDifferentRanges_AllSerializedCorrectly()
        {
            ViaHeaderValue via = new ViaHeaderValue("1.1", "host:80");
            Assert.Equal("1.1 host:80", via.ToString());

            via = new ViaHeaderValue("1.1", "[::1]", "HTTP");
            Assert.Equal("HTTP/1.1 [::1]", via.ToString());

            via = new ViaHeaderValue("1.0", "www.example.com", "WS", "(comment)");
            Assert.Equal("WS/1.0 www.example.com (comment)", via.ToString());

            via = new ViaHeaderValue("1.0", "www.example.com:80", null, "(comment)");
            Assert.Equal("1.0 www.example.com:80 (comment)", via.ToString());
        }

        [Fact]
        public void GetHashCode_UseSameAndDifferentRanges_SameOrDifferentHashCodes()
        {
            ViaHeaderValue via1 = new ViaHeaderValue("x11", "host");
            ViaHeaderValue via2 = new ViaHeaderValue("x11", "HOST");
            ViaHeaderValue via3 = new ViaHeaderValue("X11", "host");
            ViaHeaderValue via4 = new ViaHeaderValue("x11", "host", "HTTP");
            ViaHeaderValue via5 = new ViaHeaderValue("x11", "host", "http");
            ViaHeaderValue via6 = new ViaHeaderValue("x11", "host", null, "(comment)");
            ViaHeaderValue via7 = new ViaHeaderValue("x11", "host", "HTTP", "(comment)");
            ViaHeaderValue via8 = new ViaHeaderValue("x11", "host", "HTTP", "(COMMENT)");
            ViaHeaderValue via9 = new ViaHeaderValue("x12", "host");
            ViaHeaderValue via10 = new ViaHeaderValue("x11", "host2");
            ViaHeaderValue via11 = new ViaHeaderValue("x11", "host", "WS");
            ViaHeaderValue via12 = new ViaHeaderValue("x11", "host", string.Empty, string.Empty);

            Assert.Equal(via1.GetHashCode(), via2.GetHashCode());
            Assert.Equal(via1.GetHashCode(), via3.GetHashCode());
            Assert.NotEqual(via1.GetHashCode(), via4.GetHashCode());
            Assert.NotEqual(via1.GetHashCode(), via6.GetHashCode());
            Assert.NotEqual(via1.GetHashCode(), via7.GetHashCode());
            Assert.NotEqual(via1.GetHashCode(), via9.GetHashCode());
            Assert.NotEqual(via1.GetHashCode(), via10.GetHashCode());
            Assert.NotEqual(via4.GetHashCode(), via11.GetHashCode());
            Assert.Equal(via4.GetHashCode(), via5.GetHashCode());
            Assert.NotEqual(via4.GetHashCode(), via6.GetHashCode());
            Assert.NotEqual(via6.GetHashCode(), via7.GetHashCode());
            Assert.NotEqual(via7.GetHashCode(), via8.GetHashCode());
            Assert.Equal(via1.GetHashCode(), via12.GetHashCode());
        }

        [Fact]
        public void Equals_UseSameAndDifferentRanges_EqualOrNotEqualNoExceptions()
        {
            ViaHeaderValue via1 = new ViaHeaderValue("x11", "host");
            ViaHeaderValue via2 = new ViaHeaderValue("x11", "HOST");
            ViaHeaderValue via3 = new ViaHeaderValue("X11", "host");
            ViaHeaderValue via4 = new ViaHeaderValue("x11", "host", "HTTP");
            ViaHeaderValue via5 = new ViaHeaderValue("x11", "host", "http");
            ViaHeaderValue via6 = new ViaHeaderValue("x11", "host", null, "(comment)");
            ViaHeaderValue via7 = new ViaHeaderValue("x11", "host", "HTTP", "(comment)");
            ViaHeaderValue via8 = new ViaHeaderValue("x11", "host", "HTTP", "(COMMENT)");
            ViaHeaderValue via9 = new ViaHeaderValue("x12", "host");
            ViaHeaderValue via10 = new ViaHeaderValue("x11", "host2");
            ViaHeaderValue via11 = new ViaHeaderValue("x11", "host", "WS");
            ViaHeaderValue via12 = new ViaHeaderValue("x11", "host", string.Empty, string.Empty);

            Assert.False(via1.Equals(null), "x11 host vs. <null>");
            Assert.True(via1.Equals(via2), "x11 host vs. x11 HOST");
            Assert.True(via1.Equals(via3), "x11 host vs. X11 host");
            Assert.False(via1.Equals(via4), "x11 host vs. HTTP/x11 host");
            Assert.False(via4.Equals(via1), "HTTP/x11 host vs. x11 host");
            Assert.False(via1.Equals(via6), "x11 host vs. HTTP/x11 (comment)");
            Assert.False(via6.Equals(via1), "HTTP/x11 (comment) vs. x11 host");
            Assert.False(via1.Equals(via7), "x11 host vs. HTTP/x11 host (comment)");
            Assert.False(via7.Equals(via1), "HTTP/x11 host (comment) vs. x11 host");
            Assert.False(via1.Equals(via9), "x11 host vs. x12 host");
            Assert.False(via1.Equals(via10), "x11 host vs. x11 host2");
            Assert.False(via4.Equals(via11), "HTTP/x11 host vs. WS/x11 host");
            Assert.True(via4.Equals(via5), "HTTP/x11 host vs. http/x11 host");
            Assert.False(via4.Equals(via6), "HTTP/x11 host vs. x11 host (comment)");
            Assert.False(via6.Equals(via4), "x11 host (comment) vs. HTTP/x11 host");
            Assert.False(via6.Equals(via7), "x11 host (comment) vs. HTTP/x11 host (comment)");
            Assert.False(via7.Equals(via6), "HTTP/x11 host (comment) vs. x11 host (comment)");
            Assert.False(via7.Equals(via8), "HTTP/x11 host (comment) vs. HTTP/x11 host (COMMENT)");
            Assert.True(via1.Equals(via12), "x11 host vs. x11 host <empty> <empty>");
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            ViaHeaderValue source = new ViaHeaderValue("1.1", "host");
            ViaHeaderValue clone = (ViaHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.ProtocolVersion, clone.ProtocolVersion);
            Assert.Equal(source.ReceivedBy, clone.ReceivedBy);
            Assert.Equal(source.ProtocolName, clone.ProtocolName);
            Assert.Equal(source.Comment, clone.Comment);

            source = new ViaHeaderValue("1.1", "host", "HTTP");
            clone = (ViaHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.ProtocolVersion, clone.ProtocolVersion);
            Assert.Equal(source.ReceivedBy, clone.ReceivedBy);
            Assert.Equal(source.ProtocolName, clone.ProtocolName);
            Assert.Equal(source.Comment, clone.Comment);

            source = new ViaHeaderValue("1.1", "host", "HTTP", "(comment)");
            clone = (ViaHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.ProtocolVersion, clone.ProtocolVersion);
            Assert.Equal(source.ReceivedBy, clone.ReceivedBy);
            Assert.Equal(source.ProtocolName, clone.ProtocolName);
            Assert.Equal(source.Comment, clone.Comment);
        }

        [Fact]
        public void GetViaLength_DifferentValidScenarios_AllReturnNonZero()
        {
            CheckGetViaLength(" HTTP  /  1.1   .host \t (comment)  ", 1, 34,
                new ViaHeaderValue("1.1", ".host", "HTTP", "(comment)"));
            CheckGetViaLength("x11x [FE18:AB64::156]:80 (comment,) other", 0, 36,
                new ViaHeaderValue("x11x", "[FE18:AB64::156]:80", null, "(comment,)"));

            // The parser reads until it reaches an invalid/unexpected character. If until then it was able to create
            // a valid ViaHeaderValue, it will return the length of the parsed string. Therefore a string like
            // "1.1 host," is considered valid (until ','), whereas "1.1 host (invalid" is considered invalid, since
            // the comment is in an invalid format.
            CheckGetViaLength("WS/version example.com,next", 0, 22, new ViaHeaderValue("version", "example.com", "WS"));

            // Note that since 'HTTP1.1' is a valid token, it is considered to be the protocol version.
            CheckGetViaLength("HTTP1.1 host", 0, 12, new ViaHeaderValue("HTTP1.1", "host"));

            CheckGetViaLength(" v h", 1, 3, new ViaHeaderValue("v", "h"));
            CheckGetViaLength(" p/v h", 1, 5, new ViaHeaderValue("v", "h", "p"));
            CheckGetViaLength(" p/v h (c)", 1, 9, new ViaHeaderValue("v", "h", "p", "(c)"));
            CheckGetViaLength(" v h,,", 1, 3, new ViaHeaderValue("v", "h"));
            CheckGetViaLength(" p/v h,,", 1, 5, new ViaHeaderValue("v", "h", "p"));
            CheckGetViaLength(" p/v h (c),,", 1, 9, new ViaHeaderValue("v", "h", "p", "(c)"));
            CheckGetViaLength(" v h ", 1, 4, new ViaHeaderValue("v", "h"));
            CheckGetViaLength(" p/v h ", 1, 6, new ViaHeaderValue("v", "h", "p"));
            CheckGetViaLength(" p/v h (c) ", 1, 10, new ViaHeaderValue("v", "h", "p", "(c)"));

            CheckGetViaLength(null, 0, 0, null);
            CheckGetViaLength(string.Empty, 0, 0, null);
            CheckGetViaLength("  ", 0, 0, null);
        }

        [Fact]
        public void GetViaLength_DifferentInvalidScenarios_AllReturnZero()
        {
            CheckInvalidGetViaLength(" 1.1 host", 0); // no leading whitespace allowed
            CheckInvalidGetViaLength("1=host", 0);
            CheckInvalidGetViaLength("1.1 host (invalid_comment", 0);
            CheckInvalidGetViaLength("=", 0);
            CheckInvalidGetViaLength("HTTP=1.1 host", 0);
            CheckInvalidGetViaLength("HTTP=/1.1 host", 0);
            CheckInvalidGetViaLength("HTTP/1.1[ host", 0);
            CheckInvalidGetViaLength("HTTP/ = host", 0);
            CheckInvalidGetViaLength("HTTP/\u52A1 host", 0);
            CheckInvalidGetViaLength("HTTP/  ", 0);
            CheckInvalidGetViaLength("HTTP  ", 0);
            CheckInvalidGetViaLength("HTTP/1.1 /  ", 0);
            CheckInvalidGetViaLength("1.1 ", 0);
            CheckInvalidGetViaLength("  ", 0);
            CheckInvalidGetViaLength("WS/version example.com[", 0);
            CheckInvalidGetViaLength("HTTP/test[::1]", 0);
            CheckInvalidGetViaLength("HTTP/test [::1]:80(comment)", 0);
            CheckInvalidGetViaLength("1.1 http://example.com", 0);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParse(" 1.1   host ", new ViaHeaderValue("1.1", "host"));
            CheckValidParse(" HTTP  /  x11   192.168.0.1 (comment) ",
                new ViaHeaderValue("x11", "192.168.0.1", "HTTP", "(comment)"));
            CheckValidParse(" HTTP/1.1 [::1]", new ViaHeaderValue("1.1", "[::1]", "HTTP"));
            CheckValidParse("1.1 host", new ViaHeaderValue("1.1", "host"));
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("HTTP/1.1 host (comment)invalid");
            CheckInvalidParse("HTTP/1.1 host (comment)=");
            CheckInvalidParse("HTTP/1.1 host (comment) invalid");
            CheckInvalidParse("HTTP/1.1 host (comment) =");
            CheckInvalidParse("HTTP/1.1 host invalid");
            CheckInvalidParse("HTTP/1.1 host =");
            CheckInvalidParse("1.1 host invalid");
            CheckInvalidParse("1.1 host =");
            CheckInvalidParse("\u4F1A");
            CheckInvalidParse("HTTP/test [::1]:80\r(comment)");
            CheckInvalidParse("HTTP/test [::1]:80\n(comment)");

            CheckInvalidParse("X , , 1.1   host, ,next");
            CheckInvalidParse("X HTTP  /  x11   192.168.0.1\r\n (comment) , ,next");
            CheckInvalidParse(" ,HTTP/1.1 [::1]");

            CheckInvalidParse(null);
            CheckInvalidParse(string.Empty);
            CheckInvalidParse("  ");
            CheckInvalidParse("  ,,");
            CheckInvalidParse("1=host\n");
        }

        #region Helper methods

        private void CheckValidParse(string input, ViaHeaderValue expectedResult)
        {
            ViaHeaderValue result = ViaHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);

            Assert.True(ViaHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => { ViaHeaderValue.Parse(input); });

            Assert.False(ViaHeaderValue.TryParse(input, out ViaHeaderValue result));
            Assert.Null(result);
        }

        private static void CheckGetViaLength(string input, int startIndex, int expectedLength,
            ViaHeaderValue expectedResult)
        {
            object result = null;
            Assert.Equal(expectedLength, ViaHeaderValue.GetViaLength(input, startIndex, out result));
            Assert.Equal(expectedResult, result);
        }

        private static void CheckInvalidGetViaLength(string input, int startIndex)
        {
            object result = null;
            Assert.Equal(0, ViaHeaderValue.GetViaLength(input, startIndex, out result));
            Assert.Null(result);
        }
        #endregion
    }
}
