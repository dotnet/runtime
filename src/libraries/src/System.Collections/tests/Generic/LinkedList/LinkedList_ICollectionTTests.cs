// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace LinkedList_LinkedList_ICollectionTTests
{
    public class LinkedList_ICollectionTests
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
        public static void Add_Tests()
        {
            int arraySize = 16;
            int[] intItems = new int[arraySize];
            string[] stringItems = new string[arraySize];

            LinkedList<int> linkedList = new LinkedList<int>();
            LinkedList<string> linkedList2 = new LinkedList<string>();

            for (int i = 0; i < arraySize; ++i)
            {
                intItems[i] = s_generateInt();
                stringItems[i] = s_generateString();
                linkedList.AddLast(intItems[i]);
                linkedList2.AddLast(stringItems[i]);
            }

            LinkedList_T_Tests<int> helper = new LinkedList_T_Tests<int>(linkedList, intItems, s_generateInt);
            helper.Add_Tests();
            LinkedList_T_Tests<string> helper2 = new LinkedList_T_Tests<string>(linkedList2, stringItems, s_generateString);
            helper.Add_Tests();
        }

        [Fact]
        public static void Clear_Tests()
        {
            int arraySize = 16;
            int[] intItems = new int[arraySize];
            string[] stringItems = new string[arraySize];

            LinkedList<int> linkedList = new LinkedList<int>();
            LinkedList<string> linkedList2 = new LinkedList<string>();

            for (int i = 0; i < arraySize; ++i)
            {
                intItems[i] = s_generateInt();
                stringItems[i] = s_generateString();
                linkedList.AddLast(intItems[i]);
                linkedList2.AddLast(stringItems[i]);
            }

            LinkedList_T_Tests<int> helper = new LinkedList_T_Tests<int>(linkedList, intItems, s_generateInt);
            helper.Clear_Tests();
            LinkedList_T_Tests<string> helper2 = new LinkedList_T_Tests<string>(linkedList2, stringItems, s_generateString);
            helper.Clear_Tests();
        }

        [Fact]
        public static void Contains_Tests()
        {
            int arraySize = 16;
            int[] intItems = new int[arraySize];
            string[] stringItems = new string[arraySize];

            LinkedList<int> linkedList = new LinkedList<int>();
            LinkedList<string> linkedList2 = new LinkedList<string>();

            for (int i = 0; i < arraySize; ++i)
            {
                intItems[i] = s_generateInt();
                stringItems[i] = s_generateString();
                linkedList.AddLast(intItems[i]);
                linkedList2.AddLast(stringItems[i]);
            }

            LinkedList_T_Tests<int> helper = new LinkedList_T_Tests<int>(linkedList, intItems, s_generateInt);
            helper.Contains_Tests();
            LinkedList_T_Tests<string> helper2 = new LinkedList_T_Tests<string>(linkedList2, stringItems, s_generateString);
            helper.Contains_Tests();
        }

        [Fact]
        public static void CopyTo_Tests()
        {
            int arraySize = 16;
            int[] intItems = new int[arraySize];
            string[] stringItems = new string[arraySize];

            LinkedList<int> linkedList = new LinkedList<int>();
            LinkedList<string> linkedList2 = new LinkedList<string>();

            for (int i = 0; i < arraySize; ++i)
            {
                intItems[i] = s_generateInt();
                stringItems[i] = s_generateString();
                linkedList.AddLast(intItems[i]);
                linkedList2.AddLast(stringItems[i]);
            }

            LinkedList_T_Tests<int> helper = new LinkedList_T_Tests<int>(linkedList, intItems, s_generateInt);
            helper.CopyTo_Tests();
            LinkedList_T_Tests<string> helper2 = new LinkedList_T_Tests<string>(linkedList2, stringItems, s_generateString);
            helper.CopyTo_Tests();
        }

        [Fact]
        public static void CopyTo_Tests_Negative()
        {
            int arraySize = 16;
            int[] intItems = new int[arraySize];
            string[] stringItems = new string[arraySize];

            LinkedList<int> linkedList = new LinkedList<int>();
            LinkedList<string> linkedList2 = new LinkedList<string>();

            for (int i = 0; i < arraySize; ++i)
            {
                intItems[i] = s_generateInt();
                stringItems[i] = s_generateString();
                linkedList.AddLast(intItems[i]);
                linkedList2.AddLast(stringItems[i]);
            }

            LinkedList_T_Tests<int> helper = new LinkedList_T_Tests<int>(linkedList, intItems, s_generateInt);
            helper.CopyTo_Tests_Negative();
            LinkedList_T_Tests<string> helper2 = new LinkedList_T_Tests<string>(linkedList2, stringItems, s_generateString);
            helper.CopyTo_Tests_Negative();
        }

        [Fact]
        public static void Remove_Tests()
        {
            int arraySize = 16;
            int[] intItems = new int[arraySize];
            string[] stringItems = new string[arraySize];

            LinkedList<int> linkedList = new LinkedList<int>();
            LinkedList<string> linkedList2 = new LinkedList<string>();

            for (int i = 0; i < arraySize; ++i)
            {
                intItems[i] = s_generateInt();
                stringItems[i] = s_generateString();
                linkedList.AddLast(intItems[i]);
                linkedList2.AddLast(stringItems[i]);
            }

            LinkedList_T_Tests<int> helper = new LinkedList_T_Tests<int>(linkedList, intItems, s_generateInt);
            helper.Remove_Tests();
            LinkedList_T_Tests<string> helper2 = new LinkedList_T_Tests<string>(linkedList2, stringItems, s_generateString);
            helper.Remove_Tests();
        }

        [Fact]
        public static void InvalidateEnumerator_Tests()
        {
            int arraySize = 16;
            int[] intItems = new int[arraySize];
            string[] stringItems = new string[arraySize];

            LinkedList<int> linkedList = new LinkedList<int>();
            LinkedList<string> linkedList2 = new LinkedList<string>();

            for (int i = 0; i < arraySize; ++i)
            {
                intItems[i] = s_generateInt();
                stringItems[i] = s_generateString();
                linkedList.AddLast(intItems[i]);
                linkedList2.AddLast(stringItems[i]);
            }

            LinkedList_T_Tests<int> helper = new LinkedList_T_Tests<int>(linkedList, intItems, s_generateInt);
            helper.InvalidateEnumerator_Tests();
            LinkedList_T_Tests<string> helper2 = new LinkedList_T_Tests<string>(linkedList2, stringItems, s_generateString);
            helper.InvalidateEnumerator_Tests();
        }

        [Fact]
        public static void IEnumerable_Tests()
        {
            int arraySize = 16;
            int[] intItems = new int[arraySize];
            string[] stringItems = new string[arraySize];

            LinkedList<int> linkedList = new LinkedList<int>();
            LinkedList<string> linkedList2 = new LinkedList<string>();

            for (int i = 0; i < arraySize; ++i)
            {
                intItems[i] = s_generateInt();
                stringItems[i] = s_generateString();
                linkedList.AddLast(intItems[i]);
                linkedList2.AddLast(stringItems[i]);
            }

            LinkedList_T_Tests<int> helper = new LinkedList_T_Tests<int>(linkedList, intItems, s_generateInt);
            helper.IEnumerable_Tests();
            LinkedList_T_Tests<string> helper2 = new LinkedList_T_Tests<string>(linkedList2, stringItems, s_generateString);
            helper.IEnumerable_Tests();
        }

        [Fact]
        public static void SetOriginalItems_Tests()
        {
            int arraySize = 16;
            int[] intItems = new int[arraySize];
            string[] stringItems = new string[arraySize];

            LinkedList<int> linkedList = new LinkedList<int>();
            LinkedList<string> linkedList2 = new LinkedList<string>();

            for (int i = 0; i < arraySize; ++i)
            {
                intItems[i] = s_generateInt();
                stringItems[i] = s_generateString();
                linkedList.AddLast(intItems[i]);
                linkedList2.AddLast(stringItems[i]);
            }

            LinkedList_T_Tests<int> helper = new LinkedList_T_Tests<int>(linkedList, intItems, s_generateInt);
            helper.SetOriginalItems_Tests();
            LinkedList_T_Tests<string> helper2 = new LinkedList_T_Tests<string>(linkedList2, stringItems, s_generateString);
            helper.SetOriginalItems_Tests();
        }
    }
    /// <summary>
    /// Helper class that verifies some properties of the linked list.
    /// </summary>
    internal class LinkedList_T_Tests<T>
    {
        private readonly ICollection<T> _collection;
        private readonly Func<T> _generateItem;
        private readonly T[] _originalItems;

        private T[] _items;
        private T[] _generatedItems = new T[0];

        internal LinkedList_T_Tests(LinkedList<T> collection, T[] items, Func<T> generateItems)
        {
            _collection = collection;
            _items = items;
            _generateItem = generateItems;
            _originalItems = (T[])_items.Clone();
        }

        /// <summary>
        /// Runs all of the tests on Add(Object).
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        internal void Add_Tests()
        {
            T[] tempItems;

            //[] Add with null value somewhere in the middle
            _items = new T[33];

            _collection.Clear();
            tempItems = GenerateItems(32);
            Array.Copy(tempItems, 0, _items, 0, 16);
            Array.Copy(tempItems, 16, _items, 17, 16);

            AddItemsToCollection(_collection, tempItems, 0, 16);

            _collection.Add(default(T));
            _items[16] = default(T);

            AddItemsToCollection(_collection, tempItems, 16, 16);

            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] Add with null value at the begining
            _items = new T[17];

            _collection.Clear();
            _collection.Add(default(T));
            _items[0] = default(T);

            tempItems = AddItemsToCollection(_collection, 16);
            Array.Copy(tempItems, 0, _items, 1, 16);

            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] Add with null value at the end
            _items = new T[17];

            _collection.Clear();

            tempItems = AddItemsToCollection(_collection, 16);
            Array.Copy(tempItems, 0, _items, 0, 16);

            _collection.Add(default(T));
            _items[16] = default(T);

            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] Add duplicate value
            _items = new T[32];

            _collection.Clear();
            tempItems = AddItemsToCollection(_collection, 16);
            AddItemsToCollection(_collection, tempItems, 0, tempItems.Length);

            Array.Copy(tempItems, 0, _items, 0, 16);
            Array.Copy(tempItems, 0, _items, 16, 16);

            VerifyCollection(_collection, _items, 0, _items.Length);


            //[] Add some _items call clear then add more
            _items = new T[16];

            _collection.Clear();
            AddItemsToCollection(_collection, 16);
            _collection.Clear();
            _items = AddItemsToCollection(_collection, 16);

            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] Add some _items remove only some of then then Add more _items
            _items = new T[23];

            _collection.Clear();
            tempItems = GenerateItems(32);
            AddItemsToCollection(_collection, tempItems, 0, 16);

            for (int i = 0; i < 16; i++)
            {
                if ((i & 1) == 0)
                {
                    //Even number
                    _collection.Remove(tempItems[i]);
                }
                else
                {
                    //Odd
                    _items[i / 2] = tempItems[i];
                }
            }

            //Remove the very last item this may invoke special code for handling removal of the tail
            _collection.Remove(tempItems[15]);

            AddItemsToCollection(_collection, tempItems, 16, 16);
            Array.Copy(tempItems, 16, _items, 7, 16);

            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] Add some _items remove all of them then Add more _items
            _items = new T[24];

            _collection.Clear();
            tempItems = AddItemsToCollection(_collection, 16);

            for (int i = 0; i < tempItems.Length; i++)
            {
                _collection.Remove(tempItems[i]);
            }

            _items = AddItemsToCollection(_collection, 16);

            VerifyCollection(_collection, _items, 0, _items.Length);
        }

        /// <summary>
        /// Runs all of the tests on Clear().
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        internal void Clear_Tests()
        {
            //[] Call Clear on an empty collection then Add some _items
            _collection.Clear();
            _items = AddItemsToCollection(_collection, 16);

            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] Call Clear several time on an empty collection then add some _items
            _collection.Clear();
            _collection.Clear();
            _collection.Clear();
            _items = AddItemsToCollection(_collection, 16);

            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] Add some _items remove some of them call Clear then add some more _items
            _collection.Clear();
            _items = AddItemsToCollection(_collection, 16);

            for (int i = 0; i < _items.Length; i++)
            {
                if ((i & 1) == 0)
                {
                    //Even number
                    _collection.Remove(_items[i]);
                }
            }

            //Remove the very last item this may invoke special code for handling removal of the tail
            _collection.Remove(_items[15]);

            _collection.Clear();
            _items = AddItemsToCollection(_collection, 16);

            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] Add some _items remove all of them call Clear then add some more _items
            _collection.Clear();
            _items = AddItemsToCollection(_collection, 16);

            for (int i = 0; i < _items.Length; i++)
            {
                _collection.Remove(_items[i]);
            }

            _collection.Clear();
            _items = AddItemsToCollection(_collection, 16);

            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] Call Clear on a collection with one item in it
            _collection.Clear();
            _items = AddItemsToCollection(_collection, 1);

            _collection.Clear();
            _items = AddItemsToCollection(_collection, 16);

            VerifyCollection(_collection, _items, 0, _items.Length);
        }

        /// <summary>
        /// Runs all of the tests on Contains(Object).
        /// </summary>
        internal void Contains_Tests()
        {
            T[] tempItems;

            tempItems = GenerateItems(32);

            //[] On an empty collection call Contains with some non null value
            _collection.Clear();

            Assert.False(_collection.Contains(_generateItem()),
                "Err_17081 Contains returned true with non null T on an empty collection");

            //[] With an empty collection call Add with non null value and call Contains with that value
            _collection.Clear();
            _collection.Add(tempItems[0]);
            _items = new T[1] { tempItems[0] };

            Assert.True(_collection.Contains(_items[0]),
                "Err_23198ahsi Contains returned false with value that was added to an empty collection");

            //[] With an empty collection call Add with non null value and call Contains with some other value
            _collection.Clear();
            _collection.Add(tempItems[0]);
            _items = new T[1] { tempItems[0] };

            Assert.False(_collection.Contains(tempItems[1]),
                "Err_2589lpzi Contains returned false with value other then the value that was added to an empty collection");

            //[] With an empty collection call Add with non null value and call Contains with null value

            _collection.Clear();
            for (int i = 0; i < tempItems.Length; ++i)
            {
                if (!tempItems[i].Equals(default(T)))
                {
                    _collection.Add(tempItems[i]);
                    _items = new T[1] { tempItems[i] };
                    break;
                }
            }

            Assert.False(_collection.Contains(default(T)),
                "Err_3294sjpd Contains returned false with null value on an empty collection with one item added to it");

            //[] On an empty collection call Contains with a null value
            _collection.Clear();

            Assert.False(_collection.Contains(default(T)),
                "Err_6149ajpqn Contains returned true with null value on an empty collection");

            //[] On an empty collection call Add with null value and call Contains with null value
            _collection.Clear();
            _collection.Add(default(T));

            Assert.True(_collection.Contains(default(T)),
                "Err_5586pahl Contains returned false with null value on an empty collection with null added to it");

            //[] With a collection that contains null somewhere in middle of the collection call Conatains with null, some value that does not exist and with a value that does exist
            _collection.Clear();
            _items = new T[30];

            Array.Copy(tempItems, 0, _items, 0, 15);
            _items[15] = default(T);
            Array.Copy(tempItems, 15, _items, 16, 14);

            AddItemsToCollection(_collection, _items, 0, _items.Length);

            for (int i = 0; i < _items.Length; i++)
            {
                Assert.True(_collection.Contains(_items[i])); //"Err_3697ahsm Contains returned false with " + _items[i] + " expected true"
            }

            Assert.False(_collection.Contains(tempItems[30].Equals(default(T)) ? tempItems[31] : tempItems[30]),
                "Err_1259yhpa Contains returned true with unique T not in the collection expected false");

            //[] With a collection that contains null at the last item in the collection call Conatains with null, some value that does not exist and with a value that does exist

            _collection.Clear();
            _items = new T[17];

            AddItemsToCollection(_collection, tempItems, 0, 16);
            Array.Copy(tempItems, 0, _items, 0, 16);

            _collection.Add(default(T));
            _items[16] = default(T);

            for (int i = 0; i < _items.Length; i++)
            {
                Assert.True(_collection.Contains(_items[i])); //"Err_1238hspo Contains returned false with " + _items[i] + " expected true"
            }
            Assert.False(_collection.Contains(tempItems[30].Equals(default(T)) ? tempItems[31] : tempItems[30]),
                "Err_31289snos Contains returned true with new T() expected false");

            //[] Call Contains with a value that exists twice in the collection	
            _collection.Clear();
            AddItemsToCollection(_collection, tempItems, 0, 16);
            AddItemsToCollection(_collection, tempItems, 0, 16);
            _items = new T[16];
            Array.Copy(tempItems, 0, _items, 0, 16);

            for (int i = 0; i < _items.Length; i++)
            {
                Assert.True(_collection.Contains(_items[i])); //"Err_85431aphs Contains returned false with " + _items[i] + " expected true"
            }

            Assert.False(_collection.Contains(tempItems[16]),
                "Err_2468hap Contains returned true with new T() expected false");

            //[] With a collection that all of the _items in the collection are unique call Contains with every item
            _collection.Clear();
            AddItemsToCollection(_collection, tempItems, 0, 31);
            _items = new T[31];
            Array.Copy(tempItems, 0, _items, 0, 31);

            for (int i = 0; i < _items.Length; i++)
            {
                Assert.True(_collection.Contains(_items[i])); //"65943ahps  Contains returned false with " + _items[i] + " expected true"
            }

            Assert.False(_collection.Contains(tempItems[31]),
                "Err_9446haos Contains returned true with unique T not in the collection expected false");
        }

        /// <summary>
        /// Runs all of the tests on CopyTo(Array).
        /// </summary>
        internal void CopyTo_Tests()
        {
            T[] itemArray = null, tempItemsArray = null;

            // [] CopyTo with index=0 and the array is the same size as the collection
            itemArray = GenerateArray(_items.Length);

            _collection.CopyTo(itemArray, 0);

            VerifyCollection(itemArray, _items, 0, _items.Length);

            // [] CopyTo with index=0 and the array is 4 items larger then size as the collection
            itemArray = GenerateArray(_items.Length + 4);
            tempItemsArray = new T[_items.Length + 4];

            Array.Copy(itemArray, tempItemsArray, itemArray.Length);
            Array.Copy(_items, 0, tempItemsArray, 0, _items.Length);
            _collection.CopyTo(itemArray, 0);

            VerifyCollection(itemArray, tempItemsArray, 0, tempItemsArray.Length);

            // [] CopyTo with index=4 and the array is 4 items larger then size as the collection
            itemArray = GenerateArray(_items.Length + 4);
            tempItemsArray = new T[_items.Length + 4];

            Array.Copy(itemArray, tempItemsArray, itemArray.Length);
            Array.Copy(_items, 0, tempItemsArray, 4, _items.Length);
            _collection.CopyTo(itemArray, 4);

            VerifyCollection(itemArray, tempItemsArray, 0, tempItemsArray.Length);

            // [] CopyTo with index=4 and the array is 8 items larger then size as the collection
            itemArray = GenerateArray(_items.Length + 8);
            tempItemsArray = new T[_items.Length + 8];

            Array.Copy(itemArray, tempItemsArray, itemArray.Length);
            Array.Copy(_items, 0, tempItemsArray, 4, _items.Length);
            _collection.CopyTo(itemArray, 4);

            VerifyCollection(itemArray, tempItemsArray, 0, tempItemsArray.Length);
        }

        internal void CopyTo_Tests_Negative()
        {
            T[] itemArray = null, tempItemsArray = null;
            //[] Verify CopyTo with null array

            Assert.Throws<ArgumentNullException>(() => _collection.CopyTo(null, 0)); //"Err_2470zsou: Exception not thrown with null array"

            // [] Verify CopyTo with index=Int32.MinValue
            itemArray = GenerateArray(_collection.Count);
            tempItemsArray = (T[])itemArray.Clone();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => _collection.CopyTo(new T[_collection.Count], Int32.MinValue)); //"Err_68971aehps: Exception not thrown with index=Int32.MinValue"

            //Verify that the array was not mutated 
            VerifyCollection(itemArray, tempItemsArray, 0, tempItemsArray.Length);

            // [] Verify CopyTo with index=-1
            itemArray = GenerateArray(_collection.Count);
            tempItemsArray = (T[])itemArray.Clone();

            Assert.Throws<ArgumentOutOfRangeException>(() => _collection.CopyTo(new T[_collection.Count], -1)); //"Err_3771zsiap: Exception not thrown with index=-1"

            //Verify that the array was not mutated 
            VerifyCollection(itemArray, tempItemsArray, 0, tempItemsArray.Length);

            // [] Verify CopyTo with index=Int32.MaxValue
            itemArray = GenerateArray(_collection.Count);
            tempItemsArray = (T[])itemArray.Clone();

            Assert.Throws<ArgumentOutOfRangeException>(() => _collection.CopyTo(new T[_collection.Count], Int32.MaxValue)); //"Err_39744ahps: Exception not thrown with index=Int32.MaxValue"

            //Verify that the array was not mutated 
            VerifyCollection(itemArray, tempItemsArray, 0, tempItemsArray.Length);

            // [] Verify CopyTo with index=array.length
            if (0 != _collection.Count)
            {
                itemArray = GenerateArray(_collection.Count);
                tempItemsArray = (T[])itemArray.Clone();

                Assert.Throws<ArgumentException>(() => _collection.CopyTo(new T[_collection.Count], _collection.Count)); //"Err_2078auoz: Exception not thow with index=array.Length"

                //Verify that the array was not mutated 
                VerifyCollection(itemArray, tempItemsArray, 0, tempItemsArray.Length);
            }

            if (1 < _items.Length)
            {
                // [] Verify CopyTo with collection.Count > array.length - index
                itemArray = GenerateArray(_collection.Count + 1);
                tempItemsArray = (T[])itemArray.Clone();
                Assert.Throws<ArgumentException>(() => _collection.CopyTo(new T[_collection.Count + 1], 2)); //"Err_1734nmzb: Correct exception not thrown with collection.Count > array.length - index"
            }

            //Verify that the array was not mutated 
            VerifyCollection(itemArray, tempItemsArray, 0, tempItemsArray.Length);
        }

        /// <summary>
        /// Runs all of the tests on Remove(Object).
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        internal void Remove_Tests()
        {
            T[] tempItems;

            tempItems = GenerateItems(32);

            //[] On an empty collection call Remove with some non null value
            _collection.Clear();

            _items = new T[0];
            Assert.False(_collection.Remove(_generateItem())); //"Err_4517487fjvdsa Expected Remove to return false on an empty collection"

            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] On an empty collection call Remove with a null value
            _collection.Clear();

            _items = new T[0];
            Assert.False(_collection.Remove(default(T))); //"Err_2117ejdhjed Expected Remove to return false on an empty collection"

            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] With an empty collection call Add with non null value and call Remove with null value

            _collection.Clear();
            for (int i = 0; i < tempItems.Length; ++i)
            {
                if (!tempItems[i].Equals(default(T)))
                {
                    _collection.Add(tempItems[i]);
                    _items = new T[1] { tempItems[i] };
                    break;
                }
            }

            Assert.False(_collection.Remove(default(T))); //"Err_12177ehdhaz Expected Remove to return false with defualt(T) when default(T) does not exist in the collection"

            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] On an empty collection call Add with null value and call Remove with null value
            _collection.Clear();
            _items = new T[0];

            _collection.Add(default(T));
            Assert.True(_collection.Remove(default(T))); //"Err_12017rjfvbuw Expected Remove to return true with defualt(T) when default(T) exists in the collection"

            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] On an empty collection call Add with null value and call Remove with non null value
            _collection.Clear();
            _items = new T[] { default(T) };

            _collection.Add(default(T));

            //[] With a collection that contains null somewhere in middle of the collection call Remove with null, 
            //some value that does not exist and with a value that does exist

            _collection.Clear();
            _items = new T[30];

            Array.Copy(tempItems, 0, _items, 0, 16);
            Array.Copy(tempItems, 17, _items, 16, 14);

            AddItemsToCollection(_collection, tempItems, 0, 16);
            _collection.Add(default(T));
            AddItemsToCollection(_collection, tempItems, 16, 15);

            Assert.True(_collection.Remove(tempItems[16])); //"Err_047akijedue Expected Remove to return true with an item that exists in the collection"
            Assert.True(_collection.Remove(default(T))); //"Err_741ahjeid Expected Remove to return true with defualt(T) when default(T) exists in the collection"
            Assert.False(_collection.Remove(tempItems[31])); //"Err_871521aide Expected Remove to return false with an item that does not exist in the collection"

            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] With a collection that contains null at the last item in the collection call Remove with null, 
            //some value that does not exist and with a value that does exist

            _collection.Clear();
            _items = new T[15];

            AddItemsToCollection(_collection, tempItems, 0, 16);
            Array.Copy(tempItems, 0, _items, 0, 11);
            Array.Copy(tempItems, 12, _items, 11, 4);

            _collection.Add(default(T));

            Assert.True(_collection.Remove(tempItems[11])); //"Err_87948aidued Expected Remove to return true with an item that exists in the collection"
            Assert.True(_collection.Remove(default(T))); //"Err_1001ajdhe Expected Remove to return true with defualt(T) when default(T) exists in the collection"
            Assert.False(_collection.Remove(tempItems[16])); //"Err_7141ajdhe Expected Remove to return false with an item that does not exist in the collection"

            VerifyCollection(_collection, _items, 0, _items.Length);
            //[] With an empty collection call Add with non null value and call Remove with that value

            _collection.Clear();
            for (int i = 0; i < tempItems.Length; ++i)
            {
                if (!tempItems[i].Equals(default(T)))
                {
                    _collection.Add(tempItems[i]);
                    _items = new T[1] { tempItems[i] };
                    break;
                }
            }

            Assert.True(_collection.Remove(_items[0])); //"Err_1201heud Expected Remove to return true with an item that exists in the collection"
            _items = new T[0];
            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] With an empty collection call Add with non null value and call Remove with some other value
            _collection.Clear();

            _collection.Add(tempItems[0]);
            _items = new T[1] { tempItems[0] };

            Assert.False(_collection.Remove(tempItems[1])); //"Err_0111ahdued Expected Remove to return false with an item that does not exist in the collection"
            VerifyCollection(_collection, _items, 0, _items.Length);

            Assert.False(_collection.Remove(tempItems[2])); //"Err_0477edjeyd Expected Remove to return false with an item that does not exist in the collection"
            VerifyCollection(_collection, _items, 0, _items.Length);

            //[] Call Remove with a value that exist twice in the collection
            _collection.Clear();

            AddItemsToCollection(_collection, tempItems, 0, tempItems.Length);
            AddItemsToCollection(_collection, tempItems, 0, tempItems.Length);

            for (int i = 0; i < tempItems.Length; i++)
            {
                int index = i + 1;
                _items = new T[tempItems.Length + tempItems.Length - index];

                Array.Copy(tempItems, index, _items, 0, 32 - index);
                Array.Copy(tempItems, 0, _items, 32 - index, 32);

                Assert.True(_collection.Remove(tempItems[i])); //"Err_788921dhfaz Expected Remove to return true with an item that exists in the collection"

                VerifyCollection(_collection, _items, 0, _items.Length);
            }

            //[] Call Remove with a value that does not exist in the collection
            _collection.Clear();

            AddItemsToCollection(_collection, tempItems, 0, 16);
            _items = new T[16];
            Array.Copy(tempItems, 0, _items, 0, 16);

            Assert.False(_collection.Remove(tempItems[16])); //"Err_95541ahdhe Expected Remove to return false with an item that does not exist in the collection"

            for (int i = 0; i < _items.Length; i++)
            {
                VerifyCollection(_collection, _items, i, _items.Length - i);

                Assert.True(_collection.Remove(_items[i])); //"Err_02214ahdye Expected Remove to return true with an item that exists in the collection"
                Assert.False(_collection.Remove(_items[i])); //"Err_1788aeijd Expected Remove to return false with an item that does not exist in the collection"
            }

            //[] With a collection that all of the _items in the collection are unique call Remove with every item
            _collection.Clear();

            _items = AddItemsToCollection(_collection, 16);

            for (int i = 0; i < _items.Length; i++)
            {
                Assert.True(_collection.Remove(_items[i])); //"Err_1217aejdu Expected Remove to return true with an item that exists in the collection"
                VerifyCollection(_collection, _items, i + 1, _items.Length - i - 1);
            }

            _items = new T[0];
        }

        /// <summary>
        /// Modifies the collection and checks the enumerator.
        /// </summary>
        internal void InvalidateEnumerator_Tests()
        {
            T item = _generateItem();

            //[] Clear()
            _collection.Clear();
            _items = new T[] { _generateItem() };
            _collection.Add(_items[0]);
            VerifyModifiedEnumerator(_collection, false, _items.Length <= 1, () => _collection.Clear());

            //[] Add(T item)
            _collection.Clear();
            _items = new T[] { _generateItem() };
            _collection.Add(_items[0]);
            VerifyModifiedEnumerator(_collection, true, _items.Length <= 1, () => _collection.Add(item));

            T[] temp = new T[_items.Length + 1];
            Array.Copy(_items, temp, _items.Length);
            temp[_items.Length] = item;
            _items = temp;

            //[] Remove(TKey key)
            _collection.Clear();
            _items = AddItemsToCollection(_collection, 2);
            VerifyModifiedEnumerator(_collection, true, _items.Length <= 1, () => _collection.Remove(_items[1]));
            _items = new T[] { _items[0] };
        }

        /// <summary>
        /// Runs all of the tests on the IEnumerable members.
        /// </summary>
        internal void IEnumerable_Tests()
        {
            //[] Verify Count returns the expected value
            Assert.Equal(_items.Length, _collection.Count); //"Err_6487pqtw Count"

            //[] Verify IsReadOnly returns the expected value
            Assert.False(_collection.IsReadOnly); //"Err_39478pks IsReadOnly"

            T[] tempItems;
            //[] With a empty collection run all IEnumerable tests

            _collection.Clear();
            _items = new T[0];

            VerifyGenericEnumerator(_collection, _items);

            // Verifying the linked list enumerator specifically.
            VerifyLinkedListEnumerator((LinkedList<T>)_collection, _items);

            //[] Add some _items then run all IEnumerable tests

            _collection.Clear();
            _items = AddItemsToCollection(_collection, 32);

            VerifyGenericEnumerator(_collection, _items);

            // Verifying the linked list enumerator specifically.
            VerifyLinkedListEnumerator((LinkedList<T>)_collection, _items);

            //[] Add some _items then Remove some and run all IEnumerable tests
            _items = new T[14];
            _collection.Clear();

            tempItems = AddItemsToCollection(_collection, 32);
            Array.Copy(tempItems, 1, _items, 0, 7);
            Array.Copy(tempItems, 24, _items, 7, 7);

            for (int i = 8; i < 24; i++)
            {
                _collection.Remove(tempItems[i]);
            }

            //[] Add some _items then call Clear run all IEnumerable tests
            _collection.Clear();
            _items = new T[0];

            AddItemsToCollection(_collection, 32);
            _collection.Clear();

            VerifyGenericEnumerator(_collection, _items);

            // Verifying the linked list enumerator specifically.
            VerifyLinkedListEnumerator((LinkedList<T>)_collection, _items);

            //[] With a collection containing null values run all IEnumerable tests
            _items = new T[17];
            _collection.Clear();

            tempItems = AddItemsToCollection(_collection, 16);
            Array.Copy(tempItems, 0, _items, 0, 16);

            _collection.Add(default(T));
            _items[16] = default(T);

            VerifyGenericEnumerator(_collection, _items);
            // Verifying the linked list enumerator specifically.
            VerifyLinkedListEnumerator((LinkedList<T>)_collection, _items);

            //[] With a collection containing duplicates run all IEnumerable tests
            _items = new T[32];
            _collection.Clear();

            tempItems = AddItemsToCollection(_collection, 16);
            Array.Copy(tempItems, 0, _items, 0, 16);
            Array.Copy(tempItems, 0, _items, 16, 16);

            AddItemsToCollection(_collection, tempItems, 0, tempItems.Length);

            VerifyGenericEnumerator(_collection, _items);

            // Verifying the linked list enumerator specifically.
            VerifyLinkedListEnumerator((LinkedList<T>)_collection, _items);
        }

        /// <summary>
        /// Sets the collection back to the original state so that it only 
        /// contains items in OriginalItems and verifies this state.
        /// </summary>
        internal void SetOriginalItems_Tests()
        {
            //[] Set all of the items in the collection back to the original items
            _collection.Clear();
            AddItemsToCollection(_collection, _originalItems, 0, _originalItems.Length);
            _items = (T[])_originalItems.Clone();

            VerifyCollection(_collection, _originalItems, 0, _originalItems.Length);
        }

        #region Helper Methods

        private T[] GenerateItems(int numItems)
        {
            if (_generatedItems.Length < numItems)
            {
                T[] tempItems = new T[numItems];
                Array.Copy(_generatedItems, 0, tempItems, 0, _generatedItems.Length);
                int index = _generatedItems.Length;
                T value;

                for (; index < numItems; ++index)
                {
                    do
                    {
                        value = _generateItem();
                    } while (!IsUniqueItem(tempItems, value));

                    tempItems[index] = value;
                }

                _generatedItems = tempItems;
            }

            T[] items = new T[numItems];
            Array.Copy(_generatedItems, 0, items, 0, numItems);

            return items;
        }

        private T[] GenerateArray(int length)
        {
            T[] items = new T[length];

            if (null != _generateItem)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = _generateItem();
                }
            }

            return items;
        }

        private T[] AddItemsToCollection(ICollection<T> collection, int numItems)
        {
            T[] items = GenerateItems(numItems);
            AddItemsToCollection(collection, items, 0, items.Length);
            return items;
        }

        private void AddItemsToCollection(ICollection<T> collection, T[] items, int index, int count)
        {
            count += index;
            for (int i = index; i < count; i++)
                collection.Add(items[i]);
        }

        private void VerifyCollection(ICollection<T> collection, T[] items, int index, int count)
        {
            Assert.Equal(collection.Count, count); //"Collection and given count should have the same length"

            // verify contains.
            for (int i = 0; i < count; i++)
            {
                int itemsIndex = i + index;
                Assert.True(collection.Contains(items[itemsIndex]),
                    "Err_1634pnyan Verifying Contains and expected item" + items[itemsIndex] + "to be in the colleciton. Index: " + itemsIndex);
            }

            //verify copy_to
            T[] actualItems = new T[count];
            collection.CopyTo(actualItems, 0);

            // verifies the contents of the copy_to.
            ArraySegment<T> actualSegment = new ArraySegment<T>(actualItems, 0, count);
            ArraySegment<T> expectedSegment = new ArraySegment<T>(items, index, count);
            int actualItemsCount = actualSegment.Offset + actualSegment.Count;
            int expectedItemsCount = expectedSegment.Offset + expectedSegment.Count;
            int actualItemsIndex = actualSegment.Offset;
            int expectedItemsIndex = expectedSegment.Offset;
            Assert.Equal(expectedSegment.Count, actualSegment.Count); //"Err_1707ahps The length of the actual items and the expected items differ"
            for (; expectedItemsIndex < expectedItemsCount; ++actualItemsIndex, ++expectedItemsIndex)
            {
                Assert.Equal(actualSegment.Array[actualItemsIndex], expectedSegment.Array[expectedItemsIndex]); //"Err_0722haps The actual item and expected items differ at index" + expectedItemsIndex
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
                catch (InvalidOperationException) { }
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
                catch (InvalidOperationException) { }
            }

            enumerator.Dispose();
        }

        private void VerifyLinkedListEnumerator(LinkedList<T> collection, T[] expectedItems)
        {
            LinkedList<T>.Enumerator enumerator = collection.GetEnumerator();
            int iterations = 0;
            int expectedCount = expectedItems.Length;

            //[] Verify non deterministic behavior of current every time it is called before a call to MoveNext() has been made			
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    T tempCurrent = enumerator.Current;
                }
                catch (InvalidOperationException) { }
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
                catch (InvalidOperationException) { }
            }

            enumerator.Dispose();
        }

        private void VerifyModifiedEnumerator(ICollection<T> collection, bool verifyCurrent, bool atEnd, Action modifyCollection)
        {
            IEnumerator<T> genericIEnumerator = ((IEnumerable<T>)collection).GetEnumerator();
            IEnumerator iEnumerator = ((IEnumerable)collection).GetEnumerator();
            T current = default(T);
            Object objCurrent = null;

            genericIEnumerator.MoveNext();
            iEnumerator.MoveNext();

            if (verifyCurrent)
            {
                current = genericIEnumerator.Current;
                objCurrent = iEnumerator.Current;
            }

            modifyCollection();

            //[] Current
            if (verifyCurrent)
                Assert.Equal(current, genericIEnumerator.Current); //"Err_05188aied IEnumerator<T>.Current"

            bool _moveNextAtEndThrowsOnModifiedCollection = true;
            //[] MoveNext
            if (!atEnd || _moveNextAtEndThrowsOnModifiedCollection)
            {
                try
                {
                    genericIEnumerator.MoveNext();
                    Assert.True(false); //"Err_5048ajed IEnumerator<T>.MoveNext()"
                }
                catch (InvalidOperationException) { }
                try
                {
                    iEnumerator.MoveNext();
                    Assert.True(false); //"Err_548aied IEnumerator.MoveNext()"
                }
                catch (InvalidOperationException) { }
            }

            //[] Reset
            try
            {
                genericIEnumerator.Reset();
                Assert.True(false); //"Err_6412aied IEnumerator<T>.Reset()"
            }
            catch (InvalidOperationException) { }
            try
            {
                iEnumerator.Reset();
                Assert.True(false); //"Err_0215aheiud IEnumerator.Reset()"
            }
            catch (InvalidOperationException) { }
        }

        private bool IsUniqueItem(T[] items, T item)
        {
            for (int i = 0; i < items.Length; ++i)
            {
                if (items[i].Equals(item))
                    return false;
            }

            return true;
        }

        #endregion
    }
}