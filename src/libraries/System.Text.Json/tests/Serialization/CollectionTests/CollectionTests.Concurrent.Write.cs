// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CollectionTests
    {
        [Fact]
        public static void Write_ConcurrentCollection()
        {
            Assert.Equal(@"[""1""]", JsonSerializer.Serialize(new BlockingCollection<string> { "1" }));

            Assert.Equal(@"[""1""]", JsonSerializer.Serialize(new ConcurrentBag<string> { "1" }));

            Assert.Equal(@"{""key"":""value""}", JsonSerializer.Serialize(new ConcurrentDictionary<string, string> { ["key"] = "value" }));

            ConcurrentQueue<string> qc = new ConcurrentQueue<string>();
            qc.Enqueue("1");
            Assert.Equal(@"[""1""]", JsonSerializer.Serialize(qc));

            ConcurrentStack<string> qs = new ConcurrentStack<string>();
            qs.Push("1");
            Assert.Equal(@"[""1""]", JsonSerializer.Serialize(qs));
        }
    }
}
