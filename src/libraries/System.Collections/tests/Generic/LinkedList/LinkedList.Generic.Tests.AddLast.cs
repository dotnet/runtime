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
        public void AddLast_T_Tests()
        {
            int arraySize = 16;
            int seed = 21543;
            T[] tempItems, headItems, tailItems;

            headItems = new T[16];
            tailItems = new T[16];
            for (int i = 0; i < 16; i++)
            {
                headItems[i] = CreateT(seed++);
                tailItems[i] = CreateT(seed++);
            }

            //[] Verify value is default(T)
            LinkedList<T> linkedList = new LinkedList<T>();
            linkedList.AddLast(default(T));
            InitialItems_Tests(linkedList, new T[] { default(T) });

            //[] Call AddTail(T) several times
            linkedList.Clear();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(tailItems[i]);

            InitialItems_Tests(linkedList, tailItems);

            //[] Call AddTail(T) several times remove some of the items
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(tailItems[i]);

            linkedList.Remove(tailItems[2]);
            linkedList.Remove(tailItems[tailItems.Length - 3]);
            linkedList.Remove(tailItems[1]);
            linkedList.Remove(tailItems[tailItems.Length - 2]);
            linkedList.RemoveFirst();
            linkedList.RemoveLast();
            //With the above remove we should have removed the first and last 3 items 
            tempItems = new T[tailItems.Length - 6];
            Array.Copy(tailItems, 3, tempItems, 0, tailItems.Length - 6);

            InitialItems_Tests(linkedList, tempItems);

            //[] adding some more items to the tail of the linked list.
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(headItems[i]);

            T[] tempItems2 = new T[tempItems.Length + headItems.Length];
            Array.Copy(tempItems, 0, tempItems2, 0, tempItems.Length);
            Array.Copy(headItems, 0, tempItems2, tempItems.Length, headItems.Length);
            InitialItems_Tests(linkedList, tempItems2);

            //[] Call AddTail(T) several times then call Clear
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(tailItems[i]);

            linkedList.Clear();

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(headItems[i]);

            InitialItems_Tests(linkedList, headItems);

            //[] Mix AddTail and AddTail calls
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
            {
                linkedList.AddFirst(headItems[i]);
                linkedList.AddLast(tailItems[i]);
            }

            tempItems = new T[headItems.Length];
            // adding the headItems in reverse order.
            for (int i = 0; i < headItems.Length; i++)
            {
                int index = (headItems.Length - 1) - i;
                tempItems[i] = headItems[index];
            }

            tempItems2 = new T[tempItems.Length + tailItems.Length];
            Array.Copy(tempItems, 0, tempItems2, 0, tempItems.Length);
            Array.Copy(tailItems, 0, tempItems2, tempItems.Length, tailItems.Length);
            InitialItems_Tests(linkedList, tempItems2);
        }

        [Fact]
        public void AddLast_LinkedListNode()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            int seed = 21543;
            T[] tempItems, headItems, headItemsReverse, tailItems;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            headItemsReverse = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                int index = (arraySize - 1) - i;
                T head = CreateT(seed++);
                T tail = CreateT(seed++);
                headItems[i] = head;
                headItemsReverse[index] = head;
                tailItems[i] = tail;
            }

            //[] Verify value is default(T)
            linkedList = new LinkedList<T>();
            linkedList.AddLast(new LinkedListNode<T>(default(T)));
            InitialItems_Tests(linkedList, new T[] { default(T) });

            //[] Call AddTail(LinkedListNode<T>) several times
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(new LinkedListNode<T>(tailItems[i]));

            InitialItems_Tests(linkedList, tailItems);

            //[] Call AddTail(LinkedListNode<T>) several times remove some of the items
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(new LinkedListNode<T>(tailItems[i]));

            linkedList.Remove(tailItems[2]);
            linkedList.Remove(tailItems[tailItems.Length - 3]);
            linkedList.Remove(tailItems[1]);
            linkedList.Remove(tailItems[tailItems.Length - 2]);
            linkedList.RemoveFirst();
            linkedList.RemoveLast();

            //With the above remove we should have removed the first and last 3 items 
            tempItems = new T[tailItems.Length - 6];
            Array.Copy(tailItems, 3, tempItems, 0, tailItems.Length - 6);
            InitialItems_Tests(linkedList, tempItems);

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(new LinkedListNode<T>(headItems[i]));

            T[] tempItems2 = new T[tempItems.Length + headItems.Length];
            Array.Copy(tempItems, 0, tempItems2, 0, tempItems.Length);
            Array.Copy(headItems, 0, tempItems2, tempItems.Length, headItems.Length);

            InitialItems_Tests(linkedList, tempItems2);

            //[] Call AddTail(T) several times then call Clear
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(new LinkedListNode<T>(tailItems[i]));

            linkedList.Clear();

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(new LinkedListNode<T>(headItems[i]));

            InitialItems_Tests(linkedList, headItems);

            //[] Mix AddTail and AddTail calls
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
        public void AddLast_LinkedListNode_Negative()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            LinkedList<T> tempLinkedList = new LinkedList<T>();
            int seed = 21543;
            T[] items;

            //[] Verify Null node
            Assert.Throws<ArgumentNullException>(() => linkedList.AddLast(null)); //"Err_858ahia Expected null node to throws ArgumentNullException"
            InitialItems_Tests(linkedList, new T[0]);

            //[] Verify Node that already exists in this collection that is the Head
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++) };
            linkedList.AddLast(items[0]);
            Assert.Throws<InvalidOperationException>(() => linkedList.AddLast(linkedList.First)); //"Err_0568ajods Expected Node that already exists in this collection that is the Head throws InvalidOperationException\n"
            InitialItems_Tests(linkedList, items);

            //[] Verify Node that already exists in this collection that is the Tail
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++), CreateT(seed++) };
            linkedList.AddLast(items[0]);
            linkedList.AddLast(items[1]);
            Assert.Throws<InvalidOperationException>(() => linkedList.AddLast(linkedList.Last)); //"Err_98809ahied Expected Node that already exists in this collection that is the Tail throws InvalidOperationException\n"
            InitialItems_Tests(linkedList, items);

            //[] Verify Node that already exists in another collection
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++), CreateT(seed++) };
            linkedList.AddLast(items[0]);
            linkedList.AddLast(items[1]);

            tempLinkedList.Clear();
            tempLinkedList.AddLast(CreateT(seed++));
            tempLinkedList.AddLast(CreateT(seed++));
            Assert.Throws<InvalidOperationException>(() => linkedList.AddLast(tempLinkedList.Last)); //"Err_98809ahied Node that already exists in another collection throws InvalidOperationException\n"
            InitialItems_Tests(linkedList, items);
        }
    }
}