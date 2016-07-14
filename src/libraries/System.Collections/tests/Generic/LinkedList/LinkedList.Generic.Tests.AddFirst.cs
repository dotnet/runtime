// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of the LinkedList class.
    /// </summary>
    public abstract partial class LinkedList_Generic_Tests<T> : ICollection_Generic_Tests<T>
    {
        [Fact]
        public void AddFirst_T_Tests()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int seed = 21543;
            int arraySize = 16;
            T[] tempItems, tempItems2, headItems, headItemsReverse, tailItems, tailItemsReverse;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            headItemsReverse = new T[arraySize];
            tailItemsReverse = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                int index = (arraySize - 1) - i;
                T head = CreateT(seed++);
                T tail = CreateT(seed++);
                headItems[i] = head;
                headItemsReverse[index] = head;
                tailItems[i] = tail;
                tailItemsReverse[index] = tail;
            }

            //[] Verify value is default(T)
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(default(T));
            InitialItems_Tests(linkedList, new T[] { default(T) });

            //[] Call AddHead(T) several times
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddFirst(headItems[i]);

            InitialItems_Tests(linkedList, headItemsReverse);

            //[] Call AddHead(T) several times remove some of the items
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddFirst(headItems[i]);

            linkedList.Remove(headItems[2]);
            linkedList.Remove(headItems[headItems.Length - 3]);
            linkedList.Remove(headItems[1]);
            linkedList.Remove(headItems[headItems.Length - 2]);
            linkedList.RemoveFirst();
            linkedList.RemoveLast();
            //With the above remove we should have removed the first and last 3 items 
            // expected items are headItems in reverse order, or a subset of them.
            tempItems = new T[headItemsReverse.Length - 6];
            Array.Copy(headItemsReverse, 3, tempItems, 0, headItemsReverse.Length - 6);
            InitialItems_Tests(linkedList, tempItems);

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddFirst(tailItems[i]);

            tempItems2 = new T[tempItems.Length + tailItemsReverse.Length];
            Array.Copy(tailItemsReverse, 0, tempItems2, 0, tailItemsReverse.Length);
            Array.Copy(tempItems, 0, tempItems2, tailItemsReverse.Length, tempItems.Length);
            InitialItems_Tests(linkedList, tempItems2);

            //[] Call AddHead(T) several times remove all of the items
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddFirst(headItems[i]);

            for (int i = 0; i < arraySize; ++i)
                linkedList.RemoveFirst();

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddFirst(tailItems[i]);

            InitialItems_Tests(linkedList, tailItemsReverse);

            //[] Call AddHead(T) several times then call Clear
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddFirst(headItems[i]);

            linkedList.Clear();

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddFirst(tailItems[i]);

            InitialItems_Tests(linkedList, tailItemsReverse);

            //[] Mix AddHead and AddTail calls
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
            {
                linkedList.AddFirst(headItems[i]);
                linkedList.AddLast(tailItems[i]);
            }

            tempItems = new T[headItemsReverse.Length + tailItems.Length];
            Array.Copy(headItemsReverse, 0, tempItems, 0, headItemsReverse.Length);
            Array.Copy(tailItems, 0, tempItems, headItemsReverse.Length, tailItems.Length);

            InitialItems_Tests(linkedList, tempItems);
        }

        [Fact]
        public void AddFirst_LinkedListNode()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            int seed = 21543;
            T[] tempItems, tempItems2, headItems, headItemsReverse, tailItems, tailItemsReverse;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            headItemsReverse = new T[arraySize];
            tailItemsReverse = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                int index = (arraySize - 1) - i;
                T head = CreateT(seed++);
                T tail = CreateT(seed++);
                headItems[i] = head;
                headItemsReverse[index] = head;
                tailItems[i] = tail;
                tailItemsReverse[index] = tail;
            }

            //[] Verify value is default(T)
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(new LinkedListNode<T>(default(T)));
            InitialItems_Tests(linkedList, new T[] { default(T) });

            //[] Call AddHead(LinkedListNode<T>) several times
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddFirst(new LinkedListNode<T>(headItems[i]));

            InitialItems_Tests(linkedList, headItemsReverse);

            //[] Call AddHead(LinkedListNode<T>) several times remove some of the items
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddFirst(new LinkedListNode<T>(headItems[i]));

            linkedList.Remove(headItems[2]);
            linkedList.Remove(headItems[headItems.Length - 3]);
            linkedList.Remove(headItems[1]);
            linkedList.Remove(headItems[headItems.Length - 2]);
            linkedList.RemoveFirst();
            linkedList.RemoveLast();
            //With the above remove we should have removed the first and last 3 items 
            tempItems = new T[headItemsReverse.Length - 6];
            Array.Copy(headItemsReverse, 3, tempItems, 0, headItemsReverse.Length - 6);
            InitialItems_Tests(linkedList, tempItems);

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddFirst(new LinkedListNode<T>(tailItems[i]));

            tempItems2 = new T[tailItemsReverse.Length + tempItems.Length];
            Array.Copy(tailItemsReverse, 0, tempItems2, 0, tailItemsReverse.Length);
            Array.Copy(tempItems, 0, tempItems2, tailItemsReverse.Length, tempItems.Length);
            InitialItems_Tests(linkedList, tempItems2);

            //[] Call AddHead(LinkedListNode<T>) several times remove all of the items
            linkedList = new LinkedList<T>();

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddFirst(new LinkedListNode<T>(headItems[i]));

            for (int i = 0; i < arraySize; ++i)
                linkedList.RemoveFirst();

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddFirst(new LinkedListNode<T>(tailItems[i]));

            InitialItems_Tests(linkedList, tailItemsReverse);

            //[] Mix AddHead and AddTail calls
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
            {
                linkedList.AddFirst(new LinkedListNode<T>(headItems[i]));
                linkedList.AddLast(new LinkedListNode<T>(tailItems[i]));
            }

            tempItems = new T[headItemsReverse.Length + tailItems.Length];
            Array.Copy(headItemsReverse, 0, tempItems, 0, headItemsReverse.Length);
            Array.Copy(tailItems, 0, tempItems, headItemsReverse.Length, tailItems.Length);
            InitialItems_Tests(linkedList, tempItems);
        }

        [Fact]
        public void AddFirst_LinkedListNode_Negative()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            LinkedList<T> tempLinkedList = new LinkedList<T>();
            int seed = 21543;
            T[] items;

            //[] Verify Null node
            Assert.Throws<ArgumentNullException>(() => linkedList.AddFirst(null)); //"Err_858ahia Expected null node to throws ArgumentNullException\n"
            InitialItems_Tests(linkedList, new T[0]);

            //[] Verify Node that already exists in this collection that is the Head
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++) };
            linkedList.AddLast(items[0]);
            Assert.Throws<InvalidOperationException>(() => linkedList.AddFirst(linkedList.First)); //"Err_0568ajods Expected Node that already exists in this collection that is the Head throws InvalidOperationException\n"
            InitialItems_Tests(linkedList, items);

            //[] Verify Node that already exists in this collection that is the Tail
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++), CreateT(seed++) };
            linkedList.AddLast(items[0]);
            linkedList.AddLast(items[1]);
            Assert.Throws<InvalidOperationException>(() => linkedList.AddFirst(linkedList.Last)); //"Err_98809ahied Expected Node that already exists in this collection that is the Tail throws InvalidOperationException\n"
            InitialItems_Tests(linkedList, items);

            //[] Verify Node that already exists in another collection
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++), CreateT(seed++) };
            linkedList.AddLast(items[0]);
            linkedList.AddLast(items[1]);

            tempLinkedList.Clear();
            tempLinkedList.AddLast(CreateT(seed++));
            tempLinkedList.AddLast(CreateT(seed++));
            Assert.Throws<InvalidOperationException>(() => linkedList.AddFirst(tempLinkedList.Last)); //"Err_98809ahied Node that already exists in another collection throws InvalidOperationException\n"
            InitialItems_Tests(linkedList, items);
        }
    }
}