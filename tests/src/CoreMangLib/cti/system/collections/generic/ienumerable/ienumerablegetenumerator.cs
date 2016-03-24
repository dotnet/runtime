// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.IEnumerable<T>.GetEnumerator()
/// </summary>
public class IEnumerableGetEnumerator
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 20;

    public static int Main()
    {
        IEnumerableGetEnumerator testObj = new IEnumerableGetEnumerator();
        TestLibrary.TestFramework.BeginTestCase("Testing for Methord: System.Collections.Generic.IEnumerable<T>.GetEnumerator()");

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

        return retVal;
    }

    #region Positive Tests
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Using List<T> which implemented the GetEnumerator method in IEnumerable<T> and Type is string...";
        const string c_TEST_ID = "P001";

        List<String> list = new List<String>();
        String x = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        String y = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        list.Add(x);
        list.Add(y);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            IEnumerator<String> enumerator = ((IEnumerable<String>)list).GetEnumerator();

            enumerator.MoveNext();

            if (enumerator.Current != x)
            {
                string errorDesc = "Value is not " + x + " as expected: Actual(" + enumerator.Current + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


            enumerator.MoveNext();
            if (enumerator.Current != y)
            {
                string errorDesc = "Value is not " + y + " as expected: Actual(" + enumerator.Current + ")";
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
        const string c_TEST_DESC = "PosTest2: Using List<T> which implemented the GetEnumerator method in IEnumerable<T> and Type is int...";
        const string c_TEST_ID = "P002";

        List<int> list = new List<int>();
        int x = TestLibrary.Generator.GetInt32(-55);
        int y = TestLibrary.Generator.GetInt32(-55);
        list.Add(x);
        list.Add(y);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            IEnumerator<int> enumerator = ((IEnumerable<int>)list).GetEnumerator();

            enumerator.MoveNext();

            if (enumerator.Current != x)
            {
                string errorDesc = "Value is not " + x + " as expected: Actual(" + enumerator.Current + ")";
                TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


            enumerator.MoveNext();
            if (enumerator.Current != y)
            {
                string errorDesc = "Value is not " + y + " as expected: Actual(" + enumerator.Current + ")";
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
        const string c_TEST_DESC = "PosTest3: Useing customer class which implemented the GetEnumerator method in IEnumerable<T>... ";
        const string c_TEST_ID = "P003";

        int[] pArray = new int[2];
        pArray[0] = TestLibrary.Generator.GetInt32(-55);
        pArray[1] = TestLibrary.Generator.GetInt32(-55);

        MyEnumerable<int> myEnumerable = new MyEnumerable<int>(pArray);
        MyEnumerator<int> myEnumerator = new MyEnumerator<int>(pArray);
        ((IEnumerator<int>)myEnumerator).MoveNext();
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            IEnumerator<int> enumerator = ((IEnumerable<int>)myEnumerable).GetEnumerator();
            enumerator.MoveNext();

            if (enumerator.Current != ((IEnumerator<int>)myEnumerator).Current)
            {
                string errorDesc = "Value is not " + myEnumerator.Current + " as expected: Actual(" + enumerator.Current + ")";
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
            enumerator.MoveNext();
            ((IEnumerator<int>)myEnumerator).MoveNext();

            if (enumerator.Current != ((IEnumerator<int>)myEnumerator).Current)
            {
                string errorDesc = "Value is not " + myEnumerator.Current + " as expected: Actual(" + enumerator.Current + ")";
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
    #endregion

    #region Help Class
    public class MyEnumerable<T> : IEnumerable<T>
    {
        public T[] _item;
        public MyEnumerable(T[] pArray)
        {
            _item = new T[pArray.Length];

            for (int i = 0; i < pArray.Length; i++)
            {
                _item[i] = pArray[i];
            }
        }


        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new MyEnumerator<T>(_item);
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new MyEnumerator<T>(_item);
        }

        #endregion
    }

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
