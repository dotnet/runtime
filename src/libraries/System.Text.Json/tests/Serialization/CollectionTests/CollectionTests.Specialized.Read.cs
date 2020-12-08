// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CollectionTests
    {
        [Fact]
        public static void Read_SpecializedCollection()
        {
            BitVector32 bv32 = JsonSerializer.Deserialize<BitVector32>(@"{""Data"":4}");
            // Data property is skipped because it doesn't have a setter.
            Assert.Equal(0, bv32.Data);

            HybridDictionary hd = JsonSerializer.Deserialize<HybridDictionary>(@"{""key"":""value""}");
            Assert.Equal(1, hd.Count);
            Assert.Equal("value", ((JsonElement)hd["key"]).GetString());

            IOrderedDictionary iod = JsonSerializer.Deserialize<OrderedDictionary>(@"{""key"":""value""}");
            Assert.Equal(1, iod.Count);
            Assert.Equal("value", ((JsonElement)iod["key"]).GetString());

            ListDictionary ld = JsonSerializer.Deserialize<ListDictionary>(@"{""key"":""value""}");
            Assert.Equal(1, ld.Count);
            Assert.Equal("value", ((JsonElement)ld["key"]).GetString());
        }

        [Fact]
        public static void Read_SpecializedCollection_Throws()
        {
            // Add method for this collection only accepts strings, even though it only implements IList which usually
            // indicates that the element type is typeof(object).
            Assert.Throws<InvalidCastException>(() => JsonSerializer.Deserialize<StringCollection>(@"[""1"", ""2""]"));

            // Not supported. Not IList, and we don't detect the add method for this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<StringDictionary>(@"[{""Key"": ""key"",""Value"":""value""}]"));

            // Int key is not allowed.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<HybridDictionary>(@"{1:""value""}"));

            // Runtime type in this case is IOrderedDictionary (we don't replace with concrete type), which we can't instantiate.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<IOrderedDictionary>(@"{""first"":""John"",""second"":""Jane"",""third"":""Jet""}"));

            // Not supported. Not IList, and we don't detect the add method for this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<NameValueCollection>(@"[""NameValueCollection""]"));
        }
    }
}
