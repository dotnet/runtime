// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// System.Array.Sort<T>(T [],System.Int32,System.Int32,System.Collections.IComparer<T>)
/// </summary>
public class ArraySort10
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        //Bug 385712: Won't fix
        //retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1:Sort a string array using generics and customized comparer");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Tom",
                "Allin"};
            string[] s2 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Allin",
                "Peter",
                "Tom"};
            A a1 = new A();
            Array.Sort<string>(s1, 3, 3, a1);
            for (int i = 0; i < 6; i++)
            {
                if (s1[i] != s2[i])
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Sort an int32 array using customized comparer<T> ");

        try
        {
	    // We'll add two here since we later do things like subtract two from length
            int length = 2 + TestLibrary.Generator.GetByte();
            int[] i1 = new int[length];
            int[] i2 = new int[length];
            for (int i = 0; i < length; i++)
            {
                int value = TestLibrary.Generator.GetByte();
                i1[i] = value;
                i2[i] = value;
            }
            IComparer<int> b1 = new B<int>();
            int startIdx = GetInt(0, length - 2);
            int endIdx = GetInt(startIdx, length - 1);
            int count = endIdx - startIdx + 1;
            Array.Sort<int>(i1, startIdx, count, b1);
            for (int i = startIdx; i < endIdx; i++)  //manually quich sort
            {
                for (int j = i + 1; j <= endIdx; j++)
                {
                    if (i2[i] > i2[j])
                    {
                        int temp = i2[i];
                        i2[i] = i2[j];
                        i2[j] = temp;
                    }
                }
            }
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("003", "The result is not the value as expected,the start index is:" + startIdx.ToString() + "the end index is:" + endIdx.ToString());
                    retVal = false;
                }
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Sort a char array using reverse comparer<T> ");

        try
        {
            char[] c1 = new char[10] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            char[] d1 = new char[10] { 'a', 'e', 'd', 'c', 'b', 'f', 'g', 'h', 'i', 'j' };
            IComparer<char> b2 = new B<char>();
            Array.Sort<char>(c1, 1, 4, b2);
            for (int i = 0; i < 10; i++)
            {
                if (c1[i] != d1[i])
                {
                    TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Sort customized type array using default customized comparer");

        try
        {
            C<int>[] c_array = new C<int>[5];
            C<int>[] c_result = new C<int>[5];
            for (int i = 0; i < 5; i++)
            {
                int value = TestLibrary.Generator.GetInt32();
                C<int> c1 = new C<int>(value);
                c_array.SetValue(c1, i);
                c_result.SetValue(c1, i);
            }
            //sort manually
            C<int> temp;
            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (c_result[i].value > c_result[i + 1].value)
                    {
                        temp = c_result[i];
                        c_result[i] = c_result[i + 1];
                        c_result[i + 1] = temp;
                    }
                }
            }
            Array.Sort<C<int>>(c_array, 0, 5, null);
            for (int i = 0; i < 5; i++)
            {
                if (c_result[i].value != c_array[i].value)
                {
                    TestLibrary.TestFramework.LogError("007", "The result is not the value as expected");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The array to be sorted is null reference ");

        try
        {
            string[] s1 = null;
            Array.Sort<string>(s1, 0, 2, null);
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: The start index is less than the minimal bound of the array");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Peter",
                "Mike",
                "Tom",
                "Allin"};
            Array.Sort<string>(s1, -1, 4, null);
            TestLibrary.TestFramework.LogError("103", "The ArgumentOutOfRangeException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Length is less than zero");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Peter",
                "Mike",
                "Tom",
                "Allin"};
            Array.Sort<string>(s1, 3, -3, null);
            TestLibrary.TestFramework.LogError("105", "The ArgumentOutOfRangeException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: The start index and length do not specify a valid range in array");

        try
        {
            int length = TestLibrary.Generator.GetByte();
            string[] s1 = new string[length];
            for (int i = 0; i < length; i++)
            {
                string value = TestLibrary.Generator.GetString(false, 0, 10);
                s1[i] = value;
            }
            int startIdx = GetInt(0, Byte.MaxValue);
            int increment = length + 1;
            Array.Sort<string>(s1, startIdx + increment, 0, null);
            TestLibrary.TestFramework.LogError("107", "The ArgumentException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("108", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest5:The implementation of comparer caused an error during the sort");

        try
        {
            int[] i1 = new int[9] { 2, 34, 56, 87, 34, 23, 209, 34, 87 };
            IComparer<int> d1 = new D<int>();
            Array.Sort<int>(i1, 0, 9, d1);
            TestLibrary.TestFramework.LogError("109", "The ArgumentException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("110", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest6: Elements in array do not implement the IComparable interface");

        try
        {
            E[] a1 = new E[4] { new E(), new E(), new E(), new E() };
            IComparer<E> d2 = null;
            Array.Sort<E>(a1, 0, 4, d2);
            TestLibrary.TestFramework.LogError("111", "The InvalidOperationException is not throw as expected ");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("112", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ArraySort10 test = new ArraySort10();

        TestLibrary.TestFramework.BeginTestCase("ArraySort10");

        if (test.RunTests())
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

    class A : IComparer<string>
    {
        #region IComparer<string> Members

        public int Compare(string x, string y)
        {
            return x.CompareTo(y);
        }

        #endregion
    }

    class B<T> : IComparer<T> where T : IComparable
    {
        #region IComparer<T> Members

        public int Compare(T x, T y)
        {
            if (typeof(T) == typeof(char))
            {
                return -x.CompareTo(y);
            }
            return x.CompareTo(y);
        }

        #endregion
    }

    class C<T> : IComparable where T : IComparable
    {
        public T value;
        public C(T a)
        {
            this.value = a;
        }


        #region IComparable Members

        public int CompareTo(object obj)
        {
            return value.CompareTo(((C<T>)obj).value);
        }

        #endregion
    }

    class D<T> : IComparer<T> where T : IComparable
    {
        #region IComparer<T> Members

        public int Compare(T x, T y)
        {
            if (x.CompareTo(x) == 0)
                return -1;
            return 1;
        }
        #endregion
    }
    class E
    {
        public E()
        {
        }
    }

    #region Help method for geting test data

    private Int32 GetInt(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + TestLibrary.Generator.GetInt32() % (maxValue - minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }

    #endregion
}
