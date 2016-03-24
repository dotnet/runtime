// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Array.Reverse(System.Array,int32,int32)
/// </summary>
public class ArrayReverse2
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

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
        retVal = PosTest6() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Random reverse an int array 1: argument count is even");

        try
        {
            int length = GetInt(2, UInt16.MaxValue);
            int[] i1 = new int[length];             //the array length of i1
            int[] i2 = new int[length];             //the array length of i2
            int startIdx = GetInt(0, length-1);     //The starting index of the section to reverse.
            int endIdx = GetInt(startIdx, length);//The end index of the section to reverse.
            int count = endIdx - startIdx + 1;
            while (count == 0 || (count % 2 != 0))  // Make the argument count to be even
            {
                endIdx = GetInt(startIdx, length);
                count = endIdx - startIdx + 1;
            }
            for (int i = 0; i < length; i++)
            {
                int value = TestLibrary.Generator.GetByte();
                i1[i] = value;
                i2[i] = value;
            }
            int times = count / 2; // the times need to exchange value
            int endIdx_temp = endIdx;
            int startIdx_temp = startIdx;
            for (int i = 0; i < times; i++) //reverse the array manually
            {
                int temp = i2[startIdx_temp];
                i2[startIdx_temp] = i2[endIdx_temp];
                i2[endIdx_temp] = temp;
                startIdx_temp++;
                endIdx_temp--;
            }
            Array.Reverse(i1, startIdx, count);
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,the index not equal is " + i.ToString());
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Random reverse a char array 2: argument count is odd");

        try
        {
            int length = GetInt(1, UInt16.MaxValue);
            char[] i1 = new char[length];             //the array length of i1
            char[] i2 = new char[length];             //the array length of i2
            int startIdx = GetInt(0, length);     //The starting index of the section to reverse.
            int endIdx = GetInt(startIdx, length);//The end index of the section to reverse.
            int count = endIdx - startIdx + 1;
            while (count == 0 || (count % 2 == 0))  // Make the argument count to be odd but not zero
            {
                endIdx = GetInt(startIdx, length);
                count = endIdx - startIdx + 1;
            }
            for (int i = 0; i < length; i++)
            {
                char value = TestLibrary.Generator.GetChar();
                i1[i] = value;
                i2[i] = value;
            }
            int times = (count - 1) / 2; // the times need to exchange value
            int endIdx_temp = endIdx;
            int startIdx_temp = startIdx;
            for (int i = 0; i < times; i++) //reverse the array manually
            {
                char temp = i2[startIdx_temp];
                i2[startIdx_temp] = i2[endIdx_temp];
                i2[endIdx_temp] = temp;
                startIdx_temp++;
                endIdx_temp--;
            }
            Array.Reverse(i1, startIdx, count);
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("003", "The result is not the value as expected,the index not equal is " + i.ToString());
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Random reverse a string array 3: argument count is zero");

        try
        {
            int length = GetInt(1, Byte.MaxValue);
            string[] i1 = new string[length];             //the array length of i1
            string[] i2 = new string[length];             //the array length of i2
            int startIdx = GetInt(0, length);     //The starting index of the section to reverse.
            int count = 0;
            for (int i = 0; i < length; i++)
            {
                string value = TestLibrary.Generator.GetString(false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                i1[i] = value;
                i2[i] = value;
            }
            Array.Reverse(i1, startIdx, count);
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("005", "The result is not the value as expected,the index not equal is " + i.ToString());
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Reserve the last element of a string array ");

        try
        {
            int length = GetInt(1, Byte.MaxValue);
            string[] i1 = new string[length];             //the array length of i1
            string[] i2 = new string[length];             //the array length of i2
            int startIdx = length - 1;     //The starting index of the section to reverse.
            int count = 1;
            for (int i = 0; i < length; i++)
            {
                string value = TestLibrary.Generator.GetString(false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                i1[i] = value;
                i2[i] = value;
            }
            Array.Reverse(i1, startIdx, count);
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("007", "The result is not the value as expected,the index not equal is " + i.ToString());
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

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Reserve the customized type array ");

        try
        {
            int length = GetInt(1, Byte.MaxValue);
            A[] i1 = new A[length];             //the array length of i1
            A[] i2 = new A[length];             //the array length of i2
            for (int i = 0; i < length; i++)
            {
                A value = new A(TestLibrary.Generator.GetInt32());
                i1[i] = value;
                i2[length - 1 - i] = value;
            }
            Array.Reverse(i1, 0, length);
            for (int i = 0; i < length; i++)
            {
                if (i1[i].a != i2[i].a)
                {
                    TestLibrary.TestFramework.LogError("009", "The result is not the value as expected,the index not equal is " + i.ToString());
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

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Reserve array including null elements ");

        try
        {
            int length = GetInt(1, Byte.MaxValue);
            A[] i1 = new A[length];             //the array length of i1
            A[] i2 = new A[length];             //the array length of i2
            for (int i = 0; i < length; i++)
            {
                int pro = TestLibrary.Generator.GetInt16();
                if (pro % 3 == 0)
                {
                    A value = null;
                    i1[i] = value;
                    i2[length - 1 - i] = value;
                }
                else
                {
                    A value = new A(TestLibrary.Generator.GetInt32());
                    i1[i] = value;
                    i2[length - 1 - i] = value;
                }
            }
            Array.Reverse(i1, 0, length);
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i2[i])
                {
                    TestLibrary.TestFramework.LogError("011", "The result is not the value as expected,the index not equal is " + i.ToString());
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The array is null reference");

        try
        {
            string[] s1 = null;
            Array.Reverse(s1, 0, 0);
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: The array is not a one dimension array ");
        try
        {
            int[,] i1 = new int[2, 3] { { 2, 3, 5 }, { 34, 56, 77 } };
            Array.Reverse(i1, 0, 1);
            TestLibrary.TestFramework.LogError("103", "The RankException was not thrown as expected");
            retVal = false;
        }
        catch (RankException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: Set the negative startIndex argument");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Mary",
                "Joan"};
            Array.Reverse(s1, -1, 3);
            TestLibrary.TestFramework.LogError("105", "The ArgumentOutOfRangeException was not thrown as expected");
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


    // This Scenario failed !
    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: Set the  startIndex greater than the max index of the array");

        try
        {
            int[] i1 = new int[6] { 2, 34, 56, 87, 23, 209 };
            Array.Reverse(i1, 7, 0);
            TestLibrary.TestFramework.LogError("107", "The ArgumentException was not thrown as expected");
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

        TestLibrary.TestFramework.BeginScenario("NegTest5: index and length do not specify a valid range in array");

        try
        {
            int[] i1 = new int[6] { 2, 34, 56, 87, 23, 209 };
            Array.Reverse(i1, 3, 4);
            TestLibrary.TestFramework.LogError("107", "The ArgumentException was not thrown as expected");
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
    #endregion
    #endregion

    public static int Main()
    {
        ArrayReverse2 test = new ArrayReverse2();

        TestLibrary.TestFramework.BeginTestCase("ArrayReverse2");

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

class A
{
    public A(int a)
    {
        this.a_value = a;
    }
    private int a_value;
    public int a
    {
        get
        {
            return this.a_value;
        }
    }
}
