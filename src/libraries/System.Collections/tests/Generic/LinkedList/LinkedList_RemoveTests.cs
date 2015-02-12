// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace LinkedList_LinkedList_RemoveTests
{
    public class LinkedList_RemoveTests
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
        public static void Remove_T()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.Remove_T(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.Remove_T(s_generateInt);
        }

        [Fact]
        public static void Remove_Duplicates_T()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.Remove_Duplicates_T(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.Remove_Duplicates_T(s_generateInt);
        }

        [Fact]
        public static void Remove_LLNode()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.Remove_LLNode(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.Remove_LLNode(s_generateInt);
        }

        [Fact]
        public static void Remove_Duplicates_LLNode()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.Remove_Duplicates_LLNode(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.Remove_Duplicates_LLNode(s_generateInt);
        }

        [Fact]
        public static void Remove_LLNode_Negative()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.Remove_LLNode_Negative(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.Remove_LLNode_Negative(s_generateInt);
        }

        [Fact]
        public static void RemoveFirst_Tests()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.RemoveFirst_Tests(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.RemoveFirst_Tests(s_generateInt);
        }

        [Fact]
        public static void RemoveFirst_Tests_Negative()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.RemoveFirst_Tests_Negative();
        }

        [Fact]
        public static void RemoveLast_Tests()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.RemoveLast_Tests(s_generateString);

            LinkedList_T_Tests<int> helper2 = new LinkedList_T_Tests<int>();
            helper2.RemoveLast_Tests(s_generateInt);
        }

        [Fact]
        public static void RemoveLast_Tests_Negative()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>();
            helper.RemoveLast_Tests_Negative();
        }
    }

    /// <summary>
    /// Helper class that verifies some properties of the linked list.
    /// </summary>
    internal class LinkedList_T_Tests<T>
    {
        internal void Remove_T(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            T[] headItems, headItemsReverse, tailItems, tempItems;
            LinkedListNode<T> tempNode1, tempNode2, tempNode3;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                headItems[i] = generateItem();
                tailItems[i] = generateItem();
            }

            headItemsReverse = new T[arraySize];
            Array.Copy(headItems, headItemsReverse, headItems.Length);
            Array.Reverse(headItemsReverse);

            //[] Call Remove an empty collection
            linkedList = new LinkedList<T>();
            Assert.False(linkedList.Remove(headItems[0])); //"Err_1518eaid Remove retunred true with a non null "
            Assert.False(linkedList.Remove(default(T))); //"Err_45485eajid Remove retunred true with a null "
            InitialItems_Tests(linkedList, new T[0]);

            //[] Call Remove with an item that does not exist in the collection size=1
            linkedList = new LinkedList<T>();
            linkedList.AddLast(headItems[0]);

            Assert.False(linkedList.Remove(headItems[1])); //"Err_1188aeid Remove retunred true with a non null item "
            Assert.False(linkedList.Remove(default(T))); //"Err_3188eajid Remove retunred true with a null item "

            InitialItems_Tests(linkedList, new T[] { headItems[0] });

            //[] Call Remove with an item that does not exist in the collection size=1
            linkedList = new LinkedList<T>();
            linkedList.AddLast(headItems[0]);
            tempNode1 = linkedList.First;

            Assert.True(linkedList.Remove(headItems[0])); //"Err_25188ehiad Remove retunred false with the head item "

            InitialItems_Tests(linkedList, new T[0]);
            VerifyRemovedNode(tempNode1, headItems[0]);

            //[] Call Remove with an item that does not exist in the collection size=2
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);

            Assert.False(linkedList.Remove(headItems[2])); //"Err_39484ehiuad Remove retunred true with a non null item "
            Assert.False(linkedList.Remove(default(T))); //"Err_0548ieae Remove retunred true with a null item "

            InitialItems_Tests(linkedList, new T[] { headItems[0], headItems[1] });

            //[] Call Remove with the Head collection size=2
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            tempNode1 = linkedList.First;

            Assert.True(linkedList.Remove(headItems[0])); //"Err_3188eajid Remove retunred false with the head item "

            InitialItems_Tests(linkedList, new T[] { headItems[1] });
            VerifyRemovedNode(tempNode1, headItems[0]);

            //[] Call Remove with the Tail collection size=2
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            tempNode1 = linkedList.Last;

            Assert.True(linkedList.Remove(headItems[1])); //"Err_97525ehad Remove retunred false with the tail item "

            InitialItems_Tests(linkedList, new T[] { headItems[0] });
            VerifyRemovedNode(tempNode1, headItems[1]);

            //[] Call Remove all the items collection size=2
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            tempNode1 = linkedList.First;
            tempNode2 = linkedList.Last;

            Assert.True(linkedList.Remove(headItems[0])); //"Err_413882qoea Remove retunred false with the head item "
            Assert.True(linkedList.Remove(headItems[1])); //"Err_31288qiae Remove retunred false with the tail item "

            InitialItems_Tests(linkedList, new T[0]);
            VerifyRemovedNode(linkedList, new T[0], tempNode1, headItems[0]);
            VerifyRemovedNode(linkedList, new T[0], tempNode2, headItems[1]);

            //[] Call Remove with an item that does not exist in the collection size=3
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);

            Assert.False(linkedList.Remove(headItems[3])); //"Err_84900aeua Remove retunred true with a non null item "
            Assert.False(linkedList.Remove(default(T))); //"Err_5388iqiqa Remove retunred true with a null item on a empty "
            InitialItems_Tests(linkedList, new T[] { headItems[0], headItems[1], headItems[2] });

            //[] Call Remove with the Head collection size=3
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);
            tempNode1 = linkedList.First;

            Assert.True(linkedList.Remove(headItems[0])); //"Err_30884qrzo Remove retunred false with the head item, "

            InitialItems_Tests(linkedList, new T[] { headItems[1], headItems[2] });
            VerifyRemovedNode(linkedList, new T[] { headItems[1], headItems[2] }, tempNode1, headItems[0]);

            //[] Call Remove with the middle item collection size=3
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);
            tempNode1 = linkedList.First.Next;

            Assert.True(linkedList.Remove(headItems[1])); //"Err_988158quiozq Remove retunred false with the middle item "

            InitialItems_Tests(linkedList, new T[] { headItems[0], headItems[2] });
            VerifyRemovedNode(linkedList, new T[] { headItems[0], headItems[2] }, tempNode1, headItems[1]);

            //[] Call Remove with the Tail collection size=3
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);
            tempNode1 = linkedList.Last;

            Assert.True(linkedList.Remove(headItems[2])); //"Expected to be able to remove item: " + headItems[2] + "from the list."

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

            Assert.True(linkedList.Remove(headItems[2])); //"Err_5488oiwis Remove retunred false with the Tail item "
            Assert.True(linkedList.Remove(headItems[1])); //"Err_2808ajide Remove retunred false with the middle item "
            Assert.True(linkedList.Remove(headItems[0])); //"Err_1888ajiw Remove retunred false with the Head item "

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
                Assert.True(linkedList.Remove(headItems[i]), "Err_5688ajidi Remove to returned false index= " + i);
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
                Assert.True(linkedList.Remove(headItems[i]), "Err_51888ajhid Remove to returned false index=" + 1);
                T[] expectedItems = new T[i];
                Array.Copy(headItems, 0, expectedItems, 0, i);
                InitialItems_Tests(linkedList, expectedItems);
            }

            //[] Remove some items in the middle
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddFirst(headItems[i]);

            Assert.True(linkedList.Remove(headItems[2])); //"Err_6488akod Remove 3rd item "
            Assert.True(linkedList.Remove(headItems[headItems.Length - 3])); //"Err_15188ajei Remove 3rd from last item "
            Assert.True(linkedList.Remove(headItems[1])); //"Err_1588ajoied Remove 2nd item "
            Assert.True(linkedList.Remove(headItems[headItems.Length - 2])); //"Err_1888ajied Remove 2nd from last item "
            Assert.True(linkedList.Remove(headItems[0])); //"Err_402558aide Remove first item returned false "
            Assert.True(linkedList.Remove(headItems[headItems.Length - 1])); //"Err_56588eajidi Remove last item returned false "

            //With the above remove we should have removed the first and last 3 items 
            tempItems = new T[headItemsReverse.Length - 6];
            Array.Copy(headItemsReverse, 3, tempItems, 0, headItemsReverse.Length - 6);

            InitialItems_Tests(linkedList, tempItems);

            //[] Remove an item with a value of default(T)
            linkedList = new LinkedList<T>();

            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(headItems[i]);

            linkedList.AddLast(default(T));

            Assert.True(linkedList.Remove(default(T))); //"Err_29829ahid Remove default(T) "

            InitialItems_Tests(linkedList, headItems);
        }

        internal void Remove_Duplicates_T(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            T[] items;
            LinkedListNode<T>[] nodes = new LinkedListNode<T>[arraySize * 2];
            LinkedListNode<T> currentNode;
            int index;

            items = new T[arraySize];

            for (int i = 0; i < arraySize; i++)
            {
                items[i] = generateItem();
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

            Assert.True(linkedList.Remove(items[2])); //"Err_58088ajode Remove 3rd item "
            Assert.True(linkedList.Remove(items[items.Length - 3])); //"Err_15188ajei Remove 3rd from last item "
            Assert.True(linkedList.Remove(items[1])); //"Err_9945aied Err_92872 Remove 2nd item "
            Assert.True(linkedList.Remove(items[items.Length - 2])); //"Err_1888ajied Remove 2nd from last item "
            Assert.True(linkedList.Remove(items[0])); //"Err_08486aiepz Remove first item returned false "
            Assert.True(linkedList.Remove(items[items.Length - 1])); //"Err_56588eajidi Remove last item returned false "

            //[] Verify that the duplicates were removed from the begining of the collection
            currentNode = linkedList.First;

            //Verify the duplicates that should have been removed
            for (int i = 3; i < arraySize - 3; ++i)
            {
                Assert.NotNull(currentNode); //"Err_48588ahid CurrentNode is null index= " + i
                Assert.Equal(currentNode, nodes[i]); //"Err_5488ahid CurrentNode is not the expected node index= " + i
                Assert.Equal(items[i], currentNode.Value); //"Err_16588ajide CurrentNode value index=" + i
                currentNode = currentNode.Next;
            }

            //Verify the duplicates that should NOT have been removed
            for (int i = 0; i < arraySize; ++i)
            {
                Assert.NotNull(currentNode); //"Err_5658ajidi CurrentNode is null index= " + i
                Assert.Equal(currentNode, nodes[arraySize + i]); //"Err_4865423aidie CurrentNode is not the expected node index= " + i
                Assert.Equal(items[i], currentNode.Value); //"Err_54808ajoid CurrentNode value index=" + i
                currentNode = currentNode.Next;
            }
            Assert.Null(currentNode); //"Err_30878ajid Expceted CurrentNode to be null after moving through entire list"
        }

        internal void Remove_LLNode(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            T[] headItems, tailItems, tempItems;
            LinkedListNode<T> tempNode1, tempNode2, tempNode3;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                headItems[i] = generateItem();
                tailItems[i] = generateItem();
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

        internal void Remove_Duplicates_LLNode(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            T[] items;
            LinkedListNode<T>[] nodes = new LinkedListNode<T>[arraySize * 2];
            LinkedListNode<T> currentNode;
            int index;

            items = new T[arraySize];

            for (int i = 0; i < arraySize; i++)
            {
                items[i] = generateItem();
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

            //[] Verify that the duplicates were removed from the begining of the collection
            currentNode = linkedList.First;

            //Verify the duplicates that should have been removed
            for (int i = 3; i < nodes.Length - 3; ++i)
            {
                Assert.NotNull(currentNode); //"Err_48588ahid CurrentNode is null index=" + i
                Assert.Equal(currentNode, nodes[i]); //"Err_5488ahid CurrentNode is not the expected node index=" + i
                Assert.Equal(items[i % items.Length], currentNode.Value); //"Err_16588ajide CurrentNode value index=" + i

                currentNode = currentNode.Next;
            }

            Assert.Null(currentNode); //"Err_30878ajid Expceted CurrentNode to be null after moving through entire list"
        }

        internal void Remove_LLNode_Negative(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            LinkedList<T> tempLinkedList = new LinkedList<T>();
            T[] items;

            //[] Verify Null node
            linkedList = new LinkedList<T>();
            Assert.Throws<ArgumentNullException>(() => linkedList.Remove(null)); //"Err_858ahia Expected null node to throws ArgumentNullException\n"

            InitialItems_Tests(linkedList, new T[0]);

            //[] Verify Node that is a new Node
            linkedList = new LinkedList<T>();
            items = new T[] { generateItem() };
            linkedList.AddLast(items[0]);
            Assert.Throws<InvalidOperationException>(() => linkedList.Remove(new LinkedListNode<T>(generateItem()))); //"Err_0568ajods Expected Node that is a new Node throws InvalidOperationException\n"

            InitialItems_Tests(linkedList, items);

            //[] Verify Node that already exists in another collection
            linkedList = new LinkedList<T>();
            items = new T[] { generateItem(), generateItem() };
            linkedList.AddLast(items[0]);
            linkedList.AddLast(items[1]);

            tempLinkedList.Clear();
            tempLinkedList.AddLast(generateItem());
            tempLinkedList.AddLast(generateItem());
            Assert.Throws<InvalidOperationException>(() => linkedList.Remove(tempLinkedList.Last)); //"Err_98809ahied Node that already exists in another collection throws InvalidOperationException\n"

            InitialItems_Tests(linkedList, items);
        }

        internal void RemoveFirst_Tests(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            T[] headItems, tailItems;
            LinkedListNode<T> tempNode1, tempNode2, tempNode3;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                headItems[i] = generateItem();
                tailItems[i] = generateItem();
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

        internal void RemoveFirst_Tests_Negative()
        {
            //[] Call RemoveHead an empty collection
            LinkedList<T> linkedList = new LinkedList<T>();
            Assert.Throws<InvalidOperationException>(() => linkedList.RemoveFirst()); //"Expected invalidoperation exception removing from empty list."
            InitialItems_Tests(linkedList, new T[0]);
        }

        internal void RemoveLast_Tests(Func<T> generateItem)
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            T[] headItems, tailItems;
            LinkedListNode<T> tempNode1, tempNode2, tempNode3;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                headItems[i] = generateItem();
                tailItems[i] = generateItem();
            }

            //[] Call RemoveHead on a collection with one item in it
            linkedList = new LinkedList<T>();
            linkedList.AddLast(headItems[0]);
            tempNode1 = linkedList.Last;

            linkedList.RemoveLast();
            InitialItems_Tests(linkedList, new T[0]);
            VerifyRemovedNode(tempNode1, headItems[0]);

            //[] Call RemoveHead on a collection with two items in it
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            tempNode1 = linkedList.Last;
            tempNode2 = linkedList.First;

            linkedList.RemoveLast();
            InitialItems_Tests(linkedList, new T[] { headItems[0] });

            linkedList.RemoveLast();
            InitialItems_Tests(linkedList, new T[0]);

            VerifyRemovedNode(linkedList, tempNode1, headItems[1]);
            VerifyRemovedNode(linkedList, tempNode2, headItems[0]);

            //[] Call RemoveHead on a collection with three items in it
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);
            tempNode1 = linkedList.Last;
            tempNode2 = linkedList.Last.Previous;
            tempNode3 = linkedList.First;

            linkedList.RemoveLast();
            InitialItems_Tests(linkedList, new T[] { headItems[0], headItems[1] });

            linkedList.RemoveLast();
            InitialItems_Tests(linkedList, new T[] { headItems[0] });

            linkedList.RemoveLast();
            InitialItems_Tests(linkedList, new T[0]);
            VerifyRemovedNode(tempNode1, headItems[2]);
            VerifyRemovedNode(tempNode2, headItems[1]);
            VerifyRemovedNode(tempNode3, headItems[0]);

            //[] Call RemoveHead on a collection with 16 items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(headItems[i]);

            for (int i = 0; i < arraySize; ++i)
            {
                linkedList.RemoveLast();
                int length = arraySize - i - 1;
                T[] expectedItems = new T[length];
                Array.Copy(headItems, 0, expectedItems, 0, length);
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

        internal void RemoveLast_Tests_Negative()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            Assert.Throws<InvalidOperationException>(() => linkedList.RemoveLast()); //"Expected invalidoperation exception removing from empty list."
            InitialItems_Tests(linkedList, new T[0]);
        }

        #region Helper Methods

        private void VerifyRemovedNode(LinkedListNode<T> node, T expectedValue)
        {
            LinkedList<T> tempLinkedList = new LinkedList<T>();
            LinkedListNode<T> headNode, tailNode;

            tempLinkedList.AddLast(default(T));
            tempLinkedList.AddLast(default(T));
            headNode = tempLinkedList.First;
            tailNode = tempLinkedList.Last;

            Assert.Null(node.List); //"Err_298298anied Node.LinkedList returned non null"
            Assert.Null(node.Previous); //"Err_298298anied Node.Previous returned non null"
            Assert.Null(node.Next); //"Err_298298anied Node.Next returned non null"
            Assert.Equal(expectedValue, node.Value); //"Err_969518aheoia Node.Value"

            tempLinkedList.AddAfter(tempLinkedList.First, node);

            Assert.Equal(tempLinkedList, node.List); //"Err_7894ahioed Node.LinkedList"
            Assert.Equal(headNode, node.Previous); //"Err_14520aheoak Node.Previous"
            Assert.Equal(tailNode, node.Next); //"Err_42358aujea Node.Next"
            Assert.Equal(expectedValue, node.Value); //"Err_64888joqaxz Node.Value"

            InitialItems_Tests(tempLinkedList, new T[] { default(T), expectedValue, default(T) });
        }

        private void VerifyRemovedNode(LinkedList<T> linkedList, LinkedListNode<T> node, T expectedValue)
        {
            LinkedListNode<T> tailNode = linkedList.Last;

            Assert.Null(node.List); //"Err_564898ajid Node.LinkedList returned non null"
            Assert.Null(node.Previous); //"Err_30808wia Node.Previous returned non null"
            Assert.Null(node.Next); //"Err_78280aoiea Node.Next returned non null"
            Assert.Equal(expectedValue, node.Value); //"Err_98234aued Node.Value"

            linkedList.AddLast(node);
            Assert.Equal(linkedList, node.List); //"Err_038369aihead Node.LinkedList"
            Assert.Equal(tailNode, node.Previous); //"Err_789108aiea Node.Previous"
            Assert.Null(node.Next); //"Err_37896riad Node.Next returned non null"

            linkedList.RemoveLast();
        }

        private void VerifyRemovedNode(LinkedList<T> linkedList, T[] linkedListValues, LinkedListNode<T> node, T expectedValue)
        {
            LinkedListNode<T> tailNode = linkedList.Last;

            Assert.Null(node.List); //"Err_564898ajid Node.LinkedList returned non null"
            Assert.Null(node.Previous); //"Err_30808wia Node.Previous returned non null"
            Assert.Null(node.Next); //"Err_78280aoiea Node.Next returned non null"
            Assert.Equal(expectedValue, node.Value); //"Err_98234aued Node.Value"

            linkedList.AddLast(node);
            Assert.Equal(linkedList, node.List); //"Err_038369aihead Node.LinkedList"
            Assert.Equal(tailNode, node.Previous); //"Err_789108aiea Node.Previous"
            Assert.Null(node.Next); //"Err_37896riad Node.Next returned non null"
            Assert.Equal(expectedValue, node.Value); //"Err_823902jaied Node.Value"

            T[] expected = new T[linkedListValues.Length + 1];
            Array.Copy(linkedListValues, 0, expected, 0, linkedListValues.Length);
            expected[linkedListValues.Length] = expectedValue;

            InitialItems_Tests(linkedList, expected);
            linkedList.RemoveLast();
        }

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

        #endregion
    }
}
