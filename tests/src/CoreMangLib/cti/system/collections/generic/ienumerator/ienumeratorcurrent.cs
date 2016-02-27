// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.IEnumerator<T>.Current
/// </summary>
public class IEnumeratorCurrent
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 20;

    public static int Main()
    {
        IEnumeratorCurrent testObj = new IEnumeratorCurrent();
        TestLibrary.TestFramework.BeginTestCase("Testing for Property: System.Collections.Generic.IEnumerator<T>.Current");

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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region PositiveTests
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Using customer class which implemented the GetEnumerator method in IEnumerator<T> and Type is int...";
        const string c_TEST_ID = "P001";
        
        int[]  intList = new int[2];
        int x = TestLibrary.Generator.GetInt32(-55);
        int y = TestLibrary.Generator.GetInt32(-55);
        intList[0] = x;
        intList[1] = y;

        MyEnumerator<int> myEnumerator = new MyEnumerator<int>(intList);
        ((IEnumerator<int>)myEnumerator).MoveNext();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
      
        try
        {

            if (((IEnumerator<int>)myEnumerator).Current != x)
            {
                string errorDesc = "Value is not " + x + " as expected: Actual(" + ((IEnumerator<int>)myEnumerator).Current + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


            ((IEnumerator<int>)myEnumerator).MoveNext();
            if (((IEnumerator<int>)myEnumerator).Current != y)
            {
                string errorDesc = "Value is not " + y + " as expected: Actual(" + ((IEnumerator<int>)myEnumerator).Current + ")";
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
        const string c_TEST_DESC = "PosTest2: Using List<T> which implemented the GetEnumerator method in IComparer<T> and Type is String...";
        const string c_TEST_ID = "P002";

        String[] strList = new String[2];
        String x = TestLibrary.Generator.GetString(-55, false,c_MINI_STRING_LENGTH,c_MAX_STRING_LENGTH);
        String y = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        strList[0] = x;
        strList[1] = y;

        MyEnumerator<String> myEnumerator = new MyEnumerator<String>(strList);

        ((IEnumerator<String>)myEnumerator).MoveNext();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (((IEnumerator<String>)myEnumerator).Current != x)
            {
                string errorDesc = "Value is not " + x + " as expected: Actual(" + ((IEnumerator<String>)myEnumerator).Current + ")";
                TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


            ((IEnumerator<String>)myEnumerator).MoveNext();
            if (((IEnumerator<String>)myEnumerator).Current != y)
            {
                string errorDesc = "Value is not " + y + " as expected: Actual(" + ((IEnumerator<String>)myEnumerator).Current + ")";
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

    #region NegativeTests
    public bool NegTest1()
    {
        bool retVal = true;

        int[] intList = new int[0];
        MyEnumerator<int> myEnumerator = new MyEnumerator<int>(intList);
        ((IEnumerator<int>)myEnumerator).MoveNext();

        TestLibrary.TestFramework.BeginScenario("NegTest1: Using user-defined type which is empty");

        try
        {
            int i = ((IEnumerator<int>)myEnumerator).Current;
            TestLibrary.TestFramework.LogError("007", "The InvalidOperationException was not thrown as expected");
            retVal = false;
        }
        catch (InvalidOperationException)
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
    public class MyEnumerator<T> : IEnumerator<T>
    {
        public T[] _item;

        // Enumerators are positioned before the first element
        // until the first MoveNext() call.
        int position = -1;

        public MyEnumerator(T[] list)
        {
            _item = list;
        }

        public bool MoveNext()
        {
            position++;
            return (position < _item.Length);
        }

        public void Reset()
        {
            position = -1;
        }

        public object Current
        {
            get
            {
                try
                {
                    return _item[position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        #region IEnumerator<T> Members

        T IEnumerator<T>.Current
        {
            get
            {
                try
                {
                    return _item[position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IEnumerator Members

        object System.Collections.IEnumerator.Current
        {
            get
            {
                try
                {
                    return _item[position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        bool System.Collections.IEnumerator.MoveNext()
        {
            position++;
            return (position < _item.Length);
        }

        void System.Collections.IEnumerator.Reset()
        {
            position = -1;
        }

        #endregion
    }

    #endregion
}
