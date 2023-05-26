// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class AuthenticationHeaderValueTest
    {
        [Fact]
        public void Ctor_SetBothSchemeAndParameters_MatchExpectation()
        {
            AuthenticationHeaderValue auth = new AuthenticationHeaderValue("Basic", "realm=\"contoso.com\"");
            Assert.Equal("Basic", auth.Scheme);
            Assert.Equal("realm=\"contoso.com\"", auth.Parameter);

            AssertExtensions.Throws<ArgumentNullException>("scheme", () => { new AuthenticationHeaderValue(null, "x"); });
            AssertExtensions.Throws<ArgumentException>("scheme", () => { new AuthenticationHeaderValue("", "x"); });
            Assert.Throws<FormatException>(() => { new AuthenticationHeaderValue(" x", "x"); });
            Assert.Throws<FormatException>(() => { new AuthenticationHeaderValue("x ", "x"); });
            Assert.Throws<FormatException>(() => { new AuthenticationHeaderValue("x y", "x"); });
        }

        [Fact]
        public void Ctor_SetSchemeOnly_MatchExpectation()
        {
            // Just verify that this ctor forwards the call to the overload taking 2 parameters.
            AuthenticationHeaderValue auth = new AuthenticationHeaderValue("NTLM");
            Assert.Equal("NTLM", auth.Scheme);
            Assert.Null(auth.Parameter);
        }

        [Fact]
        public void ToString_UseBothNoParameterAndSetParameter_AllSerializedCorrectly()
        {
            using (HttpResponseMessage response = new HttpResponseMessage())
            {
                string input = string.Empty;

                AuthenticationHeaderValue auth = new AuthenticationHeaderValue("Digest",
                    "qop=\"auth\",algorithm=MD5-sess,nonce=\"+Upgraded+v109e309640b\",charset=utf-8,realm=\"Digest\"");

                Assert.Equal(
                    "Digest qop=\"auth\",algorithm=MD5-sess,nonce=\"+Upgraded+v109e309640b\",charset=utf-8,realm=\"Digest\"",
                    auth.ToString());
                response.Headers.ProxyAuthenticate.Add(auth);
                input += auth.ToString();

                auth = new AuthenticationHeaderValue("Negotiate");
                Assert.Equal("Negotiate", auth.ToString());
                response.Headers.ProxyAuthenticate.Add(auth);
                input += ", " + auth.ToString();

                auth = new AuthenticationHeaderValue("Custom", ""); // empty string should be treated like 'null'.
                Assert.Equal("Custom", auth.ToString());
                response.Headers.ProxyAuthenticate.Add(auth);
                input += ", " + auth.ToString();

                string result = response.Headers.ProxyAuthenticate.ToString();
                Assert.Equal(input, result);
            }
        }

        [Fact]
        public void Parse_GoodValues_Success()
        {
            HttpRequestMessage request = new HttpRequestMessage();

            string input = " Digest qop=\"auth\",algorithm=MD5-sess,nonce=\"+Upgraded+v109e309640b\",charset=utf-8 ";

            request.Headers.Authorization = AuthenticationHeaderValue.Parse(input);
            Assert.Equal(input.Trim(), request.Headers.Authorization.ToString());
        }

        [Fact]
        public void TryParse_GoodValues_Success()
        {
            HttpRequestMessage request = new HttpRequestMessage();

            string input = " Digest qop=\"auth\",algorithm=MD5-sess,nonce=\"+Upgraded+v109e309640b\",realm=\"Digest\" ";

            AuthenticationHeaderValue parsedValue;
            Assert.True(AuthenticationHeaderValue.TryParse(input, out parsedValue));
            request.Headers.Authorization = parsedValue;
            Assert.Equal(input.Trim(), request.Headers.Authorization.ToString());
        }

        [Fact]
        public void Parse_BadValues_Throws()
        {
            string input = "D\rigest qop=\"auth\",algorithm=MD5-sess,charset=utf-8,realm=\"Digest\"";

            Assert.Throws<FormatException>(() => { AuthenticationHeaderValue.Parse(input); });
        }

        [Fact]
        public void TryParse_BadValues_False()
        {
            string input = ", Digest qop=\"auth\",nonce=\"+Upgraded+v109e309640b\",charset=utf-8,realm=\"Digest\"";

            AuthenticationHeaderValue parsedValue;
            Assert.False(AuthenticationHeaderValue.TryParse(input, out parsedValue));
        }

        [Fact]
        public void Add_BadValues_Throws()
        {
            string x = SR.net_http_message_not_success_statuscode_reason;
            string input = "Digest algorithm=MD5-sess,nonce=\"+Upgraded+v109e309640b\",charset=utf-8,realm=\"Digest\", ";

            HttpRequestMessage request = new HttpRequestMessage();
            Assert.Throws<FormatException>(() => { request.Headers.Add(HttpKnownHeaderNames.Authorization, input); });
        }

        [Fact]
        public void GetHashCode_UseSameAndDifferentAuth_SameOrDifferentHashCodes()
        {
            AuthenticationHeaderValue auth1 = new AuthenticationHeaderValue("A", "b");
            AuthenticationHeaderValue auth2 = new AuthenticationHeaderValue("a", "b");
            AuthenticationHeaderValue auth3 = new AuthenticationHeaderValue("A", "B");
            AuthenticationHeaderValue auth4 = new AuthenticationHeaderValue("A");
            AuthenticationHeaderValue auth5 = new AuthenticationHeaderValue("A", "");
            AuthenticationHeaderValue auth6 = new AuthenticationHeaderValue("X", "b");

            Assert.Equal(auth1.GetHashCode(), auth2.GetHashCode());
            Assert.NotEqual(auth1.GetHashCode(), auth3.GetHashCode());
            Assert.NotEqual(auth1.GetHashCode(), auth4.GetHashCode());
            Assert.Equal(auth4.GetHashCode(), auth5.GetHashCode());
            Assert.NotEqual(auth1.GetHashCode(), auth6.GetHashCode());
        }

        [Fact]
        public void Equals_UseSameAndDifferentAuth_EqualOrNotEqualNoExceptions()
        {
            AuthenticationHeaderValue auth1 = new AuthenticationHeaderValue("A", "b");
            AuthenticationHeaderValue auth2 = new AuthenticationHeaderValue("a", "b");
            AuthenticationHeaderValue auth3 = new AuthenticationHeaderValue("A", "B");
            AuthenticationHeaderValue auth4 = new AuthenticationHeaderValue("A");
            AuthenticationHeaderValue auth5 = new AuthenticationHeaderValue("A", "");
            AuthenticationHeaderValue auth6 = new AuthenticationHeaderValue("X", "b");

            Assert.False(auth1.Equals(null));
            Assert.True(auth1.Equals(auth2));
            Assert.False(auth1.Equals(auth3));
            Assert.False(auth1.Equals(auth4));
            Assert.False(auth4.Equals(auth1));
            Assert.False(auth1.Equals(auth5));
            Assert.False(auth5.Equals(auth1));
            Assert.True(auth4.Equals(auth5));
            Assert.True(auth5.Equals(auth4));
            Assert.False(auth1.Equals(auth6));
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            AuthenticationHeaderValue source = new AuthenticationHeaderValue("Basic", "QWxhZGRpbjpvcGVuIHNlc2FtZQ==");
            AuthenticationHeaderValue clone = (AuthenticationHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.Scheme, clone.Scheme);
            Assert.Equal(source.Parameter, clone.Parameter);

            source = new AuthenticationHeaderValue("Kerberos");
            clone = (AuthenticationHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.Scheme, clone.Scheme);
            Assert.Null(clone.Parameter);
        }

        [Fact]
        public void GetAuthenticationLength_DifferentValidScenarios_AllReturnNonZero()
        {
            CallGetAuthenticationLength(" Basic  QWxhZGRpbjpvcGVuIHNlc2FtZQ==  ", 1, 37,
                new AuthenticationHeaderValue("Basic", "QWxhZGRpbjpvcGVuIHNlc2FtZQ=="));
            CallGetAuthenticationLength(" Basic  QWxhZGRpbjpvcGVuIHNlc2FtZQ==  , ", 1, 37,
                new AuthenticationHeaderValue("Basic", "QWxhZGRpbjpvcGVuIHNlc2FtZQ=="));
            CallGetAuthenticationLength(" Basic realm=\"example.com\"", 1, 25,
                new AuthenticationHeaderValue("Basic", "realm=\"example.com\""));
            CallGetAuthenticationLength(" Basic realm=\"exam,,ple.com\",", 1, 27,
                new AuthenticationHeaderValue("Basic", "realm=\"exam,,ple.com\""));
            CallGetAuthenticationLength(" Basic realm=\"exam,ple.com\",", 1, 26,
                new AuthenticationHeaderValue("Basic", "realm=\"exam,ple.com\""));
            CallGetAuthenticationLength("NTLM   ", 0, 7, new AuthenticationHeaderValue("NTLM"));
            CallGetAuthenticationLength("Digest", 0, 6, new AuthenticationHeaderValue("Digest"));
            CallGetAuthenticationLength("Digest,,", 0, 6, new AuthenticationHeaderValue("Digest"));
            CallGetAuthenticationLength("Digest a=b, c=d,,", 0, 15, new AuthenticationHeaderValue("Digest", "a=b, c=d"));
            CallGetAuthenticationLength("Kerberos,", 0, 8, new AuthenticationHeaderValue("Kerberos"));
            CallGetAuthenticationLength("Basic,NTLM", 0, 5, new AuthenticationHeaderValue("Basic"));
            CallGetAuthenticationLength("Digest a=b,c=\"d\", e=f, NTLM", 0, 21,
                new AuthenticationHeaderValue("Digest", "a=b,c=\"d\", e=f"));
            CallGetAuthenticationLength("Digest a = b , c = \"d\" ,  e = f ,NTLM", 0, 32,
                new AuthenticationHeaderValue("Digest", "a = b , c = \"d\" ,  e = f"));
            CallGetAuthenticationLength("Digest a = b , c = \"d\" ,  e = f , NTLM AbCdEf==", 0, 32,
                new AuthenticationHeaderValue("Digest", "a = b , c = \"d\" ,  e = f"));
            CallGetAuthenticationLength("Digest a = \"b\", c= \"d\" ,  e = f,NTLM AbC=,", 0, 31,
                new AuthenticationHeaderValue("Digest", "a = \"b\", c= \"d\" ,  e = f"));
            CallGetAuthenticationLength("Digest a=\"b\", c=d", 0, 17,
                new AuthenticationHeaderValue("Digest", "a=\"b\", c=d"));
            CallGetAuthenticationLength("Digest a=\"b\", c=d,", 0, 17,
                new AuthenticationHeaderValue("Digest", "a=\"b\", c=d"));
            CallGetAuthenticationLength("Digest a=\"b\", c=d ,", 0, 18,
                new AuthenticationHeaderValue("Digest", "a=\"b\", c=d"));
            CallGetAuthenticationLength("Digest a=\"b\", c=d  ", 0, 19,
                new AuthenticationHeaderValue("Digest", "a=\"b\", c=d"));
            CallGetAuthenticationLength("Custom \"blob\", c=d,Custom2 \"blob\"", 0, 18,
                new AuthenticationHeaderValue("Custom", "\"blob\", c=d"));
            CallGetAuthenticationLength("Custom \"blob\", a=b,,,c=d,Custom2 \"blob\"", 0, 24,
                new AuthenticationHeaderValue("Custom", "\"blob\", a=b,,,c=d"));
            CallGetAuthenticationLength("Custom \"blob\", a=b,c=d,,,Custom2 \"blob\"", 0, 22,
                new AuthenticationHeaderValue("Custom", "\"blob\", a=b,c=d"));
            CallGetAuthenticationLength("Custom a=b, c=d,,,InvalidNextScheme\u670D", 0, 15,
                new AuthenticationHeaderValue("Custom", "a=b, c=d"));
        }

        [Fact]
        public void GetAuthenticationLength_DifferentInvalidScenarios_AllReturnZero()
        {
            CheckInvalidGetAuthenticationLength(" NTLM", 0); // no leading whitespace allowed
            CheckInvalidGetAuthenticationLength("Basic=", 0);
            CheckInvalidGetAuthenticationLength("=Basic", 0);
            CheckInvalidGetAuthenticationLength("Digest a=b, \u670D", 0);
            CheckInvalidGetAuthenticationLength("Digest a=b, c=d, \u670D", 0);
            CheckInvalidGetAuthenticationLength("Digest a=b, c=", 0);
            CheckInvalidGetAuthenticationLength("Digest a=\"b, c", 0);
            CheckInvalidGetAuthenticationLength("Digest a=\"b", 0);
            CheckInvalidGetAuthenticationLength("Digest a=b, c=\u670D", 0);

            CheckInvalidGetAuthenticationLength("", 0);
            CheckInvalidGetAuthenticationLength(null, 0);
        }

        #region Helper methods

        private static void CallGetAuthenticationLength(string input, int startIndex, int expectedLength,
            AuthenticationHeaderValue expectedResult)
        {
            object result = null;
            Assert.Equal(expectedLength, AuthenticationHeaderValue.GetAuthenticationLength(input, startIndex, out result));
            Assert.Equal(expectedResult, result);
        }

        private static void CheckInvalidGetAuthenticationLength(string input, int startIndex)
        {
            object result = null;
            Assert.Equal(0, AuthenticationHeaderValue.GetAuthenticationLength(input, startIndex, out result));
            Assert.Null(result);
        }
        #endregion
    }
}
