// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ValueTests
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

        [Fact]
        public static void Read_ConcurrentCollection_Throws()
        {
            // Not supported. Not IList, and we don't detect the add method for this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<BlockingCollection<string>>(@"[""1""]"));

            // Not supported. Not IList, and we don't detect the add method for this collection.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ConcurrentBag<string>>(@"[""1""]"));
        }
    }
}
