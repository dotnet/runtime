// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

namespace System.Resources.Tests
{
    public static class StaticResources
    {
        /// <summary>
        ///  An empty .resources file in base64 created with ResourceWriter on .NET Framework
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

    public class ResourceSetTests_IResourceReader
    {
        class SimpleResourceReader : IResourceReader
        {
            Hashtable data;
            public SimpleResourceReader()
            {
                data = new Hashtable();
                data.Add(1, "invalid");
                data.Add("String", "message");
                data.Add("Int32", 5);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotSupportedException();
            }

            public IDictionaryEnumerator GetEnumerator()
            {
                return data.GetEnumerator();
            }

            public void Close()
            {
            }

            public void Dispose()
            {
            }
        }

        [Fact]
        public void Empty_Ctor()
        {
            Assert.Throws<ArgumentNullException>(() => new ResourceSet(null as IResourceReader));
        }

        [Fact]
        public void GetObject()
        {
            var rs = new ResourceSet(new SimpleResourceReader());

            Assert.Null(rs.GetObject("DoesNotExist"));
            Assert.Null(rs.GetObject("1"));

            Assert.Equal(5, rs.GetObject("Int32"));
            Assert.Equal(5, rs.GetObject("int32", true));
        }

        [Fact]
        public void GetString()
        {
            var rs = new ResourceSet(new SimpleResourceReader());

            Assert.Null(rs.GetString("DoesNotExist"));
            Assert.Null(rs.GetString("1"));

            Assert.Equal("message", rs.GetString("String"));
            Assert.Equal("message", rs.GetString("string", true));
        }

        [Fact]
        public void GetEnumerator()
        {
            var rs = new ResourceSet(new SimpleResourceReader());

            var expected = new HashSet<object>() {
                1, "String", "Int32"
            };

            foreach (DictionaryEntry entry in rs)
            {
                Assert.Contains(entry.Key, expected);
                expected.Remove(entry.Key);
            }

            Assert.Equal(0, expected.Count);
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
