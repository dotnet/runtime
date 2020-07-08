// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CollectionTests
    {
        [Fact]
        public static void Read_ConcurrentCollection()
        {
            ConcurrentDictionary<string, string> cd = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(@"{""key"":""value""}");
            Assert.Equal(1, cd.Count);
            Assert.Equal("value", cd["key"]);

            ConcurrentQueue<string> qc = JsonSerializer.Deserialize<ConcurrentQueue<string>>(@"[""1""]");
            Assert.Equal(1, qc.Count);
            bool found = qc.TryPeek(out string val);
            Assert.True(found);
            Assert.Equal("1", val);

            ConcurrentStack<string> qs = JsonSerializer.Deserialize<ConcurrentStack<string>>(@"[""1""]");
            Assert.Equal(1, qs.Count);
            found = qs.TryPeek(out val);
            Assert.True(found);
            Assert.Equal("1", val);
        }

        [Theory]
        [InlineData(typeof(BlockingCollection<string>), @"[""1""]")] // Not supported. Not IList, and we don't detect the add method for this collection.
        [InlineData(typeof(ConcurrentBag<string>), @"[""1""]")] // Not supported. Not IList, and we don't detect the add method for this collection.
        public static void Read_ConcurrentCollection_Throws(Type type, string json)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize(json, type));
            Assert.Contains(type.ToString(), ex.Message);
        }

        [Theory]
        [InlineData(typeof(GenericConcurrentQueuePrivateConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericConcurrentQueueInternalConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericConcurrentStackPrivateConstructor<string>), @"[""1""]")]
        [InlineData(typeof(GenericConcurrentStackInternalConstructor<string>), @"[""1""]")]
        public static void Read_ConcurrentCollection_NoPublicConstructor_Throws(Type type, string json)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize(json, type));
            Assert.Contains(type.ToString(), ex.Message);
        }
    }
}
