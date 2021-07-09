// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize(new ReadOnlySpan<byte>(), (Type)null));
        }

        [Theory]
        [MemberData(nameof(ReadSuccessCases))]
        public static void Read(Type classType, byte[] data)
        {
            var options = new JsonSerializerOptions { IncludeFields = true };
            object obj = JsonSerializer.Deserialize(data, classType, options);
            Assert.IsAssignableFrom<ITestClass>(obj);
            ((ITestClass)obj).Verify();
        }

        [Theory]
        [MemberData(nameof(ReadSuccessCases))]
        public static void ReadFromStream(Type classType, byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            var options = new JsonSerializerOptions { IncludeFields = true };
            object obj = JsonSerializer.DeserializeAsync(
                stream,
                classType,
                options).Result;

            Assert.IsAssignableFrom<ITestClass>(obj);
            ((ITestClass)obj).Verify();

            // Try again with a smaller initial buffer size to ensure we handle incomplete data
            stream = new MemoryStream(data);
            obj = JsonSerializer.DeserializeAsync(
                stream,
                classType,
                new JsonSerializerOptions { DefaultBufferSize = 5, IncludeFields = true }).Result;

            Assert.IsAssignableFrom<ITestClass>(obj);
            ((ITestClass)obj).Verify();
        }

        [Fact]
        public static void ReadGenericApi()
        {
            SimpleTestClass obj = JsonSerializer.Deserialize<SimpleTestClass>(SimpleTestClass.s_data);
            obj.Verify();
        }

        [Fact]
        public static void ParseUntyped()
        {
            byte[] bytes = Encoding.UTF8.GetBytes("42");
            object obj = JsonSerializer.Deserialize(bytes, typeof(object));
            Assert.IsType<JsonElement>(obj);
            JsonElement element = (JsonElement)obj;
            Assert.Equal(JsonValueKind.Number, element.ValueKind);
            Assert.Equal(42, element.GetInt32());
        }

        [Fact]
        public static void ToStringNullTypeFail()
        {
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(new object(), (Type)null));
        }

        [Fact]
        public static void VerifyTypeFail()
        {
            Assert.Throws<ArgumentException>(() => JsonSerializer.Serialize(1, typeof(string)));
        }

        [Fact]
        public static void NullObjectOutput()
        {
            byte[] encodedNull = Encoding.UTF8.GetBytes(@"null");

            {
                Assert.Throws<ArgumentNullException>(() => JsonSerializer.SerializeToUtf8Bytes(null, null));
            }

            {
                byte[] output = JsonSerializer.SerializeToUtf8Bytes(null, typeof(NullTests));
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
