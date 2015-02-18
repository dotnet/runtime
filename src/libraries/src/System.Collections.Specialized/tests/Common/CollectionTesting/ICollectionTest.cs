// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using TestSupport;

namespace TestSupport.Collections
{
    public class ICollection_Test : IEnumerable_Test
    {
        private ICollection _collection;

        private bool _expectedIsSynchronized;
        private CreateNewICollection _createNewCollection;
        private Type[] _validArrayTypes;
        private Type[] _invalidArrayTypes;
        private bool _copyToOnlySupportsZeroLowerBounds;

        private ICollection_Test() : base(null, null) { }

        /// <summary>
        /// Initializes a new instance of the ICollection_Test.
        /// </summary>
        /// <param name="collection">The collection to run the tests on.</param>
        /// <param name="items">The items currently in the collection.</param>
        /// <param name="expectedIsSynchronized">The expected value of IsSynchronized</param>
        public ICollection_Test(ICollection collection, Object[] items, bool expectedIsSynchronized) : this(collection, items, expectedIsSynchronized, null) { }

        /// <summary>
        /// Initializes a new instance of the ICollection_Test.
        /// </summary>
        /// <param name="collection">The collection to run the tests on.</param>
        /// <param name="items">The items currently in the collection.</param>
        /// <param name="expectedIsSynchronized">The expected value of IsSynchronized</param>
        /// <param name="createNewCollection">Creates a new Collection. This is used to verify
        /// that SyncRoot returns different values from different collections.</param>
        public ICollection_Test(ICollection collection, Object[] items, bool expectedIsSynchronized, CreateNewICollection createNewCollection) : base(collection, items)
        {
            _collection = collection;

            _expectedIsSynchronized = expectedIsSynchronized;
            _createNewCollection = createNewCollection;
            _validArrayTypes = new Type[] { typeof(Object) };
            _invalidArrayTypes = new Type[] { typeof(MyInvalidReferenceType), typeof(MyInvalidValueType) };
            _copyToOnlySupportsZeroLowerBounds = false;
        }

        /// <summary>
        /// Runs all of the ICollection tests.
        /// </summary>
        /// <returns>true if all of the tests passed else false</returns>
        new public bool RunAllTests()
        {
            bool retValue = true;

            retValue &= Count_Tests();
            retValue &= IsSynchronized_Tests();
            retValue &= SyncRoot_Tests();
            retValue &= CopyTo_Tests();
            retValue &= base.RunAllTests();

            return retValue;
        }

        /// <summary>
        /// The collection to run the tests on.
        /// </summary>
        /// <value>The collection to run the tests on.</value>
        new public ICollection Collection
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
        /// Creates a new Collection. This is used to verify
        /// that SyncRoot returns different values from different collections.
        /// If this is null the test that use this will not be run.
        /// </summary>
        /// <value>Creates a new Collection.</value>
        public CreateNewICollection CreateNewCollection
        {
            get
            {
                return _createNewCollection;
            }
            set
            {
                _createNewCollection = value;
            }
        }

        /// <summary>
        /// Specifies the types that are valid with CopyTo(Array).
        /// By default this is a Type array containing only typeof(Object).
        /// </summary>
        /// <value>Specifies the types that are valid with CopyTo(Array)</value>
        public Type[] ValidArrayTypes
        {
            get
            {
                return _validArrayTypes;
            }
            set
            {
                if (value == null)
                    _validArrayTypes = new Type[0];

                _validArrayTypes = value;
            }
        }

        /// <summary>
        /// Specifies the types that are not valid with CopyTo(Array).
        /// By default this is a Type array containing only
        /// typeof(MyInvalidReferenceType) and typeof(MyInvalidValueType).
        /// </summary>
        /// <value>Specifies the types that are invalid with CopyTo(Array)</value>
        public Type[] InvalidArrayTypes
        {
            get
            {
                return _invalidArrayTypes;
            }
            set
            {
                if (value == null)
                    _invalidArrayTypes = new Type[0];

                _invalidArrayTypes = value;
            }
        }

        /// <summary>
        /// Specifies if CopyTo(Array) only supports Arrays with a zero lower bound.
        /// </summary>
        /// <value></value>
        public bool CopyToOnlySupportsZeroLowerBounds
        {
            get
            {
                return _copyToOnlySupportsZeroLowerBounds;
            }
            set
            {
                _copyToOnlySupportsZeroLowerBounds = value;
            }
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
                testDescription = "Verify Count returns the expected value";

                retValue &= Test.Eval(_items.Length, _collection.Count, "Error Count");
            }
            catch (Exception e)
            {
                retValue &= Test.Eval(false, "The following Count test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the tests on IsSynchronized.
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        public bool IsSynchronized_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";

            try
            {
                //[] Verify IsSynchronized returns the expected value
                testDescription = "3458ahoh Verify IsSynchronized returns the expected value";

                retValue &= Test.Eval(_expectedIsSynchronized, _collection.IsSynchronized, "Err IsSynchronized");
            }
            catch (Exception e)
            {
                retValue &= Test.Eval(false, "The following IsSynchronized test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the tests on MoveNext().
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        public bool SyncRoot_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";

            try
            {
                //[] Verify SyncRoot is not null
                testDescription = "Verify SyncRoot is not null";

                retValue &= Test.Eval(null != _collection.SyncRoot, "Err SyncRoot is null");

                //[] Verify SyncRoot returns consistent results
                testDescription = "Verify SyncRoot returns consistent results";
                Object syncRoot1 = _collection.SyncRoot;
                Object syncRoot2 = _collection.SyncRoot;

                retValue &= Test.Eval(null != syncRoot1, "Err SyncRoot is null");

                retValue &= Test.Eval(syncRoot1 == syncRoot2, "Err SyncRoot is did not return the same result");

                //[] Verify SyncRoot can be used in a lock statement
                testDescription = "Verify SyncRoot can be used in a lock statement";
                lock (_collection.SyncRoot)
                {
                }

                //[] Verify that calling SyncRoot on different collections returns different values
                testDescription = "Verify that calling SyncRoot on different collections returns different values";
                if (null != _createNewCollection)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        ICollection c = _createNewCollection();

                        if (!(retValue &= Test.Eval(null != c, "CreateNewCollection returned null")))
                        {
                            retValue &= Test.Eval(_collection.SyncRoot.Equals(c.SyncRoot),
                                "SyncRoot returned on this collection and on another collection returned the same value iteration={0}", i);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                retValue &= Test.Eval(false, "The following SyncRoot test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
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

            retValue &= CopyTo_Argument_Tests();
            retValue &= CopyTo_Valid_Tests();

            return retValue;
        }

        /// <summary>
        /// Runs all of the invalid(argument checking) tests on CopyTo(Array).
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        public bool CopyTo_Argument_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";
            Object[] itemObjectArray = null, tempItemObjectArray = null;
            Array itemArray = null, tempItemArray = null;

            try
            {
                //[] Verify CopyTo with null array
                testDescription = "Verify CopyTo with null array";
                retValue &= Test.Eval(Test.VerifyException<ArgumentNullException>(delegate () { _collection.CopyTo(null, 0); }),
                    "ErrExpected ArgumentNullException with null array");

                // [] Verify CopyTo with index=Int32.MinValue
                testDescription = "Verify CopyTo with index=Int32.MinValue";
                itemObjectArray = GenerateArray(_collection.Count);
                tempItemObjectArray = (Object[])itemObjectArray.Clone();

                retValue &= Test.Eval(Test.VerifyException<ArgumentOutOfRangeException>(delegate () { _collection.CopyTo(itemObjectArray, Int32.MinValue); }),
                    "ErrException not thrown with index=Int32.MinValue");

                retValue &= Test.Eval(VerifyItems(itemObjectArray, tempItemObjectArray, VerificationLevel.Extensive),
                    "Err_" + testDescription + " verifying item array FAILED");

                // [] Verify CopyTo with index=-1
                testDescription = "Verify CopyTo with index=-1";
                itemObjectArray = GenerateArray(_collection.Count);
                tempItemObjectArray = (Object[])itemObjectArray.Clone();

                retValue &= Test.Eval(Test.VerifyException<ArgumentOutOfRangeException>(delegate () { _collection.CopyTo(itemObjectArray, -1); }),
                    "ErrException not thrown with index=-1");

                retValue &= Test.Eval(VerifyItems(itemObjectArray, tempItemObjectArray, VerificationLevel.Extensive),
                    "Err_" + testDescription + " verifying item array FAILED");


                // [] Verify CopyTo with index=Int32.MaxValue
                testDescription = "3987ahso Verify CopyTo with index=Int32.MaxValue";
                itemObjectArray = GenerateArray(_collection.Count);
                tempItemObjectArray = (Object[])itemObjectArray.Clone();

                retValue &= Test.Eval(Test.VerifyException<ArgumentException>(delegate () { _collection.CopyTo(itemObjectArray, Int32.MaxValue); }),
                    "ErrException not thrown with index=Int32.MaxValue");

                retValue &= Test.Eval(VerifyItems(itemObjectArray, tempItemObjectArray, VerificationLevel.Extensive),
                    "Err_" + testDescription + " verifying item array FAILED");


                if (0 < _collection.Count)
                {
                    // [] Verify CopyTo with index=array.length
                    testDescription = "Verify CopyTo with index=array.Length";
                    itemObjectArray = GenerateArray(_collection.Count);
                    tempItemObjectArray = (Object[])itemObjectArray.Clone();

                    retValue &= Test.Eval(Test.VerifyException<ArgumentException>(delegate () { _collection.CopyTo(itemObjectArray, _collection.Count); }),
                        "Err Exception not throw with index=array.Length");

                    retValue &= Test.Eval(VerifyItems(itemObjectArray, tempItemObjectArray, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED");
                }

                if (1 < _collection.Count)
                {
                    // [] Verify CopyTo with collection.Count > array.length - index
                    testDescription = "Verify CopyTo with collection.Count > array.length - index";
                    itemObjectArray = GenerateArray(_collection.Count + 1);
                    tempItemObjectArray = (Object[])itemObjectArray.Clone();

                    retValue &= Test.Eval(Test.VerifyException<ArgumentException>(delegate () { _collection.CopyTo(itemObjectArray, 2); }),
                        "Err Exception not thrown with collection.Count > array.length - index");

                    retValue &= Test.Eval(VerifyItems(itemObjectArray, tempItemObjectArray, VerificationLevel.Extensive),
                        "Err_" + testDescription + " verifying item array FAILED");
                }

                // [] Verify CopyTo with array is multidimensional
                testDescription = "Verify CopyTo with array is multidimensional";
                retValue &= Test.Eval(Test.VerifyException<ArgumentException>(delegate () { _collection.CopyTo(new Object[1, _collection.Count], 0); }),
                    "Err Exception not thrown with multidimensional array");

                if (0 != _items.Length)
                {
                    //[] Verify CopyTo with invalid types
                    testDescription = "5688apied Verify CopyTo with invalid types";
                    Type invalidCastExceptionType = _isGenericCompatibility ? null : typeof(InvalidCastException);

                    for (int i = 0; i < _invalidArrayTypes.Length; ++i)
                    {
                        itemArray = Array.CreateInstance(_invalidArrayTypes[i], _collection.Count);
                        tempItemArray = (Array)itemArray.Clone();

                        retValue &= Test.Eval(Test.VerifyException(typeof(ArgumentException), invalidCastExceptionType,
                            delegate () { _collection.CopyTo(itemArray, 0); }),
                            "Err Exception not thrown invalid array type {0} iteration:{1}", _invalidArrayTypes[i], i);

                        retValue &= Test.Eval(VerifyItems(itemArray, tempItemArray, VerificationLevel.Extensive),
                            "Err_" + testDescription + " verifying item array FAILED");
                    }
                }
            }
            catch (Exception e)
            {
                retValue &= Test.Eval(false, "The following CopyTo test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        /// <summary>
        /// Runs all of the valid test on CopyTo(Array).
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        public bool CopyTo_Valid_Tests()
        {
            bool retValue = true;
            String testDescription = "No description of test available";
            Object[] itemObjectArray = null, tempItemObjectArray = null;

            try
            {
                // [] CopyTo with index=0 and the array is 4 items larger then size as the collection
                testDescription = "CopyTo with index=0 and the array is 4 items larger then size as the collection";
                itemObjectArray = GenerateArrayWithRandomItems(_items.Length + 4);
                tempItemObjectArray = new Object[_items.Length + 4];

                Array.Copy(itemObjectArray, tempItemObjectArray, itemObjectArray.Length);
                Array.Copy(_items, 0, tempItemObjectArray, 0, _items.Length);
                _collection.CopyTo(itemObjectArray, 0);

                retValue &= Test.Eval(VerifyItems(itemObjectArray, tempItemObjectArray), "Err_" + testDescription + " FAILED");

                // [] CopyTo with index=4 and the array is 4 items larger then size as the collection
                testDescription = "CopyTo with index=4 and the array is 4 items larger then size as the collection";
                itemObjectArray = GenerateArrayWithRandomItems(_items.Length + 4);
                tempItemObjectArray = new Object[_items.Length + 4];

                Array.Copy(itemObjectArray, tempItemObjectArray, itemObjectArray.Length);
                Array.Copy(_items, 0, tempItemObjectArray, 4, _items.Length);
                _collection.CopyTo(itemObjectArray, 4);

                retValue &= Test.Eval(VerifyItems(itemObjectArray, tempItemObjectArray), "Err_" + testDescription + " FAILED");

                // [] CopyTo with index=4 and the array is 8 items larger then size as the collection
                testDescription = "CopyTo with index=4 and the array is 8 items larger then size as the collection";
                itemObjectArray = GenerateArrayWithRandomItems(_items.Length + 8);
                tempItemObjectArray = new Object[_items.Length + 8];

                Array.Copy(itemObjectArray, tempItemObjectArray, itemObjectArray.Length);
                Array.Copy(_items, 0, tempItemObjectArray, 4, _items.Length);
                _collection.CopyTo(itemObjectArray, 4);

                retValue &= Test.Eval(VerifyItems(itemObjectArray, tempItemObjectArray), "Err_" + testDescription + " FAILED");

                //[] Verify CopyTo with valid types
                testDescription = "Verify CopyTo with valid types";
                for (int i = 0; i < _validArrayTypes.Length; ++i)
                {
                    _collection.CopyTo(Array.CreateInstance(_validArrayTypes[i], _collection.Count), 0);

                    retValue &= Test.Eval(VerifyItems(itemObjectArray, tempItemObjectArray), "Err_" + testDescription + " FAILED");
                }
            }
            catch (Exception e)
            {
                retValue &= Test.Eval(false, "The following CopyTo test: \n{0} threw the following exception: \n {1}\n", testDescription, e);
            }

            return retValue;
        }

        private Object[] GenerateArrayWithRandomItems(int length)
        {
            Object[] items = new Object[length];
            Random rndGen = new Random(-55);

            for (int i = 0; i < items.Length; i++)
            {
                items[i] = rndGen.Next();
            }

            return items;
        }


        protected bool VerifyCollection(ICollection collection, Object[] items)
        {
            return VerifyCollection(collection, items, 0, items.Length, VerificationLevel.Normal);
        }

        private bool VerifyCollection(ICollection collection, Object[] items, VerificationLevel verificationLevel)
        {
            return VerifyCollection(collection, items, 0, items.Length, verificationLevel);
        }


        protected bool VerifyCollection(ICollection collection, Object[] items, int index, int count)
        {
            return VerifyCollection(collection, items, index, count, VerificationLevel.Normal);
        }

        private bool VerifyCollection(ICollection collection, Object[] items, int index, int count, VerificationLevel verificationLevel)
        {
            bool retValue = true;

            if (verificationLevel <= _verificationLevel)
            {
                retValue &= Verify_Count(collection, items, index, count);
                retValue &= Verify_CopyTo(collection, items, index, count);
                retValue &= base.VerifyCollection(collection, items, index, count);
            }

            return retValue;
        }

        private bool VerifyItems(Object[] actualItems, Object[] expectedItems)
        {
            return VerifyItems(actualItems, expectedItems, VerificationLevel.Normal);
        }

        private bool VerifyItems(Object[] actualItems, Object[] expectedItems, VerificationLevel verificationLevel)
        {
            if (verificationLevel <= _verificationLevel)
            {
                if (!Test.Eval(expectedItems.Length, actualItems.Length,
                    "The length of the items"))
                {
                    return false;
                }

                for (int i = 0; i < expectedItems.Length; i++)
                {
                    if (!Test.Eval(_comparer.Equals(actualItems[i], expectedItems[i]),
                        "The actual item and expected items differ at {0} actual={1} expected={2}",
                        i, actualItems[i], expectedItems[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return true;
        }

        private bool VerifyItems(Array actualItems, Array expectedItems)
        {
            return VerifyItems(actualItems, expectedItems, VerificationLevel.Normal);
        }

        private bool VerifyItems(Array actualItems, Array expectedItems, VerificationLevel verificationLevel)
        {
            if (verificationLevel <= _verificationLevel)
            {
                if (!Test.Eval(expectedItems.Length, actualItems.Length,
                    "The length of the items"))
                {
                    return false;
                }

                int actualItemsLowerBounds = actualItems.GetLowerBound(0);
                int expectedItemsLowerBounds = expectedItems.GetLowerBound(0);

                for (int i = 0; i < expectedItems.Length; i++)
                {
                    if (!Test.Eval(_comparer.Equals(actualItems.GetValue(i + actualItemsLowerBounds), expectedItems.GetValue(i + expectedItemsLowerBounds)),
                        "The actual item and expected items differ at {0} actual={1} expected={2}",
                        i, actualItems.GetValue(i + actualItemsLowerBounds), expectedItems.GetValue(i + expectedItemsLowerBounds)))
                    {
                        return false;
                    }
                }

                return true;
            }

            return true;
        }

        private bool Verify_Count(ICollection collection, Object[] items, int index, int count)
        {
            return Test.Eval(count, collection.Count, "Verifying Count");
        }

        private bool Verify_CopyTo(ICollection collection, Object[] items, int index, int count)
        {
            bool retValue = true;

            if ((retValue &= Test.Eval(count, collection.Count,
                "Verifying CopyTo the actual count and expected count differ")))
            {
                Object[] actualItems = new Object[count];

                collection.CopyTo(actualItems, 0);

                for (int i = 0; i < count && retValue; i++)
                {
                    int itemsIndex = i + index;

                    retValue &= Test.Eval(_comparer.Equals(items[itemsIndex], actualItems[i]),
                        "Expected item and actual item from copied array differ");
                }
            }

            return retValue;
        }

        private Object[] GenerateArray(int length)
        {
            Object[] itemArray = new Object[length];

            for (int i = 0; i < length; ++i)
                itemArray[i] = i ^ length;

            return itemArray;
        }

        internal class MyInvalidReferenceType { }
        internal struct MyInvalidValueType { }
    }
}
