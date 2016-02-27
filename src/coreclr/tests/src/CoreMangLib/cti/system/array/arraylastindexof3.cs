// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
///  system.Array.LastIndexOf<>(T[],T,int32,int32)
/// </summary>
public class ArrayIndexOf3
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

        TestLibrary.TestFramework.BeginScenario("PosTest1:Test the array of elements which have the same value  ");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55);
            int value = TestLibrary.Generator.GetByte(-55);
            int[] i1 = new int[length];
            for (int i = 0; i < length; i++)
            {
                i1[i] = value;
            }
            for (int i = length - 1; i >= 0; i--) // travel the array
            {
                if (Array.LastIndexOf<int>(i1, value, i, i + 1) != i)
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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Test the empty string  ");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55);
            string[] s1 = new string[length];
            for (int i = 0; i < length; i++)
            {
                s1[i] = "";
            }
            for (int i = length - 1; i >= 0; i--) // travel the array
            {
                if (Array.LastIndexOf<string>(s1, "", i, i + 1) != i)
                {
                    TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest3: Generic convert byte to int32");

        try
        {
            int[] i1 = new int[6] { 2356, 255, 988874, 90875, 255, 123334564 };
            byte b1 = 255;
            if (Array.LastIndexOf<int>(i1, b1, 5, 6) != 4)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest4: Test the array of char");

        try
        {
            char[] i1 = new char[6] { 't', 'r', 'c', '4', 'r', 'c' };
            char b1 = 'c';
            if (Array.LastIndexOf<char>(i1, b1, 4, 5) != 2)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected");
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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest5: Test the null element of the string array ");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                null,
                "Mary"};
            if (Array.LastIndexOf<string>(s1, null, 5, 6) != 4)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
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

    public bool PosTest6()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest6: Find out no result ");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Tim",
                "Mary"};
            if (Array.LastIndexOf<string>(s1, "mary", 5, 6) != -1)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("NegTest1:The array is a null reference ");

        try
        {
            string[] s1 = null;
            int i1 = Array.LastIndexOf<string>(s1, "", 1, 0);
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentNullException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: Set the negative startIndex argument");

        try
        {
            string[] s1 = new string[6]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Mary",
                "Joan"};
            int i1 = Array.LastIndexOf<string>(s1, "", -1, 3);
            TestLibrary.TestFramework.LogError("103", "The ArgumentOutOfRangeException was not thrown as expected");
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: Set the  startIndex greater than the max index of the array");

        try
        {
            int[] i1 = new int[6] { 2, 34, 56, 87, 23, 209 };
            int i2 = Array.LastIndexOf<int>(i1, 56, 6, 3);
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

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: Count argument is less than zero");
        try
        {
            int[] i1 = new int[6] { 2, 34, 56, 87, 23, 209 };
            int i2 = Array.LastIndexOf<int>(i1, 56, 3, -3);
            TestLibrary.TestFramework.LogError("107", "The ArgumentOutOfRangeException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest5: Count argument do not specify a valid section in array");
        try
        {
            int[] i1 = new int[6] { 2, 34, 56, 87, 23, 209 };
            int i2 = Array.LastIndexOf<int>(i1, 56, 3, 5);
            TestLibrary.TestFramework.LogError("109", "The ArgumentOutOfRangeException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
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
        ArrayIndexOf3 test = new ArrayIndexOf3();

        TestLibrary.TestFramework.BeginTestCase("ArrayIndexOf3");

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
