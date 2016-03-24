// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// System.Array.Sort<T>(T[],System.Int32,System.Int32)
/// </summary>
public class ArraySort9
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
        retVal = NegTest6() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1:Sort a string array using generics ");

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
            Array.Sort<string>(s1, 3, 3);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Sort an int32 array using generics");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55) + 1;
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
           
            Array.Sort<int>(i1, startIdx, count);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Sort a char array from the minimal index to the maximal index of the array ");

        try
        {
            char[] c1 = new char[10] { 'j', 'h', 'g', 'i', 'f', 'e', 'c', 'd', 'a', 'b' };
            char[] d1 = new char[10] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            Array.Sort<char>(c1, 0, c1.Length);
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
            int length = TestLibrary.Generator.GetByte(-55);
            C[] c_array = new C[length];
            C[] c_result = new C[length];
            for (int i = 0; i < length; i++)
            {
                int value = TestLibrary.Generator.GetInt32(-55);
                C c1 = new C(value);
                c_array.SetValue(c1, i);
                c_result.SetValue(c1, i);
            }
            //sort manually
            C temp;
            for (int j = 0; j < length - 1; j++)
            {
                for (int i = 0; i < length - 1; i++)
                {
                    if (c_result[i].value < c_result[i + 1].value)
                    {
                        temp = c_result[i];
                        c_result[i] = c_result[i + 1];
                        c_result[i + 1] = temp;
                    }
                }
            }
            Array.Sort<C>(c_array, 0, length);
            for (int i = 0; i < length; i++)
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: The array is null ");

        try
        {
            string[] s1 = null;
            Array.Sort<string>(s1, 0, 5);
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
            Array.Sort<string>(s1, -1, 4);
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: The length is less than zero");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Peter",
                "Mike",
                "Tom",
                "Allin"};
            Array.Sort<string>(s1, 1, -4);
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

        TestLibrary.TestFramework.BeginScenario("NegTest4: The start index is greater than the maximal range of the array");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55);
            string[] s1 = new string[length];
            for (int i = 0; i < length; i++)
            {
                string value = TestLibrary.Generator.GetString(-55, false, 0, 10);
                s1[i] = value;
            }
            int startIdx = GetInt(0, Byte.MaxValue);
            int increment = length;
            Array.Sort<string>(s1, startIdx + increment, 3);
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

        TestLibrary.TestFramework.BeginScenario("NegTest5: Elements in array do not implement the IComparable interface ");

        try
        {
            E[] a1 = new E[4] { new E(), new E(), new E(), new E() };
            Array.Sort<E>(a1, 0, 4);
            TestLibrary.TestFramework.LogError("109", "The InvalidOperationException is not throw as expected ");
            retVal = false;
        }
        catch (InvalidOperationException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest6: The index argument is valid, but the length argument is too large for that index. ");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55);
            string[] s1 = new string[length];
            for (int i = 0; i < length; i++)
            {
                string value = TestLibrary.Generator.GetString(-55, false, 0, 10);
                s1[i] = value;
            }
            int startIdx = GetInt(0, length - 1);
            int count = GetInt(length + 1, TestLibrary.Generator.GetByte(-55));
            Array.Sort<string>(s1, startIdx, count);
            TestLibrary.TestFramework.LogError("11", "The ArgumentException is not throw as expected ");
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
    #endregion
    #endregion

    public static int Main()
    {
        ArraySort9 test = new ArraySort9();

        TestLibrary.TestFramework.BeginTestCase("ArraySort9");

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
    class C : IComparable
    {
        private int c_value;
        public C(int a)
        {
            this.c_value = a;
        }
        public int value
        {
            get
            {
                return c_value;
            }
        }
        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (this.c_value <= ((C)obj).c_value)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        #endregion
    }
    class E
    {
        public E() { }
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
