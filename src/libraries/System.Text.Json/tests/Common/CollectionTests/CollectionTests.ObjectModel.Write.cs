// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests
    {
        [Fact]
        public async Task Write_ObjectModelCollection()
        {
            Collection<bool> c = new Collection<bool>() { true, false };
            Assert.Equal("[true,false]", await JsonSerializerWrapperForString.SerializeWrapper(c));

            ObservableCollection<bool> oc = new ObservableCollection<bool>() { true, false };
            Assert.Equal("[true,false]", await JsonSerializerWrapperForString.SerializeWrapper(oc));

            SimpleKeyedCollection kc = new SimpleKeyedCollection() { true, false };
            Assert.Equal("[true,false]", await JsonSerializerWrapperForString.SerializeWrapper(kc));
            Assert.Equal("[true,false]", await JsonSerializerWrapperForString.SerializeWrapper<KeyedCollection<string, bool>>(kc));

            ReadOnlyCollection<bool> roc = new ReadOnlyCollection<bool>(new List<bool> { true, false });
            Assert.Equal("[true,false]", await JsonSerializerWrapperForString.SerializeWrapper(roc));

            ReadOnlyObservableCollection<bool> rooc = new ReadOnlyObservableCollection<bool>(oc);
            Assert.Equal("[true,false]", await JsonSerializerWrapperForString.SerializeWrapper(rooc));

            ReadOnlyDictionary<string, bool> rod = new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool> { ["true"] = false });
            Assert.Equal(@"{""true"":false}", await JsonSerializerWrapperForString.SerializeWrapper(rod));
        }
    }
}
