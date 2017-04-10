// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class EncryptionPropertyCollectionTest
    {
        [Fact]
        public void Ctor_Default()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            Assert.Equal(0, encPropertyCollection.Count);
            Assert.False(encPropertyCollection.IsFixedSize);
            Assert.False(encPropertyCollection.IsReadOnly);
            Assert.False(encPropertyCollection.IsSynchronized);
            Assert.NotNull(encPropertyCollection.SyncRoot);
        }

        [Fact]
        public void Add_OneEncryptionProperty()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            encPropertyCollection.Add(encProperty);
            Assert.Equal(1, encPropertyCollection.Count);
            Assert.NotNull(encPropertyCollection.Item(0));
            Assert.Equal(encProperty, encPropertyCollection.Item(0));
        }

        [Fact]
        public void Contains_True()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            encPropertyCollection.Add(encProperty);
            Assert.True(encPropertyCollection.Contains(encProperty));
        }

        [Fact]
        public void Contains_False()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            Assert.False(encPropertyCollection.Contains(encProperty));
        }

        [Fact]
        public void IndexOf_Contains()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            encPropertyCollection.Add(encProperty);
            Assert.Equal(0, encPropertyCollection.IndexOf(encProperty));
        }

        [Fact]
        public void IndexOf_DoesNotContain()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            Assert.Equal(-1, encPropertyCollection.IndexOf(encProperty));
        }

        [Fact]
        public void Insert()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            encPropertyCollection.Insert(0, encProperty);
            Assert.Equal(0, encPropertyCollection.IndexOf(encProperty));
        }

        [Fact]
        public void Remove()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            encPropertyCollection.Add(encProperty);
            encPropertyCollection.Remove(encProperty);
            Assert.Equal(-1, encPropertyCollection.IndexOf(encProperty));
        }

        [Fact]
        public void RemoveAt()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            encPropertyCollection.Add(encProperty);
            encPropertyCollection.RemoveAt(0);
            Assert.Equal(-1, encPropertyCollection.IndexOf(encProperty));
        }

        [Fact]
        public void Indexer_Get()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            encPropertyCollection.Add(encProperty);
            Assert.Equal(encProperty, encPropertyCollection[0]);
        }

        [Fact]
        public void Indexer_Set()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty1 = new EncryptionProperty();
            EncryptionProperty encProperty2 = new EncryptionProperty();
            encPropertyCollection.Add(encProperty1);
            encPropertyCollection[0] = encProperty2;
            Assert.Equal(encProperty2, encPropertyCollection.Item(0));
        }

        [Fact]
        public void Clear()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            encPropertyCollection.Add(encProperty);
            encPropertyCollection.Clear();
            Assert.Equal(0, encPropertyCollection.Count);
        }

        [Fact]
        public void CopyTo()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            encPropertyCollection.Add(encProperty);
            EncryptionProperty[] encPropertyArray = new EncryptionProperty[encPropertyCollection.Count];
            encPropertyCollection.CopyTo(encPropertyArray, 0);
            Assert.Equal(encProperty, encPropertyArray[0]);
        }

        [Fact]
        public void Enumerator()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            encPropertyCollection.Add(encProperty);
            foreach (EncryptionProperty prop in encPropertyCollection)
            {
                Assert.Equal(prop, encProperty);
            }
        }
    }
}
