// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.WebHeaderCollectionTests
{
    public class WebHeaderCollectionTest
    {
        [Fact]
        public void Ctor_Success()
        {
            new WebHeaderCollection();
        }

        [Fact]
        public void DefaultPropertyValues_ReturnEmptyAfterConstruction_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Equal(0, w.AllKeys.Length);
            Assert.Equal(0, w.Count);
            Assert.Equal("\r\n", w.ToString());
            Assert.Empty(w);
        }

        [Fact]
        public void HttpRequestHeader_Add_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w[HttpRequestHeader.Connection] = "keep-alive";

            Assert.Equal(1, w.Count);
            Assert.Equal("keep-alive", w[HttpRequestHeader.Connection]);
            Assert.Equal("Connection", w.AllKeys[0]);
        }

        [Fact]
        public void CustomHeader_AddQuery_Success()
        {
            string customHeader = "Custom-Header";
            string customValue = "Custom;.-Value";
            WebHeaderCollection w = new WebHeaderCollection();
            w[customHeader] = customValue;

            Assert.Equal(1, w.Count);
            Assert.Equal(customValue, w[customHeader]);
            Assert.Equal(customHeader, w.AllKeys[0]);
        }

        [Fact]
        public void HttpResponseHeader_AddQuery_CommonHeader_Success()
        {
            string headerValue = "value123";
            WebHeaderCollection w = new WebHeaderCollection();
            w[HttpResponseHeader.ProxyAuthenticate] = headerValue;
            w[HttpResponseHeader.WwwAuthenticate] = headerValue;

            Assert.Equal(headerValue, w[HttpResponseHeader.ProxyAuthenticate]);
            Assert.Equal(headerValue, w[HttpResponseHeader.WwwAuthenticate]);
        }

        [Fact]
        public void HttpRequest_AddQuery_CommonHeader_Success()
        {
            string headerValue = "value123";
            WebHeaderCollection w = new WebHeaderCollection();
            w[HttpRequestHeader.Accept] = headerValue;

            Assert.Equal(headerValue, w[HttpRequestHeader.Accept]);
        }

        [Fact]
        public void CustomHeader_AddEmptyName_Fail()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentNullException>(() => w[""] = "value");
        }

        [Fact]
        public void RequestThenResponseHeaders_Add_Fail()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w[HttpRequestHeader.Accept] = "text/json";
            Assert.Throws<InvalidOperationException>(() => w[HttpResponseHeader.ContentLength] = "123");
        }

        [Fact]
        public void ResponseThenRequestHeaders_Add_Fail()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w[HttpResponseHeader.ContentLength] = "123";
            Assert.Throws<InvalidOperationException>(() => w[HttpRequestHeader.Accept] = "text/json");
        }

        [Fact]
        public void ResponseHeader_QueryRequest_Fail()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w[HttpResponseHeader.ContentLength] = "123";
            Assert.Throws<InvalidOperationException>(() => w[HttpRequestHeader.Accept]);
        }

        [Fact]
        public void RequestHeader_QueryResponse_Fail()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w[HttpRequestHeader.Accept] = "text/json";
            Assert.Throws<InvalidOperationException>(() => w[HttpResponseHeader.ContentLength]);
        }

        [Fact]
        public void NameValue_Add_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add("Accept", "text/json");
        }

        [Fact]
        public void NameValue_AddNullName_Fail()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentNullException>(() => w.Add(null, "test"));
        }

        [Fact]
        public void NameValue_AddEmptyName_Fail()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentException>(() => w.Add("", "test"));
        }

        [Fact]
        public void NameValue_AddEmptyValue_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add("custom", "");
        }

        public static object[][] InvalidNames = {
            new object[] { "(" },
            new object[] { "\u1234" },
            new object[] { "\u0019" }
        };

        [Theory, MemberData(nameof(InvalidNames))]
        public void CheckBadChars_InvalidName(string name)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentException>(() => w[name] = "test");
        }

        public static object[][] InvalidValues = {
            new object[] { "value1\rvalue2\r" },
            new object[] { "value1\nvalue2\r" },
            new object[] { "value1\u007fvalue2" },
            new object[] { "value1\r\nvalue2" },
            new object[] { "value1\u0019value2" }
        };

        [Theory, MemberData(nameof(InvalidValues))]
        public void CheckBadChars_InvalidValue(string value)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentException>(() => w["custom"] = value);
        }

        public static object[][] ValidValues = {
            new object[] { "value1\r\n" },
            new object[] { "value1\tvalue2" },
            new object[] { "value1\r\n\tvalue2" },
            new object[] { "value1\r\n value2" }
        };

        [Theory, MemberData(nameof(ValidValues))]
        public void CheckBadChars_ValidValue(string value)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w["custom"] = value;
        }

        [Fact]
        public void Custom_RemoveBlankName_Throws()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentNullException>(() => w.Remove(""));
        }

        [Fact]
        public void Custom_RemoveIllegalCharacter_Throws()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentException>(() => w.Remove("{"));
        }

        [Fact]
        public void Custom_AddThenRemove_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add("name", "value");
            w.Remove("name");
            Assert.Equal(0, w.Count);
        }

        [Fact]
        public void GetNonExistent_ReturnsNull_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Equal(0, w.Count);
            Assert.Null(w["name"]);
            Assert.Null(w.GetValues("name"));
        }

        public static object[][] TestHeadersWithValues = 
        {
            new object[] { "Accept",           "text/plain, text/html",                  new[] { "text/plain", "text/html" } },
            new object[] { "uPgRaDe",          " HTTP/2.0 , SHTTP/1.3,  , RTA/x11 ",     new[] { "HTTP/2.0", "SHTTP/1.3", string.Empty, "RTA/x11" } },
            new object[] { "Custom",           "foo, bar, spam",                         new[] { "foo, bar, spam" } },
            new object[] { "CustomQuotes",     "\"foo, bar, spam",                       new[] { "\"foo, bar, spam" } },
            new object[] { "If-Match",         "xyzzy",                                  new[] { "xyzzy" } },
            new object[] { "If-Match",         "\"xyzzy\", \"r2d2xxxx\", \"c3piozzzz\"", new[] { "\"xyzzy\"", "\"r2d2xxxx\"", "\"c3piozzzz\"" } },
            new object[] { "If-Match",         "xyzzy, \"r2d2, xxxx\", c3piozzzz",       new[] { "xyzzy", "\"r2d2, xxxx\"", "c3piozzzz" } },
            new object[] { "WWW-Authenticate", "Basic",                                  new[] { "Basic" } },
        };

        [Theory]
        [MemberData(nameof(TestHeadersWithValues))]
        public void GetValues_String_Success(string header, string value, string[] expectedValues)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add(header, value);
            string modifiedHeader = header.ToLowerInvariant(); // header should be case insensitive
            Assert.Equal(expectedValues, w.GetValues(modifiedHeader));
        }

        [Fact]
        public void ToString_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add("Accept", "text/plain");
            w.Add("Content-Length", "123");
            Assert.Equal(
                "Accept: text/plain\r\nContent-Length: 123\r\n\r\n",
                w.ToString());
        }

        [Fact]
        public void IterateCollection_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w["Accept"] = "text/plain";
            w["Content-Length"] = "123";

            string result = "";
            foreach (var item in w)
            {
                result += item;
            }

            Assert.Equal("AcceptContent-Length", result);
        }

        [Fact]
        public void IterateIEnumerable_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w["Accept"] = "text/plain";
            w["Content-Length"] = "123";

            string result = "";
            foreach (var item in (System.Collections.IEnumerable)w)
            {
                result += item;
            }

            Assert.Equal("AcceptContent-Length", result);
        }

        [Fact]
        public void Get_Success()
        {
            string[] keys = { "Accept", "uPgRaDe", "Custom" };
            string[] values = { "text/plain, text/html", " HTTP/2.0 , SHTTP/1.3,  , RTA/x11 ", "\"xyzzy\", \"r2d2xxxx\", \"c3piozzzz\"" };
            WebHeaderCollection w = new WebHeaderCollection();

            for (int i = 0; i < keys.Length; ++i)
            {
                w.Add(keys[i], values[i]);
            }

            for (int i = 0; i < keys.Length; ++i)
            {
                string expected = values[i].Trim();
                Assert.Equal(expected, w.Get(i));
            }
        }

        [Fact]
        public void Get_Fail()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add("Accept", "text/plain");

            Assert.Throws<ArgumentOutOfRangeException>(() => w.Get(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => w.Get(42));
        }

        [Fact]
        public void GetValues_Int_Success()
        {
            string[] keys = { "Accept", "uPgRaDe", "Custom" };
            string[] values = { "text/plain, text/html", " HTTP/2.0 , SHTTP/1.3,  , RTA/x11 ", "\"xyzzy\", \"r2d2xxxx\", \"c3piozzzz\"" };
            WebHeaderCollection w = new WebHeaderCollection();

            for (int i = 0; i < keys.Length; ++i)
            {
                w.Add(keys[i], values[i]);
            }

            for (int i = 0; i < keys.Length; ++i)
            {
                string[] expected = new[] { values[i].Trim() };
                Assert.Equal(expected, w.GetValues(i));
            }
        }

        [Fact]
        public void GetValues_Int_Fail()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add("Accept", "text/plain");

            Assert.Throws<ArgumentOutOfRangeException>(() => w.GetValues(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => w.GetValues(42));
        }

        [Fact]
        public void GetKey_Success()
        {
            const string key = "Accept";
            const string key2 = "Content-Length";
            WebHeaderCollection w = new WebHeaderCollection();

            w.Add(key, "text/plain");
            w.Add(key2, "123");

            Assert.Equal(key, w.GetKey(0));
            Assert.Equal(key2, w.GetKey(1));
        }

        [Fact]
        public void GetKey_Fail()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add("Accept", "text/plain");

            Assert.Throws<ArgumentOutOfRangeException>(() => w.GetKey(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => w.GetKey(42));
        }

        [Fact]
        public void Clear_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add("Accept", "text/plain");
            w.Add("Content-Length", "123");

            Assert.NotEmpty(w);
            w.Clear();
            Assert.Empty(w);
        }
    }
}
