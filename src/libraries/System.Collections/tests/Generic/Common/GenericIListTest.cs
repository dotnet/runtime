// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using TestSupport;
using TestSupport.Common_TestSupport;
using TestSupport.Collections.Common_GenericICollectionTest;
using TestSupport.Collections.Common_GenericIEnumerableTest;

namespace TestSupport.Collections
{
    public class IList_T_Test<T> : ICollection_T_Test<T>
    {
        [Flags]
        public enum VerificationMethod { None = 0, Item = 1, Contains = 2, IndexOf = 4, ICollection = 8 };

        private IList<T> _collection;
        private bool _indexerReadOnly;

        /// <summary>
        /// Initializes a new instance of the IList_T_Test.
        /// </summary>
        /// <param name="collection">The collection to run the tests on.</param>
        /// <param name="generateItem"></param>
        /// <param name="items">The items currently in the collection.</param>
        /// <param name="isReadOnly"></param>
        /// <param name="isFixedSize"></param>
        public IList_T_Test(Test test, IList<T> collection, GenerateItem<T> generateItem, T[] items, bool isReadOnly, bool isFixedSize) : base(test, collection, generateItem, items, isReadOnly)
        {
            if (isReadOnly && !isFixedSize)
            {
                throw new ArgumentException("If the collection is expected to be ReadOnly then it must be expected that the collection is FixedSize");
            }

            _collection = collection;
            _indexerReadOnly = isReadOnly;
        }

        /// <summary>
        /// Runs all of the IList<T> tests.
        /// </summary>
        /// <returns>true if all of the tests passed else false</returns>
        new public bool RunAllTests()
        {
            bool retValue = true;

            retValue &= InitialItems_Tests();

            retValue &= Item_get_Tests();
            retValue &= Item_set_Tests();
            retValue &= IndexOf_Tests();
            retValue &= Insert_Tests();
            retValue &= RemoveAt_Tests();
            retValue &= ICollection_Tests();
            retValue &= InvalidateEnumerator_Tests();
            retValue &= base.RunAllTests();

            retValue &= SetOriginalItems_Tests();

            return retValue;
        }

        /// <summary>
        /// The collection to run the tests on.
        /// </summary>
        /// <value>The collection to run the tests on.</value>
        new public IList<T> Collection
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
        /// Specifies if the indexer is read only
        /// </summary>
        /// <value>True if the indexer is read only else false</value>
        public bool IsIndexerReadOnly
        {
            get
            {
                return _indexerReadOnly;
            }
            set
            {
                _indexerReadOnly = value;
            }
        }

        /// <summary>
        /// This verifies that the collection contains the expected items.
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        new public bool InitialItems_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";

            try
            {
                //[] Verify the initial items in the collection
                testDescription = "945085ejaida Verify the initial items in the collection";
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
        /// Runs all of the argument checking(invalid) tests on get Item.
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        private bool Item_get_Argument_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";

            try
            {
                //[] Verify get_Item with index=Int32.MinValue
                testDescription = "65831irea Verify get_Item with index=Int32.MinValue";

                retValue &= m_test.Eval(m_test.VerifyException<ArgumentOutOfRangeException>(delegate () { T item = _collection[Int32.MinValue]; }),
                    "Err_7027pha expected calling Item_get with index=In32.MinValue to throw ArgumenOutOfRangeException and no exception was thrown");

                //Verify that the collection was not mutated 
                retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                    "Err_" + testDescription + " verifying item array FAILED\n");

                //[] Verify get_Item with index=-1
                testDescription = "3649hog Verify get_Item with index=-1";

                retValue &= m_test.Eval(m_test.VerifyException<ArgumentOutOfRangeException>(delegate () { T item = _collection[-1]; }),
                    "Err_6977hpas expected calling Item_get with index=-1 to throw ArgumenOutOfRangeException and no exception was thrown");

                //Verify that the collection was not mutated 
                retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                    "Err_" + testDescription + " verifying item array FAILED\n");

                //[] Verify get_Item with index=0 on Empty collection
                testDescription = "6198ahp Verify get_Item with index=0 on Empty collection";

                if (!_expectedIsReadOnly)
                {
                    _collection.Clear();
                    _items = new T[0];
                }

                if (_collection.Count == 0)
                {
                    retValue &= m_test.Eval(m_test.VerifyException<ArgumentOutOfRangeException>(delegate () { T item = _collection[0]; }),
                        "Err_3167ahps expected calling get_Item with index=0 to throw ArgumenOutOfRangeException and no exception was thrown");

                    //Verify that the collection was not mutated 
                    retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED\n");
                }

                //[] Verify get_Item with index=Count on Empty collection
                testDescription = "3164panzv Verify get_Item with index=0 on Empty collection";

                if (_items.Length == 0 && !_expectedIsReadOnly)
                {
                    _items = AddItemsToCollection(_collection, 32);
                }

                if (_items.Length != 0)
                {
                    retValue &= m_test.Eval(m_test.VerifyException<ArgumentOutOfRangeException>(delegate () { T item = _collection[_items.Length]; }),
                        "Err_47778wiapd expected calling get_Item with index=Count to throw ArgumenOutOfRangeException and no exception was thrown");

                    //Verify that the collection was not mutated 
                    retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED\n");
                }
            }
            catch (Exception e)
            {
                retValue &= m_test.Eval(false, "The following Item_get test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the valid tests on get Item.
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        private bool Item_get_Valid_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";

            try
            {
                if (_expectedIsReadOnly)
                {
                    //[] Verify get_Item with valid indicies on ReadOnly/FixedSize Collection
                    testDescription = "3189ahps Verify get_Item with valid indicies on ReadOnly/FixedSize Collection";

                    retValue &= m_test.Eval(Verify_get(_collection, _items), "Err_" + testDescription + " FAILED\n");
                }
                else
                {
                    //[] Verify get_Item with valid indicies on a Collection that only contains one item
                    testDescription = "7891hpha Verify get_Item with valid indicies on a Collection that only contains one item";

                    _collection.Clear();
                    _items = AddItemsToCollection(_collection, 1);

                    retValue &= m_test.Eval(Verify_get(_collection, _items), "Err_" + testDescription + " FAILED\n");

                    //[] Verify get_Item with valid indicies on a Collection of size 255
                    testDescription = "6931ahp Verify get_Item with valid indicies on a Collection of size 255";

                    _collection.Clear();
                    _items = AddItemsToCollection(_collection, 255);

                    retValue &= m_test.Eval(Verify_get(_collection, _items), "Err_" + testDescription + " FAILED\n");
                }
            }
            catch (Exception e)
            {
                retValue &= m_test.Eval(false, "The following Item_get test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the tests on get Item.
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        public bool Item_get_Tests()
        {
            bool retValue = true;

            retValue &= Item_get_Argument_Tests();
            retValue &= Item_get_Valid_Tests();

            return retValue;
        }

        /// <summary>
        /// Runs all of the tests on a read only collection for set Item.
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        private bool Item_set_ReadOnly_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";

            try
            {
                //[] If we are dealing with a ReadOnly collection verify setting an items throws
                if (_indexerReadOnly)
                {
                    if (0 < _items.Length)
                    {
                        //[] Verify set_Item with index=0 on a ReadOnly collection throws NotSupportedException
                        testDescription = "3268ahsp Verify set_Item with index=0 on a ReadOnly collection throws NotSupportedException";

                        retValue &= m_test.Eval(m_test.VerifyException<NotSupportedException>(delegate () { _collection[0] = _generateItem(); }),
                            "Err_18794hpa Expected set_Item to throw NotSupportedException with index=0 on ReadOnly collection");

                        //Verify that the collection was not mutated 
                        retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                            "Err_" + testDescription + " verifying item array FAILED\n");

                        //[] Verify set_Item with index=Count - 1 on a ReadOnly collection throws NotSupportedException
                        testDescription = "1359ajsph Verify set_Item with index=Count - 1 on a ReadOnly collection throws NotSupportedException";

                        retValue &= m_test.Eval(m_test.VerifyException<NotSupportedException>(delegate () { _collection[_items.Length - 1] = _generateItem(); }),
                            "Err_31687ahsp Expected set_Item to throw NotSupportedException with index=0 on ReadOnly collection");

                        //Verify that the collection was not mutated 
                        retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                            "Err_" + testDescription + " verifying item array FAILED\n");
                    }
                }
            }
            catch (Exception e)
            {
                retValue &= m_test.Eval(false, "The following Item_set test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the argument checking(invalid) tests on set Item.
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        private bool Item_set_Argument_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";
            Type notSupportedExceptionType = _indexerReadOnly ? typeof(NotSupportedException) : null;

            try
            {
                //[] Verify set_Item with index=Int32.MinValue
                testDescription = "3078euapzb Verify set_Item with index=Int32.MinValue";

                retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentOutOfRangeException), notSupportedExceptionType,
                    delegate () { _collection[Int32.MinValue] = _generateItem(); }),
                    "Err_7027pha expected calling set_Item with index=In32.MinValue to throw ArgumenOutOfRangeException");

                retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                    "Err_" + testDescription + " verifying item array FAILED\n");

                //[] Verify set_Item with index=-1
                testDescription = "3649hog Verify set_Item with index=-1";

                retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentOutOfRangeException), notSupportedExceptionType,
                    delegate () { _collection[-1] = _generateItem(); }),
                    "Err_6977hpas expected calling set_Item with index=-1 to throw ArgumenOutOfRangeException and no exception was thrown");

                retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                    "Err_" + testDescription + " verifying item array FAILED\n");

                //[] Verify set_Item with index=0 on Empty collection
                testDescription = "64648ahps Verify set_Item with index=0 on Empty collection";

                if (!_expectedIsReadOnly)
                {
                    _collection.Clear();
                    _items = new T[0];
                }

                if (_collection.Count == 0)
                {
                    retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentOutOfRangeException), notSupportedExceptionType,
                        delegate () { _collection[0] = _generateItem(); }),
                        "Err_3167ahps expected calling set_Item with index=0 to throw ArgumenOutOfRangeException and no exception was thrown");

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED\n");
                }

                //[] Verify set_Item with index=Count on Empty collection
                testDescription = "3971ahpz Verify set_Item with index=0 on Empty collection";

                if (_items.Length == 0 && !_expectedIsReadOnly)
                {
                    _items = AddItemsToCollection(_collection, 32);
                }

                if (_items.Length != 0)
                {
                    retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentOutOfRangeException), notSupportedExceptionType,
                        delegate () { _collection[_items.Length] = _generateItem(); }),
                        "Err_82382yueao expected calling set_Item with index=Count to throw ArgumenOutOfRangeException and no exception was thrown");

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED\n");
                }
            }
            catch (Exception e)
            {
                retValue &= m_test.Eval(false, "The following Item_set test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the valid tests on set Item.
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        private bool Item_set_Valid_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";

            try
            {
                if (!_indexerReadOnly)
                {
                    T[] origItems;

                    if (!_expectedIsReadOnly)
                    {
                        _collection.Clear();
                        _items = AddItemsToCollection(_collection, 32);
                    }
                    else
                    {
                        SetItemsInCollection(_collection, _items);
                    }

                    origItems = (T[])_items.Clone();

                    if (_collection.Count > 0)
                    {
                        if (!_itemsMustBeNonNull)
                        {
                            //[] Verify setting the first item to null
                            testDescription = "96403ahzpq Verify setting the first item to null";
                            _items = (T[])origItems.Clone();

                            _collection[0] = default(T);
                            _items[0] = default(T);

                            retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                            //[] Verify setting the last item to null
                            testDescription = "21456ahp Verify setting the last item to null";

                            _items = (T[])origItems.Clone();
                            SetItemsInCollection(_collection, _items);

                            _collection[_items.Length - 1] = default(T);
                            _items[_items.Length - 1] = default(T);

                            retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");
                        }
                        else
                        {
                            retValue &= m_test.Eval(m_test.VerifyException(
                                typeof(ArgumentNullException),
                                delegate () { _collection[0] = default(T); }),
                                "Err_683ahe expected setting indexer to null to throw ArgumentNullException");
                        }
                    }

                    //[] Verify set that causes duplicate _items in the collection
                    if (_itemsMustBeUnique)
                    {
                        testDescription = "3848hjaiid Verify set that causes duplicate _items in the collection";
                        if (1 < _collection.Count)
                        {
                            try
                            {
                                _collection[0] = _items[1];
                                retValue &= m_test.Eval(false, "Err_" + testDescription + " FAILED\n");
                            }
                            catch (ArgumentException) { }
                        }
                    }
                    else
                    {
                        testDescription = "15793ajhps Verify set that causes duplicate _items in the collection";

                        _items = (T[])origItems.Clone();
                        SetItemsInCollection(_collection, _items);

                        for (int i = 0; i < _items.Length; i++)
                        {
                            _collection[i] = _items[_items.Length - i - 1];
                        }

                        Array.Reverse(_items);

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");
                    }


                    //[] Verify seting every item in the collection
                    testDescription = "9468ahjps Verify seting every item in the collection";

                    _items = (T[])origItems.Clone();
                    SetItemsInCollection(_collection, _items);

                    //Here we are trying to set the item without causing a duplicate entry to be set
                    //we expect that _generateItem returns an item not already in the collection
                    for (int i = 0; i < _items.Length; i++)
                    {
                        if (i < _items.Length / 2)
                        {
                            _collection[_items.Length - i - 1] = _generateItem();
                        }
                        _collection[i] = _items[_items.Length - i - 1];
                    }

                    Array.Reverse(_items);

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                    //[] Verify seting every item in the collection back to the original item
                    testDescription = "5628ahpnz Verify seting every item in the collection back to the original item";

                    _items = origItems;

                    if (_itemsMustBeUnique)
                    {
                        //If duplicates are not allowed set every item to some other item.
                        for (int i = 0; i < _items.Length; ++i)
                        {
                            _collection[i] = _generateItem();
                        }
                    }

                    SetItemsInCollection(_collection, _items);

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");
                }
            }
            catch (Exception e)
            {
                retValue &= m_test.Eval(false, "The following Item_set test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the tests on set Item.
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        public bool Item_set_Tests()
        {
            bool retValue = true;

            retValue &= Item_set_ReadOnly_Tests();
            retValue &= Item_set_Argument_Tests();
            retValue &= Item_set_Valid_Tests();

            return retValue;
        }

        /// <summary>
        /// Runs all of the tests on IndexOf(Object).
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        public bool IndexOf_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";
            T[] tempItems;

            try
            {
                if (!_expectedIsReadOnly)
                {
                    tempItems = GenerateItems(32);

                    //[] On an empty collection call IndexOf with some non null value
                    testDescription = "13687ajsp On an empty collection call IndexOf with some non null value";

                    _collection.Clear();

                    retValue &= m_test.Eval(-1, _collection.IndexOf(_generateItem()), "Err_67891ajsp IndexOf with non null T on an empty collection");

                    if (!_itemsMustBeNonNull)
                    {
                        //[] On an empty collection call IndexOf with a null value
                        testDescription = "33897ahps On an empty collection call IndexOf with a null value";

                        _collection.Clear();

                        retValue &= m_test.Eval(-1, _collection.IndexOf(default(T)), "Err_32687ahjps IndexOf with null T on an empty collection");
                    }

                    //[] With an empty collection call Add with non null value and call IndexOf with that value
                    testDescription = "15944ahps With an empty collection call Add with non null value and call IndexOf with that value";

                    _collection.Clear();
                    _items = AddItemsToCollection(_collection, 1);

                    retValue &= m_test.Eval(0, _collection.IndexOf(_items[0]), "Err_7945ahzn IndexOf  with value that was added to an empty collection");

                    //[] With an empty collection call Add with non null value and call IndexOf with some other value
                    testDescription = "9974ahps With an empty collection call Add with non null value and call IndexOf with some other value";

                    _collection.Clear();
                    _collection.Add(tempItems[0]);
                    _items = new T[1] { tempItems[0] };

                    retValue &= m_test.Eval(-1, _collection.IndexOf(tempItems[1]),
                        "Err_9497ahps IndexOf with value other then the value that was added to an empty collection");

                    //[] With an empty collection call Add with non null value and call IndexOf with null value
                    if (!_itemsMustBeNonNull)
                    {
                        testDescription = "3794ahsp With an empty collection call Add with non null value and call IndexOf with null value";

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

                        retValue &= m_test.Eval(-1, _collection.IndexOf(default(T)),
                            "Err_4154ahsa IndexOf with null value on an empty collection with one item added to it");

                        //[] On an empty collection call Add with null value and call IndexOf with null value
                        testDescription = "8974hapsjk On an empty collection call Add with null value and call IndexOf with null value";

                        _collection.Clear();
                        _collection.Add(default(T));

                        retValue &= m_test.Eval(0, _collection.IndexOf(default(T)),
                            "Err_1479ahps IndexOf  with null value on an empty collection with null added to it");
                    }
                    else
                    {
                        retValue &= m_test.Eval(m_test.VerifyException(
                            typeof(ArgumentNullException),
                            delegate () { _collection.IndexOf(default(T)); }),
                            "Err_683ahe Expected calling IndexOf with a null item to throw ArgumentNullException");
                    }

                    //[] Call IndexOf with a value that exist twice in the collection	
                    if (!_itemsMustBeUnique)
                    {
                        testDescription = "24698ahyh Call IndexOf with a value that exist twice in the collection	";

                        _collection.Clear();
                        AddItemsToCollection(_collection, tempItems, 0, 16);
                        AddItemsToCollection(_collection, tempItems, 0, 16);
                        _items = new T[16];
                        Array.Copy(tempItems, 0, _items, 0, 16);

                        for (int i = 0; i < _items.Length; i++)
                        {
                            retValue &= m_test.Eval(i, _collection.IndexOf(_items[i]),
                                "Err_97489ahsp IndexOf with value that exists twice in the collection item {0}", i);
                        }

                        retValue &= m_test.Eval(-1, _collection.IndexOf(tempItems[16]),
                            "Err_98749ajsp IndexOf with item that does not exist in the collection");
                    }

                    if (!_itemsMustBeNonNull)
                    {
                        //[] With a collection that contains null somewhere in middle of the collection call IndexOf with null, some value that does not exist and with a value that does exist
                        testDescription = "23899ajsp With a collection that contains null somewhere in middle of the collection call IndexOf with null, some value that does not exist and with a value that does exist";

                        _collection.Clear();
                        _items = new T[29];

                        for (int tempItemsIndex = 0, itemsIndex = 0; itemsIndex < _items.Length; ++tempItemsIndex)
                        {
                            if (!_comparer.Equals(tempItems[tempItemsIndex], default(T)))
                            {
                                _collection.Add(tempItems[tempItemsIndex]);
                                _items[itemsIndex++] = tempItems[tempItemsIndex];
                            }

                            if (itemsIndex == 16)
                            {
                                _collection.Add(default(T));
                                _items[itemsIndex++] = default(T);
                            }
                        }

                        for (int i = 0; i < _items.Length; i++)
                        {
                            retValue &= m_test.Eval(i, _collection.IndexOf(_items[i]),
                                "Err_23897ahsp IndexOf with a collection that contains null somewhere in middle of the collectio item {0}", i);
                        }

                        retValue &= m_test.Eval(-1, _collection.IndexOf(_comparer.Equals(tempItems[30], default(T)) ? tempItems[31] : tempItems[30]),
                            "Err_9749ahps IndexOf with item that does not exist in the collection");

                        //[] With a collection that contains null at the last item in the collection call Conatains with null, some value that does not exist and with a value that does exist
                        testDescription = "9134ahps With a collection that contains null at the last item in the collection call Conatains with null, some value that does not exist and with a value that does exist";

                        _collection.Clear();
                        _items = new T[17];

                        // TODO: This will fail if default(T) (0 for int) is in the collection
                        //Here we depend on _generateItem returning a unique item that is not already int he collection
                        for (int tempItemsIndex = 0, itemsIndex = 0; itemsIndex < _items.Length - 1; ++tempItemsIndex)
                        {
                            if (!_comparer.Equals(tempItems[tempItemsIndex], default(T)))
                            {
                                _collection.Add(tempItems[tempItemsIndex]);
                                _items[itemsIndex++] = tempItems[tempItemsIndex];
                            }
                        }

                        _collection.Add(default(T));
                        _items[16] = default(T);

                        for (int i = 0; i < _items.Length; i++)
                        {
                            retValue &= m_test.Eval(i, _collection.IndexOf(_items[i]),
                                "Err_679ahps With a collection that contains null at the last item in the collection item {0}", i);
                        }

                        retValue &= m_test.Eval(-1, _collection.IndexOf(_comparer.Equals(tempItems[30], default(T)) ? tempItems[31] : tempItems[30]),
                            "Err_88974ahos IndexOf with item that does not exist in the collection");
                    }

                    //[] With a collection that all of the _items in the collection are unique call IndexOf with every item
                    testDescription = "45318hapos With a collection that all of the _items in the collection are unique call IndexOf with every item";

                    _collection.Clear();

                    //Here we depend on _generateItem returning a unique item that is not already int he collection
                    _items = AddItemsToCollection(_collection, 32);

                    for (int i = 0; i < _items.Length; i++)
                    {
                        retValue &= m_test.Eval(i, _collection.IndexOf(_items[i]),
                            "9779ahsp IndexOf with a collection that all of the _items in the collection are unique item {0}", i);
                    }

                    retValue &= m_test.Eval(-1, _collection.IndexOf(_generateItem()), "Err_31659ahnz IndexOf with item that does not exist in the collection");
                }
                else
                {
                    //[] With a collection that all of the _items in the collection are unique call IndexOf with every item
                    testDescription = "9497aznb With a collection that all of the _items in the collection are unique call IndexOf with every item";

                    //Here we depend on every item in the ReadOnly Collection being unique
                    for (int i = 0; i < _items.Length; i++)
                    {
                        retValue &= m_test.Eval(i, _collection.IndexOf(_items[i]),
                            "Err_23168zhp IndexOf with a collection that all of the _items in the collection are unique item {0}", i);
                    }

                    retValue &= m_test.Eval(-1, _collection.IndexOf(_generateItem()), "Err_1389zja IndexOf with item that does not exist in the collection");
                }
            }
            catch (Exception e)
            {
                retValue &= m_test.Eval(false, "The following IndexOf test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the tests on Insert(int, Object).
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        public bool Insert_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";

            try
            {
                retValue &= Insert_Argument_Tests();
                retValue &= Insert_Valid_Tests();
            }
            catch (Exception e)
            {
                retValue &= m_test.Eval(false, "The following Insert test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the argument checking(invalid) tests on Insert(int, Object).
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        private bool Insert_Argument_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";
            Type notSupportedExceptionType = _expectedIsReadOnly ? typeof(NotSupportedException) : null;

            try
            {
                //[] Verify Insert with index=Int32.MinValue
                testDescription = "2268aohs Verify Insert with index=Int32.MinValue";

                retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentOutOfRangeException), notSupportedExceptionType,
                    delegate () { _collection.Insert(Int32.MinValue, _generateItem()); }),
                    "Err_13597ahs expected calling Insert with index=In32.MinValue to throw ArgumenOutOfRangeException and no exception was thrown");

                retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                    "Err_" + testDescription + " verifying item array FAILED\n");

                //[] Verify Insert with index=-1
                testDescription = "56948ajsp Verify Insert with index=-1";

                retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentOutOfRangeException), notSupportedExceptionType,
                    delegate () { _collection.Insert(-1, _generateItem()); }),
                    "Err_31897ahsp expected calling Insert with index=-1 to throw ArgumenOutOfRangeException and no exception was thrown");

                retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                    "Err_" + testDescription + " verifying item array FAILED\n");

                //[] Verify Insert with index=0 on Empty collection
                testDescription = "659ahps Verify Insert with index=0 on Empty collection";

                if (!_expectedIsReadOnly)
                {
                    _collection.Clear();
                    _items = new T[0];
                }

                if (_collection.Count == 0)
                {
                    retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentOutOfRangeException), notSupportedExceptionType,
                        delegate () { _collection.Insert(1, _generateItem()); }),
                        "Err_97469ahps expected calling Insert with index=0 to throw ArgumenOutOfRangeException and no exception was thrown");

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED\n");
                }

                //[] Verify Insert with index=Count on Empty collection
                testDescription = "3298ayhps Verify Insert with index=0 on Empty collection";

                if (_items.Length == 0 && !_expectedIsReadOnly)
                {
                    _items = AddItemsToCollection(_collection, 32);
                }

                if (_items.Length != 0)
                {
                    retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentOutOfRangeException), notSupportedExceptionType,
                        delegate () { _collection.Insert(_items.Length + 1, _generateItem()); }),
                        "Err_1159hpa expected calling Insert with index=Count to throw ArgumenOutOfRangeException and no exception was thrown");

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED\n");
                }

                if (_itemsMustBeNonNull)
                {
                    retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentNullException), notSupportedExceptionType,
                        delegate () { _collection.Insert(0, default(T)); }),
                        "Err_1159hpa expected calling Insert with null item to throw ArgumentNullException");
                }
            }
            catch (Exception e)
            {
                retValue &= m_test.Eval(false, "The following Insert test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the valid tests on Insert(int, Object).
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        private bool Insert_Valid_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";
            T[] tempItems;

            try
            {
                //[] Verify if the colleciton is fixed size or readonly that Insert throws
                if (_expectedIsReadOnly)
                {
                    testDescription = "8944sjpas Verify if the colleciton is fixed size or readonly that Insert throws";

                    retValue &= m_test.Eval(m_test.VerifyException<NotSupportedException>(delegate () { _collection.Insert(0, _generateItem()); }),
                        "Err_21389ajhps Expected Insert to throw NotSupportedException on ReadOnly/FixedSize collection");

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED\n");
                }
                else
                {
                    //[] With empty collection call Insert with index=0, index=Count
                    testDescription = "4556anzp With empty collection call Insert with index=0, index=Count";

                    _collection.Clear();
                    _items = InsertItemsInCollection(_collection, 32);

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                    if (!_itemsMustBeNonNull)
                    {
                        //[] Insert with null at the begining
                        testDescription = "9479aidhe Insert with null at the begining";

                        _items = new T[17];
                        _collection.Clear();

                        tempItems = InsertItemsInCollection(_collection, 16, 0);
                        Array.Copy(tempItems, 0, _items, 1, 16);

                        _collection.Insert(0, default(T));
                        _items[0] = default(T);

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                        //[] Insert with null in the middle
                        testDescription = "148900aued Insert with null in the middle";

                        _items = new T[33];
                        _collection.Clear();

                        tempItems = GenerateItems(32);
                        Array.Copy(tempItems, 0, _items, 0, 16);
                        Array.Copy(tempItems, 16, _items, 17, 16);

                        InsertItemsInCollection(_collection, 0, tempItems, 0, 16);

                        _collection.Insert(16, default(T));
                        _items[16] = default(T);

                        InsertItemsInCollection(_collection, 17, tempItems, 16, 16);

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                        //[] Insert with null at the end
                        testDescription = "78989ade Insert with null at the end";

                        _items = new T[17];
                        _collection.Clear();

                        tempItems = InsertItemsInCollection(_collection, 16, 0);
                        Array.Copy(tempItems, 0, _items, 0, 16);

                        _collection.Insert(16, default(T));
                        _items[16] = default(T);

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");
                    }

                    //[] Insert with value that already exists in the collection
                    if (_itemsMustBeUnique)
                    {
                        testDescription = "6488ajodk Insert with value that already exists in the collection";
                        _collection.Clear();
                        _items = InsertItemsInCollection(_collection, 16);

                        try
                        {
                            _collection.Insert(0, _items[0]);
                            m_test.Eval(false, "Err_54848ahjoidi Expected ArgumentException to be thrown");
                        }
                        catch (ArgumentException) { } //Expected

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");
                    }
                    else
                    {
                        testDescription = "2197ajss Insert with value that already exists in the collection";
                        _items = new T[32];
                        _collection.Clear();

                        tempItems = InsertItemsInCollection(_collection, 16);
                        Array.Copy(tempItems, 0, _items, 0, 16);

                        InsertItemsInCollection(_collection, tempItems, 16);
                        Array.Copy(tempItems, 0, _items, 16, 16);

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");
                    }

                    //[] Insert some _items call Clear() then Insert more _items
                    testDescription = "97823shon Insert some _items call Clear() then Insert more _items";
                    _collection.Clear();

                    InsertItemsInCollection(_collection, 16);
                    _collection.Clear();

                    _items = InsertItemsInCollection(_collection, 16);

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                    //[] Insert some _items remove only some of them then Insert more _items
                    testDescription = "88491ahps Insert some _items remove only some of them then Insert more _items";
                    _items = new T[32];
                    _collection.Clear();

                    tempItems = InsertItemsInCollection(_collection, 32);

                    for (int i = 0; i < 16; i++)
                    {
                        _collection.RemoveAt(0);
                    }

                    Array.Copy(tempItems, 16, _items, 0, 8);
                    Array.Copy(tempItems, 24, _items, 24, 8);

                    tempItems = InsertItemsInCollection(_collection, 16, 8);
                    Array.Copy(tempItems, 0, _items, 8, 16);

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                    //[] Insert some _items remove all of them then Insert more _items
                    testDescription = "1591anzb Insert some _items remove all of them then Insert more _items";
                    _collection.Clear();

                    _items = InsertItemsInCollection(_collection, 32);

                    for (int i = 0; i < 32; i++)
                    {
                        _collection.Remove(_items[i]);
                    }

                    _items = InsertItemsInCollection(_collection, 32);

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                    //[] Mix Insert and Add methods
                    testDescription = "99469ahps Mix Insert and Add methods";
                    _collection.Clear();
                    _items = new T[48];

                    tempItems = GenerateItems(48);
                    _items = tempItems;

                    InsertItemsInCollection(_collection, 0, tempItems, 0, 16);
                    AddItemsToCollection(_collection, tempItems, 16, 16);
                    InsertItemsInCollection(_collection, 32, tempItems, 32, 16);

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                    //[] With a non empty collection call Insert with index=0 and verify the collection. 
                    //Then remove the item that was just inserted. Repeat this for every valid index in the collection
                    testDescription = "649nap With a non empty collection call Insert with index=0 and verify the collection." +
                        " Then remove the item that was just inserted. Repeat this for every valid index in the collection";
                    _collection.Clear();
                    tempItems = new T[17];

                    _items = InsertItemsInCollection(_collection, 16);

                    for (int i = 0; i < 16; i++)
                    {
                        T newItem = _generateItem();

                        _collection.Insert(i, newItem);

                        Array.Copy(_items, 0, tempItems, 0, i);
                        tempItems[i] = newItem;
                        Array.Copy(_items, i, tempItems, i + 1, _items.Length - i);

                        retValue &= m_test.Eval(VerifyCollection(_collection, tempItems), "Err_" + testDescription + " FAILED\n");

                        _collection.RemoveAt(i);
                    }
                }
            }
            catch (Exception e)
            {
                retValue &= m_test.Eval(false, "The following Insert test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the tests on RemoveAt(int).
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        public bool RemoveAt_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";

            try
            {
                retValue &= RemoveAt_Argument_Tests();
                retValue &= RemoveAt_Valid_Tests();
            }
            catch (Exception e)
            {
                retValue &= m_test.Eval(false, "The following RemoveAt test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the argument checking(invalid) tests on RemoveAt(int).
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        private bool RemoveAt_Argument_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";
            Type notSupportedExceptionType = _expectedIsReadOnly ? typeof(NotSupportedException) : null;

            try
            {
                //[] Verify RemoveAt with index=Int32.MinValue
                testDescription = "6791anzp Verify RemoveAt with index=Int32.MinValue";

                retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentOutOfRangeException), notSupportedExceptionType,
                    delegate () { _collection.RemoveAt(Int32.MinValue); }),
                    "Err_3219ahzp expected calling RemoveAt with index=In32.MinValue to throw ArgumenOutOfRangeException and no exception was thrown");

                retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                    "Err_" + testDescription + " verifying item array FAILED\n");

                //[] Verify RemoveAt with index=-1
                testDescription = "4946ahzp Verify RemoveAt with index=-1";

                retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentOutOfRangeException), notSupportedExceptionType,
                    delegate () { _collection.RemoveAt(-1); }),
                    "Err_4987hzp expected calling RemoveAt with index=-1 to throw ArgumenOutOfRangeException and no exception was thrown");

                retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                    "Err_" + testDescription + " verifying item array FAILED\n");

                //[] Verify RemoveAt with index=0 on Empty collection
                testDescription = "8949ahpz Verify RemoveAt with index=0 on Empty collection";

                if (!_expectedIsReadOnly)
                {
                    _collection.Clear();
                    _items = new T[0];
                }

                if (_collection.Count == 0)
                {
                    retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentOutOfRangeException), notSupportedExceptionType,
                    delegate () { _collection.RemoveAt(0); }),
                    "Err_97416ahzo expected calling RemoveAt with index=0 to throw ArgumenOutOfRangeException and no exception was thrown");

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED\n");
                }

                //[] With a collection containing only one item call Remove with index=1
                testDescription = "88549ahps With a collection containing only one item call Remove with index=1";

                if (!_expectedIsReadOnly)
                {
                    _collection.Clear();
                    _items = AddItemsToCollection(_collection, 1);
                }

                if (_items.Length == 1)
                {
                    retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentOutOfRangeException), notSupportedExceptionType,
                        delegate () { _collection.RemoveAt(1); }),
                        "Err_21988ahps expected calling RemoveAt with index=1 to throw ArgumenOutOfRangeException and no exception was thrown");

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED\n");
                }

                //[] Verify RemoveAt with index=Count on non Empty collection
                testDescription = "64967ahpzbz Verify RemoveAt with index=Count on non Empty collection";

                if (_items.Length == 0 && !_expectedIsReadOnly)
                {
                    _items = AddItemsToCollection(_collection, 32);
                }

                if (_items.Length != 0)
                {
                    retValue &= m_test.Eval(m_test.VerifyException(typeof(ArgumentOutOfRangeException), notSupportedExceptionType,
                        delegate () { _collection.RemoveAt(_items.Length); }),
                        "Err_7949ahzh expected calling RemoveAt with index=Count to throw ArgumenOutOfRangeException and no exception was thrown");

                    retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED\n");
                }
            }
            catch (Exception e)
            {
                retValue &= m_test.Eval(false, "The following RemoveAt test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the valid tests on RemoveAt(int).
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        private bool RemoveAt_Valid_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";
            T[] tempItems;

            try
            {
                //[] Verify if the colleciton is fixed size or readonly that Item_set throws
                if (_expectedIsReadOnly)
                {
                    testDescription = "77894azzhpa Verify if the colleciton is fixed size or readonly that RemoveAt throws";
                    if (0 != _items.Length)
                    {
                        retValue &= m_test.Eval(m_test.VerifyException<NotSupportedException>(delegate () { _collection.RemoveAt(0); }),
                            "Err_111649ahos Expected RemoveAt to throw NotSupportedException on ReadOnly/FixedSize collection");

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items, VerificationLevel.Extensive),
                            "Err_" + testDescription + " verifying collection FAILED");
                    }
                }
                else
                {
                    //[] On a collection containing several _items call Remove with every valid index
                    testDescription = "98431ahos On a collection containing several _items call Remove with every valid index";
                    _collection.Clear();
                    _items = new T[31];

                    tempItems = InsertItemsInCollection(_collection, 32);

                    for (int i = 0; i < 32; i++)
                    {
                        _collection.RemoveAt(i);
                        Array.Copy(tempItems, 0, _items, 0, i);
                        Array.Copy(tempItems, i + 1, _items, i, tempItems.Length - i - 1);

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items), "Err_" + testDescription + " FAILED\n");

                        _collection.Insert(i, tempItems[i]);
                    }

                    //[] On a collection containing several _items call Remove with index=0 several times until the collection is empty
                    testDescription = "21968shpz On a collection containing several _items call Remove with index=0 several times until the collection is empty";
                    _collection.Clear();

                    _items = InsertItemsInCollection(_collection, 32);

                    for (int i = 0; i < 32; i++)
                    {
                        _collection.RemoveAt(0);

                        retValue &= m_test.Eval(VerifyCollection(_collection, _items, i + 1, _items.Length - i - 1),
                            "Err_" + testDescription + " iteration(" + i + ") FAILED\n");
                    }

                    _items = new T[0];
                }
            }
            catch (Exception e)
            {
                retValue &= m_test.Eval(false, "The following RemoveAt test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
                retValue = false;
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the tests on the ICollection members.
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        public bool ICollection_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";
            T[] tempItems;

            try
            {
                if (!_expectedIsReadOnly)
                {
                    //[] With a empty collection run all ICollection tests
                    testDescription = "88461ajpz With a empty collection run all ICollection tests";

                    _collection.Clear();
                    _items = new T[0];

                    retValue &= m_test.Eval(Verify_ICollection(_collection, _items), "Err_2318ahps ICollection test FAILED for empty collection");

                    //[] Add some _items then run all ICollection tests
                    testDescription = "65498ajps Add some _items then run all ICollection tests";

                    _collection.Clear();
                    _items = AddItemsToCollection(_collection, 32);

                    retValue &= m_test.Eval(Verify_ICollection(_collection, _items), "Err_23198ahls ICollection test FAILED for collection some _items added to it");

                    //[] Add some _items then Remove some and run all ICollection tests
                    testDescription = "8463ahopz Add some _items then Remove some and run all ICollection tests";
                    _items = new T[14];
                    _collection.Clear();

                    tempItems = AddItemsToCollection(_collection, 32);
                    Array.Copy(tempItems, 1, _items, 0, 7);
                    Array.Copy(tempItems, 24, _items, 7, 7);

                    for (int i = 8; i < 24; i++)
                    {
                        _collection.Remove(tempItems[i]);
                    }

                    //Remove the first item and very last item this may invoke special code for handling removal of the head/tail
                    _collection.RemoveAt(0);
                    _collection.RemoveAt(_collection.Count - 1);

                    retValue &= m_test.Eval(Verify_ICollection(_collection, _items),
                        "Err_2252ahz ICollection test FAILED for collection some _items added to it and then some removed");

                    //[] Add some _items then Remove all and run all ICollection tests
                    testDescription = "2318ahs Add some _items then Remove all and run all ICollection tests";
                    _collection.Clear();
                    _items = new T[0];

                    tempItems = AddItemsToCollection(_collection, 32);

                    for (int i = 0; i < 32; i++)
                    {
                        _collection.Remove(tempItems[i]);
                    }

                    retValue &= m_test.Eval(Verify_ICollection(_collection, _items),
                        "Err_11579ahos ICollection test FAILED for collection some _items added to it and then all removed");

                    //[] Add some _items then call Clear run all ICollection tests
                    testDescription = "3216ajps Add some _items then call Clear run all ICollection tests";
                    _collection.Clear();
                    _items = new T[0];

                    AddItemsToCollection(_collection, 32);
                    _collection.Clear();

                    retValue &= m_test.Eval(Verify_ICollection(_collection, _items),
                        "Err_12549ajps ICollection test FAILED for collection some _items added to it and then clear was called");

                    //[] With a collection containing null values run all ICollection tests
                    if (!_itemsMustBeNonNull)
                    {
                        testDescription = "55649ahps With a collection containing null values run all ICollection tests";
                        _items = new T[17];
                        _collection.Clear();

                        tempItems = AddItemsToCollection(_collection, 16);
                        Array.Copy(tempItems, 0, _items, 0, 16);

                        _collection.Add(default(T));
                        _items[16] = default(T);

                        retValue &= m_test.Eval(Verify_ICollection(_collection, _items),
                            "Err_64888akoed ICollection test FAILED for collection with null values in it");
                    }

                    //[] With a collection containing duplicates run all ICollection tests
                    if (!_itemsMustBeUnique)
                    {
                        testDescription = "55649ahps With a collection containing duplicates run all ICollection tests";
                        _items = new T[32];
                        _collection.Clear();

                        tempItems = AddItemsToCollection(_collection, 16);
                        Array.Copy(tempItems, 0, _items, 0, 16);
                        Array.Copy(tempItems, 0, _items, 16, 16);

                        AddItemsToCollection(_collection, tempItems);

                        retValue &= m_test.Eval(Verify_ICollection(_collection, _items),
                            "Err_2359apsjh ICollection test FAILED for collection with duplicate values in it");
                    }
                }
                else
                {
                    //[] Verify ICollection with ReadOnlyCollection
                    testDescription = "9948aps Verify ICollection with ReadOnlyCollection";

                    retValue &= m_test.Eval(Verify_ICollection(_collection, _items),
                        "Err_23894ajhps ICollection test FAILED for ReadOnly/FixedSize collection");
                }
            }
            catch (Exception e)
            {
                retValue &= m_test.Eval(false, "The following ICollection IEnumerable  test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        public new bool InvalidateEnumerator_Tests()
        {
            bool retValue = true;
            T item = _generateItem();

            if (_expectedIsReadOnly)
            {
                return retValue;
            }

            _collection.Clear();
            _items = new T[] { _generateItem() };
            _collection.Add(_items[0]);

            //[] Insert(int index, T item);
            m_test.PushScenario("Insert(int index, T item);");
            retValue &= VerifyModifiedEnumerator(_collection, true, _items.Length <= 1, delegate () { _collection.Insert(0, item); });
            _items = ArrayUtils.Prepend(_items, item);
            m_test.PopScenario();

            //[] RemoveAt(int index)
            m_test.PushScenario("RemoveAt(int index)");
            retValue &= VerifyModifiedEnumerator(_collection, true, _items.Length <= 1, delegate () { _collection.RemoveAt(0); });
            _items = ArrayUtils.RemoveAt(_items, 0);
            m_test.PopScenario();

            return retValue;
        }

        /// <summary>
        /// Sets the collection back to the original state so that it only 
        /// 1contains items in OriginalItems and verifies this state.
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        new public bool SetOriginalItems_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";

            try
            {
                //[] Set all of the items in the collection back to the original items
                testDescription = "305810ahieaos Set all of the items in the collection back to the original items";
                if (!_expectedIsReadOnly && _collection.Count == _originalItems.Length)
                {
                    SetItemsInCollection(_collection, _originalItems);
                    _items = (T[])_originalItems.Clone();
                }
                else if (!_expectedIsReadOnly)
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

        protected bool VerifyCollection(IList<T> collection, T[] items)
        {
            return VerifyCollection(collection, items, 0, items.Length, VerificationLevel.Normal);
        }

        private bool VerifyCollection(IList<T> collection, T[] items, VerificationLevel verificationLevel)
        {
            return VerifyCollection(collection, items, 0, items.Length, verificationLevel);
        }


        protected bool VerifyCollection(IList<T> collection, T[] items, int index, int count)
        {
            return VerifyCollection(collection, items, index, count, VerificationLevel.Normal);
        }

        private bool VerifyCollection(IList<T> collection, T[] items, int index, int count, VerificationLevel verificationLevel)
        {
            bool retValue = true;

            if (verificationLevel <= _verificationLevel)
            {
                retValue &= Verify_get(collection, items, index, count);
                retValue &= Verify_IndexOf(collection, items, index, count);
                retValue &= base.VerifyCollection(collection, items, index, count);
            }

            return retValue;
        }

        private bool Verify_get(IList<T> collection, T[] items)
        {
            return Verify_get(collection, items, 0, items.Length);
        }

        private bool Verify_get(IList<T> collection, T[] items, int index, int count)
        {
            bool retValue = true;

            if (!(retValue &= m_test.Eval(count, collection.Count,
                "Err_8702haps Verifying get_Item the Count of the collection")))
            {
                for (int i = 0; i < count; i++)
                {
                    int itemsIndex = i + index;
                    if (!(retValue &= m_test.Eval(_comparer.Equals(items[itemsIndex], collection[i]),
                        "Err_1634pnyan Verifying get_Item the item in the collection({0}) and the expected items({1}) differ at={2}", collection[i], items[itemsIndex], itemsIndex)))
                    {
                        break;
                    }
                }
            }

            return retValue;
        }

        private bool Verify_IndexOf(IList<T> collection, T[] items)
        {
            return Verify_IndexOf(collection, items, 0, items.Length);
        }

        private bool Verify_IndexOf(IList<T> collection, T[] items, int index, int count)
        {
            bool retValue = true;

            if ((retValue &= m_test.Eval(count, collection.Count,
                "Err_669713ahzp Verifying IndexOf the count of the collection")))
            {
                for (int i = 0; i < count; i++)
                {
                    int itemsIndex = i + index;
                    int indexOfRetVal = collection.IndexOf(items[itemsIndex]);

                    if (!(retValue &= m_test.Eval(-1 != indexOfRetVal,
                        "Err_1634pnyan Verifying IndexOf and expected item {0} to be in the colleciton", items[itemsIndex])))
                    {
                        break;
                    }
                    else if (!(retValue &= m_test.Eval(_comparer.Equals(items[index + indexOfRetVal], items[itemsIndex]),
                        "Err_88489apps Verifying IndexOf and the index returned is wrong expected to find {0} at {1} actually found {2} at {3}",
                            items[itemsIndex], itemsIndex, items[index + indexOfRetVal], index + indexOfRetVal)))
                    {
                        break;
                    }
                    else if (!(retValue &= m_test.Eval(_comparer.Equals(collection[indexOfRetVal], items[itemsIndex]),
                        "Err_32198ahps Verifying IndexOf and the index returned is wrong expected to find {0} at {1} actually found {2} at {3}",
                            items[itemsIndex], itemsIndex, items[index + indexOfRetVal], indexOfRetVal)))
                    {
                        break;
                    }
                }
            }

            return retValue;
        }

        private bool Verify_ICollection(IList<T> collection, T[] items)
        {
            return Verify_ICollection(collection, items, 0, items.Length);
        }

        private bool Verify_ICollection(IList<T> collection, T[] items, int index, int count)
        {
            bool retValue = true;

            if ((retValue &= m_test.Eval(count, collection.Count,
                "Err_3567ahps Verifying ICollection the Count of the collection")))
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
                        Console.WriteLine("Err_10871ahsph Verifying ICollection FAILED");
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

        private T[] ModifyIList(IEnumerable<T> collection, T[] expectedItems)
        {
            IList<T> iListCollection = (IList<T>)collection;

            if (0 < iListCollection.Count)
            {
                T oldItem = iListCollection[0];

                iListCollection[0] = _generateItem();
                iListCollection[0] = oldItem;
            }
            else
            {
                iListCollection.Add(_generateItem());
                iListCollection.RemoveAt(0);
            }

            return expectedItems;
        }

        private T[] AddItemsToCollection(IList<T> collection, int numItems)
        {
            T[] items = GenerateItems(numItems);

            AddItemsToCollection(collection, items);

            return items;
        }

        private void AddItemsToCollection(IList<T> collection, T[] items)
        {
            AddItemsToCollection(collection, items, 0, items.Length);
        }

        private void AddItemsToCollection(IList<T> collection, T[] items, int index, int count)
        {
            count += index;

            for (int i = index; i < count; i++)
            {
                collection.Add(items[i]);
            }
        }

        private T[] InsertItemsInCollection(IList<T> collection, int numItems)
        {
            return InsertItemsInCollection(collection, numItems, 0);
        }

        private T[] InsertItemsInCollection(IList<T> collection, int numItems, int startIndex)
        {
            T[] items = GenerateItems(numItems);

            InsertItemsInCollection(collection, items, startIndex);

            return items;
        }

        private void InsertItemsInCollection(IList<T> collection, T[] items)
        {
            InsertItemsInCollection(collection, 0, items, 0, items.Length);
        }

        private void InsertItemsInCollection(IList<T> collection, T[] items, int startIndex)
        {
            InsertItemsInCollection(collection, startIndex, items, 0, items.Length);
        }

        private void InsertItemsInCollection(IList<T> collection, int startIndex, T[] items, int index, int count)
        {
            count += index;

            for (int i = index; i < count; i++)
            {
                collection.Insert(startIndex++, items[i]);
            }
        }

        private void SetItemsInCollection(IList<T> collection, T[] items)
        {
            SetItemsInCollection(collection, 0, items, 0, items.Length);
        }

        private void SetItemsInCollection(IList<T> collection, int collectionIndex, T[] items, int arrayIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                collection[i + collectionIndex] = items[i + arrayIndex];
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
