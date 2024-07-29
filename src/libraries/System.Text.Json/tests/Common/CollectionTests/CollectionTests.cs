// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests : SerializerTests
    {
        public CollectionTests(JsonSerializerWrapper stringSerializerWrapper)
            : base(stringSerializerWrapper) { }

        [Fact]
        public async Task RoundtripClassWithRecursiveCollectionProperties()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/76802

            var value = new ClassWithRecursiveCollectionTypes
            {
                Nested = new(),
                List = new() { new() },
                Dictionary = new Dictionary<string, ClassWithRecursiveCollectionTypes>
                {
                    ["key"] = new()
                },
            };

            string expectedJson = """
                {
                    "Nested" : {"Nested":null,"List":null,"Dictionary":null},
                    "List" : [{"Nested":null,"List":null,"Dictionary":null}],
                    "Dictionary" : {"key" : {"Nested":null,"List":null,"Dictionary":null}}
                }
                """;

            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual(expectedJson, json);

            value = await Serializer.DeserializeWrapper<ClassWithRecursiveCollectionTypes>(json);
            Assert.NotNull(value.Nested);
            Assert.Null(value.Nested.Nested);
            Assert.NotNull(value.List);
            Assert.NotNull(value.Dictionary);
        }
    }
}
