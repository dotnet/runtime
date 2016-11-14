// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Tests;

using Xunit;

namespace System.Net.WebHeaderCollectionTests
{
    public partial class WebHeaderCollectionTest
    {
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

        [Theory]
        [MemberData(nameof(SerializeDeserialize_Roundtrip_MemberData))]
        public void SerializeDeserialize_Roundtrip(WebHeaderCollection c)
        {
            Assert.Equal(c, BinaryFormatterHelpers.Clone(c));
        }

        [Fact]
        public void HttpRequestHeader_Add_Remove_Success()
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
            Assert.Throws<ArgumentException>(() => w.Add(HttpRequestHeader.ContentLength,maxStr));
            Assert.Throws<ArgumentException>(() => w.Add("ContentLength", maxStr));
        }

        [Fact]
        public void HttpRequestHeader_AddMissingColon_Failure()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentException>(() => w.Add("ContentType#text/html"));
        }

        [Fact]
        public void HttpRequestHeader_Remove_Failure()
        {
            WebHeaderCollection w = new WebHeaderCollection();
            Assert.Throws<ArgumentNullException>(() => w.Remove(null));
        }
    }
}
