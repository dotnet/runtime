// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;

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
            new object[] { "\u1234" }
        };

        [Theory, MemberData("InvalidNames")]
        public void CheckBadChars_InvalidName(string name)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentException>(() => w[name] = "test");
        }

        // #2370: this test should be combined with CheckBadChars_InvalidName once xUnit
        //        has been updated to avoid failures associated with logging text that
        //        contains low code points.
        [Fact]
        public void CheckBadChars_InvalidName_LowCodePoint()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentException>(() => w["\u0019"] = "test");
        }

        public static object[][] InvalidValues = {
            new object[] { "value1\rvalue2\r" },
            new object[] { "value1\nvalue2\r" },
            new object[] { "value1\u007fvalue2" },
            new object[] { "value1\r\nvalue2" }
        };

        [Theory, MemberData("InvalidValues")]
        public void CheckBadChars_InvalidValue(string value)
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentException>(() => w["custom"] = value);
        }

        // #2370: this test should be combined with CheckBadChars_InvalidValue once xUnit
        //        has been updated to avoid failures associated with logging text that
        //        contains low code points.
        [Fact]
        public void CheckBadChars_InvalidValue_LowCodePoint()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentException>(() => w["custom"] = "value1\u0019value2");
        }

        public static object[][] ValidValues = {
            new object[] { "value1\r\n" },
            new object[] { "value1\tvalue2" },
            new object[] { "value1\r\n\tvalue2" },
            new object[] { "value1\r\n value2" }
        };

        [Theory, MemberData("ValidValues")]
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
        }

        [Fact]
        public void GetValues_Success()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            w.Add("Accept", "text/plain, text/html");
            string[] values = w.GetValues("Accept");
            Assert.Equal(2, values.Length);
            Assert.Equal("text/plain", values[0]);
            Assert.Equal("text/html", values[1]);
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

    }
}
