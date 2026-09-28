// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class ReferenceListTests
    {
        [Fact]
        public void Constructor_CreatesEmptyList()
        {
            ReferenceList refList = new ReferenceList();
            Assert.Equal(0, refList.Count);
        }

        [Fact]
        public void Add_DataReference()
        {
            ReferenceList refList = new ReferenceList();
            DataReference dataRef = new DataReference("#data1");
            refList.Add(dataRef);
            Assert.Equal(1, refList.Count);
        }

        [Fact]
        public void Add_KeyReference()
        {
            ReferenceList refList = new ReferenceList();
            KeyReference keyRef = new KeyReference("#key1");
            refList.Add(keyRef);
            Assert.Equal(1, refList.Count);
        }

        [Fact]
        public void GetEnumerator_ReturnsValidEnumerator()
        {
            ReferenceList refList = new ReferenceList();
            refList.Add(new DataReference("#data1"));
            refList.Add(new KeyReference("#key1"));
            
            int count = 0;
            foreach (var item in refList)
            {
                count++;
            }
            Assert.Equal(2, count);
        }

        [Fact]
        public void Item_ByIndex()
        {
            ReferenceList refList = new ReferenceList();
            DataReference dataRef = new DataReference("#data1");
            refList.Add(dataRef);
            
            object item = refList.Item(0);
            Assert.NotNull(item);
        }

        [Fact]
        public void Item_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            ReferenceList refList = new ReferenceList();
            Assert.Throws<ArgumentOutOfRangeException>(() => refList.Item(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => refList.Item(0));
        }
    }
}
