// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

using Xunit;

namespace System.Net.WebHeaderCollectionTests
{
    public partial class WebHeaderCollectionTest
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
            Assert.Empty(w.AllKeys);
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

        [Theory]
        [InlineData((HttpRequestHeader)int.MinValue)]
        [InlineData((HttpRequestHeader)(-1))]
        [InlineData((HttpRequestHeader)int.MaxValue)]
        public void HttpRequestHeader_AddInvalid_Throws(HttpRequestHeader header)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<IndexOutOfRangeException>(() => w[header] = "foo");
        }

        [Theory]
        [InlineData((HttpResponseHeader)int.MinValue)]
        [InlineData((HttpResponseHeader)(-1))]
        [InlineData((HttpResponseHeader)int.MaxValue)]
        public void HttpResponseHeader_AddInvalid_Throws(HttpResponseHeader header)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<IndexOutOfRangeException>(() => w[header] = "foo");
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
        public void RequestThenResponseHeaders_Add_Throws()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w[HttpRequestHeader.Accept] = "text/json";
            Assert.Throws<InvalidOperationException>(() => w[HttpResponseHeader.ContentLength] = "123");
        }

        [Fact]
        public void ResponseThenRequestHeaders_Add_Throws()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w[HttpResponseHeader.ContentLength] = "123";
            Assert.Throws<InvalidOperationException>(() => w[HttpRequestHeader.Accept] = "text/json");
        }

        [Fact]
        public void ResponseHeader_QueryRequest_Throws()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w[HttpResponseHeader.ContentLength] = "123";
            Assert.Throws<InvalidOperationException>(() => w[HttpRequestHeader.Accept]);
        }

        [Fact]
        public void RequestHeader_QueryResponse_Throws()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w[HttpRequestHeader.Accept] = "text/json";
            Assert.Throws<InvalidOperationException>(() => w[HttpResponseHeader.ContentLength]);
        }

        [Fact]
        public void Setter_ValidName_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w["Accept"] = "text/json";
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Setter_NullOrEmptyName_Throws(string name)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentNullException>("name", () => w[name] = "test");
        }

        public static object[][] InvalidNames = {
            new object[] { "(" },
            new object[] { "\u1234" },
            new object[] { "\u0019" }
        };

        [Theory, MemberData(nameof(InvalidNames))]
        public void Setter_InvalidName_Throws(string name)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentException>("name", () => w[name] = "test");
        }

        public static object[][] InvalidValues = {
            new object[] { "value1\rvalue2\r" },
            new object[] { "value1\nvalue2\r" },
            new object[] { "value1\u007fvalue2" },
            new object[] { "value1\r\nvalue2" },
            new object[] { "value1\u0019value2" }
        };

        [Theory, MemberData(nameof(InvalidValues))]
        public void Setter_InvalidValue_Throws(string value)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentException>("value", () => w["custom"] = value);
        }

        public static object[][] ValidValues = {
            new object[] { null },
            new object[] { "" },
            new object[] { "value1\r\n" },
            new object[] { "value1\tvalue2" },
            new object[] { "value1\r\n\tvalue2" },
            new object[] { "value1\r\n value2" }
        };

        [Theory, MemberData(nameof(ValidValues))]
        public void Setter_ValidValue_Success(string value)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w["custom"] = value;
        }

        [Theory]
        [InlineData("name", "name")]
        [InlineData("name", "NaMe")]
        [InlineData("nAmE", "name")]
        public void Setter_SameHeaderTwice_Success(string firstName, string secondName)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w[firstName] = "first";
            w[secondName] = "second";
            Assert.Equal(1, w.Count);
            Assert.NotEmpty(w);
            Assert.NotEmpty(w.AllKeys);
            Assert.Equal(new[] { firstName }, w.AllKeys);
            Assert.Equal("second", w[firstName]);
            Assert.Equal("second", w[secondName]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Remove_NullOrEmptyName_Throws(string name)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentNullException>("name", () => w.Remove(name));
        }

        [Fact]
        public void Remove_IllegalCharacter_Throws()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentException>("name", () => w.Remove("{"));
        }

        [Fact]
        public void Remove_EmptyCollection_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Remove("foo");
            Assert.Equal(0, w.Count);
            Assert.Empty(w);
            Assert.Empty(w.AllKeys);
        }

        [Theory]
        [InlineData("name", "name")]
        [InlineData("name", "NaMe")]
        public void Remove_SetThenRemove_Success(string setName, string removeName)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w[setName] = "value";
            w.Remove(removeName);
            Assert.Equal(0, w.Count);
            Assert.Empty(w);
            Assert.Empty(w.AllKeys);
        }

        [Theory]
        [InlineData("name", "name")]
        [InlineData("name", "NaMe")]
        public void Remove_SetTwoThenRemoveOne_Success(string setName, string removeName)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w[setName] = "value";
            w["foo"] = "bar";
            w.Remove(removeName);
            Assert.Equal(1, w.Count);
            Assert.NotEmpty(w);
            Assert.NotEmpty(w.AllKeys);
            Assert.Equal(new[] { "foo" }, w.AllKeys);
            Assert.Equal("bar", w["foo"]);
        }

        [Fact]
        public void Getter_EmptyCollection_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Null(w["name"]);
            Assert.Equal(0, w.Count);
            Assert.Empty(w);
            Assert.Empty(w.AllKeys);
        }

        [Fact]
        public void Getter_NonEmptyCollectionNonExistentHeader_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w["name"] = "value";
            Assert.Null(w["foo"]);
            Assert.Equal(1, w.Count);
            Assert.NotEmpty(w);
            Assert.NotEmpty(w.AllKeys);
            Assert.Equal(new[] { "name" }, w.AllKeys);
            Assert.Equal("value", w["name"]);
        }

        [Fact]
        public void Getter_Success()
        {
            string[] keys = { "Accept", "uPgRaDe", "Custom" };
            string[] values = { "text/plain, text/html", " HTTP/2.0 , SHTTP/1.3,  , RTA/x11 ", "\"xyzzy\", \"r2d2xxxx\", \"c3piozzzz\"" };
            WebHeaderCollection w = new WebHeaderCollection();

            for (int i = 0; i < keys.Length; ++i)
            {
                string key = keys[i];
                string value = values[i];
                w[key] = value;
            }

            for (int i = 0; i < keys.Length; ++i)
            {
                string key = keys[i];
                string expected = values[i].Trim();
                Assert.Equal(expected, w[key]);
                Assert.Equal(expected, w[key.ToUpperInvariant()]);
                Assert.Equal(expected, w[key.ToLowerInvariant()]);
            }
        }

        [Fact]
        public void ToString_Empty_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Equal("\r\n", w.ToString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ToString_SingleHeaderWithEmptyValue_Success(string value)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w["name"] = value;
            Assert.Equal("name: \r\n\r\n", w.ToString());
        }

        [Fact]
        public void ToString_NotEmpty_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w["Accept"] = "text/plain";
            w["Content-Length"] = "123";
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
        public void Enumerator_Success()
        {
            string item1 = "Accept";
            string item2 = "Content-Length";
            string item3 = "Name";

            WebHeaderCollection w = new WebHeaderCollection();
            w[item1] = "text/plain";
            w[item2] = "123";
            w[item3] = "value";

            IEnumerable collection = w;
            IEnumerator e = collection.GetEnumerator();

            for (int i = 0; i < 2; i++)
            {
                // Not started
                Assert.Throws<InvalidOperationException>(() => e.Current);

                Assert.True(e.MoveNext());
                Assert.Same(item1, e.Current);

                Assert.True(e.MoveNext());
                Assert.Same(item2, e.Current);

                Assert.True(e.MoveNext());
                Assert.Same(item3, e.Current);

                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());

                // Ended
                Assert.Throws<InvalidOperationException>(() => e.Current);

                e.Reset();
            }
        }
    }
}
