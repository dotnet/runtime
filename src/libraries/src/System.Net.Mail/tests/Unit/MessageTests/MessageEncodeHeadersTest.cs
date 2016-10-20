// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Net.Mime;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class MessageEncodeHeadersTest
    {
        private HeaderCollection _headers = new HeaderCollection();
        private Message _message = new Message();
        private const string CustomUnicodeHeaderValue = "START\u00EA\u00EB\u00EFf\u00DA\u00EA\u00EB\u00EF\u00EF\u00DAaasubject\u00EA\u00EB\u00EFf\u00DAEND";

        [Fact]
        public void EncodeHeaders_WithCustomUnicodeHeaders_ShouldEncodeHeaders()
        {
            _headers.Add("X-Custom", CustomUnicodeHeaderValue);
            _message.EncodeHeaders(_headers, false);

            string encodedHeader = _headers.Get("X-Custom");
            Assert.True(encodedHeader.StartsWith("="));
            Assert.True(encodedHeader.EndsWith("="));
            //should contain no unicode
            Assert.False(ContainsNonAscii(encodedHeader), encodedHeader);

            Assert.Equal(Encoding.UTF8, _message.HeadersEncoding);

            // Allow Unicode
            _headers.Clear();
            _headers.Add("X-Custom", CustomUnicodeHeaderValue);
            _message.EncodeHeaders(_headers, true);

            encodedHeader = _headers.Get("X-Custom");
            Assert.False(encodedHeader.StartsWith("="));
            Assert.False(encodedHeader.EndsWith("="));
            //should contain unicode
            Assert.True(ContainsNonAscii(encodedHeader), encodedHeader);
        }

        [Fact]
        public void EncodeHeaders_WithWellKnownHeader_ShouldNotEncodeWellKnownHeaders()
        {
            string hvalue = "\"jeffART\u00EA\u00EB\u00EFf\u00DA\u00EA\" <jeff@nclmailtest.com>";
            _headers.InternalAdd("Reply-To", hvalue);
            _message.EncodeHeaders(_headers, false);

            string encodedHeader = _headers.Get("Reply-To");
            Assert.False(encodedHeader.StartsWith("="));
            Assert.False(encodedHeader.EndsWith("="));
            Assert.Equal(encodedHeader, hvalue);

            Assert.Equal(Encoding.UTF8, _message.HeadersEncoding);

            // Allow Unicode
            _headers.Clear();
            _headers.InternalAdd("Reply-To", hvalue);
            _message.EncodeHeaders(_headers, true);

            encodedHeader = _headers.Get("Reply-To");
            Assert.False(encodedHeader.StartsWith("="));
            Assert.False(encodedHeader.EndsWith("="));
            Assert.Equal(encodedHeader, hvalue);
        }

        [Fact]
        public void EncodeHeaders_WithMultipleValuesForSameHeader_ShouldEncodeEachOneCorrectly()
        {
            string hvalue = "Some Ascii header";
            _headers.Add("X-Custom", hvalue);
            _headers.Add("X-Custom", CustomUnicodeHeaderValue);
            _message.EncodeHeaders(_headers, false);

            string[] output = _headers.GetValues("X-Custom");
            Assert.Equal(2, output.Length);

            foreach (string s in output)
            {
                Assert.False(ContainsNonAscii(s), s);
            }

            Assert.Equal(Encoding.UTF8, _message.HeadersEncoding);

            // Allow Unicode
            _headers.Clear();
            _headers.Add("X-Custom", hvalue);
            _headers.Add("X-Custom", CustomUnicodeHeaderValue);
            _message.EncodeHeaders(_headers, true);

            output = _headers.GetValues("X-Custom");
            Assert.Equal(2, output.Length);
            Assert.False(ContainsNonAscii(output[0]), output[0]);
            Assert.True(ContainsNonAscii(output[1]), output[1]);
        }

        [Fact]
        public void EncodeHeaders_WithHeadersEncodingPropertySet_ShouldEncodeWithProperEncoding()
        {
            //note:  this is the WRONG code page to use for the unicode value supplied:  we are not testing that
            //here, we care about whether or not it is using the correct encoding value to encode as specified
            //in the HeadersEncoding property.
            Encoding encoding = Encoding.GetEncoding("utf-32");
            _message.HeadersEncoding = encoding;
            _headers.Add("X-Custom", CustomUnicodeHeaderValue);
            _message.EncodeHeaders(_headers, false);

            string encodedHeader = _headers.Get("X-Custom");
            Assert.True(encodedHeader.StartsWith("="), "didn't start with =");
            Assert.True(encodedHeader.EndsWith("="), "didn't end with =");
            string[] splits = encodedHeader.Split(new char[] { '?' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal("utf-32", splits[1]);
            Assert.Equal(encoding, _message.HeadersEncoding);

            // Allow Unicode, ignore header encoding
            _headers.Clear();
            _headers.Add("X-Custom", CustomUnicodeHeaderValue);
            _message.EncodeHeaders(_headers, true);

            encodedHeader = _headers.Get("X-Custom");
            Assert.False(encodedHeader.StartsWith("="));
            Assert.False(encodedHeader.EndsWith("="));
            Assert.True(ContainsNonAscii(encodedHeader), encodedHeader);
        }

        [Fact]
        public void EncodeHeaders_WithNoHeadersEncodingPropertySpecified_ShouldDefaultToUTF8()
        {
            _headers.Add("X-Custom", CustomUnicodeHeaderValue);
            _message.EncodeHeaders(_headers, false);

            string encodedHeader = _headers.Get("X-Custom");
            Assert.True(encodedHeader.StartsWith("="), "didn't start with =");
            Assert.True(encodedHeader.EndsWith("="), "didn't end with =");
            string[] splits = encodedHeader.Split(new char[] { '?' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal("utf-8", splits[1]);
            Assert.Equal(Encoding.UTF8, _message.HeadersEncoding);
        }

        [Fact]
        public void EncodeHeaders_UserSettableHeaderDoesntAllowUnicode_ShouldEncode()
        {
            // We should have rejected such a value in the past, but instead we just encode it.
            // Maintain this behavior for app-compat.
            _headers.Add("Message-ID", CustomUnicodeHeaderValue);
            _message.EncodeHeaders(_headers, false);

            string encodedHeader = _headers.Get("Message-ID");
            Assert.True(encodedHeader.StartsWith("="), encodedHeader);
            Assert.True(encodedHeader.EndsWith("="), encodedHeader);
            Assert.False(ContainsNonAscii(encodedHeader), encodedHeader);

            Assert.Equal(Encoding.UTF8, _message.HeadersEncoding);

            // Allow Unicode, but encode because this specific header doesn't allow Unicode.
            _headers.Clear();
            _headers.Add("Message-ID", CustomUnicodeHeaderValue);
            _message.EncodeHeaders(_headers, true);

            encodedHeader = _headers.Get("Message-ID");
            Assert.True(encodedHeader.StartsWith("="), encodedHeader);
            Assert.True(encodedHeader.EndsWith("="), encodedHeader);
            Assert.False(ContainsNonAscii(encodedHeader), encodedHeader);
        }

        private bool ContainsNonAscii(string input)
        {
            foreach (char ch in input)
            {
                if (ch >= (char)128)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
