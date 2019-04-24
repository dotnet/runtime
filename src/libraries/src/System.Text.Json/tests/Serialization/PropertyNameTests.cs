// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Tests;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class PropertyNameTests
    {
        [Fact]
        public static void CamelCaseDeserializeNoMatch()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(@"{""MyInt16"":1}", options);

            // This is 0 (default value) because the data does not match the property "MyInt16" that is assuming camel-casing of "myInt16".
            Assert.Equal(0, obj.MyInt16);
        }

        [Fact]
        public static void CamelCaseDeserializeMatch()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(@"{""myInt16"":1}", options);

            // This is 1 because the data matches the property "MyInt16" that is assuming camel-casing of "myInt16".
            Assert.Equal(1, obj.MyInt16);
        }

        [Fact]
        public static void CamelCaseSerialize()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(@"{}", options);
            string json = JsonSerializer.ToString(obj, options);
            Assert.Contains(@"""myInt16"":0", json);
            Assert.Contains(@"""myInt32"":0", json);
        }

        [Fact]
        public static void CustomNamePolicy()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = new UppercaseNamingPolicy();

            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(@"{""MYINT16"":1}", options);

            // This is 1 because the data matches the property "MYINT16" that is uppercase of "myInt16".
            Assert.Equal(1, obj.MyInt16);
        }

        [Fact]
        public static void NullNamePolicy()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = new NullNamingPolicy();

            // A policy that returns null is not allowed.
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Parse<SimpleTestClass>(@"{}", options));
        }

        [Fact]
        public static void IgnoreCase()
        {
            {
                // A non-match scenario with no options (case-sensitive by default).
                SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(@"{""myint16"":1}");
                Assert.Equal(0, obj.MyInt16);
            }

            {
                // A non-match scenario with default options (case-sensitive by default).
                var options = new JsonSerializerOptions();
                SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(@"{""myint16"":1}", options);
                Assert.Equal(0, obj.MyInt16);
            }

            {
                var options = new JsonSerializerOptions();
                options.PropertyNameCaseInsensitive = true;
                SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(@"{""myint16"":1}", options);
                Assert.Equal(1, obj.MyInt16);
            }
        }

        [Fact]
        public static void JsonPropertyNameAttribute()
        {
            {
                OverridePropertyNameDesignTime_TestClass obj = JsonSerializer.Parse<OverridePropertyNameDesignTime_TestClass>(@"{""Blah"":1}");
                Assert.Equal(1, obj.myInt);

                obj.myObject = 2;
                
                string json = JsonSerializer.ToString(obj);
                Assert.Contains(@"""Blah"":1", json);
                Assert.Contains(@"""BlahObject"":2", json);
            }

            // The JsonPropertyNameAttribute should be unaffected by JsonNamingPolicy and PropertyNameCaseInsensitive.
            {
                var options = new JsonSerializerOptions();
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.PropertyNameCaseInsensitive = true;

                OverridePropertyNameDesignTime_TestClass obj = JsonSerializer.Parse<OverridePropertyNameDesignTime_TestClass>(@"{""Blah"":1}", options);
                Assert.Equal(1, obj.myInt);

                string json = JsonSerializer.ToString(obj);
                Assert.Contains(@"""Blah"":1", json);
            }
        }

        [Fact]
        public static void JsonNameAttributeDuplicateDesignTimeFail()
        {
            {
                var options = new JsonSerializerOptions();
                Assert.Throws<InvalidOperationException>(() => JsonSerializer.Parse<DuplicatePropertyNameDesignTime_TestClass>("{}", options));
            }

            {
                var options = new JsonSerializerOptions();
                Assert.Throws<InvalidOperationException>(() => JsonSerializer.ToString(new DuplicatePropertyNameDesignTime_TestClass(), options));
            }
        }

        [Fact]
        public static void JsonNullNameAttribute()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNameCaseInsensitive = true;

            // A null name in JsonPropertyNameAttribute is not allowed.
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.ToString(new NullPropertyName_TestClass(), options));
        }

        [Fact]
        public static void JsonNameConflictOnCamelCasingFail()
        {
            {
                // Baseline comparison - no options set.
                IntPropertyNamesDifferentByCaseOnly_TestClass obj = JsonSerializer.Parse<IntPropertyNamesDifferentByCaseOnly_TestClass>("{}");
                JsonSerializer.ToString(obj);
            }

            {
                var options = new JsonSerializerOptions();
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                Assert.Throws<InvalidOperationException>(() => JsonSerializer.Parse<IntPropertyNamesDifferentByCaseOnly_TestClass>("{}", options));
                Assert.Throws<InvalidOperationException>(() => JsonSerializer.ToString(new IntPropertyNamesDifferentByCaseOnly_TestClass(), options));
            }

            {
                // Baseline comparison - no options set.
                ObjectPropertyNamesDifferentByCaseOnly_TestClass obj = JsonSerializer.Parse<ObjectPropertyNamesDifferentByCaseOnly_TestClass>("{}");
                JsonSerializer.ToString(obj);
            }

            {
                var options = new JsonSerializerOptions();
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                Assert.Throws<InvalidOperationException>(() => JsonSerializer.Parse<ObjectPropertyNamesDifferentByCaseOnly_TestClass>("{}", options));
                Assert.Throws<InvalidOperationException>(() => JsonSerializer.ToString(new ObjectPropertyNamesDifferentByCaseOnly_TestClass(), options));
            }
        }

        [Fact]
        public static void JsonNameConflictOnCaseInsensitiveFail()
        {
            string json = @"{""myInt"":1,""MyInt"":2}";

            {
                var options = new JsonSerializerOptions();
                options.PropertyNameCaseInsensitive = true;

                Assert.Throws<InvalidOperationException>(() => JsonSerializer.Parse<IntPropertyNamesDifferentByCaseOnly_TestClass>(json, options));
                Assert.Throws<InvalidOperationException>(() => JsonSerializer.ToString(new IntPropertyNamesDifferentByCaseOnly_TestClass(), options));
            }
        }

        [Fact]
        public static void JsonOutputNotAffectedByCasingPolicy()
        {
            {
                // Baseline.
                string json = JsonSerializer.ToString(new SimpleTestClass());
                Assert.Contains(@"""MyInt16"":0", json);
            }

            // The JSON output should be unaffected by PropertyNameCaseInsensitive.
            {
                var options = new JsonSerializerOptions();
                options.PropertyNameCaseInsensitive = true;

                string json = JsonSerializer.ToString(new SimpleTestClass(), options);
                Assert.Contains(@"""MyInt16"":0", json);
            }
        }

        [Fact]
        public static void EmptyPropertyName()
        {
            string json = @"{"""":1}";

            {
                var obj = new EmptyPropertyName_TestClass();
                obj.MyInt1 = 1;

                string jsonOut = JsonSerializer.ToString(obj);
                Assert.Equal(json, jsonOut);
            }

            {
                EmptyPropertyName_TestClass obj = JsonSerializer.Parse<EmptyPropertyName_TestClass>(json);
                Assert.Equal(1, obj.MyInt1);
            }
        }

        [Fact]
        public static void UnicodePropertyNames()
        {
            {
                ClassWithUnicodeProperty obj = JsonSerializer.Parse<ClassWithUnicodeProperty>(@"{""Aѧ"":1}");
                Assert.Equal(1, obj.Aѧ);

                // Verify the name is escaped after serialize.
                string json = JsonSerializer.ToString(obj);
                Assert.Contains(@"""A\u0467"":1", json);

                // Verify the name is unescaped after deserialize.
                obj = JsonSerializer.Parse<ClassWithUnicodeProperty>(json);
                Assert.Equal(1, obj.Aѧ);
            }

            {
                // We want to go over StackallocThreshold=256 to force a pooled allocation, so this property is 400 chars and 401 bytes.
                ClassWithUnicodeProperty obj = JsonSerializer.Parse<ClassWithUnicodeProperty>(@"{""Aѧ34567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890"":1}");
                Assert.Equal(1, obj.Aѧ34567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890);

                // Verify the name is escaped after serialize.
                string json = JsonSerializer.ToString(obj);
                Assert.Contains(@"""A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890"":1", json);

                // Verify the name is unescaped after deserialize.
                obj = JsonSerializer.Parse<ClassWithUnicodeProperty>(json);
                Assert.Equal(1, obj.Aѧ34567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890);
            }
        }
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

    public class UppercaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToUpperInvariant();
        }
    }

    public class NullNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return null;
        }
    }
}
