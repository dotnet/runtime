// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    public sealed class TraceListenerCollectionClassTests
        : ListBaseTests<TraceListenerCollection>
    {
        public override TraceListenerCollection Create()
        {
            return new TraceListenerCollection();
        }

        public TraceListener CreateListener()
        {
            return new TestTraceListener();
        }

        public override object CreateItem()
        {
            return CreateListener();
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override bool IsFixedSize
        {
            get { return false; }
        }

        [Fact]
        public void TraceListenerIndexerTest()
        {
            var list = Create();
            var item = CreateListener();
            list.Add(item);
            Assert.Equal(item, list[0]);
            item = CreateListener();
            list[0] = item;
            Assert.Equal(item, list[0]);
        }

        [Fact]
        public void TraceListenerNameIndexerTest()
        {
            var list = Create();
            var item = CreateListener();
            item.Name = "TestListener";
            list.Add(item);
            Assert.Equal(item, list["TestListener"]);
            Assert.Equal(null, list["NO_EXIST"]);
        }

        [Fact]
        public void AddRangeArrayTest()
        {
            var list = Create();
            Assert.Throws<ArgumentNullException>(() => list.AddRange((TraceListener[]) null));
            var items = 
                new TraceListener[] {
                    CreateListener(),
                    CreateListener(),
                };
            list.AddRange(items);
            Assert.Equal(items[0], list[0]);
            Assert.Equal(items[1], list[1]);
        }

        [Fact]
        public void AddRangeCollectionTest()
        {
            var list = Create();
            Assert.Throws<ArgumentNullException>(() => list.AddRange((TraceListenerCollection)null));
            var items = new TraceListenerCollection();
            var item0 = CreateListener();
            var item1 = CreateListener();
            items.Add(item0); 
            items.Add(item1);            
            list.AddRange(items);
            Assert.Equal(item0, list[0]);
            Assert.Equal(item1, list[1]);
        }
    }

    public abstract class ListBaseTests<T> : CollectionBaseTests<T>
        where T : IList
    {


        public abstract bool IsReadOnly { get; }

        public abstract bool IsFixedSize { get; }


        [Fact]
        public void IndexerTest()
        {
            var list = Create();
            var item = CreateItem();
            list.Add(item);
            Assert.Equal(item, list[0]);

            var item2 = CreateItem();
            list[0] = item2;
            Assert.Equal(item2, list[0]);
        }


        [Fact]
        public void RemoveTest()
        {
            var list = Create();
            var item = CreateItem();
            list.Add(item);
            Assert.True(list.Contains(item));
            list.Remove(item);
            Assert.False(list.Contains(item));
        }


        [Fact]
        public void IsReadOnlyTest()
        {
            var list = Create();
            Assert.Equal(IsReadOnly, list.IsReadOnly);
        }

        [Fact]
        public void IsFixedSizeTest()
        {
            var list = Create();
            Assert.Equal(IsFixedSize, list.IsFixedSize);
        }
    }

    public abstract class CollectionBaseTests<T>
        where T : ICollection
    {

        public abstract T Create();

        public abstract Object CreateItem();


        
    }
}
