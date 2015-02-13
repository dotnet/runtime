// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using TestSupport;
using TestSupport.Collections.Common_GenericICollectionTest;
using TestSupport.Common_TestSupport;
using TestSupport.Collections.Common_GenericIEnumerableTest;

namespace TestSupport.Collections
{
    namespace Common_GenericICollectionTest
    {
        public class ICollection_T_Test<T> : IEnumerable_T_Test<T>
        {
            private ICollection<T> _collection;

            protected T[] _originalItems;

            protected GenerateItem<T> _generateItem;
            protected bool _expectedIsReadOnly;
            protected bool _itemsMustBeUnique;
            protected bool _itemsMustBeUniqueNonThrowing;
            protected bool _itemsMustBeNonNull;

            private T[] _invalidValues; /* This stores values that are invalid with this implementation of IList. 
									This is really only valid with _isGenericCompatibility == true.
									If it is a Generic class then calling Add, Contains, IndexOf, Insert, 
									and Remove with one of these values should cause ArgumentException
									to be thrown. Null will automatically be added to this if _itemsMustBeNonNull is set 
									to true */


            /// <summary>
            /// Initializes a new instance of the ICollection_T_Test.
            /// </summary>
            /// <param name="collection">The collection to run the tests on.</param>
            /// <param name="generateItem"></param>
            /// <param name="items">The items currently in the collection.</param>
            /// <param name="isReadOnly"></param>
            public ICollection_T_Test(Test test, ICollection<T> collection, GenerateItem<T> generateItem, T[] items, bool isReadOnly) : base(test, collection, items)
            {
                _collection = collection;
                _generateItem = generateItem;
                _expectedIsReadOnly = isReadOnly;

                _itemsMustBeUnique = false;
                _itemsMustBeUniqueNonThrowing = false;
                _itemsMustBeNonNull = false;

                if (!isReadOnly)
                {
                    _modifyCollection = ModifyICollection;
                }

                /* The following will be invalid values as long as this is not a generic collection of objects (List<Object>)*/
                _invalidValues = new T[0];

                _originalItems = (T[])_items.Clone();
            }

            /// <summary>
            /// Runs all of the ICollection<T> tests.
            /// </summary>
            /// <returns>true if all of the tests passed else false</returns>
            new public bool RunAllTests()
            {
                bool retValue = true;

                retValue &= InitialItems_Tests();

                retValue &= Count_Tests();
                retValue &= IsReadOnly_Tests();
                retValue &= Add_Tests();
                retValue &= Clear_Tests();
                retValue &= Contains_Tests();
                retValue &= CopyTo_Tests();
                retValue &= Remove_Tests();
                retValue &= InvalidateEnumerator_Tests();
                retValue &= IEnumerable_Tests();

                retValue &= SetOriginalItems_Tests();

                return retValue;
            }

            /// <summary>
            /// The collection to run the tests on.
            /// </summary>
            /// <value>The collection to run the tests on.</value>
            new public ICollection<T> Collection
            {
                get
                {
                    return _collection;
                }
                set
                {
                    if (null == value)
                    {
                        throw new ArgumentNullException("value");
                    }

                    _collection = value;
                    base.Collection = value;
                }
            }

            /// <summary>
            /// If true specifies that all items must be non null and that setting Item to null, 
            /// Add(null), Contains(null), IndexOf(null), Insert(0, null), and Remove(null)
            /// should all throw ArgumentException. Also setting this to true adds null
            /// to InvalidValues and setting this to false removes null from InvalidValues.
            /// </summary>
            /// <value>If true specifies that all items must be non null.</value>
            public bool ItemsMustBeNonNull
            {
                get
                {
                    return _itemsMustBeNonNull;
                }
                set
                {
                    T nullItem = default(T);
                    int index = Array.IndexOf(_invalidValues, nullItem);

                    if (value)
                    {
                        if (-1 == index)
                        {
                            _invalidValues = ArrayUtils.Concat(_invalidValues, new T[] { nullItem });
                        }
                    }
                    else
                    {
                        if (-1 != index)
                        {
                            _invalidValues = ArrayUtils.RemoveAt(_invalidValues, index);
                        }
                    }

                    _itemsMustBeNonNull = value;
                }
            }

            /// <summary>
            /// Values that are invalid with setting Item, Add, Contains, IndexOf, Insert, and Remove.
            /// </summary>
            /// <value></value>
            public T[] InvalidValues
            {
                get
                {
                    return _invalidValues;
                }
                set
                {
                    if (value == null)
                        _invalidValues = new T[0];

                    _invalidValues = value;
                }
            }

            /// <summary>
            /// Generates items to be used when testing the collection. The items generated should be unique.
            /// </summary>
            /// <value>Generates items to be used when testing the collection.</value>
            public GenerateItem<T> GenerateItem
            {
                get
                {
                    return _generateItem;
                }
                set
                {
                    _generateItem = value;
                }
            }

            /// <summary>
            /// Speicfies if the items in the collection must be unique. Set Item, Add, and Insert 
            /// are expected to throw ArgumentException if the operation will cause a dupplicate item to exist in the collection.
            /// </summary>
            /// <value>Speicfies if the items in the collection must be unique.</value>
            public bool ItemsMustBeUnique
            {
                get
                {
                    return _itemsMustBeUnique;
                }
                set
                {
                    _itemsMustBeUnique = value;
                }
            }

            /// <summary>
            /// If the items must be unique, but the collection doesn't throw on Adding a dupplicate item
            /// then set this to true
            /// </summary>
            /// <value>Speicfies if adding duplicate items will throw or not if ItemsMustBeUnique is set.</value>
            public bool ItemsMustBeUniqueNonThrowing
            {
                get
                {
                    return _itemsMustBeUniqueNonThrowing;
                }
                set
                {
                    if (value)
                    {
                        if (!_itemsMustBeUnique)
                        {
                            throw (new Exception("Must set _itemsMustBeUnique Property first"));
                        }
                    }
                    _itemsMustBeUniqueNonThrowing = value;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <value></value>
            public bool IsReadOnly
            {
                get
                {
                    return _expectedIsReadOnly;
                }
                set
                {
                    _expectedIsReadOnly = value;

                    if (!_expectedIsReadOnly && null == _modifyCollection)
                    {
                        _modifyCollection = ModifyICollection;
                    }
                }
            }

            /// <summary>
            /// The original items in the collection. This is only used by 
            /// InitialItems_Tests and SetOriginalItems_Tests to verify the orignial 
            /// state of the collection and to put the collection back in the original 
            /// sate after all of the tests have been run.
            /// </summary>
            /// <value>The original items in the collection.</value>
            public T[] OriginalItems
            {
                get
                {
                    return _originalItems;
                }
                set
                {
                    if (null == value)
                    {
                        throw new ArgumentNullException("value");
                    }

                    _originalItems = value;
                }
            }

            /// <summary>
            /// This verifies that the collection contains the expected items.
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            public bool InitialItems_Tests()
            {
                bool retValue = true;
                String testDescription = "No description of test available";

                try
                {
                    //[] Verify the initial items in the collection
                    testDescription = "208689aieoad Verify the initial items in the collection";
                    retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Normal),
                            "Err_" + testDescription + " FAILED\n");
                }
                catch (Exception e)
                {
                    retValue &= m_test.Eval(false, "While Verifying the initial items in the colleciton the following exception was thrown:\n{1}\n", testDescription, e);
                }

                return retValue;
            }

            /// <summary>
            /// Runs all of the tests on Count.
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            public bool Count_Tests()
            {
                bool retValue = true;
                String testDescription = "No description of test available";

                try
                {
                    //[] Verify Count returns the expected value
                    testDescription = "6489ahosd Verify Count returns the expected value";

                    retValue &= m_test.Eval(_items.Length, _collection.Count, "Err_6487pqtw Count");
                }
                catch (Exception e)
                {
                    retValue &= m_test.Eval(false, "The following Count test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
                }

                return retValue;
            }

            /// <summary>
            /// Runs all of the tests on IsReadOnly.
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            public bool IsReadOnly_Tests()
            {
                bool retValue = true;
                String testDescription = "No description of test available";

                try
                {
                    //[] Verify IsReadOnly returns the expected value
                    testDescription = "3886ahpo Verify IsReadOnly returns the expected value";

                    retValue &= m_test.Eval(_expectedIsReadOnly, _collection.IsReadOnly, "Err_39478pks IsReadOnly");
                }
                catch (Exception e)
                {
                    retValue &= m_test.Eval(false, "The following IsReadOnly test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
                }

                return retValue;
            }

            /// <summary>
            /// Runs all of the tests on Add(Object).
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            public bool Add_Tests()
            {
                bool retValue = true;
                String testDescription = "No description of test available";

                try
                {
                    //[] Verify if the colleciton is readonly that Add throws
                    if (_expectedIsReadOnly)
                    {
                        testDescription = "94432shlk Verify if the colleciton is readonly that Add throws";

                        retValue &= m_test.Eval(m_test.VerifyException<NotSupportedException>(delegate () { _collection.Add(_generateItem()); }),
                            "Err_6321phse Expected Add to throw NotSupportedException on ReadOnly collection");

                        //Verify that the collection was not mutated 
                        retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                            "Err_" + testDescription + " verifying item array FAILED\n");
                    }
                    else
                    {
                        T[] tempItems;

                        if (!_itemsMustBeNonNull)
                        {
                            //[] Add with null value somewhere in the middle
                            testDescription = "68489hao Add with null value somewhere in the middle";
                            _items = new T[33];

                            _collection.Clear();
                            tempItems = GenerateItems(32);
                            Array.Copy(tempItems, 0, _items, 0, 16);
                            Array.Copy(tempItems, 16, _items, 17, 16);

                            AddItemsToCollection(_collection, tempItems, 0, 16);

                            _collection.Add(default(T));
                            _items[16] = default(T);

                            AddItemsToCollection(_collection, tempItems, 16, 16);

                            retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                            //[] Add with null value at the begining
                            testDescription = "3797phwa Add with null value at the begining";
                            _items = new T[17];

                            _collection.Clear();
                            _collection.Add(default(T));
                            _items[0] = default(T);

                            tempItems = AddItemsToCollection(_collection, 16);
                            Array.Copy(tempItems, 0, _items, 1, 16);

                            retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                            //[] Add with null value at the end
                            testDescription = "27890bzcio Add with null value at the end";
                            _items = new T[17];

                            _collection.Clear();

                            tempItems = AddItemsToCollection(_collection, 16);
                            Array.Copy(tempItems, 0, _items, 0, 16);

                            _collection.Add(default(T));
                            _items[16] = default(T);

                            retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");
                        }

                        //[] Add duplicate value
                        if (_itemsMustBeUnique)
                        {
                            testDescription = "69137hawm Add duplicate value";
                            _collection.Clear();
                            _items = AddItemsToCollection(_collection, 16);

                            if (!_itemsMustBeUniqueNonThrowing)
                            {
                                try
                                {
                                    _collection.Add(_items[0]);
                                    m_test.Eval(false, "Err_29882hauie Expected ArgumentException to be thrown");
                                }
                                catch (ArgumentException) { } //Expected
                            }
                            else
                            {
                                _collection.Add(_items[0]);
                            }

                            retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");
                        }
                        else
                        {
                            testDescription = "69137hawm Add duplicate value";
                            _items = new T[32];

                            _collection.Clear();
                            tempItems = AddItemsToCollection(_collection, 16);
                            AddItemsToCollection(_collection, tempItems);

                            Array.Copy(tempItems, 0, _items, 0, 16);
                            Array.Copy(tempItems, 0, _items, 16, 16);

                            retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");
                        }

                        //[] Add some _items call clear then add more
                        testDescription = "43915ona Add some _items call clear then add more";
                        _items = new T[16];

                        _collection.Clear();
                        AddItemsToCollection(_collection, 16);
                        _collection.Clear();
                        _items = AddItemsToCollection(_collection, 16);

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                        //[] Add some _items remove only some of then then Add more _items
                        testDescription = "38946lpa Add some _items remove only some of then then Add more _items";
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

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                        //[] Add some _items remove all of them then Add more _items
                        testDescription = "9481jzpq Add some _items remove all of them then Add more _items";
                        _items = new T[24];

                        _collection.Clear();
                        tempItems = AddItemsToCollection(_collection, 16);

                        for (int i = 0; i < tempItems.Length; i++)
                        {
                            _collection.Remove(tempItems[i]);
                        }

                        _items = AddItemsToCollection(_collection, 16);

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");
                    }

                    //[] Verify Add with invalid types
                    testDescription = "55058ajeie Verify Add with invalid values";
                    for (int i = 0; i < _invalidValues.Length; ++i)
                    {
                        try
                        {
                            _collection.Add(_invalidValues[i]);
                            retValue &= m_test.Eval(false, "Err_625218ajekd: Exception not thrown from Add with invalid value type {0} iteration:{1}",
                                _invalidValues[i] == null ? "<null>" : _invalidValues[i].GetType().ToString(), i);
                        }
                        catch (ArgumentException) { } // expected
                        catch (NotSupportedException)
                        {
                            retValue &= m_test.Eval(_expectedIsReadOnly,
                                "Err_548548aheiz Add with an invalid value type threw NotSuportedException on a collection that is not FixedSize type {0} iteration:{1}",
                                _invalidValues[i] == null ? "<null>" : _invalidValues[i].GetType().ToString(), i);
                        }
                        finally
                        { //Verify that the collection was not mutated 
                            retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                                "Err_" + testDescription + " verifying collection type {0} iteration:{1} FAILED\n",
                                _invalidValues[i] == null ? "<null>" : _invalidValues[i].GetType().ToString(), i);
                        }
                    }
                }
                catch (Exception e)
                {
                    retValue &= m_test.Eval(false, "The following Add test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
                }

                return retValue;
            }

            /// <summary>
            /// Runs all of the tests on Clear().
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            public bool Clear_Tests()
            {
                bool retValue = true;
                String testDescription = "No description of test available";

                try
                {
                    //[] Verify if the colleciton is readonly that Clear throws
                    if (_expectedIsReadOnly)
                    {
                        testDescription = "21897adh Verify if the colleciton is readonly that Clear throws";

                        retValue &= m_test.Eval(m_test.VerifyException<NotSupportedException>(delegate () { _collection.Clear(); }),
                            "Err_94389ahphs Expected Clear to throw NotSupportedException on ReadOnly collection");

                        //Verify that the collection was not mutated 
                        retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                            "Err_" + testDescription + " verifying item array FAILED\n");
                    }
                    else
                    {
                        //[] Call Clear on an empty collection then Add some _items
                        testDescription = "3142ajps Call Clear on an empty collection then Add some _items";

                        _collection.Clear();
                        _items = AddItemsToCollection(_collection, 16);

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                        //[] Call Clear several time on an empty collection then add some _items
                        testDescription = "9416ahs Call Clear several time on an empty collection then add some _items";

                        _collection.Clear();
                        _collection.Clear();
                        _collection.Clear();
                        _items = AddItemsToCollection(_collection, 16);

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                        //[] Add some _items remove some of them call Clear then add some more _items
                        testDescription = "3694haos Add some _items remove some of them call Clear then add some more _items";

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

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                        //[] Add some _items remove all of them call Clear then add some more _items
                        testDescription = "29473haos Add some _items remove all of them call Clear then add some more _items";

                        _collection.Clear();
                        _items = AddItemsToCollection(_collection, 16);

                        for (int i = 0; i < _items.Length; i++)
                        {
                            _collection.Remove(_items[i]);
                        }

                        _collection.Clear();
                        _items = AddItemsToCollection(_collection, 16);

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                        //[] Call Clear on a collection with one item in it
                        testDescription = "9131adhs Call Clear on a collection with one item in it";

                        _collection.Clear();
                        _items = AddItemsToCollection(_collection, 1);

                        _collection.Clear();
                        _items = AddItemsToCollection(_collection, 16);

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");
                    }
                }
                catch (Exception e)
                {
                    retValue &= m_test.Eval(false, "The following Clear test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
                }

                return retValue;
            }

            /// <summary>
            /// Runs all of the tests on Contains(Object).
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            public bool Contains_Tests()
            {
                bool retValue = true;
                String testDescription = "No description of test available";
                T[] tempItems;

                try
                {
                    if (!_expectedIsReadOnly)
                    {
                        tempItems = GenerateItems(32);

                        //[] On an empty collection call Contains with some non null value
                        testDescription = "36498ahps On an empty collection call Contains with some non null value";

                        _collection.Clear();

                        retValue &= m_test.Eval(!_collection.Contains(_generateItem()),
                            "Err_17081 Contains returned true with non null T on an empty collection");

                        //[] With an empty collection call Add with non null value and call Contains with that value
                        testDescription = "3654ahso With an empty collection call Add with non null value and call Contains with that value";

                        _collection.Clear();
                        _collection.Add(tempItems[0]);
                        _items = new T[1] { tempItems[0] };

                        retValue &= m_test.Eval(_collection.Contains(_items[0]),
                            "Err_23198ahsi Contains returned false with value that was added to an empty collection");

                        //[] With an empty collection call Add with non null value and call Contains with some other value
                        testDescription = "45731hpa With an empty collection call Add with non null value and call Contains with some other value";

                        _collection.Clear();
                        _collection.Add(tempItems[0]);
                        _items = new T[1] { tempItems[0] };

                        retValue &= m_test.Eval(!_collection.Contains(tempItems[1]),
                            "Err_2589lpzi Contains returned false with value other then the value that was added to an empty collection");

                        if (!_itemsMustBeNonNull)
                        {
                            //[] With an empty collection call Add with non null value and call Contains with null value
                            testDescription = "8974hapsjk With an empty collection call Add with non null value and call Contains with null value";

                            _collection.Clear();
                            for (int i = 0; i < tempItems.Length; ++i)
                            {
                                if (!_comparer.Equals(tempItems[i], default(T)))
                                {
                                    _collection.Add(tempItems[i]);
                                    _items = new T[1] { tempItems[i] };
                                    break;
                                }
                            }

                            retValue &= m_test.Eval(!_collection.Contains(default(T)),
                                "Err_3294sjpd Contains returned false with null value on an empty collection with one item added to it");

                            //[] On an empty collection call Contains with a null value
                            testDescription = "8976jhas On an empty collection call Contains with a null value";

                            _collection.Clear();

                            retValue &= m_test.Eval(!_collection.Contains(default(T)),
                                "Err_6149ajpqn Contains returned true with null value on an empty collection");

                            //[] On an empty collection call Add with null value and call Contains with null value
                            testDescription = "8974hapsjk On an empty collection call Add with null value and call Contains with null value";

                            _collection.Clear();
                            _collection.Add(default(T));

                            retValue &= m_test.Eval(_collection.Contains(default(T)),
                                "Err_5586pahl Contains returned false with null value on an empty collection with null added to it");

                            //[] With a collection that contains null somewhere in middle of the collection call Conatains with null, some value that does not exist and with a value that does exist
                            testDescription = "6489ahps With a collection that contains null somewhere in middle of the collection call Conatains with null, some value that does not exist and with a value that does exist";

                            _collection.Clear();
                            _items = new T[30];

                            Array.Copy(tempItems, 0, _items, 0, 15);
                            _items[15] = default(T);
                            Array.Copy(tempItems, 15, _items, 16, 14);

                            AddItemsToCollection(_collection, _items);

                            for (int i = 0; i < _items.Length; i++)
                            {
                                retValue &= m_test.Eval(_collection.Contains(_items[i]), "Err_3697ahsm Contains returned false with {0} expected true", _items[i]);
                            }

                            retValue &= m_test.Eval(!_collection.Contains(_comparer.Equals(tempItems[30], default(T)) ? tempItems[31] : tempItems[30]),
                                "Err_1259yhpa Contains returned true with unique T not in the collection expected false");

                            //[] With a collection that contains null at the last item in the collection call Conatains with null, some value that does not exist and with a value that does exist
                            testDescription = "3189ahopsx With a collection that contains null at the last item in the collection call Conatains with null, some value that does not exist and with a value that does exist";

                            _collection.Clear();
                            _items = new T[17];

                            AddItemsToCollection(_collection, tempItems, 0, 16);
                            Array.Copy(tempItems, 0, _items, 0, 16);

                            _collection.Add(default(T));
                            _items[16] = default(T);

                            for (int i = 0; i < _items.Length; i++)
                            {
                                retValue &= m_test.Eval(_collection.Contains(_items[i]), "Err_1238hspo Contains returned false with {0} expected true", _items[i]);
                            }

                            retValue &= m_test.Eval(!_collection.Contains(_comparer.Equals(tempItems[30], default(T)) ? tempItems[31] : tempItems[30]), "Err_31289snos Contains returned true with new T() expected false");
                        }

                        if (!_itemsMustBeUnique)
                        {
                            //[] Call Contains with a value that exists twice in the collection	
                            testDescription = "34569ajps Call Contains with a value that exists twice in the collection	";

                            _collection.Clear();
                            AddItemsToCollection(_collection, tempItems, 0, 16);
                            AddItemsToCollection(_collection, tempItems, 0, 16);
                            _items = new T[16];
                            Array.Copy(tempItems, 0, _items, 0, 16);

                            for (int i = 0; i < _items.Length; i++)
                            {
                                retValue &= m_test.Eval(_collection.Contains(_items[i]),
                                    "Err_85431aphs Contains returned false with {0} expected true", _items[i]);
                            }

                            retValue &= m_test.Eval(!_collection.Contains(tempItems[16]),
                                "Err_2468hap Contains returned true with new T() expected false");
                        }

                        //[] With a collection that all of the _items in the collection are unique call Contains with every item
                        testDescription = "45318hapos With a collection that all of the _items in the collection are unique call Contains with every item";

                        _collection.Clear();
                        AddItemsToCollection(_collection, tempItems, 0, 31);
                        _items = new T[31];
                        Array.Copy(tempItems, 0, _items, 0, 31);

                        for (int i = 0; i < _items.Length; i++)
                        {
                            retValue &= m_test.Eval(_collection.Contains(_items[i]), "65943ahps Contains returned false with {0} expected true", _items[i]);
                        }

                        retValue &= m_test.Eval(!_collection.Contains(tempItems[31]),
                            "Err_9446haos Contains returned true with unique T not in the collection expected false");
                    }
                    else
                    {
                        //[] With a collection that all of the items in the collection are unique call Contains with every item
                        testDescription = "7964hqpah With a collection that all of the items in the collection are unique call Contains with every item";

                        for (int i = 0; i < _items.Length; i++)
                        {
                            retValue &= m_test.Eval(_collection.Contains(_items[i]), "Err_3494ahsp Contains returned false with {0} expected true", _items[i]);
                        }

                        //Here we depend on _generateItem returning a unique item that is not already int he collection
                        retValue &= m_test.Eval(!_collection.Contains(_generateItem()),
                        "Err_25261nzmq Contains returned true with unique T not in the collection expected false");
                    }

                    //[] Verify Contains with invalid types
                    testDescription = "545488ehaide Verify Contains with invalid values";
                    for (int i = 0; i < _invalidValues.Length; ++i)
                    {
                        try
                        {
                            _collection.Contains(_invalidValues[i]);
                            retValue &= m_test.Eval(false, "Err_0577565aheuiade: Exception not thrown from Contains with invalid value type {0} iteration:{1}",
                                _invalidValues[i] == null ? "<null>" : _invalidValues[i].GetType().ToString(), i);
                        }
                        catch (ArgumentException) { } // expected
                    }
                }
                catch (Exception e)
                {
                    retValue &= m_test.Eval(false, "The following Contains test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
                }

                return retValue;
            }

            /// <summary>
            /// Runs all of the tests on CopyTo(Array).
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            public bool CopyTo_Tests()
            {
                bool retValue = true;
                String testDescription = "No description of test available";
                T[] itemArray = null, tempItemsArray = null;

                try
                {
                    //[] Verify CopyTo with null array
                    testDescription = "8461eahds Verify CopyTo with null array";

                    retValue &= m_test.Eval(m_test.VerifyException<ArgumentNullException>(delegate () { _collection.CopyTo(null, 0); }),
                        "Err_2470zsou: Exception not thrown with null array");

                    // [] Verify CopyTo with index=Int32.MinValue
                    testDescription = "235eade Verify CopyTo with index=Int32.MinValue";
                    itemArray = GenerateArray(_collection.Count);
                    tempItemsArray = (T[])itemArray.Clone();

                    retValue &= m_test.Eval(m_test.VerifyException<ArgumentOutOfRangeException>(
                        delegate () { _collection.CopyTo(new T[_collection.Count], Int32.MinValue); }),
                        "Err_68971aehps: Exception not thrown with index=Int32.MinValue");

                    //Verify that the array was not mutated 
                    retValue &= m_test.Eval(VerifyItems(itemArray, tempItemsArray, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED");

                    // [] Verify CopyTo with index=-1
                    testDescription = "39714ahs Verify CopyTo with index=-1";
                    itemArray = GenerateArray(_collection.Count);
                    tempItemsArray = (T[])itemArray.Clone();

                    retValue &= m_test.Eval(m_test.VerifyException<ArgumentOutOfRangeException>(
                        delegate () { _collection.CopyTo(new T[_collection.Count], -1); }),
                        "Err_3771zsiap: Exception not thrown with index=-1");

                    //Verify that the array was not mutated 
                    retValue &= m_test.Eval(VerifyItems(itemArray, tempItemsArray, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED");

                    // [] Verify CopyTo with index=Int32.MaxValue
                    testDescription = "3987ahso Verify CopyTo with index=Int32.MaxValue";
                    itemArray = GenerateArray(_collection.Count);
                    tempItemsArray = (T[])itemArray.Clone();

                    retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentOutOfRangeException), typeof(ArgumentException), delegate () { _collection.CopyTo(new T[_collection.Count], Int32.MaxValue); }),
                        "Err_39744ahps: Exception not thrown with index=Int32.MaxValue");

                    //Verify that the array was not mutated 
                    retValue &= m_test.Eval(VerifyItems(itemArray, tempItemsArray, VerificationLevel.Extensive),
                    "Err_" + testDescription + " verifying item array FAILED");

                    // [] Verify CopyTo with index=array.length
                    if (0 != _collection.Count)
                    {
                        testDescription = "6498aehas Verify CopyTo with index=array.Length";

                        itemArray = GenerateArray(_collection.Count);
                        tempItemsArray = (T[])itemArray.Clone();

                        retValue &= m_test.Eval(m_test.VerifyException<ArgumentException>(
                            delegate () { _collection.CopyTo(new T[_collection.Count], _collection.Count); }),
                            "Err_2078auoz: Exception not thow with index=array.Length");

                        //Verify that the array was not mutated 
                        retValue &= m_test.Eval(VerifyItems(itemArray, tempItemsArray, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED");
                    }

                    if (1 < _items.Length)
                    {
                        // [] Verify CopyTo with collection.Count > array.length - index
                        testDescription = "8497aehon Verify CopyTo with collection.Count > array.length - index";
                        itemArray = GenerateArray(_collection.Count + 1);
                        tempItemsArray = (T[])itemArray.Clone();
                        retValue &= m_test.Eval(
                            m_test.VerifyException<ArgumentException>(delegate () { _collection.CopyTo(new T[_collection.Count + 1], 2); }),
                            "Err_1734nmzb: Correct exception not thrown with collection.Count > array.length - index");
                    }

                    //Verify that the array was not mutated 
                    retValue &= m_test.Eval(VerifyItems(itemArray, tempItemsArray, VerificationLevel.Extensive),
                    "Err_" + testDescription + " verifying item array FAILED");

                    // [] CopyTo with index=0 and the array is the same size as the collection
                    testDescription = "69731ops CopyTo with index=0 and the array is the same size as the collection";
                    itemArray = GenerateArray(_items.Length);

                    _collection.CopyTo(itemArray, 0);

                    retValue &= m_test.Eval(VerifyItems(itemArray, _items), "Err_" + testDescription + " FAILED");

                    // [] CopyTo with index=0 and the array is 4 items larger then size as the collection
                    testDescription = "84987hjuh CopyTo with index=0 and the array is 4 items larger then size as the collection";
                    itemArray = GenerateArray(_items.Length + 4);
                    tempItemsArray = new T[_items.Length + 4];

                    Array.Copy(itemArray, tempItemsArray, itemArray.Length);
                    Array.Copy(_items, 0, tempItemsArray, 0, _items.Length);
                    _collection.CopyTo(itemArray, 0);

                    retValue &= m_test.Eval(VerifyItems(itemArray, tempItemsArray), "Err_" + testDescription + " FAILED");

                    // [] CopyTo with index=4 and the array is 4 items larger then size as the collection
                    testDescription = "3498huhj CopyTo with index=4 and the array is 4 items larger then size as the collection";
                    itemArray = GenerateArray(_items.Length + 4);
                    tempItemsArray = new T[_items.Length + 4];

                    Array.Copy(itemArray, tempItemsArray, itemArray.Length);
                    Array.Copy(_items, 0, tempItemsArray, 4, _items.Length);
                    _collection.CopyTo(itemArray, 4);

                    retValue &= m_test.Eval(VerifyItems(itemArray, tempItemsArray), "Err_" + testDescription + " FAILED");

                    // [] CopyTo with index=4 and the array is 8 items larger then size as the collection
                    testDescription = "4987uhkh CopyTo with index=4 and the array is 8 items larger then size as the collection";
                    itemArray = GenerateArray(_items.Length + 8);
                    tempItemsArray = new T[_items.Length + 8];

                    Array.Copy(itemArray, tempItemsArray, itemArray.Length);
                    Array.Copy(_items, 0, tempItemsArray, 4, _items.Length);
                    _collection.CopyTo(itemArray, 4);

                    retValue &= m_test.Eval(VerifyItems(itemArray, tempItemsArray), "Err_" + testDescription + " FAILED");
                }
                catch (Exception e)
                {
                    retValue &= m_test.Eval(false, "The following CopyTo test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
                }

                return retValue;
            }

            /// <summary>
            /// Runs all of the tests on Remove(Object).
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            public bool Remove_Tests()
            {
                bool retValue = true;
                String testDescription = "No description of test available";
                T[] tempItems;

                try
                {
                    //[] Verify if the colleciton is readonly that Remove throws
                    if (_expectedIsReadOnly)
                    {
                        testDescription = "21897adh Verify if the colleciton is readonly that Remove throws";
                        retValue &= m_test.Eval(
                            m_test.VerifyException<NotSupportedException>(delegate () { _collection.Remove(_generateItem()); }),
                            "Err_94389ahphs Expected Remove to throw NotSupportedException on ReadOnly collection");

                        //Verify that the collection was not mutated 
                        retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                            "Err_" + testDescription + " verifying item array FAILED\n");
                    }
                    else
                    {
                        tempItems = GenerateItems(32);

                        //[] On an empty collection call Remove with some non null value
                        testDescription = "31949ajps On an empty collection call Remove with some non null value";
                        _collection.Clear();

                        _items = new T[0];
                        m_test.Eval(!_collection.Remove(_generateItem()), "Err_4517487fjvdsa Expected Remove to return false on an empty collection");

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                        if (!_itemsMustBeNonNull)
                        {
                            //[] On an empty collection call Remove with a null value
                            testDescription = "11579anzp On an empty collection call Remove with a null value";
                            _collection.Clear();

                            _items = new T[0];
                            m_test.Eval(!_collection.Remove(default(T)), "Err_2117ejdhjed Expected Remove to return false on an empty collection");

                            retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                            //[] With an empty collection call Add with non null value and call Remove with null value
                            testDescription = "7641ahps With an empty collection call Add with non null value and call Remove with null value";

                            _collection.Clear();
                            for (int i = 0; i < tempItems.Length; ++i)
                            {
                                if (!_comparer.Equals(tempItems[i], default(T)))
                                {
                                    _collection.Add(tempItems[i]);
                                    _items = new T[1] { tempItems[i] };
                                    break;
                                }
                            }

                            m_test.Eval(!_collection.Remove(default(T)), "Err_12177ehdhaz Expected Remove to return false with defualt(T) when default(T) does not exist in the collection");

                            retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                            //[] On an empty collection call Add with null value and call Remove with null value
                            testDescription = "5588508asieap On an empty collection call Add with null value and call Remove with null value";
                            _collection.Clear();
                            _items = new T[0];

                            _collection.Add(default(T));
                            m_test.Eval(_collection.Remove(default(T)), "Err_12017rjfvbuw Expected Remove to return true with defualt(T) when default(T) exists in the collection");

                            retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                            //[] On an empty collection call Add with null value and call Remove with non null value
                            testDescription = "4826508auepauz On an empty collection call Add with null value and call Remove with null value";
                            _collection.Clear();
                            _items = new T[] { default(T) };

                            _collection.Add(default(T));

                            //[] With a collection that contains null somewhere in middle of the collection call Remove with null, 
                            //some value that does not exist and with a value that does exist
                            testDescription = "31969ajps With a collection that contains null somewhere in middle of the collection " +
                                "call Remove with null, some value that does not exist and with a value that does exist";

                            _collection.Clear();
                            _items = new T[30];

                            Array.Copy(tempItems, 0, _items, 0, 16);
                            Array.Copy(tempItems, 17, _items, 16, 14);

                            AddItemsToCollection(_collection, tempItems, 0, 16);
                            _collection.Add(default(T));
                            AddItemsToCollection(_collection, tempItems, 16, 15);

                            m_test.Eval(_collection.Remove(tempItems[16]), "Err_047akijedue Expected Remove to return true with an item that exists in the collection");
                            m_test.Eval(_collection.Remove(default(T)), "Err_741ahjeid Expected Remove to return true with defualt(T) when default(T) exists in the collection");
                            m_test.Eval(!_collection.Remove(tempItems[31]), "Err_871521aide Expected Remove to return false with an item that does not exist in the collection");

                            retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                            //[] With a collection that contains null at the last item in the collection call Remove with null, 
                            //some value that does not exist and with a value that does exist
                            testDescription = "974319anpz With a collection that contains null at the last item in the collection " +
                                "call Remove with null, some value that does not exist and with a value that does exist";

                            _collection.Clear();
                            _items = new T[15];

                            AddItemsToCollection(_collection, tempItems, 0, 16);
                            Array.Copy(tempItems, 0, _items, 0, 11);
                            Array.Copy(tempItems, 12, _items, 11, 4);

                            _collection.Add(default(T));

                            m_test.Eval(_collection.Remove(tempItems[11]), "Err_87948aidued Expected Remove to return true with an item that exists in the collection");
                            m_test.Eval(_collection.Remove(default(T)), "Err_1001ajdhe Expected Remove to return true with defualt(T) when default(T) exists in the collection");
                            m_test.Eval(!_collection.Remove(tempItems[16]), "Err_7141ajdhe Expected Remove to return false with an item that does not exist in the collection");

                            retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");
                        }

                        //[] With an empty collection call Add with non null value and call Remove with that value
                        testDescription = "3194ahp With an empty collection call Add with non null value and call Remove with that value";

                        _collection.Clear();
                        for (int i = 0; i < tempItems.Length; ++i)
                        {
                            if (!_comparer.Equals(tempItems[i], default(T)))
                            {
                                _collection.Add(tempItems[i]);
                                _items = new T[1] { tempItems[i] };
                                break;
                            }
                        }

                        m_test.Eval(_collection.Remove(_items[0]), "Err_1201heud Expected Remove to return true with an item that exists in the collection");
                        _items = new T[0];
                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                        //[] With an empty collection call Add with non null value and call Remove with some other value
                        testDescription = "49431ahps With an empty collection call Add with non null value and call Remove with some other value";
                        _collection.Clear();

                        _collection.Add(tempItems[0]);
                        _items = new T[1] { tempItems[0] };

                        m_test.Eval(!_collection.Remove(tempItems[1]), "Err_0111ahdued Expected Remove to return false with an item that does not exist in the collection");
                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                        m_test.Eval(!_collection.Remove(tempItems[2]), "Err_0477edjeyd Expected Remove to return false with an item that does not exist in the collection");
                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                        //[] Call Remove with a value that exist twice in the collection
                        if (!_itemsMustBeUnique)
                        {
                            testDescription = "1493ahsbzn Call Remove with a value that exist twice in the collection";
                            _collection.Clear();

                            AddItemsToCollection(_collection, tempItems);
                            AddItemsToCollection(_collection, tempItems);

                            for (int i = 0; i < tempItems.Length; i++)
                            {
                                int index = i + 1;
                                _items = new T[tempItems.Length + tempItems.Length - index];

                                Array.Copy(tempItems, index, _items, 0, 32 - index);
                                Array.Copy(tempItems, 0, _items, 32 - index, 32);

                                m_test.Eval(_collection.Remove(tempItems[i]), "Err_788921dhfaz Expected Remove to return true with an item that exists in the collection");

                                retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " iteration(" + i + ") FAILED\n");
                            }
                        }

                        //[] Call Remove with a value that does not exist in the collection
                        testDescription = "3169anos Call Remove with a value that does not exist in the collection";
                        _collection.Clear();

                        AddItemsToCollection(_collection, tempItems, 0, 16);
                        _items = new T[16];
                        Array.Copy(tempItems, 0, _items, 0, 16);

                        m_test.Eval(!_collection.Remove(tempItems[16]), "Err_95541ahdhe Expected Remove to return false with an item that does not exist in the collection");

                        for (int i = 0; i < _items.Length; i++)
                        {
                            retValue &= m_test.Eval(VerifyCollection(_collection, _items, i, _items.Length - i), "Err_" + testDescription + " iteration(" + i + ") FAILED\n");

                            m_test.Eval(_collection.Remove(_items[i]), "Err_02214ahdye Expected Remove to return true with an item that exists in the collection");
                            m_test.Eval(!_collection.Remove(_items[i]), "Err_1788aeijd Expected Remove to return false with an item that does not exist in the collection");
                        }

                        //[] With a collection that all of the _items in the collection are unique call Remove with every item
                        testDescription = "258549aozbn With a collection that all of the _items in the collection are unique call Remove with every item";
                        _collection.Clear();

                        _items = AddItemsToCollection(_collection, 16);

                        for (int i = 0; i < _items.Length; i++)
                        {
                            m_test.Eval(_collection.Remove(_items[i]), "Err_1217aejdu Expected Remove to return true with an item that exists in the collection");
                            retValue &= m_test.Eval(VerifyCollection(_collection, _items, i + 1, _items.Length - i - 1), "Err_" + testDescription + " FAILED\n");
                        }

                        _items = new T[0];
                    }

                    //[] Verify Remove with invalid types
                    testDescription = "20888ahiede Verify Remove with invalid values";
                    for (int i = 0; i < _invalidValues.Length; ++i)
                    {
                        try
                        {
                            _collection.Remove(_invalidValues[i]);
                            retValue &= m_test.Eval(false, "Err_621796afheie: Exception not thrown from Remove with invalid value type {0} iteration:{1}",
                                _invalidValues[i] == null ? "<null>" : _invalidValues[i].GetType().ToString(), i);
                        }
                        catch (ArgumentException) { } // expected
                        catch (NotSupportedException)
                        {
                            retValue &= m_test.Eval(_expectedIsReadOnly,
                                "Err_05651098apzhiue Remove with an invalid value type threw NotSuportedException on a collection that is not FixedSize type {0} iteration:{1}",
                                    _invalidValues[i] == null ? "<null>" : _invalidValues[i].GetType().ToString(), i);
                        }
                        finally
                        { //Verify that the collection was not mutated 
                            retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                                "Err_" + testDescription + " verifying collection type {0} iteration:{1} FAILED\n",
                                    _invalidValues[i] == null ? "<null>" : _invalidValues[i].GetType().ToString(), i);
                        }
                    }
                }
                catch (Exception e)
                {
                    retValue &= m_test.Eval(false, "The following Remove test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
                }

                return retValue;
            }

            public bool InvalidateEnumerator_Tests()
            {
                bool retValue = true;
                T item = _generateItem();

                if (_expectedIsReadOnly)
                {
                    return retValue;
                }

                //[] Clear()
                m_test.PushScenario("Clear()");
                _collection.Clear();
                _items = new T[] { _generateItem() };
                _collection.Add(_items[0]);
                retValue &= VerifyModifiedEnumerator(_collection, false, _items.Length <= 1, delegate () { _collection.Clear(); });
                m_test.PopScenario();

                //[] Add(T item)
                m_test.PushScenario("Add(T item)");
                _collection.Clear();
                _items = new T[] { _generateItem() };
                _collection.Add(_items[0]);
                retValue &= VerifyModifiedEnumerator(_collection, true, _items.Length <= 1, delegate () { _collection.Add(item); });
                _items = ArrayUtils.Concat(_items, item);
                m_test.PopScenario();

                //[] Remove(TKey key)
                m_test.PushScenario("Remove(TKey key)");
                _collection.Clear();
                _items = AddItemsToCollection(_collection, 2);
                retValue &= VerifyModifiedEnumerator(_collection, true, _items.Length <= 1, delegate () { _collection.Remove(_items[1]); });
                _items = new T[] { _items[0] };
                m_test.PopScenario();

                return retValue;
            }

            /// <summary>
            /// Runs all of the tests on the IEnumerable members.
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            public bool IEnumerable_Tests()
            {
                bool retValue = true;
                String testDescription = "No description of test available";
                T[] tempItems;

                try
                {
                    if (!_expectedIsReadOnly)
                    {
                        //[] With a empty collection run all IEnumerable tests
                        testDescription = "88461ajpz With a empty collection run all IEnumerable tests";

                        _collection.Clear();
                        _items = new T[0];

                        retValue &= m_test.Eval(Verify_IEnumerable(_collection, _items), "Err_2318ahps IEnumerable test FAILED for empty collection");

                        //[] Add some _items then run all IEnumerable tests
                        testDescription = "65498ajps Add some _items then run all IEnumerable tests";

                        _collection.Clear();
                        _items = AddItemsToCollection(_collection, 32);

                        retValue &= m_test.Eval(Verify_IEnumerable(_collection, _items), "Err_23198ahls IEnumerable test FAILED for collection some _items added to it");

                        //[] Add some _items then Remove some and run all IEnumerable tests
                        testDescription = "8463ahopz Add some _items then Remove some and run all IEnumerable tests";
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
                        testDescription = "3216ajps Add some _items then call Clear run all IEnumerable tests";
                        _collection.Clear();
                        _items = new T[0];

                        AddItemsToCollection(_collection, 32);
                        _collection.Clear();

                        retValue &= m_test.Eval(Verify_IEnumerable(_collection, _items),
                            "Err_12549ajps IEnumerable test FAILED for collection some _items added to it and then clear was called");

                        //[] With a collection containing null values run all IEnumerable tests
                        if (!_itemsMustBeNonNull)
                        {
                            testDescription = "55649ahps With a collection containing null values run all IEnumerable tests";
                            _items = new T[17];
                            _collection.Clear();

                            tempItems = AddItemsToCollection(_collection, 16);
                            Array.Copy(tempItems, 0, _items, 0, 16);

                            _collection.Add(default(T));
                            _items[16] = default(T);

                            retValue &= m_test.Eval(Verify_IEnumerable(_collection, _items),
                                "Err_64888akoed IEnumerable test FAILED for collection with null values in it");
                        }

                        //[] With a collection containing duplicates run all IEnumerable tests
                        if (!_itemsMustBeUnique)
                        {
                            testDescription = "55649ahps With a collection containing duplicates run all IEnumerable tests";
                            _items = new T[32];
                            _collection.Clear();

                            tempItems = AddItemsToCollection(_collection, 16);
                            Array.Copy(tempItems, 0, _items, 0, 16);
                            Array.Copy(tempItems, 0, _items, 16, 16);

                            AddItemsToCollection(_collection, tempItems);

                            retValue &= m_test.Eval(Verify_IEnumerable(_collection, _items),
                                "Err_2359apsjh IEnumerable test FAILED for collection with duplicate values in it");
                        }
                    }
                    else
                    {
                        //[] Verify IEnumerable with ReadOnlyCollection
                        testDescription = "9948aps Verify IEnumerable with ReadOnlyCollection";

                        retValue &= m_test.Eval(Verify_IEnumerable(_collection, _items),
                            "Err_23894ajhps IEnumerable test FAILED for ReadOnly/FixedSize collection");
                    }
                }
                catch (Exception e)
                {
                    retValue &= m_test.Eval(false, "The following IEnumerable IEnumerable  test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
                }

                return retValue;
            }

            /// <summary>
            /// Sets the collection back to the original state so that it only 
            /// contains items in OriginalItems and verifies this state.
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            public bool SetOriginalItems_Tests()
            {
                bool retValue = true;
                String testDescription = "No description of test available";

                try
                {
                    //[] Set all of the items in the collection back to the original items
                    testDescription = "4584586ahied Set all of the items in the collection back to the original items";

                    if (!_expectedIsReadOnly)
                    {
                        _collection.Clear();
                        AddItemsToCollection(_collection, _originalItems);
                        _items = (T[])_originalItems.Clone();
                    }

                    retValue &= m_test.Eval(VerifyCollection(_collection, _originalItems, VerificationLevel.Normal),
                            "Err_" + testDescription + " FAILED\n");
                }
                catch (Exception e)
                {
                    retValue &= m_test.Eval(false,
                        "While setting the items in the collection back to the original items the following exception was thrown:\n{1}\n", testDescription, e);
                }

                return retValue;
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

            private bool VerifyItems(T[] actualItems, T[] expectedItems)
            {
                return VerifyItems(actualItems, expectedItems, VerificationLevel.Normal);
            }

            private bool VerifyItems(T[] actualItems, T[] expectedItems, VerificationLevel verificationLevel)
            {
                return VerifyItems(new ArraySegment<T>(actualItems, 0, actualItems.Length),
                    new ArraySegment<T>(expectedItems, 0, expectedItems.Length),
                    verificationLevel);
            }

            private bool VerifyItems(ArraySegment<T> actualItems, ArraySegment<T> expectedItems, VerificationLevel verificationLevel)
            {
                bool retValue = true;
                int actualItemsCount = actualItems.Offset + actualItems.Count;
                int expectedItemsCount = expectedItems.Offset + expectedItems.Count;
                int actualItemsIndex = actualItems.Offset;
                int expectedItemsIndex = expectedItems.Offset;

                if (verificationLevel <= _verificationLevel)
                {
                    if (!(retValue &= m_test.Eval(expectedItems.Count, actualItems.Count,
                        "Err_1707ahps The length of the actual items and the expected items differ")))
                    {
                        return retValue;
                    }


                    if (_collectionOrder == CollectionOrder.Sequential)
                    {
                        for (; expectedItemsIndex < expectedItemsCount; ++actualItemsIndex, ++expectedItemsIndex)
                        {
                            if (!(retValue &= m_test.Eval(_comparer.Equals(actualItems.Array[actualItemsIndex], expectedItems.Array[expectedItemsIndex]),
                                "Err_0722haps The actual item and expected items differ at {0} actual={1} expected={2}", expectedItemsIndex,
                                actualItems.Array[actualItemsIndex], expectedItems.Array[expectedItemsIndex])))
                            {
                                return retValue;
                            }
                        }
                    }
                    else if (_collectionOrder == CollectionOrder.Unspecified)
                    {
                        System.Collections.BitArray itemsVisited = new System.Collections.BitArray(expectedItems.Count, false);
                        bool itemFound;


                        for (; actualItemsIndex < actualItemsCount; ++actualItemsIndex)
                        {
                            itemFound = false;
                            expectedItemsIndex = expectedItems.Offset;

                            for (int i = 0; expectedItemsIndex < expectedItemsCount; ++i, ++expectedItemsIndex)
                            {
                                if (!itemsVisited[i] && _comparer.Equals(actualItems.Array[actualItemsIndex], expectedItems.Array[expectedItemsIndex]))
                                {
                                    itemsVisited[i] = true;
                                    itemFound = true;
                                    break;
                                }
                            }

                            retValue &= m_test.Eval(itemFound, "Err_02184aied Did not find={0} in expected", actualItems.Array[actualItemsIndex]);
                        }

                        expectedItemsIndex = expectedItems.Offset;
                        for (int i = 0; expectedItemsIndex < expectedItemsCount; ++i, ++expectedItemsIndex)
                        {
                            retValue &= m_test.Eval(itemsVisited[i], "Err_0515648aieid Expected to find={0} in actual", expectedItems.Array[expectedItemsIndex]);
                        }
                    }

                    return retValue;
                }

                return retValue;
            }

            protected bool VerifyCollection(ICollection<T> collection, T[] items)
            {
                return VerifyCollection(collection, items, 0, items.Length, VerificationLevel.Normal);
            }

            private bool VerifyCollection(ICollection<T> collection, T[] items, VerificationLevel verificationLevel)
            {
                return VerifyCollection(collection, items, 0, items.Length, verificationLevel);
            }


            protected bool VerifyCollection(ICollection<T> collection, T[] items, int index, int count)
            {
                return VerifyCollection(collection, items, index, count, VerificationLevel.Normal);
            }

            private bool VerifyCollection(ICollection<T> collection, T[] items, int index, int count, VerificationLevel verificationLevel)
            {
                bool retValue = true;

                if (verificationLevel <= _verificationLevel)
                {
                    retValue &= Verify_Count(collection, items, index, count);
                    retValue &= Verify_Contains(collection, items, index, count);
                    retValue &= Verify_CopyTo(collection, items, index, count);
                    retValue &= base.VerifyCollection(collection, items, index, count);
                }

                return retValue;
            }

            private bool Verify_Count(ICollection<T> collection, T[] items, int index, int count)
            {
                return m_test.Eval(count, collection.Count, "Err_255087aiedaed Verifying Count");
            }

            private bool Verify_Contains(ICollection<T> collection, T[] items, int index, int count)
            {
                bool retValue = true;

                if ((retValue &= m_test.Eval(count, collection.Count,
                    "Err_321987ahsp Verifying Contains the actual count and expected count differ")))
                {
                    for (int i = 0; i < count; i++)
                    {
                        int itemsIndex = i + index;

                        if (!(retValue &= m_test.Eval(collection.Contains(items[itemsIndex]),
                            "Err_1634pnyan Verifying Contains and expected item {0} to be in the colleciton", items[itemsIndex])))
                        {
                            break;
                        }
                    }
                }

                return retValue;
            }

            private bool Verify_CopyTo(ICollection<T> collection, T[] items, int index, int count)
            {
                bool retValue = true;

                if ((retValue &= m_test.Eval(count, collection.Count,
                    "Err_25804akduea Verifying CopyTo the actual count and expected count differ")))
                {
                    T[] actualItems = new T[count];

                    collection.CopyTo(actualItems, 0);

                    retValue = m_test.Eval(VerifyItems(new ArraySegment<T>(actualItems, 0, count), new ArraySegment<T>(items, index, count), VerificationLevel.Normal),
                        "Err_328290ahiue Verifying items from CopyTo FAILED");
                }

                return retValue;
            }

            protected bool VerifyModifiedEnumerator(ICollection<T> collection, bool verifyCurrent, bool atEnd, ModifyCollection modifyCollection)
            {
                bool retValue = true;
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
                {
                    retValue &= m_test.Eval<T>(_comparer, current, genericIEnumerator.Current, "Err_05188aied IEnumerator<T>.Current");
                    if (_converter == null)
                    {
                        retValue &= m_test.Eval<object>(new ObjectComparer<T>(_comparer), objCurrent, iEnumerator.Current, "Err_8553wisugv IEnumerator.Current");
                    }
                    else
                    {
                        retValue &= m_test.Eval<object>(new ObjectComparerWithConverter<T>(_comparer, _converter), objCurrent, iEnumerator.Current, "Err_8553wisugv IEnumerator.Current");
                    }
                }

                //[] MoveNext
                if (!atEnd || _moveNextAtEndThrowsOnModifiedCollection)
                {
                    retValue &= m_test.VerifyException<InvalidOperationException>(delegate () { genericIEnumerator.MoveNext(); }, "Err_5048ajed IEnumerator<T>.MoveNext()");
                    retValue &= m_test.VerifyException<InvalidOperationException>(delegate () { iEnumerator.MoveNext(); }, "Err_548aied IEnumerator.MoveNext()");
                }

                //[] Reset
                if (!_isResetNotSupported)
                {
                    retValue &= m_test.VerifyException<InvalidOperationException>(delegate () { genericIEnumerator.Reset(); }, "Err_6412aied IEnumerator<T>.Reset()");
                    retValue &= m_test.VerifyException<InvalidOperationException>(delegate () { iEnumerator.Reset(); }, "Err_0215aheiud IEnumerator.Reset()");
                }

                return retValue;
            }

            private bool Verify_IEnumerable(ICollection<T> collection, T[] items)
            {
                return Verify_IEnumerable(collection, items, 0, items.Length);
            }

            private bool Verify_IEnumerable(ICollection<T> collection, T[] items, int index, int count)
            {
                bool retValue = true;

                if ((retValue &= m_test.Eval(count, collection.Count,
                    "Err_3567ahps Verifying IEnumerable the Count of the collection")))
                {
                    T[] originalItemsBeforeTest = _originalItems;
                    T[] itemsBeforeTest = _items;

                    try
                    {
                        if (0 != index || items.Length != count)
                        {
                            T[] tempItems = new T[count];
                            Array.Copy(items, index, tempItems, 0, count);
                            _items = tempItems;
                        }
                        else
                        {
                            _items = items;
                        }

                        _originalItems = itemsBeforeTest;

                        /*
                        _items and _orinalItems have been set to the expected Array. Therefore after running all of the base 
                        tests the collection should be set back to original state. We will set _items and _originalItem back to 
                        their valuse before this test was run in the finally.
                        */
                        if (!base.RunAllTests())
                        {
                            Console.WriteLine("Err_10871ahsph Verifying IEnumerable FAILED");
                            retValue = false;
                        }
                    }
                    finally
                    {
                        _originalItems = originalItemsBeforeTest;
                        _items = itemsBeforeTest;
                    }
                }

                return retValue;
            }

            private T[] ModifyICollection(IEnumerable<T> collection, T[] expectedItems)
            {
                ICollection<T> iCollection = (ICollection<T>)collection;
                T tempItem;

                do
                {
                    tempItem = _generateItem();
                } while (!IsUniqueItem(expectedItems, tempItem));


                iCollection.Add(tempItem);
                iCollection.Remove(tempItem);

                return expectedItems;
            }

            private T[] AddItemsToCollection(ICollection<T> collection, int numItems)
            {
                T[] items = GenerateItems(numItems);

                AddItemsToCollection(collection, items);

                return items;
            }

            private void AddItemsToCollection(ICollection<T> collection, T[] items)
            {
                AddItemsToCollection(collection, items, 0, items.Length);
            }

            private void AddItemsToCollection(ICollection<T> collection, T[] items, int index, int count)
            {
                count += index;

                for (int i = index; i < count; i++)
                {
                    collection.Add(items[i]);
                }
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
                    if (_comparer.Equals(items[i], item))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
