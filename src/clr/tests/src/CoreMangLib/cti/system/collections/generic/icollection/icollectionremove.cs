// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.ICollection.Remove(T)
/// </summary>
public class ICollectionRemove
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

    public static int Main(string[] args)
    {
        ICollectionRemove testObj = new ICollectionRemove();
        TestLibrary.TestFramework.BeginTestCase("Testing for Methord: System.Collections.Generic.ICollection.Remove(T)");

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

        TestLibrary.TestFramework.LogInformation("[Netativ]");
        retVal = NegTest1() && retVal;

        return retVal;
    }
    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Using List<T> which implemented the Remove method in ICollection<T> and Type is int...";
        const string c_TEST_ID = "P001";

        List<int> list = new List<int>();
        int item1 = TestLibrary.Generator.GetInt32(-55);
        list.Add(item1);
        for (int i = 1; i < 10; i++)
        {
            list.Add(TestLibrary.Generator.GetInt32(-55));
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            
            if (!((ICollection<int>)list).Remove(item1))
            {
                string errorDesc = "result is not true as expected: Actual is false";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (list.Count != 9)
            {
                string errorDesc = "List<T>.Count is not 9 as expected: Actual is "+list.Count;
                TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2: Using List<T> which implemented the Remove method in ICollection<T> and Type is a reference type...";
        const string c_TEST_ID = "P002";

        List<String> list = new List<String>();
        String item1 = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        list.Add(item1);
        
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (!((ICollection<String>)list).Remove(item1))
            {
                string errorDesc = "result is not true as expected: Actual is false";
                TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (list.Count != 0)
            {
                string errorDesc = "List<T>.Count is not 0 as expected: Actual is " + list.Count;
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

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3: Using List<T> which implemented the Remove method in ICollection<T> and the List<T> doesn't contain this item...";
        const string c_TEST_ID = "P003";

        List<int> list = new List<int>();
        Random rand = new Random(-55);
        for (int i = 0; i < 10; i++)
        {
            list.Add(rand.Next(10, 20));
        }

        int item1 = 1;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (((ICollection<int>)list).Remove(item1))
            {
                string errorDesc = "result is not true as expected: Actual is false";
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (list.Count != 10)
            {
                string errorDesc = "List<T>.Count is not 10 as expected: Actual is " + list.Count;
                TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest4: Using custome class which implemented the Remove method in ICollection<T>...";
        const string c_TEST_ID = "P004";

        MyCollection<int> myC = new MyCollection<int>();
        int item1 = TestLibrary.Generator.GetInt32(-55);
        myC.Add(item1);
        for (int i = 1;i<10; i++)
        {
            myC.Add(TestLibrary.Generator.GetInt32(-55));
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (!((ICollection<int>)myC).Remove(item1))
            {
                string errorDesc = "result is not true as expected: Actual is false";
                TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (myC.Count != 9)
            {
                string errorDesc = "MyCollection<int>.Count is not 9 as expected: Actual is " + myC.Count;
                TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Using custome class which implemented the Remove method in ICollection<T> and  is readonly");

        MyCollection<int> myC = new MyCollection<int>();
        int item1 = TestLibrary.Generator.GetInt32(-55);
        myC.Add(item1);
        myC.isReadOnly = true;

        try
        {
            ((ICollection<int>)myC).Remove(item1);
            TestLibrary.TestFramework.LogError("013", "The NotSupportedException was not thrown as expected");
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
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
