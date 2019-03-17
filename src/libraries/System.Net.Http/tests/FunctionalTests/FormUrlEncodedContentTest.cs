// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public class FormUrlEncodedContentTest
    {
        private readonly ITestOutputHelper _output;

        public FormUrlEncodedContentTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Ctor_NullSource_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FormUrlEncodedContent(null));
        }

        [Fact]
        public async Task Ctor_EmptySource_Succeed()
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>());
            Stream stream = await content.ReadAsStreamAsync();
            Assert.Equal(0, stream.Length);
        }

        [Fact]
        public async Task Ctor_OneEntry_SeparatedByEquals()
        {
            var data = new Dictionary<string, string>();
            data.Add("key", "value");
            var content = new FormUrlEncodedContent(data);

            Stream stream = await content.ReadAsStreamAsync();
            Assert.Equal(9, stream.Length);
            string result = new StreamReader(stream).ReadToEnd();
            Assert.Equal("key=value", result);
        }

        [Fact]
        public async Task Ctor_OneUnicodeEntry_Encoded()
        {
            var data = new Dictionary<string, string>();
            data.Add("key", "valueク");
            var content = new FormUrlEncodedContent(data);

            Stream stream = await content.ReadAsStreamAsync();
            Assert.Equal(18, stream.Length);
            string result = new StreamReader(stream).ReadToEnd();
            Assert.Equal("key=value%E3%82%AF", result);
        }

        [Fact]
        public async Task Ctor_TwoEntries_SeparatedByAnd()
        {
            var data = new Dictionary<string, string>();
            data.Add("key1", "value1");
            data.Add("key2", "value2");
            var content = new FormUrlEncodedContent(data);

            Stream stream = await content.ReadAsStreamAsync();
            Assert.Equal(23, stream.Length);
            string result = new StreamReader(stream).ReadToEnd();
            Assert.Equal("key1=value1&key2=value2", result);
        }

        [Fact]
        public async Task Ctor_WithSpaces_EncodedAsPlus()
        {
            var data = new Dictionary<string, string>();
            data.Add("key 1", "val%20ue 1"); // %20 is a percent-encoded space, make sure it survives.
            data.Add("key 2", "val%ue 2");
            var content = new FormUrlEncodedContent(data);

            Stream stream = await content.ReadAsStreamAsync();
            Assert.Equal(35, stream.Length);
            string result = new StreamReader(stream).ReadToEnd();
            Assert.Equal("key+1=val%2520ue+1&key+2=val%25ue+2", result);
        }

        [Fact]
        public async Task Ctor_AllAsciiChars_EncodingMatchesHttpUtilty()
        {
            var builder = new StringBuilder();
            for (int ch = 0; ch < 128; ch++)
            {
                builder.Append((char)ch);
            }
            string testString = builder.ToString();

            var data = new Dictionary<string, string>();
            data.Add("key", testString);
            var content = new FormUrlEncodedContent(data);

            Stream stream = await content.ReadAsStreamAsync();
            string result = new StreamReader(stream).ReadToEnd().ToLowerInvariant();

            // Result of UrlEncode invoked in .NET Framework 4.6
            // string expectedResult = "key=" + HttpUtility.UrlEncode(testString).ToLowerInvariant();
            // HttpUtility is not part of ProjectK.

            string expectedResult = "key=%00%01%02%03%04%05%06%07%08%09%0a%0b%0c%0d%0e%0f%10%11%12%13%14%15%16%17%18" +
                "%19%1a%1b%1c%1d%1e%1f+!%22%23%24%25%26%27()*%2b%2c-.%2f0123456789%3a%3b%3c%3d%3e%3f%40abcdefghijklm" +
                "nopqrstuvwxyz%5b%5c%5d%5e_%60abcdefghijklmnopqrstuvwxyz%7b%7c%7d%7e%7f";

            string knownDiscrepancies = "~!*()";

            _output.WriteLine("Expecting result: '{0}'", expectedResult);
            _output.WriteLine("Actual result   : '{0}'", result);

            int discrepancies = 0;
            for (int i = 0; i < result.Length && i < expectedResult.Length; i++)
            {
                if (result[i] != expectedResult[i])
                {
                    Assert.True((result[i] == '%' || expectedResult[i] == '%'),
                        "Non-Escaping mis-match at position: " + i);

                    if (result[i] == '%')
                    {
                        Assert.True(knownDiscrepancies.Contains(expectedResult[i]),
                            "Escaped when it shouldn't be: " + expectedResult[i] + " at position " + i);
                        result = result.Substring(i + 3);
                        expectedResult = expectedResult.Substring(i + 1);
                    }
                    else
                    {
                        Assert.True(knownDiscrepancies.Contains(result[i]),
                            "Not escaped when it should be : " + result[i] + " at position " + i);
                        result = result.Substring(i + 1);
                        expectedResult = expectedResult.Substring(i + 3);
                    }
                    i = -1;
                    discrepancies++;
                }
            }
            Assert.Equal(5, discrepancies);
        }
    }
}
