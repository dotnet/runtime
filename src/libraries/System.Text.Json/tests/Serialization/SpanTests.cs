// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class SpanTests
    {
        [Fact]
        public static void ParseNullTypeFail()
        {
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Parse(new ReadOnlySpan<byte>(), (Type)null));
        }

        [Theory]
        [MemberData(nameof(ReadSuccessCases))]
        public static void Read(Type classType, byte[] data)
        {
            object obj = JsonSerializer.Parse(data, classType);
            Assert.IsAssignableFrom(typeof(ITestClass), obj);
            ((ITestClass)obj).Verify();
        }

        [Theory]
        [MemberData(nameof(ReadSuccessCases))]
        public static void ReadFromStream(Type classType, byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            object obj = JsonSerializer.ReadAsync(
                stream,
                classType).Result;

            Assert.IsAssignableFrom(typeof(ITestClass), obj);
            ((ITestClass)obj).Verify();

            // Try again with a smaller initial buffer size to ensure we handle incomplete data
            stream = new MemoryStream(data);
            obj = JsonSerializer.ReadAsync(
                stream,
                classType,
                new JsonSerializerOptions { DefaultBufferSize = 5 }).Result;

            Assert.IsAssignableFrom(typeof(ITestClass), obj);
            ((ITestClass)obj).Verify();
        }

        [Fact]
        public static void ReadGenericApi()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(SimpleTestClass.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ParseUntyped()
        {
            byte[] bytes = Encoding.UTF8.GetBytes("42");
            object obj = JsonSerializer.Parse(bytes, typeof(object));
            Assert.IsType<JsonElement>(obj);
            JsonElement element = (JsonElement)obj;
            Assert.Equal(JsonValueType.Number, element.Type);
            Assert.Equal(42, element.GetInt32());
        }

        [Fact]
        public static void ToStringNullTypeFail()
        {
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.ToString(new object(), (Type)null));
        }

        [Fact]
        public static void VerifyTypeFail()
        {
            Assert.Throws<ArgumentException>(() => JsonSerializer.ToString(1, typeof(string)));
        }

        [Fact]
        public static void NullObjectOutput()
        {
            byte[] encodedNull = Encoding.UTF8.GetBytes(@"null");

            {
                byte[] output = JsonSerializer.ToUtf8Bytes(null, null);
                Assert.Equal(encodedNull, output);
            }

            {
                byte[] output = JsonSerializer.ToUtf8Bytes(null, typeof(NullTests));
                Assert.Equal(encodedNull, output);
            }
        }

        public static IEnumerable<object[]> ReadSuccessCases
        {
            get
            {
                return TestData.ReadSuccessCases;
            }
        }
    }
}
