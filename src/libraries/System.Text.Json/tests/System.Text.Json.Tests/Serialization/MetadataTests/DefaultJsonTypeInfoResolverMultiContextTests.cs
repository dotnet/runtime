// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public partial class DefaultJsonTypeInfoResolverMultiContextTests : SerializerTests
    {
        public DefaultJsonTypeInfoResolverMultiContextTests()
            : base(JsonSerializerWrapper.StringSerializer)
        {
        }

        [Fact]
        public async Task TypeInfoWithNullCreateObjectFailsDeserialization()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add(ti =>
            {
                if (ti.Type == typeof(Poco))
                {
                    ti.CreateObject = null;
                }
            });

            JsonSerializerOptions o = new();
            o.TypeInfoResolver = resolver;

            string json = """{"StringProperty":"test"}""";
            await TestMultiContextDeserialization<Poco>(json, new Poco() { StringProperty = "test" });
            await TestMultiContextDeserialization<Poco>(json, options: o, expectedExceptionType: typeof(NotSupportedException));

            Assert.Throws<InvalidOperationException>(() => resolver.Modifiers.Add(ti => { }));
        }

        [Theory]
        [MemberData(nameof(JsonSerializerSerializeWithTypeInfoOfT_TestData))]
        public async Task JsonSerializerSerializeWithTypeInfoOfT<T>(T testObj, string expectedJson)
        {
            DefaultJsonTypeInfoResolver r = new();
            JsonSerializerOptions o = new();
            o.TypeInfoResolver = r;
            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)r.GetTypeInfo(typeof(T), o);
            string json = await Serializer.SerializeWrapper(testObj, typeInfo);
            Assert.Equal(expectedJson, json);
        }

        public static IEnumerable<object[]> JsonSerializerSerializeWithTypeInfoOfT_TestData()
        {
            yield return new object[] { "value", @"""value""" };
            yield return new object[] { 5, @"5" };
            yield return new object[] { new SomeClass() { IntProp = 15, ObjProp = 17m }, @"{""ObjProp"":17,""IntProp"":15}" };
        }

        private class Poco
        {
            public string StringProperty { get; set; }
        }

        private class SomeClass
        {
            public object ObjProp { get; set; }
            public int IntProp { get; set; }
        }
    }
}
