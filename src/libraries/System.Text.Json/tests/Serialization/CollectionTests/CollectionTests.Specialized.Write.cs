// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CollectionTests
    {
        [Fact]
        public static void Write_SpecializedCollection()
        {
            Assert.Equal(@"{""Data"":4}", JsonSerializer.Serialize(new BitVector32(4)));
            Assert.Equal(@"{""Data"":4}", JsonSerializer.Serialize<object>(new BitVector32(4)));

            Assert.Equal(@"{""key"":""value""}", JsonSerializer.Serialize(new HybridDictionary { ["key"] = "value" }));
            Assert.Equal(@"{""key"":""value""}", JsonSerializer.Serialize<object>(new HybridDictionary { ["key"] = "value" }));

            Assert.Equal(@"{""key"":""value""}", JsonSerializer.Serialize(new OrderedDictionary { ["key"] = "value" }));
            Assert.Equal(@"{""key"":""value""}", JsonSerializer.Serialize<IOrderedDictionary>(new OrderedDictionary { ["key"] = "value" }));
            Assert.Equal(@"{""key"":""value""}", JsonSerializer.Serialize<object>(new OrderedDictionary { ["key"] = "value" }));

            Assert.Equal(@"{""key"":""value""}", JsonSerializer.Serialize(new ListDictionary { ["key"] = "value" }));
            Assert.Equal(@"{""key"":""value""}", JsonSerializer.Serialize<object>(new ListDictionary { ["key"] = "value" }));

            Assert.Equal(@"[""1"",""2""]", JsonSerializer.Serialize(new StringCollection { "1", "2" }));
            Assert.Equal(@"[""1"",""2""]", JsonSerializer.Serialize<object>(new StringCollection { "1", "2" }));

            Assert.Equal(@"[{""Key"":""key"",""Value"":""value""}]", JsonSerializer.Serialize(new StringDictionary { ["key"] = "value" }));
            Assert.Equal(@"[{""Key"":""key"",""Value"":""value""}]", JsonSerializer.Serialize<object>(new StringDictionary { ["key"] = "value" }));

            // Element type returned by .GetEnumerator for this type is string, specifically the key.
            Assert.Equal(@"[""key""]", JsonSerializer.Serialize(new NameValueCollection { ["key"] = "value" }));
        }
    }
}
