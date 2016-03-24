// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
///System.Array.IndexOf<T>(T[], T,System.Int32) 
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Set the argument T as int type ");

        try
        {
            int[] i1 = new int[5] { 3, 4, 6, 7, 8 };
            if (Array.IndexOf<int>(i1, 6, 0) != 2)
            {
                TestLibrary.TestFramework.LogError("001", " The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Set the argument T as string type ");

        try
        {
            string[] s1 = new string[5]{"Jack",
                "Tom",
                "mary",
                "Joan",
                "Michael"};
            int result = Array.IndexOf<string>(s1, "Joan", 3);
            if (result != 3)
            {
                TestLibrary.TestFramework.LogError("003", " The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Find out no expected value ");

        try
        {
            string[] s1 = new string[5]{"Jack",
                "Tom",
                "mary",
                "Joan",
                "Michael"};
            int result = Array.IndexOf<string>(s1, "Rabelais", 0);
            if (result != -1)
            {
                TestLibrary.TestFramework.LogError("005", " The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Find out no expected value because of the startIndex ");

        try
        {
            string[] s1 = new string[5]{"Bachham",
                "Bachham",
                "Bachham",
                "Joan",
                "Bachham"};
            int result = Array.IndexOf<string>(s1, "Joan", 4);
            if (result != -1)
            {
                TestLibrary.TestFramework.LogError("007", " The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Find out the fourth expected value ");

        try
        {
            string[] s1 = new string[5]{"Bachham",
                "Bachham",
                "Bachham",
                "Joan",
                "Bachham"};
            int result = Array.IndexOf<string>(s1, "Bachham", 4);
            if (result != 4)
            {
                TestLibrary.TestFramework.LogError("009", " The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest6: Find out the null element in an array of string ");

        try
        {
            string[] s1 = new string[5]{"Bachham",
                "Bachham",
                null,
                "Joan",
                "Bachham"};
            int result = Array.IndexOf<string>(s1, null, 0);
            if (result != 2)
            {
                TestLibrary.TestFramework.LogError("011", " The result is not the value as expected");
                retVal = false;
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: Input a null reference array ");

        try
        {
            string[] s1 = null;
            int result = Array.IndexOf<string>(s1, "Tom", 0);
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: The startIndex is negative.");

        try
        {
            int[] i1 = { 2, 3, 4, 4, 5 };
            int result = Array.IndexOf<int>(i1, 4, -1);
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: The startIndex is greater than the maxIndex of the array.");

        try
        {
            int[] i1 = new int[5] { 2, 3, 4, 4, 5 };
            int result = Array.IndexOf<int>(i1, 4, 6);
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
