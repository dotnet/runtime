// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CollectionTests
    {
        [Fact]
        public static void Read_ObjectModelCollection()
        {
            Collection<bool> c = JsonSerializer.Deserialize<Collection<bool>>("[true,false]");
            Assert.Equal(2, c.Count);
            Assert.True(c[0]);
            Assert.False(c[1]);

            // Regression test for https://github.com/dotnet/runtime/issues/30686.
            ObservableCollection<bool> oc = JsonSerializer.Deserialize<ObservableCollection<bool>>("[true,false]");
            Assert.Equal(2, oc.Count);
            Assert.True(oc[0]);
            Assert.False(oc[1]);

            SimpleKeyedCollection kc = JsonSerializer.Deserialize<SimpleKeyedCollection>("[true]");
            Assert.Equal(1, kc.Count);
            Assert.True(kc[0]);
        }

        [Fact]
        public static void Read_ObjectModelCollection_Throws()
        {
            // No default constructor.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ReadOnlyCollection<bool>>("[true,false]"));
            // No default constructor.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ReadOnlyObservableCollection<bool>>("[true,false]"));
            // No default constructor.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ReadOnlyDictionary<string, bool>>(@"{""true"":false}"));

            // Abstract types can't be instantiated. This means there's no default constructor, so the type is not supported for deserialization.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<KeyedCollection<string, bool>>("[true]"));
        }

        public class SimpleKeyedCollection : KeyedCollection<string, bool>
        {
            protected override string GetKeyForItem(bool item)
            {
                return item.ToString();
            }
        }
    }
}
