// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.ICollection.Add(T)
/// </summary>
public class ICollectionAdd
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

    public static int Main(string[] args)
    {
        ICollectionAdd testObj = new ICollectionAdd();
        TestLibrary.TestFramework.BeginTestCase("Testing for Methord: System.Collections.Generic.ICollection.Add(T)");

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

        TestLibrary.TestFramework.LogInformation("[Netativ]");
        retVal = NegTest1() && retVal;

        return retVal;
    }
    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Using List<T> which implemented the add method in ICollection<T> and Type is int...";
        const string c_TEST_ID = "P001";

        List<int> list = new List<int>();
        int item1 = TestLibrary.Generator.GetInt32(-55);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<int>)list).Add(item1);
            if (list[0] != item1)
            {
                string errorDesc = "Value is not " + list[0] + " as expected: Actual(" + item1 + ")";
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
        const string c_TEST_DESC = "PosTest2: Using List<T> which implemented the add method in ICollection<T> and Type is a reference type...";
        const string c_TEST_ID = "P002";

        List<String> list = new List<String>();
        String item1 = TestLibrary.Generator.GetString(-55, false,c_MINI_STRING_LENGTH,c_MAX_STRING_LENGTH);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<String>)list).Add(item1);
            if (list[0] != item1)
            {
                string errorDesc = "Value is not " + list[0] + " as expected: Actual(" + item1 + ")";
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
        const string c_TEST_DESC = "PosTest3: Using List<T> which implemented the add method in ICollection<T> and Type is a reference type...";
        const string c_TEST_ID = "P003";

        MyCollection<int> myC = new MyCollection<int>();
        int item1 = TestLibrary.Generator.GetInt32(-55);
        int count = 1;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((ICollection<int>)myC).Add(item1);
            if (myC.Count != count)
            {
                string errorDesc = "Value have not been add to ICollection<T>";
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Using user-defined type which is readonly");

        MyCollection<int> myC = new MyCollection<int>();
        myC.isReadOnly = true;
        int item1 = TestLibrary.Generator.GetInt32(-55);

        try
        {
            ((ICollection<int>)myC).Add(item1);
            TestLibrary.TestFramework.LogError("007", "The NotSupportedException was not thrown as expected");
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Help Class
    public class MyCollection<T> : ICollection<T>
    {
        public T[] value;
        protected int length;
        public bool isReadOnly = false;

        public MyCollection()
        {
            value =new T[10];
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
                value[length] = item;
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
            get { return length;} 
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

    #endregion
}
