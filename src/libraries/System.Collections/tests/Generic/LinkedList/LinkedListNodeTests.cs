// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace LinkedList_LinkedListNodeTests
{    /// <summary>
     /// Tests that the AddBefore and AddAfter methods work for LinkedList.
     /// </summary>
     /// <remarks>
     /// Compiled from the following tests in dev11:
     /// \qa\clr\testsrc\CoreMangLib\BCL\System_Collections_Generics\LinkedList:
     /// LinkedListNode.cs
     /// </remarks>
    public class Driver<T>
    {
        public void Verify(Func<T> generateItem)
        {
            LinkedListNode<T> node;
            T value;

            //[] Verify passing default(T) into the constructor
            node = new LinkedListNode<T>(default(T));
            VerifyLinkedListNode(node, default(T), null, null, null);

            //[] Verify passing somthing other then default(T) into the constructor
            value = generateItem();
            node = new LinkedListNode<T>(value);
            VerifyLinkedListNode(node, value, null, null, null);

            //[] Verify passing somthing other then default(T) into the constructor and set the value to something other then default(T)
            value = generateItem();
            node = new LinkedListNode<T>(value);
            value = generateItem();
            node.Value = value;

            VerifyLinkedListNode(node, value, null, null, null);

            //[] Verify passing somthing other then default(T) into the constructor and set the value to default(T)
            value = generateItem();
            node = new LinkedListNode<T>(value);
            node.Value = default(T);

            VerifyLinkedListNode(node, default(T), null, null, null);

            //[] Verify passing default(T) into the constructor and set the value to something other then default(T)
            node = new LinkedListNode<T>(default(T));
            value = generateItem();
            node.Value = value;

            VerifyLinkedListNode(node, value, null, null, null);

            //[] Verify passing default(T) into the constructor and set the value to default(T)
            node = new LinkedListNode<T>(default(T));
            value = generateItem();
            node.Value = default(T);

            VerifyLinkedListNode(node, default(T), null, null, null);
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
    }
    public class LinkedListNodeTests
    {
        [Fact]
        public static void RunLinkedListNodeTests()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int m_currentInt = -5;
            Func<int> intGenerator = () =>
            {
                int current = m_currentInt;
                m_currentInt++;
                return current;
            };
            int m_currentCharAsInt = 32;
            Func<string> stringGenerator = () =>
            {
                char item = (char)m_currentCharAsInt;
                m_currentCharAsInt++;
                return item.ToString();
            };

            intDriver.Verify(intGenerator);
            stringDriver.Verify(stringGenerator);
        }
    }
}
