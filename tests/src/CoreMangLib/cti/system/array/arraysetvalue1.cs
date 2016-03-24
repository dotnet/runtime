// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Array.SetValue(system.object,Int32)
/// </summary>
public class ArraySetValue1
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
        retVal = PosTest7() && retVal;

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Set the value of a string array ");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55) + 1;
            string[] s1 = new string[length];
            int index = TestLibrary.Generator.GetInt32(-55) % length;
            string value = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            s1.SetValue(value, index);
            if (s1[index] != value)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected.The index is: " + index.ToString());
                retVal = false;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Set the value of a char array ");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55) + 1;
            char[] s1 = new char[length];
            int index = TestLibrary.Generator.GetInt32(-55) % length;
            char value = TestLibrary.Generator.GetChar(-55);
            s1.SetValue(value, index);
            if (s1[index] != value)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected.The index is: " + index.ToString());
                retVal = false;
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Set the first value in an array");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55) + 1;
            int[] s1 = new int[length];
            int value = TestLibrary.Generator.GetInt32(-55);
            s1.SetValue(value, 0);
            if (s1[0] != value)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected.");
                retVal = false;
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Set the last value in an array");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55) + 1;
            int[] s1 = new int[length];
            int value = TestLibrary.Generator.GetInt32(-55);
            s1.SetValue(value, length - 1);
            if (s1[length - 1] != value)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected.");
                retVal = false;
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Set an int32 array using int16 type");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55) + 1;
            int[] s1 = new int[length];
            Int16 value = TestLibrary.Generator.GetInt16(-55);
            int index = TestLibrary.Generator.GetInt32(-55) % length;
            s1.SetValue(value, index);
            if (s1[index] != value)
            {
                TestLibrary.TestFramework.LogError("009", "The result is not the value as expected.");
                retVal = false;
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

        TestLibrary.TestFramework.BeginScenario("PosTest6: Set an element of a string array to null");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55) + 1;
            string[] s1 = new string[length];
            string[] s2 = new string[length];
            for (int i = 0; i < length; i++)
            {
                string value = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                s1[i] = value;
                s2[i] = value;
            }
            int index = TestLibrary.Generator.GetInt32(-55) % length;
            s1.SetValue(null, index);
            s2[index] = null;
            for (int i = 0; i < length; i++)
            {
                if (s1[i] != s2[i])
                {
                    TestLibrary.TestFramework.LogError("011", "The result is not the value as expected.");
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

    public bool PosTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest7: Set a customized array ");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55) + 1;
            A[] s1 = new A[length];
            int value = TestLibrary.Generator.GetInt32(-55);
            A a = new A(value);
            int index = TestLibrary.Generator.GetInt32(-55) % length;
            s1.SetValue(a, index);
            if (s1[index].a != value)
            {
                TestLibrary.TestFramework.LogError("013", "The result is not the value as expected.");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Negative Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The array is not one dimension");

        try
        {
            int[,] i1 = new int[2, 3] { { 2, 3, 5 }, { 34, 56, 77 } };
            i1.SetValue(0, 2);
            TestLibrary.TestFramework.LogError("103", "The ArgumentException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Set the value which cannot be cast to the element type of the current Array ");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55) + 1;
            string[] s1 = new string[length];
            int index = TestLibrary.Generator.GetInt32(-55) % length;
            char value = TestLibrary.Generator.GetChar(-55);
            s1.SetValue(value, index);
            TestLibrary.TestFramework.LogError("103", "The InvalidCastException was not thrown as expected");
            retVal = false;
        }
        catch (InvalidCastException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: The index argument is negative");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55) + 1;
            char[] s1 = new char[length];
            int index = -1;
            char value = TestLibrary.Generator.GetChar(-55);
            s1.SetValue(value, index);
            TestLibrary.TestFramework.LogError("105", "The IndexOutOfRangeException was not thrown as expected");
            retVal = false;
        }
        catch (IndexOutOfRangeException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest4: The index argument is greater than the max index of the array");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55) + 1;
            int[] s1 = new int[length];
            int index = length;
            int value = TestLibrary.Generator.GetInt32(-55);
            s1.SetValue(value, index);
            TestLibrary.TestFramework.LogError("107", "The IndexOutOfRangeException was not thrown as expected");
            retVal = false;
        }
        catch (IndexOutOfRangeException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest5: Array is length zero");

        try
        {
            int[] s1 = new int[0];
            int value = TestLibrary.Generator.GetInt32(-55);
            s1.SetValue(value, 0);
            TestLibrary.TestFramework.LogError("109", "The IndexOutOfRangeException was not thrown as expected");
            retVal = false;
        }
        catch (IndexOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("110", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ArraySetValue1 test = new ArraySetValue1();

        TestLibrary.TestFramework.BeginTestCase("ArraySetValue1");

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
