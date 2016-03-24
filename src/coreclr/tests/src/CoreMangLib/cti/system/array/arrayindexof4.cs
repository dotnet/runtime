// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Array.IndexOf<>(T<>,T,System.Int32,System.Int32)
/// </summary>
public class ArrayIndexOf4
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Travel the array");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55);
            int[] i1 = new int[length];
            for (int i = 0; i < length; i++) //set the value for the array
            {
                i1[i] = i;
            }
            for (int a = 0; a < length; a++) //check every value using indexof<> method
            {
                int result = Array.IndexOf<int>(i1, a, 0, length);
                if (result != a)
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected. The index is:" + a.ToString());
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Search the value from the middle of the array");

        try
        {
            int[] i1 = new int[5] { 6, 3, 4, 7, 10 };
            int result = Array.IndexOf<int>(i1, 7, 3, 2);
            if (result != 3)
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Set the fourth argument \"count\"to zero");

        try
        {
            int[] i1 = new int[5] { 6, 6, 6, 6, 6 };
            int result = Array.IndexOf<int>(i1, 6, 2, 0);
            if (result != -1)
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Set two equal value in one array");

        try
        {
            string[] i1 = new string[6]{"Fix",
                "Right",
                "Correct",
                "Right",
                "Hello",
                "Back"};

            int result = Array.IndexOf<string>(i1, "Right", 0, 5);
            if (result != 1)
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Find out the second value which has a equal value before it");

        try
        {
            char[] i1 = { 'g', 't', 'r', 'd', 't', 'o' };
            int result = Array.IndexOf<char>(i1, 't', 3, 2);
            if (result != 4)
            {
                TestLibrary.TestFramework.LogError("009", "The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest6: Find out no expected value");

        try
        {
            int[] i1 = new int[5] { 1, 3, 7, 8, 2 };
            int result = Array.IndexOf<int>(i1, 9, 0, 5);
            if (result != -1)
            {
                TestLibrary.TestFramework.LogError("011", "The result is not the value as expected");
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

    public bool PosTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest7: Set the second argument as a different type value");

        try
        {
            int[] i1 = new int[5] { 5, 7, 15, 6, 0 };
            byte i2 = 7;
            int result = Array.IndexOf<int>(i1, i2, 0, 5);
            if (result != 1)
            {
                TestLibrary.TestFramework.LogError("013", "The result is not the value as expected");
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

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The array to be searched is null reference ");

        try
        {
            string[] s1 = null;
            int result = Array.IndexOf<string>(s1, "Tom", 0, 10);
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException is not thrown as expected");
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: The startIndex is less than zero ");

        try
        {
            int[] s1 = new int[5] { 3, 4, 5, 6, 7 };
            int result = Array.IndexOf<int>(s1, 4, -1, 3);
            TestLibrary.TestFramework.LogError("103", "The ArgumentOutOfRangeException is not thrown as expected");
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: The startIndex is greater than range of index for the array ");

        try
        {
            int[] s1 = new int[5] { 3, 4, 5, 6, 7 };
            int result = Array.IndexOf<int>(s1, 4, 6, 0);
            TestLibrary.TestFramework.LogError("105", "The ArgumentOutOfRangeException is not thrown as expected");
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

        TestLibrary.TestFramework.BeginScenario("NegTest4: The count is less than zero ");

        try
        {
            int[] s1 = new int[5] { 3, 4, 5, 6, 7 };
            int result = Array.IndexOf<int>(s1, 4, 0, -1);
            TestLibrary.TestFramework.LogError("107", "The ArgumentOutOfRangeException is not thrown as expected");
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

        TestLibrary.TestFramework.BeginScenario("NegTest5: The count exceed the end of the array ");

        try
        {
            int[] s1 = new int[5] { 3, 4, 5, 6, 7 };
            int result = Array.IndexOf<int>(s1, 4, 3, 8);
            TestLibrary.TestFramework.LogError("109", "The ArgumentOutOfRangeException is not thrown as expected");
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
        ArrayIndexOf4 test = new ArrayIndexOf4();

        TestLibrary.TestFramework.BeginTestCase("ArrayIndexOf4");

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
