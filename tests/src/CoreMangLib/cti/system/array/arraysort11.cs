// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Array.Sort<Tkey,Tvalue>(Tkey[],Tvalue[])
/// </summary>
public class ArraySort11
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Sort a string array ");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Tom",
                "Allin"};
            int[] i1 = new int[6] { 24, 30, 28, 26, 32, 23 };
            string[] s2 = new string[6]{"Allin",
                "Jack",
                "Mary",
                "Mike",
                "Peter",
                "Tom"};
            int[] i2 = new int[6] { 23, 24, 30, 28, 26, 32 };
            Array.Sort<string, int>(s1, i1);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Sort an int32 array  ");

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
            Array.Sort<int, int>(i1, i2);
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
            char[] c1 = new char[10] { 'j', 'i', 'h', 'g', 'f', 'e', 'd', 'c', 'b', 'a' };
            char[] d1 = new char[10] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            int[] a1 = new int[10] { 2, 3, 4, 1, 0, 2, 12, 52, 31, 0 };
            int[] b1 = new int[10] { 0, 31, 52, 12, 2, 0, 1, 4, 3, 2 };
            Array.Sort<char, int>(c1, a1);
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Sort a customized type array ");

        try
        {
            A[] a1 = new A[5] { new A(3), new A(43), null, new A(-888), new A(0) };
            int[] c1 = new int[] { 3, 43, 4, 0, 5 };
            int[] d1 = new int[] { 4, 0, 5, 3, 43 };
            Array.Sort<A, int>(a1, c1);
            for (int i = 0; i < 5; i++)
            {
                if (c1[i] != d1[i])
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: The value[] argument is null ");

        try
        {
            int length = GetInt(1, Int16.MaxValue);
            char[] c1 = new char[length];
            char[] c2 = new char[length];
            for (int i = 0; i < length; i++)
            {
                char value = TestLibrary.Generator.GetChar();
                c1[i] = value;
                c2[i] = value;
            }
            string[] s1 = null;
            Array.Sort<char, string>(c1, s1);
            for (int i = 0; i < length - 1; i++)  //manually quich sort
            {
                for (int j = i + 1; j < length; j++)
                {
                    if (c2[i] > c2[j])
                    {
                        char temp = c2[i];
                        c2[i] = c2[j];
                        c2[j] = temp;
                    }
                }
            }
            for (int i = 0; i < length; i++)
            {
                if (c1[i] != c2[i])
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The first key argument is null reference ");

        try
        {
            string[] s1 = null;
            int[] i1 = { 1, 2, 3, 4, 5 };
            Array.Sort<string, int>(s1, i1);
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

        TestLibrary.TestFramework.BeginScenario("NegTest2:The length of items array is less than the length of keys array ");

        try
        {
            int length_array = GetInt(2, Int16.MaxValue);
            int length_value = GetInt(1, length_array-1);
            string[] s1 = new string[length_array];
            int[] i1 = new int[length_value];
            for (int i = 0; i < length_array; i++)
            {
                string value = TestLibrary.Generator.GetString(false, 0, 10);
                s1[i] = value;
            }
            for (int i = 0; i < length_value; i++)
            {
                int value = TestLibrary.Generator.GetInt32();
                i1[i] = value;
            }
            Array.Sort<string, int>(s1, i1);
            TestLibrary.TestFramework.LogError("103", "The ArgumentException is not throw as expected ");
            retVal = false;
        }
        catch (ArgumentException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: The keys array does not implement the IComparable interface ");

        try
        {
            D d1 = new D();
            D d2 = new D();
            D d3 = new D();
            D d4 = new D();
            int[] i2 = { 1, 2, 3, 4 };
            D[] d = new D[4] { d1, d2, d3, d4 };
            Array.Sort<D, int>(d, i2);
            TestLibrary.TestFramework.LogError("105", "The InvalidOperationException is not throw as expected ");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ArraySort11 test = new ArraySort11();

        TestLibrary.TestFramework.BeginTestCase("ArraySort11");

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

    class A : IComparable
    {
        public int value;
        public A(int a)
        {
            this.value = a;
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return this.value.CompareTo(((A)obj).value);
        }

        #endregion
    }

    class D
    {
        public D()
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

