// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace LinkedList_LinkedList_FindContainsTests
{
    public class LinkedList_FindContainsTests
    {
        private static int s_currentCharAsInt = 32;
        private static readonly Func<string> s_generateString =
            () =>
            {
                char item = (char)s_currentCharAsInt;
                s_currentCharAsInt++;
                return item.ToString();
            };

        private static int s_currentInt = 1;
        private static readonly Func<int> s_generateInt =
            () =>
            {
                int current = s_currentInt;
                s_currentInt++;
                return current;
            };

        /// <summary>
        /// Tests the Find method using a value type and a reference type.
        /// </summary>
        [Fact]
        public static void Find_T()
        {
            //LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            //helper.Find_T(m_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.Find_T(s_generateInt);
        }

        /// <summary>
        /// Tests the FindLast method using a value type and a reference type.
        /// </summary>
        [Fact]
        public static void FindLast_T()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.FindLast_T(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.FindLast_T(s_generateInt);
        }

        /// <summary>
        /// Tests the Contains method using a value type and a reference type.
        /// </summary>
        [Fact]
        public static void ContainsTests()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.Contains_Tests(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.Contains_Tests(s_generateInt);
        }
    }

    /// <summary>
    /// Helper class that verifies some properties of the linked list.
    /// </summary>
    internal class LinkedList_T_Tests<T>
    {
        /// <summary>
        /// Performs the tests for Find.
        /// </summary>
        internal void Find_T(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            T[] headItems, tailItems;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                headItems[i] = generateItem();
                tailItems[i] = generateItem();
            }

            //[] Call Find an empty collection
            linkedList = new LinkedList<T>();
            Assert.Null(linkedList.Find(headItems[0])); //"Err_2899hjaied Expected Find to return false with a non null item on a empty collection"
            Assert.Null(linkedList.Find(default(T))); //"Err_5808ajiea Expected Find to return false with a null item on a empty collection"

            //[] Call Find on a collection with one item in it
            linkedList = new LinkedList<T>();
            linkedList.AddLast(headItems[0]);
            Assert.Null(linkedList.Find(headItems[1])); //"Err_2899hjaied Expected Find to return false with a non null item on a empty collection size=1"
            Assert.Null(linkedList.Find(default(T))); //"Err_5808ajiea Expected Find to return false with a null item on a empty collection size=1"
            VerifyFind(linkedList, new T[] { headItems[0] });

            //[] Call Find on a collection with two items in it
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            Assert.Null(linkedList.Find(headItems[2])); //"Err_5680ajoed Expected Find to return false with an non null item not in the collection size=2"
            Assert.Null(linkedList.Find(default(T))); //"Err_858196aieh Expected Find to return false with an null item not in the collection size=2"
            VerifyFind(linkedList, new T[] { headItems[0], headItems[1] });

            //[] Call Find on a collection with three items in it
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);
            Assert.Null(linkedList.Find(headItems[3])); //"Err_50878adie Expected Find to return false with an non null item not in the collection size=3"
            Assert.Null(linkedList.Find(default(T))); //"Err_3969887wiqpi Expected Find to return false with an null item not in the collection size=3"
            VerifyFind(linkedList, new T[] { headItems[0], headItems[1], headItems[2] });

            //[] Call Find on a collection with mulitple items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);

            Assert.Null(linkedList.Find(tailItems[0])); //"Err_5808ajdoi Expected Find to return false with an non null item not in the collection size=16"
            Assert.Null(linkedList.Find(default(T))); //"Err_5588aied Expected Find to return false with an null item not in the collection size=16"
            VerifyFind(linkedList, headItems);

            //[] Call Find on a collection with duplicate items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);

            Assert.Null(linkedList.Find(tailItems[0])); //"Err_8548ajia Expected Find to return false with an non null item not in the collection size=16"
            Assert.Null(linkedList.Find(default(T))); //"Err_3108qoa Expected Find to return false with an null item not in the collection size=16"
            T[] tempItems = new T[headItems.Length + headItems.Length];
            Array.Copy(headItems, 0, tempItems, 0, headItems.Length);
            Array.Copy(headItems, 0, tempItems, headItems.Length, headItems.Length);
            VerifyFindDuplicates(linkedList, tempItems);


            //[] Call Find with default(T) at the beginning
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddFirst(default(T));

            Assert.Null(linkedList.Find(tailItems[0])); //"Err_8548ajia Expected Find to return false with an non null item not in the collection default(T) at the beginning"

            tempItems = new T[headItems.Length + 1];
            tempItems[0] = default(T);
            Array.Copy(headItems, 0, tempItems, 1, headItems.Length);

            VerifyFind(linkedList, tempItems);

            //[] Call Find with default(T) in the middle
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddLast(default(T));
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(tailItems[i]);

            Assert.Null(linkedList.Find(generateItem())); //"Err_78585ajhed Expected Find to return false with an non null item not in the collection default(T) in the middle"

            // prepending tempitems2 to tailitems into tempitems
            tempItems = new T[tailItems.Length + 1];
            tempItems[0] = default(T);
            Array.Copy(tailItems, 0, tempItems, 1, tailItems.Length);

            T[] tempItems2 = new T[headItems.Length + tempItems.Length];
            Array.Copy(headItems, 0, tempItems2, 0, headItems.Length);
            Array.Copy(tempItems, 0, tempItems2, headItems.Length, tempItems.Length);

            VerifyFind(linkedList, tempItems2);

            //[] Call Find on a collection with duplicate items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddLast(default(T));

            Assert.Null(linkedList.Find(tailItems[0])); //"Err_208089ajdi Expected Find to return false with an non null item not in the collection default(T) at the end"
            tempItems = new T[headItems.Length + 1];
            tempItems[headItems.Length] = default(T);
            Array.Copy(headItems, 0, tempItems, 0, headItems.Length);
            VerifyFind(linkedList, tempItems);
        }

        /// <summary>
        /// Tests FindLast.
        /// </summary>
        internal void FindLast_T(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            T[] headItems, tailItems, prependDefaultHeadItems, prependDefaultTailItems;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                headItems[i] = generateItem();
                tailItems[i] = generateItem();
            }
            prependDefaultHeadItems = new T[headItems.Length + 1];
            prependDefaultHeadItems[0] = default(T);
            Array.Copy(headItems, 0, prependDefaultHeadItems, 1, headItems.Length);

            prependDefaultTailItems = new T[tailItems.Length + 1];
            prependDefaultTailItems[0] = default(T);
            Array.Copy(tailItems, 0, prependDefaultTailItems, 1, tailItems.Length);

            //[] Call FindLast an empty collection
            linkedList = new LinkedList<T>();
            Assert.Null(linkedList.FindLast(headItems[0])); //"Err_2899hjaied Expected FindLast to return false with a non null item on a empty collection"
            Assert.Null(linkedList.FindLast(default(T))); //"Err_5808ajiea Expected FindLast to return false with a null item on a empty collection"

            //[] Call FindLast on a collection with one item in it
            linkedList = new LinkedList<T>();
            linkedList.AddLast(headItems[0]);
            Assert.Null(linkedList.FindLast(headItems[1])); //"Err_2899hjaied Expected FindLast to return false with a non null item on a empty collection size=1"
            Assert.Null(linkedList.FindLast(default(T))); //"Err_5808ajiea Expected FindLast to return false with a null item on a empty collection size=1"
            VerifyFindLast(linkedList, new T[] { headItems[0] });

            //[] Call FindLast on a collection with two items in it
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            Assert.Null(linkedList.FindLast(headItems[2])); //"Err_5680ajoed Expected FindLast to return false with an non null item not in the collection size=2"
            Assert.Null(linkedList.FindLast(default(T))); //"Err_858196aieh Expected FindLast to return false with an null item not in the collection size=2"
            VerifyFindLast(linkedList, new T[] { headItems[0], headItems[1] });

            //[] Call FindLast on a collection with three items in it
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);
            Assert.Null(linkedList.FindLast(headItems[3])); //"Err_50878adie Expected FindLast to return false with an non null item not in the collection size=3"
            Assert.Null(linkedList.FindLast(default(T))); //"Err_3969887wiqpi Expected FindLast to return false with an null item not in the collection size=3"
            VerifyFindLast(linkedList, new T[] { headItems[0], headItems[1], headItems[2] });

            //[] Call FindLast on a collection with mulitple items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
            {
                linkedList.AddLast(headItems[i]);
            }

            Assert.Null(linkedList.FindLast(tailItems[0])); //"Err_5808ajdoi Expected FindLast to return false with an non null item not in the collection size=16"
            Assert.Null(linkedList.FindLast(default(T))); //"Err_5588aied Expected FindLast to return false with an null item not in the collection size=16"
            VerifyFindLast(linkedList, headItems);

            //[] Call FindLast on a collection with duplicate items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);

            Assert.Null(linkedList.FindLast(tailItems[0])); //"Err_8548ajia Expected FindLast to return false with an non null item not in the collection size=16"
            Assert.Null(linkedList.FindLast(default(T))); //"Err_3108qoa Expected FindLast to return false with an null item not in the collection size=16"
            T[] tempItems = new T[headItems.Length + headItems.Length];
            Array.Copy(headItems, 0, tempItems, 0, headItems.Length);
            Array.Copy(headItems, 0, tempItems, headItems.Length, headItems.Length);
            VerifyFindLastDuplicates(linkedList, tempItems);

            //[] Call FindLast with default(T) at the beginning
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddFirst(default(T));

            Assert.Null(linkedList.FindLast(tailItems[0])); //"Err_8548ajia Expected FindLast to return false with an non null item not in the collection default(T) at the beginning"
            VerifyFindLast(linkedList, prependDefaultHeadItems);

            //[] Call FindLast with default(T) in the middle
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddLast(default(T));
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(tailItems[i]);

            Assert.Null(linkedList.FindLast(generateItem())); //"Err_78585ajhed Expected FindLast to return false with an non null item not in the collection default(T) in the middle"

            tempItems = new T[headItems.Length + prependDefaultTailItems.Length];
            Array.Copy(headItems, 0, tempItems, 0, headItems.Length);
            Array.Copy(prependDefaultTailItems, 0, tempItems, headItems.Length, prependDefaultTailItems.Length);

            VerifyFindLast(linkedList, tempItems);

            //[] Call FindLast on a collection with duplicate items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddLast(default(T));

            Assert.Null(linkedList.FindLast(tailItems[0])); //"Err_208089ajdi Expected FindLast to return false with an non null item not in the collection default(T) at the end"
            T[] temp = new T[headItems.Length + 1];
            temp[headItems.Length] = default(T);
            Array.Copy(headItems, temp, headItems.Length);
            VerifyFindLast(linkedList, temp);
        }

        internal void Contains_Tests(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            T[] headItems, tailItems;

            /*Iin every other test case when we are verifying AddHead, AddTail, 
               Remove, Clear, etc we will be verifying Contains. So we dont need
               to verify the interaction just thing interseting to contains*/

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                headItems[i] = generateItem();
                tailItems[i] = generateItem();
            }

            T[] prependDefaultHeadItems = new T[headItems.Length + 1];
            prependDefaultHeadItems[0] = default(T);
            Array.Copy(headItems, 0, prependDefaultHeadItems, 1, headItems.Length);

            //[] Call Contains an empty collection
            linkedList = new LinkedList<T>();
            Assert.False(linkedList.Contains(headItems[0])); //"Err_2899hjaied Expected Contains to return false with a non null item on a empty collection"
            Assert.False(linkedList.Contains(default(T))); //"Err_5808ajiea Expected Contains to return false with a null item on a empty collection"

            //[] Call Contains on a collection with one item in it
            linkedList = new LinkedList<T>();
            linkedList.AddLast(headItems[0]);
            Assert.False(linkedList.Contains(headItems[1])); //"Err_2899hjaied Expected Contains to return false with a non null item on a empty collection size=1"
            Assert.False(linkedList.Contains(default(T))); //"Err_5808ajiea Expected Contains to return false with a null item on a empty collection size=1"
            VerifyContains(linkedList, new T[] { headItems[0] });

            //[] Call Contains on a collection with two items in it
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            Assert.False(linkedList.Contains(headItems[2])); //"Err_5680ajoed Expected Contains to return false with an non null item not in the collection size=2"
            Assert.False(linkedList.Contains(default(T))); //"Err_858196aieh Expected Contains to return false with an null item not in the collection size=2"
            VerifyContains(linkedList, new T[] { headItems[0], headItems[1] });

            //[] Call Contains on a collection with three items in it
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);
            Assert.False(linkedList.Contains(headItems[3])); //"Err_50878adie Expected Contains to return false with an non null item not in the collection size=3"
            Assert.False(linkedList.Contains(default(T))); //"Err_3969887wiqpi Expected Contains to return false with an null item not in the collection size=3"
            VerifyContains(linkedList, new T[] { headItems[0], headItems[1], headItems[2] });

            //[] Call Contains on a collection with mulitple items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
            {
                linkedList.AddLast(headItems[i]);
            }

            Assert.False(linkedList.Contains(tailItems[0])); //"Err_5808ajdoi Expected Contains to return false with an non null item not in the collection size=16"
            Assert.False(linkedList.Contains(default(T))); //"Err_5588aied Expected Contains to return false with an null item not in the collection size=16"
            VerifyContains(linkedList, headItems);

            //[] Call Contains on a collection with duplicate items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);

            Assert.False(linkedList.Contains(tailItems[0])); //"Err_8548ajia Expected Contains to return false with an non null item not in the collection size=16"
            Assert.False(linkedList.Contains(default(T))); //"Err_3108qoa Expected Contains to return false with an null item not in the collection size=16"
            T[] expectedItems = new T[headItems.Length + headItems.Length];
            Array.Copy(headItems, 0, expectedItems, 0, headItems.Length);
            Array.Copy(headItems, 0, expectedItems, headItems.Length, headItems.Length);
            VerifyContains(linkedList, expectedItems);


            //[] Call Contains with default(T) at the beginning
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddFirst(default(T));

            Assert.False(linkedList.Contains(tailItems[0]),
                "Err_8548ajia Expected Contains to return false with an non null item not in the collection default(T) at the beginning");
            VerifyContains(linkedList, prependDefaultHeadItems);


            //[] Call Contains with default(T) in the middle
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddLast(default(T));
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);

            Assert.False(linkedList.Contains(tailItems[0]),
                "Err_78585ajhed Expected Contains to return false with an non null item not in the collection default(T) in the middle");
            expectedItems = new T[headItems.Length + prependDefaultHeadItems.Length];
            Array.Copy(headItems, 0, expectedItems, 0, headItems.Length);
            Array.Copy(prependDefaultHeadItems, 0, expectedItems, headItems.Length, prependDefaultHeadItems.Length);
            VerifyContains(linkedList, expectedItems);

            //[] Call Contains on a collection with duplicate items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddLast(default(T));

            Assert.False(linkedList.Contains(tailItems[0]),
                "Err_208089ajdi Expected Contains to return false with an non null item not in the collection default(T) at the end");
            T[] temp = new T[headItems.Length + 1];
            prependDefaultHeadItems[headItems.Length] = default(T);
            Array.Copy(headItems, temp, headItems.Length);
            VerifyContains(linkedList, temp);
        }

        #region Helper Methods

        private void VerifyContains(LinkedList<T> linkedList, T[] expectedItems)
        {
            //[] Verify Contains
            for (int i = 0; i < expectedItems.Length; i++)
            {
                Assert.True(linkedList.Contains(expectedItems[i]),
                    "Err_9872haid Expected Contains with item=" + expectedItems[i] + " to return true");
            }
        }

        private void VerifyFindLastDuplicates(LinkedList<T> linkedList, T[] expectedItems)
        {
            LinkedListNode<T> previousNode, currentNode = null, nextNode;
            LinkedListNode<T>[] nodes = new LinkedListNode<T>[expectedItems.Length];
            int index = 0;

            currentNode = linkedList.First;

            while (currentNode != null)
            {
                nodes[index] = currentNode;
                currentNode = currentNode.Next;
                ++index;
            }

            for (int i = 0; i < expectedItems.Length; ++i)
            {
                currentNode = linkedList.FindLast(expectedItems[i]);

                index = Array.LastIndexOf(expectedItems, expectedItems[i]);
                previousNode = 0 < index ? nodes[index - 1] : null;
                nextNode = nodes.Length - 1 > index ? nodes[index + 1] : null;

                Assert.Equal(nodes[index], currentNode); //"Node returned from FindLast idnex=" + i.ToString()

                VerifyLinkedListNode(currentNode, expectedItems[i], linkedList, previousNode, nextNode);
            }
        }

        private void VerifyFindDuplicates(LinkedList<T> linkedList, T[] expectedItems)
        {
            LinkedListNode<T> previousNode, currentNode = null, nextNode;
            LinkedListNode<T>[] nodes = new LinkedListNode<T>[expectedItems.Length];
            int index = 0;

            currentNode = linkedList.First;

            while (currentNode != null)
            {
                nodes[index] = currentNode;
                currentNode = currentNode.Next;
                ++index;
            }

            for (int i = 0; i < expectedItems.Length; ++i)
            {
                currentNode = linkedList.Find(expectedItems[i]);

                index = Array.IndexOf(expectedItems, expectedItems[i]);
                previousNode = 0 < index ? nodes[index - 1] : null;
                nextNode = nodes.Length - 1 > index ? nodes[index + 1] : null;

                Assert.Equal(nodes[index], currentNode); //"Node returned from Find idnex=" + i.ToString()

                VerifyLinkedListNode(currentNode, expectedItems[i], linkedList, previousNode, nextNode);
            }
        }

        private void VerifyFindLast(LinkedList<T> linkedList, T[] expectedItems)
        {
            LinkedListNode<T> previousNode, currentNode, nextNode;

            currentNode = null;
            for (int i = 0; i < expectedItems.Length; ++i)
            {
                previousNode = currentNode;
                currentNode = linkedList.FindLast(expectedItems[i]);
                nextNode = currentNode.Next;
                VerifyLinkedListNode(currentNode, expectedItems[i], linkedList, previousNode, nextNode);
            }

            currentNode = null;
            for (int i = expectedItems.Length - 1; 0 <= i; --i)
            {
                nextNode = currentNode;
                currentNode = linkedList.FindLast(expectedItems[i]);
                previousNode = currentNode.Previous;
                VerifyLinkedListNode(currentNode, expectedItems[i], linkedList, previousNode, nextNode);
            }
        }

        private void VerifyFind(LinkedList<T> linkedList, T[] expectedItems)
        {
            LinkedListNode<T> previousNode, currentNode, nextNode;

            currentNode = null;
            for (int i = 0; i < expectedItems.Length; ++i)
            {
                previousNode = currentNode;
                currentNode = linkedList.Find(expectedItems[i]);
                nextNode = currentNode.Next;
                VerifyLinkedListNode(currentNode, expectedItems[i], linkedList, previousNode, nextNode);
            }

            currentNode = null;
            for (int i = expectedItems.Length - 1; 0 <= i; --i)
            {
                nextNode = currentNode;
                currentNode = linkedList.Find(expectedItems[i]);
                previousNode = currentNode.Previous;
                VerifyLinkedListNode(currentNode, expectedItems[i], linkedList, previousNode, nextNode);
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

        #endregion
    }
}
