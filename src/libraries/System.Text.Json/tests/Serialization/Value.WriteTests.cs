// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Newtonsoft.Json;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ValueTests
    {
        [Fact]
        public static void WriteStringWithRelaxedEscaper()
        {
            string inputString = ">><++>>>\">>\\>>&>>>\u6f22\u5B57>>>"; // Non-ASCII text should remain unescaped. \u6f22 = \u6C49, \u5B57 = \u5B57

            string actual = JsonSerializer.Serialize(inputString, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            string expected = "\">><++>>>\\\">>\\\\>>&>>>\u6f22\u5B57>>>\"";
            Assert.Equal(JsonConvert.SerializeObject(inputString), actual);
            Assert.Equal(expected, actual);
            Assert.NotEqual(expected, JsonSerializer.Serialize(inputString));
        }

        [Fact]
        public static void WritePrimitives()
        {
            {
                string json = JsonSerializer.Serialize(1);
                Assert.Equal("1", json);
            }

            {
                int? value = 1;
                string json = JsonSerializer.Serialize(value);
                Assert.Equal("1", json);
            }

            {
                int? value = null;
                string json = JsonSerializer.Serialize(value);
                Assert.Equal("null", json);
            }

            {
                string json = JsonSerializer.Serialize((string)null);
                Assert.Equal("null", json);
            }

            {
                Span<byte> json = JsonSerializer.SerializeToUtf8Bytes(1);
                Assert.Equal(Encoding.UTF8.GetBytes("1"), json.ToArray());
            }

            {
                string json = JsonSerializer.Serialize(long.MaxValue);
                Assert.Equal(long.MaxValue.ToString(), json);
            }

            {
                Span<byte> json = JsonSerializer.SerializeToUtf8Bytes(long.MaxValue);
                Assert.Equal(Encoding.UTF8.GetBytes(long.MaxValue.ToString()), json.ToArray());
            }

            {
                string json = JsonSerializer.Serialize("Hello");
                Assert.Equal(@"""Hello""", json);
            }

            {
                Span<byte> json = JsonSerializer.SerializeToUtf8Bytes("Hello");
                Assert.Equal(Encoding.UTF8.GetBytes(@"""Hello"""), json.ToArray());
            }

            {
                Uri uri = new Uri("https://domain/path");
                Assert.Equal(@"""https://domain/path""", JsonSerializer.Serialize(uri));
            }

            {
                Uri.TryCreate("~/path", UriKind.RelativeOrAbsolute, out Uri uri);
                Assert.Equal(@"""~/path""", JsonSerializer.Serialize(uri));
            }

            // The next two scenarios validate that we're NOT using Uri.ToString() for serializing Uri. The serializer
            // will escape backslashes and ampersands, but otherwise should be the same as the output of Uri.OriginalString.

            {
                // ToString would collapse the relative segment
                Uri uri = new Uri("http://a/b/../c");
                Assert.Equal(@"""http://a/b/../c""", JsonSerializer.Serialize(uri));
            }

            {
                // "%20" gets turned into a space by Uri.ToString()
                // https://coding.abel.nu/2014/10/beware-of-uri-tostring/
                Uri uri = new Uri("http://localhost?p1=Value&p2=A%20B%26p3%3DFooled!");
                Assert.Equal(@"""http://localhost?p1=Value\u0026p2=A%20B%26p3%3DFooled!""", JsonSerializer.Serialize(uri));
            }

            {
                Version version = new Version(1, 2);
                Assert.Equal(@"""1.2""", JsonSerializer.Serialize(version));
            }

            {
                Version version = new Version(1, 2, 3);
                Assert.Equal(@"""1.2.3""", JsonSerializer.Serialize(version));
            }

            {
                Version version = new Version(1, 2, 3, 4);
                Assert.Equal(@"""1.2.3.4""", JsonSerializer.Serialize(version));
            }
        }
    }
}
