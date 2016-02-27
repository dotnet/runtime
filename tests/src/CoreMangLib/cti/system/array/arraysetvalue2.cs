// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Array.SetValue(system.object,Int32[])
/// </summary>
public class ArrayIndexOf1
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Test a two dimension array ");

        try
        {
            string[,] s1 = new string[2, 3];
            string s_value = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            s1.SetValue(s_value, 1, 0);
            if (s1[1, 0] != s_value)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected.");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Test a three dimension array ");

        try
        {
            int[, ,] s1 = new int[9, 7, 5];
            int i_value = TestLibrary.Generator.GetInt32(-55);
            s1.SetValue(i_value, 8, 6, 4);
            if (s1[8, 6, 4] != i_value)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected.");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Test a multiple dimension array ");

        try
        {
            string[, , , , , ,] s1 = new string[7, 7, 7, 7, 7, 7, 7];
            int[] i_index = new int[7] { 2, 3, 4, 6, 4, 5, 0 };
            string s_value = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            s1.SetValue(s_value, i_index);
            if (s1[2, 3, 4, 6, 4, 5, 0] != s_value)
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Test a multiple dimension array with customized type TypeA ");

        try
        {
            TypeA[, , , ,] s1 = new TypeA[9, 9, 9, 9, 9];
            int[] i_index = new int[5] { 2, 3, 4, 6, 8 };
            string value = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            TypeA typea = new TypeA(value);
            s1.SetValue(typea, i_index);
            if (s1[2, 3, 4, 6, 8].a != value)
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1:The second argument indices is null reference ");

        try
        {
            int[, ,] s1 = new int[9, 7, 5];
            int i_value = TestLibrary.Generator.GetInt32(-55);
            int[] i_index = null;
            s1.SetValue(i_value, i_index);
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException was not thrown as expected");
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

        TestLibrary.TestFramework.BeginScenario("NegTest2:The number of dimensions in the current Array is not equal to the number of elements in indices ");

        try
        {
            int[, ,] s1 = new int[9, 7, 5];
            int i_value = TestLibrary.Generator.GetInt32(-55);
            int[] i_index = new int[4] { 1, 3, 2, 7 };
            s1.SetValue(i_value, i_index);
            TestLibrary.TestFramework.LogError("103", "The ArgumentException was not thrown as expected");
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: Value cannot be cast to the element type of the current Array");

        try
        {
            int[, ,] s1 = new int[9, 7, 5];
            string i_value = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            int[] i_index = new int[3] { 1, 3, 4 };
            s1.SetValue(i_value, i_index);
            TestLibrary.TestFramework.LogError("105", "The InvalidCastException was not thrown as expected");
            retVal = false;

        }
        catch (InvalidCastException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest4: indices is outside the range of array");

        try
        {
            int[, ,] s1 = new int[9, 7, 5];
            int i_value = TestLibrary.Generator.GetInt32(-55);
            int[] i_index = new int[3] { 9, 3, 4 };
            s1.SetValue(i_value, i_index);
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
    #endregion
    #endregion

    public static int Main()
    {
        ArrayIndexOf1 test = new ArrayIndexOf1();

        TestLibrary.TestFramework.BeginTestCase("ArrayIndexOf1");

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
class TypeA
{
    public TypeA(string a)
    {
        this.a_value = a;
    }
    private string a_value;
    public string a
    {
        get
        {
            return this.a_value;
        }
    }
}
