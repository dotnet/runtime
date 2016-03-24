// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.ICollection<T>.Contains(T)
/// </summary>
public class ICollectionContains
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

    public static int Main(string[] args)
    {
        ICollectionContains testObj = new ICollectionContains();
        TestLibrary.TestFramework.BeginTestCase("Testing for Methord: System.Collections.Generic.ICollection<T>.Contains(T)");

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
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;

        return retVal;
    }
    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Using List<T> which implemented the Contanins method in ICollection<T> and Type is Byte...";
        const string c_TEST_ID = "P001";

        Byte[] byteValue = new Byte[10];
        TestLibrary.Generator.GetBytes(-55, byteValue);

        List<Byte> list = new List<Byte>(byteValue);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            bool contains = ((ICollection<Byte>)list).Contains(byteValue[1]);
            if (!contains)
            {
                string errorDesc = "Value is not false as expected: Actual is true";
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
        const string c_TEST_DESC = "PosTest2: Using List<T> which implemented the Contanins method in ICollection<T> and Type is a reference type...";
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
            bool contains = ((ICollection<String>)list).Contains(strValue[1]);
            if (!contains)
            {
                string errorDesc = "Value is not false as expected: Actual is true";
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
        const string c_TEST_DESC = "PosTest3: Using List<T> which implemented the Contanins method in ICollection<T> and Type is a user-defined type...";
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
            bool contains = ((ICollection<MyClass>)list).Contains(mcValue[1]);
            if (!contains)
            {
                string errorDesc = "Value is not false as expected: Actual is true";
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
        const string c_TEST_DESC = "PosTest4: Verify the List<T> doesn't  contain a null object...";
        const string c_TEST_ID = "P004";

        String[] strValue = new String[10];
        for (int i = 0; i < 10; i++)
        {
            strValue[i] = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        }
        List<String> list = new List<String>(strValue);


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            bool contains = ((ICollection<String>)list).Contains(null);
            if (contains)
            {
                string errorDesc = "Value is not true as expected: Actual is false";
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

    public bool PosTest5()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest5: Verify the List<T> contains a null object...";
        const string c_TEST_ID = "P005";

        String[] strValue = new String[10];
        for (int i = 0; i < 9; i++)
        {
            strValue[i] = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        }
        strValue[9] = null;
        List<String> list = new List<String>(strValue);


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            bool contains = ((ICollection<String>)list).Contains(null);
            if (!contains)
            {
                string errorDesc = "Value is not false as expected: Actual is true";
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest6()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest6: Using customer class which implemented the Contanins method in ICollection<T> and Type is int...";
        const string c_TEST_ID = "P006";

        int value = TestLibrary.Generator.GetInt32(-55);
        MyCollection<int> myC = new MyCollection<int>();
        for (int i = 0; i < 10; i++)
        {
            value = TestLibrary.Generator.GetInt32(-55);
            myC.Add(value);
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            bool contains = ((ICollection<int>)myC).Contains(value);
            if (!contains)
            {
                string errorDesc = "Value is not false as expected: Actual is true";
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
            if ((Object)item == null)
            {
                for (int i = 0; i < length; i++)
                    if ((Object)_items[i] == null)
                        return true;
                return false;
            }
            else
            {
                EqualityComparer<T> c = EqualityComparer<T>.Default;
                for (int i = 0; i < length; i++)
                {
                    if (c.Equals(_items[i], item)) return true;
                }
                return false;
            }
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
    public class MyClass
    { }
    #endregion
}
