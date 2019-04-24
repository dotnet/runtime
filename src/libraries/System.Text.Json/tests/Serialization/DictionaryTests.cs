// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class DictionaryTests
    {
        [Fact]
        public static void DirectReturn()
        {
            {
                Dictionary<string, string> obj = JsonSerializer.Parse<Dictionary<string, string>>(@"{""Hello"":""World"", ""Hello2"":""World2""}");
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.ToString(obj);
                Assert.Equal(@"{""Hello"":""World"",""Hello2"":""World2""}", json);

                // Round-trip the json
                obj = JsonSerializer.Parse<Dictionary<string, string>>(json);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);
            }

            {
                IDictionary<string, string> obj = JsonSerializer.Parse<IDictionary<string, string>>(@"{""Hello"":""World""}");
                Assert.Equal("World", obj["Hello"]);

                string json = JsonSerializer.ToString(obj);
                Assert.Equal(@"{""Hello"":""World""}", json);

                obj = JsonSerializer.Parse<Dictionary<string, string>>(json);
                Assert.Equal("World", obj["Hello"]);
            }

            {
                IReadOnlyDictionary<string, string> obj = JsonSerializer.Parse<IReadOnlyDictionary<string, string>>(@"{""Hello"":""World""}");
                Assert.Equal("World", obj["Hello"]);

                string json = JsonSerializer.ToString(obj);
                Assert.Equal(@"{""Hello"":""World""}", json);

                obj = JsonSerializer.Parse<Dictionary<string, string>>(json);
                Assert.Equal("World", obj["Hello"]);
            }
        }

        [Fact]
        public static void ThrowsOnDuplicateKeys()
        {
            // todo: this should throw a JsonReaderException
            Assert.Throws<ArgumentException>(() => JsonSerializer.Parse<Dictionary<string, string>>(@"{""Hello"":""World"", ""Hello"":""World""}"));
        }

        [Fact]
        public static void UnicodePropertyNames()
        {
            {
                Dictionary<string, int> obj = JsonSerializer.Parse<Dictionary<string, int>>(@"{""Aѧ"":1}");
                Assert.Equal(1, obj["Aѧ"]);

                // Verify the name is escaped after serialize.
                string json = JsonSerializer.ToString(obj);
                Assert.Equal(@"{""A\u0467"":1}", json);
            }

            {
                // We want to go over StackallocThreshold=256 to force a pooled allocation, so this property is 200 chars and 400 bytes.
                const int charsInProperty = 200;

                string longPropertyName = new string('ѧ', charsInProperty);

                Dictionary<string, int> obj = JsonSerializer.Parse<Dictionary<string, int>>($"{{\"{longPropertyName}\":1}}");
                Assert.Equal(1, obj[longPropertyName]);

                // Verify the name is escaped after serialize.
                string json = JsonSerializer.ToString(obj);

                // Duplicate the unicode character 'charsInProperty' times.
                string longPropertyNameEscaped = new StringBuilder().Insert(0, @"\u0467", charsInProperty).ToString();

                string expectedJson = $"{{\"{longPropertyNameEscaped}\":1}}";
                Assert.Equal(expectedJson, json);

                // Verify the name is unescaped after deserialize.
                obj = JsonSerializer.Parse<Dictionary<string, int>>(json);
                Assert.Equal(1, obj[longPropertyName]);
            }
        }
    }
}
