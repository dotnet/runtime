// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests
    {
        [Fact]
        public async Task Read_ObjectModelCollection()
        {
            Collection<bool> c = await Serializer.DeserializeWrapper<Collection<bool>>("[true,false]");
            Assert.Equal(2, c.Count);
            Assert.True(c[0]);
            Assert.False(c[1]);

            // Regression test for https://github.com/dotnet/runtime/issues/30686.
            ObservableCollection<bool> oc = await Serializer.DeserializeWrapper<ObservableCollection<bool>>("[true,false]");
            Assert.Equal(2, oc.Count);
            Assert.True(oc[0]);
            Assert.False(oc[1]);

            SimpleKeyedCollection kc = await Serializer.DeserializeWrapper<SimpleKeyedCollection>("[true]");
            Assert.Equal(1, kc.Count);
            Assert.True(kc[0]);
        }

        [Fact]
        public async Task Read_ObjectModelCollection_Throws()
        {
            // No default constructor.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.DeserializeWrapper<ReadOnlyCollection<bool>>("[true,false]"));
            // No default constructor.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.DeserializeWrapper<ReadOnlyObservableCollection<bool>>("[true,false]"));
            // No default constructor.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.DeserializeWrapper<ReadOnlyDictionary<string, bool>>(@"{""true"":false}"));

            // Abstract types can't be instantiated. This means there's no default constructor, so the type is not supported for deserialization.
            await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.DeserializeWrapper<KeyedCollection<string, bool>>("[true]"));
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
