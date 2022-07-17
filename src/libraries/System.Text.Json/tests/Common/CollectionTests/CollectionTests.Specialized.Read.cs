// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests
    {
        [Fact]
        public async Task Read_SpecializedCollection()
        {
            BitVector32 bv32 = await Serializer.DeserializeWrapper<BitVector32>(@"{""Data"":4}");
            // Data property is skipped because it doesn't have a setter.
            Assert.Equal(0, bv32.Data);

            HybridDictionary hd = await Serializer.DeserializeWrapper<HybridDictionary>(@"{""key"":""value""}");
            Assert.Equal(1, hd.Count);
            Assert.Equal("value", ((JsonElement)hd["key"]).GetString());

            IOrderedDictionary iod = await Serializer.DeserializeWrapper<OrderedDictionary>(@"{""key"":""value""}");
            Assert.Equal(1, iod.Count);
            Assert.Equal("value", ((JsonElement)iod["key"]).GetString());

            ListDictionary ld = await Serializer.DeserializeWrapper<ListDictionary>(@"{""key"":""value""}");
            Assert.Equal(1, ld.Count);
            Assert.Equal("value", ((JsonElement)ld["key"]).GetString());
        }

        [Fact]
        public async Task Read_SpecializedCollection_Throws()
        {
            // Add method for this collection only accepts strings, even though it only implements IList which usually
            // indicates that the element type is typeof(object).
            await Assert.ThrowsAsync<InvalidCastException>(async () => await Serializer.DeserializeWrapper<StringCollection>(@"[""1"", ""2""]"));

            // Not supported. Not IList, and we don't detect the add method for this collection.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.DeserializeWrapper<StringDictionary>(@"[{""Key"": ""key"",""Value"":""value""}]"));

            // Int key is not allowed.
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<HybridDictionary>(@"{1:""value""}"));

            // Runtime type in this case is IOrderedDictionary (we don't replace with concrete type), which we can't instantiate.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.DeserializeWrapper<IOrderedDictionary>(@"{""first"":""John"",""second"":""Jane"",""third"":""Jet""}"));

            // Not supported. Not IList, and we don't detect the add method for this collection.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.DeserializeWrapper<NameValueCollection>(@"[""NameValueCollection""]"));
        }
    }
}
