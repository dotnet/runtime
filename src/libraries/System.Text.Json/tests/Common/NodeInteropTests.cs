// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Tests.Schemas.OrderPayload;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract class NodeInteropTests : SerializerTests
    {
        public NodeInteropTests(JsonSerializerWrapper serializerWrapper) : base(serializerWrapper) { }

        [Fact]
        public async Task CompareResultsAgainstSerializer()
        {
            List<Order> obj = Order.PopulateLargeObject(2);
            string expected = await Serializer.SerializeWrapper(obj);

            JsonArray jArray = await Serializer.DeserializeWrapper<JsonArray>(expected);
            string actual = jArray.ToJsonString();
            Assert.Equal(expected, actual);

            jArray = JsonNode.Parse(expected).AsArray();
            actual = jArray.ToJsonString();
            Assert.Equal(expected, actual);
        }

        public class Poco
        {
            public string MyString { get; set; }
            public JsonNode Node { get; set; }
            public JsonArray Array { get; set; }
            public JsonValue Value { get; set; }
            public JsonValue IntValue { get; set; }
            public JsonObject Object { get; set; }
        }

        [Fact]
        public async Task NodesAsPocoProperties()
        {
            const string Expected = "{\"MyString\":null,\"Node\":42,\"Array\":[43],\"Value\":44,\"IntValue\":45,\"Object\":{\"Property\":46}}";

            var poco = new Poco
            {
                Node = 42,
                Array = new JsonArray(43),
                Value = (JsonValue)44,
                IntValue = (JsonValue)45,
                Object = new JsonObject
                {
                    ["Property"] = 46
                }
            };

            string json = await Serializer.SerializeWrapper(poco);
            Assert.Equal(Expected, json);

            poco = await Serializer.DeserializeWrapper<Poco>(json);
            Assert.Equal(42, (int)poco.Node);
            Assert.Equal(43, (int)poco.Array[0]);
            Assert.Equal(44, (int)poco.Value);
            Assert.Equal(45, (int)poco.IntValue);
            Assert.Equal(46, (int)poco.Object["Property"]);
        }
    }
}
