// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ExceptionTests
    {
        [Fact]
        public static void RootThrownFromReaderFails()
        {
            try
            {
                int i2 = JsonSerializer.Parse<int>("12bad");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal(0, e.LineNumber);
                Assert.Equal(2, e.BytePositionInLine);
                Assert.Equal("$", e.Path);
                Assert.Contains("Path: $ | LineNumber: 0 | BytePositionInLine: 2.", e.Message);

                // Verify Path is not repeated.
                Assert.True(e.Message.IndexOf("Path:") == e.Message.LastIndexOf("Path:"));
            }
        }

        [Fact]
        public static void ThrownFromSerializerFails()
        {
            try
            {
                JsonSerializer.Parse<IDictionary<string, int>>(@"{""Key"":1, ""Key"":2}");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal(0, e.LineNumber);
                Assert.Equal(17, e.BytePositionInLine);
                Assert.Contains("LineNumber: 0 | BytePositionInLine: 17.", e.Message);
                Assert.Contains("$.Key", e.Path);

                // Verify Path is not repeated.
                Assert.True(e.Message.IndexOf("Path:") == e.Message.LastIndexOf("Path:"));
            }
        }

        [Fact]
        public static void ThrownFromReaderFails()
        {
            string json = Encoding.UTF8.GetString(BasicCompany.s_data);

            json = json.Replace(@"""zip"" : 98052", @"""zip"" : bad");

            try
            {
                JsonSerializer.Parse<BasicCompany>(json);
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal(18, e.LineNumber);
                Assert.Equal(8, e.BytePositionInLine);
                Assert.Equal("$.mainSite.zip", e.Path);
                Assert.Contains("Path: $.mainSite.zip | LineNumber: 18 | BytePositionInLine: 8.",
                    e.Message);

                // Verify Path is not repeated.
                Assert.True(e.Message.IndexOf("Path:") == e.Message.LastIndexOf("Path:"));

                Assert.NotNull(e.InnerException);
                JsonException inner = (JsonException)e.InnerException;
                Assert.Equal(18, inner.LineNumber);
                Assert.Equal(8, inner.BytePositionInLine);
            }
        }

        [Fact]
        public static void PathForDictionaryFails()
        {
            try
            {
                JsonSerializer.Parse<Dictionary<string, int>>(@"{""Key1"":1, ""Key2"":bad}");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$.Key2", e.Path);
            }

            try
            {
                JsonSerializer.Parse<Dictionary<string, int>>(@"{""Key1"":1, ""Key2"":");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$.Key2", e.Path);
            }

            try
            {
                JsonSerializer.Parse<Dictionary<string, int>>(@"{""Key1"":1, ""Key2""");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                // Key2 is not yet a valid key name since there is no : delimiter.
                Assert.Equal("$.Key1", e.Path);
            }
        }

        [Fact]
        public static void PathForArrayFails()
        {
            try
            {
                JsonSerializer.Parse<int[]>(@"[1, bad]");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$[1]", e.Path);
            }

            try
            {
                JsonSerializer.Parse<int[]>(@"[1,");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$[1]", e.Path);
            }

            try
            {
                JsonSerializer.Parse<int[]>(@"[1");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                // No delimiter.
                Assert.Equal("$[0]", e.Path);
            }

            try
            {
                JsonSerializer.Parse<int[]>(@"[1 /* comment starts but doesn't end");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                // The reader treats the space as a delimiter.
                Assert.Equal("$[1]", e.Path);
            }
        }

        [Fact]
        public static void PathForListFails()
        {
            try
            {
                JsonSerializer.Parse<List<int>>(@"[1, bad]");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$[1]", e.Path);
            }
        }

        [Fact]
        public static void PathFor2dArrayFails()
        {
            try
            {
                JsonSerializer.Parse<int[][]>(@"[[1, 2],[3,bad]]");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$[1][1]", e.Path);
            }
        }

        [Fact]
        public static void PathFor2dListFails()
        {
            try
            {
                JsonSerializer.Parse<List<List<int>>>(@"[[1, 2],[3,bad]]");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$[1][1]", e.Path);
            }
        }

        [Fact]
        public static void PathForChildPropertyFails()
        {
            try
            {
                JsonSerializer.Parse<RootClass>(@"{""Child"":{""MyInt"":bad]}");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$.Child.MyInt", e.Path);
            }
        }

        [Fact]
        public static void PathForChildListFails()
        {
            try
            {
                JsonSerializer.Parse<RootClass>(@"{""Child"":{""MyIntArray"":[1, bad]}");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$.Child.MyIntArray[1]", e.Path);
            }
        }

        [Fact]
        public static void PathForChildDictionaryFails()
        {
            try
            {
                JsonSerializer.Parse<RootClass>(@"{""Child"":{""MyDictionary"":{""Key"": bad]");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$.Child.MyDictionary.Key", e.Path);
            }
        }

        [Fact]
        public static void PathForSpecialCharacterFails()
        {
            try
            {
                JsonSerializer.Parse<RootClass>(@"{""Child"":{""MyDictionary"":{""Key1"":{""Children"":[{""MyDictionary"":{""K.e.y"":""");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$.Child.MyDictionary.Key1.Children[0].MyDictionary['K.e.y']", e.Path);
            }
        }

        [Fact]
        public static void PathForSpecialCharacterNestedFails()
        {
            try
            {
                JsonSerializer.Parse<RootClass>(@"{""Child"":{""Children"":[{}, {""MyDictionary"":{""K.e.y"": {""MyInt"":bad");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$.Child.Children[1].MyDictionary['K.e.y'].MyInt", e.Path);
            }
        }

        [Fact]
        public static void EscapingFails()
        {
            try
            {
                ClassWithUnicodeProperty obj = JsonSerializer.Parse<ClassWithUnicodeProperty>(@"{""Aѧ"":bad}");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal(@"$.A\u0467", e.Path);
            }
        }

        [Fact]
        [ActiveIssue("JsonElement needs to support Path")]
        public static void ExtensionPropertyRoundTripFails()
        {
            try
            {
                JsonSerializer.Parse<ClassWithExtensionProperty>(@"{""MyNestedClass"":{""UnknownProperty"":bad}}");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                // Until JsonElement supports populating Path ("UnknownProperty"), which will be prepended by the serializer ("MyNestedClass"), this will fail.
                Assert.Equal("$.MyNestedClass.UnknownProperty", e.Path);
            }
        }

        [Fact]
        public static void CaseInsensitiveFails()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;

            // Baseline (no exception)
            {
                SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(@"{""myint32"":1}", options);
                Assert.Equal(1, obj.MyInt32);
            }

            {
                SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(@"{""MYINT32"":1}", options);
                Assert.Equal(1, obj.MyInt32);
            }

            try
            {
                JsonSerializer.Parse<SimpleTestClass>(@"{""myint32"":bad}", options);
            }
            catch (JsonException e)
            {
                // The Path should reflect the case even though it is different from the property.
                Assert.Equal("$.myint32", e.Path);
            }

            try
            {
                JsonSerializer.Parse<SimpleTestClass>(@"{""MYINT32"":bad}", options);
            }
            catch (JsonException e)
            {
                // Verify the previous json property name was not cached.
                Assert.Equal("$.MYINT32", e.Path);
            }
        }

        public class RootClass
        {
            public ChildClass Child { get; set; }
        }

        public class ChildClass
        {
            public int MyInt { get; set; }
            public int[] MyIntArray { get; set; }
            public Dictionary<string, ChildClass> MyDictionary { get; set; }
            public ChildClass[] Children { get; set; }
        }
    }
}
