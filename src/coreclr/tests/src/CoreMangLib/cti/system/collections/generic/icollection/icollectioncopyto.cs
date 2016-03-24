// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.ICollection<T>.CopyTo(T[],System.Int32)
/// </summary>
public class ICollectionCopyTo
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

    public static int Main(string[] args)
    {
        ICollectionCopyTo testObj = new ICollectionCopyTo();
        TestLibrary.TestFramework.BeginTestCase("Testing for Methord: System.Collections.Generic.ICollection<T>.CopyTo(T[],System.Int32)");

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

        TestLibrary.TestFramework.LogInformation("[Netativ]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }
    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Using List<T> which implemented the CopyTo method in ICollection<T> and Type is Byte...";
        const string c_TEST_ID = "P001";

        int capacity = 10;
        Byte[] byteValue = new Byte[capacity];
        TestLibrary.Generator.GetBytes(-55, byteValue);
        List<Byte> list = new List<Byte>(byteValue);

        Byte[] array = new Byte[capacity];

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<Byte>)list).CopyTo(array,0);
            for(int i=0;i<capacity;i++)
            {
                if (list[i] != array[i])
                {
                    string errorDesc = "Value is not " + list[i].ToString() + " as expected: Actual(" + array[i].ToString() + ")";
                    TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                    retVal = false;
                }
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
        const string c_TEST_DESC = "PosTest2: Using List<T> which implemented the CopyTo method in ICollection<T> and Type is a reference type...";
        const string c_TEST_ID = "P002";

        int capacity = 10;
        String[] strValue = new String[capacity];
        for (int i = 0; i < capacity; i++)
        {
            strValue[i] = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        }
        List<String> list = new List<String>(strValue);

        String[] array = new String[capacity];


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<String>)list).CopyTo(array,0);
            for(int i=0;i<capacity;i++)
            {
                if (list[i] != array[i])
                {
                    string errorDesc = "Value is not " + list[i] + " as expected: Actual(" + array[i]+ ")";
                    TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                    retVal = false;
                }
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
        const string c_TEST_DESC = "PosTest3: Using List<T> which implemented the CopyTo method in ICollection<T> and Type is a user-defined type...";
        const string c_TEST_ID = "P003";

        int capacity = 10;
        List<MyClass> list = new List<MyClass>();
        for (int i = 0; i < capacity; i++)
        {
            list.Add(new MyClass());        
        }

        MyClass[] array = new MyClass[10];

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<MyClass>)list).CopyTo(array,0);
            for(int i=0;i<capacity;i++)
            {
                if (list[i] != array[i])
                {
                    string errorDesc = "Value is not " + list[i].ToString() + " as expected: Actual(" + array[i].ToString()+ ")";
                    TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                    retVal = false;
                }
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
        const string c_TEST_DESC = "PosTest4: Using customer class which implemented the CopyTo method in ICollection<T> and Type is int...";
        const string c_TEST_ID = "P004";

        int capacity = 10;
        MyCollection<int> myC = new MyCollection<int>();
        for (int i = 0; i < capacity; i++)
        {
            myC.Add(TestLibrary.Generator.GetInt32(-55));
        }

        int[] array = new int[capacity];

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        
        try
        {
            ((ICollection<int>)myC).CopyTo(array, 0);
            for (int i = 0; i < capacity; i++)
            {
                if (myC[i] != array[i])
                {
                    string errorDesc = "Value is not " + myC[i].ToString() + " as expected: Actual(" + array[i].ToString() + ")";
                    TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                    retVal = false;
                }
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
        const string c_TEST_DESC = "PosTest5: Using List<T> which implemented the CopyTo method in ICollection<T> and Index is greater than zero...";
        const string c_TEST_ID = "P005";

        int capacity = 10;
        Byte[] byteValue = new Byte[capacity];
        TestLibrary.Generator.GetBytes(-55, byteValue);
        List<Byte> list = new List<Byte>(byteValue);

        Random rand = new Random(-55);
        int index = rand.Next(1, 100);
        Byte[] array = new Byte[capacity+index];

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<Byte>)list).CopyTo(array, index);
            for (int i = 0; i < capacity; i++)
            {
                if (list[i] != array[i+index])
                {
                    string errorDesc = "Value is not " + list[i].ToString() + " as expected: Actual(" + array[i+index].ToString() + ")";
                    TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest1: Using List<T> which implemented the CopyTo method in ICollection<T> and Array is a null reference...";
        const string c_TEST_ID = "N001";

        int capacity = 10;
        Byte[] byteValue = new Byte[capacity];
        TestLibrary.Generator.GetBytes(-55, byteValue);
        List<Byte> list = new List<Byte>(byteValue);

        Byte[] array = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<Byte>)list).CopyTo(array, 0);
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, "The ArgumentNullException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest2: Using List<T> which implemented the CopyTo method in ICollection<T> and arrayIndex is less than zero...";
        const string c_TEST_ID = "N002";

        int capacity = 10;
        Byte[] byteValue = new Byte[capacity];
        TestLibrary.Generator.GetBytes(-55, byteValue);
        List<Byte> list = new List<Byte>(byteValue);

        Byte[] array = new Byte[capacity];
        int index =TestLibrary.Generator.GetInt32(-55);
        index = -index;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<Byte>)list).CopyTo(array, index);
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, "The ArgumentOutOfRangeException was not thrown as expected when arrayindex is " + index.ToString());
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest3: Using List<T> which implemented the CopyTo method in ICollection<T> and arrayIndex is equal length of array...";
        const string c_TEST_ID = "N003";

        int capacity = 10;
        Byte[] byteValue = new Byte[capacity];
        TestLibrary.Generator.GetBytes(-55, byteValue);
        List<Byte> list = new List<Byte>(byteValue);

        Byte[] array = new Byte[capacity];

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<Byte>)list).CopyTo(array, capacity);
            TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, "The ArgumentException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest4: Using List<T> which implemented the CopyTo method in ICollection<T> and arrayIndex is grater than length of array...";
        const string c_TEST_ID = "N004";

        int capacity = 10;
        Byte[] byteValue = new Byte[capacity];
        TestLibrary.Generator.GetBytes(-55, byteValue);
        List<Byte> list = new List<Byte>(byteValue);

        Random rand = new Random(-55);
        int index = rand.Next(capacity, 100);
        Byte[] array = new Byte[capacity];
        
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<Byte>)list).CopyTo(array, index);
            TestLibrary.TestFramework.LogError("017" + " TestId-" + c_TEST_ID, "The ArgumentException was not thrown as expected when arrayindex is " + index.ToString());
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest5: Using List<T> which implemented the CopyTo method in ICollection<T> and The number of elements in the list<T>  is greater than the available space from arrayIndex to the end of array...";
        const string c_TEST_ID = "N005";

        int capacity = 10;
        Byte[] byteValue = new Byte[capacity];
        TestLibrary.Generator.GetBytes(-55, byteValue);
        List<Byte> list = new List<Byte>(byteValue);

        Random rand = new Random(-55);
        int index = rand.Next(1, 10);
        Byte[] array = new Byte[capacity];

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<Byte>)list).CopyTo(array, index);
            TestLibrary.TestFramework.LogError("019" + " TestId-" + c_TEST_ID, "The ArgumentException was not thrown as expected when arrayindex is " + index.ToString());
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
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
            Array.Copy(_items, 0, array, arrayIndex, length);
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
