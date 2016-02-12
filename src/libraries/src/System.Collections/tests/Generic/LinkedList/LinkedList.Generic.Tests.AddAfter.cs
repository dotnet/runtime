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
        public void AddAfter_LLNode()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            int seed = 8293;
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
            linkedList.AddFirst(headItems[0]);
            linkedList.AddAfter(linkedList.First, default(T));
            InitialItems_Tests(linkedList, new T[] { headItems[0], default(T) });

            //[] Node is the Head
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);

            tempItems = new T[headItems.Length];
            Array.Copy(headItems, 0, tempItems, 0, headItems.Length);
            Array.Reverse(tempItems, 1, headItems.Length - 1);

            for (int i = 1; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.First, headItems[i]);

            InitialItems_Tests(linkedList, tempItems);

            //[] Node is the Tail
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            for (int i = 1; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last, headItems[i]);

            InitialItems_Tests(linkedList, headItems);

            //[] Node is after the Head
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);

            tempItems = new T[headItems.Length];
            Array.Copy(headItems, 0, tempItems, 0, headItems.Length);
            Array.Reverse(tempItems, 2, headItems.Length - 2);

            for (int i = 2; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.First.Next, headItems[i]);

            InitialItems_Tests(linkedList, tempItems);

            //[] Node is before the Tail
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);

            tempItems = new T[headItems.Length];
            Array.Copy(headItems, 2, tempItems, 1, headItems.Length - 2);
            tempItems[0] = headItems[0];
            tempItems[tempItems.Length - 1] = headItems[1];

            for (int i = 2; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last.Previous, headItems[i]);

            InitialItems_Tests(linkedList, tempItems);

            //[] Node is somewhere in the middle
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);

            tempItems = new T[headItems.Length];
            Array.Copy(headItems, 3, tempItems, 1, headItems.Length - 3);
            tempItems[0] = headItems[0];
            tempItems[tempItems.Length - 2] = headItems[1];
            tempItems[tempItems.Length - 1] = headItems[2];

            for (int i = 3; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last.Previous.Previous, headItems[i]);

            InitialItems_Tests(linkedList, tempItems);

            //[] Call AddAfter several times remove some of the items
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            for (int i = 1; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last, headItems[i]);

            linkedList.Remove(headItems[2]);
            linkedList.Remove(headItems[headItems.Length - 3]);
            linkedList.Remove(headItems[1]);
            linkedList.Remove(headItems[headItems.Length - 2]);
            linkedList.RemoveFirst();
            linkedList.RemoveLast();
            //With the above remove we should have removed the first and last 3 items 
            tempItems = new T[headItems.Length - 6];
            Array.Copy(headItems, 3, tempItems, 0, headItems.Length - 6);

            InitialItems_Tests(linkedList, tempItems);

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last, tailItems[i]);

            T[] tempItems2 = new T[tempItems.Length + tailItems.Length];
            Array.Copy(tempItems, 0, tempItems2, 0, tempItems.Length);
            Array.Copy(tailItems, 0, tempItems2, tempItems.Length, tailItems.Length);

            InitialItems_Tests(linkedList, tempItems2);

            //[] Call AddAfter several times remove all of the items
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            for (int i = 1; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last, headItems[i]);

            for (int i = 0; i < arraySize; ++i)
                linkedList.RemoveFirst();

            linkedList.AddFirst(tailItems[0]);
            for (int i = 1; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last, tailItems[i]);

            InitialItems_Tests(linkedList, tailItems);

            //[] Call AddAfter several times then call Clear
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            for (int i = 1; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last, headItems[i]);

            linkedList.Clear();

            linkedList.AddFirst(tailItems[0]);
            for (int i = 1; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last, tailItems[i]);

            InitialItems_Tests(linkedList, tailItems);

            //[] Mix AddBefore and AddAfter calls
            linkedList = new LinkedList<T>();
            linkedList.AddLast(headItems[0]);
            linkedList.AddLast(tailItems[0]);
            for (int i = 1; i < arraySize; ++i)
            {
                linkedList.AddBefore(linkedList.First, headItems[i]);
                linkedList.AddAfter(linkedList.Last, tailItems[i]);
            }

            tempItems = new T[headItemsReverse.Length + tailItems.Length];
            Array.Copy(headItemsReverse, 0, tempItems, 0, headItemsReverse.Length);
            Array.Copy(tailItems, 0, tempItems, headItemsReverse.Length, tailItems.Length);
            InitialItems_Tests(linkedList, tempItems);
        }

        [Fact]
        public void AddAfter_LLNode_Negative()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            LinkedList<T> tempLinkedList = new LinkedList<T>();
            int seed = 8293;
            T[] items;

            //[] Verify Null node
            linkedList = new LinkedList<T>();
            Assert.Throws<ArgumentNullException>(() => linkedList.AddAfter(null, CreateT(seed++))); //"Err_858ahia Expected null node to throws ArgumentNullException\n"

            InitialItems_Tests(linkedList, new T[0]);
            //[] Verify Node that is a new Node
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++) };
            linkedList.AddLast(items[0]);
            Assert.Throws<InvalidOperationException>(() => linkedList.AddAfter(new LinkedListNode<T>(CreateT(seed++)), CreateT(seed++))); //"Err_0568ajods Expected Node that is a new Node throws InvalidOperationException\n"

            InitialItems_Tests(linkedList, items);

            //[] Verify Node that already exists in another collection
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++), CreateT(seed++) };
            linkedList.AddLast(items[0]);
            linkedList.AddLast(items[1]);

            tempLinkedList.Clear();
            tempLinkedList.AddLast(CreateT(seed++));
            tempLinkedList.AddLast(CreateT(seed++));
            Assert.Throws<InvalidOperationException>(() => linkedList.AddAfter(tempLinkedList.Last, CreateT(seed++))); //"Err_98809ahied Node that already exists in another collection throws InvalidOperationException\n"

            InitialItems_Tests(linkedList, items);
        }

        [Fact]
        public void AddAfter_LLNode_LLNode()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int seed = 8293;
            int arraySize = 16;
            T[] tempItems, headItems, headItemsReverse, tailItems, tailItemsReverse;

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
            linkedList.AddFirst(headItems[0]);
            linkedList.AddAfter(linkedList.First, new LinkedListNode<T>(default(T)));
            InitialItems_Tests(linkedList, new T[] { headItems[0], default(T) });

            //[] Node is the Head
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);

            tempItems = new T[headItems.Length];
            Array.Copy(headItems, 0, tempItems, 0, headItems.Length);
            Array.Reverse(tempItems, 1, headItems.Length - 1);

            for (int i = 1; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.First, new LinkedListNode<T>(headItems[i]));

            InitialItems_Tests(linkedList, tempItems);

            //[] Node is the Tail
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            for (int i = 1; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last, new LinkedListNode<T>(headItems[i]));

            InitialItems_Tests(linkedList, headItems);

            //[] Node is after the Head
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            tempItems = new T[headItems.Length];
            Array.Copy(headItems, 0, tempItems, 0, headItems.Length);
            Array.Reverse(tempItems, 2, headItems.Length - 2);

            for (int i = 2; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.First.Next, new LinkedListNode<T>(headItems[i]));

            InitialItems_Tests(linkedList, tempItems);

            //[] Node is before the Tail
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);

            tempItems = new T[headItems.Length];
            Array.Copy(headItems, 2, tempItems, 1, headItems.Length - 2);
            tempItems[0] = headItems[0];
            tempItems[tempItems.Length - 1] = headItems[1];

            for (int i = 2; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last.Previous, new LinkedListNode<T>(headItems[i]));

            InitialItems_Tests(linkedList, tempItems);

            //[] Node is somewhere in the middle
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);

            tempItems = new T[headItems.Length];
            Array.Copy(headItems, 3, tempItems, 1, headItems.Length - 3);
            tempItems[0] = headItems[0];
            tempItems[tempItems.Length - 2] = headItems[1];
            tempItems[tempItems.Length - 1] = headItems[2];

            for (int i = 3; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last.Previous.Previous, new LinkedListNode<T>(headItems[i]));

            InitialItems_Tests(linkedList, tempItems);


            //[] Call AddAfter several times remove some of the items
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            for (int i = 1; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last, new LinkedListNode<T>(headItems[i]));

            linkedList.Remove(headItems[2]);
            linkedList.Remove(headItems[headItems.Length - 3]);
            linkedList.Remove(headItems[1]);
            linkedList.Remove(headItems[headItems.Length - 2]);
            linkedList.RemoveFirst();
            linkedList.RemoveLast();
            //With the above remove we should have removed the first and last 3 items 
            tempItems = new T[headItems.Length - 6];
            Array.Copy(headItems, 3, tempItems, 0, headItems.Length - 6);

            InitialItems_Tests(linkedList, tempItems);

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last, new LinkedListNode<T>(tailItems[i]));

            T[] tempItems2 = new T[tempItems.Length + tailItems.Length];
            Array.Copy(tempItems, 0, tempItems2, 0, tempItems.Length);
            Array.Copy(tailItems, 0, tempItems2, tempItems.Length, tailItems.Length);

            InitialItems_Tests(linkedList, tempItems2);

            //[] Call AddAfter several times remove all of the items
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            for (int i = 1; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last, new LinkedListNode<T>(headItems[i]));

            for (int i = 0; i < arraySize; ++i)
                linkedList.RemoveFirst();

            linkedList.AddFirst(tailItems[0]);
            for (int i = 1; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last, new LinkedListNode<T>(tailItems[i]));

            InitialItems_Tests(linkedList, tailItems);

            //[] Call AddAfter several times then call Clear
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            for (int i = 1; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last, new LinkedListNode<T>(headItems[i]));

            linkedList.Clear();

            linkedList.AddFirst(tailItems[0]);
            for (int i = 1; i < arraySize; ++i)
                linkedList.AddAfter(linkedList.Last, new LinkedListNode<T>(tailItems[i]));

            InitialItems_Tests(linkedList, tailItems);

            //[] Mix AddBefore and AddAfter calls
            linkedList = new LinkedList<T>();
            linkedList.AddLast(headItems[0]);
            linkedList.AddLast(tailItems[0]);
            for (int i = 1; i < arraySize; ++i)
            {
                if (0 == (i & 1))
                {
                    linkedList.AddBefore(linkedList.First, headItems[i]);
                    linkedList.AddAfter(linkedList.Last, tailItems[i]);
                }
                else
                {
                    linkedList.AddBefore(linkedList.First, new LinkedListNode<T>(headItems[i]));
                    linkedList.AddAfter(linkedList.Last, new LinkedListNode<T>(tailItems[i]));
                }
            }

            tempItems = new T[headItemsReverse.Length + tailItems.Length];
            Array.Copy(headItemsReverse, 0, tempItems, 0, headItemsReverse.Length);
            Array.Copy(tailItems, 0, tempItems, headItemsReverse.Length, tailItems.Length);
            InitialItems_Tests(linkedList, tempItems);
        }

        [Fact]
        public void AddAfter_LLNode_LLNode_Negative()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            LinkedList<T> tempLinkedList = new LinkedList<T>();
            int seed = 8293;
            T[] items;

            //[] Verify Null node
            linkedList = new LinkedList<T>();
            Assert.Throws<ArgumentNullException>(() => linkedList.AddAfter(null, new LinkedListNode<T>(CreateT(seed++)))); //"Err_858ahia Expected null node to throws ArgumentNullException\n"
            InitialItems_Tests(linkedList, new T[0]);

            //[] Verify Node that is a new Node
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++) };
            linkedList.AddLast(items[0]);
            Assert.Throws<InvalidOperationException>(() => linkedList.AddAfter(new LinkedListNode<T>(CreateT(seed++)), new LinkedListNode<T>(CreateT(seed++)))); //"Err_0568ajods Expected Node that is a new Node throws InvalidOperationException\n"

            InitialItems_Tests(linkedList, items);

            //[] Verify Node that already exists in another collection
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++), CreateT(seed++) };
            linkedList.AddLast(items[0]);
            linkedList.AddLast(items[1]);

            tempLinkedList.Clear();
            tempLinkedList.AddLast(CreateT(seed++));
            tempLinkedList.AddLast(CreateT(seed++));
            Assert.Throws<InvalidOperationException>(() => linkedList.AddAfter(tempLinkedList.Last, new LinkedListNode<T>(CreateT(seed++)))); //"Err_98809ahied Node that already exists in another collection throws InvalidOperationException\n"

            InitialItems_Tests(linkedList, items);

            // negative tests on NewNode

            //[] Verify Null newNode
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++) };
            linkedList.AddLast(items[0]);
            Assert.Throws<ArgumentNullException>(() => linkedList.AddAfter(linkedList.First, null)); //"Err_0808ajeoia Expected null newNode to throws ArgumentNullException\n"

            InitialItems_Tests(linkedList, items);

            //[] Verify newNode that already exists in this collection
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++), CreateT(seed++) };
            linkedList.AddLast(items[0]);
            linkedList.AddLast(items[1]);
            Assert.Throws<InvalidOperationException>(() => linkedList.AddAfter(linkedList.First, linkedList.Last)); //"Err_58808adjioe Verify newNode that already exists in this collection throws InvalidOperationException\n"

            InitialItems_Tests(linkedList, items);

            //[] Verify newNode that already exists in another collection
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++), CreateT(seed++) };
            linkedList.AddLast(items[0]);
            linkedList.AddLast(items[1]);

            tempLinkedList.Clear();
            tempLinkedList.AddLast(CreateT(seed++));
            tempLinkedList.AddLast(CreateT(seed++));
            Assert.Throws<InvalidOperationException>(() => linkedList.AddAfter(linkedList.First, tempLinkedList.Last)); //"Err_54808ajied newNode that already exists in another collection throws InvalidOperationException\n"

            InitialItems_Tests(linkedList, items);
        }
    }
}