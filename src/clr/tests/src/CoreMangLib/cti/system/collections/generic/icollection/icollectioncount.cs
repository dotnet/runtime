// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.ICollection.Count
/// </summary>
public class ICollectionCount
{
    public static int Main(string[] args)
    {
        ICollectionCount testObj = new ICollectionCount();
        TestLibrary.TestFramework.BeginTestCase("Testing for Property: System.Collections.Generic.ICollection.Count");

        if (testObj.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 0;
        }
    }

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        return retVal;
    }
    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Verify the Count of  empty  List<T> which implemented the Count property in ICollection<T> is 0...";
        const string c_TEST_ID = "P001";

        List<int> list = new List<int>();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (((ICollection<int>)list).Count != 0)
            {
                string errorDesc = "Count is not 0 as expected: Actual is "+list.Count;
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2: Using List<T> which implemented the Count property in ICollection<T> and Type is a reference type...";
        const string c_TEST_ID = "P002";

        List<String> list = new List<String>();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (((ICollection<String>)list).Count != 0)
            {
                string errorDesc = "Count is not 0 as expected: Actual is " + list.Count;
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3: Using List<T> which implemented the Count property in ICollection<T> and count is not zero...";
        const string c_TEST_ID = "P003";

        List<int> list = new List<int>();
        int count = 10;
        for (int i = 0; i < count; i++)
        {
            list.Add(TestLibrary.Generator.GetInt32(-55));
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (((ICollection<int>)list).Count != 10)
            {
                string errorDesc = "Count is not 10 as expected: Actual is " + list.Count;
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest4: Using custome class which implemented the Count property in ICollection<T>...";
        const string c_TEST_ID = "P004";

        MyCollection<int> myC = new MyCollection<int>();
        int count = 10;
        for (int i = 0; i <count; i++)
        {
            myC.Add(TestLibrary.Generator.GetInt32(-55));
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (((ICollection<int>)myC).Count != 10)
            {
                string errorDesc = "Count is not 10 as expected: Actual is " + myC.Count;
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Help Class
    public class MyCollection<T> : ICollection<T>
    {
        public T[] _items;
        protected int length;
        public bool isReadOnly = false;

        public MyCollection()
        {
            _items = new T[10];
            length = 0;
        }

        #region ICollection<T> Members
        public void Add(T item)
        {
            if (isReadOnly)
            {
                throw new NotSupportedException();
            }
            else
            {
                _items[length] = item;
                length++;
            }
        }

        public void Clear()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool Contains(T item)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int Count
        {
            get { return length; }
        }

        public bool IsReadOnly
        {
            get { return isReadOnly; }
        }

        public bool Remove(T item)
        {
            if (isReadOnly)
            {
                throw new NotSupportedException();
            }
            int index = Array.IndexOf(_items, item, 0, length);
            if (index < 0)
            {
                return false;
            }
            else
            {
                if ((uint)index >= (uint)length)
                {
                    throw new ArgumentOutOfRangeException(); ;
                }
                length--;
                if (index < length)
                {
                    Array.Copy(_items, index + 1, _items, index, length - index);
                }
                _items[length] = default(T);

                return true;
            }
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }

    #endregion
}
