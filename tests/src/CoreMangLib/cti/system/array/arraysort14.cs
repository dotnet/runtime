// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Array.Sort<Tkey,Tvalue>(Tkey[],Tvalue[],System.Int32,System.Int32,IComparer)
/// </summary>

public class ArraySort14
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
        retVal = NegTest5() && retVal;
        //DevDiv Bug 385712: Won't fix
        //retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Sort a string array using comparer ");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Tom",
                "Allin"};
            int[] i1 = new int[6] { 24, 30, 28, 26, 32, 23 };
            string[] s2 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Allin",
                "Peter",
                "Tom"};
            int[] i2 = new int[6] { 24, 30, 28, 23, 26, 32 };
            A a1 = new A();
            Array.Sort<string, int>(s1, i1, 3, 3, a1);
            for (int i = 0; i < 6; i++)
            {
                if (s1[i] != s2[i])
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
                    retVal = false;
                }
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("002", "The result is not the value as expected");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Sort an int32 array using comparer ");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55);
            int[] i1 = new int[length];
            int[] i2 = new int[length];
            for (int i = 0; i < length; i++)
            {
                int value = TestLibrary.Generator.GetInt32(-55);
                i1[i] = value;
                i2[i] = value;
            }
            IComparer<int> b1 = new B<int>();
            int startIdx = GetInt(0, length - 1);
            int endIdx = GetInt(startIdx, length - 1);
            int count = endIdx - startIdx + 1;
            Array.Sort<int, int>(i1, i2, startIdx, count, b1);
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("004", "The result is not the value as expected");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Sort a char array using reverse comparer ");

        try
        {
            char[] c1 = new char[10] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            char[] d1 = new char[10] { 'a', 'e', 'd', 'c', 'b', 'f', 'g', 'h', 'i', 'j' };
            int[] a1 = new int[10] { 2, 3, 4, 1, 0, 2, 12, 52, 31, 0 };
            int[] b1 = new int[10] { 2, 0, 1, 4, 3, 2, 12, 52, 31, 0 };
            IComparer<char> b2 = new B<char>();
            Array.Sort<char, int>(c1, a1, 1, 4, b2);
            for (int i = 0; i < 10; i++)
            {
                if (c1[i] != d1[i])
                {
                    TestLibrary.TestFramework.LogError("006", "The result is not the value as expected");
                    retVal = false;
                }
                if (a1[i] != b1[i])
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

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Sort an int array and the items array is null ");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55);
            int[] i1 = new int[length];
            int[] i2 = new int[length];
            for (int i = 0; i < length; i++)
            {
                int value = TestLibrary.Generator.GetByte(-55);
                i1[i] = value;
                i2[i] = value;
            }
            int startIdx = GetInt(0, length - 2);
            int endIdx = GetInt(startIdx, length - 1);
            int count = endIdx - startIdx + 1;
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
            Array.Sort<int, int>(i1, null, startIdx, count, new B<int>());
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("009", "The result is not the value as expected");
                    retVal = false;
                }
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The first argument is null reference ");

        try
        {
            string[] s1 = null;
            int[] i1 = { 1, 2, 3, 4, 5 };
            Array.Sort<string, int>(s1, i1, 0, 2, new B<string>());
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
            int[] i1 = new int[6] { 1, 2, 3, 4, 5, 6 };
            Array.Sort<string, int>(s1, i1, -1, 4, new B<string>());
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
            int[] i1 = new int[6] { 1, 2, 3, 4, 5, 6 };
            Array.Sort<string, int>(s1, i1, 3, -3, new B<string>());
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

        TestLibrary.TestFramework.BeginScenario("NegTest4: The start index is greater than the maximal index of the array");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55);
            string[] s1 = new string[length];
            for (int i = 0; i < length; i++)
            {
                string value = TestLibrary.Generator.GetString(-55, false, 0, 10);
                s1[i] = value;
            }
            int[] i1 = new int[6] { 1, 2, 3, 4, 5, 6 };
            int startIdx = GetInt(1, Byte.MaxValue);
            int increment = length;
            Array.Sort<string, int>(s1, i1, startIdx + increment, 0, new B<string>());
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

        TestLibrary.TestFramework.BeginScenario("NegTest5: The start index is valid, but the length is too large for that index");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55);
            string[] s1 = new string[length];
            for (int i = 0; i < length; i++)
            {
                string value = TestLibrary.Generator.GetString(-55, false, 0, 10);
                s1[i] = value;
            }
            int[] i1 = new int[6] { 1, 2, 3, 4, 5, 6 };
            int startIdx = GetInt(1, length - 1);
            int count = length;
            Array.Sort<string, int>(s1, i1, startIdx, count);
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

        TestLibrary.TestFramework.BeginScenario("NegTest6:The implementation of comparer caused an error during the sort");

        try
        {
            string[] s1 = new string[9]{"Jack",
                "Mary",
                "Peter",
                "Mike",
                "Tom",
                "Allin",
                "Kelly",
               "Agassi",
               "Koter"};
            int[] i1 = new int[9] { 2, 34, 56, 87, 34, 23, 209, 34, 87 };
            IComparer<string> d1 = new D<string>();
            Array.Sort<string, int>(s1, i1, 0, 9, d1);
            TestLibrary.TestFramework.LogError("111", "The ArgumentException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("112", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool NegTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest7: The keys array does not implement the IComparable interface ");

        try
        {
            E d1 = new E();
            E d2 = new E();
            E d3 = new E();
            E d4 = new E();
            int[] i2 = { 1, 2, 3, 4 };
            E[] e = new E[4] { d1, d2, d3, d4 };
            Array.Sort<E, int>(e, i2, null);
            TestLibrary.TestFramework.LogError("113", "The InvalidOperationException is not throw as expected ");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("114", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ArraySort14 test = new ArraySort14();

        TestLibrary.TestFramework.BeginTestCase("ArraySort14");

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
                return minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue);
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
