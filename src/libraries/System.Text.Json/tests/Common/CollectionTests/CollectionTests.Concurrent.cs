// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests
    {
        [Fact]
        public async Task Read_ConcurrentCollection()
        {
            ConcurrentDictionary<string, string> cd = await JsonSerializerWrapperForString.DeserializeWrapper<ConcurrentDictionary<string, string>>(@"{""key"":""value""}");
            Assert.Equal(1, cd.Count);
            Assert.Equal("value", cd["key"]);

            ConcurrentQueue<string> qc = await JsonSerializerWrapperForString.DeserializeWrapper<ConcurrentQueue<string>>(@"[""1""]");
            Assert.Equal(1, qc.Count);
            bool found = qc.TryPeek(out string val);
            Assert.True(found);
            Assert.Equal("1", val);

            ConcurrentStack<string> qs = await JsonSerializerWrapperForString.DeserializeWrapper<ConcurrentStack<string>>(@"[""1""]");
            Assert.Equal(1, qs.Count);
            found = qs.TryPeek(out val);
            Assert.True(found);
            Assert.Equal("1", val);
        }

        [Theory]
        [InlineData(typeof(BlockingCollection<string>), @"[""1""]")] // Not supported. Not IList, and we don't detect the add method for this collection.
        [InlineData(typeof(ConcurrentBag<string>), @"[""1""]")] // Not supported. Not IList, and we don't detect the add method for this collection.
        public async Task Read_ConcurrentCollection_Throws(Type type, string json)
        {
            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper(json, type));
            Assert.Contains(type.ToString(), ex.Message);
        }

        [Theory]
        [InlineData(typeof(GenericConcurrentQueuePrivateConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericConcurrentQueueInternalConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericConcurrentStackPrivateConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericConcurrentStackInternalConstructor<string>), @"[""1""]")]
        public async Task Read_ConcurrentCollection_NoPublicConstructor_Throws(Type type, string json)
        {
            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper(json, type));
            Assert.Contains(type.ToString(), ex.Message);
        }

        [Fact]
        public async Task Write_ConcurrentCollection()
        {
            Assert.Equal(@"[""1""]", await JsonSerializerWrapperForString.SerializeWrapper(new BlockingCollection<string> { "1" }));

            Assert.Equal(@"[""1""]", await JsonSerializerWrapperForString.SerializeWrapper(new ConcurrentBag<string> { "1" }));

            Assert.Equal(@"{""key"":""value""}", await JsonSerializerWrapperForString.SerializeWrapper(new ConcurrentDictionary<string, string> { ["key"] = "value" }));

            ConcurrentQueue<string> qc = new ConcurrentQueue<string>();
            qc.Enqueue("1");
            Assert.Equal(@"[""1""]", await JsonSerializerWrapperForString.SerializeWrapper(qc));

            ConcurrentStack<string> qs = new ConcurrentStack<string>();
            qs.Push("1");
            Assert.Equal(@"[""1""]", await JsonSerializerWrapperForString.SerializeWrapper(qs));
        }
    }
}
