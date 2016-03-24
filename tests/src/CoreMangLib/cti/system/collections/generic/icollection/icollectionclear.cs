// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.ICollection<T>.Clear()
/// </summary>
public class ICollectionClear
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

    public static int Main(string[] args)
    {
        ICollectionClear testObj = new ICollectionClear();
        TestLibrary.TestFramework.BeginTestCase("Testing for Methord: System.Collections.Generic.ICollection<T>.Clear())");

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
        const string c_TEST_DESC = "PosTest1: Using List<T> which implemented the Clear method in ICollection<T> and Type is Byte...";
        const string c_TEST_ID = "P001";

        Byte[] byteValue = new Byte[10];
        TestLibrary.Generator.GetBytes(-55, byteValue);

        List<Byte> list = new List<Byte>(byteValue);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<Byte>)list).Clear();
            if (list.Count != 0)
            {
                string errorDesc = "ICollection.Count is not " + list.Count.ToString() + " as expected: Actual(0)";
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
        const string c_TEST_DESC = "PosTest2: Using List<T> which implemented the Clear method in ICollection<T> and Type is a reference type...";
        const string c_TEST_ID = "P002";

        String[] strValue = new String[10];
        for (int i = 0; i < 10; i++)
        {
            strValue[i] = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        }
        List<String> list = new List<String>(strValue);
        

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<String>)list).Clear();
            if (list.Count != 0)
            {
                string errorDesc = "ICollection.Count is not 0 as expected: Actual(" + list.Count.ToString() + ")";
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
        const string c_TEST_DESC = "PosTest3: Using List<T> which implemented the Clear method in ICollection<T> and Type is a user-defined type...";
        const string c_TEST_ID = "P003";

        MyClass[] mcValue = new MyClass[10];
        for (int i = 0; i < 10; i++)
        {
            mcValue[i] = new MyClass();
        }
        List<MyClass> list = new List<MyClass>(mcValue);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<MyClass>)list).Clear();
            if (list.Count != 0)
            {
                string errorDesc = "ICollection.Count is not 0 as expected: Actual(" + list.Count.ToString() + ")";
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
        const string c_TEST_DESC = "PosTest4: Using customer class which implemented the Clear method in ICollection<T> and Type is int...";
        const string c_TEST_ID = "P004";

        MyCollection<int> myC = new MyCollection<int>();
        for (int i = 0; i < 10; i++)
        {
            myC.Add(TestLibrary.Generator.GetInt32(-55));
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<int>)myC).Clear();
            if (myC.Count != 0)
            {
                string errorDesc = "ICollection.Count is not 0 as expected: Actual(" + myC.Count.ToString() + ")";
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

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Using user-defined type which is readonly");

        MyCollection<int> myC = new MyCollection<int>();
        myC.isReadOnly = true;

        try
        {
            ((ICollection<int>)myC).Clear();
            TestLibrary.TestFramework.LogError("009", "The NotSupportedException was not thrown as expected");
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
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

        public T this[int index]
        {
            get
            {
                // Fllowing trick can reduce the range check by one
                if ((uint)index >= (uint)length)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return _items[index];
            }
            
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
            if (isReadOnly)
            {
                throw new NotSupportedException();
            }
            else
            {
                Array.Clear(_items, 0, length);
                length = 0;
            }
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
            throw new Exception("The method or operation is not implemented.");
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
    public class  MyClass
    {}
    #endregion
}
