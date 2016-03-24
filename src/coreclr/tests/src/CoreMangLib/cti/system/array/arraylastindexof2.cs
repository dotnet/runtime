// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Array.LastIndexOf<>(T[],T)
/// </summary>
public class ArrayIndexOf2
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Find out the last value which is equal to 3");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55);
            int[] i1 = new int[length];
            for (int i = 0; i < length; i++)
            {
                if (i % 3 == 0)
                {
                    i1[i] = 3;
                }
                else
                {
                    i1[i] = i;
                }
            }
            for (int a = length - 1; a > length - 4; a--)
            {
                if (a % 3 == 0)
                {
                    int i2 = Array.LastIndexOf<int>(i1, 3);
                    if (i2 != a)
                    {
                        TestLibrary.TestFramework.LogError("001", "The result is not the value as expected ");
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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Find the last string value in an array ");

        try
        {
            string[] s1 = new string[5]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Jack"};
            if (Array.LastIndexOf<string>(s1, "Jack") != 4)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest3: Find out the null element in an array of string ");

        try
        {
            string[] s1 = new string[7]{"Jack",
                "Mary",
                "Mike",
                 null,
                "Peter",
                 null,
                "Jack"};
            if (Array.LastIndexOf<string>(s1, null) != 5)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Find out no expected value in an array of string ");

        try
        {
            string[] s1 = new string[5]{"Jack",
                "Mary",
                "Mike",
                "Peter",
                "Jack"};
            if (Array.LastIndexOf<string>(s1, "Tom") != -1)
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Find out the last empty string ");

        try
        {
            string[] s1 = new string[5]{"",
                "",
                "",
                "",
                "Tom"};
            if (Array.LastIndexOf<string>(s1, "") != 3)
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The array is a null reference");

        try
        {
            string[] s1 = null;
            int i1 = Array.LastIndexOf<string>(s1, "");
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException is not thrown as expected ");
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
    #endregion
    #endregion

    public static int Main()
    {
        ArrayIndexOf2 test = new ArrayIndexOf2();

        TestLibrary.TestFramework.BeginTestCase("ArrayIndexOf2");

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
