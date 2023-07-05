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
            Assert.Equal(@"{""Data"":4}", await Serializer.SerializeWrapper(new BitVector32(4)));
            Assert.Equal(@"{""Data"":4}", await Serializer.SerializeWrapper<object>(new BitVector32(4)));

            Assert.Equal(@"{""key"":""value""}", await Serializer.SerializeWrapper(new HybridDictionary { ["key"] = "value" }));
            Assert.Equal(@"{""key"":""value""}", await Serializer.SerializeWrapper<object>(new HybridDictionary { ["key"] = "value" }));

            Assert.Equal(@"{""key"":""value""}", await Serializer.SerializeWrapper(new OrderedDictionary { ["key"] = "value" }));
            Assert.Equal(@"{""key"":""value""}", await Serializer.SerializeWrapper<IOrderedDictionary>(new OrderedDictionary { ["key"] = "value" }));
            Assert.Equal(@"{""key"":""value""}", await Serializer.SerializeWrapper<object>(new OrderedDictionary { ["key"] = "value" }));

            Assert.Equal(@"{""key"":""value""}", await Serializer.SerializeWrapper(new ListDictionary { ["key"] = "value" }));
            Assert.Equal(@"{""key"":""value""}", await Serializer.SerializeWrapper<object>(new ListDictionary { ["key"] = "value" }));

            Assert.Equal(@"[""1"",""2""]", await Serializer.SerializeWrapper(new StringCollection { "1", "2" }));
            Assert.Equal(@"[""1"",""2""]", await Serializer.SerializeWrapper<object>(new StringCollection { "1", "2" }));

            Assert.Equal(@"[{""Key"":""key"",""Value"":""value""}]", await Serializer.SerializeWrapper(new StringDictionary { ["key"] = "value" }));
            Assert.Equal(@"[{""Key"":""key"",""Value"":""value""}]", await Serializer.SerializeWrapper<object>(new StringDictionary { ["key"] = "value" }));

            // Element type returned by .GetEnumerator for this type is string, specifically the key.
            Assert.Equal(@"[""key""]", await Serializer.SerializeWrapper(new NameValueCollection { ["key"] = "value" }));
        }
    }
}
