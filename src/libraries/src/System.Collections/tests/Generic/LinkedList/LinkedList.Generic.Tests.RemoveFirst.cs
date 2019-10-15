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
        public void RemoveFirst_Tests()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            int seed = 21543;
            T[] headItems, tailItems;
            LinkedListNode<T> tempNode1, tempNode2, tempNode3;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                headItems[i] = CreateT(seed++);
                tailItems[i] = CreateT(seed++);
            }

            //[] Call RemoveHead on a collection with one item in it
            linkedList = new LinkedList<T>();
            linkedList.AddLast(headItems[0]);
            tempNode1 = linkedList.First;

            linkedList.RemoveFirst();
            InitialItems_Tests(linkedList, new T[0]);
            VerifyRemovedNode(tempNode1, headItems[0]);

            //[] Call RemoveHead on a collection with two items in it
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            tempNode1 = linkedList.First;
            tempNode2 = linkedList.Last;

            linkedList.RemoveFirst();
            InitialItems_Tests(linkedList, new T[] { headItems[1] });

            linkedList.RemoveFirst();
            InitialItems_Tests(linkedList, new T[0]);

            VerifyRemovedNode(linkedList, tempNode1, headItems[0]);
            VerifyRemovedNode(linkedList, tempNode2, headItems[1]);

            //[] Call RemoveHead on a collection with three items in it
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);
            tempNode1 = linkedList.First;
            tempNode2 = linkedList.First.Next;
            tempNode3 = linkedList.Last;

            linkedList.RemoveFirst();
            InitialItems_Tests(linkedList, new T[] { headItems[1], headItems[2] });

            linkedList.RemoveFirst();
            InitialItems_Tests(linkedList, new T[] { headItems[2] });

            linkedList.RemoveFirst();
            InitialItems_Tests(linkedList, new T[0]);

            VerifyRemovedNode(tempNode1, headItems[0]);
            VerifyRemovedNode(tempNode2, headItems[1]);
            VerifyRemovedNode(tempNode3, headItems[2]);

            //[] Call RemoveHead on a collection with 16 items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(headItems[i]);

            for (int i = 0; i < arraySize; ++i)
            {
                linkedList.RemoveFirst();
                int startIndex = i + 1;
                int length = arraySize - i - 1;
                T[] expectedItems = new T[length];
                Array.Copy(headItems, startIndex, expectedItems, 0, length);
                InitialItems_Tests(linkedList, expectedItems);
            }

            //[] Mix RemoveHead and RemoveTail call
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(headItems[i]);

            for (int i = 0; i < arraySize; ++i)
            {
                if ((i & 1) == 0)
                    linkedList.RemoveFirst();
                else
                    linkedList.RemoveLast();

                int startIndex = (i / 2) + 1;
                int length = arraySize - i - 1;
                T[] expectedItems = new T[length];
                Array.Copy(headItems, startIndex, expectedItems, 0, length);
                InitialItems_Tests(linkedList, expectedItems);
            }
        }

        [Fact]
        public void RemoveFirst_Tests_Negative()
        {
            //[] Call RemoveHead an empty collection
            LinkedList<T> linkedList = new LinkedList<T>();
            Assert.Throws<InvalidOperationException>(() => linkedList.RemoveFirst()); //"Expected invalidoperation exception removing from empty list."
            InitialItems_Tests(linkedList, new T[0]);
        }
    }
}