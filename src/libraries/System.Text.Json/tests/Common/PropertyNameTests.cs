// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is saved as Unicode in order to test inline (not escaped) unicode characters.

using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class PropertyNameTests : SerializerTests
    {
        public PropertyNameTests(JsonSerializerWrapper serializerWrapper) : base(serializerWrapper) { }

        [Fact]
        public async Task CamelCaseDeserializeNoMatch()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            SimpleTestClass obj = await Serializer.DeserializeWrapper<SimpleTestClass>(@"{""MyInt16"":1}", options);

            // This is 0 (default value) because the data does not match the property "MyInt16" that is assuming camel-casing of "myInt16".
            Assert.Equal(0, obj.MyInt16);
        }

        [Fact]
        public async Task CamelCaseDeserializeMatch()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            SimpleTestClass obj = await Serializer.DeserializeWrapper<SimpleTestClass>(@"{""myInt16"":1}", options);

            // This is 1 because the data matches the property "MyInt16" that is assuming camel-casing of "myInt16".
            Assert.Equal(1, obj.MyInt16);
        }

        [Fact]
        public async Task CamelCaseSerialize()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            SimpleTestClass obj = await Serializer.DeserializeWrapper<SimpleTestClass>(@"{}", options);

            string json = await Serializer.SerializeWrapper(obj, options);
            Assert.Contains(@"""myInt16"":0", json);
            Assert.Contains(@"""myInt32"":0", json);
        }

        [Fact]
        public async Task CustomNamePolicy()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = new UppercaseNamingPolicy();

            SimpleTestClass obj = await Serializer.DeserializeWrapper<SimpleTestClass>(@"{""MYINT16"":1}", options);

            // This is 1 because the data matches the property "MYINT16" that is uppercase of "myInt16".
            Assert.Equal(1, obj.MyInt16);
        }

        [Fact]
        public async Task NullNamePolicy()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = new NullNamingPolicy();

            // A policy that returns null is not allowed.
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<SimpleTestClass>(@"{}", options));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(new SimpleTestClass(), options));
        }

        [Fact]
        public async Task IgnoreCase()
        {
            {
                // A non-match scenario with no options (case-sensitive by default).
                SimpleTestClass obj = await Serializer.DeserializeWrapper<SimpleTestClass>(@"{""myint16"":1}");
                Assert.Equal(0, obj.MyInt16);
            }

            {
                // A non-match scenario with default options (case-sensitive by default).
                var options = new JsonSerializerOptions();
                SimpleTestClass obj = await Serializer.DeserializeWrapper<SimpleTestClass>(@"{""myint16"":1}", options);
                Assert.Equal(0, obj.MyInt16);
            }

            {
                var options = new JsonSerializerOptions();
                options.PropertyNameCaseInsensitive = true;
                SimpleTestClass obj = await Serializer.DeserializeWrapper<SimpleTestClass>(@"{""myint16"":1}", options);
                Assert.Equal(1, obj.MyInt16);
            }
        }

        [Fact]
        public async Task JsonPropertyNameAttribute()
        {
            {
                OverridePropertyNameDesignTime_TestClass obj = await Serializer.DeserializeWrapper<OverridePropertyNameDesignTime_TestClass>(@"{""Blah"":1}");
                Assert.Equal(1, obj.myInt);

                obj.myObject = 2;

                string json = await Serializer.SerializeWrapper(obj);
                Assert.Contains(@"""Blah"":1", json);
                Assert.Contains(@"""BlahObject"":2", json);
            }

            // The JsonPropertyNameAttribute should be unaffected by JsonNamingPolicy and PropertyNameCaseInsensitive.
            {
                var options = new JsonSerializerOptions();
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.PropertyNameCaseInsensitive = true;

                OverridePropertyNameDesignTime_TestClass obj = await Serializer.DeserializeWrapper<OverridePropertyNameDesignTime_TestClass>(@"{""Blah"":1}", options);
                Assert.Equal(1, obj.myInt);

                string json = await Serializer.SerializeWrapper(obj);
                Assert.Contains(@"""Blah"":1", json);
            }
        }

        [Fact]
        public async Task JsonNameAttributeDuplicateDesignTimeFail()
        {
            {
                var options = new JsonSerializerOptions();
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<DuplicatePropertyNameDesignTime_TestClass>("{}", options));
            }

            {
                var options = new JsonSerializerOptions();
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(new DuplicatePropertyNameDesignTime_TestClass(), options));
            }
        }

        [Fact]
        public async Task JsonNameConflictOnCamelCasingFail()
        {
            {
                // Baseline comparison - no options set.
                IntPropertyNamesDifferentByCaseOnly_TestClass obj = await Serializer.DeserializeWrapper<IntPropertyNamesDifferentByCaseOnly_TestClass>("{}");
                await Serializer.SerializeWrapper(obj);
            }

            {
                var options = new JsonSerializerOptions();
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<IntPropertyNamesDifferentByCaseOnly_TestClass>("{}", options));
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(new IntPropertyNamesDifferentByCaseOnly_TestClass(), options));
            }

            {
                // Baseline comparison - no options set.
                ObjectPropertyNamesDifferentByCaseOnly_TestClass obj = await Serializer.DeserializeWrapper<ObjectPropertyNamesDifferentByCaseOnly_TestClass>("{}");
                await Serializer.SerializeWrapper(obj);
            }

            {
                var options = new JsonSerializerOptions();
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<ObjectPropertyNamesDifferentByCaseOnly_TestClass>("{}", options));
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(new ObjectPropertyNamesDifferentByCaseOnly_TestClass(), options));
            }
        }

        [Fact]
        public async Task JsonOutputNotAffectedByCasingPolicy()
        {
            {
                // Baseline.
                string json = await Serializer.SerializeWrapper(new SimpleTestClass());
                Assert.Contains(@"""MyInt16"":0", json);
            }

            // The JSON output should be unaffected by PropertyNameCaseInsensitive.
            {
                var options = new JsonSerializerOptions();
                options.PropertyNameCaseInsensitive = true;

                string json = await Serializer.SerializeWrapper(new SimpleTestClass(), options);
                Assert.Contains(@"""MyInt16"":0", json);
            }
        }

        [Fact]
        public async Task EmptyPropertyName()
        {
            string json = @"{"""":1}";

            {
                var obj = new EmptyPropertyName_TestClass();
                obj.MyInt1 = 1;

                string jsonOut = await Serializer.SerializeWrapper(obj);
                Assert.Equal(json, jsonOut);
            }

            {
                EmptyPropertyName_TestClass obj = await Serializer.DeserializeWrapper<EmptyPropertyName_TestClass>(json);
                Assert.Equal(1, obj.MyInt1);
            }
        }

        [Fact]
        public async Task UnicodePropertyNames()
        {
            ClassWithUnicodeProperty obj = await Serializer.DeserializeWrapper<ClassWithUnicodeProperty>("{\"A\u0467\":1}");
            Assert.Equal(1, obj.A\u0467);

            // Specifying encoder on options does not impact deserialize.
            var options = new JsonSerializerOptions();
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            obj = await Serializer.DeserializeWrapper<ClassWithUnicodeProperty>("{\"A\u0467\":1}", options);
            Assert.Equal(1, obj.A\u0467);

            string json;

            // Verify the name is escaped after serialize.
            json = await Serializer.SerializeWrapper(obj);
            Assert.Contains(@"""A\u0467"":1", json);

            // With custom escaper
            json = await Serializer.SerializeWrapper(obj, options);
            Assert.Contains("\"A\u0467\":1", json);

            // Verify the name is unescaped after deserialize.
            obj = await Serializer.DeserializeWrapper<ClassWithUnicodeProperty>(json);
            Assert.Equal(1, obj.A\u0467);

            // With custom escaper
            obj = await Serializer.DeserializeWrapper<ClassWithUnicodeProperty>(json, options);
            Assert.Equal(1, obj.A\u0467);
        }

        [Fact]
        public async Task UnicodePropertyNamesWithPooledAlloc()
        {
            // We want to go over StackallocByteThreshold=256 to force a pooled allocation, so this property is 400 chars and 401 bytes.
            ClassWithUnicodeProperty obj = await Serializer.DeserializeWrapper<ClassWithUnicodeProperty>("{\"A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890\":1}");
            Assert.Equal(1, obj.A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890);

            // Verify the name is escaped after serialize.
            string json = await Serializer.SerializeWrapper(obj);
            Assert.Contains(@"""A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890"":1", json);

            // Verify the name is unescaped after deserialize.
            obj = await Serializer.DeserializeWrapper<ClassWithUnicodeProperty>(json);
            Assert.Equal(1, obj.A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890);
        }

        public class ClassWithPropertyNamePermutations
        {
            public int a { get; set; }
            public int aa { get; set; }
            public int aaa { get; set; }
            public int aaaa { get; set; }
            public int aaaaa { get; set; }
            public int aaaaaa { get; set; }

            // 7 characters - caching code only keys up to 7.
            public int aaaaaaa { get; set; }
            public int aaaaaab { get; set; }

            // 8 characters.
            public int aaaaaaaa { get; set; }
            public int aaaaaaab { get; set; }

            // 9 characters.
            public int aaaaaaaaa { get; set; }
            public int aaaaaaaab { get; set; }

            public int \u0467 { get; set; }
            public int \u0467\u0467 { get; set; }
            public int \u0467\u0467a { get; set; }
            public int \u0467\u0467b { get; set; }
            public int \u0467\u0467\u0467 { get; set; }
            public int \u0467\u0467\u0467a { get; set; }
            public int \u0467\u0467\u0467b { get; set; }
            public int \u0467\u0467\u0467\u0467 { get; set; }
            public int \u0467\u0467\u0467\u0467a { get; set; }
            public int \u0467\u0467\u0467\u0467b { get; set; }
        }

        [Fact]
        public async Task CachingKeys()
        {
            ClassWithPropertyNamePermutations obj;

            void Verify()
            {
                Assert.Equal(1, obj.a);
                Assert.Equal(2, obj.aa);
                Assert.Equal(3, obj.aaa);
                Assert.Equal(4, obj.aaaa);
                Assert.Equal(5, obj.aaaaa);
                Assert.Equal(6, obj.aaaaaa);
                Assert.Equal(7, obj.aaaaaaa);
                Assert.Equal(7, obj.aaaaaab);
                Assert.Equal(8, obj.aaaaaaaa);
                Assert.Equal(8, obj.aaaaaaab);
                Assert.Equal(9, obj.aaaaaaaaa);
                Assert.Equal(9, obj.aaaaaaaab);

                Assert.Equal(2, obj.\u0467);
                Assert.Equal(4, obj.\u0467\u0467);
                Assert.Equal(5, obj.\u0467\u0467a);
                Assert.Equal(5, obj.\u0467\u0467b);
                Assert.Equal(6, obj.\u0467\u0467\u0467);
                Assert.Equal(7, obj.\u0467\u0467\u0467a);
                Assert.Equal(7, obj.\u0467\u0467\u0467b);
                Assert.Equal(8, obj.\u0467\u0467\u0467\u0467);
                Assert.Equal(9, obj.\u0467\u0467\u0467\u0467a);
                Assert.Equal(9, obj.\u0467\u0467\u0467\u0467b);
            }

            obj = new ClassWithPropertyNamePermutations
            {
                a = 1,
                aa = 2,
                aaa = 3,
                aaaa = 4,
                aaaaa = 5,
                aaaaaa = 6,
                aaaaaaa = 7,
                aaaaaab = 7,
                aaaaaaaa = 8,
                aaaaaaab = 8,
                aaaaaaaaa = 9,
                aaaaaaaab = 9,
                \u0467 = 2,
                \u0467\u0467 = 4,
                \u0467\u0467a = 5,
                \u0467\u0467b = 5,
                \u0467\u0467\u0467 = 6,
                \u0467\u0467\u0467a = 7,
                \u0467\u0467\u0467b = 7,
                \u0467\u0467\u0467\u0467 = 8,
                \u0467\u0467\u0467\u0467a = 9,
                \u0467\u0467\u0467\u0467b = 9,
            };

            // Verify baseline.
            Verify();

            string json = await Serializer.SerializeWrapper(obj);

            // Verify the length is consistent with a verified value.
            Assert.Equal(354, json.Length);

            obj = await Serializer.DeserializeWrapper<ClassWithPropertyNamePermutations>(json);

            // Verify round-tripped object.
            Verify();
        }

        [Fact]
        public async Task BadNamingPolicy_ThrowsInvalidOperation()
        {
            var options = new JsonSerializerOptions { DictionaryKeyPolicy = new NullNamingPolicy() };

            var inputPrimitive = new Dictionary<string, int>
            {
                { "validKey", 1 }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(inputPrimitive, options));

            var inputClass = new Dictionary<string, OverridePropertyNameDesignTime_TestClass>
            {
                { "validKey", new OverridePropertyNameDesignTime_TestClass() }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(inputClass, options));
        }

        public class OverridePropertyNameDesignTime_TestClass
        {
            [JsonPropertyName("Blah")]
            public int myInt { get; set; }

            [JsonPropertyName("BlahObject")]
            public object myObject { get; set; }
        }

        public class DuplicatePropertyNameDesignTime_TestClass
        {
            [JsonPropertyName("Blah")]
            public int MyInt1 { get; set; }

            [JsonPropertyName("Blah")]
            public int MyInt2 { get; set; }
        }

        public class EmptyPropertyName_TestClass
        {
            [JsonPropertyName("")]
            public int MyInt1 { get; set; }
        }

        public class NullPropertyName_TestClass
        {
            [JsonPropertyName(null)]
            public int MyInt1 { get; set; }
        }

        public class IntPropertyNamesDifferentByCaseOnly_TestClass
        {
            public int myInt { get; set; }
            public int MyInt { get; set; }
        }

        public class ObjectPropertyNamesDifferentByCaseOnly_TestClass
        {
            public int myObject { get; set; }
            public int MyObject { get; set; }
        }

        [Fact]
        public async Task SpecialCharacters()
        {
            ClassWithSpecialCharacters obj = new()
            {
                Baseline = 1,
                Schema = 2,
                SmtpId = 3,
                Emojies = 4,
                ꀀ = 5,
                YiIt_2 = 6
            };

            string json = await Serializer.SerializeWrapper(obj);
            Assert.Equal(
                "{\"Baseline\":1," +
                "\"$schema\":2," +
                "\"smtp-id\":3," +
                "\"\\uD83D\\uDE00\\uD83D\\uDE01\":4," +
                "\"\\uA000\":5," +
                "\"\\uA000_2\":6}", json);

            obj = await Serializer.DeserializeWrapper<ClassWithSpecialCharacters>(json);
            Assert.Equal(1, obj.Baseline);
            Assert.Equal(2, obj.Schema);
            Assert.Equal(3, obj.SmtpId);
            Assert.Equal(4, obj.Emojies);
            Assert.Equal(5, obj.ꀀ);
            Assert.Equal(6, obj.YiIt_2);
        }

        public class ClassWithSpecialCharacters
        {
            [JsonPropertyOrder(1)]
            public int Baseline { get; set; }

            [JsonPropertyOrder(2)]
            [JsonPropertyName("$schema")] // Invalid C# property name.
            public int Schema { get; set; }

            [JsonPropertyOrder(3)]
            [JsonPropertyName("smtp-id")] // Invalid C# property name.
            public int SmtpId { get; set; }

            [JsonPropertyOrder(4)]
            [JsonPropertyName("😀😁")] // Invalid C# property name. Unicode:\uD83D\uDE00\uD83D\uDE01
            public int Emojies { get; set; }

            [JsonPropertyOrder(5)]
            public int ꀀ { get; set; } // Valid C# property name. Unicode:\uA000

            [JsonPropertyOrder(6)]
            [JsonPropertyName("\uA000_2")] // Valid C# property name: ꀀ_2
            public int YiIt_2 { get; set; }
        }
    }
}
