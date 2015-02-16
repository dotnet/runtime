// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Tests.HashSet_HashSetTestSupport;

namespace Tests
{
    namespace HashSet_HashSetTestSupport
    {
        public class HashSetTestSupport
        {
            public static void HashSetContains(HashSet<IEnumerable>[] actual, HashSet<IEnumerable>[] expected)
            {
                Assert.Null(HashSetContainsSupersets(actual, expected)); //"Error: actual result doesn't contain an expected item"
                Assert.Null(HashSetContainsSupersets(expected, actual)); //"Error: actual result contains an unexpected item"
            }

            public static HashSet<IEnumerable> HashSetContainsSupersets(HashSet<IEnumerable>[] search, HashSet<IEnumerable>[] items)
            {
                IEnumerableEqualityComparer comparer = new IEnumerableEqualityComparer();

                foreach (HashSet<IEnumerable> item in items)
                {
                    bool found = false;

                    foreach (HashSet<IEnumerable> isit in search)
                    {
                        bool couldbe = true;

                        foreach (IEnumerable subitem in item)
                        {
                            bool subfound = false;

                            foreach (IEnumerable subisit in isit)
                            {
                                if (comparer.Equals(subitem, subisit))
                                {
                                    subfound = true;
                                    break;
                                }
                            }

                            if (!subfound)
                            {
                                couldbe = false;
                                break;
                            }
                        }

                        if (couldbe)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        return item;
                }

                return null;
            }
        }
        public class HashSetTestSupport<T>
        {
            #region Member Variables

            private ICollection<T> _collection;

            private T[] _items;

            private readonly T[] _originalItems;

            private readonly Func<T> _generateItem;

            private readonly IEqualityComparer<T> _comparer;

            #endregion

            public HashSetTestSupport(HashSet<T> collection, Func<T> generateItem, T[] items)
                : this(collection, generateItem, items, EqualityComparer<T>.Default)
            { }

            public HashSetTestSupport(HashSet<T> collection, Func<T> generateItem, T[] items, IEqualityComparer<T> comparer)
            {
                _collection = collection;
                _generateItem = generateItem;
                _items = items;
                _originalItems = (T[])_items.Clone();
                _comparer = comparer;
            }

            /// <summary>
            /// Validates that the hashset is the same and performs other ICollection tests.
            /// </summary>
            public void VerifyHashSetTests()
            {
                VerifyHashSet((HashSet<T>)_collection, _items, _comparer);

                InitialItems_Tests();
                Count_Tests();
                IsReadOnly_Tests();
                Add_Tests();
                Clear_Tests();
                Contains_Tests();
                CopyTo_Tests();
                Remove_Tests();
                IEnumerable_Tests();
            }

            /// <summary>
            /// Performs the negative ICollection tests on the HashSet.
            /// </summary>
            public void VerifyHashSet_NegativeTests()
            {
                CopyTo_Tests_Negative();
                InvalidateEnumerator_Tests_Negative();
            }

            /// <summary>
            /// Performs simple validation to make sure both sets are the same.
            /// </summary>
            public static void VerifyHashSet(HashSet<T> set, ICollection<T> contents, IEqualityComparer<T> comparerToUse)
            {
                // Comparing the comparers given.
                Assert.Equal(set.Comparer, comparerToUse); //"Error in resulting HashSet: comparer does not match"
                                                           //Comparing the set contents.

                var setComparer = new HashSetComparer<T>(comparerToUse);
                Assert.True(setComparer.Equals(contents, set)); //"Expected both sets to be the same."
            }

            #region Private Test Methods

            private void InitialItems_Tests()
            {
                //[] Verify the initial items in the collection
                VerifyCollection(_collection, _items, 0, _items.Length);
            }

            /// <summary>
            /// Runs all of the tests on Count.
            /// </summary>
            private void Count_Tests()
            {
                //[] Verify Count returns the expected value
                Assert.Equal(_items.Length, _collection.Count); //"Err_6487pqtw Count"
            }

            /// <summary>
            /// Runs all of the tests on IsReadOnly.
            /// </summary>
            private void IsReadOnly_Tests()
            {
                Assert.False(_collection.IsReadOnly); //"Err_39478pks IsReadOnly"
            }

            /// <summary>
            /// Runs all of the tests on Add(Object).
            /// </summary>
            private void Add_Tests()
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
                    _collection.Remove(tempItems[i]);

                _items = AddItemsToCollection(_collection, 16);

                VerifyCollection(_collection, _items, 0, _items.Length);

                //[] Add duplicate value
                _collection.Clear();
                _items = AddItemsToCollection(_collection, 16);
                _collection.Add(_items[0]); //won't get put into the set, so should be same still.
                VerifyCollection(_collection, _items, 0, _items.Length);
            }

            /// <summary>
            /// Runs all of the tests on Clear().
            /// </summary>
            private void Clear_Tests()
            {
                //[] Call Clear on an empty collection then Add some _items
                _collection.Clear();
                _items = AddItemsToCollection(_collection, 16);

                VerifyCollection(_collection, _items, 0, _items.Length);

                //[] Call Clear several time on a collection with some items alread... then add some _items
                _collection.Clear();
                Assert.Equal(0, _collection.Count); //"Should not have any items in the collection."
                foreach (var item in _collection)
                    Assert.True(false); //"Should not be able to iterate over a collection that has been cleared."

                _collection.Clear();
                _collection.Clear();
                Assert.Equal(0, _collection.Count); //"Should not have any items in the collection."
                foreach (var item in _collection)
                    Assert.True(false); //"Should not be able to iterate over a collection that has been cleared."

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
            private void Contains_Tests()
            {
                T[] tempItems;

                //[] On an empty collection call Contains with some non null value

                _collection.Clear();

                Assert.False(_collection.Contains(_generateItem())); //"Err_17081 Contains returned true with non null T on an empty collection"

                //[] With an empty collection call Add with non null value and call Contains with that value
                _collection.Clear();
                _items = AddItemsToCollection(_collection, 1);

                Assert.True(_collection.Contains(_items[0]),
                       "Err_23198ahsi Contains returned false with value that was added to an empty collection");

                //[] With an empty collection call Add with non null value and call Contains with some other value

                _collection.Clear();
                _items = AddItemsToCollection(_collection, 1);

                //Here we depend on _generateItem returning a unique item that is not already int he collection
                Assert.False(_collection.Contains(_generateItem()),
                         "Err_2589lpzi Contains returned false with value other then the value that was added to an empty collection");

                //[] With an empty collection call Add with non null value and call Contains with null value

                _collection.Clear();
                _items = AddItemsToCollection(_collection, 1);

                // TODO: This will fail if default(T) (0 for int) is in the collection
                Assert.False(_collection.Contains(default(T)),
                    "Err_3294sjpd Contains returned false with null value on an empty collection with one item added to it");

                //[] On an empty collection call Contains with a null value

                _collection.Clear();

                Assert.False(_collection.Contains(default(T))); //"Err_6149ajpqn Contains returned true with null value on an empty collection"

                //[] On an empty collection call Add with null value and call Contains with null value

                _collection.Clear();
                _collection.Add(default(T));

                Assert.True(_collection.Contains(default(T)),
                     "Err_5586pahl Contains returned false with null value on an empty collection with null added to it");

                //[] With a collection that contains null somewhere in middle of the collection call Conatains with null, some value that does not exist and with a value that does exist

                _collection.Clear();
                _items = new T[33];

                tempItems = GenerateItems(32);
                Array.Copy(tempItems, 0, _items, 0, 16);
                _items[16] = default(T);
                Array.Copy(tempItems, 16, _items, 17, 16);

                AddItemsToCollection(_collection, tempItems, 0, 16);
                _collection.Add(default(T));
                AddItemsToCollection(_collection, tempItems, 16, 16);

                for (int i = 0; i < _items.Length; i++)
                {
                    Assert.True(_collection.Contains(_items[i])); //"Err_3697ahsm Contains returned false with " + _items[i] + " expected true"
                }

                Assert.False(_collection.Contains(_generateItem())); //"Err_1259yhpa Contains returned true with unique T not in the collection expected false"

                //[] With a collection that contains null at the last item in the collection call Conatains with null, some value that does not exist and with a value that does exist

                _collection.Clear();
                _items = new T[17];

                tempItems = AddItemsToCollection(_collection, 16);
                Array.Copy(tempItems, 0, _items, 0, 16);

                _collection.Add(default(T));
                _items[16] = default(T);

                for (int i = 0; i < _items.Length; i++)
                {
                    Assert.True(_collection.Contains(_items[i])); //"Err_1238hspo Contains returned false with " + _items[i] + " expected true"
                }

                Assert.False(_collection.Contains(_generateItem())); //"Err_31289snos Contains returned true with new T() expected false"

                //[] With a collection that all of the _items in the collection are unique call Contains with every item

                _collection.Clear();
                _items = AddItemsToCollection(_collection, 32);

                for (int i = 0; i < _items.Length; i++)
                    Assert.True(_collection.Contains(_items[i])); //"65943ahps Contains returned false with index: " + i + " expected true"

                //Here we depend on _generateItem returning a unique item that is not already int he collection
                Assert.False(_collection.Contains(_generateItem())); //"Err_9446haos Contains returned true with unique T not in the collection expected false"
            }

            /// <summary>
            /// Runs all of the tests on CopyTo(Array).
            /// </summary>
            private void CopyTo_Tests()
            {
                T[] itemArray = null, tempItemsArray = null;

                // [] CopyTo with index=0 and the array is the same size as the collection
                itemArray = GenerateItems(_items.Length);

                _collection.CopyTo(itemArray, 0);

                VerifyItems(new ArraySegment<T>(itemArray, 0, itemArray.Length), new ArraySegment<T>(_items, 0, _items.Length));

                // [] CopyTo with index=0 and the array is 4 items larger then size as the collection
                itemArray = GenerateItems(_items.Length + 4);
                tempItemsArray = new T[_items.Length + 4];

                Array.Copy(itemArray, tempItemsArray, itemArray.Length);
                Array.Copy(_items, 0, tempItemsArray, 0, _items.Length);
                _collection.CopyTo(itemArray, 0);

                VerifyItems(new ArraySegment<T>(itemArray, 0, itemArray.Length), new ArraySegment<T>(tempItemsArray, 0, tempItemsArray.Length));

                // [] CopyTo with index=4 and the array is 4 items larger then size as the collection
                itemArray = GenerateItems(_items.Length + 4);
                tempItemsArray = new T[_items.Length + 4];

                Array.Copy(itemArray, tempItemsArray, itemArray.Length);
                Array.Copy(_items, 0, tempItemsArray, 4, _items.Length);
                _collection.CopyTo(itemArray, 4);

                VerifyItems(new ArraySegment<T>(itemArray, 0, itemArray.Length), new ArraySegment<T>(tempItemsArray, 0, tempItemsArray.Length));

                // [] CopyTo with index=4 and the array is 8 items larger then size as the collection
                itemArray = GenerateItems(_items.Length + 8);
                tempItemsArray = new T[_items.Length + 8];

                Array.Copy(itemArray, tempItemsArray, itemArray.Length);
                Array.Copy(_items, 0, tempItemsArray, 4, _items.Length);
                _collection.CopyTo(itemArray, 4);

                VerifyItems(new ArraySegment<T>(itemArray, 0, itemArray.Length), new ArraySegment<T>(tempItemsArray, 0, tempItemsArray.Length));
            }

            private void CopyTo_Tests_Negative()
            {
                //[] Verify CopyTo with null array
                Assert.Throws<ArgumentNullException>(() => { _collection.CopyTo(null, 0); }); //"Err_2470zsou: Exception not thrown with null array"

                // [] Verify CopyTo with index=Int32.MinValue
                T[] itemArray = GenerateItems(_collection.Count);
                T[] tempItemsArray = (T[])itemArray.Clone();

                Assert.Throws<ArgumentOutOfRangeException>(
                    () => { _collection.CopyTo(new T[_collection.Count], Int32.MinValue); }); //"Err_68971aehps: Exception not thrown with index=Int32.MinValue"

                //Verify that the array was not mutated 
                VerifyItems(new ArraySegment<T>(itemArray, 0, itemArray.Length), new ArraySegment<T>(tempItemsArray, 0, tempItemsArray.Length));

                // [] Verify CopyTo with index=-1
                itemArray = GenerateItems(_collection.Count);
                tempItemsArray = (T[])itemArray.Clone();

                Assert.Throws<ArgumentOutOfRangeException>(() => { _collection.CopyTo(new T[_collection.Count], -1); }); //"Err_3771zsiap: Exception not thrown with index=-1"

                //Verify that the array was not mutated 
                VerifyItems(new ArraySegment<T>(itemArray, 0, itemArray.Length), new ArraySegment<T>(tempItemsArray, 0, tempItemsArray.Length));
                // [] Verify CopyTo with index=Int32.MaxValue
                itemArray = GenerateItems(_collection.Count);
                tempItemsArray = (T[])itemArray.Clone();

                Assert.Throws<ArgumentOutOfRangeException>(() => { _collection.CopyTo(new T[_collection.Count], Int32.MinValue); }); //"Err_39744ahps: Exception not thrown with index=Int32.MaxValue"

                //Verify that the array was not mutated 
                VerifyItems(new ArraySegment<T>(itemArray, 0, itemArray.Length), new ArraySegment<T>(tempItemsArray, 0, tempItemsArray.Length));

                // [] Verify CopyTo with index=array.length
                if (0 != _collection.Count)
                {
                    itemArray = GenerateItems(_collection.Count);
                    tempItemsArray = (T[])itemArray.Clone();

                    Assert.Throws<ArgumentException>(
                        () => { _collection.CopyTo(new T[_collection.Count], _collection.Count); }); //"Err_2078auoz: Exception not thow with index=array.Length"

                    //Verify that the array was not mutated 
                    VerifyItems(new ArraySegment<T>(itemArray, 0, itemArray.Length), new ArraySegment<T>(tempItemsArray, 0, tempItemsArray.Length));
                }

                if (1 < _items.Length)
                {
                    // [] Verify CopyTo with collection.Count > array.length - index
                    itemArray = GenerateItems(_collection.Count + 1);
                    tempItemsArray = (T[])itemArray.Clone();

                    Assert.Throws<ArgumentException>(() => { _collection.CopyTo(new T[_collection.Count + 1], 2); }); //"Err_1734nmzb: Correct exception not thrown with collection.Count > array.length - index"
                }

                //Verify that the array was not mutated 
                VerifyItems(new ArraySegment<T>(itemArray, 0, itemArray.Length), new ArraySegment<T>(tempItemsArray, 0, tempItemsArray.Length));
            }

            /// <summary>
            /// Runs all of the tests on Remove(Object).
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            private void Remove_Tests()
            {
                T[] tempItems;

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

                _items = AddItemsToCollection(_collection, 1);

                // TODO: This will fail if default(T) (0 for int) is in the collection
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

                _items = new T[31];

                tempItems = GenerateItems(32);
                Array.Copy(tempItems, 0, _items, 0, 16);
                Array.Copy(tempItems, 17, _items, 16, 15);

                AddItemsToCollection(_collection, tempItems, 0, 16);
                _collection.Add(default(T));
                AddItemsToCollection(_collection, tempItems, 16, 16);

                Assert.True(_collection.Remove(tempItems[16])); //"Err_047akijedue Expected Remove to return true with an item that exists in the collection"
                                                                // TODO: This will fail if default(T) (0 for int) exists in the collection somewhere else other then where we insterted it
                Assert.True(_collection.Remove(default(T))); //"Err_741ahjeid Expected Remove to return true with defualt(T) when default(T) exists in the collection"
                Assert.False(_collection.Remove(_generateItem())); //"Err_871521aide Expected Remove to return false with an item that does not exist in the collection"

                VerifyCollection(_collection, _items, 0, _items.Length);

                //[] With a collection that contains null at the last item in the collection call Remove with null, 
                //some value that does not exist and with a value that does exist
                _collection.Clear();
                _items = new T[15];

                tempItems = AddItemsToCollection(_collection, 16);
                Array.Copy(tempItems, 0, _items, 0, 11);
                Array.Copy(tempItems, 12, _items, 11, 4);

                _collection.Add(default(T));

                Assert.True(_collection.Remove(tempItems[11])); //"Err_87948aidued Expected Remove to return true with an item that exists in the collection"
                                                                // TODO: This will fail if default(T) (0 for int) is in the collection
                Assert.True(_collection.Remove(default(T))); //"Err_1001ajdhe Expected Remove to return true with defualt(T) when default(T) exists in the collection"
                Assert.False(_collection.Remove(_generateItem())); //"Err_7141ajdhe Expected Remove to return false with an item that does not exist in the collection"

                VerifyCollection(_collection, _items, 0, _items.Length);

                //[] With an empty collection call Add with non null value and call Remove with that value
                _collection.Clear();
                _items = new T[0];

                tempItems = AddItemsToCollection(_collection, 1);
                Assert.True(_collection.Remove(tempItems[0])); //"Err_1201heud Expected Remove to return true with an item that exists in the collection"

                VerifyCollection(_collection, _items, 0, _items.Length);

                //[] With an empty collection call Add with non null value and call Remove with some other value
                _collection.Clear();

                _items = AddItemsToCollection(_collection, 1);

                //Here we depend on _generateItem returning a unique item that is not already int he collection
                Assert.False(_collection.Remove(_generateItem())); //"Err_0111ahdued Expected Remove to return false with an item that does not exist in the collection"

                VerifyCollection(_collection, _items, 0, _items.Length);

                // TODO: This will fail if _generateItem returns default(T) (0 for int)
                Assert.False(_collection.Remove(_generateItem())); //"Err_0477edjeyd Expected Remove to return false with an item that does not exist in the collection"

                VerifyCollection(_collection, _items, 0, _items.Length);

                //[] Call Remove with a value that does not exist in the collection
                _collection.Clear();

                _items = AddItemsToCollection(_collection, 16);
                //Here we depend on _generateItem returning a unique item that is not already int he collection
                Assert.False(_collection.Remove(_generateItem())); //"Err_95541ahdhe Expected Remove to return false with an item that does not exist in the collection"

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

            private void InvalidateEnumerator_Tests_Negative()
            {
                T item = _generateItem();

                //[] Clear()
                VerifyModifiedEnumerator(_collection, false, () => { _collection.Clear(); });
                _items = new T[] { _generateItem() };
                _collection.Add(_items[0]);

                //[] Add(T item)
                VerifyModifiedEnumerator(_collection, true, () => { _collection.Add(item); });

                T[] tempArray = new T[_items.Length + 1];
                Array.Copy(_items, 0, tempArray, 0, _items.Length);
                tempArray[_items.Length] = item;
                _items = tempArray;

                //[] Remove(TKey key)
                VerifyModifiedEnumerator(_collection, true, () => { _collection.Remove(item); });
                tempArray = new T[_items.Length - 1];
                Array.Copy(_items, 0, tempArray, 0, _items.Length - 1);
                _items = tempArray;
            }

            /// <summary>
            /// Runs all of the tests on the IEnumerable members.
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            private void IEnumerable_Tests()
            {
                T[] tempItems;

                //[] With a empty collection run all IEnumerable tests

                _collection.Clear();
                _items = new T[0];

                VerifyEnumerator(_collection.GetEnumerator(), _items, 0, _items.Length);

                //[] Add some _items then run all IEnumerable tests

                _collection.Clear();
                _items = AddItemsToCollection(_collection, 32);

                VerifyEnumerator(_collection.GetEnumerator(), _items, 0, _items.Length);

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

                VerifyEnumerator(_collection.GetEnumerator(), _items, 0, _items.Length);

                //[] With a collection containing null values run all IEnumerable tests
                _items = new T[17];
                _collection.Clear();

                tempItems = AddItemsToCollection(_collection, 16);
                Array.Copy(tempItems, 0, _items, 0, 16);

                _collection.Add(default(T));
                _items[16] = default(T);

                VerifyEnumerator(_collection.GetEnumerator(), _items, 0, _items.Length);
            }

            #endregion

            #region Helper Methods

            private T[] ModifyICollection(IEnumerable<T> collection, T[] expectedItems)
            {
                ICollection<T> iCollection = (ICollection<T>)collection;
                T tempItem = _generateItem();

                iCollection.Add(tempItem);
                iCollection.Remove(tempItem);
                return expectedItems;
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
                // Verifying Count.
                Assert.Equal(count, collection.Count); //"Err_255087aiedaed Verifying Count"

                // Verifying that CopyTo works.
                for (int i = 0; i < count; i++)
                {
                    int itemsIndex = i + index;
                    Assert.True(collection.Contains(items[itemsIndex]),
                        "Err_1634pnyan Verifying Contains and expected item " + items[itemsIndex] + " to be in the colleciton");
                }

                VerifyEnumerator(collection.GetEnumerator(), items, index, count);
            }

            private void VerifyEnumerator(IEnumerator<T> enumerator, T[] expectedItems, int startIndex, int count)
            {
                int iterations = 0;

                //[] Verify non deterministic behavior of current every time it is called before a call to MoveNext() has been made			
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        T tempCurrent = enumerator.Current;
                    }
                    catch (Exception) { }
                }

                List<Boolean> itemsVisited = new List<Boolean>();
                bool itemFound;
                int size = count;

                for (int i = 0; i < size; ++i) itemsVisited.Add(false);

                while ((iterations < count) && enumerator.MoveNext())
                {
                    T currentItem = enumerator.Current;
                    T tempItem;

                    //[] Verify we have not gotten more items then we expected
                    Assert.True(iterations < count, "Err_9844awpa More items have been returned fromt the enumerator(" + iterations + "items) then are " +
                            "in the expectedElements(" + count + "items)");

                    //[] Verify Current returned the correct value
                    itemFound = false;

                    for (int i = 0; i < itemsVisited.Count; ++i)
                    {
                        var expected = expectedItems[startIndex + i];
                        bool areEqual = _comparer.Equals(expected, currentItem);
                        //bool areEqual = (expected == null && currentItem == null) || (expected != null && expected.Equals(currentItem));
                        if (!itemsVisited[i] && areEqual)
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
                        Assert.Equal(currentItem, tempItem); //"Err_8776phaw Current is returning inconsistant results Current"
                    }

                    iterations++;
                }

                for (int i = 0; i < count; ++i)
                {
                    Assert.True(itemsVisited[i], "Err_052848ahiedoi Expected Current to return:" + expectedItems[startIndex + i]);
                }

                Assert.Equal(count, iterations); //"Err_658805eauz Number of items to iterate through"

                for (int i = 0; i < 3; i++)
                {
                    Assert.False(enumerator.MoveNext()); //"Err_2929ahiea Expected MoveNext to return false after " + iterations + " iterations"
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
            }

            private void VerifyItems(ArraySegment<T> actualItems, ArraySegment<T> expectedItems)
            {
                int actualItemsCount = actualItems.Offset + actualItems.Count;
                int expectedItemsCount = expectedItems.Offset + expectedItems.Count;
                int actualItemsIndex = actualItems.Offset;
                int expectedItemsIndex = expectedItems.Offset;

                Assert.Equal(expectedItems.Count, actualItems.Count); //"Err_1707ahps The length of the actual items and the expected items differ"
                List<Boolean> itemsVisited = new List<Boolean>();
                bool itemFound;

                for (int i = 0; i < expectedItems.Count; ++i) itemsVisited.Add(false);

                for (; actualItemsIndex < actualItemsCount; ++actualItemsIndex)
                {
                    itemFound = false;
                    expectedItemsIndex = expectedItems.Offset;

                    for (int i = 0; expectedItemsIndex < expectedItemsCount; ++i, ++expectedItemsIndex)
                    {
                        var expected = expectedItems.Array[expectedItemsIndex];
                        var actual = actualItems.Array[actualItemsIndex];
                        bool areEqual = _comparer.Equals(expected, actual);
                        //bool areEqual = (expected == null && actual == null) || (expected != null && expected.Equals(actual));
                        if (!itemsVisited[i] && areEqual)
                        {
                            itemsVisited[i] = true;
                            itemFound = true;
                            break;
                        }
                    }
                    Assert.True(itemFound); //"Err_02184aied Did not find=" + actualItems.Array[actualItemsIndex] + " in expected"
                }

                expectedItemsIndex = expectedItems.Offset;
                for (int i = 0; expectedItemsIndex < expectedItemsCount; ++i, ++expectedItemsIndex)
                {
                    Assert.True(itemsVisited[i]); //"Err_0515648aieid Expected to find=" + expectedItems.Array[expectedItemsIndex] + " in actual"
                }
            }

            private void VerifyModifiedEnumerator(ICollection<T> collection, bool verifyCurrent, Action modifyCollection)
            {
                IEnumerator<T> genericIEnumerator = ((IEnumerable<T>)collection).GetEnumerator();
                System.Collections.IEnumerator iEnumerator = ((System.Collections.IEnumerable)collection).GetEnumerator();
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
                    Assert.True(_comparer.Equals(current, genericIEnumerator.Current)); //"Err_05188aied IEnumerator<T>.Current"

                //[] MoveNext
                Assert.Throws<InvalidOperationException>(() => { genericIEnumerator.MoveNext(); }); //"Err_5048ajed IEnumerator<T>.MoveNext()"
                Assert.Throws<InvalidOperationException>(() => { iEnumerator.MoveNext(); }); //"Err_548aied IEnumerator.MoveNext()"

                //[] Reset
                Assert.Throws<InvalidOperationException>(() => { genericIEnumerator.Reset(); }); //"Err_6412aied IEnumerator<T>.Reset()"
                Assert.Throws<InvalidOperationException>(() => { iEnumerator.Reset(); }); //"Err_0215aheiud IEnumerator.Reset()"
            }

            private T[] _generatedItems = new T[0];
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

            private bool IsUniqueItem(T[] items, T item)
            {
                for (int i = 0; i < items.Length; ++i)
                {
                    if (_comparer.Equals(item, items[i]))
                        return false;
                    //if (items[i] == null && item == null)
                    //    return false;
                    //if (items[i] != null && items[i].Equals(item))
                    //    return false;
                }

                return true;
            }

            #endregion
        }
        #region Helper Classes

        public class Item
        {
            public int x;

            private static int s_nextIndex = 1;
            private static object s_nextIndexLock = new object();

            public Item(int y)
            {
                x = y;
            }

            public static Item GenerateNext()
            {
                int current;

                lock (s_nextIndexLock)
                {
                    current = s_nextIndex;
                    s_nextIndex++;
                }

                return new Item(current);
            }
        }
        public class ItemEqualityComparer : IEqualityComparer<Item>
        {
            public bool Equals(Item x, Item y)
            {
                if ((x != null) & (y != null))
                    return (x.x == y.x);
                else
                    return ((y == null) & (x == null));
            }

            public int GetHashCode(Item x)
            {
                if (x == null) return 0;
                return (x.x);
            }
        }
        public class ValueItem : IEquatable<ValueItem>, IEnumerable<ValueItem>, IEnumerable
        {
            public int x;
            public int y;

            private static int s_nextIndex = 1;
            private static object s_nextIndexLock = new object();

            public ValueItem(int x2, int y2)
            {
                x = x2;
                y = y2;
            }

            public static ValueItem GenerateNext()
            {
                int xVal, yVal;

                lock (s_nextIndexLock)
                {
                    xVal = s_nextIndex;
                    s_nextIndex++;
                    yVal = s_nextIndex;
                    s_nextIndex++;
                }

                return new ValueItem(xVal, yVal);
            }

            public bool Equals(ValueItem other)
            {
                return (x == other.x);
            }

            public override int GetHashCode()
            {
                return x.GetHashCode();
            }

            public override bool Equals(Object obj)
            {
                ValueItem v = obj as ValueItem;
                if (v == null) return false;

                return this.Equals(v);
            }

            public IEnumerator<ValueItem> GetEnumerator()
            {
                return new ValueItemEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new ValueItemEnumerator(this);
            }

            private class ValueItemEnumerator : IEnumerator<ValueItem>, IEnumerator
            {
                private ValueItem _item;
                private int _position = -1;

                public ValueItemEnumerator(ValueItem item)
                {
                    _item = item;
                }

                public bool MoveNext()
                {
                    _position++;
                    return (_position < 1);
                }

                public void Reset()
                {
                    _position = -1;
                }

                public void Dispose()
                {
                    _item = null;
                }

                public ValueItem Current
                {
                    get
                    {
                        if (_position == 0) return _item;
                        else throw new InvalidOperationException();
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        if (_position == 0) return _item;
                        throw new InvalidOperationException();
                    }
                }
            }

            public override string ToString()
            {
                return "ValueItem(x: " + x + ", y: " + y + ")";
            }
        }
        public class ValueItemYEqualityComparer : IEqualityComparer<ValueItem>
        {
            public ValueItemYEqualityComparer()
            {
            }

            public bool Equals(ValueItem x, ValueItem y)
            {
                if ((x != null) & (y != null))
                    return (x.y == y.y);
                else
                    return ((y == null) & (x == null));
            }

            public int GetHashCode(ValueItem x)
            {
                if (x == null) return 0;
                return (x.y);
            }
        }
        public class HashSetComparer<T> : IEqualityComparer<ICollection<T>>
        {
            private readonly IEqualityComparer<T> _comparer;

            public HashSetComparer()
                : this(EqualityComparer<T>.Default)
            { }

            public HashSetComparer(IEqualityComparer<T> comparer)
            {
                if (comparer == null)
                    _comparer = EqualityComparer<T>.Default;
                else
                    _comparer = comparer;
            }

            public bool Equals(ICollection<T> expected, ICollection<T> actual)
            {
                //make sure the same number of items
                Assert.Equal(expected.Count, actual.Count); //"Error: Expected count does not equal "

                //make sure each item in x is also in y
                foreach (T item in expected)
                {
                    bool found = false;
                    foreach (T isit in actual)
                    {
                        if (_comparer.Equals(isit, item))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Console.WriteLine("expected set: ");
                        foreach (var e in expected)
                            Console.WriteLine(e.ToString());
                        Console.WriteLine("actual set: ");
                        foreach (var e in actual)
                            Console.WriteLine(e.ToString());
                        Assert.True(false, String.Format("Error: Item ({0}) from Expected set not found in Actual HashSet", item));
                    }
                }

                //make sure each item in y is also in x
                foreach (T item in actual)
                {
                    bool found = false;

                    foreach (T isit in expected)
                    {
                        if (_comparer.Equals(isit, item))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Console.WriteLine("expected set: ");
                        foreach (var e in expected)
                            Console.WriteLine(e.ToString());
                        Console.WriteLine("actual set: ");
                        foreach (var e in actual)
                            Console.WriteLine(e.ToString());
                        Assert.True(false, String.Format("Error: Item ({0}) found in Actual HashSet NOT in Expected", item));
                    }
                }

                return true;
            }

            //don't expect to actually use this, but need to live up to contract
            public int GetHashCode(ICollection<T> x)
            {
                return 0;
            }
        }
        internal class SetEqualityComparer<U> : IEqualityComparer<HashSet<U>>
        {
            private IEqualityComparer<U> _comparer;

            public SetEqualityComparer()
            {
                _comparer = EqualityComparer<U>.Default;
            }

            public bool Equals(HashSet<U> set1, HashSet<U> set2)
            {
                // handle null cases first
                if (set1 == null)
                {
                    return (set2 == null);
                }
                else if (set2 == null)
                {
                    // set1 != null
                    return false;
                }

                // n^2 search because items are hashed according to their respective ECs
                foreach (U set2Item in set2)
                {
                    bool found = false;
                    foreach (U set1Item in set1)
                    {
                        if (_comparer.Equals(set2Item, set1Item))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        return false;
                    }
                }
                foreach (U set1Item in set1)
                {
                    bool found = false;
                    foreach (U set2Item in set2)
                    {
                        if (_comparer.Equals(set1Item, set2Item))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        return false;
                    }
                }
                return true;
            }

            public int GetHashCode(HashSet<U> obj)
            {
                int hashCode = 0;
                if (obj != null)
                {
                    foreach (U t in obj)
                    {
                        hashCode = hashCode ^ (_comparer.GetHashCode(t) & 0x7FFFFFFF);
                    }
                }
                return hashCode;
            }
        }
        public class IEnumerableEqualityComparer : IEqualityComparer<IEnumerable>
        {
            private IEnumerable _self;

            public void setSelf(IEnumerable self)
            {
                _self = self;
            }

            public bool Equals(IEnumerable x, IEnumerable y)
            {
                if ((x.GetType() == new ValueItem(0, 0).GetType()) & (y.GetType() == new ValueItem(0, 0).GetType()))
                {
                    ValueItem x_vi;
                    ValueItem y_vi;

                    x_vi = (ValueItem)x;
                    y_vi = (ValueItem)y;

                    return ((x_vi.x == y_vi.x) & (x_vi.y == y_vi.y));
                }
                else if (x == _self)
                {
                    return (y == _self);
                }
                else if (y == _self)
                {
                    return false;
                }
                else
                {
                    foreach (object elex in x)
                    {
                        bool found = false;

                        foreach (object eley in y)
                        {
                            if (elex.Equals(eley))
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found) return false;
                    }

                    foreach (object eley in y)
                    {
                        bool found = false;

                        foreach (object elex in x)
                        {
                            if (eley.Equals(elex))
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found) return false;
                    }
                }
                return true;
            }

            public int GetHashCode(IEnumerable x)
            {
                int hash = 0;

                foreach (object ele in x)
                {
                    hash = hash ^ ele.GetHashCode();
                }

                if ((x == _self))
                {
                    hash = x.GetHashCode();
                }

                return hash;
            }
        }
        public class ItemAbsoluteEqualityComparer : IEqualityComparer<Item>
        {
            public bool Equals(Item x, Item y)
            {
                if ((x != null) & (y != null))
                    return (x.x == y.x);
                else
                    return ((y == null) & (x == null));
            }

            public int GetHashCode(Item x)
            {
                if (x == null) return 0;
                if (x.x > 0)
                    return (x.x);
                else
                    return (-x.x);
            }
        }
    }
    #endregion
}