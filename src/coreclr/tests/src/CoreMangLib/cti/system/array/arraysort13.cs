// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Array.Sort<Tkey,Tvalue>(Tkey[],Tvalue[],System.Int32,System.Int32)
/// </summary>
public class ArraySort13
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
        retVal = PosTest5() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Sort a string array using default comparer ");

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
            Array.Sort<string, int>(s1, i1, 3, 3);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Sort an int32 array using default comparer ");

        try
        {
            int length = GetInt(1, Byte.MaxValue);
            int[] i1 = new int[length];
            int[] i2 = new int[length];
            for (int i = 0; i < length; i++)
            {
                int value = TestLibrary.Generator.GetInt32();
                i1[i] = value;
                i2[i] = value;
            }
            int startIdx = GetInt(0, length - 1);
            int endIdx = GetInt(startIdx, length - 1);
            int count = endIdx - startIdx + 1;
            Array.Sort<int, int>(i1, i2, startIdx, count);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Sort an int array and the items array is null ");

        try
        {
            int length = GetInt(1, Int16.MaxValue);
            int[] i1 = new int[length];
            int[] i2 = new int[length];
            for (int i = 0; i < length; i++)
            {
                int value = TestLibrary.Generator.GetByte();
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
            Array.Sort<int, int>(i1, null, startIdx, count);
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("006", "The result is not the value as expected");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Sort a customized array ");

        try
        {
            int length = 10;
            C[] i1 = { new C(10), new C(9), new C(8), new C(7), new C(6), new C(5), new C(4), new C(3), new C(2), new C(1) };
            C[] i2 = { new C(10), new C(9), new C(8), new C(3), new C(4), new C(5), new C(6), new C(7), new C(2), new C(1) };
            int[] i4 = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };
            int[] i3 = { 1, 2, 3, 8, 7, 6, 5, 4, 9, 0 };
            
            Array.Sort<C, int>(i1, i4, 3, 5); // arrays should be sorted from 3 to 7 
            
            for (int i = 0; i < length; i++)
            {
                if (i1[i].show_v != i2[i].show_v)
                {
                    TestLibrary.TestFramework.LogError("008", "The result is not the value as expected");
                    retVal = false;
                }
                if (i3[i] != i4[i])
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

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Sort a customized array: keys are same");

        try
        {
            int length = 10;
            C[] i1 = { new C(1), new C(1), new C(1), new C(1), new C(1), new C(1), new C(1), new C(1), new C(1), new C(1) };
            C[] i2 = { new C(1), new C(1), new C(1), new C(1), new C(1), new C(1), new C(1), new C(1), new C(1), new C(1) };
            int[] i4 = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };
            int[] i3 = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };

            //Array.Sort swaps the items even if keys are same: Post Dev10 Bug 868577
            Array.Sort<C, int>(i1, i4, 3, 5); // arrays should be sorted from 3 to 7 

            for (int i = 0; i < length; i++)
            {
                if (i1[i].show_v != i2[i].show_v)
                {
                    TestLibrary.TestFramework.LogError("010", "The result is not the value as expected");
                    retVal = false;
                }
                if (i3[i] != i4[i])
                {
                    TestLibrary.TestFramework.LogError("011", "The result is not the value as expected");
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

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The first argument is null reference ");

        try
        {
            string[] s1 = null;
            int[] i1 = { 1, 2, 3, 4, 5 };
            Array.Sort<string, int>(s1, i1, 0, 2);
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
            Array.Sort<string, int>(s1, i1, -1, 4);
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
            Array.Sort<string, int>(s1, i1, 3, -3);
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
            int length = GetInt(1, Byte.MaxValue);
            string[] s1 = new string[length];
            for (int i = 0; i < length; i++)
            {
                string value = TestLibrary.Generator.GetString(false, 0, 10);
                s1[i] = value;
            }
            int[] i1 = new int[6] { 1, 2, 3, 4, 5, 6 };
            int startIdx = GetInt(1, Byte.MaxValue);
            int increment = length;
            Array.Sort<string, int>(s1, i1, startIdx + increment, 0);
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
            int length = GetInt(1, Byte.MaxValue);
            string[] s1 = new string[length];
            for (int i = 0; i < length; i++)
            {
                string value = TestLibrary.Generator.GetString(false, 0, 10);
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

        TestLibrary.TestFramework.BeginScenario("NegTest6: The keys array does not implement the IComparable interface ");

        try
        {
            D d1 = new D();
            D d2 = new D();
            D d3 = new D();
            D d4 = new D();
            int[] i2 = { 1, 2, 3, 4 };
            D[] d = new D[4] { d1, d2, d3, d4 };
            Array.Sort<D, int>(d, i2, null);
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
        ArraySort13 test = new ArraySort13();

        TestLibrary.TestFramework.BeginTestCase("ArraySort13");

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

    class A<T> : IComparer<T> where T : IComparable
    {

        #region IComparer<T> Members

        int IComparer<T>.Compare(T x, T y)
        {
            if (typeof(T) == typeof(char))
            {
                return -(x.CompareTo(y));
            }
            return x.CompareTo(y);
        }

        #endregion
    }
    class B<T> : IComparer<T> where T : IComparable
    {

        #region IComparer<T> Members

        int IComparer<T>.Compare(T x, T y)
        {
            return -(x.CompareTo(y));
        }

        #endregion
    }

    class C : IComparable
    {
        protected int c_value;
        public C(int a)
        {
            this.c_value = a;
        }
        public int show_v
        {
            get
            {
                return this.c_value;
            }
        }

        #region IComparable Members

        int IComparable.CompareTo(object obj)
        {
            return this.c_value.CompareTo((obj as C).c_value);
        }

        #endregion
    }


    class D
    {
        public D()
        {
        }
    }
    class E<T> : IComparer<T> where T : IComparable
    {
        #region IComparer<T> Members

        int IComparer<T>.Compare(T x, T y)
        {
            if (x.CompareTo(x) == 0)
            {
                return -1;
            }
            return 1;
        }

        #endregion
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
    #endregion