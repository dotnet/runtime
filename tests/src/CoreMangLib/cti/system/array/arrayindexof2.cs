// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
///System.Array.IndexOf<T>(T[], T) 
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

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
            if (Array.IndexOf<int>(i1, 6) != 2)
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
            int result = Array.IndexOf<string>(s1, "Joan");
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
            int result = Array.IndexOf<string>(s1, "Mary");
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Input a null reference array ");

        try
        {
            string[] s1 = null;
            int result = Array.IndexOf<string>(s1, "Tom");
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
