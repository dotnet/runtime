// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;

using Xunit;

namespace System.PrivateUri.Tests
{
    public class IriEncodingDecodingTest
    {
        // This array contains potentially problematic URI data strings and their canonical encoding.
        private static string[,] RFC3986CompliantDecoding =
        {
            { "%3B%2F%3F%3A%40%26%3D%2B%24%2C", "%3B%2F%3F%3A%40%26%3D%2B%24%2C" }, // Encoded RFC 2396 Reserved Marks.
            { @"-_.~", @"-_.~" }, // RFC3986 Unreserved Marks.
            { "%2D%5F%2E%7E", @"-_.~" }, // Encoded RFC 3986 Unreserved Marks.
            { "%2F%3F%3A%40%23%5B%5D", "%2F%3F%3A%40%23%5B%5D" }, // Encoded RFC 3986 Gen Delims.
            { @";&=+$,!'()*", @";&=+$,!'()*" }, // RFC 3986 Sub Delims.
            { "%3B%26%3D%2B%24%2C%21%27%28%29%2A", "%3B%26%3D%2B%24%2C%21%27%28%29%2A" }, // Encoded RFC3986 Sub Delims.
            { "%E2%80%8F%E2%80%8E%E2%80%AA%E2%80%AB%E2%80%AC%E2%80%AD%E2%80%AE",
                "%E2%80%8F%E2%80%8E%E2%80%AA%E2%80%AB%E2%80%AC%E2%80%AD%E2%80%AE" }, // Encoded Unicode Bidi Control Characters.
            { "\u200E\u200F\u202A\u202B\u202C\u202D\u202E",
                "%E2%80%8E%E2%80%8F%E2%80%AA%E2%80%AB%E2%80%AC%E2%80%AD%E2%80%AE" }, // Unencoded Unicode Bidi Control Characters
        };

        [Fact]
        public void AbsoluteUri_DangerousPathSymbols_RFC3986CompliantAbsoluteUri()
        {
            Uri uri;
            string baseUri = "http://a/%C3%88/";
            for (int i = 0; i < RFC3986CompliantDecoding.GetLength(0); i++)
            {
                string sourceStr = baseUri + RFC3986CompliantDecoding[i, 0];
                uri = new Uri(sourceStr);
                Assert.Equal(baseUri + RFC3986CompliantDecoding[i, 1], uri.AbsoluteUri);
            }
        }

        [Fact]
        public void AbsolutePath_DangerousPathSymbols_RFC3986CompliantAbsolutePath()
        {
            Uri uri;
            string host = "http://a";
            string path = "/%C3%88/";
            for (int i = 0; i < RFC3986CompliantDecoding.GetLength(0); i++)
            {
                string sourceStr = host + path + RFC3986CompliantDecoding[i, 0];
                uri = new Uri(sourceStr);
                Assert.Equal(path + RFC3986CompliantDecoding[i, 1], uri.AbsolutePath);
            }
        }

        [Fact]
        public void Segments_DangerousPathSymbols_RFC3986CompliantPathSegments()
        {
            Uri uri;
            string host = "http://a";
            string path = "/%C3%88/";
            for (int i = 0; i < RFC3986CompliantDecoding.GetLength(0); i++)
            {
                string sourceStr = host + path + RFC3986CompliantDecoding[i, 0];
                uri = new Uri(sourceStr);
                Assert.Equal(path + RFC3986CompliantDecoding[i, 1], string.Join(string.Empty, uri.Segments));
            }
        }

        [Fact]
        public void Equality_DangerousPathSymbols_RFC3986CompliantEquality()
        {
            string baseUri = "http://a/%C3%88/";
            for (int i = 0; i < RFC3986CompliantDecoding.GetLength(0); i++)
            {
                Uri uri1 = new Uri(baseUri + RFC3986CompliantDecoding[i, 0]);
                Uri uri2 = new Uri(baseUri + RFC3986CompliantDecoding[i, 1]);
                Assert.Equal(uri1, uri2);
            }
        }

        [Fact]
        public void PathAndQuery_DangerousQuerySymbols_RFC3986CompliantPathAndQuery()
        {
            Uri uri;
            string host = "http://a";
            string path = "/%C3%88/";
            for (int i = 0; i < RFC3986CompliantDecoding.GetLength(0); i++)
            {
                string sourceStr = host + path + "?" + RFC3986CompliantDecoding[i, 0];
                uri = new Uri(sourceStr);
                Assert.Equal(path + "?" + RFC3986CompliantDecoding[i, 1], uri.PathAndQuery);
            }
        }

        [Fact]
        public void Query_DangerousQuerySymbols_RFC3986CompliantQuery()
        {
            Uri uri;
            string baseUri = "http://a/%C3%88/";
            for (int i = 0; i < RFC3986CompliantDecoding.GetLength(0); i++)
            {
                string sourceStr = baseUri + "?" + RFC3986CompliantDecoding[i, 0];
                uri = new Uri(sourceStr);
                Assert.Equal("?" + RFC3986CompliantDecoding[i, 1], uri.Query);
            }
        }

        [Fact]
        public void Equality_DangerousQuerySymbols_RFC3986CompliantEquality()
        {
            string baseUri = "http://a/%C3%88/";
            for (int i = 0; i < RFC3986CompliantDecoding.GetLength(0); i++)
            {
                Uri uriTest = new Uri(baseUri + "?" + RFC3986CompliantDecoding[i, 0]);
                Uri uriTest1 = new Uri(baseUri + "?" + RFC3986CompliantDecoding[i, 1]);
                Assert.Equal(uriTest, uriTest1);
            }
        }

        [Fact]
        public void Fragment_DangerousFragmentSymbols_RFC3986CompliantFragment()
        {
            Uri uri;
            string baseUri = "http://a/%C3%88/";
            for (int i = 0; i < RFC3986CompliantDecoding.GetLength(0); i++)
            {
                string sourceStr = baseUri + "#" + RFC3986CompliantDecoding[i, 0];
                uri = new Uri(sourceStr);
                Assert.Equal("#" + RFC3986CompliantDecoding[i, 1], uri.Fragment);
            }
        }

        [Fact]
        public void UserInfo_DangerousUserInfoSymbols_RFC3986CompliantUserInfo()
        {
            Uri uri;
            string baseUri = "http://";
            string host = "a";
            string path = "/%C3%88/";
            for (int i = 0; i < RFC3986CompliantDecoding.GetLength(0); i++)
            {
                string sourceStr = baseUri + RFC3986CompliantDecoding[i, 0] + "@" + host + path;
                uri = new Uri(sourceStr);
                Assert.Equal(RFC3986CompliantDecoding[i, 1], uri.UserInfo);
            }
        }
    }
}
