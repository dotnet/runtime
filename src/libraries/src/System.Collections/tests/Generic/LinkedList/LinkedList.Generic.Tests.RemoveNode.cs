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
        public void Remove_LLNode()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            int seed = 21543;
            T[] headItems, tailItems, tempItems;
            LinkedListNode<T> tempNode1, tempNode2, tempNode3;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                headItems[i] = CreateT(seed++);
                tailItems[i] = CreateT(seed++);
            }

            //[] Call Remove with an item that exists in the collection size=1
            linkedList = new LinkedList<T>();
            linkedList.AddLast(headItems[0]);
            tempNode1 = linkedList.First;

            linkedList.Remove(linkedList.First); //Remove when  VS Whidbey: 234648 is resolved

            VerifyRemovedNode(linkedList, new T[0], tempNode1, headItems[0]);
            InitialItems_Tests(linkedList, new T[0]);

            //[] Call Remove with the Head collection size=2
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            tempNode1 = linkedList.First;

            linkedList.Remove(linkedList.First); //Remove when  VS Whidbey: 234648 is resolved

            InitialItems_Tests(linkedList, new T[] { headItems[1] });
            VerifyRemovedNode(tempNode1, headItems[0]);

            //[] Call Remove with the Tail collection size=2
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            tempNode1 = linkedList.Last;

            linkedList.Remove(linkedList.Last); //Remove when  VS Whidbey: 234648 is resolved

            InitialItems_Tests(linkedList, new T[] { headItems[0] });
            VerifyRemovedNode(tempNode1, headItems[1]);

            //[] Call Remove all the items collection size=2
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            tempNode1 = linkedList.First;
            tempNode2 = linkedList.Last;

            linkedList.Remove(linkedList.First); //Remove when  VS Whidbey: 234648 is resolved		
            linkedList.Remove(linkedList.Last); //Remove when  VS Whidbey: 234648 is resolved

            InitialItems_Tests(linkedList, new T[0]);
            VerifyRemovedNode(linkedList, new T[0], tempNode1, headItems[0]);
            VerifyRemovedNode(linkedList, new T[0], tempNode2, headItems[1]);

            //[] Call Remove with the Head collection size=3
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);
            tempNode1 = linkedList.First;

            linkedList.Remove(linkedList.First); //Remove when  VS Whidbey: 234648 is resolved
            InitialItems_Tests(linkedList, new T[] { headItems[1], headItems[2] });
            VerifyRemovedNode(linkedList, new T[] { headItems[1], headItems[2] }, tempNode1, headItems[0]);

            //[] Call Remove with the middle item collection size=3
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);
            tempNode1 = linkedList.First.Next;

            linkedList.Remove(linkedList.First.Next); //Remove when  VS Whidbey: 234648 is resolved		
            InitialItems_Tests(linkedList, new T[] { headItems[0], headItems[2] });
            VerifyRemovedNode(linkedList, new T[] { headItems[0], headItems[2] }, tempNode1, headItems[1]);

            //[] Call Remove with the Tail collection size=3
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);
            tempNode1 = linkedList.Last;

            linkedList.Remove(linkedList.Last); //Remove when  VS Whidbey: 234648 is resolved		
            InitialItems_Tests(linkedList, new T[] { headItems[0], headItems[1] });
            VerifyRemovedNode(linkedList, new T[] { headItems[0], headItems[1] }, tempNode1, headItems[2]);

            //[] Call Remove all the items collection size=3
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);
            tempNode1 = linkedList.First;
            tempNode2 = linkedList.First.Next;
            tempNode3 = linkedList.Last;

            linkedList.Remove(linkedList.First.Next.Next); //Remove when  VS Whidbey: 234648 is resolved		
            linkedList.Remove(linkedList.First.Next); //Remove when  VS Whidbey: 234648 is resolved
            linkedList.Remove(linkedList.First); //Remove when  VS Whidbey: 234648 is resolved		

            InitialItems_Tests(linkedList, new T[0]);
            VerifyRemovedNode(tempNode1, headItems[0]);
            VerifyRemovedNode(tempNode2, headItems[1]);
            VerifyRemovedNode(tempNode3, headItems[2]);

            //[] Call Remove all the items starting with the first collection size=16
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(headItems[i]);

            for (int i = 0; i < arraySize; ++i)
            {
                linkedList.Remove(linkedList.First); //Remove when  VS Whidbey: 234648 is resolved		
                int startIndex = i + 1;
                int length = arraySize - i - 1;
                T[] expectedItems = new T[length];
                Array.Copy(headItems, startIndex, expectedItems, 0, length);
                InitialItems_Tests(linkedList, expectedItems);
            }

            //[] Call Remove all the items starting with the last collection size=16
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(headItems[i]);

            for (int i = arraySize - 1; 0 <= i; --i)
            {
                linkedList.Remove(linkedList.Last); //Remove when  VS Whidbey: 234648 is resolved		
                T[] expectedItems = new T[i];
                Array.Copy(headItems, 0, expectedItems, 0, i);
                InitialItems_Tests(linkedList, expectedItems);
            }

            //[] Remove some items in the middle
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddFirst(headItems[i]);

            linkedList.Remove(linkedList.First.Next.Next); //Remove when  VS Whidbey: 234648 is resolved		
            linkedList.Remove(linkedList.Last.Previous.Previous); //Remove when  VS Whidbey: 234648 is resolved		
            linkedList.Remove(linkedList.First.Next); //Remove when  VS Whidbey: 234648 is resolved		
            linkedList.Remove(linkedList.Last.Previous);
            linkedList.Remove(linkedList.First); //Remove when  VS Whidbey: 234648 is resolved		
            linkedList.Remove(linkedList.Last); //Remove when  VS Whidbey: 234648 is resolved

            //With the above remove we should have removed the first and last 3 items 
            T[] headItemsReverse = new T[arraySize];
            Array.Copy(headItems, headItemsReverse, headItems.Length);
            Array.Reverse(headItemsReverse);

            tempItems = new T[headItemsReverse.Length - 6];
            Array.Copy(headItemsReverse, 3, tempItems, 0, headItemsReverse.Length - 6);

            InitialItems_Tests(linkedList, tempItems);

            //[] Remove an item with a value of default(T)
            linkedList = new LinkedList<T>();

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(headItems[i]);

            linkedList.AddLast(default(T));

            linkedList.Remove(linkedList.Last); //Remove when  VS Whidbey: 234648 is resolved

            InitialItems_Tests(linkedList, headItems);
        }

        [Fact]
        public void Remove_Duplicates_LLNode()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            int seed = 21543;
            T[] items;
            LinkedListNode<T>[] nodes = new LinkedListNode<T>[arraySize * 2];
            LinkedListNode<T> currentNode;
            int index;

            items = new T[arraySize];

            for (int i = 0; i < arraySize; i++)
            {
                items[i] = CreateT(seed++);
            }

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(items[i]);

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(items[i]);

            currentNode = linkedList.First;
            index = 0;

            while (currentNode != null)
            {
                nodes[index] = currentNode;
                currentNode = currentNode.Next;
                ++index;
            }

            linkedList.Remove(linkedList.First.Next.Next); //Remove when  VS Whidbey: 234648 is resolved		
            linkedList.Remove(linkedList.Last.Previous.Previous); //Remove when  VS Whidbey: 234648 is resolved		
            linkedList.Remove(linkedList.First.Next); //Remove when  VS Whidbey: 234648 is resolved		
            linkedList.Remove(linkedList.Last.Previous);
            linkedList.Remove(linkedList.First); //Remove when  VS Whidbey: 234648 is resolved		
            linkedList.Remove(linkedList.Last); //Remove when  VS Whidbey: 234648 is resolved

            //[] Verify that the duplicates were removed from the beginning of the collection
            currentNode = linkedList.First;

            //Verify the duplicates that should have been removed
            for (int i = 3; i < nodes.Length - 3; ++i)
            {
                Assert.NotNull(currentNode); //"Err_48588ahid CurrentNode is null index=" + i
                Assert.Equal(currentNode, nodes[i]); //"Err_5488ahid CurrentNode is not the expected node index=" + i
                Assert.Equal(items[i % items.Length], currentNode.Value); //"Err_16588ajide CurrentNode value index=" + i

                currentNode = currentNode.Next;
            }

            Assert.Null(currentNode); //"Err_30878ajid Expected CurrentNode to be null after moving through entire list"
        }

        [Fact]
        public void Remove_LLNode_Negative()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            LinkedList<T> tempLinkedList = new LinkedList<T>();
            int seed = 21543;
            T[] items;

            //[] Verify Null node
            linkedList = new LinkedList<T>();
            Assert.Throws<ArgumentNullException>(() => linkedList.Remove(null)); //"Err_858ahia Expected null node to throws ArgumentNullException\n"

            InitialItems_Tests(linkedList, new T[0]);

            //[] Verify Node that is a new Node
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++) };
            linkedList.AddLast(items[0]);
            Assert.Throws<InvalidOperationException>(() => linkedList.Remove(new LinkedListNode<T>(CreateT(seed++)))); //"Err_0568ajods Expected Node that is a new Node throws InvalidOperationException\n"

            InitialItems_Tests(linkedList, items);

            //[] Verify Node that already exists in another collection
            linkedList = new LinkedList<T>();
            items = new T[] { CreateT(seed++), CreateT(seed++) };
            linkedList.AddLast(items[0]);
            linkedList.AddLast(items[1]);

            tempLinkedList.Clear();
            tempLinkedList.AddLast(CreateT(seed++));
            tempLinkedList.AddLast(CreateT(seed++));
            Assert.Throws<InvalidOperationException>(() => linkedList.Remove(tempLinkedList.Last)); //"Err_98809ahied Node that already exists in another collection throws InvalidOperationException\n"

            InitialItems_Tests(linkedList, items);
        }
    }
}