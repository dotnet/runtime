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
        public async Task Write_SpecializedCollection()
        {
            Assert.Equal(@"{""Data"":4}", await JsonSerializerWrapperForString.SerializeWrapper(new BitVector32(4)));
            Assert.Equal(@"{""Data"":4}", await JsonSerializerWrapperForString.SerializeWrapper<object>(new BitVector32(4)));

            Assert.Equal(@"{""key"":""value""}", await JsonSerializerWrapperForString.SerializeWrapper(new HybridDictionary { ["key"] = "value" }));
            Assert.Equal(@"{""key"":""value""}", await JsonSerializerWrapperForString.SerializeWrapper<object>(new HybridDictionary { ["key"] = "value" }));

            Assert.Equal(@"{""key"":""value""}", await JsonSerializerWrapperForString.SerializeWrapper(new OrderedDictionary { ["key"] = "value" }));
            Assert.Equal(@"{""key"":""value""}", await JsonSerializerWrapperForString.SerializeWrapper<IOrderedDictionary>(new OrderedDictionary { ["key"] = "value" }));
            Assert.Equal(@"{""key"":""value""}", await JsonSerializerWrapperForString.SerializeWrapper<object>(new OrderedDictionary { ["key"] = "value" }));

            Assert.Equal(@"{""key"":""value""}", await JsonSerializerWrapperForString.SerializeWrapper(new ListDictionary { ["key"] = "value" }));
            Assert.Equal(@"{""key"":""value""}", await JsonSerializerWrapperForString.SerializeWrapper<object>(new ListDictionary { ["key"] = "value" }));

            Assert.Equal(@"[""1"",""2""]", await JsonSerializerWrapperForString.SerializeWrapper(new StringCollection { "1", "2" }));
            Assert.Equal(@"[""1"",""2""]", await JsonSerializerWrapperForString.SerializeWrapper<object>(new StringCollection { "1", "2" }));

            Assert.Equal(@"[{""Key"":""key"",""Value"":""value""}]", await JsonSerializerWrapperForString.SerializeWrapper(new StringDictionary { ["key"] = "value" }));
            Assert.Equal(@"[{""Key"":""key"",""Value"":""value""}]", await JsonSerializerWrapperForString.SerializeWrapper<object>(new StringDictionary { ["key"] = "value" }));

            // Element type returned by .GetEnumerator for this type is string, specifically the key.
            Assert.Equal(@"[""key""]", await JsonSerializerWrapperForString.SerializeWrapper(new NameValueCollection { ["key"] = "value" }));
        }
    }
}
