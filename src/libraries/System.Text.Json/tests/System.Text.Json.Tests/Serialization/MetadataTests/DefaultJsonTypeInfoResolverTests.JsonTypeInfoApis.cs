// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    // TODO: ensure converter rooting happens for APIs taking JsonTypeInfo
    public static partial class DefaultJsonTypeInfoResolverTests
    {
        [Theory]
        [InlineData("value", @"""value""")]
        [InlineData(5, @"5")]
        [MemberData(nameof(JsonSerializerSerializeWithTypeInfoOfT_TestData))]
        public static void JsonSerializerSerializeWithTypeInfoOfT<T>(T testObj, string expectedJson)
        {
            DefaultJsonTypeInfoResolver r = new();
            JsonSerializerOptions o = new();
            o.TypeInfoResolver = r;
            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)r.GetTypeInfo(typeof(T), o);
            string json = JsonSerializer.Serialize(testObj, typeInfo);
            Assert.Equal(expectedJson, json);
        }

        public static IEnumerable<object[]> JsonSerializerSerializeWithTypeInfoOfT_TestData()
        {
            yield return new object[] { new SomeClass() { IntProp = 15, ObjProp = 17m }, @"{""ObjProp"":17,""IntProp"":15}" };
        }
    }
}
