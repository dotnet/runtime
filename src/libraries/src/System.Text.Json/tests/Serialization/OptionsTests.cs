// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class OptionsTests
    {
        [Fact]
        public static void SetOptionsFail()
        {
            var options = new JsonSerializerOptions();

            JsonSerializer.Parse<int>("1", options);

            // Verify defaults and ensure getters do not throw.
            Assert.False(options.AllowTrailingCommas);
            Assert.Equal(16 * 1024, options.DefaultBufferSize);
            Assert.Equal(null, options.DictionaryKeyPolicy);
            Assert.False(options.IgnoreNullValues);
            Assert.Equal(0, options.MaxDepth);
            Assert.Equal(false, options.PropertyNameCaseInsensitive);
            Assert.Equal(null, options.PropertyNamingPolicy);
            Assert.Equal(JsonCommentHandling.Disallow, options.ReadCommentHandling);
            Assert.False(options.WriteIndented);

            // Setters should always throw; we don't check to see if the value is the same or not.
            Assert.Throws<InvalidOperationException>(() => options.AllowTrailingCommas = options.AllowTrailingCommas);
            Assert.Throws<InvalidOperationException>(() => options.DefaultBufferSize = options.DefaultBufferSize);
            Assert.Throws<InvalidOperationException>(() => options.DictionaryKeyPolicy = options.DictionaryKeyPolicy);
            Assert.Throws<InvalidOperationException>(() => options.IgnoreNullValues = options.IgnoreNullValues);
            Assert.Throws<InvalidOperationException>(() => options.MaxDepth = options.MaxDepth);
            Assert.Throws<InvalidOperationException>(() => options.PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive);
            Assert.Throws<InvalidOperationException>(() => options.PropertyNamingPolicy = options.PropertyNamingPolicy);
            Assert.Throws<InvalidOperationException>(() => options.ReadCommentHandling = options.ReadCommentHandling);
            Assert.Throws<InvalidOperationException>(() => options.WriteIndented = options.WriteIndented);
        }

        [Fact]
        public static void DefaultBufferSizeFail()
        {
            Assert.Throws<ArgumentException>(() => new JsonSerializerOptions().DefaultBufferSize = 0);
            Assert.Throws<ArgumentException>(() => new JsonSerializerOptions().DefaultBufferSize = -1);
        }

        [Fact]
        public static void DefaultBufferSize()
        {
            var options = new JsonSerializerOptions();

            Assert.Equal(16 * 1024, options.DefaultBufferSize);

            options.DefaultBufferSize = 1;
            Assert.Equal(1, options.DefaultBufferSize);
        }

        [Fact]
        public static void AllowTrailingCommas()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<int[]>("[1,]"));

            var options = new JsonSerializerOptions();
            options.AllowTrailingCommas = true;

            int[] value = JsonSerializer.Parse<int[]>("[1,]", options);
            Assert.Equal(1, value[0]);
        }

        [Fact]
        public static void WriteIndented()
        {
            var obj = new BasicCompany();
            obj.Initialize();

            // Verify default value.
            string json = JsonSerializer.ToString(obj);
            Assert.DoesNotContain(Environment.NewLine, json);

            // Verify default value on options.
            var options = new JsonSerializerOptions();
            json = JsonSerializer.ToString(obj, options);
            Assert.DoesNotContain(Environment.NewLine, json);

            // Change the value on options.
            options = new JsonSerializerOptions();
            options.WriteIndented = true;
            json = JsonSerializer.ToString(obj, options);
            Assert.Contains(Environment.NewLine, json);
        }

        [Fact]
        public static void ExtensionDataUsesReaderOptions()
        {
            // We just verify trailing commas.
            const string json = @"{""MyIntMissing"":2,}";

            // Verify baseline without options.
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<ClassWithExtensionProperty>(json));

            // Verify baseline with options.
            var options = new JsonSerializerOptions();
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<ClassWithExtensionProperty>(json, options));

            // Set AllowTrailingCommas to true.
            options = new JsonSerializerOptions();
            options.AllowTrailingCommas = true;
            JsonSerializer.Parse<ClassWithExtensionProperty>(json, options);
        }

        [Fact]
        public static void ExtensionDataUsesWriterOptions()
        {
            // We just verify whitespace.

            ClassWithExtensionProperty obj = JsonSerializer.Parse<ClassWithExtensionProperty>(@"{""MyIntMissing"":2}");

            // Verify baseline without options.
            string json = JsonSerializer.ToString(obj);
            Assert.False(HasNewLine());

            // Verify baseline with options.
            var options = new JsonSerializerOptions();
            json = JsonSerializer.ToString(obj, options);
            Assert.False(HasNewLine());

            // Set AllowTrailingCommas to true.
            options = new JsonSerializerOptions();
            options.WriteIndented = true;
            json = JsonSerializer.ToString(obj, options);
            Assert.True(HasNewLine());

            bool HasNewLine()
            {
                int iEnd = json.IndexOf("2", json.IndexOf("MyIntMissing"));
                return json.Substring(iEnd + 1).StartsWith(Environment.NewLine);
            }
        }

        [Fact]
        public static void ReadCommentHandling()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<object>("/* commment */"));

            var options = new JsonSerializerOptions();

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<object>("/* commment */", options));

            options = new JsonSerializerOptions();
            options.ReadCommentHandling = JsonCommentHandling.Allow;

            JsonSerializer.Parse<object>("/* commment */", options);
        }

        [Fact]
        public static void MaxDepthRead()
        {
            JsonSerializer.Parse<BasicCompany>(BasicCompany.s_data);

            var options = new JsonSerializerOptions();

            JsonSerializer.Parse<BasicCompany>(BasicCompany.s_data, options);

            options = new JsonSerializerOptions();
            options.MaxDepth = 1;

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<BasicCompany>(BasicCompany.s_data, options));
        }
    }
}
