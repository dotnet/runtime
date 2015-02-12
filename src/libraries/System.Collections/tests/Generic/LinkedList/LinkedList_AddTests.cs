// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace LinkedList_LinkedList_AddTests
{
    public class LinkedList_AddTests
    {
        private static int s_currentCharAsInt = 32;
        private static readonly Func<string> s_generateString =
            () =>
            {
                char item = (char)s_currentCharAsInt;
                s_currentCharAsInt++;
                return item.ToString();
            };

        private static int s_currentInt = -5;
        private static readonly Func<int> s_generateInt =
            () =>
            {
                int current = s_currentInt;
                s_currentInt++;
                return current;
            };

        [Fact]
        public static void AddLast_Tests()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.AddLast_T_Tests(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.AddLast_T_Tests(s_generateInt);
        }

        [Fact]
        public static void AddLast_LinkedListNode()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.AddLast_LinkedListNode(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.AddLast_LinkedListNode(s_generateInt);
        }

        [Fact]
        public static void AddLast_LinkedListNode_Negative()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.AddLast_LinkedListNode_Negative(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.AddLast_LinkedListNode_Negative(s_generateInt);
        }

        [Fact]
        public static void AddFirst_Tests()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.AddFirst_T_Tests(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.AddFirst_T_Tests(s_generateInt);
        }

        [Fact]
        public static void AddFirst_LinkedListNode()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.AddFirst_LinkedListNode(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.AddFirst_LinkedListNode(s_generateInt);
        }
        [Fact]
        public static void AddFirst_LinkedListNode_Negative()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.AddFirst_LinkedListNode_Negative(s_generateString);
        }
    }

    /// <summary>
    /// Helper class that verifies some properties of the linked list.
    /// </summary>
    internal class LinkedList_T_Tests<T>
    {
        internal void AddLast_T_Tests(Func<T> generateItemFunc)
        {
            int arraySize = 16;
            T[] tempItems, headItems, tailItems;

            headItems = new T[16];
            tailItems = new T[16];
            for (int i = 0; i < 16; i++)
            {
                headItems[i] = generateItemFunc();
                tailItems[i] = generateItemFunc();
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

        internal void AddLast_LinkedListNode(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            T[] tempItems, headItems, headItemsReverse, tailItems;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            headItemsReverse = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                int index = (arraySize - 1) - i;
                T head = generateItem();
                T tail = generateItem();
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

        internal void AddLast_LinkedListNode_Negative(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            LinkedList<T> tempLinkedList = new LinkedList<T>();
            T[] items;

            //[] Verify Null node
            Assert.Throws<ArgumentNullException>(() => linkedList.AddLast(null)); //"Err_858ahia Expected null node to throws ArgumentNullException"
            InitialItems_Tests(linkedList, new T[0]);

            //[] Verify Node that already exists in this collection that is the Head
            linkedList = new LinkedList<T>();
            items = new T[] { generateItem() };
            linkedList.AddLast(items[0]);
            Assert.Throws<InvalidOperationException>(() => linkedList.AddLast(linkedList.First)); //"Err_0568ajods Expected Node that already exists in this collection that is the Head throws InvalidOperationException\n"
            InitialItems_Tests(linkedList, items);

            //[] Verify Node that already exists in this collection that is the Tail
            linkedList = new LinkedList<T>();
            items = new T[] { generateItem(), generateItem() };
            linkedList.AddLast(items[0]);
            linkedList.AddLast(items[1]);
            Assert.Throws<InvalidOperationException>(() => linkedList.AddLast(linkedList.Last)); //"Err_98809ahied Expected Node that already exists in this collection that is the Tail throws InvalidOperationException\n"
            InitialItems_Tests(linkedList, items);

            //[] Verify Node that already exists in another collection
            linkedList = new LinkedList<T>();
            items = new T[] { generateItem(), generateItem() };
            linkedList.AddLast(items[0]);
            linkedList.AddLast(items[1]);

            tempLinkedList.Clear();
            tempLinkedList.AddLast(generateItem());
            tempLinkedList.AddLast(generateItem());
            Assert.Throws<InvalidOperationException>(() => linkedList.AddLast(tempLinkedList.Last)); //"Err_98809ahied Node that already exists in another collection throws InvalidOperationException\n"
            InitialItems_Tests(linkedList, items);
        }

        internal void AddFirst_T_Tests(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            T[] tempItems, tempItems2, headItems, headItemsReverse, tailItems, tailItemsReverse;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            headItemsReverse = new T[arraySize];
            tailItemsReverse = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                int index = (arraySize - 1) - i;
                T head = generateItem();
                T tail = generateItem();
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

        internal void AddFirst_LinkedListNode(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            T[] tempItems, tempItems2, headItems, headItemsReverse, tailItems, tailItemsReverse;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            headItemsReverse = new T[arraySize];
            tailItemsReverse = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                int index = (arraySize - 1) - i;
                T head = generateItem();
                T tail = generateItem();
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

        internal void AddFirst_LinkedListNode_Negative(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            LinkedList<T> tempLinkedList = new LinkedList<T>();
            T[] items;

            //[] Verify Null node
            Assert.Throws<ArgumentNullException>(() => linkedList.AddFirst(null)); //"Err_858ahia Expected null node to throws ArgumentNullException\n"
            InitialItems_Tests(linkedList, new T[0]);

            //[] Verify Node that already exists in this collection that is the Head
            linkedList = new LinkedList<T>();
            items = new T[] { generateItem() };
            linkedList.AddLast(items[0]);
            Assert.Throws<InvalidOperationException>(() => linkedList.AddFirst(linkedList.First)); //"Err_0568ajods Expected Node that already exists in this collection that is the Head throws InvalidOperationException\n"
            InitialItems_Tests(linkedList, items);

            //[] Verify Node that already exists in this collection that is the Tail
            linkedList = new LinkedList<T>();
            items = new T[] { generateItem(), generateItem() };
            linkedList.AddLast(items[0]);
            linkedList.AddLast(items[1]);
            Assert.Throws<InvalidOperationException>(() => linkedList.AddFirst(linkedList.Last)); //"Err_98809ahied Expected Node that already exists in this collection that is the Tail throws InvalidOperationException\n"
            InitialItems_Tests(linkedList, items);

            //[] Verify Node that already exists in another collection
            linkedList = new LinkedList<T>();
            items = new T[] { generateItem(), generateItem() };
            linkedList.AddLast(items[0]);
            linkedList.AddLast(items[1]);

            tempLinkedList.Clear();
            tempLinkedList.AddLast(generateItem());
            tempLinkedList.AddLast(generateItem());
            Assert.Throws<InvalidOperationException>(() => linkedList.AddFirst(tempLinkedList.Last)); //"Err_98809ahied Node that already exists in another collection throws InvalidOperationException\n"
            InitialItems_Tests(linkedList, items);
        }

        #region Helper Methods

        /// <summary>
        /// Tests the items in the list to make sure they are the same.
        /// </summary>
        private void InitialItems_Tests(LinkedList<T> collection, T[] expectedItems)
        {
            VerifyState(collection, expectedItems);
            VerifyGenericEnumerator(collection, expectedItems);
            VerifyEnumerator(collection, expectedItems);
        }

        /// <summary>
        /// Verifies that the tail/head properties are valid and
        /// can iterate through the list (backwards and forwards) to 
        /// verify the contents of the list.
        /// </summary>
        private static void VerifyState(LinkedList<T> linkedList, T[] expectedItems)
        {
            T[] tempArray;
            int index;
            LinkedListNode<T> currentNode, previousNode, nextNode;

            //[] Verify Count
            Assert.Equal(expectedItems.Length, linkedList.Count); //"Err_0821279 List.Count"

            //[] Verify Head/Tail
            if (expectedItems.Length == 0)
            {
                Assert.Null(linkedList.First); //"Err_48928ahid Expected Head to be null\n"
                Assert.Null(linkedList.Last); //"Err_56418ahjidi Expected Tail to be null\n"
            }
            else if (expectedItems.Length == 1)
            {
                VerifyLinkedListNode(linkedList.First, expectedItems[0], linkedList, null, null);
                VerifyLinkedListNode(linkedList.Last, expectedItems[0], linkedList, null, null);
            }
            else
            {
                VerifyLinkedListNode(linkedList.First, expectedItems[0], linkedList, true, false);
                VerifyLinkedListNode(linkedList.Last, expectedItems[expectedItems.Length - 1], linkedList, false, true);
            }

            //[] Moving forward throught he collection starting at head
            currentNode = linkedList.First;
            previousNode = null;
            index = 0;

            while (currentNode != null)
            {
                nextNode = currentNode.Next;

                VerifyLinkedListNode(currentNode, expectedItems[index], linkedList, previousNode, nextNode);

                previousNode = currentNode;
                currentNode = currentNode.Next;

                ++index;
            }

            //[] Moving backword throught he collection starting at Tail
            currentNode = linkedList.Last;
            nextNode = null;
            index = 0;

            while (currentNode != null)
            {
                previousNode = currentNode.Previous;
                VerifyLinkedListNode(currentNode, expectedItems[expectedItems.Length - 1 - index], linkedList, previousNode, nextNode);

                nextNode = currentNode;
                currentNode = currentNode.Previous;

                ++index;
            }

            //[] Verify Contains
            for (int i = 0; i < expectedItems.Length; i++)
            {
                Assert.True(linkedList.Contains(expectedItems[i]),
                    "Err_9872haid Expected Contains with item=" + expectedItems[i] + " to return true");
            }

            //[] Verify CopyTo
            tempArray = new T[expectedItems.Length];
            linkedList.CopyTo(tempArray, 0);

            for (int i = 0; i < expectedItems.Length; i++)
            {
                Assert.Equal(expectedItems[i], tempArray[i]); //"Err_0310auazp After CopyTo index=" + i.ToString()
            }

            //[] Verify Enumerator()
            index = 0;
            foreach (T item in linkedList)
            {
                Assert.Equal(expectedItems[index], item); //"Err_0310auazp Enumerator index=" + index.ToString()
                ++index;
            }
        }

        /// <summary>
        /// Verifies that the contents of a linkedlistnode are correct.
        /// </summary>
        private static void VerifyLinkedListNode(LinkedListNode<T> node, T expectedValue, LinkedList<T> expectedList,
            LinkedListNode<T> expectedPrevious, LinkedListNode<T> expectedNext)
        {
            Assert.Equal(expectedValue, node.Value); //"Err_548ajoid Node Value"
            Assert.Equal(expectedList, node.List); //"Err_0821279 Node List"

            Assert.Equal(expectedPrevious, node.Previous); //"Err_8548ajhiod Previous Node"
            Assert.Equal(expectedNext, node.Next); //"Err_4688anmjod Next Node"
        }

        /// <summary>
        /// verifies that the contents of a linkedlist node are correct.
        /// </summary>
        private static void VerifyLinkedListNode(LinkedListNode<T> node, T expectedValue, LinkedList<T> expectedList,
            bool expectedPreviousNull, bool expectedNextNull)
        {
            Assert.Equal(expectedValue, node.Value); //"Err_548ajoid Expected Node Value"
            Assert.Equal(expectedList, node.List); //"Err_0821279 Expected Node List"

            if (expectedPreviousNull)
                Assert.Null(node.Previous); //"Expected node.Previous to be null."
            else
                Assert.NotNull(node.Previous); //"Expected node.Previous not to be null"

            if (expectedNextNull)
                Assert.Null(node.Next); //"Expected node.Next to be null."
            else
                Assert.NotNull(node.Next); //"Expected node.Next not to be null"
        }

        /// <summary>
        /// Verifies that the generic enumerator retrieves the correct items.
        /// </summary>
        private void VerifyGenericEnumerator(ICollection<T> collection, T[] expectedItems)
        {
            IEnumerator<T> enumerator = collection.GetEnumerator();
            int iterations = 0;
            int expectedCount = expectedItems.Length;

            //[] Verify non deterministic behavior of current every time it is called before a call to MoveNext() has been made			
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    T tempCurrent = enumerator.Current;
                }
                catch (Exception) { }
            }

            // There is a sequential order to the collection, so we're testing for that.
            while ((iterations < expectedCount) && enumerator.MoveNext())
            {
                T currentItem = enumerator.Current;
                T tempItem;

                //[] Verify we have not gotten more items then we expected
                Assert.True(iterations < expectedCount,
                    "Err_9844awpa More items have been returned fromt the enumerator(" + iterations + " items) than are in the expectedElements(" + expectedCount + " items)");

                //[] Verify Current returned the correct value
                Assert.Equal(currentItem, expectedItems[iterations]); //"Err_1432pauy Current returned unexpected value at index: " + iterations

                //[] Verify Current always returns the same value every time it is called
                for (int i = 0; i < 3; i++)
                {
                    tempItem = enumerator.Current;
                    Assert.Equal(currentItem, tempItem); //"Err_8776phaw Current is returning inconsistant results"
                }

                iterations++;
            }

            Assert.Equal(expectedCount, iterations); //"Err_658805eauz Number of items to iterate through"

            for (int i = 0; i < 3; i++)
            {
                Assert.False(enumerator.MoveNext()); //"Err_2929ahiea Expected MoveNext to return false after" + iterations + " iterations"
            }

            //[] Verify non deterministic behavior of current every time it is called after the enumerator is positioned after the last item
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    T tempCurrent = enumerator.Current;
                }
                catch (Exception) { }
            }

            enumerator.Dispose();
        }

        /// <summary>
        /// Verifies that the non-generic enumerator retrieves the correct items.
        /// </summary>
        private void VerifyEnumerator(ICollection<T> collection, T[] expectedItems)
        {
            IEnumerator enumerator = collection.GetEnumerator();
            int iterations = 0;
            int expectedCount = expectedItems.Length;

            //[] Verify non deterministic behavior of current every time it is called before a call to MoveNext() has been made			
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    object tempCurrent = enumerator.Current;
                }
                catch (Exception) { }
            }

            // There is no sequential order to the collection, so we're testing that all the items
            // in the readonlydictionary exist in the array.
            bool[] itemsVisited = new bool[expectedCount];
            bool itemFound;
            while ((iterations < expectedCount) && enumerator.MoveNext())
            {
                object currentItem = enumerator.Current;
                object tempItem;

                //[] Verify we have not gotten more items then we expected                
                Assert.True(iterations < expectedCount,
                    "Err_9844awpa More items have been returned fromt the enumerator(" + iterations + " items) then are in the expectedElements(" + expectedCount + " items)");

                //[] Verify Current returned the correct value
                itemFound = false;

                for (int i = 0; i < itemsVisited.Length; ++i)
                {
                    if (itemsVisited[i])
                        continue;
                    if ((expectedItems[i] == null && currentItem == null)
                        || (expectedItems[i] != null && expectedItems[i].Equals(currentItem)))
                    {
                        itemsVisited[i] = true;
                        itemFound = true;
                        break;
                    }
                }
                Assert.True(itemFound, "Err_1432pauy Current returned unexpected value=" + currentItem);

                //[] Verify Current always returns the same value every time it is called
                for (int i = 0; i < 3; i++)
                {
                    tempItem = enumerator.Current;
                    Assert.Equal(currentItem, tempItem); //"Err_8776phaw Current is returning inconsistant results Current."
                }

                iterations++;
            }

            for (int i = 0; i < expectedCount; ++i)
            {
                Assert.True(itemsVisited[i], "Err_052848ahiedoi Expected Current to return true for item: " + expectedItems[i] + "index: " + i);
            }

            Assert.Equal(expectedCount, iterations); //"Err_658805eauz Number of items to iterate through"

            for (int i = 0; i < 3; i++)
            {
                Assert.False(enumerator.MoveNext()); //"Err_2929ahiea Expected MoveNext to return false after" + iterations + " iterations"
            }

            //[] Verify non deterministic behavior of current every time it is called after the enumerator is positioned after the last item
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    object tempCurrent = enumerator.Current;
                }
                catch (Exception) { }
            }
        }

        #endregion
    }
}
