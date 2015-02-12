// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using TestSupport;
using TestSupport.Collections.SortedList_GenericIEnumerableTest;
using TestSupport.Common_TestSupport;
using TestSupport.Collections.SortedList_IEnumerableTest;

namespace TestSupport.Collections
{
    namespace SortedList_GenericIEnumerableTest
    {
        public delegate TOutput Converter<in TInput, out TOutput>(TInput input);
        public class IEnumerable_T_Test<T>
        {
            private IEnumerable<T> _collection;
            protected ModifyUnderlyingCollection_T<T> _modifyCollection;

            protected T[] _items;
            protected IEqualityComparer<T> _comparer;
            protected VerificationLevel _verificationLevel;

            protected CollectionOrder _collectionOrder;

            protected Converter<Object, T> _converter;

            protected bool _isResetNotSupported;
            protected bool _moveNextAtEndThrowsOnModifiedCollection;

            private IEnumerable_T_Test() { }

            /// <summary>
            /// Initializes a new instance of the IEnumerable_T_Test.
            /// </summary>
            /// <param name="collection">The collection to run the tests on.</param>
            /// <param name="items">The items currently in the collection.</param>
            public IEnumerable_T_Test(IEnumerable<T> collection, T[] items) : this(collection, items, null) { }

            /// <summary>
            /// Initializes a new instance of the IEnumerable_T_Test.
            /// </summary>
            /// <param name="collection">The collection to run the tests on.</param>
            /// <param name="items"></param>
            /// <param name="modifyCollection"></param>
            public IEnumerable_T_Test(IEnumerable<T> collection, T[] items, ModifyUnderlyingCollection_T<T> modifyCollection)
            {
                _collection = collection;
                _modifyCollection = modifyCollection;
                _verificationLevel = VerificationLevel.Extensive;
                _collectionOrder = CollectionOrder.Sequential;
                _converter = null;
                _isResetNotSupported = false;
                _moveNextAtEndThrowsOnModifiedCollection = true;

                if (items == null)
                    _items = new T[0];
                else
                    _items = items;

                _comparer = EqualityComparer<T>.Default;
            }

            /// <summary>
            /// If true specifes that IEnumerator.Reset is not supported on the 
            /// IEnumerator returned from Collection and will throw NotSupportedException.
            /// </summary>
            public bool IsResetNotSupported
            {
                get
                {
                    return _isResetNotSupported;
                }
                set
                {
                    _isResetNotSupported = value;
                }
            }

            /// <summary>
            /// Specifies if the MoveNext will throw when the eumerator is positioneda t
            /// the end of the collection when the collection has been modified.
            /// </summary>
            public bool MoveNextAtEndThrowsOnModifiedCollection
            {
                get
                {
                    return _moveNextAtEndThrowsOnModifiedCollection;
                }
                set
                {
                    _moveNextAtEndThrowsOnModifiedCollection = value;
                }
            }

            /// <summary>
            /// Runs all of the IEnumerable<T> tests.
            /// </summary>
            /// <returns>true if all of the tests passed else false</returns>
            public bool RunAllTests()
            {
                bool retValue = true;

                retValue &= MoveNext_Tests();
                retValue &= Current_Tests();
                retValue &= ModifiedCollection_Test();
                retValue &= NonGenericIEnumerable_Test();

                return retValue;
            }

            /// <summary>
            /// The collection to run the tests on.
            /// </summary>
            /// <value>The collection to run the tests on.</value>
            public IEnumerable<T> Collection
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
                }
            }

            /// <summary>
            /// The items in the Collection
            /// </summary>
            /// <value>The items in the Collection</value>
            public T[] Items
            {
                get
                {
                    return _items;
                }
                set
                {
                    if (null == value)
                    {
                        throw new ArgumentNullException("value");
                    }

                    _items = value;
                }
            }

            /// <summary>
            /// Modifies the collection. If null the test that verify that enumerating a 
            /// collection that has been modified since the enumerator has been created 
            /// will not be run.
            /// </summary>
            /// <value>Modifies the collection.</value>
            public ModifyUnderlyingCollection_T<T> ModifyCollection
            {
                get
                {
                    return _modifyCollection;
                }
                set
                {
                    _modifyCollection = value;
                }
            }

            /// <summary>
            /// The IComparer used to compare the items. If null Comparer<Object>.Defualt will be used.
            /// </summary>
            /// <value>The IComparer used to compare the items.</value>
            public IEqualityComparer<T> Comparer
            {
                get
                {
                    return _comparer;
                }
                set
                {
                    if (null == value)
                        _comparer = EqualityComparer<T>.Default;
                    else
                        _comparer = value;
                }
            }

            /// <summary>
            /// The Verification level to use. If VerificationLevel is Extensize the collection 
            /// will be verified after argument checking (invalid) tests. 
            /// </summary>
            /// <value>The Verification level to use.</value>
            public VerificationLevel VerificationLevel
            {
                get
                {
                    return _verificationLevel;
                }
                set
                {
                    _verificationLevel = value;
                }
            }

            /// <summary>
            /// This specifies where Add places an item at the beginning of the collection, 
            /// the end of the collection, or unspecifed. Items is expected to be in the 
            /// smae order as the enumerator unless AddOrder.Unspecified is used. 
            /// </summary>
            /// <value>This specifies where Add places an item.</value>
            public CollectionOrder CollectionOrder
            {
                get
                {
                    return _collectionOrder;
                }
                set
                {
                    _collectionOrder = value;
                }
            }

            /// <summary>
            ///	This converts  the Object retruned from the non generic IEnumerator to T.
            /// </summary>
            /// <value>Converts  the Object retruned from the non generic IEnumerator to T.</value>
            public Converter<Object, T> Converter
            {
                get
                {
                    return _converter;
                }
                set
                {
                    _converter = value;
                }
            }

            /// <summary>
            /// Runs all of the tests on MoveNext().
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            public bool MoveNext_Tests()
            {
                bool retValue = true;
                int iterations = 0;
                IEnumerator<T> enumerator = _collection.GetEnumerator();
                String testDescription = "No description of test available";

                try
                {
                    //[] Call MoveNext() untill the end of the collection has been reached
                    testDescription = "1082ahhd Call MoveNext() untill the end of the collection has been reached";

                    while (enumerator.MoveNext())
                    {
                        iterations++;
                    }

                    retValue &= Test.Eval(_items.Length, iterations, "Err_64897adhs Number of items to iterate through");

                    //[] Call MoveNext() several times after the end of the collection has been reached
                    testDescription = "78083adshp Call MoveNext() several times after the end of the collection has been reached";

                    for (int j = 0; j < 3; j++)
                    {
                        try
                        {
                            T tempCurrent = enumerator.Current;
                        }
                        catch (Exception) { }//Behavior of Current here is undefined 

                        retValue &= Test.Eval(!enumerator.MoveNext(),
                            "Err_1081adohs Expected MoveNext() to return false on the {0} after the end of the collection has been reached\n", j + 1);
                    }
                }
                catch (Exception e)
                {
                    retValue &= Test.Eval(false, "The following test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
                }

                return retValue;
            }

            /// <summary>
            /// Runs all of the tests on Current.
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            public bool Current_Tests()
            {
                bool retValue = true;
                IEnumerator<T> enumerator;
                String testDescription = "No description of test available";

                try
                {
                    //[] Call MoveNext() untill the end of the collection has been reached
                    testDescription = "1082ahhd Call MoveNext() untill the end of the collection has been reached";
                    enumerator = _collection.GetEnumerator();

                    retValue &= Test.Eval(VerifyEnumerator(enumerator, _items), "Err_" + testDescription + " FAILED\n");

                    //[] Enumerate only part of the collection
                    testDescription = "64589eahps Enumerate only part of the collection ";
                    enumerator = _collection.GetEnumerator();

                    retValue &= Test.Eval(VerifyEnumerator(enumerator, _items, 0, _items.Length / 2, ExpectedEnumeratorRange.Start, true),
                        "Err_" + testDescription + " FAILED\n");
                }
                catch (Exception e)
                {
                    retValue &= Test.Eval(false, "The following test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
                }

                return retValue;
            }

            /// <summary>
            /// Runs tests when the collection has been modified after 
            /// the enumerator was created.
            /// </summary>
            /// <returns>true if all of the test passed else false.</returns>
            public bool ModifiedCollection_Test()
            {
                bool retValue = true;
                IEnumerator<T> enumerator;
                String testDescription = "No description of test available";
                bool atEnd;

                try
                {
                    if (null == _modifyCollection)
                    {
                        //We have no way to modify the collection so we will just have to return true;
                        return true;
                    }

                    //[] Verify Modifying collecton with new Enumerator
                    testDescription = "7885huad Verify Modifying collecton with new Enumerator";
                    enumerator = _collection.GetEnumerator();
                    atEnd = _items.Length <= 1;
                    _items = _modifyCollection(_collection, _items);

                    retValue &= Test.Eval(VerifyModifiedEnumerator(enumerator, atEnd), "Err_" + testDescription + " FAILED\n");

                    //[] Verify enumerating to the first item
                    if (0 != _items.Length)
                    {
                        testDescription = "3158eadf Verify enumerating to the first item";
                        enumerator = _collection.GetEnumerator();
                        atEnd = 1 == _items.Length;

                        retValue &= Test.Eval(VerifyEnumerator(enumerator, _items, 0, 1, ExpectedEnumeratorRange.Start, true),
                            "Err_" + testDescription + " FAILED\n");

                        //[] Verify Modifying collection on an enumerator that has enumerated to the first item in the collection
                        testDescription = "9434hhk Verify Modifying collection on an enumerator that has enumerated to the first item in the collection";

                        _items = _modifyCollection(_collection, _items);

                        retValue &= Test.Eval(VerifyModifiedEnumerator(enumerator, atEnd), "Err_" + testDescription + " FAILED\n");
                    }

                    //[] Verify enumerating part of the collection
                    if (0 != _items.Length)
                    {
                        testDescription = "128uhkh Verify enumerating part of the collection";
                        enumerator = _collection.GetEnumerator();

                        retValue &= Test.Eval(VerifyEnumerator(enumerator, _items, 0, _items.Length / 2, ExpectedEnumeratorRange.Start, true),
                            "Err_" + testDescription + " FAILED\n");

                        //[] Verify Modifying collection on an enumerator that has enumerated part of the collection
                        testDescription = "3549hkhu Verify Modifying collection on an enumerator that has enumerated part of the collection";
                        _items = _modifyCollection(_collection, _items);

                        retValue &= Test.Eval(VerifyModifiedEnumerator(enumerator, false), "Err_" + testDescription + " FAILED\n");
                    }

                    //[] Verify enumerating the entire collection
                    if (0 != _items.Length)
                    {
                        testDescription = "3874khlerd Verify enumerating the entire collection";
                        enumerator = _collection.GetEnumerator();

                        retValue &= Test.Eval(VerifyEnumerator(enumerator, _items), "Err_" + testDescription + " FAILED\n");

                        //[] Verify Modifying collection on an enumerator that has enumerated the entire collection		
                        testDescription = "55403hoa Verify Modifying collection on an enumerator that has enumerated the entire collection";
                        _items = _modifyCollection(_collection, _items);

                        retValue &= Test.Eval(VerifyModifiedEnumerator(enumerator, true), "Err_" + testDescription + " FAILED\n");
                    }

                    //[] Verify enumerating past the end of the collection
                    if (0 != _items.Length)
                    {
                        testDescription = "77564hklu Verify enumerating past the end of the collection";
                        enumerator = _collection.GetEnumerator();

                        retValue &= Test.Eval(VerifyEnumerator(enumerator, _items), "Err_" + testDescription + " FAILED\n");

                        //[] Verify Modifying collection on an enumerator that has enumerated past the end of the collection		
                        testDescription = "984uhluh Verify Modifying collection on an enumerator that has enumerated past the end of the collection";
                        _items = _modifyCollection(_collection, _items);

                        retValue &= Test.Eval(VerifyModifiedEnumerator(enumerator, true), "Err_" + testDescription + " FAILED\n");
                    }
                }
                catch (Exception e)
                {
                    retValue &= Test.Eval(false, "The following test: \n{0} threw the following exception: \n {1}", testDescription, e);
                }

                return retValue;
            }

            public bool NonGenericIEnumerable_Test()
            {
                bool retValue = true;
                IEnumerable_Test nonGenericTests;
                object[] objectItems = new object[_items.Length];

                _items.CopyTo(objectItems, 0);

                if (_modifyCollection == null)
                {
                    nonGenericTests = new IEnumerable_Test(_collection, objectItems, null);
                }
                else
                {
                    nonGenericTests = new IEnumerable_Test(_collection, objectItems, NonGenericModifyCollection);
                }

                nonGenericTests.IsGenericCompatibility = true;
                nonGenericTests.VerificationLevel = _verificationLevel;
                nonGenericTests.CollectionOrder = _collectionOrder;
                nonGenericTests.IsResetNotSupported = _isResetNotSupported;
                nonGenericTests.MoveNextAtEndThrowsOnModifiedCollection = _moveNextAtEndThrowsOnModifiedCollection;

                if (_converter == null)
                {
                    nonGenericTests.Comparer = new ObjectComparer<T>(_comparer);
                }
                else
                {
                    nonGenericTests.Comparer = new ObjectComparerWithConverter<T>(_comparer, _converter);
                }

                retValue &= nonGenericTests.RunAllTests();

                return retValue;
            }


            private bool VerifyModifiedEnumerator(IEnumerator<T> enumerator, bool atEnd)
            {
                bool retValue = true;

                //[] Verify Current
                try
                {
                    T currentItem = enumerator.Current;
                }
                catch (Exception) { }

                //[] Verify MoveNext()
                if (!atEnd || _moveNextAtEndThrowsOnModifiedCollection)
                {
                    try
                    {
                        enumerator.MoveNext();

                        retValue &= Test.Eval(false, "Err_2507poaq: MoveNext() should have thrown an exception on a modified collection");
                    }
                    catch (InvalidOperationException) { }
                    catch (Exception e)
                    {
                        retValue &= Test.Eval(false,
                            "Err_6186pypa: MoveNext() should have thrown an InvalidOperationException on a modified collection but {0} was thrown", e.GetType());
                    }
                }
                else
                {
                    // The eumerator is positioned at the end of the collection and it shouldn't throw
                    retValue &= Test.Eval(!enumerator.MoveNext(), "Err_3923lgtk: MoveNext() should have returned false at the end of the collection");
                }

                return retValue;
            }

            protected bool VerifyCollection(IEnumerable<T> collection, T[] expectedItems)
            {
                return VerifyCollection(collection, expectedItems, 0, expectedItems.Length);
            }

            protected bool VerifyCollection(IEnumerable<T> collection, T[] expectedItems, int startIndex, int count)
            {
                return VerifyEnumerator(collection.GetEnumerator(), expectedItems, startIndex, count, ExpectedEnumeratorRange.Start | ExpectedEnumeratorRange.End);
            }

            private bool VerifyEnumerator(IEnumerator<T> enumerator, T[] expectedItems)
            {
                return VerifyEnumerator(enumerator, expectedItems, 0, expectedItems.Length, ExpectedEnumeratorRange.Start | ExpectedEnumeratorRange.End);
            }

            private bool VerifyEnumerator(IEnumerator<T> enumerator, T[] expectedItems, int startIndex, int count, ExpectedEnumeratorRange expectedEnumeratorRange)
            {
                return VerifyEnumerator(enumerator, expectedItems, startIndex, count, expectedEnumeratorRange, false);
            }

            private bool VerifyEnumerator(
                IEnumerator<T> enumerator,
                T[] expectedItems,
                int startIndex,
                int count,
                ExpectedEnumeratorRange expectedEnumeratorRange,
                bool looseMatchingUnspecifiedOrder)
            {
                bool retValue = true;
                int iterations = 0;

                if ((expectedEnumeratorRange & ExpectedEnumeratorRange.Start) != 0)
                {
                    //[] Verify non deterministic behavior of current every time it is called before a call to MoveNext() has been made			
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            T tempCurrent = enumerator.Current;
                        }
                        catch (Exception) { }
                    }
                }

                if (_collectionOrder == CollectionOrder.Unspecified)
                {
                    System.Collections.BitArray itemsVisited;
                    bool itemFound;

                    if (looseMatchingUnspecifiedOrder)
                    {
                        itemsVisited = new System.Collections.BitArray(expectedItems.Length, false);
                    }
                    else
                    {
                        itemsVisited = new System.Collections.BitArray(count, false);
                    }

                    while ((iterations < count) && enumerator.MoveNext())
                    {
                        T currentItem = enumerator.Current;
                        T tempItem;

                        //[] Verify we have not gotten more items then we expected
                        retValue &= Test.Eval(iterations < count, "Err_9844awpa More items have been returned fromt the enumerator({0} items) then are " +
                                "in the expectedElements({1} items)", iterations, count);

                        //[] Verify Current returned the correct value
                        itemFound = false;

                        for (int i = 0; i < itemsVisited.Length; ++i)
                        {
                            if (!itemsVisited[i] && _comparer.Equals(currentItem, expectedItems[startIndex + i]))
                            {
                                itemsVisited[i] = true;
                                itemFound = true;
                                break;
                            }
                        }

                        retValue &= Test.Eval(itemFound, "Err_1432pauy Current returned unexpected value={0}", currentItem);

                        //[] Verify Current always returns the same value every time it is called
                        for (int i = 0; i < 3; i++)
                        {
                            tempItem = enumerator.Current;

                            retValue &= Test.Eval(_comparer.Equals(currentItem, tempItem),
                                "Err_8776phaw Current is returning inconsistant results Current returned={0} expected={1}", tempItem, currentItem);
                        }

                        iterations++;
                    }

                    if (looseMatchingUnspecifiedOrder)
                    {
                        int visitedItemsCount = 0;
                        for (int i = 0; i < itemsVisited.Length; ++i)
                        {
                            if (itemsVisited[i])
                            {
                                ++visitedItemsCount;
                            }
                        }

                        Test.Eval(count, visitedItemsCount, "Err_2398289aheid Number of items enumerator returned");
                    }
                    else
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            retValue &= Test.Eval(itemsVisited[i], "Err_052848ahiedoi Expected Current to return {0}", expectedItems[startIndex + i]);
                        }
                    }
                }
                else
                {
                    while ((iterations < count) && enumerator.MoveNext())
                    {
                        T currentItem = enumerator.Current;
                        T tempItem;

                        //[] Verify we have not gotten more items then we expected
                        retValue &= Test.Eval(iterations < count, "Err_9844awpa More items have been returned fromt the enumerator({0} items) then are " +
                                "in the expectedElements({1} items)", iterations, count);

                        //[] Verify Current returned the correct value
                        retValue &= Test.Eval(_comparer.Equals(currentItem, expectedItems[startIndex + iterations]),
                            "Err_1432pauy Current returned unexpected value={0} expected={1}", currentItem, expectedItems[startIndex + iterations]);

                        //[] Verify Current always returns the same value every time it is called
                        for (int i = 0; i < 3; i++)
                        {
                            tempItem = enumerator.Current;

                            retValue &= Test.Eval(_comparer.Equals(currentItem, tempItem),
                                "Err_8776phaw Current is returning inconsistant results Current returned={0} expected={1}", tempItem, currentItem);
                        }

                        iterations++;
                    }
                }

                retValue &= Test.Eval(count, iterations, "Err_658805eauz Number of items to iterate through");

                if ((expectedEnumeratorRange & ExpectedEnumeratorRange.End) != 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        retValue &= Test.Eval(!enumerator.MoveNext(), "Err_2929ahiea Expected MoveNext to return false after {0} iterations", iterations);
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

                return retValue;
            }

            private object[] NonGenericModifyCollection(IEnumerable collection, Object[] expectedItems)
            {
                T[] stronglyTypedExpectedItems = new T[expectedItems.Length];
                T[] stronglyTypedResult;
                Object[] result;


                Array.Copy(expectedItems, 0, stronglyTypedExpectedItems, 0, expectedItems.Length);
                stronglyTypedResult = _modifyCollection((IEnumerable<T>)collection, stronglyTypedExpectedItems);

                result = new Object[stronglyTypedResult.Length];
                Array.Copy(stronglyTypedResult, 0, result, 0, stronglyTypedResult.Length);

                return result;
            }
        }
        public class ObjectComparer<T> : IEqualityComparer<Object>
        {
            private IEqualityComparer<T> _comparer;

            public ObjectComparer(IEqualityComparer<T> comparer)
            {
                _comparer = comparer;
            }

            public new bool Equals(object x, object y) { return _comparer.Equals((T)x, (T)y); }
            public int GetHashCode(object x) { return _comparer.GetHashCode((T)x); }
        }
        public class ObjectComparerWithConverter<T> : IEqualityComparer<Object>
        {
            private IEqualityComparer<T> _comparer;
            private Converter<Object, T> _converter;

            public ObjectComparerWithConverter(IEqualityComparer<T> comparer, Converter<Object, T> converter)
            {
                _comparer = comparer;
                _converter = converter;
            }

            public new bool Equals(object x, object y) { return _comparer.Equals(Convert(x), Convert(y)); }
            public int GetHashCode(object x) { return _comparer.GetHashCode(Convert(x)); }

            private T Convert(Object item)
            {
                if (item is T)
                {
                    return (T)item;
                }
                else
                {
                    return _converter(item);
                }
            }
        }
        public class ConverterHelper
        {
            public static System.Collections.Generic.KeyValuePair<K, V> DictionaryEntryToKeyValuePairConverter<K, V>(Object item)
            {
                System.Collections.DictionaryEntry dictionaryEntry = (System.Collections.DictionaryEntry)item;
                return new System.Collections.Generic.KeyValuePair<K, V>((K)dictionaryEntry.Key, (V)dictionaryEntry.Value);
            }
        }
    }
}
