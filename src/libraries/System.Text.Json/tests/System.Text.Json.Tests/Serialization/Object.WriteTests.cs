// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Encodings.Web;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ObjectTests
    {
        [Fact]
        public static void VerifyTypeFail()
        {
            Assert.Throws<ArgumentException>(() => JsonSerializer.Serialize(1, typeof(string)));
        }

        [Theory]
        [MemberData(nameof(WriteSuccessCases))]
        public static void Write(ITestClass testObj)
        {
            var options = new JsonSerializerOptions { IncludeFields = true };
            string json;

            {
                testObj.Initialize();
                testObj.Verify();
                json = JsonSerializer.Serialize(testObj, testObj.GetType(), options);
            }

            {
                ITestClass obj = (ITestClass)JsonSerializer.Deserialize(json, testObj.GetType(), options);
                obj.Verify();
            }
        }

        public static IEnumerable<object[]> WriteSuccessCases
        {
            get
            {
                return TestData.WriteSuccessCases;
            }
        }

        [Fact]
        public static void WriteObjectAsObject()
        {
            var obj = new ObjectObject { Object = new object() };
            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(@"{""Object"":{}}", json);
        }

        public class ObjectObject
        {
            public object Object { get; set; }
        }

        [Fact]
        public static void WriteObject_PublicIndexer()
        {
            var indexer = new Indexer();
            indexer[42] = 42;
            indexer.NonIndexerProp = "Value";
            Assert.Equal(@"{""NonIndexerProp"":""Value""}", JsonSerializer.Serialize(indexer));
        }

        private class Indexer
        {
            private int _index = -1;

            public int this[int index]
            {
                get => _index;
                set => _index = value;
            }

            public string NonIndexerProp { get; set; }
        }

        [Fact]
        public static void WriteObjectWorks_ReferenceTypeMissingPublicParameterlessConstructor()
        {
            PublicParameterizedConstructorTestClass parameterless = PublicParameterizedConstructorTestClass.Instance;
            Assert.Equal("{\"Name\":\"42\"}", JsonSerializer.Serialize(parameterless));

            ClassWithInternalParameterlessCtor internalObj = ClassWithInternalParameterlessCtor.Instance;
            Assert.Equal("{\"Name\":\"InstancePropertyInternal\"}", JsonSerializer.Serialize(internalObj));

            ClassWithPrivateParameterlessCtor privateObj = ClassWithPrivateParameterlessCtor.Instance;
            Assert.Equal("{\"Name\":\"InstancePropertyPrivate\"}", JsonSerializer.Serialize(privateObj));

            var list = new CollectionWithoutPublicParameterlessCtor(new List<object> { 1, "foo", false });
            Assert.Equal("[1,\"foo\",false]", JsonSerializer.Serialize(list));

            var envelopeList = new List<object>()
            {
                ConcreteDerivedClassWithNoPublicDefaultCtor.Error("oops"),
                ConcreteDerivedClassWithNoPublicDefaultCtor.Ok<string>(),
                ConcreteDerivedClassWithNoPublicDefaultCtor.Ok<int>(),
                ConcreteDerivedClassWithNoPublicDefaultCtor.Ok()
            };
            Assert.Equal("[{\"ErrorString\":\"oops\",\"Result\":null},{\"Result\":null},{\"Result\":0},{\"ErrorString\":\"ok\",\"Result\":null}]", JsonSerializer.Serialize(envelopeList));
        }

        [Fact]
        public static void WritePolymorphicSimple()
        {
            string json = JsonSerializer.Serialize(new { Prop = (object)new[] { 0 } });
            Assert.Equal(@"{""Prop"":[0]}", json);
        }

        [Fact]
        public static void WritePolymorphicDifferentAttributes()
        {
            string json = JsonSerializer.Serialize(new Polymorphic());
            Assert.Equal(@"{""P1"":"""",""p_3"":""""}", json);
        }

        private class Polymorphic
        {
            public object P1 => "";

            [JsonIgnore]
            public object P2 => "";

            [JsonPropertyName("p_3")]
            public object P3 => "";
        }

        // https://github.com/dotnet/runtime/issues/30814
        [Fact]
        public static void EscapingShouldntStackOverflow()
        {
            var test = new { Name = "\u6D4B\u8A6611" };

            var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            string result = JsonSerializer.Serialize(test, options);

            Assert.Equal("{\"name\":\"\u6D4B\u8A6611\"}", result);
        }
    }
}
