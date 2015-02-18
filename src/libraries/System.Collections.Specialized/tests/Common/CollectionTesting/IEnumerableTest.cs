// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using TestSupport;

namespace TestSupport.Collections
{
    public class IEnumerable_Test
    {
        private IEnumerable _collection;
        protected ModifyUnderlyingCollection _modifyCollection;

        protected Object[] _items;
        protected System.Collections.Generic.IEqualityComparer<Object> _comparer;
        protected VerificationLevel _verificationLevel;

        protected bool _isGenericCompatibility;
        protected bool _isResetNotSupported;

        protected CollectionOrder _collectionOrder;


        private IEnumerable_Test() { }

        /// <summary>
        /// Initializes a new instance of the IEnumerable_Test.
        /// </summary>
        /// <param name="collection">The collection to run the tests on.</param>
        /// <param name="items">The items currently in the collection.</param>
        public IEnumerable_Test(IEnumerable collection, Object[] items) : this(collection, items, null) { }

        /// <summary>
        /// Initializes a new instance of the IEnumerable_Test.
        /// </summary>
        /// <param name="collection">The collection to run the tests on.</param>
        /// <param name="items">The items currently in the collection.</param>
        /// <param name="modifyCollection">Modifies the collection to invalidate the enumerator.</param>
        public IEnumerable_Test(IEnumerable collection, Object[] items, ModifyUnderlyingCollection modifyCollection)
        {
            _collection = collection;
            _modifyCollection = modifyCollection;
            _isGenericCompatibility = false;
            _isResetNotSupported = false;
            _verificationLevel = VerificationLevel.Extensive;
            _collectionOrder = CollectionOrder.Sequential;

            if (items == null)
                _items = new Object[0];
            else
                _items = items;

            _comparer = System.Collections.Generic.EqualityComparer<Object>.Default;
        }

        /// <summary>
        /// Runs all of the IEnumerable tests.
        /// </summary>
        /// <returns>true if all of the tests passed else false</returns>
        public bool RunAllTests()
        {
            bool retValue = true;

            retValue &= MoveNext_Tests();
            retValue &= Current_Tests();
            retValue &= Reset_Tests();
            retValue &= ModifiedCollection_Test();

            return retValue;
        }

        /// <summary>
        /// The collection to run the tests on.
        /// </summary>
        /// <value>The collection to run the tests on.</value>
        public IEnumerable Collection
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
        public Object[] Items
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
        public ModifyUnderlyingCollection ModifyCollection
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
        /// The IComparer used to compare the items. If null Comparer<Object>.Default will be used.
        /// </summary>
        /// <value>The IComparer used to compare the items.</value>
        public System.Collections.Generic.IEqualityComparer<Object> Comparer
        {
            get
            {
                return _comparer;
            }
            set
            {
                if (null == value)
                    _comparer = System.Collections.Generic.EqualityComparer<Object>.Default;
                else
                    _comparer = value;
            }
        }

        /// <summary>
        /// If true specifies that IEnumerator.Current has undefined behavior when
        /// the enumerator is positioned before the first item or after the last item.
        /// </summary>
        public bool IsGenericCompatibility
        {
            get
            {
                return _isGenericCompatibility;
            }
            set
            {
                _isGenericCompatibility = value;
            }
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
        /// The Verification level to use. If VerificationLevel is Extensive the collection
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
        /// the end of the collection, or unspecified. Items is expected to be in the
        /// same order as the enumerator unless AddOrder.Unspecified is used.
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
        /// Runs all of the tests on MoveNext().
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        public bool MoveNext_Tests()
        {
            bool retValue = true;
            int iterations = 0;
            IEnumerator enumerator = _collection.GetEnumerator();
            String testDescription = "No description of test available";
            int j;
            bool scenarioResult;

            for (int i = 0; i < 3 && retValue; i++)
            {
                try
                {
                    //[] Call MoveNext() until the end of the collection has been reached
                    testDescription = "14328edfae Call MoveNext() untill the end of the collection has been reached";

                    while (enumerator.MoveNext())
                    {
                        iterations++;
                    }

                    retValue &= Test.Eval(_items.Length, iterations,
                        "Err_64897adhs Items iterated through");

                    //[] Call MoveNext() several times after the end of the collection has been reached
                    testDescription = "78083adshp Call MoveNext() several times after the end of the collection has been reached";

                    for (j = 0, scenarioResult = true; j < 3 && scenarioResult; j++)
                    {
                        try
                        {
                            Object tempCurrent = enumerator.Current;

                            retValue &= scenarioResult = Test.Eval(_isGenericCompatibility,
                                "Expected Current to throw InvalidOperationException after the end of the collection " +
                                    "has been reached and nothing was thrown Iterations({0})\n", i);
                        }
                        catch (InvalidOperationException) { }
                        catch (Exception e)
                        {
                            retValue &= scenarioResult = Test.Eval(_isGenericCompatibility,
                                "Expected Current to throw InvalidOperationException after the end of the collection " +
                                    "has been reache and the following exception was thrown Iterations({0}): \n{1}\n", i, e);
                        }

                        retValue &= scenarioResult = Test.Eval(!enumerator.MoveNext(),
                            "Expected MoveNext() to return false on the {0} after the end of the collection has been reached\n", j + 1);
                    }

                    iterations = 0;

                    if (!_isResetNotSupported)
                        enumerator.Reset();
                    else
                        enumerator = _collection.GetEnumerator();
                }
                catch (Exception e)
                {
                    retValue &= Test.Eval(false, "The following test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
                    break;
                }
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
            IEnumerator enumerator = _collection.GetEnumerator();
            String testDescription = "No description of test available";

            for (int i = 0; i < 3 && retValue; i++)
            {
                try
                {
                    //[] Call MoveNext() until the end of the collection has been reached
                    testDescription = "Call MoveNext() untill the end of the collection has been reached";

                    retValue &= Test.Eval(VerifyEnumerator(enumerator, _items), "Err_" + testDescription + " FAILED\n");

                    //[] Enumerate only part of the collection
                    testDescription = "Enumerate only part of the collection ";

                    if (_isResetNotSupported)
                        enumerator = _collection.GetEnumerator();
                    else
                        enumerator.Reset();

                    retValue &= Test.Eval(VerifyEnumerator(enumerator, _items, 0, _items.Length / 2, ExpectedEnumeratorRange.Start, true), "Err_" + testDescription + " FAILED\n");

                    //[] After Enumerating only part of the collection call Reset() and enumerate through the entire collection
                    testDescription = "After Enumerating only part of the collection call Reset() and enumerate through the entire collection";

                    if (_isResetNotSupported)
                        enumerator = _collection.GetEnumerator();
                    else
                        enumerator.Reset();

                    retValue &= Test.Eval(VerifyEnumerator(enumerator, _items), "Err_" + testDescription + " FAILED\n");

                    //[] Reset the enumerator for the next iteration
                    if (_isResetNotSupported)
                        enumerator = _collection.GetEnumerator();
                    else
                        enumerator.Reset();
                }
                catch (Exception e)
                {
                    retValue &= Test.Eval(false, "The following test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
                    break;
                }
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the tests on Reset().
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        public bool Reset_Tests()
        {
            bool retValue = true;
            IEnumerator enumerator = _collection.GetEnumerator();
            String testDescription = "No description of test available";

            for (int i = 0; i < 3 && retValue; i++)
            {
                try
                {
                    if (!_isResetNotSupported)
                    {
                        //[] Call Reset() several times on a new Enumerator then enumerate the collection
                        testDescription = "Call Reset() several times on a new Enumerator";
                        retValue &= VerifyEnumerator(enumerator, _items);
                        enumerator.Reset();

                        //[] Enumerate part of the collection then call Reset() several times
                        testDescription = "Enumerate part of the collection then call Reset() several times";

                        retValue &= Test.Eval(VerifyEnumerator(enumerator, _items, 0, _items.Length / 2, ExpectedEnumeratorRange.Start, true), "Err_" + testDescription + " FAILED\n");

                        enumerator.Reset();
                        enumerator.Reset();
                        enumerator.Reset();

                        //[] After Enumerating only part of the collection and Reset() was called several times and enumerate through the entire collection
                        testDescription = "After Enumerating only part of the collection and Reset() was called several times and enumerate through the entire collection";

                        retValue &= Test.Eval(VerifyEnumerator(enumerator, _items), "Err_" + testDescription + " FAILED\n");

                        enumerator.Reset();

                        //[] Enumerate the entire collection then call Reset() several times
                        testDescription = "Enumerate the entire collection then call Reset() several times";

                        retValue &= Test.Eval(VerifyEnumerator(enumerator, _items), "Err_" + testDescription + " FAILED\n");

                        enumerator.Reset();
                        enumerator.Reset();
                        enumerator.Reset();

                        //[] After Enumerating the entire collection and Reset() was called several times and enumerate through the entire collection
                        testDescription = "After Enumerating the entire collection and Reset() was called several times and enumerate through the entire collection";

                        retValue &= Test.Eval(VerifyEnumerator(enumerator, _items), "Err_" + testDescription + " FAILED\n");

                        enumerator.Reset();
                    }
                    else
                    {
                        //[] Call Reset() several times on a new Enumerator then enumerate the collection
                        testDescription = "Call Reset() several times on a new Enumerator";
                        int j = 0;
                        bool scenarioResult;

                        for (j = 0, scenarioResult = true; j < 3 && scenarioResult; ++j)
                        {

                            retValue &= scenarioResult = Test.Eval(Test.VerifyException<InvalidOperationException>(new ExceptionGenerator(enumerator.Reset)),
                                "Verify Reset() Iteration:{0} FAILED", j);
                        }

                        retValue &= Test.Eval(VerifyEnumerator(enumerator, _items),
                            "Err_" + testDescription + " FAILED\n Expected Reset to throw InvalidOperationException\n");

                        //[] Enumerate only part of the collection
                        testDescription = "Enumerate only part of the collection ";
                        enumerator = _collection.GetEnumerator();

                        retValue &= Test.Eval(VerifyEnumerator(enumerator, _items, 0, _items.Length / 2, ExpectedEnumeratorRange.Start | ExpectedEnumeratorRange.End, true), "Err_" + testDescription + " FAILED\n");

                        //[] After Enumerating only part of the collection call Reset() and enumerate through the entire collection
                        testDescription = "After Enumerating only part of the collection call Reset() and enumerate through the entire collection";
                        for (j = 0; j < 3; ++j)
                        {
                            retValue &= Test.Eval(Test.VerifyException<InvalidOperationException>(new ExceptionGenerator(enumerator.Reset)),
                                "Verify Reset() Iteration:{0} FAILED", j);
                        }

                        retValue &= Test.Eval(VerifyEnumerator(enumerator, _items, _items.Length / 2, _items.Length - (_items.Length / 2), ExpectedEnumeratorRange.Start | ExpectedEnumeratorRange.End, true),
                            "Err_" + testDescription + " Reset is Supported FAILED\n");

                        //[] Enumerate the entire collection then call Reset() several times
                        testDescription = "Enumerate the entire collection then call Reset() several times";
                        enumerator = _collection.GetEnumerator();
                        retValue &= Test.Eval(VerifyEnumerator(enumerator, _items), "Err_" + testDescription + " FAILED\n");

                        for (j = 0; j < 3; ++j)
                        {
                            retValue &= Test.Eval(Test.VerifyException<InvalidOperationException>(new ExceptionGenerator(enumerator.Reset)),
                                "Verify Reset() Iteration:{0} FAILED", j);
                        }

                        //[] After Enumerating the entire collection and Reset() was called several times and enumerate through the entire collection
                        testDescription = "After Enumerating the entire collection and Reset() was called several times and enumerate through the entire collection";
                        retValue &= Test.Eval(VerifyEnumerator(enumerator, _items, 0, 0, ExpectedEnumeratorRange.End, true), "Err_" + testDescription + " FAILED\n");

                        enumerator = _collection.GetEnumerator();
                    }
                }
                catch (Exception e)
                {
                    retValue &= Test.Eval(false, "The following test: \n{0} threw the following exception: \n {1}", testDescription, e);
                    break;
                }
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
            IEnumerator enumerator;
            String testDescription = "No description of test available";
            Object currentItem;

            try
            {
                if (null == _modifyCollection)
                {
                    //We have no way to modify the collection so we will just have to return true;
                    return true;
                }

                //[] Verify Modifying collecton with new Enumerator
                testDescription = "Verify Modifying collecton with new Enumerator";
                enumerator = _collection.GetEnumerator();
                _items = _modifyCollection(_collection, _items);

                retValue &= Test.Eval(VerifyModifiedEnumerator(enumerator), "Err_" + testDescription + " FAILED\n");

                //[] Verify enumerating to the first item
                //We can only do this test if there is more then 1 item in it
                //If the collection only has one item in it we will enumerate
                //to the first item in the test "Verify enumerating the entire collection"
                if (1 < _items.Length)
                {
                    testDescription = "Verify enumerating to the first item";
                    enumerator = _collection.GetEnumerator();

                    retValue &= Test.Eval(VerifyEnumerator(enumerator, _items, 0, 1,
                        1 < _items.Length ? ExpectedEnumeratorRange.Start : ExpectedEnumeratorRange.Start | ExpectedEnumeratorRange.End, true),
                        "Err_" + testDescription + " FAILED\n");

                    //[] Verify Modifying collection on an enumerator that has enumerated to the first item in the collection
                    testDescription = "Verify Modifying collection on an enumerator that has enumerated to the first item in the collection";
                    currentItem = enumerator.Current;
                    _items = _modifyCollection(_collection, _items);

                    retValue &= Test.Eval(VerifyModifiedEnumerator(enumerator, currentItem), "Err_" + testDescription + " FAILED\n");
                }

                //[] Verify enumerating part of the collection
                //We can only do this test if there is more then 1 item in it
                //If the collection only has one item in it we will enumerate
                //to the first item in the test "Verify enumerating the entire collection"
                if (1 < _items.Length)
                {
                    testDescription = "Verify enumerating part of the collection";
                    enumerator = _collection.GetEnumerator();

                    retValue &= Test.Eval(VerifyEnumerator(enumerator, _items, 0, _items.Length / 2, ExpectedEnumeratorRange.Start, true), "Err_" + testDescription + " FAILED\n");

                    //[] Verify Modifying collection on an enumerator that has enumerated part of the collection
                    testDescription = " Verify Modifying collection on an enumerator that has enumerated part of the collection";
                    currentItem = enumerator.Current;
                    _items = _modifyCollection(_collection, _items);


                    retValue &= Test.Eval(VerifyModifiedEnumerator(enumerator, currentItem), "Err_" + testDescription + " FAILED\n");
                }

                //[] Verify enumerating the entire collection
                if (0 != _items.Length)
                {
                    testDescription = "Verify enumerating the entire collection";
                    enumerator = _collection.GetEnumerator();

                    retValue &= Test.Eval(VerifyEnumerator(enumerator, _items, 0, _items.Length, ExpectedEnumeratorRange.Start), "Err_" + testDescription + " FAILED\n");

                    //[] Verify Modifying collection on an enumerator that has enumerated the entire collection
                    testDescription = "Verify Modifying collection on an enumerator that has enumerated the entire collection";
                    currentItem = enumerator.Current;
                    _items = _modifyCollection(_collection, _items);

                    retValue &= Test.Eval(VerifyModifiedEnumerator(enumerator, currentItem), "Err_" + testDescription + " FAILED\n");
                }

                //[] Verify enumerating past the end of the collection
                if (0 != _items.Length)
                {
                    testDescription = "Verify enumerating past the end of the collection";
                    enumerator = _collection.GetEnumerator();

                    retValue &= Test.Eval(VerifyEnumerator(enumerator, _items), "Err_" + testDescription + " FAILED\n");

                    //[] Verify Modifying collection on an enumerator that has enumerated past the end of the collection
                    testDescription = "Verify Modifying collection on an enumerator that has enumerated past the end of the collection";
                    currentItem = null;
                    _items = _modifyCollection(_collection, _items);

                    retValue &= Test.Eval(VerifyModifiedEnumerator(enumerator, currentItem, true), "Err_" + testDescription + " FAILED\n");
                }
            }
            catch (Exception e)
            {
                retValue &= Test.Eval(false, "The following test: \n{0} threw the following exception: \n {1}", testDescription, e);
            }

            return retValue;
        }

        private bool VerifyModifiedEnumerator(IEnumerator enumerator)
        {
            return VerifyModifiedEnumerator(enumerator, null, true);
        }

        private bool VerifyModifiedEnumerator(IEnumerator enumerator, Object expectedCurrent)
        {
            return VerifyModifiedEnumerator(enumerator, expectedCurrent, false);
        }

        private bool VerifyModifiedEnumerator(IEnumerator enumerator, Object expectedCurrent, bool expectCurrentThrow)
        {
            bool retValue = true;
            int i;
            bool scenarioResult;

            //[] Verify Current
            try
            {
                Object currentItem = enumerator.Current;

                if (expectCurrentThrow)
                {
                    retValue &= Test.Eval(_isGenericCompatibility, "Current should have thrown an exception on a modified collection");
                }
                else
                {
                    //[] Verify Current always returns the same value every time it is called
                    for (i = 0, scenarioResult = true; i < 3 && scenarioResult; i++)
                    {
                        retValue &= scenarioResult = Test.Eval(_comparer.Equals(expectedCurrent, currentItem),
                            "Current is returning inconsistant results Current returned={0} expected={1} Iteration({2})",
                            currentItem, expectCurrentThrow, i);
                        currentItem = enumerator.Current;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                retValue &= Test.Eval(expectCurrentThrow, "Did not expect Current to thow InvalidOperationException");
            }
            catch (Exception e)
            {
                retValue &= Test.Eval(!_isGenericCompatibility || !expectCurrentThrow,
                    "Current should have thrown an InvalidOperationException on a modified collection but the following was thrown:\n{0}", e);
            }

            //[] Verify MoveNext()
            try
            {
                enumerator.MoveNext();
                retValue &= Test.Eval(false, "MoveNext() should have thrown an exception on a modified collection");
            }
            catch (InvalidOperationException) { }
            catch (Exception e)
            {
                retValue &= Test.Eval(false, "MoveNext() should have thrown an InvalidOperationException on a modified collection but the following was thrown:\n{0}", e);
            }

            //[] Verify Reset()
            try
            {
                enumerator.Reset();
                retValue &= Test.Eval(false, "Reset() should have thrown an exception on a modified collection");
            }
            catch (InvalidOperationException) { }
            catch (Exception e)
            {
                retValue &= Test.Eval(false, "Reset() should have thrown an InvalidOperationException on a modified collection but the following was thrown:\n{0}", e);
            }

            return retValue;
        }

        protected bool VerifyCollection(IEnumerable collection, Object[] expectedItems)
        {
            return VerifyCollection(collection, expectedItems, 0, expectedItems.Length);
        }

        protected bool VerifyCollection(IEnumerable collection, Object[] expectedItems, int startIndex, int count)
        {
            return VerifyEnumerator(collection.GetEnumerator(), expectedItems, startIndex, count, ExpectedEnumeratorRange.Start | ExpectedEnumeratorRange.End);
        }

        private bool VerifyEnumerator(IEnumerator enumerator, Object[] expectedItems)
        {
            return VerifyEnumerator(enumerator, expectedItems, 0, expectedItems.Length, ExpectedEnumeratorRange.Start | ExpectedEnumeratorRange.End);
        }

        private bool VerifyEnumerator(IEnumerator enumerator, Object[] expectedItems, int startIndex, int count)
        {
            return VerifyEnumerator(enumerator, expectedItems, startIndex, count, ExpectedEnumeratorRange.Start | ExpectedEnumeratorRange.End);
        }

        private bool VerifyEnumerator(
            IEnumerator enumerator,
            Object[] expectedItems,
            int startIndex,
            int count,
            ExpectedEnumeratorRange expectedEnumeratorRange)
        {
            return VerifyEnumerator(enumerator, expectedItems, startIndex, count, expectedEnumeratorRange, false);
        }

        private bool VerifyEnumerator(
            IEnumerator enumerator,
            Object[] expectedItems,
            int startIndex,
            int count,
            ExpectedEnumeratorRange expectedEnumeratorRange,
            bool looseMatchingUnspecifiedOrder)
        {
            bool retValue = true;
            int iterations = 0;
            int i;
            bool scenarioResult;

            //[] Verify Current throws every time it is called before a call to MoveNext() has been made
            if ((expectedEnumeratorRange & ExpectedEnumeratorRange.Start) != 0)
            {
                for (i = 0, scenarioResult = true; i < 3 && scenarioResult; i++)
                {
                    try
                    {
                        Object tempCurrent = enumerator.Current;

                        retValue &= scenarioResult = Test.Eval(_isGenericCompatibility,
                            "Expected Current to throw InvalidOperationException before MoveNext() " +
                            "has been called and nothing was thrown Iterations({0})", i);
                    }
                    catch (InvalidOperationException) { }
                    catch (Exception e)
                    {
                        retValue &= scenarioResult = Test.Eval(_isGenericCompatibility,
                            "Expected Current to throw InvalidOperationException before MoveNext() " +
                            "has been called and the following exception was thrown Iterations({0}): \n{1}", i, e);
                    }
                }
            }

            scenarioResult = true;

            if (_collectionOrder == CollectionOrder.Unspecified)
            {
                System.Collections.Generic.List<Boolean> itemsVisited;
                bool itemFound;

                if (looseMatchingUnspecifiedOrder)
                {
                    itemsVisited = new System.Collections.Generic.List<Boolean>(expectedItems.Length);
                }
                else
                {
                    itemsVisited = new System.Collections.Generic.List<Boolean>(count);
                }

                for (i = 0; i < itemsVisited.Count; i++) itemsVisited[i] = false;

                while ((iterations < count) && enumerator.MoveNext())
                {
                    Object currentItem = enumerator.Current;
                    Object tempItem;

                    //[] Verify we have not gotten more items then we expected
                    retValue &= Test.Eval(iterations < count, "More items have been returned fromt the enumerator({0} items) then are " +
                            "in the expectedElements({1} items)", iterations, count);

                    //[] Verify Current returned the correct value
                    itemFound = false;

                    for (i = 0; i < itemsVisited.Count; ++i)
                    {
                        if (!itemsVisited[i] && _comparer.Equals(currentItem, expectedItems[startIndex + i]))
                        {
                            itemsVisited[i] = true;
                            itemFound = true;
                            break;
                        }
                    }

                    retValue &= Test.Eval(itemFound, "Current returned unexpected value={0}", currentItem);

                    //[] Verify Current always returns the same value every time it is called
                    for (i = 0; i < 3; i++)
                    {
                        tempItem = enumerator.Current;

                        retValue &= Test.Eval(_comparer.Equals(currentItem, tempItem),
                            "Current is returning inconsistant results Current returned={0} expected={1}", tempItem, currentItem);
                    }

                    iterations++;
                }


                if (looseMatchingUnspecifiedOrder)
                {
                    int visitedItemsCount = 0;
                    for (i = 0; i < itemsVisited.Count; ++i)
                    {
                        if (itemsVisited[i])
                        {
                            ++visitedItemsCount;
                        }
                    }

                    Test.Eval(count, visitedItemsCount, "Number of items enumerator returned");
                }
                else
                {
                    for (i = 0; i < count; ++i)
                    {
                        retValue &= Test.Eval(itemsVisited[i], "Expected Current to return {0}", expectedItems[startIndex + i]);
                    }
                }
            }
            else
            {
                while ((iterations < count) && enumerator.MoveNext())
                {
                    Object currentItem = enumerator.Current;
                    Object tempItem;

                    //[] Verify we have not gotten more items then we expected
                    retValue &= Test.Eval(iterations < count, "More items have been returned fromt the enumerator({0} items) then are " +
                            "in the expectedElements({1} items)", iterations, count);

                    //[] Verify Current returned the correct value
                    retValue &= Test.Eval(_comparer.Equals(currentItem, expectedItems[startIndex + iterations]),
                        "Current returned unexpected value={0} expected={1}", currentItem, expectedItems[startIndex + iterations]);

                    //[] Verify Current always returns the same value every time it is called
                    for (i = 0; i < 3; i++)
                    {
                        tempItem = enumerator.Current;

                        retValue &= Test.Eval(_comparer.Equals(currentItem, tempItem),
                            "Current is returning inconsistant results Current returned={0} expected={1}", tempItem, currentItem);
                    }

                    iterations++;
                }
            }

            retValue &= Test.Eval(count, iterations, "Items iterated through");

            if ((expectedEnumeratorRange & ExpectedEnumeratorRange.End) != 0)
            {
                //[] Verify MoveNext returns false  every time it is called after the end of the collection has been reached
                for (i = 0, scenarioResult = true; i < 3 && scenarioResult; i++)
                {
                    retValue &= scenarioResult &= Test.Eval(!enumerator.MoveNext(), "Expected MoveNext to return false iteration {0}", i);
                }

                //[] Verify Current throws every time it is called after the end of the collection has been reached
                for (i = 0, scenarioResult = true; i < 3 && scenarioResult; i++)
                {
                    try
                    {
                        Object tempCurrent = enumerator.Current;

                        retValue &= scenarioResult = Test.Eval(_isGenericCompatibility,
                            "Expected Current to throw InvalidOperationException after the end of the collection " +
                            "has been reached and nothing was thrown Iterations({0})", i);
                    }
                    catch (InvalidOperationException) { }
                    catch (Exception e)
                    {
                        retValue &= scenarioResult = Test.Eval(_isGenericCompatibility,
                            "Expected Current to throw InvalidOperationException after the end of the collection " +
                            "has been reache and the following exception was thrown Iterations({0}): \n{1}", i, e);
                    }
                }
            }

            return retValue;
        }
    }
}
