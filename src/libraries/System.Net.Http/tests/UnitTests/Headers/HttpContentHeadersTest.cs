// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Tests
{
    public class HttpContentHeadersTest
    {
        private HttpContentHeaders _headers;

        public HttpContentHeadersTest()
        {
            _headers = new HttpContentHeaders(null);
        }

        [Fact]
        public void ContentLength_AddInvalidValueUsingUnusualCasing_ParserRetrievedUsingCaseInsensitiveComparison()
        {
            _headers = new HttpContentHeaders(new ComputeLengthHttpContent(() => 15));

            // Use uppercase header name to make sure the parser gets retrieved using case-insensitive comparison.
            Assert.Throws<FormatException>(() => { _headers.Add("CoNtEnT-LeNgTh", "this is invalid"); });
        }

        [Fact]
        public void ContentLength_ReadValue_TryComputeLengthInvoked()
        {
            _headers = new HttpContentHeaders(new ComputeLengthHttpContent(() => 15));

            // The delegate is invoked to return the length.
            Assert.Equal(15, _headers.ContentLength);
            Assert.Equal((long)15, _headers.GetSingleParsedValue(KnownHeaders.ContentLength.Descriptor));

            // After getting the calculated content length, set it to null.
            _headers.ContentLength = null;
            Assert.Null(_headers.ContentLength);
            Assert.False(_headers.Contains(KnownHeaders.ContentLength.Name));

            _headers.ContentLength = 27;
            Assert.Equal((long)27, _headers.ContentLength);
            Assert.Equal((long)27, _headers.GetSingleParsedValue(KnownHeaders.ContentLength.Descriptor));
        }

        [Fact]
        public void ContentLength_SetCustomValue_TryComputeLengthNotInvoked()
        {
            _headers = new HttpContentHeaders(new ComputeLengthHttpContent(() => { throw new ShouldNotBeInvokedException(); }));

            _headers.ContentLength = 27;
            Assert.Equal((long)27, _headers.ContentLength);
            Assert.Equal((long)27, _headers.GetSingleParsedValue(KnownHeaders.ContentLength.Descriptor));

            // After explicitly setting the content length, set it to null.
            _headers.ContentLength = null;
            Assert.Null(_headers.ContentLength);
            Assert.False(_headers.Contains(KnownHeaders.ContentLength.Name));

            // Make sure the header gets serialized correctly
            _headers.ContentLength = 12345;
            Assert.Equal("12345", _headers.GetValues("Content-Length").First());
        }

        [Fact]
        public void ContentLength_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            _headers = new HttpContentHeaders(new ComputeLengthHttpContent(() => { throw new ShouldNotBeInvokedException(); }));
            _headers.TryAddWithoutValidation(HttpKnownHeaderNames.ContentLength, " 68 ");

            Assert.Equal(68, _headers.ContentLength);
        }

        [Fact]
        public void ContentType_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            MediaTypeHeaderValue value = new MediaTypeHeaderValue("text/plain");
            value.CharSet = "utf-8";
            value.Parameters.Add(new NameValueHeaderValue("custom", "value"));

            Assert.Null(_headers.ContentType);

            _headers.ContentType = value;
            Assert.Same(value, _headers.ContentType);

            _headers.ContentType = null;
            Assert.Null(_headers.ContentType);
        }

        [Fact]
        public void ContentType_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            _headers.TryAddWithoutValidation("Content-Type", "text/plain; charset=utf-8; custom=value");

            MediaTypeHeaderValue value = new MediaTypeHeaderValue("text/plain");
            value.CharSet = "utf-8";
            value.Parameters.Add(new NameValueHeaderValue("custom", "value"));

            Assert.Equal(value, _headers.ContentType);
        }

        [Fact]
        public void ContentType_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            _headers.TryAddWithoutValidation("Content-Type", "text/plain; charset=utf-8; custom=value, other/type");
            Assert.Null(_headers.ContentType);
            Assert.Equal(1, _headers.GetValues("Content-Type").Count());
            Assert.Equal("text/plain; charset=utf-8; custom=value, other/type",
                _headers.GetValues("Content-Type").First());

            _headers.Clear();
            _headers.TryAddWithoutValidation("Content-Type", ",text/plain"); // leading separator
            Assert.Null(_headers.ContentType);
            Assert.Equal(1, _headers.GetValues("Content-Type").Count());
            Assert.Equal(",text/plain", _headers.GetValues("Content-Type").First());

            _headers.Clear();
            _headers.TryAddWithoutValidation("Content-Type", "text/plain,"); // trailing separator
            Assert.Null(_headers.ContentType);
            Assert.Equal(1, _headers.GetValues("Content-Type").Count());
            Assert.Equal("text/plain,", _headers.GetValues("Content-Type").First());
        }

        [Fact]
        public void ContentRange_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(_headers.ContentRange);
            ContentRangeHeaderValue value = new ContentRangeHeaderValue(1, 2, 3);

            _headers.ContentRange = value;
            Assert.Equal(value, _headers.ContentRange);

            _headers.ContentRange = null;
            Assert.Null(_headers.ContentRange);
        }

        [Fact]
        public void ContentRange_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            _headers.TryAddWithoutValidation("Content-Range", "custom 1-2/*");

            ContentRangeHeaderValue value = new ContentRangeHeaderValue(1, 2);
            value.Unit = "custom";

            Assert.Equal(value, _headers.ContentRange);
        }

        [Fact]
        public void ContentLocation_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(_headers.ContentLocation);

            Uri expected = new Uri("http://example.com/path/");
            _headers.ContentLocation = expected;
            Assert.Equal(expected, _headers.ContentLocation);

            _headers.ContentLocation = null;
            Assert.Null(_headers.ContentLocation);
            Assert.False(_headers.Contains("Content-Location"));
        }

        [Fact]
        public void ContentLocation_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            _headers.TryAddWithoutValidation("Content-Location", "  http://www.example.com/path/?q=v  ");
            Assert.Equal(new Uri("http://www.example.com/path/?q=v"), _headers.ContentLocation);

            _headers.Clear();
            _headers.TryAddWithoutValidation("Content-Location", "/relative/uri/");
            Assert.Equal(new Uri("/relative/uri/", UriKind.Relative), _headers.ContentLocation);
        }

        [Fact]
        public void ContentLocation_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            _headers.TryAddWithoutValidation("Content-Location", " http://example.com http://other");
            Assert.Null(_headers.GetSingleParsedValue(KnownHeaders.ContentLocation.Descriptor));
            Assert.Equal(1, _headers.GetValues("Content-Location").Count());
            Assert.Equal(" http://example.com http://other", _headers.GetValues("Content-Location").First());

            _headers.Clear();
            _headers.TryAddWithoutValidation("Content-Location", "http://host /other");
            Assert.Null(_headers.GetSingleParsedValue(KnownHeaders.ContentLocation.Descriptor));
            Assert.Equal(1, _headers.GetValues("Content-Location").Count());
            Assert.Equal("http://host /other", _headers.GetValues("Content-Location").First());
        }

        [Fact]
        public void ContentEncoding_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, _headers.ContentEncoding.Count);

            _headers.ContentEncoding.Add("custom1");
            _headers.ContentEncoding.Add("custom2");

            Assert.Equal(2, _headers.ContentEncoding.Count);
            Assert.Equal(2, _headers.GetValues("Content-Encoding").Count());

            Assert.Equal("custom1", _headers.ContentEncoding.ElementAt(0));
            Assert.Equal("custom2", _headers.ContentEncoding.ElementAt(1));

            _headers.ContentEncoding.Clear();
            Assert.Equal(0, _headers.ContentEncoding.Count);
            Assert.False(_headers.Contains("Content-Encoding"));
        }

        [Fact]
        public void ContentEncoding_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            _headers.TryAddWithoutValidation("Content-Encoding", ",custom1, custom2, custom3,");

            Assert.Equal(3, _headers.ContentEncoding.Count);
            Assert.Equal(3, _headers.GetValues("Content-Encoding").Count());

            Assert.Equal("custom1", _headers.ContentEncoding.ElementAt(0));
            Assert.Equal("custom2", _headers.ContentEncoding.ElementAt(1));
            Assert.Equal("custom3", _headers.ContentEncoding.ElementAt(2));

            _headers.ContentEncoding.Clear();
            Assert.Equal(0, _headers.ContentEncoding.Count);
            Assert.False(_headers.Contains("Content-Encoding"));
        }

        [Fact]
        public void ContentEncoding_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            _headers.TryAddWithoutValidation("Content-Encoding", "custom1 custom2"); // no separator

            Assert.Equal(0, _headers.ContentEncoding.Count);
            Assert.Equal(1, _headers.GetValues("Content-Encoding").Count());
            Assert.Equal("custom1 custom2", _headers.GetValues("Content-Encoding").First());
        }

        [Fact]
        public void ContentLanguage_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, _headers.ContentLanguage.Count);

            // Note that Content-Language for us is just a list of tokens. We don't verify if the format is a valid
            // language tag. Users will pass the language tag to other classes like Encoding.GetEncoding() to retrieve
            // an encoding. These classes will do not only syntax checking but also verify if the language tag exists.
            _headers.ContentLanguage.Add("custom1");
            _headers.ContentLanguage.Add("custom2");

            Assert.Equal(2, _headers.ContentLanguage.Count);
            Assert.Equal(2, _headers.GetValues("Content-Language").Count());

            Assert.Equal("custom1", _headers.ContentLanguage.ElementAt(0));
            Assert.Equal("custom2", _headers.ContentLanguage.ElementAt(1));

            _headers.ContentLanguage.Clear();
            Assert.Equal(0, _headers.ContentLanguage.Count);
            Assert.False(_headers.Contains("Content-Language"));
        }

        [Fact]
        public void ContentLanguage_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            _headers.TryAddWithoutValidation("Content-Language", ",custom1, custom2, custom3,");

            Assert.Equal(3, _headers.ContentLanguage.Count);
            Assert.Equal(3, _headers.GetValues("Content-Language").Count());

            Assert.Equal("custom1", _headers.ContentLanguage.ElementAt(0));
            Assert.Equal("custom2", _headers.ContentLanguage.ElementAt(1));
            Assert.Equal("custom3", _headers.ContentLanguage.ElementAt(2));

            _headers.ContentLanguage.Clear();
            Assert.Equal(0, _headers.ContentLanguage.Count);
            Assert.False(_headers.Contains("Content-Language"));
        }

        [Fact]
        public void ContentLanguage_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            _headers.TryAddWithoutValidation("Content-Language", "custom1 custom2"); // no separator

            Assert.Equal(0, _headers.ContentLanguage.Count);
            Assert.Equal(1, _headers.GetValues("Content-Language").Count());
            Assert.Equal("custom1 custom2", _headers.GetValues("Content-Language").First());
        }

        [Fact]
        public void ContentMD5_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(_headers.ContentMD5);

            byte[] expected = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            _headers.ContentMD5 = expected;
            Assert.Equal(expected, _headers.ContentMD5); // must be the same object reference

            // Make sure the header gets serialized correctly
            Assert.Equal("AQIDBAUGBw==", _headers.GetValues("Content-MD5").First());

            _headers.ContentMD5 = null;
            Assert.Null(_headers.ContentMD5);
            Assert.False(_headers.Contains("Content-MD5"));
        }

        [Fact]
        public void ContentMD5_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            _headers.TryAddWithoutValidation("Content-MD5", "  lvpAKQ==  ");
            Assert.Equal(new byte[] { 150, 250, 64, 41 }, _headers.ContentMD5);

            _headers.Clear();
            _headers.TryAddWithoutValidation("Content-MD5", "+dIkS/MnOP8=");
            Assert.Equal(new byte[] { 249, 210, 36, 75, 243, 39, 56, 255 }, _headers.ContentMD5);
        }

        [Fact]
        public void ContentMD5_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            _headers.TryAddWithoutValidation("Content-MD5", "AQ--");
            Assert.Null(_headers.GetSingleParsedValue(KnownHeaders.ContentMD5.Descriptor));
            Assert.Equal(1, _headers.GetValues("Content-MD5").Count());
            Assert.Equal("AQ--", _headers.GetValues("Content-MD5").First());

            _headers.Clear();
            _headers.TryAddWithoutValidation("Content-MD5", "AQ==, CD");
            Assert.Null(_headers.GetSingleParsedValue(KnownHeaders.ContentMD5.Descriptor));
            Assert.Equal(1, _headers.GetValues("Content-MD5").Count());
            Assert.Equal("AQ==, CD", _headers.GetValues("Content-MD5").First());
        }

        [Fact]
        public void Allow_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, _headers.Allow.Count);

            _headers.Allow.Add("custom1");
            _headers.Allow.Add("custom2");

            Assert.Equal(2, _headers.Allow.Count);
            Assert.Equal(2, _headers.GetValues("Allow").Count());

            Assert.Equal("custom1", _headers.Allow.ElementAt(0));
            Assert.Equal("custom2", _headers.Allow.ElementAt(1));

            _headers.Allow.Clear();
            Assert.Equal(0, _headers.Allow.Count);
            Assert.False(_headers.Contains("Allow"));
        }

        [Fact]
        public void Allow_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            _headers.TryAddWithoutValidation("Allow", ",custom1, custom2, custom3,");

            Assert.Equal(3, _headers.Allow.Count);
            Assert.Equal(3, _headers.GetValues("Allow").Count());

            Assert.Equal("custom1", _headers.Allow.ElementAt(0));
            Assert.Equal("custom2", _headers.Allow.ElementAt(1));
            Assert.Equal("custom3", _headers.Allow.ElementAt(2));

            _headers.Allow.Clear();
            Assert.Equal(0, _headers.Allow.Count);
            Assert.False(_headers.Contains("Allow"));
        }

        [Fact]
        public void Allow_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            _headers.TryAddWithoutValidation("Allow", "custom1 custom2"); // no separator

            Assert.Equal(0, _headers.Allow.Count);
            Assert.Equal(1, _headers.GetValues("Allow").Count());
            Assert.Equal("custom1 custom2", _headers.GetValues("Allow").First());
        }

        [Fact]
        public void Expires_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(_headers.Expires);

            DateTimeOffset expected = DateTimeOffset.Now;
            _headers.Expires = expected;
            Assert.Equal(expected, _headers.Expires);

            _headers.Expires = null;
            Assert.Null(_headers.Expires);
            Assert.False(_headers.Contains("Expires"));
        }

        [Fact]
        public void Expires_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            _headers.TryAddWithoutValidation("Expires", "  Sun, 06 Nov 1994 08:49:37 GMT  ");
            Assert.Equal(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero), _headers.Expires);

            _headers.Clear();
            _headers.TryAddWithoutValidation("Expires", "Sun, 06 Nov 1994 08:49:37 GMT");
            Assert.Equal(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero), _headers.Expires);
        }

        [Fact]
        public void Expires_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            _headers.TryAddWithoutValidation("Expires", " Sun, 06 Nov 1994 08:49:37 GMT ,");
            Assert.Null(_headers.GetSingleParsedValue(KnownHeaders.Expires.Descriptor));
            Assert.Equal(1, _headers.GetValues("Expires").Count());
            Assert.Equal(" Sun, 06 Nov 1994 08:49:37 GMT ,", _headers.GetValues("Expires").First());

            _headers.Clear();
            _headers.TryAddWithoutValidation("Expires", " Sun, 06 Nov ");
            Assert.Null(_headers.GetSingleParsedValue(KnownHeaders.Expires.Descriptor));
            Assert.Equal(1, _headers.GetValues("Expires").Count());
            Assert.Equal(" Sun, 06 Nov ", _headers.GetValues("Expires").First());
        }

        [Fact]
        public void LastModified_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(_headers.LastModified);

            DateTimeOffset expected = DateTimeOffset.Now;
            _headers.LastModified = expected;
            Assert.Equal(expected, _headers.LastModified);

            _headers.LastModified = null;
            Assert.Null(_headers.LastModified);
            Assert.False(_headers.Contains("Last-Modified"));
        }

        [Fact]
        public void LastModified_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            _headers.TryAddWithoutValidation("Last-Modified", "  Sun, 06 Nov 1994 08:49:37 GMT  ");
            Assert.Equal(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero), _headers.LastModified);

            _headers.Clear();
            _headers.TryAddWithoutValidation("Last-Modified", "Sun, 06 Nov 1994 08:49:37 GMT");
            Assert.Equal(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero), _headers.LastModified);
        }

        [Fact]
        public void LastModified_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            _headers.TryAddWithoutValidation("Last-Modified", " Sun, 06 Nov 1994 08:49:37 GMT ,");
            Assert.Null(_headers.GetSingleParsedValue(KnownHeaders.LastModified.Descriptor));
            Assert.Equal(1, _headers.GetValues("Last-Modified").Count());
            Assert.Equal(" Sun, 06 Nov 1994 08:49:37 GMT ,", _headers.GetValues("Last-Modified").First());

            _headers.Clear();
            _headers.TryAddWithoutValidation("Last-Modified", " Sun, 06 Nov ");
            Assert.Null(_headers.GetSingleParsedValue(KnownHeaders.LastModified.Descriptor));
            Assert.Equal(1, _headers.GetValues("Last-Modified").Count());
            Assert.Equal(" Sun, 06 Nov ", _headers.GetValues("Last-Modified").First());
        }

        [Fact]
        public void InvalidHeaders_AddRequestAndResponseHeaders_Throw()
        {
            // Try adding request, response, and general _headers. Use different casing to make sure case-insensitive
            // comparison is used.
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Accept-Ranges", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("age", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("ETag", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Location", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Proxy-Authenticate", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Retry-After", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Server", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Vary", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("WWW-Authenticate", "v"); });

            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Accept", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Accept-Charset", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Accept-Encoding", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Accept-Language", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Authorization", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Expect", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("From", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Host", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("If-Match", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("If-Modified-Since", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("If-None-Match", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("If-Range", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("If-Unmodified-Since", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Max-Forwards", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Proxy-Authorization", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Range", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Referer", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("TE", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("User-Agent", "v"); });

            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Cache-Control", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Connection", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Date", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Pragma", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Trailer", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Transfer-Encoding", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Upgrade", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Via", "v"); });
            Assert.Throws<InvalidOperationException>(() => { _headers.Add("Warning", "v"); });
        }

        private sealed class ComputeLengthHttpContent : HttpContent
        {
            private readonly Func<long?> _tryComputeLength;

            internal ComputeLengthHttpContent(Func<long?> tryComputeLength)
            {
                _tryComputeLength = tryComputeLength;
            }

            protected internal override bool TryComputeLength(out long length)
            {
                long? result = _tryComputeLength();
                length = result.GetValueOrDefault();
                return result.HasValue;
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) { throw new NotImplementedException(); }
        }
    }
}
