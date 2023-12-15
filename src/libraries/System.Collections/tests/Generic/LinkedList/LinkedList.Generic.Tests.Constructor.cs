// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public abstract partial class LinkedList_Generic_Tests<T> : ICollection_Generic_Tests<T>
    {
        [Fact]
        public void CtorTest()
        {
            LinkedList_T_Tests<string> helper = new LinkedList_T_Tests<string>(new LinkedList<string>(), new string[0]);
            helper.InitialItems_Tests();
            LinkedList_T_Tests<Person> helper2 = new LinkedList_T_Tests<Person>(new LinkedList<Person>(), new Person[0]);
            helper2.InitialItems_Tests();
        }

        [Fact]
        public void Ctor_IEnumerableTest()
        {
            int arraySize = 16;
            int[] intArray = new int[arraySize];
            Person[] personArray = new Person[arraySize];

            int printableCharIndex = 65; // ascii "A"
            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i;
                char asChar = (char)(printableCharIndex + i);
                personArray[i] = new Person(asChar.ToString(), i);
            }

            // Testing with value types
            LinkedList<int> intList = new LinkedList<int>(Clone(intArray));
            LinkedList_T_Tests<int> helper = new LinkedList_T_Tests<int>(intList, intArray);
            helper.InitialItems_Tests();

            intArray = new int[0];
            intList = new LinkedList<int>(Clone(intArray));
            helper = new LinkedList_T_Tests<int>(intList, intArray);
            helper.InitialItems_Tests();

            intArray = new int[] { -3 };
            intList = new LinkedList<int>(Clone(intArray));
            helper = new LinkedList_T_Tests<int>(intList, intArray);
            helper.InitialItems_Tests();

            // Testing with an reference type
            LinkedList<Person> personList = new LinkedList<Person>(Clone(personArray));
            LinkedList_T_Tests<Person> helper2 = new LinkedList_T_Tests<Person>(personList, personArray);
            helper2.InitialItems_Tests();

            personArray = new Person[0];
            personList = new LinkedList<Person>(Clone(personArray));
            helper2 = new LinkedList_T_Tests<Person>(personList, personArray);
            helper2.InitialItems_Tests();

            personArray = new Person[] { new Person("Jack", 18) };
            personList = new LinkedList<Person>(Clone(personArray));
            helper2 = new LinkedList_T_Tests<Person>(personList, personArray);
            helper2.InitialItems_Tests();
        }

        [Fact]
        public void Ctor_IEnumerableTest_Negative()
        {
            Assert.Throws<ArgumentNullException>(() => new LinkedList<string>(null)); //"Err_982092 Expected ArgumentNullException to be thrown with null collection"
        }

        private static IEnumerable<T2> Clone<T2>(T2[] source)
        {
            if (source == null)
                return null;
            return (T2[])source.Clone();
        }
    }

    /// <summary>
    /// Helper class that verifies some properties of the linked list.
    /// </summary>
    internal class LinkedList_T_Tests<T>
    {
        private readonly LinkedList<T> _collection;
        private readonly T[] _expectedItems;

        /// <summary>
        /// Tests the linked list based on the expected items and the given linked list.
        /// </summary>
        public LinkedList_T_Tests(LinkedList<T> collection, T[] expectedItems)
        {
            _collection = collection;
            _expectedItems = expectedItems;
        }

        /// <summary>
        /// Tests the initial items in the list.
        /// </summary>
        public void InitialItems_Tests()
        {
            VerifyState(_collection, _expectedItems);
            VerifyGenericEnumerator(_collection, _expectedItems);
            VerifyEnumerator(_collection, _expectedItems);
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

            //[] Moving forward through he collection starting at head
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

            //[] Moving backward through he collection starting at Tail
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
                    "Err_9844awpa More items have been returned from the enumerator(" + iterations + " items) than are in the expectedElements(" + expectedCount + " items)");

                //[] Verify Current returned the correct value
                Assert.Equal(currentItem, expectedItems[iterations]); //"Err_1432pauy Current returned unexpected value at index: " + iterations

                //[] Verify Current always returns the same value every time it is called
                for (int i = 0; i < 3; i++)
                {
                    tempItem = enumerator.Current;
                    Assert.Equal(currentItem, tempItem); //"Err_8776phaw Current is returning inconsistent results"
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
                    "Err_9844awpa More items have been returned from the enumerator(" + iterations + " items) then are in the expectedElements(" + expectedCount + " items)");

                //[] Verify Current returned the correct value
                itemFound = false;

                for (int i = 0; i < itemsVisited.Length; ++i)
                {
                    if (!itemsVisited[i] && expectedItems[i].Equals(currentItem))
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
                    Assert.Equal(currentItem, tempItem); //"Err_8776phaw Current is returning inconsistent results Current."
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
    }

    /// <summary>
    /// Class to test reference type.
    /// </summary>
    internal class Person
    {
        internal readonly string Name;

        internal readonly int Age;

        internal Person(string name, int age)
        {
            Name = name;
            Age = age;
        }

        public override bool Equals(object obj)
        {
            Person given = obj as Person;
            if (given == null)
                return false;

            bool isNameEqual = Name.Equals(given.Name);
            bool isAgeEqual = Age.Equals(given.Age);
            return isNameEqual && isAgeEqual;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
