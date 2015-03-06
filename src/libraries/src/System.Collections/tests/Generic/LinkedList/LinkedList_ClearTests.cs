// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace LinkedList_LinkedList_ClearTests
{
    public class LinkedList_ClearTests
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
        public static void Clear_Tests()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.ClearTests(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.ClearTests(s_generateInt);
        }
    }

    /// <summary>
    /// Helper class that verifies some properties of the linked list.
    /// </summary>
    internal class LinkedList_T_Tests<T>
    {
        internal void ClearTests(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            T[] headItems, tailItems;
            LinkedListNode<T>[] nodes;

            headItems = new T[16];
            tailItems = new T[16];
            for (int i = 0; i < 16; i++)
            {
                headItems[i] = generateItem();
                tailItems[i] = generateItem();
            }

            //[] Call Clear() several times on an empty collection
            linkedList.Clear();
            linkedList.Clear();
            linkedList.Clear();

            VerifyState(linkedList, new T[0]);


            //[] Call Clear() several times on a collection with one item in it
            linkedList.AddLast(headItems[0]);
            nodes = GetLinkedListNodes(linkedList);
            linkedList.Clear();
            linkedList.Clear();
            linkedList.Clear();

            VerifyState(linkedList, new T[0]);
            VerifyRemovedNodes(linkedList, nodes, new T[] { headItems[0] });

            //[] Call Clear() several times on a collection with two items in it
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            nodes = GetLinkedListNodes(linkedList);
            linkedList.Clear();
            linkedList.Clear();
            linkedList.Clear();

            VerifyState(linkedList, new T[0]);
            VerifyRemovedNodes(linkedList, nodes, new T[] { headItems[0], headItems[1] });

            //[] Call Clear() several times on a collection with 3 items in it
            linkedList.AddLast(headItems[0]);
            linkedList.AddFirst(headItems[1]);
            linkedList.AddLast(headItems[2]);
            nodes = GetLinkedListNodes(linkedList);
            linkedList.Clear();
            linkedList.Clear();
            linkedList.Clear();

            VerifyState(linkedList, new T[0]);
            VerifyRemovedNodes(linkedList, nodes, new T[] { headItems[1], headItems[0], headItems[2] });

            //[] Call Clear() several times on a collection with multiple items in it
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);

            nodes = GetLinkedListNodes(linkedList);

            linkedList.Clear();
            linkedList.Clear();
            linkedList.Clear();

            VerifyState(linkedList, new T[0]);
            VerifyRemovedNodes(linkedList, nodes, headItems);

            //[] Call Add some items then remove some of the items then call Clear";
            linkedList.Clear();
            for (int i = arraySize - 1; -1 < i; --i)
                linkedList.AddFirst(headItems[i]);

            nodes = GetLinkedListNodes(linkedList);

            linkedList.Remove(headItems[2]);
            linkedList.Remove(headItems[headItems.Length - 3]);
            linkedList.Remove(headItems[1]);
            linkedList.Remove(headItems[headItems.Length - 2]);
            linkedList.RemoveFirst();
            linkedList.RemoveLast();
            //With the above remove we should have removed the first and last 3 items 

            linkedList.Clear();
            VerifyState(linkedList, new T[0]);
            VerifyRemovedNodes(linkedList, nodes, headItems);

            //[] Add some more items after clear then call ClearAgain
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(tailItems[i]);

            nodes = GetLinkedListNodes(linkedList);

            linkedList.Clear();

            VerifyState(linkedList, new T[0]);
            VerifyRemovedNodes(linkedList, nodes, tailItems);
        }

        #region Helper Methods

        private void VerifyRemovedNodes(LinkedList<T> linkedList, LinkedListNode<T>[] nodes, T[] expectedValues)
        {
            LinkedListNode<T> tailNode = linkedList.Last;

            for (int i = 0; i < nodes.Length; ++i)
            {
                Assert.Null(nodes[i].List); //"Err_564898ajid Node.LinkedList returned non null"
                Assert.Null(nodes[i].Previous); //"Err_30808wia Node.Previous returned non null"
                Assert.Null(nodes[i].Next); //"Err_78280aoiea Node.Next returned non null"
                Assert.Equal(expectedValues[i], nodes[i].Value); //"Err_98234aued Node.Value"

                linkedList.AddLast(nodes[i]);
                Assert.Equal(linkedList, nodes[i].List); //"Err_038369aihead Node.LinkedList"
                Assert.Equal(tailNode, nodes[i].Previous); //"Err_789108aiea Node.Previous"
                Assert.Null(nodes[i].Next); //"Err_37896riad Node.Next returned non null"
                Assert.Equal(expectedValues[i], nodes[i].Value); //"Err_823902jaied Node.Value"
                tailNode = linkedList.Last;
            }

            VerifyState(linkedList, expectedValues);

            for (int i = 0; i < nodes.Length; ++i)
                linkedList.Remove(nodes[i]);
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

        private LinkedListNode<T>[] GetLinkedListNodes(LinkedList<T> linkedList)
        {
            LinkedListNode<T>[] nodes = new LinkedListNode<T>[linkedList.Count];
            LinkedListNode<T> current = linkedList.First;
            int index = 0;

            while (current != null)
            {
                nodes[index] = current;
                current = current.Next;
                ++index;
            }

            return nodes;
        }

        #endregion
    }
}
