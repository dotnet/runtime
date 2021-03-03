// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Encodings.Web;
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
                int i2 = JsonSerializer.Deserialize<int>("12bad");
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
        public static void TypeMismatchIDictionaryExceptionThrown()
        {
            try
            {
                JsonSerializer.Deserialize<IDictionary<string, string>>(@"{""Key"":1}");
                Assert.True(false, "Type Mismatch JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal(0, e.LineNumber);
                Assert.Equal(8, e.BytePositionInLine);
                Assert.Contains("LineNumber: 0 | BytePositionInLine: 8.", e.Message);
                Assert.Contains("$.Key", e.Path);

                // Verify Path is not repeated.
                Assert.True(e.Message.IndexOf("Path:") == e.Message.LastIndexOf("Path:"));
            }
        }

        [Fact]
        public static void TypeMismatchIDictionaryExceptionWithCustomEscaperThrown()
        {
            var options = new JsonSerializerOptions();
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            JsonException e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, int>>("{\"Key\u0467\":1", options));
            Assert.Equal(0, e.LineNumber);
            Assert.Equal(10, e.BytePositionInLine);
            Assert.Contains("LineNumber: 0 | BytePositionInLine: 10.", e.Message);
            Assert.Contains("$.Key\u0467", e.Path);

            // Verify Path is not repeated.
            Assert.True(e.Message.IndexOf("Path:") == e.Message.LastIndexOf("Path:"));
        }

        [Fact]
        public static void ThrownFromReaderFails()
        {
            string json = Encoding.UTF8.GetString(BasicCompany.s_data);

            json = json.Replace(@"""zip"" : 98052", @"""zip"" : bad");

            try
            {
                JsonSerializer.Deserialize<BasicCompany>(json);
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
                JsonSerializer.Deserialize<Dictionary<string, int>>(@"{""Key1"":1, ""Key2"":bad}");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$.Key2", e.Path);
            }

            try
            {
                JsonSerializer.Deserialize<Dictionary<string, int>>(@"{""Key1"":1, ""Key2"":");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$.Key2", e.Path);
            }

            try
            {
                JsonSerializer.Deserialize<Dictionary<string, int>>(@"{""Key1"":1, ""Key2""");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                // Key2 is not yet a valid key name since there is no : delimiter.
                Assert.Equal("$.Key1", e.Path);
            }
        }

        [Fact]
        public static void DeserializePathForDictionaryFails()
        {
            const string Json = "{\"Key1\u0467\":1, \"Key2\u0467\":bad}";
            const string JsonEscaped = "{\"Key1\\u0467\":1, \"Key2\\u0467\":bad}";
            const string Expected = "$.Key2\u0467";

            JsonException e;

            // Without custom escaper.
            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, int>>(Json));
            Assert.Equal(Expected, e.Path);

            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, int>>(JsonEscaped));
            Assert.Equal(Expected, e.Path);

            // Custom escaper should not change Path.
            var options = new JsonSerializerOptions();
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, int>>(Json, options));
            Assert.Equal(Expected, e.Path);

            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, int>>(JsonEscaped, options));
            Assert.Equal(Expected, e.Path);
        }

        private class ClassWithUnicodePropertyName
        {
            public int Property\u04671 { get; set; } // contains a trailing "1"
        }

        [Fact]
        public static void DeserializePathForObjectFails()
        {
            const string GoodJson = "{\"Property\u04671\":1}";
            const string GoodJsonEscaped = "{\"Property\\u04671\":1}";
            const string BadJson = "{\"Property\u04671\":bad}";
            const string BadJsonEscaped = "{\"Property\\u04671\":bad}";
            const string Expected = "$.Property\u04671";

            ClassWithUnicodePropertyName obj;

            // Baseline.
            obj = JsonSerializer.Deserialize<ClassWithUnicodePropertyName>(GoodJson);
            Assert.Equal(1, obj.Property\u04671);

            obj = JsonSerializer.Deserialize<ClassWithUnicodePropertyName>(GoodJsonEscaped);
            Assert.Equal(1, obj.Property\u04671);

            JsonException e;

            // Exception.
            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithUnicodePropertyName>(BadJson));
            Assert.Equal(Expected, e.Path);

            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithUnicodePropertyName>(BadJsonEscaped));
            Assert.Equal(Expected, e.Path);
        }

        [Fact]
        public static void PathForArrayFails()
        {
            try
            {
                JsonSerializer.Deserialize<int[]>(@"[1, bad]");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$[1]", e.Path);
            }

            try
            {
                JsonSerializer.Deserialize<int[]>(@"[1,");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$[1]", e.Path);
            }

            try
            {
                JsonSerializer.Deserialize<int[]>(@"[1");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                // No delimiter.
                Assert.Equal("$[0]", e.Path);
            }

            try
            {
                JsonSerializer.Deserialize<int[]>(@"[1 /* comment starts but doesn't end");
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
                JsonSerializer.Deserialize<List<int>>(@"[1, bad]");
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
                JsonSerializer.Deserialize<int[][]>(@"[[1, 2],[3,bad]]");
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
                JsonSerializer.Deserialize<List<List<int>>>(@"[[1, 2],[3,bad]]");
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
                JsonSerializer.Deserialize<RootClass>(@"{""Child"":{""MyInt"":bad]}");
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
                JsonSerializer.Deserialize<RootClass>(@"{""Child"":{""MyIntArray"":[1, bad]}");
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
                JsonSerializer.Deserialize<RootClass>(@"{""Child"":{""MyDictionary"":{""Key"": bad]");
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
                JsonSerializer.Deserialize<RootClass>(@"{""Child"":{""MyDictionary"":{""Key1"":{""Children"":[{""MyDictionary"":{""K.e.y"":""");
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
                JsonSerializer.Deserialize<RootClass>(@"{""Child"":{""Children"":[{}, {""MyDictionary"":{""K.e.y"": {""MyInt"":bad");
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
                ClassWithUnicodeProperty obj = JsonSerializer.Deserialize<ClassWithUnicodeProperty>("{\"A\u0467\":bad}");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$.A\u0467", e.Path);
            }
        }

        [Fact]
        public static void CaseInsensitiveFails()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;

            // Baseline (no exception)
            {
                SimpleTestClass obj = JsonSerializer.Deserialize<SimpleTestClass>(@"{""myint32"":1}", options);
                Assert.Equal(1, obj.MyInt32);
            }

            {
                SimpleTestClass obj = JsonSerializer.Deserialize<SimpleTestClass>(@"{""MYINT32"":1}", options);
                Assert.Equal(1, obj.MyInt32);
            }

            try
            {
                JsonSerializer.Deserialize<SimpleTestClass>(@"{""myint32"":bad}", options);
            }
            catch (JsonException e)
            {
                // The Path should reflect the case even though it is different from the property.
                Assert.Equal("$.myint32", e.Path);
            }

            try
            {
                JsonSerializer.Deserialize<SimpleTestClass>(@"{""MYINT32"":bad}", options);
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

        private class ClassWithPropertyToClassWithInvalidArray
        {
            public ClassWithInvalidArray Inner { get; set; } = new ClassWithInvalidArray();
        }

        private class ClassWithInvalidArray
        {
            public int[,] UnsupportedArray { get; set; }
        }

        private class ClassWithInvalidDictionary
        {
            public Dictionary<string, int[,]> UnsupportedDictionary { get; set; }
        }

        [Fact]
        public static void ClassWithUnsupportedArray()
        {
            Exception ex = Assert.Throws<NotSupportedException>(() =>
                JsonSerializer.Deserialize<ClassWithInvalidArray>(@"{""UnsupportedArray"":[]}"));

            // The exception contains the type.
            Assert.Contains(typeof(int[,]).ToString(), ex.Message);

            ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(new ClassWithInvalidArray()));
            Assert.Contains(typeof(int[,]).ToString(), ex.Message);
            Assert.DoesNotContain("Path: ", ex.Message);
        }

        [Fact]
        public static void ClassWithUnsupportedArrayInProperty()
        {
            Exception ex = Assert.Throws<NotSupportedException>(() =>
                JsonSerializer.Deserialize<ClassWithPropertyToClassWithInvalidArray>(@"{""Inner"":{""UnsupportedArray"":[]}}"));

            // The exception contains the type and Path.
            Assert.Contains(typeof(int[,]).ToString(), ex.Message);
            Assert.Contains("Path: $.Inner | LineNumber: 0 | BytePositionInLine: 10.", ex.Message);

            ex = Assert.Throws<NotSupportedException>(() =>
                JsonSerializer.Serialize(new ClassWithPropertyToClassWithInvalidArray()));

            Assert.Contains(typeof(int[,]).ToString(), ex.Message);
            Assert.Contains(typeof(ClassWithInvalidArray).ToString(), ex.Message);
            Assert.Contains("Path: $.Inner.", ex.Message);

            // The original exception contains the type.
            Assert.NotNull(ex.InnerException);
            Assert.Contains(typeof(int[,]).ToString(), ex.InnerException.Message);
            Assert.DoesNotContain("Path: ", ex.InnerException.Message);
        }

        [Fact]
        public static void ClassWithUnsupportedDictionary()
        {
            Exception ex = Assert.Throws<NotSupportedException>(() =>
                JsonSerializer.Deserialize<ClassWithInvalidDictionary>(@"{""UnsupportedDictionary"":{}}"));

            Assert.Contains("System.Int32[,]", ex.Message);

            // The exception for element types does not contain the parent type and the property name
            // since the verification occurs later and is no longer bound to the parent type.
            Assert.DoesNotContain("ClassWithInvalidDictionary.UnsupportedDictionary", ex.Message);

            // The exception for element types includes Path.
            Assert.Contains("Path: $.UnsupportedDictionary", ex.Message);

            // Serializing works for unsupported types if the property is null; elements are not verified until serialization occurs.
            var obj = new ClassWithInvalidDictionary();
            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(@"{""UnsupportedDictionary"":null}", json);

            obj.UnsupportedDictionary = new Dictionary<string, int[,]>();
            ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(obj));

            // The exception contains the type and Path.
            Assert.Contains(typeof(int[,]).ToString(), ex.Message);
            Assert.Contains("Path: $.UnsupportedDictionary", ex.Message);

            // The original exception contains the type.
            Assert.NotNull(ex.InnerException);
            Assert.Contains(typeof(int[,]).ToString(), ex.InnerException.Message);
            Assert.DoesNotContain("Path: ", ex.InnerException.Message);
        }

        [Fact]
        public static void UnsupportedTypeFromRoot()
        {
            Exception ex = Assert.Throws<NotSupportedException>(() =>
                JsonSerializer.Deserialize<int[,]>(@"[]"));

            Assert.Contains(typeof(int[,]).ToString(), ex.Message);

            // Root-level Types (not from a property) do not include the Path.
            Assert.DoesNotContain("Path: $", ex.Message);
        }

        [Fact]
        public static void DeserializeUnsupportedType()
        {
            // Any test payload is fine.
            string json = @"""Some string""";

            RunTest<Type>(json);
            RunTest<SerializationInfo>(json);
            RunTest<IntPtr>(json);
            RunTest<UIntPtr>(json);

            void RunTest<T>(string json)
            {
                string fullName = typeof(T).FullName;

                NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<T>(json));
                string exAsStr = ex.ToString();
                Assert.Contains(fullName, exAsStr);
                Assert.Contains("$", exAsStr);

                json = $@"{{""Prop"":{json}}}";

                ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ClassWithType<T>>(json));
                exAsStr = ex.ToString();
                Assert.Contains(fullName, exAsStr);
                Assert.Contains("$.Prop", exAsStr);

                // NSE is not thrown because the serializer handles null.
                if (!typeof(T).IsValueType)
                {
                    Assert.Null(JsonSerializer.Deserialize<T>("null"));

                    ClassWithType<T> obj = JsonSerializer.Deserialize<ClassWithType<T>>(@"{""Prop"":null}");
                    Assert.Null(obj.Prop);
                }
            }
        }

        [Fact]
        public static void SerializeUnsupportedType()
        {
            RunTest<Type>(typeof(int));
            RunTest<SerializationInfo>(new SerializationInfo(typeof(Type), new FormatterConverter()));
            RunTest<IntPtr>((IntPtr)123);
            RunTest<UIntPtr>((UIntPtr)123);

            void RunTest<T>(T value)
            {
                Type type = typeof(T);
                string fullName = type.FullName;

                NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(value));
                string exAsStr = ex.ToString();
                Assert.Contains(fullName, exAsStr);
                Assert.Contains("$", exAsStr);

                ClassWithType<T> obj = new ClassWithType<T> { Prop = value };

                ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(obj));
                exAsStr = ex.ToString();
                Assert.Contains(fullName, exAsStr);
                Assert.Contains("$.Prop", exAsStr);

                if (!type.IsValueType)
                {
                    string serialized = JsonSerializer.Serialize((T)(object)null);
                    Assert.Equal("null", serialized);

                    obj.Prop = (T)(object)null;
                    serialized = JsonSerializer.Serialize(obj);
                    Assert.Equal(@"{""Prop"":null}", serialized);

                    serialized = JsonSerializer.Serialize(obj, new JsonSerializerOptions { IgnoreNullValues = true });
                    Assert.Equal(@"{}", serialized);
                }
            }
        }

        [Theory]
        [InlineData(typeof(ClassWithBadCtor))]
        [InlineData(typeof(StructWithBadCtor))]
        public static void TypeWithBadCtorNoProps(Type type)
        {
            var instance = Activator.CreateInstance(
                type,
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] { new SerializationInfo(typeof(Type), new FormatterConverter()), new StreamingContext(default) },
                culture: null)!;

            Assert.Equal("{}", JsonSerializer.Serialize(instance, type));

            // Each constructor parameter must bind to an object property or field.
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize("{}", type));
            string exAsStr = ex.ToString();
            Assert.Contains(typeof(SerializationInfo).FullName, exAsStr);
            Assert.Contains(typeof(StreamingContext).FullName, exAsStr);
        }

        [Theory]
        [InlineData(typeof(ClassWithBadCtor_WithProps))]
        [InlineData(typeof(StructWithBadCtor_WithProps))]
        public static void TypeWithBadCtorWithPropsInvalid(Type type)
        {
            var instance = Activator.CreateInstance(
                type,
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] { new SerializationInfo(typeof(Type), new FormatterConverter()), new StreamingContext(default) },
                culture: null)!;

            string serializationInfoName = typeof(SerializationInfo).FullName;

            // (De)serialization of SerializationInfo type is not supported.

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(instance, type));
            string exAsStr = ex.ToString();
            Assert.Contains(serializationInfoName, exAsStr);
            Assert.Contains("$.Info", exAsStr);

            ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize(@"{""Info"":{}}", type));
            exAsStr = ex.ToString();
            Assert.Contains(serializationInfoName, exAsStr);
            Assert.Contains("$.Info", exAsStr);

            // Deserialization of null is okay since no data is read.
            object obj = JsonSerializer.Deserialize(@"{""Info"":null}", type);
            Assert.Null(type.GetProperty("Info").GetValue(obj));

            // Deserialization of other non-null tokens is not okay.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize(@"{""Info"":1}", type));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize(@"{""Info"":""""}", type));
        }

        public class ClassWithBadCtor
        {
            public ClassWithBadCtor(SerializationInfo info, StreamingContext ctx) { }
        }

        public struct StructWithBadCtor
        {
            [JsonConstructor]
            public StructWithBadCtor(SerializationInfo info, StreamingContext ctx) { }
        }

        public class ClassWithBadCtor_WithProps
        {
            public SerializationInfo Info { get; set; }

            public StreamingContext Ctx { get; set; }

            public ClassWithBadCtor_WithProps(SerializationInfo info, StreamingContext ctx) =>
                (Info, Ctx) = (info, ctx);
        }

        public struct StructWithBadCtor_WithProps
        {
            public SerializationInfo Info { get; set; }

            public StreamingContext Ctx { get; set; }

            [JsonConstructor]
            public StructWithBadCtor_WithProps(SerializationInfo info, StreamingContext ctx) =>
                (Info, Ctx) = (info, ctx);
        }
    }
}
