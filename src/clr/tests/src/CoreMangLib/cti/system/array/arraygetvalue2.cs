// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Array.GetValue(System.Int32[])
/// </summary>
public class ArrayGetValue2
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Test the two dimension array ");

        try
        {
            int length1 = TestLibrary.Generator.GetByte(-55);
            int length2 = TestLibrary.Generator.GetByte(-55);
            int[,] i1 = new int[length1, length2];
            for (int i = 0; i < length1; i++)
            {
                for (int y = 0; y < length2; y++)
                {
                    i1[i, y] = i * length2 + y;
                }
            }

            for (int a = 0; a < length1; a++)
            {
                for (int b = 0; b < length2; b++)
                {
                    if ((int)(i1.GetValue(a, b)) != a * length2 + b)
                    {
                        TestLibrary.TestFramework.LogError("001", "The result is not the value as expected.");
                        retVal = false;
                    }
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Test the three dimension array ");

        try
        {
            string[, ,] s1 = new string[9, 9, 9];
            s1[3, 4, 5] = "Hello";
            if ((string)s1.GetValue(3, 4, 5) != "Hello")
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected. ");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Test the multiple dimension array ");

        try
        {
            int[, , , , , ,] d = new int[9, 9, 9, 9, 9, 9, 9];
            int[] index1 = new int[7] { 5, 4, 3, 2, 1, 3, 4 };
            int value = TestLibrary.Generator.GetInt32(-55);
            d.SetValue(value, index1);
            if ((int)d.GetValue(index1) != value)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected. ");
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: There are negative numbers in index[] ");

        try
        {
            int[, , ,] i1 = new int[5, 5, 5, 5];
            i1[2, 3, 4, 1] = TestLibrary.Generator.GetInt32(-55);
            int result = (int)i1.GetValue(2, -3, 4, -1);
            TestLibrary.TestFramework.LogError("101", "The IndexOutOfRangeException is not thrown as expected ");
            retVal = false;
        }
        catch (IndexOutOfRangeException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: The index is greater than the upper bound");

        try
        {
            int[, , ,] i1 = new int[5, 5, 5, 5];
            i1[2, 3, 4, 1] = TestLibrary.Generator.GetInt32(-55);
            int result = (int)i1.GetValue(2, 5, 4, 4);
            TestLibrary.TestFramework.LogError("103", "The IndexOutOfRangeException is not thrown as expected ");
            retVal = false;
        }
        catch (IndexOutOfRangeException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: The number of dimensions is not equal to the number of elements in index[]");

        try
        {
            int[, , , , ,] i1 = new int[9, 9, 9, 9, 9, 9];
            int result = (int)i1.GetValue(2, 5, 4, 4);
            TestLibrary.TestFramework.LogError("105", "The ArgumentException is not thrown as expected ");
            retVal = false;
        }
        catch (ArgumentException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest4: Set the argument index[] to null");

        try
        {
            int[, , , , ,] i1 = new int[9, 9, 9, 9, 9, 9];
            int[] i2 = null;
            int result = (int)i1.GetValue(i2);
            TestLibrary.TestFramework.LogError("107", "The ArgumentNullException is not thrown as expected ");
            retVal = false;
        }
        catch (ArgumentNullException)
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
        ArrayGetValue2 test = new ArrayGetValue2();

        TestLibrary.TestFramework.BeginTestCase("ArrayGetValue2");

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
