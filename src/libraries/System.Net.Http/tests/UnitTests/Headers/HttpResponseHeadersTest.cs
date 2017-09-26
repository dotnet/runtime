// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class HttpResponseHeadersTest
    {
        private HttpResponseHeaders headers;

        public HttpResponseHeadersTest()
        {
            headers = new HttpResponseHeaders();
        }

        #region Response headers
        [Fact]
        public void Location_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Null(headers.Location);

            Uri expected = new Uri("http://example.com/path/");
            headers.Location = expected;
            Assert.Equal(expected, headers.Location);

            headers.Location = null;
            Assert.Null(headers.Location);
            Assert.False(headers.Contains("Location"),
                "Header store should not contain a header 'Location' after setting it to null.");
        }

        [Fact]
        public void Location_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            // just verify header names are compared using case-insensitive comparison.
            headers.TryAddWithoutValidation("LoCaTiOn", "  http://www.example.com/path/?q=v  ");
            Assert.Equal(new Uri("http://www.example.com/path/?q=v"), headers.Location);

            headers.Clear();
            headers.TryAddWithoutValidation("Location", "http://host");
            Assert.Equal(new Uri("http://host"), headers.Location);

            // This violates the RFCs, the Location header should be absolute.  However,
            // IIS and HttpListener do not enforce this requirement.
            headers.Clear();
            headers.Add("LoCaTiOn", "/relative/");
            Assert.Equal<Uri>(new Uri("/relative/", UriKind.Relative), headers.Location);
        }

        [Fact]
        public void Location_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Location", " http://example.com http://other");
            Assert.Null(headers.GetParsedValues(KnownHeaders.Location.Descriptor));
            Assert.Equal(1, headers.GetValues("Location").Count());
            Assert.Equal(" http://example.com http://other", headers.GetValues("Location").First());
        }

        [Fact]
        public void Location_RequiresEncoding_Encoded()
        {
            // Absolute
            headers.Location = new Uri("http://www.example\u30AF.com/%25path\u30AF/?q=v\u30AF");
            IEnumerable<string> values = headers.GetValues("Location");
            string[] strings = values.ToArray<string>();
            Assert.Equal(1, strings.Length);
            Assert.Equal("http://www.example\u30AF.com/%25path%E3%82%AF/?q=v%E3%82%AF", strings[0]);

            headers.Clear();

            // Relative
            headers.Location = new Uri("%25path\u30AF/?q=v\u30AF", UriKind.Relative);
            values = headers.GetValues("Location");
            strings = values.ToArray<string>();
            Assert.Equal(1, strings.Length);
            Assert.Equal("%25path%E3%82%AF/?q=v%E3%82%AF", strings[0]);
        }

        [Fact]
        public void ETag_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Null(headers.ETag);

            EntityTagHeaderValue etag = new EntityTagHeaderValue("\"tag\"", true);
            headers.ETag = etag;
            Assert.Same(etag, headers.ETag);

            headers.ETag = null;
            Assert.Null(headers.ETag);
            Assert.False(headers.Contains("ETag"),
                "Header store should not contain a header 'ETag' after setting it to null.");
        }

        [Fact]
        public void ETag_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("ETag", "W/\"tag\"");
            Assert.Equal(new EntityTagHeaderValue("\"tag\"", true), headers.ETag);
        }

        [Fact]
        public void ETag_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("ETag", ",\"tag\""); // leading separator
            Assert.Null(headers.ETag);
            Assert.Equal(1, headers.GetValues("ETag").Count());
            Assert.Equal(",\"tag\"", headers.GetValues("ETag").First());

            headers.Clear();
            headers.TryAddWithoutValidation("ETag", "\"tag\","); // trailing separator
            Assert.Null(headers.ETag);
            Assert.Equal(1, headers.GetValues("ETag").Count());
            Assert.Equal("\"tag\",", headers.GetValues("ETag").First());
        }

        [Fact]
        public void AcceptRanges_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Equal(0, headers.AcceptRanges.Count);

            headers.AcceptRanges.Add("custom1");
            headers.AcceptRanges.Add("custom2");

            Assert.Equal(2, headers.AcceptRanges.Count);
            Assert.Equal(2, headers.GetValues("Accept-Ranges").Count());

            Assert.Equal("custom1", headers.AcceptRanges.ElementAt(0));
            Assert.Equal("custom2", headers.AcceptRanges.ElementAt(1));

            headers.AcceptRanges.Clear();
            Assert.Equal(0, headers.AcceptRanges.Count);
            Assert.False(headers.Contains("Accept-Ranges"),
                "There should be no Accept-Ranges header after calling Clear().");
        }

        [Fact]
        public void AcceptRanges_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Accept-Ranges", ",custom1, custom2, custom3,");

            Assert.Equal(3, headers.AcceptRanges.Count);
            Assert.Equal(3, headers.GetValues("Accept-Ranges").Count());

            Assert.Equal("custom1", headers.AcceptRanges.ElementAt(0));
            Assert.Equal("custom2", headers.AcceptRanges.ElementAt(1));
            Assert.Equal("custom3", headers.AcceptRanges.ElementAt(2));

            headers.AcceptRanges.Clear();
            Assert.Equal(0, headers.AcceptRanges.Count);
            Assert.False(headers.Contains("Accept-Ranges"),
                "There should be no Accept-Ranges header after calling Clear().");
        }

        [Fact]
        public void AcceptRanges_AddInvalidValue_Throw()
        {
            Assert.Throws<FormatException>(() => { headers.AcceptRanges.Add("this is invalid"); });
        }

        [Fact]
        public void AcceptRanges_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Accept-Ranges", "custom1 custom2"); // no separator

            Assert.Equal(0, headers.AcceptRanges.Count);
            Assert.Equal(1, headers.GetValues("Accept-Ranges").Count());
            Assert.Equal("custom1 custom2", headers.GetValues("Accept-Ranges").First());
        }

        [Fact]
        public void WwwAuthenticate_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Equal(0, headers.WwwAuthenticate.Count);

            headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("NTLM"));
            headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Basic", "realm=\"contoso.com\""));

            Assert.Equal(2, headers.WwwAuthenticate.Count);
            Assert.Equal(2, headers.GetValues("WWW-Authenticate").Count());

            Assert.Equal(new AuthenticationHeaderValue("NTLM"),
                headers.WwwAuthenticate.ElementAt(0));
            Assert.Equal(new AuthenticationHeaderValue("Basic", "realm=\"contoso.com\""),
                headers.WwwAuthenticate.ElementAt(1));

            headers.WwwAuthenticate.Clear();
            Assert.Equal(0, headers.WwwAuthenticate.Count);
            Assert.False(headers.Contains("WWW-Authenticate"),
                "There should be no WWW-Authenticate header after calling Clear().");
        }

        [Fact]
        public void WwwAuthenticate_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.Add("WWW-Authenticate", "Negotiate");
            headers.TryAddWithoutValidation("WWW-Authenticate", "Basic realm=\"contoso.com\", Digest a=b, c=d, NTLM");
            headers.TryAddWithoutValidation("WWW-Authenticate", "Kerberos");

            Assert.Equal(5, headers.WwwAuthenticate.Count);
            Assert.Equal(5, headers.GetValues("WWW-Authenticate").Count());

            Assert.Equal(new AuthenticationHeaderValue("Negotiate"),
                headers.WwwAuthenticate.ElementAt(0));
            Assert.Equal(new AuthenticationHeaderValue("Basic", "realm=\"contoso.com\""),
                headers.WwwAuthenticate.ElementAt(1));
            Assert.Equal(new AuthenticationHeaderValue("Digest", "a=b, c=d"),
                headers.WwwAuthenticate.ElementAt(2));
            Assert.Equal(new AuthenticationHeaderValue("NTLM"),
                headers.WwwAuthenticate.ElementAt(3));
            Assert.Equal(new AuthenticationHeaderValue("Kerberos"),
                headers.WwwAuthenticate.ElementAt(4));

            headers.WwwAuthenticate.Clear();
            Assert.Equal(0, headers.WwwAuthenticate.Count);
            Assert.False(headers.Contains("WWW-Authenticate"),
                "There should be no WWW-Authenticate header after calling Clear().");
        }

        [Fact]
        public void ProxyAuthenticate_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Equal(0, headers.ProxyAuthenticate.Count);

            headers.ProxyAuthenticate.Add(new AuthenticationHeaderValue("NTLM"));
            headers.ProxyAuthenticate.Add(new AuthenticationHeaderValue("Basic", "realm=\"contoso.com\""));

            Assert.Equal(2, headers.ProxyAuthenticate.Count);
            Assert.Equal(2, headers.GetValues("Proxy-Authenticate").Count());

            Assert.Equal(new AuthenticationHeaderValue("NTLM"),
                headers.ProxyAuthenticate.ElementAt(0));
            Assert.Equal(new AuthenticationHeaderValue("Basic", "realm=\"contoso.com\""),
                headers.ProxyAuthenticate.ElementAt(1));

            headers.ProxyAuthenticate.Clear();
            Assert.Equal(0, headers.ProxyAuthenticate.Count);
            Assert.False(headers.Contains("Proxy-Authenticate"),
                "There should be no Proxy-Authenticate header after calling Clear().");
        }

        [Fact]
        public void ProxyAuthenticate_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.Add("Proxy-Authenticate", "Negotiate");
            headers.TryAddWithoutValidation("Proxy-Authenticate", "Basic realm=\"contoso.com\"");
            headers.TryAddWithoutValidation("Proxy-Authenticate", "NTLM");

            Assert.Equal(3, headers.ProxyAuthenticate.Count);
            Assert.Equal(3, headers.GetValues("Proxy-Authenticate").Count());

            Assert.Equal(new AuthenticationHeaderValue("Negotiate"),
                headers.ProxyAuthenticate.ElementAt(0));
            Assert.Equal(new AuthenticationHeaderValue("Basic", "realm=\"contoso.com\""),
                headers.ProxyAuthenticate.ElementAt(1));
            Assert.Equal(new AuthenticationHeaderValue("NTLM"),
                headers.ProxyAuthenticate.ElementAt(2));

            headers.ProxyAuthenticate.Clear();
            Assert.Equal(0, headers.ProxyAuthenticate.Count);
            Assert.False(headers.Contains("Proxy-Authenticate"),
                "There should be no Proxy-Authenticate header after calling Clear().");
        }

        [Fact]
        public void Server_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Equal(0, headers.Server.Count);

            headers.Server.Add(new ProductInfoHeaderValue("(custom1)"));
            headers.Server.Add(new ProductInfoHeaderValue("custom2", "1.1"));

            Assert.Equal(2, headers.Server.Count);
            Assert.Equal(2, headers.GetValues("Server").Count());
            Assert.Equal(new ProductInfoHeaderValue("(custom1)"), headers.Server.ElementAt(0));
            Assert.Equal(new ProductInfoHeaderValue("custom2", "1.1"), headers.Server.ElementAt(1));

            headers.Server.Clear();
            Assert.Equal(0, headers.Server.Count);
            Assert.False(headers.Contains("Server"), "Server header should be removed after calling Clear().");

            headers.Server.Add(new ProductInfoHeaderValue("(comment)"));
            headers.Server.Remove(new ProductInfoHeaderValue("(comment)"));
            Assert.Equal(0, headers.Server.Count);
            Assert.False(headers.Contains("Server"), "Server header should be removed after removing last value.");
        }

        [Fact]
        public void Server_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Server", "CERN/3.0 libwww/2.17 (mycomment)");

            Assert.Equal(3, headers.Server.Count);
            Assert.Equal(3, headers.GetValues("Server").Count());

            Assert.Equal(new ProductInfoHeaderValue("CERN", "3.0"), headers.Server.ElementAt(0));
            Assert.Equal(new ProductInfoHeaderValue("libwww", "2.17"), headers.Server.ElementAt(1));
            Assert.Equal(new ProductInfoHeaderValue("(mycomment)"), headers.Server.ElementAt(2));

            headers.Server.Clear();
            Assert.Equal(0, headers.Server.Count);
            Assert.False(headers.Contains("Server"), "Server header should be removed after calling Clear().");
        }

        [Fact]
        public void Server_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Server", "custom会");
            Assert.Null(headers.GetParsedValues(KnownHeaders.Server.Descriptor));
            Assert.Equal(1, headers.GetValues("Server").Count());
            Assert.Equal("custom会", headers.GetValues("Server").First());

            headers.Clear();
            // Note that "Server" uses whitespace as separators, so the following is an invalid value
            headers.TryAddWithoutValidation("Server", "custom1, custom2");
            Assert.Null(headers.GetParsedValues(KnownHeaders.Server.Descriptor));
            Assert.Equal(1, headers.GetValues("Server").Count());
            Assert.Equal("custom1, custom2", headers.GetValues("Server").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Server", "custom1, ");
            Assert.Null(headers.GetParsedValues(KnownHeaders.Server.Descriptor));
            Assert.Equal(1, headers.GetValues("Server").Count());
            Assert.Equal("custom1, ", headers.GetValues("Server").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Server", ",custom1");
            Assert.Null(headers.GetParsedValues(KnownHeaders.Server.Descriptor));
            Assert.Equal(1, headers.GetValues("Server").Count());
            Assert.Equal(",custom1", headers.GetValues("Server").First());
        }

        [Fact]
        public void RetryAfter_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Null(headers.RetryAfter);

            RetryConditionHeaderValue retry = new RetryConditionHeaderValue(new TimeSpan(0, 1, 10));
            headers.RetryAfter = retry;
            Assert.Same(retry, headers.RetryAfter);

            headers.RetryAfter = null;
            Assert.Null(headers.RetryAfter);
            Assert.False(headers.Contains("RetryAfter"),
                "Header store should not contain a header 'ETag' after setting it to null.");
        }

        [Fact]
        public void RetryAfter_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Retry-After", " 2100000 ");
            Assert.Equal(new RetryConditionHeaderValue(new TimeSpan(0, 0, 2100000)), headers.RetryAfter);
        }

        [Fact]
        public void RetryAfter_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Retry-After", "123,"); // trailing separator
            Assert.Null(headers.RetryAfter);
            Assert.Equal(1, headers.GetValues("Retry-After").Count());
            Assert.Equal("123,", headers.GetValues("Retry-After").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Retry-After", ",Sun, 06 Nov 1994 08:49:37 GMT"); // leading separator
            Assert.Null(headers.RetryAfter);
            Assert.Equal(1, headers.GetValues("Retry-After").Count());
            Assert.Equal(",Sun, 06 Nov 1994 08:49:37 GMT", headers.GetValues("Retry-After").First());
        }

        [Fact]
        public void Vary_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Equal(0, headers.Vary.Count);

            headers.Vary.Add("custom1");
            headers.Vary.Add("custom2");

            Assert.Equal(2, headers.Vary.Count);
            Assert.Equal(2, headers.GetValues("Vary").Count());

            Assert.Equal("custom1", headers.Vary.ElementAt(0));
            Assert.Equal("custom2", headers.Vary.ElementAt(1));

            headers.Vary.Clear();
            Assert.Equal(0, headers.Vary.Count);
            Assert.False(headers.Contains("Vary"),
                "There should be no Vary header after calling Clear().");
        }

        [Fact]
        public void Vary_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Vary", ",custom1, custom2, custom3,");

            Assert.Equal(3, headers.Vary.Count);
            Assert.Equal(3, headers.GetValues("Vary").Count());

            Assert.Equal("custom1", headers.Vary.ElementAt(0));
            Assert.Equal("custom2", headers.Vary.ElementAt(1));
            Assert.Equal("custom3", headers.Vary.ElementAt(2));

            headers.Vary.Clear();
            Assert.Equal(0, headers.Vary.Count);
            Assert.False(headers.Contains("Vary"),
                "There should be no Vary header after calling Clear().");
        }

        [Fact]
        public void Vary_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Vary", "custom1 custom2"); // no separator

            Assert.Equal(0, headers.Vary.Count);
            Assert.Equal(1, headers.GetValues("Vary").Count());
            Assert.Equal("custom1 custom2", headers.GetValues("Vary").First());
        }

        [Fact]
        public void Age_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Null(headers.Age);

            TimeSpan expected = new TimeSpan(0, 1, 2);
            headers.Age = expected;
            Assert.Equal(expected, headers.Age);

            headers.Age = null;
            Assert.Null(headers.Age);
            Assert.False(headers.Contains("Age"),
                "Header store should not contain a header 'Age' after setting it to null.");

            // Make sure the header gets serialized correctly
            headers.Age = new TimeSpan(0, 1, 2);
            Assert.Equal("62", headers.GetValues("Age").First());
        }

        [Fact]
        public void Age_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Age", "  15  ");
            Assert.Equal(new TimeSpan(0, 0, 15), headers.Age);

            headers.Clear();
            headers.TryAddWithoutValidation("Age", "0");
            Assert.Equal(new TimeSpan(0, 0, 0), headers.Age);
        }

        [Fact]
        public void Age_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Age", "10,");
            Assert.Null(headers.GetParsedValues(KnownHeaders.Age.Descriptor));
            Assert.Equal(1, headers.GetValues("Age").Count());
            Assert.Equal("10,", headers.GetValues("Age").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Age", "1.1");
            Assert.Null(headers.GetParsedValues(KnownHeaders.Age.Descriptor));
            Assert.Equal(1, headers.GetValues("Age").Count());
            Assert.Equal("1.1", headers.GetValues("Age").First());
        }

        #endregion

        // General headers are tested in more detail in HttpRequestHeadersTest. This file only makes sure
        // HttpResponseHeaders correctly forwards calls to HttpGeneralHeaders.
        #region General headers

        [Fact]
        public void Connection_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Equal(0, headers.Connection.Count);
            Assert.Null(headers.ConnectionClose);

            headers.Connection.Add("custom1");
            headers.ConnectionClose = true;

            // Connection collection has 1 values plus 'close'
            Assert.Equal(2, headers.Connection.Count);
            Assert.Equal(2, headers.GetValues("Connection").Count());
            Assert.True(headers.ConnectionClose == true, "ConnectionClose");

            headers.TryAddWithoutValidation("Connection", "custom2");
            Assert.Equal(3, headers.Connection.Count);
            Assert.Equal(3, headers.GetValues("Connection").Count());
        }

        [Fact]
        public void TransferEncoding_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Equal(0, headers.TransferEncoding.Count);
            Assert.Null(headers.TransferEncodingChunked);

            headers.TransferEncoding.Add(new TransferCodingHeaderValue("custom1"));
            headers.TransferEncodingChunked = true;

            // Connection collection has 1 value plus 'chunked'
            Assert.Equal(2, headers.TransferEncoding.Count);
            Assert.Equal(2, headers.GetValues("Transfer-Encoding").Count());
            Assert.Equal(true, headers.TransferEncodingChunked);

            // Note that 'chunked' is already in the collection, we add 'chunked' again here. Therefore the total 
            // number of headers is 4 (2x custom, 2x 'chunked').
            headers.TryAddWithoutValidation("Transfer-Encoding", " , custom2, chunked ,");
            Assert.Equal(4, headers.TransferEncoding.Count);
            Assert.Equal(4, headers.GetValues("Transfer-Encoding").Count());
        }

        [Fact]
        public void Upgrade_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Equal(0, headers.Upgrade.Count);

            headers.Upgrade.Add(new ProductHeaderValue("custom1"));

            Assert.Equal(1, headers.Upgrade.Count);
            Assert.Equal(1, headers.GetValues("Upgrade").Count());

            headers.TryAddWithoutValidation("Upgrade", " , custom1 / 1.0, ");
            Assert.Equal(2, headers.Upgrade.Count);
            Assert.Equal(2, headers.GetValues("Upgrade").Count());
        }

        [Fact]
        public void Date_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Null(headers.Date);

            DateTimeOffset expected = DateTimeOffset.Now;
            headers.Date = expected;
            Assert.Equal(expected, headers.Date);

            headers.Clear();
            headers.TryAddWithoutValidation("Date", "  Sun, 06 Nov 1994 08:49:37 GMT  ");
            Assert.Equal(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero), headers.Date);
        }

        [Fact]
        public void Via_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Equal(0, headers.Via.Count);

            headers.Via.Add(new ViaHeaderValue("x11", "host"));

            Assert.Equal(1, headers.Via.Count);

            headers.TryAddWithoutValidation("Via", ", 1.1 host2");
            Assert.Equal(2, headers.Via.Count);
        }

        [Fact]
        public void Warning_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Equal(0, headers.Warning.Count);

            headers.Warning.Add(new WarningHeaderValue(199, "microsoft.com", "\"Miscellaneous warning\""));

            Assert.Equal(1, headers.Warning.Count);

            headers.TryAddWithoutValidation("Warning", "112 example.com \"Disconnected operation\"");
            Assert.Equal(2, headers.Warning.Count);
        }

        [Fact]
        public void CacheControl_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Null(headers.CacheControl);

            CacheControlHeaderValue value = new CacheControlHeaderValue();
            value.NoCache = true;
            headers.CacheControl = value;
            Assert.Equal(value, headers.CacheControl);

            headers.TryAddWithoutValidation("Cache-Control", "must-revalidate");
            value = new CacheControlHeaderValue();
            value.NoCache = true;
            value.MustRevalidate = true;
            Assert.Equal(value, headers.CacheControl);
        }

        [Fact]
        public void Trailer_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Equal(0, headers.Trailer.Count);

            headers.Trailer.Add("custom1");

            Assert.Equal(1, headers.Trailer.Count);

            headers.TryAddWithoutValidation("Trailer", ",custom2, ,");
            Assert.Equal(2, headers.Trailer.Count);
        }

        [Fact]
        public void Pragma_ReadAndWriteProperty_CallsForwardedToHttpGeneralHeaders()
        {
            Assert.Equal(0, headers.Pragma.Count);

            headers.Pragma.Add(new NameValueHeaderValue("custom1", "value1"));

            Assert.Equal(1, headers.Pragma.Count);

            headers.TryAddWithoutValidation("Pragma", "custom2");
            Assert.Equal(2, headers.Pragma.Count);
        }

        #endregion

        [Fact]
        public void CustomHeaders_RequestHeadersAsCustomHeaders_Success()
        {
            // Header names reserved for request headers are permitted as custom response headers.
            headers.Add("Accept", "v");
            headers.Add("Accept-Charset", "v");
            headers.Add("Accept-Encoding", "v");
            headers.Add("Accept-Language", "v");
            headers.Add("Authorization", "v");
            headers.Add("Expect", "v");
            headers.Add("From", "v");
            headers.Add("Host", "v");
            headers.Add("If-Match", "v");
            headers.Add("If-Modified-Since", "v");
            headers.Add("If-None-Match", "v");
            headers.Add("If-Range", "v");
            headers.Add("If-Unmodified-Since", "v");
            headers.Add("Max-Forwards", "v");
            headers.Add("Proxy-Authorization", "v");
            headers.Add("Range", "v");
            headers.Add("Referer", "v");
            headers.Add("TE", "v");
            headers.Add("User-Agent", "v");
        }

        [Fact]
        public void InvalidHeaders_AddContentHeaders_Throw()
        {
            // Try adding content headers. Use different casing to make sure case-insensitive comparison
            // is used.
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Allow", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Content-Encoding", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Content-Language", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("content-length", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Content-Location", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Content-MD5", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Content-Range", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("CONTENT-TYPE", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Expires", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Last-Modified", "v"); });
        }
    }
}
