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
            EncryptionProperty encProperty1 = new EncryptionProperty();
            EncryptionProperty encProperty2 = new EncryptionProperty();
            encPropertyCollection.Add(encProperty1);
            encPropertyCollection.Add(encProperty2);
            Assert.Equal(0, encPropertyCollection.IndexOf(encProperty1));
            Assert.Equal(1, encPropertyCollection.IndexOf(encProperty2));
        }

        [Fact]
        public void IndexOf_DoesNotContain()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            Assert.Equal(-1, encPropertyCollection.IndexOf(encProperty));
        }

        [Fact]
        public void Insert_EmptyCollection()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            encPropertyCollection.Insert(0, encProperty);
            Assert.Equal(0, encPropertyCollection.IndexOf(encProperty));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void InsertAt(int idx)
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            for (int i = 0; i < 2; i++)
            {
                encPropertyCollection.Add(new EncryptionProperty());
            }
            EncryptionProperty encProperty3 = new EncryptionProperty();
            encPropertyCollection.Insert(idx, encProperty3);
            Assert.Equal(idx, encPropertyCollection.IndexOf(encProperty3));
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(5, 2)]
        public void Remove(int collectionSize, int removeIdx)
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty toRemove = null;

            for (int i = 0; i < collectionSize; i++)
            {
                EncryptionProperty property = new EncryptionProperty();
                encPropertyCollection.Add(property);

                if (i == removeIdx)
                {
                    toRemove = property;
                }
            }

            encPropertyCollection.Remove(toRemove);
            Assert.Equal(-1, encPropertyCollection.IndexOf(toRemove));
            Assert.Equal(collectionSize - 1, encPropertyCollection.Count);
        }

        [Fact]
        public void Remove_NotExisting()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            for (int i = 0; i < 2; i++)
            {
                encPropertyCollection.Add(new EncryptionProperty());
            }
            EncryptionProperty encProperty3 = new EncryptionProperty();
            encPropertyCollection.Remove(encProperty3);
            Assert.Equal(2, encPropertyCollection.Count);
        }

        [Fact]
        public void Remove_MultipleOccurences()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            for (int i = 0; i < 2; i++)
            {
                encPropertyCollection.Add(new EncryptionProperty());
            }
            EncryptionProperty multiple = new EncryptionProperty();
            for (int i = 0; i < 2; i++)
            {
                encPropertyCollection.Add(multiple);
            }
            encPropertyCollection.Remove(multiple);
            // Only the first occurence will be removed.
            Assert.Equal(3, encPropertyCollection.Count);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(5, 2)]
        public void RemoveAt(int collectionSize, int removeIdx)
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty toRemove = null;

            for (int i = 0; i < collectionSize; i++)
            {
                EncryptionProperty property = new EncryptionProperty();
                encPropertyCollection.Add(property);

                if (i == removeIdx)
                {
                    toRemove = property;
                }
            }

            encPropertyCollection.RemoveAt(removeIdx);
            Assert.Equal(-1, encPropertyCollection.IndexOf(toRemove));
            Assert.Equal(collectionSize - 1, encPropertyCollection.Count);
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
        public void CopyTo_ArrayNull()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            EncryptionProperty encProperty = new EncryptionProperty();
            encPropertyCollection.Add(encProperty);
            Assert.Throws<ArgumentNullException>(() => encPropertyCollection.CopyTo(null, 0));
        }

        [Fact]
        public void CopyTo_ArrayTooSmall()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            for (int i = 0; i < 2; i++)
            {
                encPropertyCollection.Add(new EncryptionProperty());
            }
            EncryptionProperty[] encPropertyArray = new EncryptionProperty[1];
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => encPropertyCollection.CopyTo(encPropertyArray, 0));
        }

        [Fact]
        public void CopyTo_IndexOutOfRange()
        {
            EncryptionPropertyCollection encPropertyCollection = new EncryptionPropertyCollection();
            for (int i = 0; i < 2; i++)
            {
                encPropertyCollection.Add(new EncryptionProperty());
            }
            EncryptionProperty[] encPropertyArray = new EncryptionProperty[2];
            Assert.Throws<ArgumentOutOfRangeException>(() => encPropertyCollection.CopyTo(encPropertyArray, -1));
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
