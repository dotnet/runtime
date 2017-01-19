// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;

namespace System.Resources.Tests
{
    public static class StaticResources 
    {
        /// <summary>
        ///  An empty .resources file in base64 created with ResourceWriter on full .NET framework
        /// </summary>
        public const string Empty = "zsrvvgEAAACRAAAAbFN5c3RlbS5SZXNvdXJjZXMuUmVzb3VyY2VSZWFkZXIsIG1zY29ybGliLCBWZXJzaW9uPTQuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49Yjc3YTVjNTYxOTM0ZTA4OSNTeXN0ZW0uUmVzb3VyY2VzLlJ1bnRpbWVSZXNvdXJjZVNldAIAAAAAAAAAAAAAAFBBRFBBRFC0AAAA";

        /// <summary>
        ///  A .resources file in base64 with the following keys:
        ///    String: "message"
        ///    Int: (object)42
        ///    Float: (object)3.14159
        ///    Bytes: new byte[]{ 41, 42, 43, 44, 192, 168, 1, 1 }
        ///    ByteStream: new UnmanagedMemoryStream(new byte[]{ 41, 42, 43, 44, 192, 168, 1, 1 })
        /// </summary>
        public const string WithData = "zsrvvgEAAACRAAAAbFN5c3RlbS5SZXNvdXJjZXMuUmVzb3VyY2VSZWFkZXIsIG1zY29ybGliLCBWZXJzaW9uPTQuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49Yjc3YTVjNTYxOTM0ZTA4OSNTeXN0ZW0uUmVzb3VyY2VzLlJ1bnRpbWVSZXNvdXJjZVNldAIAAAAFAAAAAAAAAFBBRFBBRFCTxNurUOnkxTbThwtVRFcMfHGiDAAAAABCAAAANwAAACgAAAAZAAAALwEAABRCAHkAdABlAFMAdAByAGUAYQBtAAAAAAAKQgB5AHQAZQBzAA0AAAAKRgBsAG8AYQB0ABoAAAAGSQBuAHQAIwAAAAxTAHQAcgBpAG4AZwAoAAAAIQgAAAApKisswKgBASAIAAAAKSorLMCoAQENboYb8PkhCUAIKgAAAAEHbWVzc2FnZQ==";
    }

    public abstract class ResourceSetTests
    {
        public abstract ResourceSet GetSet(string base64Data);

        [Fact]
        public void GetDefaultReader()
        {
            var set = GetSet(StaticResources.Empty);
            Assert.Equal(typeof(ResourceReader), set.GetDefaultReader());
        }

        [Fact]
        public void GetDefaultWriter()
        {
            var set = GetSet(StaticResources.Empty);
            Assert.Equal(typeof(ResourceWriter), set.GetDefaultWriter());
        }

        [Fact]
        public void EnumerateEmpty()
        {
            var set = GetSet(StaticResources.Empty);
            var enumerator = set.GetEnumerator();
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void Enumerate()
        {
            var set = GetSet(StaticResources.WithData);
            var keys = new List<string>
            {
                "String",
                "Int",
                "Float",
                "Bytes",
                "ByteStream"
            };
            var enumerator = set.GetEnumerator();
            var idx = 0;
            while (enumerator.MoveNext())
            {
                Assert.Contains((string)enumerator.Key, keys);
                idx++;
            }
            Assert.Equal(keys.Count, idx);
        }

        public static IEnumerable<object[]> EnglishResourceData()
        {
            yield return new object[] { "String", "message" };
            yield return new object[] { "Int", 42 };
            yield return new object[] { "Float", 3.14159 };
            yield return new object[] { "Bytes", new byte[] { 41, 42, 43, 44, 192, 168, 1, 1 } };
        }

        [Theory]
        [MemberData(nameof(EnglishResourceData))]
        public void GetObject(string key, object expectedValue)
        {
            var set = GetSet(StaticResources.WithData);
            Assert.Equal(expectedValue, set.GetObject(key));
            Assert.Equal(expectedValue, set.GetObject(key.ToLower(), true));
        }

        [Fact]
        public void GetString()
        {
            var set = GetSet(StaticResources.WithData);
            Assert.Equal("message", set.GetString("String"));
            Assert.Equal("message", set.GetString("string", true));
        }
    }

    public class ResourceSetTests_StreamCtor : ResourceSetTests
    {
        public override ResourceSet GetSet(string base64Data)
        {
            return new ResourceSet(new MemoryStream(Convert.FromBase64String(base64Data)));
        }
    }

    public class ResourceSetTests_ResourceReaderCtor : ResourceSetTests
    {
        public override ResourceSet GetSet(string base64Data)
        {
            return new ResourceSet(new ResourceReader(new MemoryStream(Convert.FromBase64String(base64Data))));
        }
    }
}
