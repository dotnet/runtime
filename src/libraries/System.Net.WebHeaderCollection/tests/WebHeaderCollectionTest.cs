// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

using Xunit;

namespace System.Net.Tests
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
            AssertExtensions.Throws<ArgumentNullException>("name", () => w[name] = "test");
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
            AssertExtensions.Throws<ArgumentException>("name", () => w[name] = "test");
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
            AssertExtensions.Throws<ArgumentException>("value", () => w["custom"] = value);
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
        [InlineData("name")]
        [InlineData("nAMe")]
        public void Remove_HeaderExists_RemovesFromCollection(string name)
        {
            var headers = new WebHeaderCollection()
            {
                { "name", "value" }
            };
            headers.Remove(name);
            Assert.Empty(headers);

            headers.Remove(name);
            Assert.Empty(headers);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Remove_NullOrEmptyHeader_ThrowsArgumentNullException(string name)
        {
            var headers = new WebHeaderCollection();
            AssertExtensions.Throws<ArgumentNullException>("name", () => headers.Remove(name));
        }

        [Theory]
        [InlineData(" \r \t \n")]
        [InlineData("  name  ")]
        [MemberData(nameof(InvalidValues))]
        public void Remove_InvalidHeader_ThrowsArgumentException(string name)
        {
            var headers = new WebHeaderCollection();
            AssertExtensions.Throws<ArgumentException>("name", () => headers.Remove(name));
        }

        [Fact]
        public void Remove_IllegalCharacter_Throws()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            AssertExtensions.Throws<ArgumentException>("name", () => w.Remove("{"));
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

        public static IEnumerable<object[]> SerializeDeserialize_Roundtrip_MemberData()
        {
            for (int i = 0; i < 10; i++)
            {
                var wc = new WebHeaderCollection();
                for (int j = 0; j < i; j++)
                {
                    wc[$"header{j}"] = $"value{j}";
                }
                yield return new object[] { wc };
            }
        }

        public static IEnumerable<object[]> Add_Value_TestData()
        {
            yield return new object[] { null, string.Empty };
            yield return new object[] { string.Empty, string.Empty };
            yield return new object[] { "VaLue", "VaLue" };
            yield return new object[] { "  value  ", "value" };

            // Documentation says this should fail but it does not.
            string longString = new string('a', 65536);
            yield return new object[] { longString, longString };
        }

        [Theory]
        [MemberData(nameof(Add_Value_TestData))]
        public void Add_ValidValue_Success(string value, string expectedValue)
        {
            var headers = new WebHeaderCollection
            {
                { "name", value }
            };

            Assert.Equal(expectedValue, headers["name"]);
        }

        [Fact]
        public void Add_HeaderAlreadyExists_AppendsValue()
        {
            var headers = new WebHeaderCollection
            {
                { "name", "value1" },
                { "name", null },
                { "name", "value2" },
                { "NAME", "value3" },
                { "name", "" }
            };
            Assert.Equal("value1,,value2,value3,", headers["name"]);
        }

        [Fact]
        public void Add_NullName_ThrowsArgumentNullException()
        {
            var headers = new WebHeaderCollection();
            AssertExtensions.Throws<ArgumentNullException>("name", () => headers.Add(null, "value"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("(")]
        [InlineData("\r \t \n")]
        [InlineData("  name  ")]
        [MemberData(nameof(InvalidValues))]
        public void Add_InvalidName_ThrowsArgumentException(string name)
        {
            var headers = new WebHeaderCollection();
            AssertExtensions.Throws<ArgumentException>("name", () => headers.Add(name, "value"));
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void Add_InvalidValue_ThrowsArgumentException(string value)
        {
            var headers = new WebHeaderCollection();
            AssertExtensions.Throws<ArgumentException>("value", () => headers.Add("name", value));
        }

        [Fact]
        public void Add_ValidHeader_AddsToHeaders()
        {
            var headers = new WebHeaderCollection()
            {
                "name:value1",
                "name:",
                "NaMe:value2",
                "name:  ",
            };
            Assert.Equal("value1,,value2,", headers["name"]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Add_NullHeader_ThrowsArgumentNullException(string header)
        {
            var headers = new WebHeaderCollection();
            AssertExtensions.Throws<ArgumentNullException>("header", () => headers.Add(header));
        }

        [Theory]
        [InlineData(" \r \t \n", "header")]
        [InlineData("nocolon", "header")]
        [InlineData("  :value", "name")]
        [InlineData("name  :value", "name")]
        [InlineData("name:va\rlue", "value")]
        public void Add_InvalidHeader_ThrowsArgumentException(string header, string paramName)
        {
            var headers = new WebHeaderCollection();
            AssertExtensions.Throws<ArgumentException>(paramName, () => headers.Add(header));
        }

        private const string HeaderType = "Set-Cookie";
        private const string Cookie1 = "locale=en; path=/; expires=Fri, 05 Oct 2018 06:28:57 -0000";
        private const string Cookie2 = "uuid=123abc; path=/; expires=Fri, 05 Oct 2018 06:28:57 -0000; secure; HttpOnly";
        private const string Cookie3 = "country=US; path=/; expires=Fri, 05 Oct 2018 06:28:57 -0000";
        private const string Cookie4 = "m_session=session1; path=/; expires=Sun, 08 Oct 2017 00:28:57 -0000; secure; HttpOnly";

        private const string Cookie1NoAttribute = "locale=en";
        private const string Cookie2NoAttribute = "uuid=123abc";
        private const string Cookie3NoAttribute = "country=US";
        private const string Cookie4NoAttribute = "m_session=session1";

        private const string CookieInvalid = "helloWorld";

        [Fact]
        public void GetValues_MultipleSetCookieHeadersWithExpiresAttribute_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add(HeaderType, Cookie1);
            w.Add(HeaderType, Cookie2);
            w.Add(HeaderType, Cookie3);
            w.Add(HeaderType, Cookie4);

            string[] values = w.GetValues(HeaderType);
            Assert.Equal(4, values.Length);
            Assert.Equal(Cookie1, values[0]);
            Assert.Equal(Cookie2, values[1]);
            Assert.Equal(Cookie3, values[2]);
            Assert.Equal(Cookie4, values[3]);
        }

        [Fact]
        public void GetValues_SingleSetCookieHeaderWithMultipleCookiesWithExpiresAttribute_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add(HeaderType, Cookie1 + "," + Cookie2 + "," + Cookie3 + "," + Cookie4);

            string[] values = w.GetValues(HeaderType);
            Assert.Equal(4, values.Length);
            Assert.Equal(Cookie1, values[0]);
            Assert.Equal(Cookie2, values[1]);
            Assert.Equal(Cookie3, values[2]);
            Assert.Equal(Cookie4, values[3]);
        }

        [Fact]
        public void GetValues_MultipleSetCookieHeadersWithNoAttribute_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add(HeaderType, Cookie1NoAttribute);
            w.Add(HeaderType, Cookie2NoAttribute);
            w.Add(HeaderType, Cookie3NoAttribute);
            w.Add(HeaderType, Cookie4NoAttribute);

            string[] values = w.GetValues(HeaderType);
            Assert.Equal(4, values.Length);
            Assert.Equal(Cookie1NoAttribute, values[0]);
            Assert.Equal(Cookie2NoAttribute, values[1]);
            Assert.Equal(Cookie3NoAttribute, values[2]);
            Assert.Equal(Cookie4NoAttribute, values[3]);
        }

        [Fact]
        public void GetValues_SingleSetCookieHeaderWithMultipleCookiesWithNoAttribute_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add(HeaderType, Cookie1NoAttribute + "," + Cookie2NoAttribute + "," + Cookie3NoAttribute + "," + Cookie4NoAttribute);

            string[] values = w.GetValues(HeaderType);
            Assert.Equal(4, values.Length);
            Assert.Equal(Cookie1NoAttribute, values[0]);
            Assert.Equal(Cookie2NoAttribute, values[1]);
            Assert.Equal(Cookie3NoAttribute, values[2]);
            Assert.Equal(Cookie4NoAttribute, values[3]);
        }

        [Fact]
        public void GetValues_InvalidSetCookieHeader_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add(HeaderType, CookieInvalid);

            string[] values = w.GetValues(HeaderType);
            Assert.Equal(0, values.Length);
        }

        [Fact]
        public void GetValues_MultipleValuesHeader_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            string headerType = "Accept";
            w.Add(headerType, "text/plain, text/html");
            string[] values = w.GetValues(headerType);
            Assert.Equal(2, values.Length);
            Assert.Equal("text/plain", values[0]);
            Assert.Equal("text/html", values[1]);
        }

        [Fact]
        public void HttpRequestHeader_Add_Rmemove_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add(HttpRequestHeader.Warning, "Warning1");

            Assert.Equal(1, w.Count);
            Assert.Equal("Warning1", w[HttpRequestHeader.Warning]);
            Assert.Equal("Warning", w.AllKeys[0]);

            w.Remove(HttpRequestHeader.Warning);
            Assert.Equal(0, w.Count);
        }

        [Fact]
        public void HttpRequestHeader_Get_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add("header1", "value1");
            w.Add("header1", "value2");
            string[] values = w.GetValues(0);
            Assert.Equal("value1", values[0]);
            Assert.Equal("value2", values[1]);
        }

        [Fact]
        public void HttpRequestHeader_ToByteArray_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add("header1", "value1");
            w.Add("header1", "value2");
            byte[] byteArr = w.ToByteArray();
            Assert.NotEmpty(byteArr);
        }

        [Fact]
        public void HttpRequestHeader_GetKey_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add("header1", "value1");
            w.Add("header1", "value2");
            Assert.NotEmpty(w.GetKey(0));
        }

        [Fact]
        public void HttpRequestHeader_GetValues_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add("header1", "value1");
            Assert.Equal("value1", w.GetValues("header1")[0]);
        }

        [Fact]
        public void HttpRequestHeader_Clear_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add("header1", "value1");
            w.Add("header1", "value2");
            w.Clear();
            Assert.Equal(0, w.Count);
        }

        [Fact]
        public void HttpRequestHeader_IsRestricted_Success()
        {
            Assert.True(WebHeaderCollection.IsRestricted("Accept"));
            Assert.False(WebHeaderCollection.IsRestricted("Age"));
            Assert.False(WebHeaderCollection.IsRestricted("Accept", true));
        }

        [Fact]
        public void HttpRequestHeader_AddHeader_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add(HttpRequestHeader.ContentLength, "10");
            w.Add(HttpRequestHeader.ContentType, "text/html");
            Assert.Equal(2,w.Count);
        }

        [Fact]
        public void WebHeaderCollection_Keys_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add(HttpRequestHeader.ContentLength, "10");
            w.Add(HttpRequestHeader.ContentType, "text/html");
            Assert.Equal(2, w.Keys.Count);
        }

        [Fact]
        public void HttpRequestHeader_AddHeader_Failure()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            char[] arr = new char[ushort.MaxValue + 1];
            string maxStr = new string(arr);
            AssertExtensions.Throws<ArgumentException>("value", () => w.Add(HttpRequestHeader.ContentLength,maxStr));
            AssertExtensions.Throws<ArgumentException>("value", () => w.Add("ContentLength", maxStr));
        }

        [Fact]
        public void HttpResponseHeader_Set_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Set(HttpResponseHeader.ProxyAuthenticate, "value123");

            Assert.Equal("value123", w[HttpResponseHeader.ProxyAuthenticate]);
        }

        [Fact]
        public void HttpRequestHeader_Set_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Set(HttpRequestHeader.Connection, "keep-alive");

            Assert.Equal(1, w.Count);
            Assert.Equal("keep-alive", w[HttpRequestHeader.Connection]);
            Assert.Equal("Connection", w.AllKeys[0]);
        }

        [Fact]
        public void NameValue_Set_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Set("firstName", "first");
            Assert.Equal(1, w.Count);
            Assert.NotEmpty(w);
            Assert.NotEmpty(w.AllKeys);
            Assert.Equal(new[] { "firstName" }, w.AllKeys);
            Assert.Equal("first", w["firstName"]);
        }

        [Fact]
        public void AddLongString_DoesNotThrow()
        {
            string longString = new string('a', 65536);
            WebHeaderCollection headerCollection = new WebHeaderCollection();

            headerCollection.Add("Long-Header", longString);
            headerCollection["Long-Header-2"] = longString;

            headerCollection.Add(HttpResponseHeader.SetCookie, "someValueToChangeType"); // this will implicitly change _type

            headerCollection.Add("Long-Header-3", longString);
            headerCollection["Long-Header-4"] = longString;

            Assert.Equal(longString, headerCollection["Long-Header"]);
            Assert.Equal(longString, headerCollection["Long-Header-2"]);
            Assert.Equal(longString, headerCollection["Long-Header-3"]);
            Assert.Equal(longString, headerCollection["Long-Header-4"]);
        }
    }
}
