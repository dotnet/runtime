// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CollectionTests
    {
        [Fact]
        public static void Write_ObjectModelCollection()
        {
            Collection<bool> c = new Collection<bool>() { true, false };
            Assert.Equal("[true,false]", JsonSerializer.Serialize(c));

            ObservableCollection<bool> oc = new ObservableCollection<bool>() { true, false };
            Assert.Equal("[true,false]", JsonSerializer.Serialize(oc));

            SimpleKeyedCollection kc = new SimpleKeyedCollection() { true, false };
            Assert.Equal("[true,false]", JsonSerializer.Serialize(kc));
            Assert.Equal("[true,false]", JsonSerializer.Serialize<KeyedCollection<string, bool>>(kc));

            ReadOnlyCollection<bool> roc = new ReadOnlyCollection<bool>(new List<bool> { true, false });
            Assert.Equal("[true,false]", JsonSerializer.Serialize(roc));

            ReadOnlyObservableCollection<bool> rooc = new ReadOnlyObservableCollection<bool>(oc);
            Assert.Equal("[true,false]", JsonSerializer.Serialize(rooc));

            ReadOnlyDictionary<string, bool> rod = new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool> { ["true"] = false });
            Assert.Equal(@"{""true"":false}", JsonSerializer.Serialize(rod));
        }
    }
}
